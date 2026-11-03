using System;
using UnityEngine;

/// <summary>
/// Controlador de pierna procedural e IK con detección de suelo acotada a la longitud física real de la pierna.
/// Elimina los rayos largos de 28m y soporta pasos reactivos de emergencia hacia el Capture Point (XcoM).
/// </summary>
public class ProceduralLeg : MonoBehaviour
{
    [Header("Configuración IK y Dimensiones")]
    [SerializeField] private TwoBoneIKSolver ikSolver = new TwoBoneIKSolver();
    [SerializeField] private float femurLength = 10.5f;
    [SerializeField] private float tibiaLength = 10.0f;
    [SerializeField] private float ankleToSoleOffset = 1.2f;

    [Header("Referencias")]
    [SerializeField] private Transform bodyRoot;
    [SerializeField] private Rigidbody bodyRigidbody;
    [SerializeField] private MechaPhysicsConfig physicsConfig;

    [Header("Parámetros de Marcha Normal")]
    [SerializeField] private float stepDistance = 4.5f;
    [SerializeField] private float turnStepAngle = 20f;
    [SerializeField] private float stepHeight = 1.8f;
    [SerializeField] private float stepDuration = 0.42f;
    [SerializeField] private float stepLeadTime = 0.32f;
    [SerializeField] private Vector3 localRestOffset = new Vector3(-8.4f, 0f, 0f);

#pragma warning disable 0414
    [Header("Escalones y Geometría de Pie")]
    [SerializeField] private float maxClimbableStepHeight = 4.5f;
    [SerializeField] private float stepAnticipationDistance = 5.5f;
    [SerializeField] private float footToeForwardOffset = 4.2f;
    [SerializeField] private float footHalfWidth = 2.6f;
    [SerializeField] private float footRadius = 1.3f;
#pragma warning restore 0414

