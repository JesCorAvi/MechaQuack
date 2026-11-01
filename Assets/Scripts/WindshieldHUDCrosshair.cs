using UnityEngine;

/// <summary>
/// Sistema holográfico profesional de mirilla (HUD Crosshair) proyectado sobre el parabrisas de MechaQuack.
/// Sustituye el antiguo cilindro azul por un HUD táctico de mecha militar con:
/// - Retícula central de precisión con marcas cardinales y chevron.
/// - 4 soportes de encuadre táctico (Targeting Brackets) con animación de enganche (Lock-On).
/// - Telémetro dinámico (Rangefinder) que mide la distancia real a enemigos o terreno.
/// - Horizonte artificial con indicadores de inclinación (pitch/roll).
/// - Indicadores de estado de armas en tiempo real: Ametralladora (Gatling) y Misiles.
/// - Expansión dinámica de la mirilla (Bloom / Recoil) al disparar.
/// </summary>
public class WindshieldHUDCrosshair : MonoBehaviour
{
    public static WindshieldHUDCrosshair Instance { get; private set; }

    [Header("Colores Holográficos")]
    [SerializeField] private Color normalColor = new Color(0.12f, 0.95f, 0.82f, 0.95f); // Cyan holográfico
    [SerializeField] private Color lockedColor = new Color(1.0f, 0.22f, 0.15f, 0.95f);  // Rojo alerta táctica
    [SerializeField] private Color firingColor = new Color(1.0f, 0.75f, 0.15f, 0.95f);  // Ámbar disparo

    [Header("Configuración de Escaneo")]
    [SerializeField] private float scanRange = 260f;
    [SerializeField] private float reticleRadius = 0.55f;

    // Elementos visuales
    private Transform reticleCenterRoot;
    private Transform bracketsRoot;
    private Transform horizonWingsRoot;

    private Transform bracketTL;
    private Transform bracketTR;
    private Transform bracketBL;
    private Transform bracketBR;

    private TextMesh telemetryTop;
    private TextMesh telemetryBottom;
    private TextMesh leftWeaponStatus;
    private TextMesh rightWeaponStatus;

    private Material hudMaterial;

    // Estado dinámico
    private float recoilBloom = 0f;
    private float recoilBloomVel = 0f;
    private float lockOnProgress = 0f;
    private bool hasTargetLock = false;
    private Transform currentLockedTarget;
    private float currentTargetDistance = 0f;

    public Transform CurrentLockedTarget => currentLockedTarget;
    public bool HasTargetLock => hasTargetLock;

    public void TriggerRecoil(float amount = 0.15f)
    {
        recoilBloom = Mathf.Clamp(recoilBloom + amount, 0f, 1.2f);
    }

    private void Awake()
    {
        Instance = this;
        BuildWindshieldCrosshair();
    }

    private void Start()
    {
        // Si existe el antiguo cilindro azul "Crosshair_Ring", destruirlo
        Transform oldBlueDot = transform.parent != null ? transform.parent.Find("Crosshair_Ring") : null;
        if (oldBlueDot == null) oldBlueDot = GameObject.Find("Crosshair_Ring")?.transform;
        if (oldBlueDot != null)
        {
            Destroy(oldBlueDot.gameObject);
        }
    }

    private void Update()
    {
        ScanForwardTargets();
        UpdateRecoilAndAnimation(Time.deltaTime);
        UpdateWeaponStatusReadouts();
        UpdateHorizonLadder();
    }

