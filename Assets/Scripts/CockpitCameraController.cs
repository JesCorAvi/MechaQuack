using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] private float currentDistance = 7.5f;
    [SerializeField] private float targetDistance = 7.5f;
    [SerializeField] private float minDistance = 2.8f;
    [SerializeField] private float maxDistance = 160.0f; // Permite ver todo el Mecha exterior colosal
    [SerializeField] private float mouseSensitivity = 2.4f;
    [SerializeField] private float zoomStep = 4.5f;
    [SerializeField] private float zoomSmoothing = 14f;

    [Header("Modo Actual")]
    public CameraMode currentMode = CameraMode.FreeOrbit;

    private float yaw = 0f;    // Ángulo relativo a la cabina (0 = mirando hacia el frente de la cabina)
    private float pitch = 22f;  // Ángulo vertical en grados
    private Vector3 droneWorldPos;
    private float droneYaw = 0f;
    private float dronePitch = 15f;

    public float CurrentDistance => currentDistance;

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

        yaw = 0f;
        pitch = 24f;
        currentDistance = 7.5f;
        targetDistance = 7.5f;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

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
            Vector2 delta = mouse.delta.ReadValue();

            if (isDragging && delta.sqrMagnitude > 0.01f)
            {
                // Si estaba en modo fija de cabina, cambiar automáticamente a órbita libre
                if (currentMode == CameraMode.FixedCockpit)
                {
                    currentMode = CameraMode.FreeOrbit;
                    MechaHUD.Instance?.UpdateCameraModeLabel(GetModeLabel());
                }

                if (currentMode == CameraMode.SpectatorDrone)
                {
                    droneYaw += delta.x * mouseSensitivity * 0.1f;
                    dronePitch = Mathf.Clamp(dronePitch - delta.y * mouseSensitivity * 0.1f, -80f, 80f);
                }
                else
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

        // Escala dinámica: más veloz a mayor distancia para no tener que dar mil vueltas a la rueda
        float dynamicSpeed = zoomStep * (1f + (targetDistance / maxDistance) * 1.5f);
        targetDistance = Mathf.Clamp(targetDistance - (scrollUnits * dynamicSpeed), minDistance, maxDistance);
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
        float horizDist = currentDistance * Mathf.Cos(pitch * Mathf.Deg2Rad);
        float vertDist = currentDistance * Mathf.Sin(pitch * Mathf.Deg2Rad);

        // Yaw relativo al frente de la cabina
        float radYaw = yaw * Mathf.Deg2Rad;
        Vector3 camLocalPos = new Vector3(
            pivotLocal.x - Mathf.Sin(radYaw) * horizDist,
            Mathf.Max(0.5f, pivotLocal.y + vertDist),
            pivotLocal.z - Mathf.Cos(radYaw) * horizDist
        );

        // Cuando la cámara se aleja mucho, eleva el punto de mira hacia el centro del mecha
        float zoomProgress = Mathf.Clamp01((currentDistance - minDistance) / (maxDistance - minDistance));
        Vector3 lookTargetLocal = pivotLocal + Vector3.up * Mathf.Lerp(0.15f, 1.2f, zoomProgress);

        Vector3 worldCamPos = cockpitTr != null ? cockpitTr.TransformPoint(camLocalPos) : camLocalPos;
        Vector3 worldLookTarget = cockpitTr != null ? cockpitTr.TransformPoint(lookTargetLocal) : lookTargetLocal;

        // Anti-recorte de cámara (Camera Collision Push-In según camera-systems)
        // Evita que al alejar la cámara se incruste dentro de rocas, pilares o rascacielos
        Vector3 camToTarget = worldCamPos - worldLookTarget;
        float desiredDist = camToTarget.magnitude;
        if (desiredDist > 0.05f)
        {
            Vector3 camDir = camToTarget / desiredDist;
            int groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast");
            if (Physics.SphereCast(worldLookTarget, 0.28f, camDir, out RaycastHit camHit, desiredDist, groundMask, QueryTriggerInteraction.Ignore))
            {
                if (mechaRoot == null || !camHit.collider.transform.IsChildOf(mechaRoot))
                {
                    float safeDist = Mathf.Max(minDistance, camHit.distance - 0.15f);
                    worldCamPos = worldLookTarget + camDir * safeDist;
                }
            }
        }

        // Suavizado exponencial independiente de la tasa de refresco (Pattern 2 de camera-systems)
        float smoothT = 1f - Mathf.Exp(-24f * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, worldCamPos, smoothT);
        transform.LookAt(worldLookTarget);
    }

    private void UpdateSpectatorDrone()
    {
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
        Transform cockpitTr = duckTarget.parent ?? mechaRoot;

        Vector3 cockpitCamOffset = cockpitTr.TransformPoint(new Vector3(0f, 2.8f, -4.2f));
        Vector3 lookTarget = cockpitTr.TransformPoint(new Vector3(0f, 1.2f, 3.5f));

        transform.position = Vector3.Lerp(transform.position, cockpitCamOffset, Time.deltaTime * 14f);
        transform.LookAt(lookTarget);
    }

    public void ToggleMode()
    {
        currentMode = (CameraMode)(((int)currentMode + 1) % 3);

        if (currentMode == CameraMode.SpectatorDrone)
        {
            droneWorldPos = transform.position + transform.forward * -2f + Vector3.up * 4f;
            droneYaw = transform.eulerAngles.y;
            dronePitch = transform.eulerAngles.x;
        }

        MechaHUD.Instance?.UpdateCameraModeLabel(GetModeLabel());
    }

    public void SetTargetZoom(float dist)
    {
        targetDistance = Mathf.Clamp(dist, minDistance, maxDistance);
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
        pitch = 24f;
        targetDistance = 4.8f;
        currentDistance = 4.8f;
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
}
