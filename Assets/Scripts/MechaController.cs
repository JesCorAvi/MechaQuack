using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador físico principal rediseñado para MechaQuack.
/// Integra:
/// - Caída libre real (9.81 m/s², sin flotación ni falsos positivos de suelo en riscos)
/// - Modelo de equilibrio por Péndulo Invertido Lineal (LIPM), XcoM y Polígono de Soporte 2D
/// - Recuperación con tiempo de reacción, pasos reactivos de captura (Capture Point) y contrapeso
/// - Pérdida de equilibrio real e inevitable pasado el Punto de No Retorno (sin correcciones mágicas)
/// - Active Ragdoll / colapso físico al caer y secuencia procedural pesada en 3 etapas para levantarse
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MechaController : MonoBehaviour
{
    public enum LocomotionState
    {
        Grounded,   // Equilibrado sobre uno o dos pies dentro del polígono de soporte
        Unstable,   // XcoM fuera del polígono: intentando contrapeso y paso de emergencia
        Airborne,   // Recién despegado del suelo (< 0.25s)
        Falling,    // Caída libre acelerada (9.81 m/s²)
        Landing,    // Absorbiendo energía cinética del impacto (compresión de rodillas y tambaleo)
        Fallen,     // Volcado en el suelo tras superar el punto de no retorno o impacto excesivo
        GettingUp   // Secuencia procedural pesada de reincorporación
    }

    private static MechaController _instance;
    public static MechaController Instance
    {
        get
        {
            if (_instance == null) _instance = Object.FindFirstObjectByType<MechaController>();
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Configuración Física Modular (Sin Números Mágicos)")]
    [SerializeField] private MechaPhysicsConfig physicsConfig;

    [Header("Referencias de Piernas Procedurales")]
    [SerializeField] private ProceduralLeg leftLeg;
    [SerializeField] private ProceduralLeg rightLeg;

    [Header("Partes Físicas y Visuales")]
    [SerializeField] private Transform cockpitTransform;
    [SerializeField] private Transform pelvisTransform;
    [SerializeField] private Transform leftLegTransform;
    [SerializeField] private Transform rightLegTransform;
    [SerializeField] private Transform leftArmTransform;
    [SerializeField] private Transform rightArmTransform;

    [Header("Armas y Artillería (Compatibilidad Cabina)")]
    [SerializeField] private Transform gatlingRotorTransform;
    [SerializeField] private Transform gatlingMuzzleTransform;
    [SerializeField] private Light gatlingMuzzleLight;
    [SerializeField] private float gatlingRoundsPerSecond = 12f;
    [SerializeField] private Transform missilePodTransform;
    [SerializeField] private Transform[] missileLaunchTubes;
    [SerializeField] private float missileCooldownDuration = 3.5f;
    [SerializeField] private float gunnerAimYaw = 0f;
    [SerializeField] private float gunnerAimPitch = 0f;
    [SerializeField] private Transform gatlingGimbalTransform;
    [SerializeField] private Transform missilePodGimbalTransform;

    [Header("Salud y Estado de Juego")]
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp = 100;

    [Header("Depuración Visual (Gizmos)")]
    [SerializeField] private bool drawBalanceGizmos = true;

    // Sub-sistemas físicos
    private Rigidbody _rb;
    private MechaBalanceModel _balanceModel;
    private LocomotionState _currentState = LocomotionState.Grounded;

    // Variables de estado físico en FixedUpdate
    private float _stateTimer = 0f;
    private float _airborneDuration = 0f;
    private float _peakDownwardSpeed = 0f;
    private float _recoveryReactionTimer = 0f;
    private float _landingCompressionOffset = 0f;
    private float _landingCompressionVelocity = 0f;
    private float _getUpProgress = 0f;
    private Quaternion _fallenStartRotation;
    private Vector3 _fallenStartPosition;
    private Vector3 _counterweightTorsoEuler = Vector3.zero;
    private int _lastSteppedLegIndex = 1;

    // Inputs y armas
    private float _inputMove = 0f;
    private float _inputTurn = 0f;
    private bool _isGatlingHolding = false;
    private bool _isGatlingFiring = false;
    private float _gatlingFireCooldown = 0f;
    private float _gatlingRotorSpeed = 0f;
    private float _gatlingRecoilZ = 0f;
    private float _missileRecoilZ = 0f;
    private float _currentMissileCooldown = 0f;
    private float _shakeIntensity = 0f;
    private int _score = 0;
    private bool _isGameOver = false;

    private Vector3 _initialCockpitLocalPos;
    private Quaternion _initialPelvisLocalRot = Quaternion.identity;
    private Quaternion _initialLeftArmLocalRot = Quaternion.identity;
    private Quaternion _initialRightArmLocalRot = Quaternion.identity;
    private Quaternion _initialGatlingGimbalRot = Quaternion.identity;
    private Quaternion _initialMissileGimbalRot = Quaternion.identity;

    private readonly List<CockpitFloorButton> _buttons = new List<CockpitFloorButton>();
    private DuckController _duckInCockpit;

    // Propiedades públicas (API intacta + Telemetría Física)
    public LocomotionState CurrentLocomotionState => _currentState;
    public MechaBalanceModel BalanceModel => _balanceModel;
    public MechaPhysicsConfig Config => physicsConfig;
    public Rigidbody Rb => _rb;
    public MechaPhysicsConfig PhysicsConfig => physicsConfig;
    public LocomotionState CurrentState => _currentState;
    public void SetMoveInput(float move) => _inputMove = move;
    public void SetTurnInput(float turn) => _inputTurn = turn;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public int Score => _score;
    public bool IsGameOver => _isGameOver;
    public bool IsOverturned => _currentState == LocomotionState.Fallen || Vector3.Angle(transform.up, Vector3.up) > 60f;
    public bool LeftGrounded => leftLeg != null && leftLeg.IsGrounded;
    public bool RightGrounded => rightLeg != null && rightLeg.IsGrounded;
    public bool IsAirborne => _currentState == LocomotionState.Airborne || _currentState == LocomotionState.Falling;
    public float LandingSquash => _landingCompressionOffset;
    public float IsAirborneDuration => _airborneDuration;
    public bool IsGatlingFiring => _isGatlingFiring || _gatlingRecoilZ > 0.05f;
    public float LaserCooldown => _currentMissileCooldown;
    public float MissileCooldown => _currentMissileCooldown;
    public float ShakeIntensity => _shakeIntensity;
    public float GunnerAimYaw => gunnerAimYaw;
    public float GunnerAimPitch => gunnerAimPitch;
    public void TriggerFootstepShake(float intensity) => _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
    public GameObject PunchWaveObject => null;
    public GameObject LaserBeamObject => null;

    public void AdjustGunnerAim(float deltaYaw, float deltaPitch)
    {
        gunnerAimYaw = Mathf.Clamp(gunnerAimYaw + deltaYaw, -48f, 48f);
        gunnerAimPitch = Mathf.Clamp(gunnerAimPitch + deltaPitch, -20f, 36f);
    }

    public void SetGunnerAim(float yaw, float pitch)
    {
        gunnerAimYaw = Mathf.Clamp(yaw, -48f, 48f);
        gunnerAimPitch = Mathf.Clamp(pitch, -20f, 36f);
    }

    public void ResetGunnerAim()
    {
        gunnerAimYaw = 0f;
        gunnerAimPitch = 0f;
    }

    public Vector3 GetGunnerAimDirection()
    {
        Quaternion aimOffset = Quaternion.Euler(-gunnerAimPitch, gunnerAimYaw, 0f);
        return (transform.rotation * aimOffset * Vector3.forward).normalized;
    }

    public Vector3 GetGunnerAimTargetPoint(float range = 280f)
    {
        Vector3 origin = (cockpitTransform != null) ? cockpitTransform.position + Vector3.up * 4.4f : transform.position + Vector3.up * 28f;
        Vector3 dir = GetGunnerAimDirection();
        int mask = ~LayerMask.GetMask("Player", "Ignore Raycast");
        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }
        return origin + dir * range;
    }

    private void Awake()
    {
        Instance = this;
        Application.runInBackground = true;
        currentHp = maxHp;

        // Crear configuración por defecto en memoria si no se asignó un ScriptableObject en el Inspector
        if (physicsConfig == null)
        {
            physicsConfig = ScriptableObject.CreateInstance<MechaPhysicsConfig>();
            physicsConfig.Set52mColossusScale();
        }

        _balanceModel = new MechaBalanceModel(physicsConfig);

        _rb = GetComponent<Rigidbody>();
        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
        ConfigureRigidbody();

        AutoWireReferences();
    }

    private void ConfigureRigidbody()
    {
        _rb.mass = physicsConfig.mass;
        _rb.useGravity = false; // Gestionamos la aceleración gravitacional exacta en FixedUpdate para control estricto de escala
        _rb.linearDamping = physicsConfig.groundedLinearDamping;
        _rb.angularDamping = physicsConfig.groundedAngularDamping;
        _rb.centerOfMass = physicsConfig.localCenterOfMass;
        _rb.inertiaTensor = physicsConfig.inertiaTensor;
        _rb.inertiaTensorRotation = Quaternion.identity;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void AutoWireReferences()
    {
        if (leftLeg == null && leftLegTransform != null)
            leftLeg = leftLegTransform.GetComponent<ProceduralLeg>();
        if (rightLeg == null && rightLegTransform != null)
            rightLeg = rightLegTransform.GetComponent<ProceduralLeg>();

        if (leftLeg == null || rightLeg == null)
        {
            var legs = GetComponentsInChildren<ProceduralLeg>();
            foreach (var l in legs)
            {
                if (l.name.ToLower().Contains("left") || l.transform.localPosition.x < 0f) leftLeg = l;
                else rightLeg = l;
            }
        }

        if (leftLeg != null)
        {
            leftLeg.SetConfig(physicsConfig);
            leftLeg.OnStepTouchdownDetailed += OnFootTouchdown;
        }

        if (rightLeg != null)
        {
            rightLeg.SetConfig(physicsConfig);
            rightLeg.OnStepTouchdownDetailed += OnFootTouchdown;
        }

        if (pelvisTransform == null)
        {
            Transform p = transform.Find("Pelvis");
            pelvisTransform = p != null ? p : cockpitTransform;
        }

        if (cockpitTransform != null) _initialCockpitLocalPos = cockpitTransform.localPosition;
        if (pelvisTransform != null) _initialPelvisLocalRot = pelvisTransform.localRotation;
        if (leftArmTransform != null) _initialLeftArmLocalRot = leftArmTransform.localRotation;
        if (rightArmTransform != null) _initialRightArmLocalRot = rightArmTransform.localRotation;

        if (gatlingGimbalTransform == null) gatlingGimbalTransform = leftArmTransform;
        if (missilePodGimbalTransform == null) missilePodGimbalTransform = missilePodTransform;

        if (gatlingGimbalTransform != null) _initialGatlingGimbalRot = gatlingGimbalTransform.localRotation;
        if (missilePodGimbalTransform != null) _initialMissileGimbalRot = missilePodGimbalTransform.localRotation;

        _duckInCockpit = FindFirstObjectByType<DuckController>();
        _buttons.AddRange(GetComponentsInChildren<CockpitFloorButton>());
    }

    private void Update()
    {
        if (_isGameOver) return;

        UpdateTimers(Time.deltaTime);
        ReadButtonsInput();
        UpdateGatlingWeapon(Time.deltaTime);
        UpdateGimbalsAndCockpitVisuals(Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (_isGameOver) return;

        float dt = Time.deltaTime;
        if (leftLeg != null) leftLeg.UpdateKinematics(_currentState, dt);
        if (rightLeg != null) rightLeg.UpdateKinematics(_currentState, dt);
        UpdateCounterweightVisuals(dt);
    }

    // =========================================================================
    // NÚCLEO FÍSICO EN PASO DE TIEMPO FIJO (FixedUpdate)
    // =========================================================================
    private void FixedUpdate()
    {
        if (_isGameOver) return;

        float dt = Time.fixedDeltaTime;
        _stateTimer += dt;

        // 1. Sondeo físico estricto de suelo (sin falsos positivos más allá de maxLegExtension)
        bool lGrounded = leftLeg != null && leftLeg.ProbeGroundPhysics(dt);
        bool rGrounded = rightLeg != null && rightLeg.ProbeGroundPhysics(dt);

        // 2. Evaluar modelo de Péndulo Invertido (CoM, XcoM, Polígono de Soporte y Punto de No Retorno)
        Vector3 lPos = leftLeg != null ? leftLeg.CurrentSolePosition : transform.position;
        Quaternion lRot = leftLeg != null ? leftLeg.CurrentFootRotation : transform.rotation;
        Vector3 rPos = rightLeg != null ? rightLeg.CurrentSolePosition : transform.position;
        Quaternion rRot = rightLeg != null ? rightLeg.CurrentFootRotation : transform.rotation;

        bool isStepping = (leftLeg != null && leftLeg.IsStepping) || (rightLeg != null && rightLeg.IsStepping);
        bool isVoluntary = Mathf.Abs(_inputMove) > 0.01f || Mathf.Abs(_inputTurn) > 0.01f;
        _balanceModel.Evaluate(_rb, transform, lGrounded, lPos, lRot, rGrounded, rPos, rRot, dt, isStepping, isVoluntary);

        // 3. Aplicar Gravedad Exacta (siempre activa en todos los estados)
        ApplyRealGravity();

        // 4. Ejecutar lógica de estado físico
        switch (_currentState)
        {
            case LocomotionState.Grounded:
                SimulateGroundedState(lGrounded, rGrounded, dt);
                break;

            case LocomotionState.Unstable:
                SimulateUnstableState(lGrounded, rGrounded, dt);
                break;

            case LocomotionState.Airborne:
            case LocomotionState.Falling:
                SimulateAirborneState(lGrounded, rGrounded, dt);
                break;

            case LocomotionState.Landing:
                SimulateLandingState(lGrounded, rGrounded, dt);
                break;

            case LocomotionState.Fallen:
                SimulateFallenState(dt);
                break;

            case LocomotionState.GettingUp:
                SimulateGettingUpState(dt);
                break;
        }
    }

    /// <summary>
    /// Aplica aceleración gravitacional real (9.81 m/s² * gravityScale) respetando la velocidad terminal.
    /// </summary>
    private void ApplyRealGravity()
    {
        if (_rb.linearVelocity.y > -physicsConfig.terminalVelocity)
        {
            float g = physicsConfig.gravity * physicsConfig.gravityScale;
            _rb.AddForce(Vector3.down * g, ForceMode.Acceleration);
        }
    }

    private void SimulateGroundedState(bool lGrounded, bool rGrounded, float dt)
    {
        if (!lGrounded && !rGrounded)
        {
            TransitionToState(LocomotionState.Airborne);
            return;
        }

        _rb.linearDamping = physicsConfig.groundedLinearDamping;
        _rb.angularDamping = physicsConfig.groundedAngularDamping;

        ApplyLegSuspension(lGrounded, rGrounded, 0f);
        ApplySlopeSlidingForces(lGrounded, rGrounded);
        ApplyUprightPDControl(1.0f);
        ApplyVoluntaryLocomotion(lGrounded, rGrounded);
        CoordinateNormalGait();

        // Comprobar si pierde estabilidad física real
        var bal = _balanceModel.CurrentState;
        if (bal.beyondPointOfNoReturn)
        {
            TransitionToState(LocomotionState.Fallen);
        }
        else if (bal.tiltAngleDegrees > physicsConfig.maxStableSlopeAngle || (!lGrounded && rGrounded) || (lGrounded && !rGrounded) || (!bal.isStaticallyStable && bal.tiltAngleDegrees > 15f))
        {
            TransitionToState(LocomotionState.Unstable);
        }
    }

    private void SimulateUnstableState(bool lGrounded, bool rGrounded, float dt)
    {
        if (!lGrounded && !rGrounded)
        {
            TransitionToState(LocomotionState.Airborne);
            return;
        }

        var bal = _balanceModel.CurrentState;

        // 1. Si cruzó el Punto de No Retorno, colapsa inevitablemente (NO se corrige mágicamente)
        if (bal.beyondPointOfNoReturn)
        {
            Debug.Log($"[FÍSICA MECHA] Punto de No Retorno superado (Margen: {bal.stabilityMargin:F2}m, Inclinación: {bal.tiltAngleDegrees:F1}°). ¡Colapso físico!");
            TransitionToState(LocomotionState.Fallen);
            return;
        }

        // 2. Si recuperó estabilidad (ambas patas con apoyo firme y torso razonablemente erguido), vuelve a Grounded
        if (lGrounded && rGrounded && bal.tiltAngleDegrees < physicsConfig.maxStableSlopeAngle * 0.75f)
        {
            TransitionToState(LocomotionState.Grounded);
            return;
        }

        ApplyLegSuspension(lGrounded, rGrounded, 0.15f);

        // Permitir locomoción voluntaria y coordinación de paso para que el mecha pueda corregir el desequilibrio
        ApplyVoluntaryLocomotion(lGrounded, rGrounded);
        CoordinateNormalGait();

        // 3. El par estabilizador de los tobillos se degrada según la inclinación
        float tiltFactor = Mathf.Clamp01(1f - (bal.tiltAngleDegrees / physicsConfig.criticalTiltAngle));
        float ankleAuthority = Mathf.Lerp(0.35f, 0.90f, tiltFactor);
        ApplyUprightPDControl(ankleAuthority);

        // Aplicar torque físico de vuelco gravitacional real (r x mg)
        Vector3 toppleTorque = _balanceModel.CalculateGravitationalToppleTorque();
        _rb.AddTorque(toppleTorque, ForceMode.Force);

        // 4. Intentar paso de emergencia tras el tiempo de reacción humano/hidráulico si no está dando paso
        _recoveryReactionTimer += dt;
        bool isStepping = (leftLeg != null && leftLeg.IsStepping) || (rightLeg != null && rightLeg.IsStepping);
        if (_recoveryReactionTimer >= physicsConfig.recoveryReactionTime && !isStepping)
        {
            TryExecuteEmergencyCaptureStep(bal.idealCaptureStepPoint);
        }
    }

    private void SimulateAirborneState(bool lGrounded, bool rGrounded, float dt)
    {
        // En el aire: CERO suspensión y resistencia aerodinámica mínima para caída real (10m = ~1.42s)
        _rb.linearDamping = physicsConfig.airborneLinearDamping;
        _rb.angularDamping = physicsConfig.airborneAngularDamping;

        _airborneDuration += dt;
        float downSpeed = -_rb.linearVelocity.y;
        if (downSpeed > _peakDownwardSpeed)
        {
            _peakDownwardSpeed = downSpeed;
        }

        if (_currentState == LocomotionState.Airborne && _airborneDuration >= physicsConfig.fallingStateTimeThreshold)
        {
            _currentState = LocomotionState.Falling;
        }

        // Detectar contacto real de aterrizaje (solo cuando las piernas tocan suelo dentro de su alcance real y va descendiendo)
        if ((lGrounded || rGrounded) && _rb.linearVelocity.y <= 0.5f && _airborneDuration > 0.06f)
        {
            EvaluateTouchdownImpact(_peakDownwardSpeed, _airborneDuration);
        }
    }

    private void SimulateLandingState(bool lGrounded, bool rGrounded, float dt)
    {
        if (!lGrounded && !rGrounded)
        {
            TransitionToState(LocomotionState.Airborne);
            return;
        }

        _rb.linearDamping = physicsConfig.groundedLinearDamping * 1.5f;
        _rb.angularDamping = physicsConfig.groundedAngularDamping;

        // Recuperar progresivamente la compresión de rodillas del impacto
        _landingCompressionOffset = Mathf.SmoothDamp(_landingCompressionOffset, 0f, ref _landingCompressionVelocity, 0.24f);

        ApplyLegSuspension(lGrounded, rGrounded, _landingCompressionOffset);
        ApplyUprightPDControl(0.75f);

        if (_balanceModel.CurrentState.beyondPointOfNoReturn)
        {
            TransitionToState(LocomotionState.Fallen);
            return;
        }

        if (_stateTimer >= physicsConfig.landingRecoveryDuration)
        {
            TransitionToState(LocomotionState.Grounded);
        }
    }

    private void SimulateFallenState(float dt)
    {
        // En estado Fallen: SIN suspensión vertical mágica y SIN enderezamiento automático.
        // Actúa el par gravitacional completo hasta que el chasis descansa físicamente sobre el terreno.
        _rb.linearDamping = 0.45f;
        _rb.angularDamping = 0.85f;

        float tilt = _balanceModel.CurrentState.tiltAngleDegrees;
        if (tilt < 72f)
        {
            Vector3 toppleDir = Vector3.Cross(Vector3.up, transform.up).normalized;
            if (toppleDir.sqrMagnitude < 0.01f) toppleDir = transform.forward;

            // Torque gravitacional pesado para completar el derribo hasta el suelo
            Vector3 gravityTopple = _balanceModel.CalculateGravitationalToppleTorque();
            if (gravityTopple.sqrMagnitude < 1000f)
            {
                float leverArm = physicsConfig.nominalRideHeight * 0.6f * Mathf.Sin(Mathf.Max(15f, tilt) * Mathf.Deg2Rad);
                float torqueMag = physicsConfig.mass * physicsConfig.gravity * leverArm * physicsConfig.gravityToppleMultiplier;
                gravityTopple = toppleDir * torqueMag;
            }
            _rb.AddTorque(gravityTopple, ForceMode.Force);
        }

        // Amortiguación pasiva de articulaciones (Active Ragdoll pesado)
        if (leftLeg != null)
        {
            Vector3 foldedPosL = transform.TransformPoint(new Vector3(-physicsConfig.footHalfWidth * 1.5f, -physicsConfig.nominalRideHeight * 0.45f, 0f));
            leftLeg.SetDirectFootTarget(foldedPosL, transform.rotation, 6f, dt);
        }
        if (rightLeg != null)
        {
            Vector3 foldedPosR = transform.TransformPoint(new Vector3(physicsConfig.footHalfWidth * 1.5f, -physicsConfig.nominalRideHeight * 0.45f, 0f));
            rightLeg.SetDirectFootTarget(foldedPosR, transform.rotation, 6f, dt);
        }

        // Si pasa el tiempo de aturdimiento y el jugador intenta moverse (o pulsa acción), inicia la recuperación
        if (_stateTimer >= physicsConfig.minFallenStunTime && (Mathf.Abs(_inputMove) > 0.1f || Mathf.Abs(_inputTurn) > 0.1f))
        {
            RecoverFromTumble();
        }
    }

    /// <summary>
    /// Secuencia procedural pesada de reincorporación en 3 etapas.
    /// 1) Apoyo de brazos/rodillas y freno de giro
    /// 2) Impulso vertical y elevación de pelvis
    /// 3) Estabilización bípeda y reenganche de control
    /// </summary>
    private void SimulateGettingUpState(float dt)
    {
        _getUpProgress += dt / Mathf.Max(1.0f, physicsConfig.getUpSequenceDuration);
        float t = Mathf.Clamp01(_getUpProgress);

        _rb.linearDamping = physicsConfig.groundedLinearDamping * 1.8f;
        _rb.angularDamping = physicsConfig.groundedAngularDamping * 1.5f;

        Quaternion uprightTarget = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        if (t < 0.35f)
        {
            // Etapa 1: Apoyo de brazos/rodillas, orientar hacia vertical suavemente
            float stage1T = t / 0.35f;
            Quaternion currentRotTarget = Quaternion.Slerp(_fallenStartRotation, uprightTarget, stage1T * 0.45f);
            Vector3 rotDelta = (currentRotTarget * Quaternion.Inverse(transform.rotation)).eulerAngles;
            NormalizeAngles(ref rotDelta);
            _rb.AddTorque(rotDelta * (physicsConfig.mass * 12f), ForceMode.Force);

            // Pies recogen hacia abajo del centro de gravedad
            if (leftLeg != null)
            {
                Vector3 braceL = transform.TransformPoint(new Vector3(-physicsConfig.footHalfWidth * 1.2f, -physicsConfig.nominalRideHeight * 0.65f, 0f));
                leftLeg.SetDirectFootTarget(braceL, uprightTarget, 8f, dt);
            }
            if (rightLeg != null)
            {
                Vector3 braceR = transform.TransformPoint(new Vector3(physicsConfig.footHalfWidth * 1.2f, -physicsConfig.nominalRideHeight * 0.65f, 0f));
                rightLeg.SetDirectFootTarget(braceR, uprightTarget, 8f, dt);
            }
        }
        else if (t < 0.75f)
        {
            // Etapa 2: Elevación de cadera y empuje de piernas
            float stage2T = (t - 0.35f) / 0.40f;
            _rb.AddForce(Vector3.up * (physicsConfig.mass * physicsConfig.gravity * 1.25f), ForceMode.Force);
            ApplyUprightPDControl(0.5f + stage2T * 0.5f);

            bool lG = leftLeg != null && leftLeg.ProbeGroundPhysics(dt);
            bool rG = rightLeg != null && rightLeg.ProbeGroundPhysics(dt);
            ApplyLegSuspension(lG, rG, physicsConfig.nominalRideHeight * (1f - stage2T) * 0.5f);
        }
        else
        {
            // Etapa 3: Estabilización bípeda final
            ApplyUprightPDControl(1.0f);
            bool lG = leftLeg != null && leftLeg.ProbeGroundPhysics(dt);
            bool rG = rightLeg != null && rightLeg.ProbeGroundPhysics(dt);
            ApplyLegSuspension(lG, rG, 0f);

            if (t >= 1.0f)
            {
                TransitionToState(LocomotionState.Grounded);
                ProceduralAudio.Instance?.PlayHydraulicRecover();
                FloatingText.Spawn(transform.position + Vector3.up * 7f, "¡EN PIE!", Color.green, 0.45f);
            }
        }
    }

    private void ApplyLegSuspension(bool lGrounded, bool rGrounded, float compressionOffset)
    {
        if (!lGrounded && !rGrounded) return;

        float totalWeight = physicsConfig.mass * physicsConfig.gravity * physicsConfig.gravityScale;
        float baseEquilibrium = (lGrounded && rGrounded) ? (totalWeight * 0.5f) : totalWeight;

        if (lGrounded && leftLeg != null)
        {
            ApplySingleLegSpring(leftLeg, baseEquilibrium, compressionOffset);
        }

        if (rGrounded && rightLeg != null)
        {
            ApplySingleLegSpring(rightLeg, baseEquilibrium, compressionOffset);
        }
    }

    private void ApplySingleLegSpring(ProceduralLeg leg, float baseEquilibriumForce, float compressionOffset)
    {
        float dist = leg.CurrentHipToGroundDistance;
        if (dist > physicsConfig.maxLegExtension) return; // NUNCA aplicar fuerza al vacío

        Vector3 hipPos = leg.HipWorldPosition;
        float targetRide = physicsConfig.nominalRideHeight - compressionOffset;
        float compression = targetRide - dist;

        float vertVel = Vector3.Dot(_rb.GetPointVelocity(hipPos), transform.up);
        float springForce = baseEquilibriumForce + (compression * physicsConfig.legSpringStiffness) - (vertVel * physicsConfig.legSpringDamping);

        _rb.AddForceAtPosition(transform.up * Mathf.Max(0f, springForce), hipPos, ForceMode.Force);
    }

    private void ApplySlopeSlidingForces(bool lGrounded, bool rGrounded)
    {
        bool sliding = (lGrounded && leftLeg != null && leftLeg.IsSlidingOnSlope) ||
                       (rGrounded && rightLeg != null && rightLeg.IsSlidingOnSlope);

        if (!sliding) return;

        Vector3 avgNormal = Vector3.up;
        if (lGrounded && rGrounded && leftLeg != null && rightLeg != null)
            avgNormal = (leftLeg.GroundNormal + rightLeg.GroundNormal).normalized;
        else if (lGrounded && leftLeg != null)
            avgNormal = leftLeg.GroundNormal;
        else if (rGrounded && rightLeg != null)
            avgNormal = rightLeg.GroundNormal;

        Vector3 downSlopeDir = Vector3.ProjectOnPlane(Vector3.down, avgNormal).normalized;
        _rb.AddForce(downSlopeDir * (physicsConfig.mass * physicsConfig.slopeSlideAcceleration), ForceMode.Force);
    }

    private void ApplyUprightPDControl(float authority)
    {
        Vector3 rotAxis = Vector3.Cross(transform.up, Vector3.up);
        float angleRad = Vector3.Angle(transform.up, Vector3.up) * Mathf.Deg2Rad;

        Vector3 targetTorque = rotAxis * (angleRad * physicsConfig.uprightTorqueKp) - _rb.angularVelocity * physicsConfig.uprightTorqueKd;
        float maxAllowed = physicsConfig.maxAnkleCorrectiveTorque * authority;

        if (targetTorque.magnitude > maxAllowed)
        {
            targetTorque = targetTorque.normalized * maxAllowed;
        }

        _rb.AddTorque(targetTorque, ForceMode.Force);
    }

    private void ApplyVoluntaryLocomotion(bool lGrounded, bool rGrounded)
    {
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
        if (tiltAngle > physicsConfig.criticalTiltAngle) return;

        if (Mathf.Abs(_inputMove) > 0.01f)
        {
            Vector3 moveForce = transform.forward * (_inputMove * physicsConfig.moveForce);
            _rb.AddForce(moveForce, ForceMode.Force);
        }

        if (Mathf.Abs(_inputTurn) > 0.01f)
        {
            Vector3 turn = Vector3.up * (_inputTurn * physicsConfig.turnTorque);
            _rb.AddTorque(turn, ForceMode.Force);
        }

        // Limitar velocidad horizontal máxima
        Vector3 horizVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        if (horizVel.magnitude > physicsConfig.maxWalkSpeed)
        {
            Vector3 clamped = horizVel.normalized * physicsConfig.maxWalkSpeed;
            _rb.linearVelocity = new Vector3(clamped.x, _rb.linearVelocity.y, clamped.z);
        }
    }

    private void CoordinateNormalGait()
    {
        if (leftLeg == null || rightLeg == null) return;
        if (leftLeg.IsStepping || rightLeg.IsStepping) return;

        bool lWants = leftLeg.WantsNormalStep(out float lUrg);
        bool rWants = rightLeg.WantsNormalStep(out float rUrg);

        if (!lWants && !rWants) return;

        if (lWants && rWants)
        {
            if (_lastSteppedLegIndex == 1)
            {
                leftLeg.TryTakeStep(leftLeg.HomeTargetPosition, -1f, false);
                _lastSteppedLegIndex = 0;
            }
            else
            {
                rightLeg.TryTakeStep(rightLeg.HomeTargetPosition, -1f, false);
                _lastSteppedLegIndex = 1;
            }
        }
        else if (lWants)
        {
            leftLeg.TryTakeStep(leftLeg.HomeTargetPosition, -1f, false);
            _lastSteppedLegIndex = 0;
        }
        else
        {
            rightLeg.TryTakeStep(rightLeg.HomeTargetPosition, -1f, false);
            _lastSteppedLegIndex = 1;
        }
    }

    private void TryExecuteEmergencyCaptureStep(Vector3 idealCapturePoint)
    {
        if (leftLeg == null || rightLeg == null) return;

        // Elegir la pierna que no esté pisando o que esté más próxima al capture point
        float distL = Vector3.Distance(leftLeg.CurrentFootPosition, idealCapturePoint);
        float distR = Vector3.Distance(rightLeg.CurrentFootPosition, idealCapturePoint);

        ProceduralLeg chosen = (!leftLeg.IsStepping && (leftLeg.IsStepping == rightLeg.IsStepping ? distL < distR : true)) ? leftLeg : rightLeg;
        if (chosen.TryTakeStep(idealCapturePoint, physicsConfig.emergencyStepDuration, true))
        {
            _recoveryReactionTimer = 0f;
            ProceduralAudio.Instance?.PlayQuack();
        }
    }

    private void EvaluateTouchdownImpact(float downwardSpeed, float airTime)
    {
        if (downwardSpeed < 3.0f && airTime < 0.1f)
        {
            TransitionToState(LocomotionState.Grounded);
            return;
        }

        Debug.Log($"[GAME] ¡ATERRIZAJE MECHA! Velocidad vertical: {downwardSpeed:F1} m/s, Tiempo en aire: {airTime:F2}s");

        _landingCompressionOffset = Mathf.Clamp(downwardSpeed * 0.14f, 0.4f, physicsConfig.nominalRideHeight * 0.35f);

        float trauma = Mathf.Clamp01(downwardSpeed / 20.0f);
        _shakeIntensity = Mathf.Max(_shakeIntensity, trauma * 1.35f);

        if (downwardSpeed > 9.0f)
        {
            ProceduralAudio.Instance?.PlayCrashLand();
            ProceduralAudio.Instance?.PlayQuack();
        }
        else
        {
            ProceduralAudio.Instance?.PlayMechaStep();
        }

        if (downwardSpeed > 14.0f)
        {
            TriggerHitStop(0.065f, 0.04f);
        }

        Vector3 leftFootPos = (leftLeg != null) ? leftLeg.CurrentFootPosition : transform.position - transform.right * 8.4f;
        Vector3 rightFootPos = (rightLeg != null) ? rightLeg.CurrentFootPosition : transform.position + transform.right * 8.4f;
        VoxelExplosion.Spawn(leftFootPos, new Color(0.72f, 0.68f, 0.58f), Mathf.RoundToInt(Mathf.Clamp(downwardSpeed * 2.2f, 15f, 40f)), 0.6f, 18f);
        VoxelExplosion.Spawn(rightFootPos, new Color(0.72f, 0.68f, 0.58f), Mathf.RoundToInt(Mathf.Clamp(downwardSpeed * 2.2f, 15f, 40f)), 0.6f, 18f);

        if (_duckInCockpit != null)
        {
            float duckLaunchY = Mathf.Clamp(downwardSpeed * 0.75f, 4.5f, 18f);
            Vector3 duckJolt = new Vector3(Random.Range(-4f, 4f), duckLaunchY, Random.Range(-4f, 4f));
            _duckInCockpit.ApplyExternalNudge(duckJolt);
        }

        Vector3 textPos = transform.position + Vector3.up * 7f;
        if (downwardSpeed > 18.0f)
            FloatingText.Spawn(textPos, "💥 ¡¡CATAPLUM!! ¡ATERRIZAJE FORZOSO! 🦆", Color.red, 0.55f);
        else if (downwardSpeed > 9.0f)
            FloatingText.Spawn(textPos, "💥 ¡CHOFFF! ¡44 TONELADAS!", new Color(1f, 0.65f, 0.1f), 0.45f);

        float tilt = Vector3.Angle(transform.up, Vector3.up);
        if (downwardSpeed > physicsConfig.collapseImpactSpeedThreshold || tilt > physicsConfig.maxLandingTiltAngle)
        {
            TransitionToState(LocomotionState.Fallen);
        }
        else
        {
            TransitionToState(LocomotionState.Landing);
        }
    }

    private void TransitionToState(LocomotionState newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;
        _stateTimer = 0f;

        if (newState == LocomotionState.Airborne)
        {
            _airborneDuration = 0f;
            _peakDownwardSpeed = 0f;
        }
        else if (newState == LocomotionState.Fallen)
        {
            _fallenStartRotation = transform.rotation;
            _fallenStartPosition = transform.position;
            _balanceModel.ResetTimers();
            ProceduralAudio.Instance?.PlayQuack();
            FloatingText.Spawn(transform.position + Vector3.up * 6f, "¡MECHA VOLCADO! PISOTÓN (E) PARA ENDEREZAR", Color.yellow, 0.45f);
        }
        else if (newState == LocomotionState.GettingUp)
        {
            _getUpProgress = 0f;
            _fallenStartRotation = transform.rotation;
        }
    }

    public void RecoverFromTumble()
    {
        if (_currentState != LocomotionState.Fallen && Vector3.Angle(transform.up, Vector3.up) <= 45f) return;

        ProceduralAudio.Instance?.PlayHydraulicRecover();
        FloatingText.Spawn(transform.position + Vector3.up * 7f, "¡PATALETA HIDRÁULICA!", Color.yellow, 0.45f);

        // Impulso vertical y reseteo de velocidad angular
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x * 0.2f, 22f, _rb.linearVelocity.z * 0.2f);
        _rb.angularVelocity = Vector3.zero;

        Quaternion uprightRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        transform.rotation = uprightRot;
        _rb.rotation = uprightRot;

        TransitionToState(LocomotionState.GettingUp);
    }

    private void OnFootTouchdown(Vector3 pos, Vector3 norm, bool isEmergency)
    {
        ProceduralAudio.Instance?.PlayMechaStep();
        TriggerFootstepShake(isEmergency ? 0.28f : 0.15f);

        if (_duckInCockpit != null && _duckInCockpit.IsGrounded)
        {
            _duckInCockpit.ApplyExternalNudge(new Vector3(Random.Range(-0.8f, 0.8f), 0f, 0f));
        }
    }

    // =========================================================================
    // ARMAS, COCKPIT Y COMPATIBILIDAD CON SISTEMAS
    // =========================================================================
    private void UpdateTimers(float dt)
    {
        if (_currentMissileCooldown > 0f)
            _currentMissileCooldown = Mathf.Max(0f, _currentMissileCooldown - dt);

        if (_shakeIntensity > 0f)
            _shakeIntensity = Mathf.Max(0f, _shakeIntensity - dt * 2.5f);
    }

    private void ReadButtonsInput()
    {
        _inputMove = 0f;
        _inputTurn = 0f;
        _isGatlingHolding = false;

        if (_buttons.Count == 0)
        {
            _buttons.AddRange(GetComponentsInChildren<CockpitFloorButton>());
        }

        for (int i = 0; i < _buttons.Count; i++)
        {
            var btn = _buttons[i];
            if (btn == null || !btn.isPressed) continue;

            switch (btn.buttonId)
            {
                case "FWD": _inputMove += 1.0f; break;
                case "BWD": _inputMove -= 0.7f; break;
                case "LEFT": _inputTurn -= 1.0f; break;
                case "RIGHT": _inputTurn += 1.0f; break;
                case "GATLING":
                case "PUNCH":
                    if (btn.type == CockpitFloorButton.ButtonType.Hold)
                    {
                        _isGatlingHolding = true;
                    }
                    break;
            }
        }
    }

    private void UpdateGatlingWeapon(float dt)
    {
        if (_isGatlingHolding && !_isGameOver)
        {
            _gatlingRotorSpeed = Mathf.Lerp(_gatlingRotorSpeed, 1400f, dt * 8f);
            _gatlingFireCooldown -= dt;
            if (_gatlingFireCooldown <= 0f)
            {
                _gatlingFireCooldown = 1f / Mathf.Max(1f, gatlingRoundsPerSecond);
                FireSingleGatlingShot();
            }
        }
        else if (!_isGatlingFiring)
        {
            _gatlingRotorSpeed = Mathf.Lerp(_gatlingRotorSpeed, 0f, dt * 3.5f);
        }

        if (gatlingRotorTransform != null && Mathf.Abs(_gatlingRotorSpeed) > 0.5f)
        {
            gatlingRotorTransform.Rotate(Vector3.forward, _gatlingRotorSpeed * dt, Space.Self);
        }

        _gatlingRecoilZ = Mathf.Lerp(_gatlingRecoilZ, 0f, dt * 12f);
        _missileRecoilZ = Mathf.Lerp(_missileRecoilZ, 0f, dt * 8f);
    }

    public void FireSingleGatlingShot()
    {
        if (_isGameOver) return;

        Vector3 muzzlePos;
        if (gatlingMuzzleTransform != null)
            muzzlePos = gatlingMuzzleTransform.position;
        else if (leftArmTransform != null)
            muzzlePos = leftArmTransform.position + leftArmTransform.forward * 4.5f;
        else
            muzzlePos = transform.position + transform.forward * 4f - transform.right * 3.5f + Vector3.up * 2f;

        Vector3 aimDir = GetGunnerAimDirection();
        Vector2 spread = Random.insideUnitCircle * 0.020f;
        aimDir = (aimDir + transform.right * spread.x + transform.up * spread.y).normalized;

        GatlingTracer.Spawn(muzzlePos, aimDir);
        ProceduralAudio.Instance?.PlayGatlingFire();
        WindshieldHUDCrosshair.Instance?.TriggerRecoil(0.08f);

        if (gatlingMuzzleLight != null)
        {
            StopCoroutine(nameof(MuzzleLightFlashRoutine));
            StartCoroutine(MuzzleLightFlashRoutine());
        }

        _gatlingRecoilZ = Mathf.Clamp(_gatlingRecoilZ + 0.35f, 0f, 1.2f);
        _shakeIntensity = Mathf.Max(_shakeIntensity, 0.12f);
    }

    private IEnumerator MuzzleLightFlashRoutine()
    {
        if (gatlingMuzzleLight != null) gatlingMuzzleLight.enabled = true;
        yield return new WaitForSeconds(0.04f);
        if (gatlingMuzzleLight != null) gatlingMuzzleLight.enabled = false;
    }

    public void TriggerWeaponAction(string buttonId, Vector3 origin)
    {
        if (_isGameOver) return;

        if (buttonId == "GATLING" || buttonId == "PUNCH")
        {
            StartCoroutine(GatlingBurstRoutine(10, 0.045f));
        }
        else if (buttonId == "MISSILES" || buttonId == "LASER")
        {
            if (_currentMissileCooldown <= 0f)
            {
                FireMissileSalvo();
                _currentMissileCooldown = missileCooldownDuration;
            }
        }
    }

    private IEnumerator GatlingBurstRoutine(int shots, float interval)
    {
        _isGatlingFiring = true;
        _gatlingRotorSpeed = 1600f;
        for (int i = 0; i < shots; i++)
        {
            FireSingleGatlingShot();
            yield return new WaitForSeconds(interval);
        }
        _isGatlingFiring = false;
    }

    private void FireMissileSalvo()
    {
        ProceduralAudio.Instance?.PlayMissileLaunch();
        _shakeIntensity = Mathf.Max(_shakeIntensity, 0.55f);
        _missileRecoilZ = 1.0f;
        WindshieldHUDCrosshair.Instance?.TriggerRecoil(0.24f);

        int tubeCount = (missileLaunchTubes != null && missileLaunchTubes.Length > 0) ? missileLaunchTubes.Length : 4;
        for (int i = 0; i < tubeCount; i++)
        {
            Vector3 spawnPos = transform.position + transform.forward * 3f + transform.right * 13.6f + Vector3.up * 26f;
            if (missileLaunchTubes != null && i < missileLaunchTubes.Length && missileLaunchTubes[i] != null)
            {
                spawnPos = missileLaunchTubes[i].position;
            }
            Vector3 aimDir = GetGunnerAimDirection();
            Vector2 spread = Random.insideUnitCircle * 0.06f;
            aimDir = (aimDir + transform.right * spread.x + transform.up * spread.y).normalized;

            StartCoroutine(DelayedMissileLaunch(i * 0.08f, spawnPos, aimDir));
        }
    }

    private IEnumerator DelayedMissileLaunch(float delay, Vector3 pos, Vector3 dir)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        MissileProjectile.Spawn(pos, Quaternion.LookRotation(dir));
    }

    private void UpdateGimbalsAndCockpitVisuals(float dt)
    {
        if (gatlingGimbalTransform != null)
        {
            Quaternion aimRot = Quaternion.Euler(-gunnerAimPitch, gunnerAimYaw, 0f);
            gatlingGimbalTransform.localRotation = Quaternion.Slerp(gatlingGimbalTransform.localRotation, _initialGatlingGimbalRot * aimRot, dt * 10f);
        }

        if (missilePodGimbalTransform != null)
        {
            Quaternion aimRot = Quaternion.Euler(-gunnerAimPitch, gunnerAimYaw, 0f);
            missilePodGimbalTransform.localRotation = Quaternion.Slerp(missilePodGimbalTransform.localRotation, _initialMissileGimbalRot * aimRot, dt * 10f);
        }
    }

    private void UpdateCounterweightVisuals(float dt)
    {
        if (cockpitTransform == null) return;

        Vector3 squashOffset = new Vector3(0f, -_landingCompressionOffset * 0.85f, 0f);

        if (_shakeIntensity > 0.01f)
        {
            float trauma = Mathf.Clamp01(_shakeIntensity);
            float shakeMag = trauma * trauma * 0.22f;
            float t = Time.time * 34f;
            float stabShakeX = Mathf.Sin(t * 1.7f) * shakeMag;
            float stabShakeY = Mathf.Cos(t * 2.3f) * shakeMag;
            cockpitTransform.localPosition = _initialCockpitLocalPos + squashOffset + new Vector3(stabShakeX, stabShakeY, 0f);
        }
        else
        {
            cockpitTransform.localPosition = _initialCockpitLocalPos + squashOffset;
        }
    }

    public void TriggerHitStop(float duration, float hitStopScale)
    {
        StopCoroutine(nameof(HitStopRoutine));
        StartCoroutine(HitStopRoutine(duration, hitStopScale));
    }

    private IEnumerator HitStopRoutine(float duration, float hitStopScale)
    {
        float prevTimeScale = Time.timeScale;
        Time.timeScale = hitStopScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = prevTimeScale;
    }

    public void TakeDamage(int damage)
    {
        if (_isGameOver) return;
        currentHp = Mathf.Max(0, currentHp - damage);
        ProceduralAudio.Instance?.PlayAlarm();
        _shakeIntensity = Mathf.Max(_shakeIntensity, 0.45f);

        if (currentHp <= 0)
        {
            _isGameOver = true;
            TransitionToState(LocomotionState.Fallen);
            MechaHUD.Instance?.ShowGameOver(_score);
        }
    }

    public void AddScore(int points)
    {
        _score += points;
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private static void NormalizeAngles(ref Vector3 euler)
    {
        euler.x = Mathf.DeltaAngle(0f, euler.x);
        euler.y = Mathf.DeltaAngle(0f, euler.y);
        euler.z = Mathf.DeltaAngle(0f, euler.z);
    }

    private void OnDrawGizmos()
    {
        if (!drawBalanceGizmos || _balanceModel == null) return;

        var bal = _balanceModel.CurrentState;

        // 1. Polígono de soporte
        int count = _balanceModel.PolygonVertexCount;
        if (count >= 3)
        {
            Gizmos.color = bal.isDynamicallyStable ? Color.green : Color.red;
            for (int i = 0; i < count; i++)
            {
                Vector3 pA = _balanceModel.GetPolygonVertexWorld(i);
                Vector3 pB = _balanceModel.GetPolygonVertexWorld((i + 1) % count);
                Gizmos.DrawLine(pA, pB);
            }
        }

        // 2. CoM (Azul) y XcoM (Amarillo/Rojo)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(bal.worldCoM, 0.8f);
        Gizmos.DrawLine(bal.worldCoM, bal.projectedCoM);

        Gizmos.color = bal.isDynamicallyStable ? Color.yellow : Color.magenta;
        Gizmos.DrawWireSphere(bal.extrapolatedCoM, 0.6f);
        Gizmos.DrawLine(bal.projectedCoM, bal.extrapolatedCoM);

        // 3. Ideal Capture Step Point (Cian)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(bal.idealCaptureStepPoint, 0.5f);
    }
}
