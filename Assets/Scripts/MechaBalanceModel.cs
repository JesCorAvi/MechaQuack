using UnityEngine;

/// <summary>
/// Modelo matemático de equilibrio bípedo en tiempo fijo (FixedUpdate).
/// Calcula el Polígono de Soporte Convexo de los pies, la proyección del Centro de Masa (CoM),
/// el Centro de Masa Extrapolado (XcoM / Capture Point) según el modelo de Péndulo Invertido Lineal,
/// el margen de estabilidad y el torque gravitacional real de vuelco.
/// </summary>
public class MechaBalanceModel
{
    public struct BalanceState
    {
        public Vector3 worldCoM;
        public Vector3 worldCoMVelocity;
        public Vector3 projectedCoM;            // Proyección del CoM en el plano de soporte
        public Vector3 extrapolatedCoM;         // XcoM = CoM + Vel / omega_0 (Capture Point)
        public Vector3 supportCenter;            // Centro geométrico del polígono de soporte
        public Vector3 closestSupportPoint;      // Punto más cercano del borde del polígono al XcoM
        public float stabilityMargin;            // Positivo = dentro del polígono; Negativo = fuera (desequilibrado)
        public float tiltAngleDegrees;           // Ángulo del chasis respecto a la vertical
        public float unstableTimer;              // Tiempo acumulado fuera del margen estable
        public bool isStaticallyStable;          // CoM dentro del polígono
        public bool isDynamicallyStable;         // XcoM dentro del polígono
        public bool beyondPointOfNoReturn;       // True si la caída es físicamente inevitable
        public Vector3 idealCaptureStepPoint;    // Dónde debería pisar una pata para detener la caída
    }

    private readonly MechaPhysicsConfig _config;
    private BalanceState _state;

    // Vértices preasignados en el plano XZ para evitar GC Allocations por frame (máx 8 vértices para 2 pies rectangulares)
    private readonly Vector2[] _supportPolygon2D = new Vector2[8];
    private readonly Vector2[] _hullBuffer = new Vector2[8];
    private int _polygonVertexCount = 0;
    private float _supportPlaneY = 0f;

    public BalanceState CurrentState => _state;
    public int PolygonVertexCount => _polygonVertexCount;
    public float SupportPlaneY => _supportPlaneY;