    /// <summary>
    /// Escanea el frente del Mecha para calcular el telémetro y detectar robots enemigos.
    /// </summary>
    private void ScanForwardTargets()
    {
        Vector3 origin = transform.position;
        Vector3 fwd = transform.forward;
        int mask = ~LayerMask.GetMask("Player", "Ignore Raycast");

        hasTargetLock = false;
        currentLockedTarget = null;
        currentTargetDistance = 0f;

        // Sondeo cónico hacia adelante para detectar enemigos en el punto de mira
        if (Physics.SphereCast(origin, reticleRadius * 1.5f, fwd, out RaycastHit sphereHit, scanRange, mask, QueryTriggerInteraction.Ignore))
        {
            var bot = sphereHit.collider.GetComponentInParent<EnemyBot>();
            if (bot != null)
            {
                hasTargetLock = true;
                currentLockedTarget = bot.transform;
                currentTargetDistance = sphereHit.distance;
            }
        }

        // Si no hay bot, medir distancia al terreno / edificios con raycast directo
        if (!hasTargetLock && Physics.Raycast(origin, fwd, out RaycastHit rayHit, scanRange, mask, QueryTriggerInteraction.Ignore))
        {
            currentTargetDistance = rayHit.distance;
        }

        // Animación suave de transición de Lock-On
        float targetLockRatio = hasTargetLock ? 1.0f : 0.0f;
        lockOnProgress = Mathf.MoveTowards(lockOnProgress, targetLockRatio, Time.deltaTime * 5f);

        // Color del HUD
        Color activeColor = Color.Lerp(normalColor, lockedColor, lockOnProgress);
        if (recoilBloom > 0.15f)
        {
            activeColor = Color.Lerp(activeColor, firingColor, Mathf.Clamp01(recoilBloom));
        }

        if (hudMaterial != null)
        {
            hudMaterial.color = activeColor;
            if (hudMaterial.HasProperty("_EmissionColor"))
            {
                hudMaterial.SetColor("_EmissionColor", activeColor * 1.8f);
            }
        }

        // Actualizar textos de telémetro
        if (telemetryTop != null)
        {
            if (hasTargetLock)
            {
                telemetryTop.text = "▲ [TARGET LOCK: BOT HOSTIL]";
                telemetryTop.color = lockedColor;
            }
            else
            {
                telemetryTop.text = "· [SISTEMA DE TIRO ACTIVO] ·";
                telemetryTop.color = activeColor;
            }
        }

        if (telemetryBottom != null)
        {
            if (currentTargetDistance > 0.1f)
                telemetryBottom.text = $"RNG: {currentTargetDistance:F1}m";
            else
                telemetryBottom.text = "RNG: ---";
            telemetryBottom.color = activeColor;
        }
    }

    private void UpdateRecoilAndAnimation(float dt)
    {
        // Amortiguación del bloom de retroceso
        recoilBloom = Mathf.SmoothDamp(recoilBloom, 0f, ref recoilBloomVel, 0.12f, Mathf.Infinity, dt);

        // Animar brackets tácticos:
        // Se contraen al fijar objetivo (Lock-On) y se expanden con el retroceso
        float baseBracketDist = Mathf.Lerp(0.68f, 0.45f, lockOnProgress) + recoilBloom * 0.25f;

        if (bracketTL != null) bracketTL.localPosition = new Vector3(-baseBracketDist, baseBracketDist * 0.75f, 0f);
        if (bracketTR != null) bracketTR.localPosition = new Vector3(baseBracketDist, baseBracketDist * 0.75f, 0f);
        if (bracketBL != null) bracketBL.localPosition = new Vector3(-baseBracketDist, -baseBracketDist * 0.75f, 0f);
        if (bracketBR != null) bracketBR.localPosition = new Vector3(baseBracketDist, -baseBracketDist * 0.75f, 0f);

        // Animar retícula central con el retroceso
        if (reticleCenterRoot != null)
        {
            float scale = 1.0f + recoilBloom * 0.35f;
            reticleCenterRoot.localScale = new Vector3(scale, scale, 1.0f);
        }
    }

    private void UpdateWeaponStatusReadouts()
    {
        if (MechaController.Instance == null) return;

        // Estado Ametralladora (Izquierda)
        if (leftWeaponStatus != null)
        {
            if (MechaController.Instance.IsGatlingFiring)
            {
                leftWeaponStatus.text = "[GATLING] ¡FUEGO!";
                leftWeaponStatus.color = firingColor;
            }
            else
            {
                leftWeaponStatus.text = "[GATLING] LISTA";
                leftWeaponStatus.color = normalColor;
            }
        }

        // Estado Misiles (Derecha)
        if (rightWeaponStatus != null)
        {
            float cd = MechaController.Instance.MissileCooldown;
            if (cd > 0f)
            {
                rightWeaponStatus.text = $"[MISILES] {cd:F1}s";
                rightWeaponStatus.color = new Color(0.96f, 0.62f, 0.04f);
            }
            else
            {
                rightWeaponStatus.text = "[MISILES] 4/4";
                rightWeaponStatus.color = normalColor;
            }
        }
    }

