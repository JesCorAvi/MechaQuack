using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador del pato en el espacio local de la cabina.
/// Al simular en coordenadas locales de la cabina, se eliminan completamente
/// los conflictos de físicas (PhysX parent fighting) y el pato nunca se atasca.
/// </summary>
public class DuckController : MonoBehaviour
{
    public static DuckController Instance { get; private set; }

    [Header("Límites de la Cabina Multinivel")]
    [SerializeField] private float cockpitHalfX = 10.2f;
    [SerializeField] private float cockpitHalfZ = 7.8f;
    [SerializeField] private float cockpitMaxY = 16.0f;
    [SerializeField] private float duckRadius = 0.5f;

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 14.0f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float friction = 0.88f; // Resbaladizo cómico
    [SerializeField] private float rotationSpeed = 16f;

    [Header("Salto y Culazo")]
    [SerializeField] private float jumpForce = 15.5f;
    [SerializeField] private float stompSpeed = 32f;
    [SerializeField] private float gravity = 36f;
    [SerializeField] private float stepSnapOffset = 0.85f; // Absorción suave de escalones al correr

    [Header("Inclinación de Cabina (Tolerancia y Deslizamiento)")]
    [Tooltip("Ángulo de inclinación en grados por debajo del cual el pato no resbala (tolerancia / agarre de patas de goma)")]
    [SerializeField] private float tiltToleranceAngle = 14f;
    [Tooltip("Aceleración máxima de deslizamiento por pendiente (siempre inferior a la aceleración de 42 m/s² del pato para poder contraatacar)")]
    [SerializeField] private float maxTiltSlideAccel = 18f;

    [Header("Animación Cómica")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private Transform leftWingTransform;
    [SerializeField] private Transform rightWingTransform;
    [SerializeField] private Transform dropShadowTransform;
    [SerializeField] private float waddleFrequency = 14f;
    [SerializeField] private float waddleAngle = 14f;

    // Estado local en la cabina
    private Vector3 localPos;
    private Vector3 localVel;
    private float localYaw = 0f;
    private bool isGrounded = true;
    private bool isStomping = false;
    private float currentFloorY = 0.5f;

    // Buffer preasignado para detección de suelo sin GC allocations
    private readonly RaycastHit[] groundHits = new RaycastHit[8];

    private float waddleTimer = 0f;
    private float wingFlapTimer = 0f;
    private Vector3 initialVisualScale = Vector3.one;
    private float squashTimer = 0f;

    private readonly string[] quackPhrases = new string[]
    {
        "¡CUAC!",
        "¡QUACK!",
        "¡HONK!",
        "¡CUAAAC! 🦆",
        "¡PATO-PODER!",
        "¡PATO AL MANDO!"
    };

    public bool IsGrounded => isGrounded;
    public bool IsStomping => isStomping;
    public Vector3 LocalPosition => localPos;

    /// <summary>
    /// Nivel actual del pato dentro de la cabina de 3 pisos:
    /// 1 = Planta Conductor (Y < 3.0m)
    /// 2 = Planta Artillero (3.0m <= Y < 7.5m)
    /// 3 = Planta Capitán / Puente de Mando (Y >= 7.5m)
    /// </summary>
    public int CurrentFloorLevel
    {
        get
        {
            if (localPos.y < 3.0f) return 1;
            if (localPos.y < 7.5f) return 2;
            return 3;
        }
    }

    public string CurrentFloorName
    {
        get
        {
            switch (CurrentFloorLevel)
            {
                case 1: return "PLANTA 1: CONDUCTOR [Búnker Blindado - Guiado por Capitán]";
                case 2: return "PLANTA 2: ARTILLERO [Ventanales Panorámicos - Puntería Despejada]";
                case 3: return "PLANTA 3: CABEZA DEL MECHA [Capitán - 3ª Persona Automática]";
                default: return "PLANTA 1: CONDUCTOR";
            }
        }
    }

    private void Awake()
    {
        Instance = this;

        // Si tenía Rigidbody o Colliders que causen conflictos, asegurarlos
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // El pato se simula en espacio local de cabina
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        if (visualTransform == null)
        {
            visualTransform = transform.Find("Visual") ?? (transform.childCount > 0 ? transform.GetChild(0) : transform);
        }

        if (visualTransform != null)
        {
            initialVisualScale = visualTransform.localScale;
            if (leftWingTransform == null) leftWingTransform = visualTransform.Find("Duck_Wing_L");
            if (rightWingTransform == null) rightWingTransform = visualTransform.Find("Duck_Wing_R");
        }

        localPos = transform.localPosition;
        localYaw = transform.localEulerAngles.y;
    }

    private void Update()
    {
        if (MechaController.Instance != null && MechaController.Instance.IsGameOver)
        {
            localVel = Vector3.zero;
            return;
        }

        ReadInput();
        UpdatePhysics(Time.deltaTime);
        CheckButtons();
        UpdateVisuals();
        UpdateDropShadow();

        // Aplicar posición y rotación local en la cabina
        transform.localPosition = localPos;
        transform.localRotation = Quaternion.Euler(0f, localYaw, 0f);
    }

    private void ReadInput()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null) return;

