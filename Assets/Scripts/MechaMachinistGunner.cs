using UnityEngine;

/// <summary>
/// Controlador y actor del Maquinista Artillero (Gunner / Machinist Duck) de MechaQuack.
/// Ubicado en su puesto táctico elevado dentro de la cabina, apunta visualmente las armas,
/// empuña los mandos de tiro (joysticks) con retroceso y emite reacciones cómicas y telemetría
/// cuando se disparan la Ametralladora Gatling y los Micromisiles guiados.
/// </summary>
public class MechaMachinistGunner : MonoBehaviour
{
    public static MechaMachinistGunner Instance { get; private set; }

    [Header("Referencias de Armamento y Mecha")]
    [SerializeField] private MechaController mecha;

    [Header("Partes del Maquinista y Puesto")]
    [SerializeField] private Transform gunnerBodyTransform;
    [SerializeField] private Transform gunnerHeadTransform;
    [SerializeField] private Transform joystickLeft;
    [SerializeField] private Transform joystickRight;
    [SerializeField] private Transform targetingScreenTransform;
    [SerializeField] private Light tacticalScreenLight;

    private float barkCooldown = 0f;
    private float joystickRecoilAngle = 0f;
    private Quaternion joyLRestRot;
    private Quaternion joyRRestRot;
    private Quaternion headRestRot;
    private float idleScanTimer = 0f;

    private readonly string[] gatlingBarks = new string[]
    {
        "¡FUEGO DE SUPRESIÓN! 💥",
        "¡RECIBID PLOMO, CHATARRAS! 🔫",
        "¡CADENCIA MÁXIMA! ⚙",
        "¡CAÑÓN AL ROJO VIVO! 🔥"
    };

    private readonly string[] missileBarks = new string[]
    {
        "¡SALVA DE MISILES EN CAMINO! 🚀",
        "¡BLANCO FIJADO! ¡FUEGO! 🎯",
        "¡LLUVIA DE FUEGO GUIADA! ⚡",
        "¡MISILES FUERA! 💥"
    };

    private void Awake()
    {
        Instance = this;
        if (mecha == null) mecha = GetComponentInParent<MechaController>();

        if (joystickLeft != null) joyLRestRot = joystickLeft.localRotation;
        if (joystickRight != null) joyRRestRot = joystickRight.localRotation;
        if (gunnerHeadTransform != null) headRestRot = gunnerHeadTransform.localRotation;
    }

    private void Update()
    {
        if (mecha == null) return;

        barkCooldown -= Time.deltaTime;
        idleScanTimer += Time.deltaTime;

        bool isFiringGatling = mecha.IsGatlingFiring;
        bool isFiringMissiles = mecha.MissileCooldown > 3.0f;

        // 1. REACCIÓN DE ARMAS Y RETROCESO EN PALANCAS DE TIRO
        if (isFiringGatling)
        {
            // Vibración rápida por fuego de ametralladora
            joystickRecoilAngle = Mathf.Sin(Time.time * 48f) * 16f;

            if (tacticalScreenLight != null)
            {
                tacticalScreenLight.enabled = true;
                tacticalScreenLight.color = Color.yellow;
                tacticalScreenLight.intensity = 2.8f + Mathf.PingPong(Time.time * 20f, 1.5f);
            }

            if (barkCooldown <= 0f)
            {
                TriggerRandomBark(gatlingBarks, Color.yellow);
                barkCooldown = 2.2f;
            }
        }
        else if (isFiringMissiles)
        {
            // Palancas empujadas hacia adelante con fuerza al disparar misiles
            joystickRecoilAngle = 22f;

            if (tacticalScreenLight != null)
            {
                tacticalScreenLight.enabled = true;
                tacticalScreenLight.color = Color.cyan;
                tacticalScreenLight.intensity = 3.5f;
            }

            if (barkCooldown <= 0f)
            {
                TriggerRandomBark(missileBarks, Color.cyan);
                barkCooldown = 2.8f;
            }
        }
        else
        {
            // Estado de guardia: recuperación suave de retroceso y escaneo del horizonte
            joystickRecoilAngle = Mathf.Lerp(joystickRecoilAngle, 0f, Time.deltaTime * 10f);

            if (tacticalScreenLight != null)
            {
                tacticalScreenLight.enabled = true;
                tacticalScreenLight.color = new Color(0.2f, 0.8f, 1f);
                tacticalScreenLight.intensity = 1.2f;
            }

            // Escaneo sutil de cabeza buscando blancos
            if (gunnerHeadTransform != null)
            {
                float scanAngle = Mathf.Sin(idleScanTimer * 1.5f) * 14f;
                gunnerHeadTransform.localRotation = headRestRot * Quaternion.Euler(0f, scanAngle, 0f);
            }
        }

        // Aplicar rotación dinámica a los joysticks del maquinista
        if (joystickLeft != null)
        {
            joystickLeft.localRotation = joyLRestRot * Quaternion.Euler(-joystickRecoilAngle, 0f, 0f);
        }
        if (joystickRight != null)
        {
            joystickRight.localRotation = joyRRestRot * Quaternion.Euler(-joystickRecoilAngle, 0f, 0f);
        }

        // Aviso cómico en caso de vuelco del mecha
        if (mecha.IsOverturned && barkCooldown <= 0.2f)
        {
            FloatingText.Spawn(transform.position + Vector3.up * 1.8f, "¡ALARMA DE VUELCO! ¡DALE AL CULAZO, PILOTO!", Color.red, 0.36f);
            barkCooldown = 3.0f;
        }
    }

    private void TriggerRandomBark(string[] barks, Color color)
    {
        if (barks == null || barks.Length == 0) return;
        string bark = barks[Random.Range(0, barks.Length)];
        FloatingText.Spawn(transform.position + Vector3.up * 1.8f, bark, color, 0.36f);
    }
}