    private void UpdateHorizonLadder()
    {
        if (horizonWingsRoot == null || MechaController.Instance == null) return;

        // Horizonte artificial: contrarresta el balanceo (Roll) del Mecha para mantener línea de horizonte
        float roll = transform.eulerAngles.z;
        horizonWingsRoot.localRotation = Quaternion.Euler(0f, 0f, -roll);
    }

    /// <summary>
    /// Invoca una sacudida de retroceso táctico en la mirilla (Game-Feel).
    /// </summary>
    public void TriggerWeaponRecoil(float amount = 0.25f)
    {
        recoilBloom = Mathf.Clamp(recoilBloom + amount, 0f, 0.65f);
    }

    /// <summary>
    /// Construye proceduralmente la geometría y textos del HUD sobre el parabrisas.
    /// </summary>
    private void BuildWindshieldCrosshair()
    {
        // Material unlit emisivo transparente para HUD
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        hudMaterial = new Material(shader);
        hudMaterial.name = "M_WindshieldHUD";
        hudMaterial.color = normalColor;
        if (hudMaterial.HasProperty("_EmissionColor"))
        {
            hudMaterial.SetColor("_EmissionColor", normalColor * 2.0f);
            hudMaterial.EnableKeyword("_EMISSION");
        }

        // 1. RETÍCULA CENTRAL DE PRECISIÓN TÁCTICA (SIN DISCO SÓLIDO AZUL)
        reticleCenterRoot = new GameObject("Reticle_Center").transform;
        reticleCenterRoot.parent = transform;
        reticleCenterRoot.localPosition = Vector3.zero;
        reticleCenterRoot.localRotation = Quaternion.identity;

        // Punto diminuto de precisión en el centro
        CreateLineBar(reticleCenterRoot, Vector3.zero, new Vector3(0.035f, 0.035f, 0.01f), hudMaterial);

        // 4 Marcas cardinales con hueco central abierto para visión clara
        float tickOffset = 0.18f;
        float tickLen = 0.18f;
        float tickThick = 0.016f;
        // Norte
        CreateLineBar(reticleCenterRoot, new Vector3(0f, tickOffset + tickLen * 0.5f, 0f), new Vector3(tickThick, tickLen, 0.01f), hudMaterial);
        // Sur
        CreateLineBar(reticleCenterRoot, new Vector3(0f, -(tickOffset + tickLen * 0.5f), 0f), new Vector3(tickThick, tickLen, 0.01f), hudMaterial);
        // Este
        CreateLineBar(reticleCenterRoot, new Vector3(tickOffset + tickLen * 0.5f, 0f, 0f), new Vector3(tickLen, tickThick, 0.01f), hudMaterial);
        // Oeste
        CreateLineBar(reticleCenterRoot, new Vector3(-(tickOffset + tickLen * 0.5f), 0f, 0f), new Vector3(tickLen, tickThick, 0.01f), hudMaterial);

        // 2. SOPORTES TÁCTICOS DE ESQUINA (TARGETING BRACKETS: ⌜ ⌝ ⌞ ⌟)
        bracketsRoot = new GameObject("Targeting_Brackets").transform;
        bracketsRoot.parent = transform;
        bracketsRoot.localPosition = Vector3.zero;
        bracketsRoot.localRotation = Quaternion.identity;

        bracketTL = CreateBracket(bracketsRoot, "Bracket_TL", true, true);
        bracketTR = CreateBracket(bracketsRoot, "Bracket_TR", false, true);
        bracketBL = CreateBracket(bracketsRoot, "Bracket_BL", true, false);
        bracketBR = CreateBracket(bracketsRoot, "Bracket_BR", false, false);

        // 3. HORIZONTE ARTIFICIAL (ALAS DE NIVELACIÓN)
        horizonWingsRoot = new GameObject("Artificial_Horizon").transform;
        horizonWingsRoot.parent = transform;
        horizonWingsRoot.localPosition = Vector3.zero;
        horizonWingsRoot.localRotation = Quaternion.identity;

        // Ala izquierda
        CreateLineBar(horizonWingsRoot, new Vector3(-1.35f, 0f, 0f), new Vector3(0.35f, 0.014f, 0.01f), hudMaterial);
        CreateLineBar(horizonWingsRoot, new Vector3(-1.52f, -0.05f, 0f), new Vector3(0.014f, 0.10f, 0.01f), hudMaterial);
        // Ala derecha
        CreateLineBar(horizonWingsRoot, new Vector3(1.35f, 0f, 0f), new Vector3(0.35f, 0.014f, 0.01f), hudMaterial);
        CreateLineBar(horizonWingsRoot, new Vector3(1.52f, -0.05f, 0f), new Vector3(0.014f, 0.10f, 0.01f), hudMaterial);

        // 4. TEXTOS DIGITALES DEL HUD (TIPOGRAFÍA NÍTIDA Y NO SOLAPADA)
        telemetryTop = CreateHUDText("Telemetry_Top", transform, new Vector3(0f, 0.88f, 0f), TextAnchor.MiddleCenter, 24, 0.048f);
        telemetryBottom = CreateHUDText("Telemetry_Bottom", transform, new Vector3(0f, -0.65f, 0f), TextAnchor.MiddleCenter, 24, 0.052f);

        leftWeaponStatus = CreateHUDText("Weapon_Left", transform, new Vector3(-1.45f, -0.65f, 0f), TextAnchor.MiddleRight, 22, 0.044f);
        rightWeaponStatus = CreateHUDText("Weapon_Right", transform, new Vector3(1.45f, -0.65f, 0f), TextAnchor.MiddleLeft, 22, 0.044f);
    }