        float h = 0f;
        float v = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;

        Vector2 inputDir = new Vector2(h, v).normalized;

        // Calcular dirección según hacia dónde mira la cámara en el plano horizontal de la cabina
        Transform parentCockpit = transform.parent;
        Vector3 moveDirLocal = Vector3.zero;

        if (inputDir.sqrMagnitude > 0.01f)
        {
            Camera cam = Camera.main;
            Vector3 camFwd = cam != null ? cam.transform.forward : Vector3.forward;
            Vector3 camRight = cam != null ? cam.transform.right : Vector3.right;

            if (parentCockpit != null)
            {
                camFwd = parentCockpit.InverseTransformDirection(camFwd);
                camRight = parentCockpit.InverseTransformDirection(camRight);
            }

            camFwd.y = 0f;
            if (camFwd.sqrMagnitude > 0.001f) camFwd.Normalize(); else camFwd = Vector3.forward;

            camRight.y = 0f;
            if (camRight.sqrMagnitude > 0.001f) camRight.Normalize(); else camRight = Vector3.right;

            moveDirLocal = (camFwd * inputDir.y + camRight * inputDir.x).normalized;
        }

        // Aceleración con inercia resbaladiza
        localVel.x += moveDirLocal.x * acceleration * Time.deltaTime;
        localVel.z += moveDirLocal.z * acceleration * Time.deltaTime;

        Vector2 horiz = Vector2.ClampMagnitude(new Vector2(localVel.x, localVel.z), moveSpeed);
        localVel.x = horiz.x;
        localVel.z = horiz.y;

        // Rotar hacia la dirección de avance
        if (moveDirLocal.sqrMagnitude > 0.05f)
        {
            float targetAngle = Mathf.Atan2(moveDirLocal.x, moveDirLocal.z) * Mathf.Rad2Deg;
            localYaw = Mathf.LerpAngle(localYaw, targetAngle, Time.deltaTime * rotationSpeed);
        }

        // Salto (Espacio)
        if (keyboard.spaceKey.wasPressedThisFrame && isGrounded && !isStomping)
        {
            PerformJump();
        }

        bool mouseAllowed = CockpitCameraController.Instance == null || CockpitCameraController.Instance.IsCursorLocked;

        // Culazo / Pisotón (E / Shift / Clic Izq en el aire)
        bool stompKey = keyboard.eKey.wasPressedThisFrame ||
                        keyboard.leftShiftKey.wasPressedThisFrame ||
                        keyboard.rightShiftKey.wasPressedThisFrame ||
                        (mouse != null && mouseAllowed && mouse.leftButton.wasPressedThisFrame && !isGrounded);

        if (stompKey)
        {
            if (!isGrounded && !isStomping)
            {
                PerformStomp();
            }
            else if (isGrounded)
            {
                PerformQuack();
            }
        }

        // Graznido (Q / Clic Der)
        bool quackKey = keyboard.qKey.wasPressedThisFrame ||
                        (mouse != null && mouseAllowed && mouse.rightButton.wasPressedThisFrame);

