using UnityEngine;

/// <summary>
/// Representa el Pedestal y Círculo Marcado de Mando del Capitán en la Planta 3 (Cabeza del Mecha).
/// Contiene la plataforma circular central (Pedestal_Hazard_Ring y Captain_Holo_Disc).
/// Cuando el pato entra físicamente en este círculo marcado, se desbloquea y activa
/// automáticamente la cámara exterior en 3ª persona del titán colosal de 50 metros.
/// Al salir del círculo (por ejemplo para caminar hacia las escaleras o el botón SPAWN),
/// la cámara se repliega de forma inmediata y automática al interior de la cabina.
/// </summary>
public class CaptainCommandPedestal : MonoBehaviour
{
    public static CaptainCommandPedestal Instance { get; private set; }

    [Header("Geometría del Círculo de Mando")]
    [Tooltip("Radio de detección nominal para entrar al círculo de mando")]
    [SerializeField] private float detectionRadius = 2.2f;

    [Tooltip("Margen de histéresis hacia el exterior para evitar fluctuaciones en el borde al caminar")]
    [SerializeField] private float exitHysteresis = 0.3f;

    [Tooltip("Rango vertical mínimo y máximo relativo al pedestal para considerar al pato sobre la plataforma")]
    [SerializeField] private float minHeightOffset = -0.6f;
    [SerializeField] private float maxHeightOffset = 3.5f;

    [Header("Componentes Visuales")]
    [SerializeField] private Transform holoDiscTransform;
    [SerializeField] private Transform hazardRingTransform;
    [SerializeField] private Color activeEmissionColor = new Color(0.1f, 0.95f, 1.0f, 1.0f);
    [SerializeField] private Color idleEmissionColor = new Color(0.04f, 0.35f, 0.45f, 0.8f);

    private Material holoDiscMaterial;
    private bool isPlayerInside = false;

    public float Radius => detectionRadius;
    public float ExitRadius => detectionRadius + exitHysteresis;
    public bool IsPlayerInside => isPlayerInside;

    private void Awake()
    {
        Instance = this;

        if (holoDiscTransform == null)
        {
            holoDiscTransform = transform.Find("Captain_Holo_Disc");
        }
        if (hazardRingTransform == null)
        {
            hazardRingTransform = transform.Find("Pedestal_Hazard_Ring");
        }

        if (holoDiscTransform != null)
        {
            var mr = holoDiscTransform.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                holoDiscMaterial = mr.material;
            }
        }
    }

    private void Start()
    {
        // Calibrar el radio automáticamente según la escala del cilindro si fue alterada
        float calculatedRadius = transform.localScale.x * 0.5f;
        if (calculatedRadius > 0.5f)
        {
            detectionRadius = calculatedRadius;
        }
    }

    private void Update()
    {
        if (CockpitCameraController.Instance != null)
        {
            isPlayerInside = CockpitCameraController.Instance.IsInCaptainCircle;
        }

        // Animación dinámica de pulso holográfico (alta energía cuando el Capitán está en el círculo)
        if (holoDiscMaterial != null)
        {
            float pulse = Mathf.Sin(Time.time * (isPlayerInside ? 4.5f : 1.8f)) * 0.5f + 0.5f;
            Color targetCol = Color.Lerp(idleEmissionColor, activeEmissionColor, isPlayerInside ? (0.7f + pulse * 0.3f) : (pulse * 0.35f));
            if (holoDiscMaterial.HasProperty("_EmissionColor"))
            {
                holoDiscMaterial.SetColor("_EmissionColor", targetCol);
                holoDiscMaterial.EnableKeyword("_EMISSION");
            }
        }
    }

    /// <summary>
    /// Comprueba si unas coordenadas locales relativas a la cabina se encuentran dentro del círculo marcado.
    /// Incorpora histéresis anti-rebote.
    /// </summary>
    public bool CheckDuckInside(Vector3 localPos, bool currentlyInside)
    {
        Vector3 pedestalLocal = transform.localPosition;
        float dy = localPos.y - pedestalLocal.y;
        if (dy < minHeightOffset || dy > maxHeightOffset) return false;

        float dx = localPos.x - pedestalLocal.x;
        float dz = localPos.z - pedestalLocal.z;
        float distXZ = Mathf.Sqrt(dx * dx + dz * dz);

        float threshold = currentlyInside ? ExitRadius : Radius;
        return distXZ <= threshold;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isPlayerInside ? Color.cyan : new Color(0f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, ExitRadius);
    }
}