    [Header("Curvas de Paso")]
    [SerializeField] private AnimationCurve stepHeightCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 3.5f),
        new Keyframe(0.45f, 1f, 0f, 0f),
        new Keyframe(1f, 0f, -3.0f, 0f)
    );
    [SerializeField] private AnimationCurve stepEaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Capas")]
    [SerializeField] private LayerMask groundMask;

    // Estado interno
    private bool _isStepping;
    private bool _isEmergencyStep;
    private bool _isGrounded;
    private bool _isSlidingOnSlope;
    private float _stepProgress = 1f;
    private float _currentStepDuration = 0.42f;
    private float _currentStepHeight = 1.8f;

    private Vector3 _currentFootPos;
    private Quaternion _currentFootRot;
    private Vector3 _startFootPos;
    private Quaternion _startFootRot;
    private Vector3 _targetFootPos;
    private Quaternion _targetFootRot;

    private Vector3 _groundContactPoint;
    private Vector3 _groundNormal = Vector3.up;
    private float _currentHipToGroundDistance = 21.2f;
    private float _currentSlopeAngle = 0f;

    private Vector3 _homeTargetPos;
    private Vector3 _homeGroundNormal = Vector3.up;
    private bool _homeTargetValid;

    // Buffer preasignado (0 GC Allocations por frame)
    private readonly RaycastHit[] _raycastHits = new RaycastHit[8];

    public event Action<Vector3, Vector3> OnStepTouchdown;
    public event Action<Vector3, Vector3, bool> OnStepTouchdownDetailed;

    // Propiedades públicas de lectura limpia
    public bool IsStepping => _isStepping;
    public bool IsEmergencyStep => _isEmergencyStep;
    public bool IsGrounded => _isGrounded;
    public bool IsSlidingOnSlope => _isSlidingOnSlope;
    public Vector3 CurrentFootPosition => _currentFootPos;
    public Vector3 CurrentSolePosition => _currentFootPos - _groundNormal * ankleToSoleOffset;
    public Quaternion CurrentFootRotation => _currentFootRot;
    public Vector3 GroundNormal => _groundNormal;
    public float CurrentHipToGroundDistance => _currentHipToGroundDistance;
    public float CurrentSlopeAngle => _currentSlopeAngle;
    public Vector3 HomeTargetPosition => _homeTargetPos;
    public Vector3 HipWorldPosition => ikSolver != null && ikSolver.hip != null ? ikSolver.hip.position : transform.position;
    public float FemurLength { get => femurLength; set { femurLength = value; if (ikSolver != null) ikSolver.UpperLength = value; } }
    public float TibiaLength { get => tibiaLength; set { tibiaLength = value; if (ikSolver != null) ikSolver.LowerLength = value; } }
    public float MaxPhysicalReach => physicsConfig != null ? physicsConfig.maxLegExtension : (femurLength + tibiaLength);
    public TwoBoneIKSolver IKSolver => ikSolver;
    public float AnkleToSoleOffset => ankleToSoleOffset;
    public float LegReach => femurLength + tibiaLength;

    public void SetLocalRestOffset(Vector3 offset) => localRestOffset = offset;

    private void Awake()
    {
        if (bodyRoot == null && transform.parent != null)
            bodyRoot = transform.parent;

        if (bodyRigidbody == null && bodyRoot != null)
            bodyRigidbody = bodyRoot.GetComponent<Rigidbody>();

        if (groundMask.value == 0)
            groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

        ikSolver.UpperLength = femurLength;
        ikSolver.LowerLength = tibiaLength;
        ikSolver.Initialize();

        float defaultRide = physicsConfig != null ? physicsConfig.nominalRideHeight : 20.5f;
        Vector3 initPos = HipWorldPosition + Vector3.down * defaultRide;
        _currentFootPos = ikSolver.ankle != null ? ikSolver.ankle.position : initPos;
        _currentFootRot = bodyRoot != null ? bodyRoot.rotation : transform.rotation;
        _startFootPos = _currentFootPos;
        _targetFootPos = _currentFootPos;
    }

    private void Start()
    {
        if (UpdateHomeTarget())
        {
            _currentFootPos = _homeTargetPos;
            _targetFootPos = _currentFootPos;
            _startFootPos = _currentFootPos;
            _groundNormal = _homeGroundNormal;
        }
        ikSolver.Solve(_currentFootPos, _currentFootRot);
    }

    public void SetConfig(MechaPhysicsConfig config)
    {
        physicsConfig = config;
    }

    /// <summary>
    /// Sondeo físico estricto ejecutado en FixedUpdate desde el controlador principal.
    /// NUNCA reporta suelo más allá de MaxPhysicalReach.
    /// Retorna si el terreno bajo la pierna ofrece soporte firme dentro del alcance.
    /// </summary>
    public bool ProbeGroundPhysics(float dt)
    {
        if (groundMask.value == 0)
            groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

        Vector3 hipPos = HipWorldPosition;
        float maxReach = MaxPhysicalReach;
        float probeRadius = physicsConfig != null ? physicsConfig.footProbeRadius : footRadius;

        Vector3 footXZ = new Vector3(_currentFootPos.x, hipPos.y, _currentFootPos.z);
        Vector3 horizOffset = footXZ - hipPos;
        horizOffset.y = 0f;
        if (horizOffset.sqrMagnitude > (maxReach * 0.65f) * (maxReach * 0.65f))
        {
            horizOffset = horizOffset.normalized * (maxReach * 0.65f);
        }

        Vector3 probeOrigin = new Vector3(hipPos.x + horizOffset.x, hipPos.y + 0.25f, hipPos.z + horizOffset.z);
        float maxCastDistance = maxReach + 0.25f; // Estricto: si el suelo está a más de maxReach, es aire

        int hitCount = Physics.SphereCastNonAlloc(
            probeOrigin,
            probeRadius,
            Vector3.down,
            _raycastHits,
            maxCastDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        float closestDist = float.MaxValue;
        RaycastHit bestHit = default;
        bool foundValidGround = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _raycastHits[i];
            if (hit.collider == null) continue;
            if (bodyRoot != null && hit.collider.transform.IsChildOf(bodyRoot)) continue;

            float verticalDistFromHip = hipPos.y - hit.point.y;
            if (verticalDistFromHip <= maxReach && hit.distance < closestDist)
            {
                closestDist = hit.distance;
                bestHit = hit;
                foundValidGround = true;
            }
        }

        if (!foundValidGround)
        {
            _isGrounded = false;
            _isSlidingOnSlope = false;
            _currentHipToGroundDistance = maxReach + 1f;
            return false;
        }

        _groundContactPoint = bestHit.point;
        _groundNormal = bestHit.normal;
        _currentHipToGroundDistance = Mathf.Max(0.2f, hipPos.y - bestHit.point.y);
        _currentSlopeAngle = Vector3.Angle(_groundNormal, Vector3.up);

        float maxStableSlope = physicsConfig != null ? physicsConfig.maxStableSlopeAngle : 32f;
        float criticalSlope = physicsConfig != null ? physicsConfig.criticalSlideSlopeAngle : 46f;

        // Gestión de deslizamiento físico del pie en pendientes pronunciadas
        if (_currentSlopeAngle > maxStableSlope)
        {
            _isSlidingOnSlope = true;
            Vector3 downSlopeDir = Vector3.ProjectOnPlane(Vector3.down, _groundNormal).normalized;
            float slideFactor = Mathf.InverseLerp(maxStableSlope, criticalSlope, _currentSlopeAngle);
            float slideSpeed = (physicsConfig != null ? physicsConfig.slopeSlideAcceleration : 7.5f) * slideFactor;
            _currentFootPos += downSlopeDir * (slideSpeed * dt);
        }
        else
        {
            _isSlidingOnSlope = false;
        }

        bool slopeWalkable = _currentSlopeAngle < criticalSlope;
        if (!_isStepping)
        {
            _isGrounded = slopeWalkable;
        }

        return slopeWalkable;
    }

    /// <summary>
    /// Actualización cinemática del pie e IK (llamada desde LateUpdate según el estado de la máquina de estados).
    /// </summary>
    public void UpdateKinematics(MechaController.LocomotionState state, float dt)
    {
        UpdateHomeTarget();

        switch (state)
        {
            case MechaController.LocomotionState.Grounded:
            case MechaController.LocomotionState.Unstable:
            case MechaController.LocomotionState.Landing:
                if (_isStepping)
                {
                    AdvanceStepTrajectory(dt);
                }
                else if (_isGrounded)
                {
                    // Fijar el pie exactamente sobre el punto de contacto real
                    _currentFootPos.y = _groundContactPoint.y + ankleToSoleOffset;
                    Vector3 fwd = Vector3.ProjectOnPlane(bodyRoot != null ? bodyRoot.forward : transform.forward, _groundNormal);
                    if (fwd.sqrMagnitude > 0.001f)
                    {
                        _currentFootRot = Quaternion.Slerp(_currentFootRot, Quaternion.LookRotation(fwd.normalized, _groundNormal), dt * 12f);
                    }
                }
                ikSolver.Solve(_currentFootPos, _currentFootRot);
                break;

            case MechaController.LocomotionState.Airborne:
            case MechaController.LocomotionState.Falling:
                _isStepping = false;
                _isGrounded = false;
                // En el aire: las piernas cuelgan con inercia pesada bajo la cadera (sin estirarse hacia el suelo)
                Vector3 vel = bodyRigidbody != null ? bodyRigidbody.linearVelocity : Vector3.zero;
                float vertLag = Mathf.Clamp(-vel.y * 0.03f, -0.35f, 0.45f);
                Vector3 localAirTarget = new Vector3(localRestOffset.x, -MaxPhysicalReach * 0.78f + vertLag, localRestOffset.z);
                Vector3 worldAirTarget = bodyRoot != null ? bodyRoot.TransformPoint(localAirTarget) : (HipWorldPosition + localAirTarget);

                _currentFootPos = Vector3.Lerp(_currentFootPos, worldAirTarget, dt * 8f);
                Quaternion airRot = (bodyRoot != null ? bodyRoot.rotation : transform.rotation) * Quaternion.Euler(15f, 0f, 0f);
                _currentFootRot = Quaternion.Slerp(_currentFootRot, airRot, dt * 8f);
                ikSolver.Solve(_currentFootPos, _currentFootRot);
                break;

            case MechaController.LocomotionState.Fallen:
            case MechaController.LocomotionState.GettingUp:
                _isStepping = false;
                ikSolver.Solve(_currentFootPos, _currentFootRot);
                break;
        }
    }

    /// <summary>
    /// Posiciona el objetivo IK directamente (usado durante el Active Ragdoll en el suelo y la secuencia de levantarse).
    /// </summary>
    public void SetDirectFootTarget(Vector3 worldPos, Quaternion worldRot, float lerpSpeed, float dt)
    {
        _currentFootPos = Vector3.Lerp(_currentFootPos, worldPos, dt * lerpSpeed);
        _currentFootRot = Quaternion.Slerp(_currentFootRot, worldRot, dt * lerpSpeed);
        ikSolver.Solve(_currentFootPos, _currentFootRot);
    }

    public bool UpdateHomeTarget()
    {
        if (bodyRoot == null) return false;
        if (groundMask.value == 0)
            groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

        Vector3 hipPos = HipWorldPosition;
        Vector3 restWorld = bodyRoot.TransformPoint(localRestOffset);
        Vector3 vel = bodyRigidbody != null ? bodyRigidbody.linearVelocity : Vector3.zero;
        Vector3 planarVel = new Vector3(vel.x, 0f, vel.z);
        Vector3 predictedPos = restWorld + planarVel * stepLeadTime;

        Vector3 probeStart = new Vector3(predictedPos.x, hipPos.y + 0.3f, predictedPos.z);
        float maxDist = MaxPhysicalReach + 0.4f;

        if (Physics.SphereCast(probeStart, footRadius, Vector3.down, out RaycastHit hit, maxDist, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.transform.IsChildOf(bodyRoot) && (hipPos.y - hit.point.y) <= MaxPhysicalReach)
            {
                _homeTargetPos = hit.point + hit.normal * ankleToSoleOffset;
                _homeGroundNormal = hit.normal;
                _homeTargetValid = true;
                return true;
            }
        }

        _homeTargetPos = new Vector3(predictedPos.x, hipPos.y - MaxPhysicalReach * 0.88f, predictedPos.z);
        _homeGroundNormal = Vector3.up;
        _homeTargetValid = false;
        return false;
    }

    public bool UpdateHomeTarget(out Vector3 groundPos, out Vector3 groundNorm)
    {
        bool valid = UpdateHomeTarget();
        groundPos = _homeTargetPos;
        groundNorm = _homeGroundNormal;
        return valid;
    }

    public bool WantsNormalStep(out float urgency)
    {
        urgency = 0f;
        if (_isStepping || !_homeTargetValid) return false;

        float dist = Vector3.Distance(_currentFootPos, _homeTargetPos);
        Vector3 bodyFwd = bodyRoot != null ? bodyRoot.forward : transform.forward;
        float angle = Vector3.Angle(_currentFootRot * Vector3.forward, bodyFwd);

        if (dist > stepDistance || angle > turnStepAngle)
        {
            urgency = Mathf.Max(dist / Mathf.Max(0.1f, stepDistance), angle / Mathf.Max(1f, turnStepAngle));
            return true;
        }

        return false;
    }

    public bool WantsStep(out float urgency) => WantsNormalStep(out urgency);

    /// <summary>
    /// Inicia un paso normal o un paso reactivo de emergencia (Capture Step) para recuperar el equilibrio.
    /// </summary>
    public bool TryTakeStep(Vector3 worldDestination, float customDuration = -1f, bool isEmergency = false)
    {
        if (_isStepping && !isEmergency) return false;
        if (groundMask.value == 0)
            groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

        Vector3 hipPos = HipWorldPosition;
        Vector3 horizDelta = new Vector3(worldDestination.x - hipPos.x, 0f, worldDestination.z - hipPos.z);
        float maxHorizReach = MaxPhysicalReach * 0.72f;
        if (horizDelta.magnitude > maxHorizReach)
        {
            horizDelta = horizDelta.normalized * maxHorizReach;
        }

        Vector3 probeOrigin = new Vector3(hipPos.x + horizDelta.x, hipPos.y + 0.35f, hipPos.z + horizDelta.z);
        if (!Physics.SphereCast(probeOrigin, footRadius, Vector3.down, out RaycastHit hit, MaxPhysicalReach + 0.4f, groundMask, QueryTriggerInteraction.Ignore))
        {
            // No hay suelo donde apoyar el paso (ej. precipicio)
            return false;
        }

        if (bodyRoot != null && hit.collider.transform.IsChildOf(bodyRoot)) return false;

        _startFootPos = _currentFootPos;
        _startFootRot = _currentFootRot;
        _targetFootPos = hit.point + hit.normal * ankleToSoleOffset;
        _groundNormal = hit.normal;

        Vector3 fwd = Vector3.ProjectOnPlane(bodyRoot != null ? bodyRoot.forward : transform.forward, hit.normal);
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
        _targetFootRot = Quaternion.LookRotation(fwd.normalized, hit.normal);

        _currentStepDuration = customDuration > 0f ? customDuration : stepDuration;
        _currentStepHeight = isEmergency ? stepHeight * 1.25f : stepHeight;
        _stepProgress = 0f;
        _isStepping = true;
        _isEmergencyStep = isEmergency;
        _isGrounded = false;
        return true;
    }

    public void TakeStep(Vector3 targetPos, Vector3 targetNorm, float duration)
    {
        TryTakeStep(targetPos, duration, false);
    }

    private void AdvanceStepTrajectory(float dt)
    {
        _stepProgress += dt / Mathf.Max(0.05f, _currentStepDuration);
        float t = Mathf.Clamp01(_stepProgress);
        float easeT = stepEaseCurve != null ? stepEaseCurve.Evaluate(t) : t;
        float arcY = (stepHeightCurve != null ? stepHeightCurve.Evaluate(t) : Mathf.Sin(t * Mathf.PI)) * _currentStepHeight;

        Vector3 pos = Vector3.Lerp(_startFootPos, _targetFootPos, easeT);
        pos.y += arcY;
        _currentFootPos = pos;
        _currentFootRot = Quaternion.Slerp(_startFootRot, _targetFootRot, easeT);

        if (t >= 1.0f)
        {
            _currentFootPos = _targetFootPos;
            _currentFootRot = _targetFootRot;
            _isStepping = false;
            _isGrounded = true;
            bool wasEmergency = _isEmergencyStep;
            _isEmergencyStep = false;
            OnStepTouchdown?.Invoke(_currentFootPos, _groundNormal);
            OnStepTouchdownDetailed?.Invoke(_currentFootPos, _groundNormal, wasEmergency);
        }
    }
}
