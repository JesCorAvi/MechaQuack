using UnityEngine;

/// <summary>
/// Solucionador analítico de Cinemática Inversa (Two-Bone IK) en 3D para extremidades de 2 huesos.
/// Resuelve Cadera -> Rodilla -> Tobillo con vector de orientación de rodilla (Pole Target).
/// Es completamente analítico (Ley de Cosenos), determinista, de coste mínimo (0 allocations)
/// y sin dependencias de paquetes externos (compatible sin Unity Animation Rigging).
/// </summary>
[System.Serializable]
public class TwoBoneIKSolver
{
    [Header("Articulaciones")]
    public Transform hip;
    public Transform knee;
    public Transform ankle;

    [Header("Orientación")]
    public Transform poleTarget;
    public Vector3 defaultPoleOffset = new Vector3(0f, 0f, 3.5f);

    [Header("Dimensiones")]
    [SerializeField] private float upperLength = 10.5f;
    [SerializeField] private float lowerLength = 10.0f;
    [SerializeField] private bool autoCalculateLengths = true;

    public float UpperLength { get => upperLength; set => upperLength = value; }
    public float LowerLength { get => lowerLength; set => lowerLength = value; }
    public float TotalLength => upperLength + lowerLength;

    public void Initialize()
    {
        if (hip == null || knee == null || ankle == null) return;

        if (autoCalculateLengths)
        {
            upperLength = Vector3.Distance(hip.position, knee.position);
            lowerLength = Vector3.Distance(knee.position, ankle.position);
            if (upperLength < 0.01f) upperLength = 10.5f;
            if (lowerLength < 0.01f) lowerLength = 10.0f;
        }
    }

    /// <summary>
    /// Resuelve las rotaciones de cadera y rodilla para posicionar el tobillo en targetPosition con targetRotation.
    /// </summary>
    public void Solve(Vector3 targetPosition, Quaternion targetRotation)
    {
        if (hip == null || knee == null || ankle == null) return;

        Vector3 hipPos = hip.position;
        Vector3 forwardRef = hip.parent != null ? hip.parent.forward : hip.forward;
        Vector3 upRef = hip.parent != null ? hip.parent.up : hip.up;
        Vector3 polePos = poleTarget != null ? poleTarget.position : (hipPos + forwardRef * defaultPoleOffset.z + upRef * defaultPoleOffset.y);

        Vector3 toTarget = targetPosition - hipPos;
        float distToTarget = toTarget.magnitude;

        // Limitar distancia para evitar hiperextensión singular
        float maxReach = (upperLength + lowerLength) * 0.999f;
        float minReach = Mathf.Max(0.1f, Mathf.Abs(upperLength - lowerLength) + 0.05f);
        float clampedDist = Mathf.Clamp(distToTarget, minReach, maxReach);

        Vector3 targetDir = distToTarget > 0.001f ? (toTarget / distToTarget) : hip.forward;
        Vector3 clampedTarget = hipPos + targetDir * clampedDist;

        // Ley de cosenos para el ángulo en la cadera (alpha)
        float cosAlpha = (upperLength * upperLength + clampedDist * clampedDist - lowerLength * lowerLength) / (2f * upperLength * clampedDist);
        float alpha = Mathf.Acos(Mathf.Clamp(cosAlpha, -1f, 1f));

        // Construir plano de flexión de la extremidad con el pole target
        Vector3 toPole = polePos - hipPos;
        Vector3 planeNormal = Vector3.Cross(targetDir, toPole);
        if (planeNormal.sqrMagnitude < 0.0001f)
        {
            planeNormal = hip.right;
        }
        else
        {
            planeNormal.Normalize();
        }

        // Vector perpendicular a targetDir en dirección hacia la rodilla/pole
        Vector3 bendDir = Vector3.Cross(planeNormal, targetDir).normalized;

        // Posición calculada de la rodilla en el mundo
        Vector3 kneeDir = (targetDir * Mathf.Cos(alpha) + bendDir * Mathf.Sin(alpha)).normalized;
        Vector3 kneePos = hipPos + kneeDir * upperLength;

        // Dirección del fémur (cadera -> rodilla)
        Vector3 u1 = kneeDir;
        // Base ortonormal para la cadera: -Y hacia la rodilla, +Z hacia adelante (rodilla)
        Vector3 hipY = -u1;
        Vector3 hipX = -planeNormal;
        Vector3 hipZ = Vector3.Cross(hipX, hipY).normalized;
        hip.rotation = Quaternion.LookRotation(hipZ, hipY);

        // Dirección de la tibia (rodilla -> tobillo)
        Vector3 u2 = (clampedTarget - kneePos).normalized;
        Vector3 kneeY = -u2;
        Vector3 kneeX = -planeNormal;
        Vector3 kneeZ = Vector3.Cross(kneeX, kneeY).normalized;
        knee.rotation = Quaternion.LookRotation(kneeZ, kneeY);

        // Rotación del pie/tobillo según orientación de apoyo calculada
        ankle.rotation = targetRotation;
    }

    public void DrawGizmos(Color color)
    {
        if (hip == null || knee == null || ankle == null) return;

        Gizmos.color = color;
        Gizmos.DrawLine(hip.position, knee.position);
        Gizmos.DrawLine(knee.position, ankle.position);
        Gizmos.DrawWireSphere(hip.position, 0.15f);
        Gizmos.DrawWireSphere(knee.position, 0.15f);
        Gizmos.DrawWireSphere(ankle.position, 0.15f);

        if (poleTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(poleTarget.position, 0.15f);
            Gizmos.DrawLine(knee.position, poleTarget.position);
        }
    }
}
