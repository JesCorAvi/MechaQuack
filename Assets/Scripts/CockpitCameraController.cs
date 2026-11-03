using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

/// <summary>
/// Sistema de cámara libre y orbital relativo a la cabina del Mecha.
/// Permite zoom ultra fluido con la rueda del ratón (desde el pato en la cabina
/// hasta alejar la vista para contemplar el Mecha gigante completo en el exterior).
/// </summary>
public class CockpitCameraController : MonoBehaviour
{
    public static CockpitCameraController Instance { get; private set; }

    public enum CameraMode { FreeOrbit, SpectatorDrone, FixedCockpit }

    [Header("Objetivo")]
    [SerializeField] private Transform duckTarget;
    [SerializeField] private Transform mechaRoot;

    [Header("Configuración Órbita Libre (Modo 0)")]
    [SerializeField] private float currentDistance = 5.6f;
    [SerializeField] private float targetDistance = 5.6f;
    [SerializeField] private float minDistance = 0.85f;
    [SerializeField] private float maxDistance = 160.0f; // Permite ver todo el Mecha exterior colosal
    [SerializeField] private float maxInteriorDistance = 6.2f; // Zoom interior máximo en Niveles 1 y 2
    [SerializeField] private float captainExteriorDistance = 55.0f; // Zoom exterior automático al entrar en la cabeza del Mecha
    [SerializeField] private float mouseSensitivity = 2.4f;
    [SerializeField] private float zoomStep = 4.5f;
    [SerializeField] private float zoomSmoothing = 14f;

    [Header("Control y Anclaje de Cursor")]
    [SerializeField] private bool lockCursorOnStart = true;
    private bool isCursorLocked = false;

    [Header("Modo Actual")]
    public CameraMode currentMode = CameraMode.FreeOrbit;

    private float yaw = 0f;    // Ángulo relativo a la cabina (0 = mirando hacia el frente de la cabina)
    private float pitch = 22f;  // Ángulo vertical en grados
    private Vector3 droneWorldPos;
    private float droneYaw = 0f;
    private float dronePitch = 15f;
    private int currentFloor = 1;
    private int previousFloor = 1;

    [Header("Zona de Mando del Capitán (Círculo Planta 3)")]
    [Tooltip("Centro local del círculo de mando del Capitán en coordenadas de cabina")]
    [SerializeField] private Vector3 captainCircleCenter = new Vector3(0f, 9.2f, 1.5f);
    [Tooltip("Radio de detección nominal para entrar al círculo de mando")]
    [SerializeField] private float captainCircleRadius = 2.2f;
    [Tooltip("Radio de histéresis para salir del círculo (evita oscilaciones rápidas en el borde)")]
    [SerializeField] private float captainCircleExitRadius = 2.55f;
    [SerializeField] private bool isInCaptainCircle = false;
    private bool wasInCaptainCircle = false;

    // Buffer preasignado para evitar GC allocations en SphereCast
    private readonly RaycastHit[] camObstacleHits = new RaycastHit[16];

    public float CurrentDistance => currentDistance;
    public float TargetDistance => targetDistance;
    public bool IsCursorLocked => isCursorLocked;
    public int CurrentFloor => currentFloor;
    public bool IsInCaptainCircle => isInCaptainCircle;
    public bool IsCaptainExteriorUnlocked => isInCaptainCircle;
    public Vector3 CaptainCircleCenter => captainCircleCenter;
    public float CaptainCircleRadius => captainCircleRadius;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (duckTarget == null)
        {
            var duck = FindFirstObjectByType<DuckController>();
            if (duck != null) duckTarget = duck.transform;
        }

        if (mechaRoot == null)
        {
            var mecha = FindFirstObjectByType<MechaController>();
            if (mecha != null) mechaRoot = mecha.transform;
        }

