using System;
using UnityEngine;

/// <summary>
/// Gestiona una pata procedural de Mecha: detección de terreno con Raycast NonAlloc,
/// cálculo de Home Target predictivo según velocidad, anticipación y superación de escalones/rocas
/// (Step Climbing sin tropezar) y resolución de IK analítico.
/// </summary>
public class ProceduralLeg : MonoBehaviour
{
    [Header("Configuración IK")]
    [SerializeField] private TwoBoneIKSolver ikSolver = new TwoBoneIKSolver();
    [SerializeField] private float ankleToSoleOffset = 0.45f;

    [Header("Referencias de Mecha")]
    [SerializeField] private Transform bodyRoot;
    [SerializeField] private Rigidbody bodyRigidbody;

    [Header("Parámetros de Paso")]
    [Tooltip("Distancia mínima desde el Home Target para activar un nuevo paso")]
    [SerializeField] private float stepDistance = 2.0f;
    [Tooltip("Ángulo de giro mínimo del cuerpo en grados para activar un paso")]
    [SerializeField] private float turnStepAngle = 22f;
    [Tooltip("Altura máxima del arco del pie en el aire en suelo plano")]
    [SerializeField] private float stepHeight = 0.65f;
    [Tooltip("Duración base del paso en segundos")]
    [SerializeField] private float stepDuration = 0.38f;
    [Tooltip("Factor de predicción hacia adelante según la velocidad del mecha")]
    [SerializeField] private float stepLeadTime = 0.32f;
    [Tooltip("Ángulo máximo de pendiente en el que el pie puede apoyarse")]
    [SerializeField] private float maxSlopeAngle = 48f;
    [Tooltip("Radio de SphereCast para estabilidad de contacto")]
    [SerializeField] private float footRadius = 0.4f;

    [Header("Escalones y Obstáculos (Step Climbing)")]
    [Tooltip("Distancia hacia adelante para anticipar escalones o bordillos antes de chocar")]
    [SerializeField] private float stepAnticipationDistance = 1.8f;
    [Tooltip("Altura máxima de escalón que el mecha puede subir")]
    [SerializeField] private float maxClimbableStepHeight = 1.6f;
    [Tooltip("Altura mínima para considerar que hay un escalón a subir")]
    [SerializeField] private float minStepObstacleHeight = 0.12f;
    [Tooltip("Margen de elevación vertical adicional al superar un escalón")]
    [SerializeField] private float stepClearanceMargin = 0.45f;
    [Tooltip("Desplazamiento hacia adelante de la punta del pie respecto al tobillo")]
    [SerializeField] private float footToeForwardOffset = 2.4f;
    [Tooltip("Semi-ancho del pie para sondear los laterales")]
    [SerializeField] private float footHalfWidth = 1.2f;