    public MechaBalanceModel(MechaPhysicsConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Obtiene un vértice del polígono de soporte actual en coordenadas de mundo para depuración (Gizmos).
    /// </summary>
    public Vector3 GetPolygonVertexWorld(int index)
    {
        if (index < 0 || index >= _polygonVertexCount) return Vector3.zero;
        Vector2 v = _supportPolygon2D[index];
        return new Vector3(v.x, _supportPlaneY, v.y);
    }

    /// <summary>
    /// Evalúa el estado de equilibrio completo en FixedUpdate.
    /// </summary>
    public void Evaluate(
        Rigidbody rb,
        Transform rootTransform,
        bool leftGrounded, Vector3 leftFootPos, Quaternion leftFootRot,
        bool rightGrounded, Vector3 rightFootPos, Quaternion rightFootRot,
        float dt,
        bool isStepping = false,
        bool isVoluntaryMoving = false)
    {
        _state.worldCoM = rb.worldCenterOfMass;
        _state.worldCoMVelocity = rb.linearVelocity;
        _state.tiltAngleDegrees = Vector3.Angle(rootTransform.up, Vector3.up);

        // 1. Construir el polígono de soporte según los pies apoyados o en paso de apoyo
        BuildSupportPolygon(leftGrounded, leftFootPos, leftFootRot, rightGrounded, rightFootPos, rightFootRot, isStepping);

        if (_polygonVertexCount == 0)
        {
            // En el aire o sin ningún pie apoyado
            _state.isStaticallyStable = false;
            _state.isDynamicallyStable = false;
            _state.stabilityMargin = -999f;
            _state.projectedCoM = new Vector3(_state.worldCoM.x, _supportPlaneY, _state.worldCoM.z);
            _state.extrapolatedCoM = _state.projectedCoM;
            return;
        }

        // 2. Frecuencia natural del Péndulo Invertido Lineal: omega_0 = sqrt(g / h_com)
        float pendulumHeight = Mathf.Max(0.5f, _state.worldCoM.y - _supportPlaneY);
        float effectiveGravity = _config.gravity * _config.gravityScale;
        float omega0 = Mathf.Sqrt(effectiveGravity / pendulumHeight);

        // 3. Proyección del CoM y cálculo del XcoM (Extrapolated Center of Mass)
        _state.projectedCoM = new Vector3(_state.worldCoM.x, _supportPlaneY, _state.worldCoM.z);
        Vector3 horizVel = new Vector3(_state.worldCoMVelocity.x, 0f, _state.worldCoMVelocity.z);
        _state.extrapolatedCoM = _state.projectedCoM + (horizVel / omega0);
        _state.idealCaptureStepPoint = _state.extrapolatedCoM + horizVel.normalized * 0.25f;

        // 4. Evaluar inclusión en el polígono convexo y distancia firmada al borde (Stability Margin)
        Vector2 com2D = new Vector2(_state.projectedCoM.x, _state.projectedCoM.z);
        Vector2 xcom2D = new Vector2(_state.extrapolatedCoM.x, _state.extrapolatedCoM.z);

        _state.isStaticallyStable = IsPointInsideConvexPolygon(com2D, out _);
        _state.isDynamicallyStable = IsPointInsideConvexPolygon(xcom2D, out Vector2 closestPt2D);

        _state.closestSupportPoint = new Vector3(closestPt2D.x, _supportPlaneY, closestPt2D.y);
        float distToEdge = Vector2.Distance(xcom2D, closestPt2D);
        _state.stabilityMargin = _state.isDynamicallyStable ? distToEdge : -distToEdge;

        // 5. Acumular tiempo de inestabilidad física
        // Se considera inestabilidad física real si el CoM estático está fuera del polígono o el torso se inclina
        bool physicallyUnstable = !_state.isStaticallyStable && (_state.tiltAngleDegrees > 15.0f);
        if (physicallyUnstable)
        {
            _state.unstableTimer += dt;
        }
        else
        {
            _state.unstableTimer = Mathf.Max(0f, _state.unstableTimer - dt * 2.5f);
        }

        // 6. Criterio riguroso de Punto de No Retorno (Físicamente irrecuperable):
        // NUNCA colapsa si el mecha está prácticamente erguido (<18° de inclinación).
        // Solo colapsa si:
        // A) Inclinación crítica absoluta (> criticalTiltAngle, ej. 42°) donde el centro de gravedad vence a los actuadores.
        // B) Pérdida de apoyo unilateral (pata en precipicio) con inclinación hacia el vacío (> 20°) sostenida.
        // C) Vuelco progresivo sostenido con CoM fuera del polígono e inclinación > 25° durante más de maxUnstableDuration.
        bool exceededCriticalTilt = _state.tiltAngleDegrees > _config.criticalTiltAngle;
        bool singleFootCliffLoss = (_polygonVertexCount <= 4) && (!_state.isStaticallyStable) && (_state.tiltAngleDegrees > 20.0f) && (_state.unstableTimer > _config.maxUnstableDuration);
        bool sustainedTopple = (!_state.isStaticallyStable) && (_state.tiltAngleDegrees > 25.0f) && (_state.unstableTimer > _config.maxUnstableDuration);

        _state.beyondPointOfNoReturn = exceededCriticalTilt || singleFootCliffLoss || sustainedTopple;
    }

    /// <summary>
    /// Calcula el par físico real de vuelco gravitacional cuando el CoM rebasa el borde de apoyo:
    /// Tau = r x F_g, donde r = (CoM - PuntoDeApoyoMasCercano).
    /// </summary>
    public Vector3 CalculateGravitationalToppleTorque()
    {
        if (_polygonVertexCount == 0 || _state.isStaticallyStable)
            return Vector3.zero;

        Vector3 pivotToCoM = _state.worldCoM - _state.closestSupportPoint;
        Vector3 gravityForce = Vector3.down * (_config.mass * _config.gravity * _config.gravityScale * _config.gravityToppleMultiplier);
        return Vector3.Cross(pivotToCoM, gravityForce);
    }

    public void ResetTimers()
    {
        _state.unstableTimer = 0f;
        _state.beyondPointOfNoReturn = false;
    }

    private void BuildSupportPolygon(
        bool leftGrounded, Vector3 leftPos, Quaternion leftRot,
        bool rightGrounded, Vector3 rightPos, Quaternion rightRot,
        bool isStepping = false)
    {
        int rawCount = 0;
        _supportPlaneY = 0f;
        int groundedCount = 0;

        // Si una pata está dando un paso sobre terreno firme, forma parte de la base dinámica de apoyo
        if (leftGrounded || isStepping)
        {
            AddFootCorners(leftPos, leftRot, ref rawCount);
            _supportPlaneY += leftPos.y;
            groundedCount++;
        }

        if (rightGrounded || isStepping)
        {
            AddFootCorners(rightPos, rightRot, ref rawCount);
            _supportPlaneY += rightPos.y;
            groundedCount++;
        }

        if (groundedCount == 0)
        {
            _polygonVertexCount = 0;
            return;
        }

        _supportPlaneY /= groundedCount;

        // Calcular la envolvente convexa (Convex Hull 2D mediante algoritmo Jarvis March / Gift Wrapping sin GC)
        ComputeConvexHull2D(rawCount);

        // Calcular el centroide geométrico
        Vector2 centroid = Vector2.zero;
        for (int i = 0; i < _polygonVertexCount; i++)
        {
            centroid += _supportPolygon2D[i];
        }
        centroid /= Mathf.Max(1, _polygonVertexCount);
        _state.supportCenter = new Vector3(centroid.x, _supportPlaneY, centroid.y);
    }

    private void AddFootCorners(Vector3 footPos, Quaternion footRot, ref int count)
    {
        Vector3 fwd = Vector3.ProjectOnPlane(footRot * Vector3.forward, Vector3.up).normalized;
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
        Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);

        Vector3 fExt = fwd * _config.footHalfLength;
        Vector3 rExt = right * _config.footHalfWidth;

        Vector3 c0 = footPos + fExt + rExt;
        Vector3 c1 = footPos + fExt - rExt;
        Vector3 c2 = footPos - fExt - rExt;
        Vector3 c3 = footPos - fExt + rExt;

        _hullBuffer[count++] = new Vector2(c0.x, c0.z);
        _hullBuffer[count++] = new Vector2(c1.x, c1.z);
        _hullBuffer[count++] = new Vector2(c2.x, c2.z);
        _hullBuffer[count++] = new Vector2(c3.x, c3.z);
    }