        if (quackKey)
        {
            PerformQuack();
        }
    }

    private void UpdatePhysics(float dt)
    {
        // 1. Efecto físico de pendiente con tolerancia (el pato no resbala en inclinaciones leves)
        Transform parentCockpit = transform.parent;
        Vector3 localDown = parentCockpit != null ? parentCockpit.InverseTransformDirection(Vector3.down) : Vector3.down;
        Vector2 slopeHorizontal = new Vector2(localDown.x, localDown.z);
        float sinTilt = Mathf.Clamp01(slopeHorizontal.magnitude);
        float tiltAngleDeg = Mathf.Asin(sinTilt) * Mathf.Rad2Deg;

        // Solo deslizar si supera la tolerancia de inclinación y está en el suelo
        if (tiltAngleDeg > tiltToleranceAngle && isGrounded)
        {
            float excessAngle = tiltAngleDeg - tiltToleranceAngle;
            float tiltFactor = Mathf.Clamp01(excessAngle / 28f); // Escala progresiva
            Vector2 slopeDir = slopeHorizontal.normalized;
            Vector2 slideAccel = slopeDir * (tiltFactor * maxTiltSlideAccel);

            localVel.x += slideAccel.x * dt;
            localVel.z += slideAccel.y * dt;
        }

        // Fricción resbaladiza
        localVel.x *= Mathf.Pow(friction, dt * 60f);
        localVel.z *= Mathf.Pow(friction, dt * 60f);

        // Frenado horizontal al caer en picada
        if (isStomping)
        {
            localVel.x *= Mathf.Pow(0.1f, dt * 60f);
            localVel.z *= Mathf.Pow(0.1f, dt * 60f);
        }

        // Integrar movimiento horizontal
        localPos.x += localVel.x * dt;
        localPos.z += localVel.z * dt;

        // 2. Detección de suelo multinivel robusta (Planta 1, Planta 2, Planta 3 y Escaleras)
        float detectedFloorY = 0f;
        bool foundFloor = false;

        if (parentCockpit != null)
        {
            // Lanzar raycast vertical desde una altura suficiente (localPos.y + 1.2m)
            // para que el origen NUNCA quede dentro de un escalón o rampa ascendente
            Vector3 worldOrigin = parentCockpit.TransformPoint(new Vector3(localPos.x, localPos.y + 1.2f, localPos.z));
            Vector3 worldDown = parentCockpit.TransformDirection(Vector3.down);
            int hitCount = Physics.RaycastNonAlloc(worldOrigin, worldDown, groundHits, 24.0f, ~LayerMask.GetMask("Player", "Ignore Raycast"), QueryTriggerInteraction.Ignore);

            float highestLocalY = -100f;
            for (int i = 0; i < hitCount; i++)
            {
                var h = groundHits[i];
                if (h.collider == null) continue;
                // Exclusividad: Solo colisionar con superficies dentro de la cabina
                if (!h.collider.transform.IsChildOf(parentCockpit)) continue;
                if (h.collider.transform.IsChildOf(transform)) continue;

                Vector3 localHitPoint = parentCockpit.InverseTransformPoint(h.point);
                // Considerar impactos en o por debajo del pato (con holgura de escalón stepSnapOffset)
                if (localHitPoint.y <= localPos.y + stepSnapOffset + 0.35f && localHitPoint.y > highestLocalY)
                {
                    highestLocalY = localHitPoint.y;
                    foundFloor = true;
                }
            }

            if (foundFloor)
            {
                detectedFloorY = highestLocalY;
            }
            else
            {
                // Fallback seguro por plantas para evitar caídas catastróficas al vacío si un raycast falla
                if (localPos.y >= 7.0f) detectedFloorY = 9.0f;
                else if (localPos.y >= 2.5f) detectedFloorY = 4.5f;
                else detectedFloorY = 0.0f;
            }
        }

        if (detectedFloorY < 0f)
        {
            detectedFloorY = 0f;
        }

        currentFloorY = detectedFloorY + duckRadius;

        // 3. Adhesión al suelo / Escalones suaves (Step Snapping bidireccional a 14 m/s)
        if (isGrounded && !isStomping)
        {
            float diff = localPos.y - currentFloorY;
            // Si el pato sube un peldaño/rampa (diff < 0) o desciende suavemente (diff <= stepSnapOffset)
            if (diff >= -stepSnapOffset && diff <= stepSnapOffset)
            {
                localPos.y = currentFloorY;
                localVel.y = 0f;
            }
            else if (diff > stepSnapOffset)
            {
                // El pato ha dejado de tocar suelo (salto voluntario o caída libre por el hueco)
                isGrounded = false;
            }
            else if (diff < -stepSnapOffset)
            {
                // El pato se encuentra por debajo de una rampa que sube rápido: ajustar a la superficie
                localPos.y = currentFloorY;
                localVel.y = 0f;
            }
        }

        // 4. Gravedad e integración vertical cuando está en el aire
        if (!isGrounded)
        {
            localVel.y -= gravity * dt;
            localPos.y += localVel.y * dt;

            if (localPos.y <= currentFloorY)
            {
                bool landedFromStomp = isStomping;
                localPos.y = currentFloorY;
                localVel.y = 0f;
                isGrounded = true;
                isStomping = false;

                if (landedFromStomp)
                {
                    OnStompLanded();
                }
            }
        }

        // Límites de las paredes de la cabina multinivel
        if (localPos.x < -cockpitHalfX) { localPos.x = -cockpitHalfX; localVel.x *= -0.3f; }
        if (localPos.x >  cockpitHalfX) { localPos.x =  cockpitHalfX; localVel.x *= -0.3f; }
        if (localPos.z < -cockpitHalfZ) { localPos.z = -cockpitHalfZ; localVel.z *= -0.3f; }
        if (localPos.z >  cockpitHalfZ) { localPos.z =  cockpitHalfZ; localVel.z *= -0.3f; }
        if (localPos.y >  cockpitMaxY)  { localPos.y =  cockpitMaxY;  localVel.y = 0f; }
        if (localPos.y <  0.5f)         { localPos.y =  0.5f;          localVel.y = 0f; isGrounded = true; }
    }

    private void CheckButtons()
    {
        if (MechaController.Instance == null) return;

        var buttons = MechaController.Instance.GetComponentsInChildren<CockpitFloorButton>();
        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (btn == null) continue;

            float distX = localPos.x - btn.transform.localPosition.x;
            float distZ = localPos.z - btn.transform.localPosition.z;
            float distHoriz = Mathf.Sqrt(distX * distX + distZ * distZ);

            // Verificación estricta de altura para no activar botones de pisos superiores o inferiores
            float distY = Mathf.Abs(localPos.y - (btn.transform.localPosition.y + 0.5f));

            float activationRadius = btn.detectionRadius > 0.1f ? btn.detectionRadius : 1.6f;

            if (distHoriz <= activationRadius && distY < 1.6f)
            {
                if (isGrounded && !isStomping)
                {
                    btn.OnDuckStanding(this);
                }
            }
            else
            {
                btn.OnDuckExit();
            }
        }
    }

    public void PerformJump()
    {
        localVel.y = jumpForce;
        isGrounded = false;
        wingFlapTimer = 0.45f;
        TriggerSquash(0.75f, 1.35f, 0.25f);
        ProceduralAudio.Instance?.PlayJump();
        Debug.Log("[GAME] Pato saltó en la cabina");

        if (MechaController.Instance != null && MechaController.Instance.IsOverturned)
        {
            MechaController.Instance.RecoverFromTumble();
        }
    }

    public void PerformStomp()
    {
        isStomping = true;
        isGrounded = false;
        localVel.y = -stompSpeed;
        localVel.x *= 0.2f;
        localVel.z *= 0.2f;
        wingFlapTimer = 0.35f;
        TriggerSquash(0.7f, 1.45f, 0.35f);
        Debug.Log("[GAME] ¡Culazo de pato iniciado en el aire!");
    }

    private void OnStompLanded()
    {
        TriggerSquash(1.45f, 0.45f, 0.35f);
        ProceduralAudio.Instance?.PlayStomp();
        Debug.Log($"[GAME] Culazo de pato aterrizó en ({localPos.x:F2}, {localPos.z:F2})");

        // Comprobar si cayó sobre un botón de armas
        CockpitFloorButton hitBtn = null;
        var buttons = MechaController.Instance != null ? MechaController.Instance.GetComponentsInChildren<CockpitFloorButton>() : null;
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                if (btn == null) continue;

                float distX = localPos.x - btn.transform.localPosition.x;
                float distZ = localPos.z - btn.transform.localPosition.z;
                float distHoriz = Mathf.Sqrt(distX * distX + distZ * distZ);
                float distY = Mathf.Abs(localPos.y - (btn.transform.localPosition.y + 0.5f));
                float stompRadius = btn.detectionRadius > 0.1f ? btn.detectionRadius * 1.35f : 2.2f;

                if (distHoriz <= stompRadius && distY < 1.8f)
                {
                    hitBtn = btn;
                    break;
                }
            }
        }

        if (hitBtn != null)
        {
            hitBtn.OnDuckStomp(this);
            if (MechaController.Instance != null && (hitBtn.buttonId == "GATLING" || hitBtn.buttonId == "MISSILES" || hitBtn.buttonId == "PUNCH" || hitBtn.buttonId == "LASER"))
            {
                MechaController.Instance.TriggerWeaponAction(hitBtn.buttonId, hitBtn.transform.position);
            }
        }
        else
        {
            FloatingText.Spawn(transform.position + Vector3.up * 1.0f, "¡SLAM!", Color.white, 0.32f);
        }

        VoxelExplosion.Spawn(transform.position + Vector3.up * 0.1f, new Color(1f, 0.9f, 0.2f), 10, 0.28f, 6f);
    }

    public void PerformQuack()
    {
        wingFlapTimer = 0.45f;
        if (isGrounded) localVel.y = 3.5f; // Pequeño brinquito
        isGrounded = false;

        TriggerSquash(1.25f, 0.8f, 0.2f);
        ProceduralAudio.Instance?.PlayQuack();
        Debug.Log("[GAME] ¡CUAC! Graznido cómico emitido");

        string phrase = quackPhrases[Random.Range(0, quackPhrases.Length)];
        FloatingText.Spawn(transform.position + Vector3.up * 1.6f, phrase, new Color(1f, 0.9f, 0.15f), 0.32f);
    }

    public void TeleportToLocal(Vector3 targetLocalPos)
    {
        localPos = targetLocalPos;
        localVel = Vector3.zero;
        transform.localPosition = localPos;
        isGrounded = true;
        isStomping = false;
    }

    public void ForceTriggerStompAt(Vector3 targetLocalPos)
    {
        TeleportToLocal(new Vector3(targetLocalPos.x, targetLocalPos.y + 2.5f, targetLocalPos.z));
        PerformStomp();
    }

    public void ApplyExternalNudge(Vector3 velocityImpulse)
    {
        localVel += velocityImpulse;
        Vector2 clamped = Vector2.ClampMagnitude(new Vector2(localVel.x, localVel.z), moveSpeed * 1.6f);
        localVel.x = clamped.x;
        localVel.z = clamped.y;
        if (velocityImpulse.y > 1f) isGrounded = false;
    }

    public void ResetPosition(Vector3 newLocalPos)
    {
        localPos = newLocalPos;
        localVel = Vector3.zero;
        isGrounded = true;
        isStomping = false;
    }

    private void TriggerSquash(float horizontalScale, float verticalScale, float duration)
    {
        squashTimer = duration;
        if (visualTransform != null)
        {
            visualTransform.localScale = new Vector3(
                initialVisualScale.x * horizontalScale,
                initialVisualScale.y * verticalScale,
                initialVisualScale.z * horizontalScale
            );
        }
    }

    private void UpdateVisuals()
    {
        if (visualTransform == null) return;

        if (squashTimer > 0f)
        {
            squashTimer -= Time.deltaTime;
            visualTransform.localScale = Vector3.Lerp(visualTransform.localScale, initialVisualScale, Time.deltaTime * 12f);
        }
        else
        {
            visualTransform.localScale = initialVisualScale;
        }

        // Waddle al caminar
        float speed = Mathf.Sqrt(localVel.x * localVel.x + localVel.z * localVel.z);
        if (isGrounded && speed > 0.6f)
        {
            waddleTimer += Time.deltaTime * waddleFrequency;
            float roll = Mathf.Sin(waddleTimer) * waddleAngle;
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, roll);
        }
        else
        {
            visualTransform.localRotation = Quaternion.Slerp(visualTransform.localRotation, Quaternion.identity, Time.deltaTime * 10f);
        }

        // Aleteo de alas
        if (wingFlapTimer > 0f || !isGrounded)
        {
            if (wingFlapTimer > 0f) wingFlapTimer -= Time.deltaTime;
            float flap = Mathf.Sin(Time.time * 42f) * 45f;
            if (leftWingTransform != null) leftWingTransform.localRotation = Quaternion.Euler(0f, 0f, 20f + flap);
            if (rightWingTransform != null) rightWingTransform.localRotation = Quaternion.Euler(0f, 0f, -20f - flap);
        }
        else
        {
            if (leftWingTransform != null) leftWingTransform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            if (rightWingTransform != null) rightWingTransform.localRotation = Quaternion.Euler(0f, 0f, -20f);
        }
    }

    private void UpdateDropShadow()
    {
        if (dropShadowTransform == null) return;

        float heightAboveFloor = Mathf.Max(0f, localPos.y - currentFloorY);
        dropShadowTransform.localPosition = new Vector3(0f, 0.01f - heightAboveFloor, 0f);
        float scale = Mathf.Clamp(1.0f - heightAboveFloor * 0.12f, 0.35f, 1.0f);
        dropShadowTransform.localScale = new Vector3(scale, 0.005f, scale);
    }
}