    [Header("Curvas de Interpolación")]
    [SerializeField] private AnimationCurve stepHeightCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 4f),
        new Keyframe(0.45f, 1f, 0f, 0f),
        new Keyframe(1f, 0f, -3f, 0f)
    );
    [SerializeField] private AnimationCurve stepEaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Máscara de Terreno")]
    [SerializeField] private LayerMask groundMask;

    [Header("Offsets de Reposo (Espacio Local Mecha)")]
    [SerializeField] private Vector3 localRestOffset = new Vector3(0f, 0f, 0f);

    // Estado interno
    private bool isStepping = false;
    private bool isGrounded = true;
    private bool isClimbingStep = false;
    private float stepProgress = 1f;
    private float currentStepDuration = 0.38f;
    private float dynamicStepHeight = 0.65f;

    private Vector3 currentFootPos;
    private Quaternion currentFootRot;
    private Vector3 startFootPos;
    private Quaternion startFootRot;
    private Vector3 targetFootPos;
    private Quaternion targetFootRot;
    private Vector3 currentGroundNormal = Vector3.up;

    private Vector3 currentHomeTarget;
    private Vector3 homeGroundNormal = Vector3.up;
    private bool homeTargetValid = false;

    // Anticipación de escalones frontales
    private bool detectedStepAhead = false;
    private Vector3 detectedStepDestination;
    private Vector3 detectedStepNormal = Vector3.up;
    private float detectedStepHeight = 0f;

    // Buffer NonAlloc para evitar recolección de basura (GC Allocations)
    private readonly RaycastHit[] raycastBuffer = new RaycastHit[12];

    // Eventos
    public event Action<Vector3, Vector3> OnStepTouchdown;

    // Propiedades públicas
    public bool IsStepping => isStepping;
    public bool IsGrounded => isGrounded;
    public bool IsClimbingStep => isClimbingStep;
    public Vector3 CurrentFootPosition => currentFootPos;
    public Quaternion CurrentFootRotation => currentFootRot;
    public Vector3 TargetFootPosition => targetFootPos;
    public Vector3 HomeTargetPosition => currentHomeTarget;
    public Vector3 GroundNormal => currentGroundNormal;
    public TwoBoneIKSolver IKSolver => ikSolver;
    public float LegReach => ikSolver != null ? ikSolver.TotalLength : 5.6f;
    public float AnkleToSoleOffset => ankleToSoleOffset;
    public bool DetectedStepAhead => detectedStepAhead;

    private void Awake()
    {
        if (bodyRoot == null && transform.parent != null)
        {
            bodyRoot = transform.parent;
        }

        if (bodyRigidbody == null && bodyRoot != null)
        {
            bodyRigidbody = bodyRoot.GetComponent<Rigidbody>();
        }

        if (groundMask.value == 0)
        {
            groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast");
        }

        ikSolver.Initialize();

        if (ikSolver.ankle != null)
        {
            currentFootPos = ikSolver.ankle.position;
            currentFootRot = ikSolver.ankle.rotation;
        }
        else
        {
            currentFootPos = transform.position + Vector3.down * 4.05f;
            currentFootRot = transform.rotation;
        }

        startFootPos = currentFootPos;
        targetFootPos = currentFootPos;
        startFootRot = currentFootRot;
        targetFootRot = currentFootRot;
    }

    private void Start()
    {
        if (UpdateHomeTarget(out Vector3 groundPos, out Vector3 groundNorm))
        {
            currentFootPos = groundPos + groundNorm * ankleToSoleOffset;
            targetFootPos = currentFootPos;
            startFootPos = currentFootPos;
            currentGroundNormal = groundNorm;

            Vector3 fwd = Vector3.ProjectOnPlane(bodyRoot != null ? bodyRoot.forward : transform.forward, groundNorm).normalized;
            currentFootRot = Quaternion.LookRotation(fwd, groundNorm);
            targetFootRot = currentFootRot;
            startFootRot = currentFootRot;
        }

        ikSolver.Solve(currentFootPos, currentFootRot);
    }

    private void Update()
    {
        UpdateHomeTarget(out _, out _);
        CheckStepAhead();

        if (isStepping)
        {
            AdvanceStep(Time.deltaTime);
        }
        else
        {
            ClampPlantedFootToGround();
        }

        // Aplicar el solver analítico en cada frame
        ikSolver.Solve(currentFootPos, currentFootRot);
    }

    /// <summary>
    /// Mantiene la pata firmemente apoyada en el relieve exacto del terreno mientras no está en el aire.
    /// Garantiza que nunca quede flotando ni penetrando el suelo.
    /// </summary>
    private void ClampPlantedFootToGround()
    {
        Vector3 hipWorld = ikSolver.hip != null ? ikSolver.hip.position : transform.position;
        Vector3 probeStart = new Vector3(currentFootPos.x, hipWorld.y + 1.5f, currentFootPos.z);
        float rayDist = ikSolver.TotalLength + 3.0f;

        if (Physics.Raycast(probeStart, Vector3.down, out RaycastHit hit, rayDist, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (bodyRoot == null || !hit.collider.transform.IsChildOf(bodyRoot))
            {
                currentFootPos.y = hit.point.y + ankleToSoleOffset;
                currentGroundNormal = hit.normal;

                Vector3 bodyFwd = bodyRoot != null ? bodyRoot.forward : transform.forward;
                Vector3 projFwd = Vector3.ProjectOnPlane(bodyFwd, currentGroundNormal);
                if (projFwd.sqrMagnitude > 0.001f)
                {
                    currentFootRot = Quaternion.LookRotation(projFwd.normalized, currentGroundNormal);
                }
            }
        }
    }

    /// <summary>
    /// Escanea hacia adelante desde la puntera del pie en busca de escalones, bordillos o rocas antes de chocar.
    /// Si detecta un escalón subible, prepara el pie para elevarse de inmediato y montarse encima sin tropezar.
    /// </summary>
    private void CheckStepAhead()
    {
        detectedStepAhead = false;
        if (isStepping) return;

        Vector3 moveDir = bodyRoot != null ? bodyRoot.forward : transform.forward;
        float speed = 0f;
        if (bodyRigidbody != null)
        {
            Vector3 v = new Vector3(bodyRigidbody.linearVelocity.x, 0f, bodyRigidbody.linearVelocity.z);
            speed = v.magnitude;
            if (speed > 0.08f) moveDir = v.normalized;
        }

        Vector3 footFwd = currentFootRot * Vector3.forward;
        Vector3 footRight = currentFootRot * Vector3.right;
        float footSoleY = currentFootPos.y - ankleToSoleOffset;

        // Proyectar desde el extremo delantero del pie (puntera) en 3 puntos: centro, lateral interno y lateral externo
        Vector3 toeCenter = currentFootPos + footFwd * footToeForwardOffset;
        Vector3[] probeOrigins = new Vector3[]
        {
            toeCenter,
            toeCenter - footRight * (footHalfWidth * 0.7f),
            toeCenter + footRight * (footHalfWidth * 0.7f)
        };

        // Sondear a dos alturas: rasante baja (0.12m para bordillos pequeños) y media (0.40m para rocas)
        float[] probeHeights = new float[] { 0.12f, 0.40f };
        float scanDist = stepAnticipationDistance + speed * 0.22f;

        RaycastHit hitObstacle = default;
        bool obstacleFound = false;

        for (int h = 0; h < probeHeights.Length && !obstacleFound; h++)
        {
            float yOffset = probeHeights[h];
            for (int r = 0; r < probeOrigins.Length; r++)
            {
                Vector3 origin = new Vector3(probeOrigins[r].x, footSoleY + yOffset, probeOrigins[r].z);
                if (Physics.Raycast(origin, moveDir, out RaycastHit fwdHit, scanDist, groundMask, QueryTriggerInteraction.Ignore))
                {
                    if (bodyRoot == null || !fwdHit.collider.transform.IsChildOf(bodyRoot))
                    {
                        // Verificar que es un resalte o pared vertical (inclinación > 20° respecto al suelo)
                        float wallAngle = Vector3.Angle(fwdHit.normal, Vector3.up);
                        if (wallAngle > 20f)
                        {
                            hitObstacle = fwdHit;
                            obstacleFound = true;
                            break;
                        }
                    }
                }
            }
        }

        if (obstacleFound)
        {
            // Sondear hacia abajo a 0.20m por detrás del frente para encontrar la superficie superior (funciona incluso con bordillos delgados de 0.4m)
            Vector3 topProbeOrigin = hitObstacle.point + moveDir * 0.20f + Vector3.up * (maxClimbableStepHeight + 0.6f);
            if (Physics.Raycast(topProbeOrigin, Vector3.down, out RaycastHit topHit, maxClimbableStepHeight + 1.2f, groundMask, QueryTriggerInteraction.Ignore))
            {
                if (bodyRoot == null || !topHit.collider.transform.IsChildOf(bodyRoot))
                {
                    float stepElevation = topHit.point.y - footSoleY;
                    float topSlope = Vector3.Angle(topHit.normal, Vector3.up);

                    if (stepElevation >= minStepObstacleHeight && stepElevation <= maxClimbableStepHeight && topSlope <= maxSlopeAngle)
                    {
                        detectedStepAhead = true;
                        detectedStepDestination = topHit.point + topHit.normal * ankleToSoleOffset;
                        detectedStepNormal = topHit.normal;
                        detectedStepHeight = stepElevation;
                        return;
                    }
                }
            }
        }

        // 2. Comprobar si el Home Target predictivo cae sobre un escalón más alto
        if (homeTargetValid)
        {
            float homeElevation = (currentHomeTarget.y - ankleToSoleOffset) - footSoleY;
            if (homeElevation >= minStepObstacleHeight && homeElevation <= maxClimbableStepHeight)
            {
                detectedStepAhead = true;
                detectedStepDestination = currentHomeTarget;
                detectedStepNormal = homeGroundNormal;
                detectedStepHeight = homeElevation;
            }
        }
    }

    /// <summary>
    /// Calcula la posición ideal del pie en el suelo (Home Target) con predicción según la velocidad lineal y angular.
    /// </summary>
    public bool UpdateHomeTarget(out Vector3 groundPosition, out Vector3 groundNormal)
    {
        groundPosition = currentFootPos;
        groundNormal = Vector3.up;

        if (bodyRoot == null) return false;

        Vector3 hipWorld = ikSolver.hip != null ? ikSolver.hip.position : transform.position;
        Vector3 restPos = bodyRoot.TransformPoint(localRestOffset);

        Vector3 velocity = bodyRigidbody != null ? bodyRigidbody.linearVelocity : Vector3.zero;
        Vector3 planarVel = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 predictedOffset = planarVel * stepLeadTime;

        if (bodyRigidbody != null)
        {
            float angY = bodyRigidbody.angularVelocity.y;
            Vector3 lever = restPos - bodyRoot.position;
            Vector3 tangentialVel = Vector3.Cross(new Vector3(0f, angY, 0f), lever);
            predictedOffset += tangentialVel * (stepLeadTime * 0.75f);
        }

        Vector3 probeStart = new Vector3(restPos.x + predictedOffset.x, hipWorld.y + 1.5f, restPos.z + predictedOffset.z);
        float rayDistance = (ikSolver.TotalLength + 3.0f);

        int hitCount = Physics.SphereCastNonAlloc(
            probeStart,
            footRadius,
            Vector3.down,
            raycastBuffer,
            rayDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        float bestDistance = float.MaxValue;
        bool found = false;
        RaycastHit bestHit = default;

        for (int i = 0; i < hitCount; i++)
        {
            var hit = raycastBuffer[i];
            if (hit.collider == null) continue;
            if (hit.collider.transform.IsChildOf(bodyRoot)) continue;

            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope > maxSlopeAngle) continue;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestHit = hit;
                found = true;
            }
        }

        if (!found)
        {
            hitCount = Physics.RaycastNonAlloc(
                probeStart,
                Vector3.down,
                raycastBuffer,
                rayDistance,
                groundMask,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < hitCount; i++)
            {
                var hit = raycastBuffer[i];
                if (hit.collider == null) continue;
                if (hit.collider.transform.IsChildOf(bodyRoot)) continue;

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestHit = hit;
                    found = true;
                }
            }
        }

        if (found)
        {
            groundPosition = bestHit.point;
            groundNormal = bestHit.normal;
            currentHomeTarget = groundPosition + groundNormal * ankleToSoleOffset;
            homeGroundNormal = groundNormal;
            homeTargetValid = true;
            return true;
        }

        currentHomeTarget = new Vector3(probeStart.x, hipWorld.y - ikSolver.TotalLength * 0.95f, probeStart.z);
        homeGroundNormal = Vector3.up;
        homeTargetValid = false;
        return false;
    }

    /// <summary>
    /// Evalúa si la pata requiere dar un paso por distancia, ángulo de rotación, sobre-extensión o escalón enfrente.
    /// </summary>
    public bool WantsStep(out float urgency)
    {
        urgency = 0f;
        if (isStepping) return false;

        // Prioridad absoluta: si hay un escalón delante, dar el paso ya para levantarse antes de chocar
        if (detectedStepAhead)
        {
            urgency = 10f;
            return true;
        }

        float distFromHome = Vector3.Distance(currentFootPos, currentHomeTarget);

        Vector3 bodyFwd = bodyRoot != null ? bodyRoot.forward : transform.forward;
        Vector3 footFwd = currentFootRot * Vector3.forward;
        float angleDiff = Vector3.Angle(footFwd, bodyFwd);

        Vector3 hipPos = ikSolver.hip != null ? ikSolver.hip.position : transform.position;
        float distFromHip = Vector3.Distance(hipPos, currentFootPos);
        bool overExtended = distFromHip > ikSolver.TotalLength * 0.90f;

        if (distFromHome > stepDistance || angleDiff > turnStepAngle || overExtended)
        {
            float distRatio = distFromHome / Mathf.Max(0.1f, stepDistance);
            float angleRatio = angleDiff / Mathf.Max(1f, turnStepAngle);
            urgency = Mathf.Max(distRatio, angleRatio);
            if (overExtended) urgency += 3.0f;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Inicia la ejecución de un paso procedural hacia targetDestination.
    /// Si hay un escalón, activa la trayectoria vertical de elevación temprana para no tropezar.
    /// </summary>
    public void TakeStep(Vector3 destination, Vector3 surfaceNormal, float durationOverride = -1f)
    {
        if (isStepping) return;

        startFootPos = currentFootPos;
        startFootRot = currentFootRot;

        if (detectedStepAhead)
        {
            targetFootPos = detectedStepDestination;
            currentGroundNormal = detectedStepNormal;
            isClimbingStep = true;
            dynamicStepHeight = Mathf.Max(stepHeight, detectedStepHeight + stepClearanceMargin);
            detectedStepAhead = false;
        }
        else
        {
            targetFootPos = destination;
            currentGroundNormal = surfaceNormal;
            float heightDelta = targetFootPos.y - startFootPos.y;
            isClimbingStep = heightDelta >= minStepObstacleHeight;

            // Prospección proactiva de obstáculos a lo largo de la trayectoria
            dynamicStepHeight = stepHeight;
            if (heightDelta > 0.05f)
            {
                dynamicStepHeight += heightDelta * 0.9f;
            }

            float maxObstacleHeight = 0f;
            Vector3 hipWorld = ikSolver.hip != null ? ikSolver.hip.position : transform.position;
            for (int i = 1; i <= 3; i++)
            {
                float frac = i / 4.0f;
                Vector3 samplePt = Vector3.Lerp(startFootPos, targetFootPos, frac);
                Vector3 probePos = new Vector3(samplePt.x, hipWorld.y + 1.0f, samplePt.z);
                if (Physics.Raycast(probePos, Vector3.down, out RaycastHit obsHit, ikSolver.TotalLength + 2f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    if (bodyRoot == null || !obsHit.collider.transform.IsChildOf(bodyRoot))
                    {
                        float baseline = Mathf.Lerp(startFootPos.y, targetFootPos.y, frac);
                        float obstacleDist = obsHit.point.y - baseline;
                        if (obstacleDist > maxObstacleHeight)
                        {
                            maxObstacleHeight = obstacleDist;
                        }
                    }
                }
            }

            if (maxObstacleHeight > 0.05f)
            {
                dynamicStepHeight = Mathf.Max(dynamicStepHeight, maxObstacleHeight + stepClearanceMargin);
                isClimbingStep = true;
            }
        }

        Vector3 bodyFwd = bodyRoot != null ? bodyRoot.forward : transform.forward;
        Vector3 projectedFwd = Vector3.ProjectOnPlane(bodyFwd, currentGroundNormal);
        if (projectedFwd.sqrMagnitude < 0.001f) projectedFwd = bodyFwd;
        targetFootRot = Quaternion.LookRotation(projectedFwd.normalized, currentGroundNormal);

        currentStepDuration = durationOverride > 0f ? durationOverride : stepDuration;
        stepProgress = 0f;
        isStepping = true;
        isGrounded = false;
    }

    private void AdvanceStep(float deltaTime)
    {
        stepProgress += deltaTime / Mathf.Max(0.05f, currentStepDuration);
        float t = Mathf.Clamp01(stepProgress);

        if (isClimbingStep)
        {
            // Trayectoria especializada para subir escalones/rocas:
            // 1. Primer tercio: elevación vertical rápida antes de avanzar horizontalmente (evita chocar con el frontal)
            // 2. Segundo tercio: traslación horizontal limpia a cota máxima sobre el escalón
            // 3. Último tercio: descenso controlado sobre la superficie superior
            float hProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.20f) / 0.75f));

            float vArc;
            if (t < 0.45f)
            {
                vArc = Mathf.Sin((t / 0.45f) * (Mathf.PI * 0.5f));
            }
            else if (t < 0.75f)
            {
                vArc = 1.0f;
            }
            else
            {
                float settleT = (t - 0.75f) / 0.25f;
                vArc = Mathf.Cos(settleT * (Mathf.PI * 0.5f));
            }

            Vector3 planarPos = Vector3.Lerp(startFootPos, targetFootPos, hProgress);
            float baseHeight = Mathf.Lerp(startFootPos.y, targetFootPos.y, hProgress);
            float currentY = baseHeight + vArc * dynamicStepHeight;

            currentFootPos = new Vector3(planarPos.x, currentY, planarPos.z);
            currentFootRot = Quaternion.Slerp(startFootRot, targetFootRot, hProgress);
        }
        else
        {
            // Trayectoria estándar suave para terreno continuo
            float horizontalT = stepEaseCurve != null ? stepEaseCurve.Evaluate(t) : t;
            float verticalArc = stepHeightCurve != null ? stepHeightCurve.Evaluate(t) : Mathf.Sin(t * Mathf.PI);

            Vector3 planarPos = Vector3.Lerp(startFootPos, targetFootPos, horizontalT);
            float baseHeight = Mathf.Lerp(startFootPos.y, targetFootPos.y, t);
            float currentY = baseHeight + verticalArc * dynamicStepHeight;

            currentFootPos = new Vector3(planarPos.x, currentY, planarPos.z);
            currentFootRot = Quaternion.Slerp(startFootRot, targetFootRot, horizontalT);
        }

        if (t >= 1.0f)
        {
            currentFootPos = targetFootPos;
            currentFootRot = targetFootRot;
            isStepping = false;
            isGrounded = true;
            isClimbingStep = false;

            OnStepTouchdown?.Invoke(targetFootPos, currentGroundNormal);
        }
    }

    public void SnapToGround(Vector3 groundPos, Vector3 groundNorm)
    {
        isStepping = false;
        isGrounded = true;
        isClimbingStep = false;
        stepProgress = 1f;

        currentFootPos = groundPos + groundNorm * ankleToSoleOffset;
        targetFootPos = currentFootPos;
        startFootPos = currentFootPos;
        currentGroundNormal = groundNorm;

        Vector3 fwd = Vector3.ProjectOnPlane(bodyRoot != null ? bodyRoot.forward : transform.forward, groundNorm).normalized;
        currentFootRot = Quaternion.LookRotation(fwd, groundNorm);
        targetFootRot = currentFootRot;
        startFootRot = currentFootRot;

        ikSolver.Solve(currentFootPos, currentFootRot);
    }

    public void SetLocalRestOffset(Vector3 offset)
    {
        localRestOffset = offset;
    }

    private void OnDrawGizmosSelected()
    {
        if (ikSolver != null)
        {
            ikSolver.DrawGizmos(isGrounded ? Color.green : Color.cyan);
        }

        Gizmos.color = homeTargetValid ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(currentHomeTarget, 0.25f);
        Gizmos.DrawRay(currentHomeTarget, homeGroundNormal * 0.8f);

        Gizmos.color = isStepping ? Color.magenta : Color.blue;
        Gizmos.DrawWireCube(currentFootPos, new Vector3(0.6f, 0.2f, 0.9f));

        // Visualizar puntera y rayo de anticipación
        Vector3 footFwd = currentFootRot * Vector3.forward;
        Vector3 toeCenter = currentFootPos + footFwd * footToeForwardOffset;
        float footSoleY = currentFootPos.y - ankleToSoleOffset;
        Gizmos.color = detectedStepAhead ? Color.yellow : Color.cyan;
        Gizmos.DrawWireSphere(new Vector3(toeCenter.x, footSoleY + 0.12f, toeCenter.z), 0.18f);
        Vector3 moveFwd = bodyRoot != null ? bodyRoot.forward : transform.forward;
        Gizmos.DrawRay(new Vector3(toeCenter.x, footSoleY + 0.12f, toeCenter.z), moveFwd * stepAnticipationDistance);

        if (detectedStepAhead)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(detectedStepDestination, new Vector3(0.7f, 0.2f, 0.7f));
            Gizmos.DrawLine(currentFootPos, detectedStepDestination);
        }

        if (isStepping)
        {
            Gizmos.color = isClimbingStep ? Color.yellow : Color.magenta;
            Gizmos.DrawLine(startFootPos, targetFootPos);
        }
    }
}
