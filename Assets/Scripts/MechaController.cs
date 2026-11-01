using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador físico principal del mecha gigante MechaQuack.
/// Gestiona la locomoción con Rigidbody 3D, suspensión en dos patas independientes,
/// pérdida de equilibrio y vuelcos al salirse de puentes o desniveles, armas (Puño y Láser),
/// y recuperación hidráulica ("¡PATALETA!").
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MechaController : MonoBehaviour
{
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

    [Header("Físicas y Locomoción")]
    [SerializeField] private float movePower = 1200000f;
    [SerializeField] private float turnTorque = 900000f;
    [SerializeField] private float maxLinearVelocity = 22f;
    [SerializeField] private float suspensionStrength = 1800000f;
    [SerializeField] private float suspensionDamping = 220000f;
    [SerializeField] private float targetRideHeight = 21.2f;
    [SerializeField] private float legRayLength = 28.0f;
    [Tooltip("Aceleración gravitacional extra hacia abajo durante caída libre para evitar flotar como globo")]
    [SerializeField] private float extraFallGravity = 17.5f;
    [Tooltip("Alcance máximo vertical desde la cadera para considerar pie en suelo")]
    [SerializeField] private float maxGroundedReach = 28.0f;

    [Header("Partes Físicas y Visuales")]
    [SerializeField] private Transform cockpitTransform;
    [SerializeField] private Transform leftLegTransform;
    [SerializeField] private Transform rightLegTransform;
    [SerializeField] private Transform leftArmTransform;
    [SerializeField] private Transform rightArmTransform;

    [Header("Armas: Ametralladora (Gatling)")]
    [SerializeField] private Transform gatlingRotorTransform;
    [SerializeField] private Transform gatlingMuzzleTransform;
    [SerializeField] private Light gatlingMuzzleLight;
    [SerializeField] private float gatlingRoundsPerSecond = 12f;

    [Header("Armas: Lanzamisiles (Missile Pod)")]
    [SerializeField] private Transform missilePodTransform;
    [SerializeField] private Transform[] missileLaunchTubes;
    [SerializeField] private float missileCooldownDuration = 3.5f;

    [Header("Sistema de Puntería de Artillería (Gunner Aiming)")]
    [Tooltip("Ángulo de azimut horizontal de puntería (-48° a +48°) controlado desde la estación de artillería")]
    [SerializeField] private float gunnerAimYaw = 0f;
    [Tooltip("Ángulo de elevación vertical de puntería (-20° a +36°) controlado desde la estación de artillería")]
    [SerializeField] private float gunnerAimPitch = 0f;
    [SerializeField] private Transform gatlingGimbalTransform;
    [SerializeField] private Transform missilePodGimbalTransform;

    [Header("Chasis y Salud")]
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp = 100;

    [Header("Cooldowns y Tiempos")]
    [SerializeField] private float laserCooldownDuration = 3.5f;

    // Estado interno
    private Rigidbody rb;
    private float currentLaserCooldown = 0f;
    private int score = 0;
    private bool isGameOver = false;
    private bool isOverturned = false;
    private float overturnPromptTimer = 0f;
    private float recoveryGraceTimer = 0f;

    private float walkCycle = 0f;
    private float stepTimer = 0f;
    private float punchTimer = 0f;
    private float laserTimer = 0f;
    private float shakeIntensity = 0f;

    private bool isGatlingFiring = false;
    private bool isGatlingHolding = false;
    private float gatlingFireCooldown = 0f;
    private float gatlingRotorSpeed = 0f;
    private float gatlingRecoilZ = 0f;
    private float missileRecoilZ = 0f;

    private float inputMove = 0f;
    private float inputTurn = 0f;

    private bool leftGrounded = false;
    private bool rightGrounded = false;
    private bool isAirborne = false;
    private bool wasAirborne = false;
    private float airborneDuration = 0f;
    private float airborneGraceTimer = 0f;
    private float peakFallVelocity = 0f;
    private float lastVerticalVelocity = 0f;
    private float landingSquash = 0f;
    private float landingSquashVelocity = 0f;

    private Vector3 initialLeftArmPos;
    private Vector3 initialRightArmPos;
    private Vector3 initialCockpitLocalPos;
    private Quaternion initialLeftArmRot = Quaternion.identity;
    private Quaternion initialRightArmRot = Quaternion.identity;
    private Quaternion initialGatlingGimbalRot = Quaternion.identity;
    private Quaternion initialMissileGimbalRot = Quaternion.identity;

    private readonly List<CockpitFloorButton> buttons = new List<CockpitFloorButton>();
    private DuckController duckInCockpit;
    private readonly RaycastHit[] groundRayHits = new RaycastHit[16];

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public int Score => score;
    public bool IsGameOver => isGameOver;
    public bool IsOverturned => isOverturned;
    public bool LeftGrounded => leftGrounded;
    public bool RightGrounded => rightGrounded;
    public bool IsAirborne => isAirborne;
    public float LandingSquash => landingSquash;
    public float IsAirborneDuration => airborneDuration;
    public bool IsGatlingFiring => isGatlingFiring || punchTimer > 0f;
    public float LaserCooldown => currentLaserCooldown;
    public float MissileCooldown => currentLaserCooldown;
    public float ShakeIntensity => shakeIntensity;
    public float GunnerAimYaw => gunnerAimYaw;
    public float GunnerAimPitch => gunnerAimPitch;
    public void TriggerFootstepShake(float intensity) => shakeIntensity = Mathf.Max(shakeIntensity, intensity);
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

    public Vector3 GetGunnerAimTargetPoint(float range = 240f)
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

        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.mass = 44000f;
        rb.linearDamping = 1.2f;
        rb.angularDamping = 2.2f;
        rb.centerOfMass = new Vector3(0f, 6.4f, 0f); // Centro de masa elevado para inclinación física realista
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (missileCooldownDuration <= 0f) missileCooldownDuration = laserCooldownDuration;
        else laserCooldownDuration = missileCooldownDuration;

        if (cockpitTransform != null) initialCockpitLocalPos = cockpitTransform.localPosition;
        if (leftArmTransform != null)
        {
            initialLeftArmPos = leftArmTransform.localPosition;
            initialLeftArmRot = leftArmTransform.localRotation;
        }
        if (rightArmTransform != null)
        {
            initialRightArmPos = rightArmTransform.localPosition;
            initialRightArmRot = rightArmTransform.localRotation;
        }

        if (gatlingGimbalTransform == null) gatlingGimbalTransform = leftArmTransform;
        if (missilePodGimbalTransform == null) missilePodGimbalTransform = missilePodTransform;

        if (gatlingGimbalTransform != null) initialGatlingGimbalRot = gatlingGimbalTransform.localRotation;
        if (missilePodGimbalTransform != null) initialMissileGimbalRot = missilePodGimbalTransform.localRotation;

        duckInCockpit = FindFirstObjectByType<DuckController>();
        buttons.AddRange(GetComponentsInChildren<CockpitFloorButton>());
    }

    private void Update()
    {
        if (isGameOver) return;

        UpdateCooldowns();
        ReadButtonsInput();
        UpdateGatlingMachineGun();
        AnimateLegsAndArms();
        AnimateCockpitSway();
        CheckOverturnedState();
    }

    private void UpdateCooldowns()
    {
        if (currentLaserCooldown > 0f)
            currentLaserCooldown = Mathf.Max(0f, currentLaserCooldown - Time.deltaTime);

        if (punchTimer > 0f)
            punchTimer = Mathf.Max(0f, punchTimer - Time.deltaTime);

        if (laserTimer > 0f)
            laserTimer = Mathf.Max(0f, laserTimer - Time.deltaTime);

        if (shakeIntensity > 0f)
            shakeIntensity = Mathf.Max(0f, shakeIntensity - Time.deltaTime * 2.5f);

        if (landingSquash > 0.001f)
            landingSquash = Mathf.SmoothDamp(landingSquash, 0f, ref landingSquashVelocity, 0.28f);
    }

    /// <summary>
    /// Lee los botones pisados en la cabina para determinar dirección, giro y disparo continuo de ametralladora.
    /// </summary>
    private void ReadButtonsInput()
    {
        inputMove = 0f;
        inputTurn = 0f;
        isGatlingHolding = false;

        if (buttons.Count == 0)
        {
            buttons.AddRange(GetComponentsInChildren<CockpitFloorButton>());
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            var btn = buttons[i];
            if (btn == null || !btn.isPressed) continue;

            switch (btn.buttonId)
            {
                case "FWD": inputMove += 1.0f; break;
                case "BWD": inputMove -= 0.7f; break;
                case "LEFT": inputTurn -= 1.0f; break;
                case "RIGHT": inputTurn += 1.0f; break;
                case "GATLING":
                case "PUNCH":
                    if (btn.type == CockpitFloorButton.ButtonType.Hold)
                    {
                        isGatlingHolding = true;
                    }
                    break;
            }
        }
    }

    private void UpdateGatlingMachineGun()
    {
        if (isGatlingHolding && !isGameOver)
        {
            gatlingRotorSpeed = Mathf.Lerp(gatlingRotorSpeed, 1400f, Time.deltaTime * 8f);
            gatlingFireCooldown -= Time.deltaTime;
            if (gatlingFireCooldown <= 0f)
            {
                gatlingFireCooldown = 1f / Mathf.Max(1f, gatlingRoundsPerSecond);
                FireSingleGatlingShot();
            }
        }
        else if (!isGatlingFiring)
        {
            gatlingRotorSpeed = Mathf.Lerp(gatlingRotorSpeed, 0f, Time.deltaTime * 3.5f);
        }

        if (gatlingRotorTransform != null && Mathf.Abs(gatlingRotorSpeed) > 0.5f)
        {
            gatlingRotorTransform.Rotate(Vector3.forward, gatlingRotorSpeed * Time.deltaTime, Space.Self);
        }

        // Recuperación de retroceso dinámico
        gatlingRecoilZ = Mathf.Lerp(gatlingRecoilZ, 0f, Time.deltaTime * 12f);
        missileRecoilZ = Mathf.Lerp(missileRecoilZ, 0f, Time.deltaTime * 8f);
    }

    public void FireSingleGatlingShot()
    {
        if (isGameOver) return;

        Vector3 muzzlePos;
        if (gatlingMuzzleTransform != null)
        {
            muzzlePos = gatlingMuzzleTransform.position;
        }
        else if (leftArmTransform != null)
        {
            muzzlePos = leftArmTransform.position + leftArmTransform.forward * 4.5f;
        }
        else
        {
            muzzlePos = transform.position + transform.forward * 4f - transform.right * 3.5f + Vector3.up * 2f;
        }

        Vector3 aimDir = GetGunnerAimDirection();
        // Ligera dispersión balística de ametralladora pesada
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

        gatlingRecoilZ = Mathf.Clamp(gatlingRecoilZ + 0.35f, 0f, 1.2f);
        shakeIntensity = Mathf.Max(shakeIntensity, 0.12f);
    }

    private System.Collections.IEnumerator MuzzleLightFlashRoutine()
    {
        if (gatlingMuzzleLight != null) gatlingMuzzleLight.enabled = true;
        yield return new WaitForSeconds(0.04f);
        if (gatlingMuzzleLight != null) gatlingMuzzleLight.enabled = false;
    }

    private void FixedUpdate()
    {
        if (isGameOver) return;

        ApplyLocomotionAndSuspension();
    }

    /// <summary>
    /// Simulación de físicas: Proyección de patas independientes, suspensión,
    /// desequilibrio si una pata queda en el aire, y fuerzas de locomoción.
    /// </summary>
    private void ApplyLocomotionAndSuspension()
    {
        if (isOverturned) return;

        int groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

        // Origen de los rayos de soporte (raíz de cadera dinámica según posición de las piernas)
        Vector3 leftLegRoot = leftLegTransform != null ? leftLegTransform.position : transform.TransformPoint(new Vector3(-8.4f, 21.2f, 0f));
        Vector3 rightLegRoot = rightLegTransform != null ? rightLegTransform.position : transform.TransformPoint(new Vector3(8.4f, 21.2f, 0f));

        lastVerticalVelocity = rb.linearVelocity.y;

        bool hitGroundL = CheckFootGrounded(leftLegRoot, out RaycastHit hitL, groundMask);
        bool hitGroundR = CheckFootGrounded(rightLegRoot, out RaycastHit hitR, groundMask);

        var pLegL = leftLegTransform != null ? leftLegTransform.GetComponent<ProceduralLeg>() : null;
        var pLegR = rightLegTransform != null ? rightLegTransform.GetComponent<ProceduralLeg>() : null;

        // Calcular cota efectiva y estado grounded para cada pierna
        float distL = hitGroundL ? hitL.distance : float.MaxValue;
        bool footPlantL = (pLegL != null && pLegL.IsGrounded);
        if (footPlantL)
        {
            float footDist = leftLegRoot.y - (pLegL.CurrentFootPosition.y - pLegL.AnkleToSoleOffset);
            if (footDist > 0f && (footDist < distL || !hitGroundL)) distL = footDist;
        }
        leftGrounded = (hitGroundL && hitL.distance <= legRayLength) || (footPlantL && distL <= legRayLength);
        float effectiveDistL = Mathf.Min(distL, legRayLength);

        float distR = hitGroundR ? hitR.distance : float.MaxValue;
        bool footPlantR = (pLegR != null && pLegR.IsGrounded);
        if (footPlantR)
        {
            float footDist = rightLegRoot.y - (pLegR.CurrentFootPosition.y - pLegR.AnkleToSoleOffset);
            if (footDist > 0f && (footDist < distR || !hitGroundR)) distR = footDist;
        }
        rightGrounded = (hitGroundR && hitR.distance <= legRayLength) || (footPlantR && distR <= legRayLength);
        float effectiveDistR = Mathf.Min(distR, legRayLength);

        // Determinación robusta de caída libre (Airborne):
        // Solo entra en estado aéreo si ninguna pata ni cadera tiene contacto con el suelo
        // y lleva velocidad de caída o transcurre un breve periodo de gracia (evita falsos positivos al caminar).
        bool noGroundContact = (!leftGrounded && !rightGrounded) && (!footPlantL && !footPlantR);
        if (noGroundContact)
        {
            airborneGraceTimer += Time.fixedDeltaTime;
        }
        else
        {
            airborneGraceTimer = 0f;
        }

        isAirborne = noGroundContact && (airborneGraceTimer > 0.08f || rb.linearVelocity.y < -2.2f);

        // --- GESTIÓN DE CAÍDA LIBRE PESADA Y ATERRIZAJE (Robot Torpe) ---
        if (isAirborne)
        {
            wasAirborne = true;
            airborneDuration += Time.fixedDeltaTime;
            float currentFallSpeed = -rb.linearVelocity.y;
            if (currentFallSpeed > peakFallVelocity) peakFallVelocity = currentFallSpeed;

            // En el aire: casi cero amortiguación aerodinámica para que caiga rápido y con peso (no flote)
            rb.linearDamping = 0.05f;

            // Gravedad extra de máquina colosal pesada de 44 toneladas
            rb.AddForce(Vector3.down * (rb.mass * extraFallGravity), ForceMode.Force);

            // Oscilación y balanceo torpe en el aire
            if (airborneDuration > 0.12f)
            {
                float airFlailTorque = Mathf.Sin(airborneDuration * 9.5f) * 32000f;
                rb.AddTorque(transform.forward * airFlailTorque, ForceMode.Force);
            }
        }
        else
        {
            rb.linearDamping = 1.0f;

            if (wasAirborne)
            {
                wasAirborne = false;
                float impactSpeed = Mathf.Max(peakFallVelocity, Mathf.Abs(lastVerticalVelocity));
                peakFallVelocity = 0f;
                if (airborneDuration > 0.18f && impactSpeed > 5.5f)
                {
                    HandleLandingImpact(impactSpeed, airborneDuration);
                }
                airborneDuration = 0f;
            }
        }

        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);

        // Equilibrio gravitacional exacto por pata (peso = 44000kg * 9.81 / 2 = 215820N)
        float baseEquilibriumForce = (rb.mass * 9.81f) * 0.5f;

        // 1. APLICAR SUSPENSIÓN CALIBRADA (Mantiene el mecha a la cota exacta para que los pies pisen el suelo)
        if (leftGrounded && effectiveDistL < legRayLength)
        {
            float compression = targetRideHeight - effectiveDistL;
            float vertVel = Vector3.Dot(rb.GetPointVelocity(leftLegRoot), transform.up);
            float springForce = baseEquilibriumForce + (compression * suspensionStrength) - (vertVel * suspensionDamping);
            rb.AddForceAtPosition(transform.up * Mathf.Max(0f, springForce), leftLegRoot, ForceMode.Force);
        }

        if (rightGrounded && effectiveDistR < legRayLength)
        {
            float compression = targetRideHeight - effectiveDistR;
            float vertVel = Vector3.Dot(rb.GetPointVelocity(rightLegRoot), transform.up);
            float springForce = baseEquilibriumForce + (compression * suspensionStrength) - (vertVel * suspensionDamping);
            rb.AddForceAtPosition(transform.up * Mathf.Max(0f, springForce), rightLegRoot, ForceMode.Force);
        }

        // 2. FÍSICAS DE DESEQUILIBRIO (Solo si una pata está en el abismo/vacío, no durante pasos de caminata)
        bool legSteppingL = pLegL != null && pLegL.IsStepping;
        bool legSteppingR = pLegR != null && pLegR.IsStepping;

        if (leftGrounded && !rightGrounded && !legSteppingR && tiltAngle < 75f)
        {
            // Pata derecha en el vacío: el mecha pierde apoyo y el peso lo hace inclinarse hacia la derecha
            rb.AddTorque(transform.forward * 45000f, ForceMode.Force);
            rb.AddForce(transform.right * 22000f, ForceMode.Force);
        }
        else if (!leftGrounded && rightGrounded && !legSteppingL && tiltAngle < 75f)
        {
            // Pata izquierda en el vacío: el mecha se desestabiliza hacia la izquierda
            rb.AddTorque(-transform.forward * 45000f, ForceMode.Force);
            rb.AddForce(-transform.right * 22000f, ForceMode.Force);
        }

        // 3. ESTABILIZACIÓN VERTICAL (Solo cuando ambas patas están firmemente apoyadas y no está volcado)
        if (leftGrounded && rightGrounded && tiltAngle < 38f)
        {
            Vector3 rotAxis = Vector3.Cross(transform.up, Vector3.up);
            rb.AddTorque(rotAxis * (tiltAngle * 3500f) - rb.angularVelocity * 1100f, ForceMode.Force);
        }

        // 4. FUERZAS DE LOCOMOCIÓN (Solo si al menos una pata toca suelo y no está totalmente volcado)
        if ((leftGrounded || rightGrounded) && tiltAngle < 55f)
        {
            if (Mathf.Abs(inputMove) > 0.01f)
            {
                Vector3 moveForce = transform.forward * (inputMove * movePower);
                rb.AddForce(moveForce, ForceMode.Force);
            }

            if (Mathf.Abs(inputTurn) > 0.01f)
            {
                Vector3 turn = Vector3.up * (inputTurn * turnTorque);
                rb.AddTorque(turn, ForceMode.Force);
            }

            // Limitar velocidad máxima en suelo para control preciso
            Vector3 horizVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (horizVel.magnitude > maxLinearVelocity)
            {
                Vector3 clamped = horizVel.normalized * maxLinearVelocity;
                rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.z);
            }
        }
    }

    private bool CheckFootGrounded(Vector3 probeOrigin, out RaycastHit groundHit, int mask)
    {
        groundHit = default;
        int hitCount = Physics.RaycastNonAlloc(probeOrigin, -transform.up, groundRayHits, maxGroundedReach, mask, QueryTriggerInteraction.Ignore);
        float closestDist = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            var hit = groundRayHits[i];
            if (hit.collider == null) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;
            // Solo considerar suelo si está dentro del alcance real de la pierna
            if (hit.distance < closestDist && hit.distance <= maxGroundedReach)
            {
                closestDist = hit.distance;
                groundHit = hit;
                found = true;
            }
        }
        return found;
    }

    /// <summary>
    /// Gestiona el impacto físico, squash de suspensión, sacudida de pantalla,
    /// nubes de polvo y reacción cómica del pato al aterrizar tras una caída.
    /// </summary>
    private void HandleLandingImpact(float impactSpeed, float airTime)
    {
        if (impactSpeed < 3.0f && airTime < 0.2f) return;

        Debug.Log($"[GAME] ¡ATERRIZAJE MECHA! Velocidad vertical: {impactSpeed:F1} m/s, Tiempo en aire: {airTime:F2}s");

        // 1. Squash mecánico de suspensión (las rodillas ceden y el chasis baja)
        landingSquash = Mathf.Clamp(impactSpeed * 0.14f, 0.4f, 2.8f);

        // 2. Shake de pantalla basado en trauma cuadrático según game-feel
        float trauma = Mathf.Clamp01(impactSpeed / 20.0f);
        shakeIntensity = Mathf.Max(shakeIntensity, trauma * 1.35f);

        // 3. Audio de impacto contundente
        if (impactSpeed > 9.0f)
        {
            ProceduralAudio.Instance?.PlayCrashLand();
            ProceduralAudio.Instance?.PlayQuack();
        }
        else
        {
            ProceduralAudio.Instance?.PlayMechaStep();
        }

        // 4. Hit-Stop en impactos masivos (congelación de fotogramas breve)
        if (impactSpeed > 14.0f)
        {
            TriggerHitStop(0.065f, 0.04f);
        }

        // 5. Partículas / escombros voxel en los pies
        Vector3 leftFootPos = (leftLegTransform != null) ? leftLegTransform.position + Vector3.down * 19.5f : transform.position - transform.right * 8.4f;
        Vector3 rightFootPos = (rightLegTransform != null) ? rightLegTransform.position + Vector3.down * 19.5f : transform.position + transform.right * 8.4f;

        VoxelExplosion.Spawn(leftFootPos, new Color(0.72f, 0.68f, 0.58f), Mathf.RoundToInt(Mathf.Clamp(impactSpeed * 2.2f, 15f, 40f)), 0.6f, 18f);
        VoxelExplosion.Spawn(rightFootPos, new Color(0.72f, 0.68f, 0.58f), Mathf.RoundToInt(Mathf.Clamp(impactSpeed * 2.2f, 15f, 40f)), 0.6f, 18f);

        // 6. Inercia del pato dentro de la cabina (sale despedido hacia arriba)
        if (duckInCockpit != null)
        {
            float duckLaunchY = Mathf.Clamp(impactSpeed * 0.75f, 4.5f, 18f);
            Vector3 duckJolt = new Vector3(Random.Range(-4f, 4f), duckLaunchY, Random.Range(-4f, 4f));
            duckInCockpit.ApplyExternalNudge(duckJolt);
        }

        // 7. Textos flotantes cómicos según la magnitud de la caída
        Vector3 textPos = transform.position + Vector3.up * 7f;
        if (impactSpeed > 18.0f)
        {
            FloatingText.Spawn(textPos, "💥 ¡¡CATAPLUM!! ¡ATERRIZAJE FORZOSO! 🦆", Color.red, 0.55f);
        }
        else if (impactSpeed > 9.0f)
        {
            FloatingText.Spawn(textPos, "💥 ¡CHOFFF! ¡44 TONELADAS!", new Color(1f, 0.65f, 0.1f), 0.45f);
        }
        else if (impactSpeed > 4.5f)
        {
            FloatingText.Spawn(textPos, "¡BOING! (PIES DE PLOMO)", Color.yellow, 0.35f);
        }

        // 8. Tropezón / riesgo de vuelco si el aterrizaje es muy inclinado desde gran altura
        float tilt = Vector3.Angle(transform.up, Vector3.up);
        if (impactSpeed > 24.0f && tilt > 24.0f)
        {
            rb.AddTorque(transform.forward * 45000f + transform.right * 70000f, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Comprueba si el Mecha ha volcado y gestiona avisos en pantalla.
    /// </summary>
    private void CheckOverturnedState()
    {
        if (recoveryGraceTimer > 0f)
        {
            recoveryGraceTimer -= Time.deltaTime;
            isOverturned = false;
            return;
        }

        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
        bool wasOverturned = isOverturned;
        isOverturned = tiltAngle > 60f;

        if (isOverturned)
        {
            if (!wasOverturned)
            {
                Debug.Log($"[GAME] ¡MECHA VOLCADO! Inclinación: {tiltAngle:F1}°");
            }

            overturnPromptTimer -= Time.deltaTime;
            if (overturnPromptTimer <= 0f)
            {
                overturnPromptTimer = 2.8f;
                FloatingText.Spawn(transform.position + Vector3.up * 6f, "¡MECHA VOLCADO! PISOTÓN (E) PARA ENDEREZAR", Color.yellow, 0.45f);
                ProceduralAudio.Instance?.PlayQuack();
            }
        }
        else if (wasOverturned)
        {
            overturnPromptTimer = 0f;
            Debug.Log("[GAME] Mecha equilibrado de nuevo");
        }
    }

    /// <summary>
    /// Maniobra cómica de emergencia ("¡PATALETA!"): Salto hidráulico que voltea
    /// el Mecha de vuelta sobre sus pies cuando está tumbado en el suelo.
    /// </summary>
    public void RecoverFromTumble()
    {
        if (!isOverturned) return;

        ProceduralAudio.Instance?.PlayHydraulicRecover();
        FloatingText.Spawn(transform.position + Vector3.up * 7f, "¡PATALETA HIDRÁULICA!", Color.yellow, 0.45f);
        Debug.Log("[GAME] ¡PATALETA HIDRÁULICA ejecutada! Mecha reincorporado");

        // Impulso vertical y reseteo de velocidad angular
        rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.2f, 28f, rb.linearVelocity.z * 0.2f);
        rb.angularVelocity = Vector3.zero;

        // Orientar erguido en el aire tanto en transform como en Rigidbody
        Quaternion uprightRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        transform.rotation = uprightRot;
        rb.rotation = uprightRot;

        isOverturned = false;
        recoveryGraceTimer = 0.65f;
        overturnPromptTimer = 0f;
    }

    private void AnimateLegsAndArms()
    {
        bool hasProceduralLegs = (leftLegTransform != null && leftLegTransform.GetComponent<ProceduralLeg>() != null);
        if (!hasProceduralLegs)
        {
            bool isWalking = (Mathf.Abs(inputMove) > 0.01f || Mathf.Abs(inputTurn) > 0.01f) && (leftGrounded || rightGrounded) && !isOverturned;

            if (isWalking)
            {
                walkCycle += Time.deltaTime * 7.5f;
                stepTimer += Time.deltaTime;

                if (stepTimer >= 0.42f)
                {
                    stepTimer = 0f;
                    ProceduralAudio.Instance?.PlayMechaStep();

                    if (duckInCockpit != null && duckInCockpit.IsGrounded)
                    {
                        duckInCockpit.ApplyExternalNudge(new Vector3(Mathf.Sin(walkCycle) * 1.6f, 0f, 0f));
                    }
                }

                float swing = Mathf.Sin(walkCycle) * 26f;
                if (leftLegTransform != null) leftLegTransform.localRotation = Quaternion.Euler(swing, 0f, 0f);
                if (rightLegTransform != null) rightLegTransform.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            }
            else
            {
                if (leftLegTransform != null) leftLegTransform.localRotation = Quaternion.Slerp(leftLegTransform.localRotation, Quaternion.identity, Time.deltaTime * 8f);
                if (rightLegTransform != null) rightLegTransform.localRotation = Quaternion.Slerp(rightLegTransform.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            }
        }

        AnimateArms();
    }

    private void AnimateCockpitSway()
    {
        if (cockpitTransform == null) return;

        Vector3 squashOffset = new Vector3(0f, -landingSquash * 0.85f, 0f);

        if (GetComponent<MechBodyStabilizer>() != null)
        {
            if (shakeIntensity > 0.01f)
            {
                // Vibración armónica basada en trauma cuadrático (game-feel suave y contundente, sin ruido blanco estático)
                float trauma = Mathf.Clamp01(shakeIntensity);
                float shakeMag = trauma * trauma * 0.22f;
                float t = Time.time * 34f;
                float stabShakeX = Mathf.Sin(t * 1.7f) * shakeMag;
                float stabShakeY = Mathf.Cos(t * 2.3f) * shakeMag;
                cockpitTransform.localPosition = initialCockpitLocalPos + squashOffset + new Vector3(stabShakeX, stabShakeY, 0f);
            }
            else
            {
                cockpitTransform.localPosition = initialCockpitLocalPos + squashOffset;
            }
            return;
        }

        float bobY = Mathf.Abs(Mathf.Sin(walkCycle)) * 0.18f;
        float traumaLeg = Mathf.Clamp01(shakeIntensity);
        float shakeMagLeg = traumaLeg * traumaLeg * 0.32f;
        float tLeg = Time.time * 34f;
        float shakeX = Mathf.Sin(tLeg * 1.7f) * shakeMagLeg;
        float shakeY = Mathf.Cos(tLeg * 2.3f) * shakeMagLeg;

        cockpitTransform.localPosition = initialCockpitLocalPos + squashOffset + new Vector3(shakeX, bobY + shakeY, 0f);

        float tiltZ = Mathf.Sin(walkCycle) * 1.8f;
        cockpitTransform.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
    }

    private void AnimateArms()
    {
        Quaternion aimRotOffset = Quaternion.Euler(-gunnerAimPitch, gunnerAimYaw, 0f);

        // Si está en caída libre, los brazos bracean/aletean cómicamente para mantener el equilibrio
        if (isAirborne)
        {
            float airArmFlail = Mathf.Sin(Time.time * 9.5f) * 16f;
            aimRotOffset *= Quaternion.Euler(airArmFlail, 0f, airArmFlail * 0.4f);
        }

        // Brazo izquierdo (Ametralladora Gatling)
        if (leftArmTransform != null)
        {
            leftArmTransform.localPosition = initialLeftArmPos - new Vector3(0f, 0f, gatlingRecoilZ);
            if (gatlingGimbalTransform == leftArmTransform)
            {
                leftArmTransform.localRotation = Quaternion.Slerp(leftArmTransform.localRotation, initialLeftArmRot * aimRotOffset, Time.deltaTime * 12f);
            }
        }

        if (gatlingGimbalTransform != null && gatlingGimbalTransform != leftArmTransform)
        {
            gatlingGimbalTransform.localRotation = Quaternion.Slerp(gatlingGimbalTransform.localRotation, initialGatlingGimbalRot * aimRotOffset, Time.deltaTime * 12f);
        }

        // Brazo derecho (Pod Lanzamisiles)
        if (rightArmTransform != null)
        {
            rightArmTransform.localPosition = initialRightArmPos - new Vector3(0f, 0f, missileRecoilZ);
        }

        if (missilePodGimbalTransform != null)
        {
            missilePodGimbalTransform.localRotation = Quaternion.Slerp(missilePodGimbalTransform.localRotation, initialMissileGimbalRot * aimRotOffset, Time.deltaTime * 12f);
        }
    }

    public void TriggerWeaponAction(string weaponId, Vector3 buttonWorldPos)
    {
        if (isGameOver) return;

        if (weaponId == "GATLING" || weaponId == "PUNCH")
        {
            StopCoroutine(nameof(GatlingBurstRoutine));
            StartCoroutine(GatlingBurstRoutine(buttonWorldPos));
        }
        else if (weaponId == "MISSILES" || weaponId == "LASER")
        {
            if (currentLaserCooldown > 0f)
            {
                FloatingText.Spawn(buttonWorldPos + Vector3.up * 1.5f,
                    $"⏳ RECARGANDO MISILES ({currentLaserCooldown:F1}s)", new Color(0.3f, 0.8f, 1f), 0.3f);
                return;
            }

            currentLaserCooldown = missileCooldownDuration;
            laserTimer = 0.45f;
            StopCoroutine(nameof(MissileSalvoRoutine));
            StartCoroutine(MissileSalvoRoutine(buttonWorldPos));
        }
    }

    private System.Collections.IEnumerator GatlingBurstRoutine(Vector3 buttonPos)
    {
        isGatlingFiring = true;
        punchTimer = 0.5f;
        gatlingRotorSpeed = 1600f;
        shakeIntensity = 0.35f;

        FloatingText.Spawn(buttonPos + Vector3.up * 1.5f, "💥 ¡RÁFAGA GATLING!", new Color(1f, 0.65f, 0.2f), 0.35f);

        int burstRounds = 10;
        float delay = 0.045f;

        for (int i = 0; i < burstRounds; i++)
        {
            FireSingleGatlingShot();
            yield return new WaitForSeconds(delay);
        }

        isGatlingFiring = false;
    }

    private System.Collections.IEnumerator MissileSalvoRoutine(Vector3 buttonPos)
    {
        FloatingText.Spawn(buttonPos + Vector3.up * 1.5f, "🚀 ¡SALVA DE MISILES!", new Color(0.2f, 0.9f, 1f), 0.35f);

        Transform lockedTarget = WindshieldHUDCrosshair.Instance != null ? WindshieldHUDCrosshair.Instance.CurrentLockedTarget : null;
        Vector3 aimDir = GetGunnerAimDirection();
        Vector3 aimTargetPoint = (lockedTarget != null)
            ? (lockedTarget.position + Vector3.up * 1.5f)
            : GetGunnerAimTargetPoint(280f);

        // Abanico táctico de dispersión para las 4 toberas de la salva
        Vector2[] spreads = new Vector2[]
        {
            new Vector2(-0.045f,  0.035f),
            new Vector2( 0.045f,  0.035f),
            new Vector2(-0.045f, -0.030f),
            new Vector2( 0.045f, -0.030f)
        };

        int numMissiles = (missileLaunchTubes != null && missileLaunchTubes.Length > 0) ? missileLaunchTubes.Length : 4;
        for (int i = 0; i < numMissiles; i++)
        {
            Vector2 spread = spreads[i % spreads.Length];
            Vector3 launchDir = (aimDir + transform.right * spread.x + transform.up * spread.y).normalized;
            Quaternion spawnRot = Quaternion.LookRotation(launchDir, transform.up);

            Vector3 spawnPos;
            if (missileLaunchTubes != null && i < missileLaunchTubes.Length && missileLaunchTubes[i] != null)
            {
                spawnPos = missileLaunchTubes[i].position + launchDir * 1.8f;
            }
            else if (missilePodTransform != null)
            {
                Vector3 offset = new Vector3((i % 2 == 0 ? -0.8f : 0.8f), (i < 2 ? 0.8f : -0.8f), 3.2f);
                spawnPos = missilePodTransform.TransformPoint(offset);
            }
            else if (rightArmTransform != null)
            {
                Vector3 offset = new Vector3((i % 2 == 0 ? -0.5f : 0.5f), (i < 2 ? 0.5f : -0.5f), 3.2f);
                spawnPos = rightArmTransform.TransformPoint(offset);
            }
            else
            {
                spawnPos = transform.position + transform.forward * 4f + transform.right * 4f + Vector3.up * 3f;
            }

            MissileProjectile.Spawn(spawnPos, spawnRot, lockedTarget, aimTargetPoint);
            ProceduralAudio.Instance?.PlayMissileLaunch();
            WindshieldHUDCrosshair.Instance?.TriggerRecoil(0.25f);

            missileRecoilZ = Mathf.Clamp(missileRecoilZ + 0.45f, 0f, 1.5f);
            shakeIntensity = Mathf.Max(shakeIntensity, 0.35f);

            yield return new WaitForSeconds(0.085f);
        }
    }

    /// <summary>
    /// Microcongelación de fotogramas (Hit-Stop) según la técnica de game-feel
    /// para maximizar el impacto físico visceral de las armas pesadas.
    /// </summary>
    public void TriggerHitStop(float duration = 0.045f, float slow = 0.05f)
    {
        StopCoroutine(nameof(HitStopRoutine));
        StartCoroutine(HitStopRoutine(duration, slow));
    }

    private System.Collections.IEnumerator HitStopRoutine(float duration, float slow)
    {
        Time.timeScale = slow;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    public void AddScore(int points)
    {
        score += points;
    }

    public void TakeDamage(int damage)
    {
        if (isGameOver) return;

        currentHp = Mathf.Max(0, currentHp - damage);
        shakeIntensity = 0.8f;
        Debug.Log($"[GAME] Mecha recibió {damage} de daño. HP: {currentHp}/{maxHp}");

        if (duckInCockpit != null)
        {
            Vector3 randomKnock = new Vector3(Random.Range(-10f, 10f), 6f, Random.Range(-10f, 10f));
            duckInCockpit.ApplyExternalNudge(randomKnock);
        }

        MechaHUD.Instance?.TriggerDamageFlash();

        if (currentHp <= 0)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        shakeIntensity = 1.2f;
        ProceduralAudio.Instance?.PlayExplosion();
        Debug.Log($"[GAME] ¡MECHA DESTRUIDO! Score final: {score}");

        FloatingText.Spawn(transform.position + Vector3.up * 8f, "¡MECHA DESTRUIDO!", Color.red, 0.5f);
        MechaHUD.Instance?.ShowGameOver(score);
    }

    public void RestartGame()
    {
        isGameOver = false;
        isOverturned = false;
        currentHp = maxHp;
        score = 0;
        currentLaserCooldown = 0f;
        punchTimer = 0f;
        laserTimer = 0f;
        isGatlingFiring = false;
        isGatlingHolding = false;
        gatlingRotorSpeed = 0f;
        gatlingRecoilZ = 0f;
        missileRecoilZ = 0f;
        shakeIntensity = 0f;
        walkCycle = 0f;
        stepTimer = 0f;
        isAirborne = false;
        wasAirborne = false;
        airborneDuration = 0f;
        airborneGraceTimer = 0f;
        peakFallVelocity = 0f;
        lastVerticalVelocity = 0f;
        landingSquash = 0f;
        landingSquashVelocity = 0f;
        Debug.Log("[GAME] Partida reiniciada. Mecha restaurado en plataforma inicial");

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = new Vector3(0f, 1.2f, 0f);
        transform.rotation = Quaternion.identity;

        if (cockpitTransform != null) cockpitTransform.localPosition = initialCockpitLocalPos;
        if (leftArmTransform != null) leftArmTransform.localPosition = initialLeftArmPos;
        if (rightArmTransform != null) rightArmTransform.localPosition = initialRightArmPos;
        if (leftLegTransform != null) leftLegTransform.localRotation = Quaternion.identity;
        if (rightLegTransform != null) rightLegTransform.localRotation = Quaternion.identity;

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null) buttons[i].ResetButton();
        }

        EnemySpawner.Instance?.ClearAllEnemies();

        var floatingTexts = Object.FindObjectsByType<FloatingText>(FindObjectsSortMode.None);
        for (int i = 0; i < floatingTexts.Length; i++)
        {
            if (floatingTexts[i] != null) Destroy(floatingTexts[i].gameObject);
        }

        if (duckInCockpit != null)
        {
            duckInCockpit.ResetPosition(new Vector3(0f, 0.6f, 0.35f));
        }

        CockpitCameraController.Instance?.ResetCamera();
        MechaHUD.Instance?.HideGameOver();
    }
}