    private void ComputeConvexHull2D(int inputCount)
    {
        if (inputCount <= 0)
        {
            _polygonVertexCount = 0;
            return;
        }

        // Encontrar el punto más a la izquierda
        int leftMost = 0;
        for (int i = 1; i < inputCount; i++)
        {
            if (_hullBuffer[i].x < _hullBuffer[leftMost].x)
                leftMost = i;
        }

        int p = leftMost;
        int hullCount = 0;
        do
        {
            _supportPolygon2D[hullCount++] = _hullBuffer[p];
            int q = (p + 1) % inputCount;
            for (int i = 0; i < inputCount; i++)
            {
                if (CrossProduct2D(_hullBuffer[p], _hullBuffer[i], _hullBuffer[q]) < 0f)
                {
                    q = i;
                }
            }
            p = q;
        } while (p != leftMost && hullCount < 8);

        _polygonVertexCount = hullCount;
    }

    private static float CrossProduct2D(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    private bool IsPointInsideConvexPolygon(Vector2 pt, out Vector2 closestEdgePoint)
    {
        closestEdgePoint = pt;
        if (_polygonVertexCount == 0) return false;

        bool inside = true;
        float minSqDist = float.MaxValue;

        for (int i = 0; i < _polygonVertexCount; i++)
        {
            Vector2 a = _supportPolygon2D[i];
            Vector2 b = _supportPolygon2D[(i + 1) % _polygonVertexCount];

            // En orden horario (originado por el Gift Wrapping anterior), CrossProduct < 0 significa fuera
            if (CrossProduct2D(a, b, pt) > 0f)
            {
                inside = false;
            }

            Vector2 proj = ClosestPointOnSegment2D(a, b, pt);
            float sqDist = (pt - proj).sqrMagnitude;
            if (sqDist < minSqDist)
            {
                minSqDist = sqDist;
                closestEdgePoint = proj;
            }
        }

        return inside;
    }

    private static Vector2 ClosestPointOnSegment2D(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.00001f) return a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
        return a + ab * t;
    }
}