        Camera cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            cam.nearClipPlane = 0.05f; // Evita recorte de polígonos al empujar la cámara cerca del pato o mamparas
        }

        minDistance = 0.85f;
        yaw = 0f;
        pitch = 22f;
        currentDistance = 5.6f;
        targetDistance = 5.6f;

        // Auto-sincronización con el pedestal del capitán si está presente en la jerarquía
        if (CaptainCommandPedestal.Instance != null)
        {
            captainCircleCenter = CaptainCommandPedestal.Instance.transform.localPosition;
            captainCircleRadius = CaptainCommandPedestal.Instance.Radius;
            captainCircleExitRadius = CaptainCommandPedestal.Instance.ExitRadius;
        }
        else if (mechaRoot != null)
        {
            var allTransforms = mechaRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allTransforms.Length; i++)
            {
                if (allTransforms[i] != null && allTransforms[i].name == "Captain_Command_Pedestal")
                {
                    captainCircleCenter = allTransforms[i].localPosition;
                    float r = allTransforms[i].localScale.x * 0.5f;
                    if (r > 0.5f)
                    {
                        captainCircleRadius = r;
                        captainCircleExitRadius = r + 0.35f;
                    }
                    break;
                }
            }
        }

        if (lockCursorOnStart)
        {
            SetCursorLock(true);
        }
    }

    public void SetCursorLock(bool locked)
    {
        isCursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        MechaHUD.Instance?.UpdateCursorLockStatus(locked);
    }

    public void ToggleCursorLock()
    {
        SetCursorLock(!isCursorLocked);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && isCursorLocked)
        {
            SetCursorLock(false);
        }
    }

    private bool IsPointerOverUI()
    {
        if (MechaHUD.Instance != null && MechaHUD.Instance.UIDocumentComponent != null)
        {
            var rootEl = MechaHUD.Instance.UIDocumentComponent.rootVisualElement;
            if (rootEl != null && rootEl.panel != null && Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                Vector2 panelPos = new Vector2(mousePos.x, Screen.height - mousePos.y);
                VisualElement picked = rootEl.panel.Pick(panelPos);
                if (picked != null && picked != rootEl && picked.name != "damage-overlay" && picked.name != "hud-root")
                {
                    VisualElement curr = picked;
                    while (curr != null && curr != rootEl)
                    {
                        if (curr is Button) return true;
                        curr = curr.parent;
                    }
                }
            }
        }
        return false;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        // 1. Detectar planta del pato para aplicar reglas de navegación y HUD
        int detectedFloor = currentFloor;
        if (duckTarget != null)
        {
            float dy = duckTarget.localPosition.y;
            if (dy < 3.0f) detectedFloor = 1;
            else if (dy < 8.4f) detectedFloor = 2; // Calibrado a 8.4m para que las escaleras no activen P3 a mitad de trayecto
            else detectedFloor = 3;
        }

        if (detectedFloor != currentFloor)
        {
            previousFloor = currentFloor;
            currentFloor = detectedFloor;
        }

        // 2. Detección física estricta del Círculo Marcado del Capitán (Planta 3)
        // La cámara exterior en 3ª persona NO se activa por pisar la planta, sino ÚNICAMENTE al entrar en el círculo marcado
        bool insideCircle = false;
        if (duckTarget != null)
        {
            Vector3 dLocal = duckTarget.localPosition;
            if (CaptainCommandPedestal.Instance != null)
            {
                insideCircle = CaptainCommandPedestal.Instance.CheckDuckInside(dLocal, isInCaptainCircle);
            }
            else
            {
                // Verificación analítica robusta con histéresis anti-rebote
                bool atCaptainHeight = dLocal.y >= 8.4f && dLocal.y <= 15.0f;
                if (atCaptainHeight)
                {
                    float dx = dLocal.x - captainCircleCenter.x;
                    float dz = dLocal.z - captainCircleCenter.z;
                    float distXZ = Mathf.Sqrt(dx * dx + dz * dz);
                    float threshold = isInCaptainCircle ? captainCircleExitRadius : captainCircleRadius;
                    insideCircle = distXZ <= threshold;
                }
            }
        }

        isInCaptainCircle = insideCircle;

        // Transición reactiva exclusiva de entrada/salida del círculo de mando
        if (isInCaptainCircle != wasInCaptainCircle)
        {
            wasInCaptainCircle = isInCaptainCircle;

            if (isInCaptainCircle)
            {
                // ¡ENTRADA AL CÍRCULO MARCADO DEL CAPITÁN!
                // Activar automáticamente la cámara exterior en 3ª persona del titán de 50 metros
                targetDistance = captainExteriorDistance;
                pitch = 18f;
                if (duckTarget != null)
                {
                    FloatingText.Spawn(duckTarget.position + Vector3.up * 1.5f, "👑 PUESTO DEL CAPITÁN: ¡3ª PERSONA ACTIVADA!", Color.cyan, 0.45f);
                }
                ProceduralAudio.Instance?.PlayButtonClick();
            }
            else
            {
                // ¡SALIDA DEL CÍRCULO MARCADO!
                // Replegar inmediatamente la cámara al interior estricto de la cabina
                targetDistance = maxInteriorDistance;
                if (currentMode == CameraMode.SpectatorDrone)
                {
                    currentMode = CameraMode.FreeOrbit;
                    MechaHUD.Instance?.UpdateCameraModeLabel(GetModeLabel());
                }
                if (duckTarget != null)
                {
                    FloatingText.Spawn(duckTarget.position + Vector3.up * 1.5f, "🛡️ CABINA INTERIOR: VISTA REPLEGADA", Color.white, 0.35f);
                }
            }
        }

        // REGLA ESTRICTA: Fuera del círculo del capitán, la vista exterior permanece estrictamente bloqueada
        if (!isInCaptainCircle)
        {
            if (targetDistance > maxInteriorDistance)
            {
                targetDistance = maxInteriorDistance;
            }
            if (currentMode == CameraMode.SpectatorDrone)
            {
                currentMode = CameraMode.FreeOrbit;
                MechaHUD.Instance?.UpdateCameraModeLabel(GetModeLabel());
            }
        }

        // Alternar anclaje del ratón con Alt, Escape o Tab
        if (keyboard != null && (keyboard.leftAltKey.wasPressedThisFrame ||
                                 keyboard.rightAltKey.wasPressedThisFrame ||
                                 keyboard.escapeKey.wasPressedThisFrame ||
                                 keyboard.tabKey.wasPressedThisFrame))
        {
            ToggleCursorLock();
        }

        // Si el ratón está liberado y el jugador hace clic izquierdo en el juego (fuera de la UI), re-anclar
        if (!isCursorLocked && mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            if (!IsPointerOverUI())
            {
                SetCursorLock(true);
            }
        }

        // Alternar modo con tecla C
        if (keyboard != null && keyboard.cKey.wasPressedThisFrame)
        {
            ToggleMode();
        }

        // Zoom por teclado opcional (+ / - o RePág / AvPág)
        if (keyboard != null)
        {
            if (keyboard.equalsKey.isPressed || keyboard.numpadPlusKey.isPressed || keyboard.pageUpKey.isPressed)
            {
                ApplyZoomDelta(1.5f * Time.deltaTime * 30f);
            }
            if (keyboard.minusKey.isPressed || keyboard.numpadMinusKey.isPressed || keyboard.pageDownKey.isPressed)
            {
                ApplyZoomDelta(-1.5f * Time.deltaTime * 30f);
            }
        }

        // Ratón: Rotación 360º y Rueda de Zoom
        if (mouse != null)
        {
            bool isDragging = mouse.leftButton.isPressed || mouse.rightButton.isPressed;
            bool shouldRotate = isCursorLocked || isDragging;
            Vector2 delta = mouse.delta.ReadValue();

            if (shouldRotate && delta.sqrMagnitude > 0.001f)
            {
                // Si estaba en modo fija de cabina, cambiar automáticamente a órbita libre al arrastrar
                if (currentMode == CameraMode.FixedCockpit && isDragging)
                {
                    currentMode = CameraMode.FreeOrbit;
                    MechaHUD.Instance?.UpdateCameraModeLabel(GetModeLabel());
                }

                if (currentMode == CameraMode.SpectatorDrone && IsCaptainExteriorUnlocked)
                {
                    droneYaw += delta.x * mouseSensitivity * 0.1f;
                    dronePitch = Mathf.Clamp(dronePitch - delta.y * mouseSensitivity * 0.1f, -80f, 80f);
                }
                else if (currentMode == CameraMode.FreeOrbit)
                {
                    yaw += delta.x * mouseSensitivity * 0.15f;
                    pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity * 0.15f, -10f, 75f);
                }
            }

            // Lectura robusta de la RUEDA DEL RATÓN
            float scrollY = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) > 0.001f)
            {
                // Maneja tanto ticks grandes (Windows +/-120) como ticks normalizados (+/-1 o touchpad)
                float normalizedScroll = Mathf.Abs(scrollY) >= 10f ? (scrollY / 120f) : scrollY;
                ApplyZoomDelta(normalizedScroll);
            }
        }

        // Suavizado dinámico del zoom independiente de los FPS
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, 1f - Mathf.Exp(-zoomSmoothing * Time.deltaTime));
    }

    /// <summary>
    /// Aplica variación de zoom. Valores positivos acercan (zoom in), negativos alejan (zoom out).
    /// </summary>
    public void ApplyZoomDelta(float scrollUnits)
    {
        if (currentMode == CameraMode.FixedCockpit)
        {
            currentMode = CameraMode.FreeOrbit;
            MechaHUD.Instance?.UpdateCameraModeLabel(GetModeLabel());
        }

        float effectiveMaxDist = IsCaptainExteriorUnlocked ? maxDistance : maxInteriorDistance;

        // Escala dinámica: más veloz a mayor distancia para no tener que dar mil vueltas a la rueda
        float dynamicSpeed = zoomStep * (1f + (targetDistance / maxDistance) * 1.5f);
        targetDistance = Mathf.Clamp(targetDistance - (scrollUnits * dynamicSpeed), minDistance, effectiveMaxDist);
    }

    private void LateUpdate()
    {
        if (duckTarget == null) return;

        switch (currentMode)
        {
            case CameraMode.FreeOrbit:
                UpdateFreeOrbit();
                break;
            case CameraMode.SpectatorDrone:
                UpdateSpectatorDrone();
                break;
            case CameraMode.FixedCockpit:
                UpdateFixedCockpit();
                break;
        }
    }

    private void UpdateFreeOrbit()
    {
        Transform cockpitTr = duckTarget.parent;

        Vector3 pivotLocal = duckTarget.localPosition + Vector3.up * 0.85f;
        float cosP = Mathf.Cos(pitch * Mathf.Deg2Rad);
        float sinP = Mathf.Sin(pitch * Mathf.Deg2Rad);
        float radYaw = yaw * Mathf.Deg2Rad;

        // Vector unitario que apunta desde el pivote hacia la posición deseada de la cámara
        Vector3 dirLocal = new Vector3(
            -Mathf.Sin(radYaw) * cosP,
            sinP,
            -Mathf.Cos(radYaw) * cosP
        ).normalized;

        // --- SISTEMA ANTI-RECORTE Y EMPUJE DE CÁMARA MULTINIVEL ---
        // Delimita los planos de suelo y techo analíticos según la planta en la que se encuentra el pato
        float boxMinY, boxMaxY;
        if (pivotLocal.y < 3.8f)
        {
            // Planta 1: Conductor
            boxMinY = 0.45f;
            boxMaxY = 4.30f;
        }
        else if (pivotLocal.y < 8.2f)
        {
            // Planta 2: Artillero
            boxMinY = 4.70f;
            boxMaxY = 8.75f;
        }
        else
        {
            // Planta 3: Capitán
            boxMinY = 9.20f;
            boxMaxY = 14.10f; // Calibrado para no recortar con el techo de la cabeza (14.5m)
        }

        float maxInteriorDist = 12.0f;

        if (dirLocal.x > 0.0001f)
        {
            float d = (9.6f - pivotLocal.x) / dirLocal.x;
            if (d > 0f && d < maxInteriorDist) maxInteriorDist = d;
        }
        else if (dirLocal.x < -0.0001f)
        {
            float d = (-9.6f - pivotLocal.x) / dirLocal.x;
            if (d > 0f && d < maxInteriorDist) maxInteriorDist = d;
        }

        if (dirLocal.y > 0.0001f)
        {
            float d = (boxMaxY - pivotLocal.y) / dirLocal.y;
            if (d > 0f && d < maxInteriorDist) maxInteriorDist = d;
        }
        else if (dirLocal.y < -0.0001f)
        {
            float d = (boxMinY - pivotLocal.y) / dirLocal.y;
            if (d > 0f && d < maxInteriorDist) maxInteriorDist = d;
        }

        if (dirLocal.z > 0.0001f)
        {
            float d = (7.6f - pivotLocal.z) / dirLocal.z;
            if (d > 0f && d < maxInteriorDist) maxInteriorDist = d;
        }
        else if (dirLocal.z < -0.0001f)
        {
            float d = (-6.8f - pivotLocal.z) / dirLocal.z;
            if (d > 0f && d < maxInteriorDist) maxInteriorDist = d;
        }

        // Margen de seguridad de 0.20m para evitar que el plano cercano toque paredes
        maxInteriorDist = Mathf.Max(minDistance, maxInteriorDist - 0.20f);

        Vector3 camLocalPos;
        Vector3 lookTargetLocal;

        // REGLA ESTRICTA DE CÁMARA:
        // En Plantas 1 y 2, y en Planta 3 FUERA del círculo de mando, la vista está estrictamente restringida al interior.
        // Únicamente cuando el pato entra al CÍRCULO MARCADO del Capitán, la vista exterior en 3ª persona se activa.
        if (!IsCaptainExteriorUnlocked || targetDistance <= maxInteriorDistance)
        {
            // VISTA INTERIOR CABINA
            float safeCockpitDist = Mathf.Min(currentDistance, maxInteriorDist);
            camLocalPos = pivotLocal + dirLocal * safeCockpitDist;
            camLocalPos.y = Mathf.Clamp(camLocalPos.y, boxMinY, boxMaxY);
            lookTargetLocal = pivotLocal + Vector3.up * 0.12f;
        }
        else
        {
            // VISTA EXTERIOR MECHA EN 3ª PERSONA (Exclusiva de la Cabeza del Mecha en Planta 3)
            float zoomProgress = Mathf.Clamp01((currentDistance - maxInteriorDistance) / (maxDistance - maxInteriorDistance));
            Vector3 mechaCenterLocal = new Vector3(0f, 6.0f, 1.0f); // Centro superior de masa / torso del Mecha de 50m
            lookTargetLocal = Vector3.Lerp(pivotLocal + Vector3.up * 0.12f, mechaCenterLocal, Mathf.SmoothStep(0f, 1f, zoomProgress));
            camLocalPos = lookTargetLocal + dirLocal * currentDistance;
        }

        Vector3 worldCamPos = cockpitTr != null ? cockpitTr.TransformPoint(camLocalPos) : camLocalPos;
        Vector3 worldLookTarget = cockpitTr != null ? cockpitTr.TransformPoint(lookTargetLocal) : lookTargetLocal;

        // 2. Colisión exterior con obstáculos del entorno (rascacielos, montañas, peñascos, puentes gigantes):
        // Usa SphereCastNonAlloc filtrando la estructura del propio Mecha y del Pato sin GC allocations.
        Vector3 camRay = worldCamPos - worldLookTarget;
        float camRayDist = camRay.magnitude;
        if (camRayDist > 0.1f)
        {
            Vector3 camRayDir = camRay / camRayDist;
            int groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast");
            int hitCount = Physics.SphereCastNonAlloc(worldLookTarget, 0.32f, camRayDir, camObstacleHits, camRayDist, groundMask, QueryTriggerInteraction.Ignore);

            float minHitDist = camRayDist;
            bool hitObstacle = false;

            for (int i = 0; i < hitCount; i++)
            {
                var h = camObstacleHits[i];
                if (h.collider == null) continue;
                if (mechaRoot != null && h.collider.transform.IsChildOf(mechaRoot)) continue;
                if (duckTarget != null && h.collider.transform.IsChildOf(duckTarget)) continue;

                if (h.distance < minHitDist && h.distance > 1.2f)
                {
                    minHitDist = h.distance;
                    hitObstacle = true;
                }
            }

            if (hitObstacle)
            {
                float safeExteriorDist = Mathf.Max(minDistance, minHitDist - 0.35f);
                worldCamPos = worldLookTarget + camRayDir * safeExteriorDist;
            }
        }

        // Suavizado exponencial independiente de la tasa de refresco (Pattern 2 de camera-systems)
        float smoothT = 1f - Mathf.Exp(-28f * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, worldCamPos, smoothT);
        transform.LookAt(worldLookTarget);
    }

    private void UpdateSpectatorDrone()
    {
        if (!IsCaptainExteriorUnlocked)
        {
            currentMode = CameraMode.FreeOrbit;
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float flySpeed = 18f * Time.deltaTime;
            Vector3 fwd = transform.forward;
            Vector3 right = transform.right;

            if (keyboard.upArrowKey.isPressed) droneWorldPos += fwd * flySpeed;
            if (keyboard.downArrowKey.isPressed) droneWorldPos -= fwd * flySpeed;
            if (keyboard.rightArrowKey.isPressed) droneWorldPos += right * flySpeed;
            if (keyboard.leftArrowKey.isPressed) droneWorldPos -= right * flySpeed;
        }

        transform.position = droneWorldPos;
        transform.rotation = Quaternion.Euler(dronePitch, droneYaw, 0f);
    }

    private void UpdateFixedCockpit()
    {
        if (mechaRoot == null) return;
        Transform cockpitTr = duckTarget != null && duckTarget.parent != null ? duckTarget.parent : mechaRoot;

        float floorBaseY = currentFloor == 1 ? 0f : (currentFloor == 2 ? 4.5f : 9.0f);
        Vector3 cockpitCamOffset = cockpitTr.TransformPoint(new Vector3(0f, floorBaseY + 2.8f, -4.2f));
        Vector3 lookTarget = cockpitTr.TransformPoint(new Vector3(0f, floorBaseY + 1.2f, 3.5f));

        transform.position = Vector3.Lerp(transform.position, cockpitCamOffset, Time.deltaTime * 14f);
        transform.LookAt(lookTarget);
    }

    public void ToggleMode()
    {
        if (!IsCaptainExteriorUnlocked)
        {
            // Fuera del círculo del capitán, sólo se puede alternar entre Órbita Libre y Fija de Cabina (Dron bloqueado)
            currentMode = currentMode == CameraMode.FreeOrbit ? CameraMode.FixedCockpit : CameraMode.FreeOrbit;
            if (duckTarget != null)
            {
                FloatingText.Spawn(duckTarget.position + Vector3.up * 1.4f, "⚠️ 3ª PERSONA / DRON: EXCLUSIVO DEL CÍRCULO DEL CAPITÁN", Color.yellow, 0.35f);
            }
        }
        else
        {
            currentMode = (CameraMode)(((int)currentMode + 1) % 3);

            if (currentMode == CameraMode.SpectatorDrone)
            {
                droneWorldPos = transform.position + transform.forward * -2f + Vector3.up * 4f;
                droneYaw = transform.eulerAngles.y;
                dronePitch = transform.eulerAngles.x;
            }
        }

        MechaHUD.Instance?.UpdateCameraModeLabel(GetModeLabel());
    }

    public void SetTargetZoom(float dist)
    {
        float maxD = IsCaptainExteriorUnlocked ? maxDistance : maxInteriorDistance;
        targetDistance = Mathf.Clamp(dist, minDistance, maxD);
        if (currentMode == CameraMode.FixedCockpit)
        {
            currentMode = CameraMode.FreeOrbit;
            MechaHUD.Instance?.UpdateCameraModeLabel(GetModeLabel());
        }
    }

    public void ResetCamera()
    {
        currentMode = CameraMode.FreeOrbit;
        yaw = 0f;
        pitch = 22f;
        targetDistance = 5.6f;
        currentDistance = 5.6f;
        MechaHUD.Instance?.UpdateCameraModeLabel(GetModeLabel());
    }

    public string GetModeLabel()
    {
        switch (currentMode)
        {
            case CameraMode.FreeOrbit: return "ÓRBITA LIBRE 360º";
            case CameraMode.SpectatorDrone: return "DRON VUELO (FLECHAS)";
            case CameraMode.FixedCockpit: return "FIJA CABINA";
            default: return "ÓRBITA LIBRE";
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform tr = duckTarget != null && duckTarget.parent != null ? duckTarget.parent : transform;
        if (tr != null)
        {
            Vector3 worldCenter = tr.TransformPoint(captainCircleCenter);
            Gizmos.color = isInCaptainCircle ? Color.cyan : new Color(0f, 1f, 1f, 0.35f);
            Gizmos.DrawWireSphere(worldCenter, captainCircleRadius);
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.25f);
            Gizmos.DrawWireSphere(worldCenter, captainCircleExitRadius);
        }
    }
}
