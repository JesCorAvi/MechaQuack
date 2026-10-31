using UnityEngine;

/// <summary>
/// Componente para mostrar textos flotantes cómicos en el espacio 3D (por ejemplo, "¡CUAC!").
/// Se eleva, hace un pop de escala y se desvanece suavemente antes de autodestruirse.
/// Siempre rota hacia la cámara activa (Billboard).
/// </summary>
public class FloatingText : MonoBehaviour
{
    [Header("Configuración de Animación")]
    [Tooltip("Velocidad de elevación del texto en metros por segundo.")]
    [SerializeField] private float floatSpeed = 1.8f;

    [Tooltip("Duración total en segundos antes de desaparecer.")]
    [SerializeField] private float lifetime = 1.2f;

    [Tooltip("Escala máxima alcanzada durante el efecto pop.")]
    [SerializeField] private float popScale = 1.4f;

    private TextMesh textMesh;
    private Color initialColor = Color.yellow;
    private float timer = 0f;
    private Vector3 initialScale;

    private void Awake()
    {
        textMesh = GetComponent<TextMesh>();
        if (textMesh != null)
        {
            initialColor = textMesh.color;
        }
        initialScale = transform.localScale;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / lifetime);

        // Movimiento hacia arriba
        transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

        // Billboard: mirar siempre hacia la cámara principal
        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }

        // Efecto pop de escala cómico (sube y luego se estabiliza)
        float scaleMultiplier = progress < 0.25f 
            ? Mathf.Lerp(0.5f, popScale, progress / 0.25f)
            : Mathf.Lerp(popScale, 1.0f, (progress - 0.25f) / 0.75f);
        transform.localScale = initialScale * scaleMultiplier;

        // Desvanecimiento suave (Fade out)
        if (textMesh != null)
        {
            float alpha = Mathf.Clamp01(1f - progress);
            textMesh.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
        }

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Método estático auxiliar para crear un texto flotante en cualquier punto del mundo.
    /// </summary>
    public static FloatingText Spawn(Vector3 worldPosition, string message, Color textColor, float characterSize = 0.25f)
    {
        GameObject textObj = new GameObject("FloatingText_" + message);
        textObj.transform.position = worldPosition;

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = message;
        tm.fontSize = 48;
        tm.characterSize = characterSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = textColor;

        // Añadir MeshRenderer con shader adecuado para renderizado correcto
        MeshRenderer mr = textObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            // Usar material predeterminado de fuentes para asegurar legibilidad
            mr.material = tm.font != null && tm.font.material != null ? tm.font.material : mr.material;
        }

        FloatingText ft = textObj.AddComponent<FloatingText>();
        return ft;
    }
}