    private Transform CreateBracket(Transform parent, string bName, bool isLeft, bool isTop)
    {
        GameObject go = new GameObject(bName);
        go.transform.parent = parent;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        float barLength = 0.28f;
        float barThick = 0.024f;

        float signX = isLeft ? 1f : -1f;
        float signY = isTop ? -1f : 1f;

        // Barra horizontal
        CreateLineBar(go.transform, new Vector3(signX * barLength * 0.5f, 0f, 0f), new Vector3(barLength, barThick, 0.01f), hudMaterial);
        // Barra vertical
        CreateLineBar(go.transform, new Vector3(0f, signY * barLength * 0.5f, 0f), new Vector3(barThick, barLength, 0.01f), hudMaterial);

        return go.transform;
    }

    private GameObject CreateLineBar(Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = "HUD_Bar";
        bar.transform.parent = parent;
        bar.transform.localPosition = localPos;
        bar.transform.localRotation = Quaternion.identity;
        bar.transform.localScale = localScale;
        bar.GetComponent<MeshRenderer>().material = mat;
        Destroy(bar.GetComponent<Collider>());
        return bar;
    }

    private TextMesh CreateHUDText(string objName, Transform parent, Vector3 localPos, TextAnchor anchor, int fontSize, float charSize)
    {
        GameObject textObj = new GameObject(objName);
        textObj.transform.parent = parent;
        textObj.transform.localPosition = localPos;
        textObj.transform.localRotation = Quaternion.identity;

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = "";
        tm.fontSize = fontSize;
        tm.characterSize = charSize;
        tm.anchor = anchor;
        tm.alignment = anchor == TextAnchor.MiddleLeft ? TextAlignment.Left : (anchor == TextAnchor.MiddleRight ? TextAlignment.Right : TextAlignment.Center);
        tm.fontStyle = FontStyle.Bold;
        tm.color = normalColor;

        return tm;
    }
}
