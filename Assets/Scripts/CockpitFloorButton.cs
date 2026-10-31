using UnityEngine;

/// <summary>
/// Representa un botón físico en el suelo de la cabina del mecha.
/// Puede ser de tipo continuo (Hold) para dirección, o de impacto (Stomp) para armas.
/// </summary>
public class CockpitFloorButton : MonoBehaviour
{
    public enum ButtonType
    {
        Hold,   // Se activa mientras el pato esté parado encima (FWD, BWD, LEFT, RIGHT)
        Stomp,  // Requiere un pisotón aéreo/culazo con la tecla E para activarse (PUNCH, LASER)
        Toggle  // Conmuta encendido/apagado al pisar o hacer culazo (SPAWN ENEMIGOS)
    }

    [Header("Identificación")]
    public string buttonId = "FWD";
    public string label = "▲ AVANZAR";
    public ButtonType type = ButtonType.Hold;
    public Color buttonColor = Color.green;

    [Header("Mecánicas Visuales")]
    [SerializeField] private Transform capTransform;
    [SerializeField] private float pressDepth = 0.16f;
    [SerializeField] private float sinkSpeed = 16f;

    [Header("Estado")]
    public bool isPressed = false;
    private Vector3 initialCapLocalPos;
    private Material capMaterial;
    private Color initialEmissive;
    private float stompCooldownTimer = 0f;
    private float hintCooldownTimer = 0f;

    private void Awake()
    {
        if (capTransform == null && transform.childCount > 0)
        {
            capTransform = transform.Find("Cap") ?? transform.GetChild(0);
        }

        if (capTransform != null)
        {
            initialCapLocalPos = capTransform.localPosition;
            MeshRenderer mr = capTransform.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                capMaterial = mr.material;
            }
        }
    }

    private void Update()
    {
        if (stompCooldownTimer > 0f)
        {
            stompCooldownTimer -= Time.deltaTime;
            if (stompCooldownTimer <= 0f)
            {
                isPressed = false;
            }
        }

        if (hintCooldownTimer > 0f)
        {
            hintCooldownTimer -= Time.deltaTime;
        }

        // Animar hundimiento suave del botón
        if (capTransform != null)
        {
            Vector3 targetPos = initialCapLocalPos - (isPressed ? Vector3.up * pressDepth : Vector3.zero);
            capTransform.localPosition = Vector3.Lerp(capTransform.localPosition, targetPos, Time.deltaTime * sinkSpeed);
        }

        // Brillo emisivo cuando está activo
        if (capMaterial != null)
        {
            float intensity = isPressed ? 0.9f : 0.2f;
            Color emissiveCol = buttonColor * intensity;
            if (capMaterial.HasProperty("_EmissionColor"))
            {
                capMaterial.SetColor("_EmissionColor", emissiveCol);
                capMaterial.EnableKeyword("_EMISSION");
            }
        }
    }

    /// <summary>
    /// Llamado continuamente mientras el pato se encuentra sobre el botón.
    /// </summary>
    public void OnDuckStanding(DuckController duck)
    {
        if (type == ButtonType.Hold)
        {
            if (!isPressed)
            {
                isPressed = true;
                ProceduralAudio.Instance?.PlayButtonClick();
                Debug.Log($"[GAME] Botón {buttonId} ({label}) presionado");
            }
        }
        else if (type == ButtonType.Stomp)
        {
            // Si solo camina por encima sin hacer culazo, mostrar recordatorio cómico
            if (hintCooldownTimer <= 0f)
            {
                hintCooldownTimer = 1.8f;
                FloatingText.Spawn(transform.position + Vector3.up * 1.2f, "¡SALTA Y HAZ CULAZO (E)!", Color.white, 0.22f);
            }
        }
        else if (type == ButtonType.Toggle)
        {
            if (stompCooldownTimer <= 0f)
            {
                stompCooldownTimer = 1.2f;
                isPressed = !isPressed;
                Debug.Log($"[GAME] Botón {buttonId} conmutado a: {isPressed}");
                ExecuteToggleAction();
            }
        }
    }

    /// <summary>
    /// Llamado cuando el pato sale del área del botón.
    /// </summary>
    public void OnDuckExit()
    {
        if (type == ButtonType.Hold && isPressed)
        {
            isPressed = false;
            Debug.Log($"[GAME] Botón {buttonId} liberado");
        }
    }

    /// <summary>
    /// Llamado cuando el pato aterriza con un pisotón (Stomp / Culazo) sobre este botón.
    /// </summary>
    public bool OnDuckStomp(DuckController duck)
    {
        if (type == ButtonType.Stomp)
        {
            isPressed = true;
            stompCooldownTimer = 0.35f;
            ProceduralAudio.Instance?.PlayStomp();
            Debug.Log($"[GAME] Botón de arma {buttonId} activado con culazo");

            // Hundir instantáneamente el cap
            if (capTransform != null)
            {
                capTransform.localPosition = initialCapLocalPos - Vector3.up * (pressDepth * 1.3f);
            }

            return true;
        }
        else if (type == ButtonType.Hold)
        {
            // Un pisotón en un botón de avance/giro también lo activa con fuerza
            isPressed = true;
            stompCooldownTimer = 0.5f;
            ProceduralAudio.Instance?.PlayButtonClick();
            Debug.Log($"[GAME] Botón de movimiento {buttonId} pisado con fuerza");
            return true;
        }
        else if (type == ButtonType.Toggle)
        {
            stompCooldownTimer = 1.2f;
            isPressed = !isPressed;
            ExecuteToggleAction();
            ProceduralAudio.Instance?.PlayStomp();
            Debug.Log($"[GAME] Botón conmutador {buttonId} activado con culazo (Estado: {isPressed})");
            return true;
        }

        return false;
    }

    private void ExecuteToggleAction()
    {
        if (buttonId == "SPAWN")
        {
            EnemySpawner.Instance?.ToggleSpawning();
        }
    }

    /// <summary>
    /// Restablece el botón a su estado inicial no pulsado.
    /// </summary>
    public void ResetButton()
    {
        isPressed = false;
        stompCooldownTimer = 0f;
        if (capTransform != null) capTransform.localPosition = initialCapLocalPos;
        if (capMaterial != null && capMaterial.HasProperty("_EmissionColor"))
        {
            capMaterial.SetColor("_EmissionColor", initialEmissive);
        }
    }
}
