using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Controlador del HUD de MechaQuack construido 100% con UI Toolkit (UXML y USS).
/// Gestiona la barra de vida dinámica, tiempo de recarga del láser, radar circular
/// en tiempo real con blips de enemigos, puntuación, destello de daño y modal de Game Over.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MechaHUD : MonoBehaviour
{
    public static MechaHUD Instance { get; private set; }

    [Header("Configuración Radar")]
    [SerializeField] private float maxRadarRange = 120f;

    private UIDocument uiDocument;
    private VisualElement root;

    // Elementos de la interfaz
    private VisualElement hpBarFill;
    private Label hpText;
    private Label laserStatusText;
    private Label scoreText;

    private VisualElement radarCircle;
    private readonly List<VisualElement> pooledBlips = new List<VisualElement>();

    private VisualElement damageOverlay;
    private Button btnCam;
    private Button btnZoom;
    private Button btnQuack;

    private VisualElement gameOverModal;
    private Label finalScoreText;
    private Button btnRestart;

    private float damageFlashAlpha = 0f;

    private void Awake()
    {
        Instance = this;
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        if (root == null) return;

        // Asegurar asignación de fuente SDF para renderizado correcto de todos los textos
        if (uiDocument.panelSettings != null && uiDocument.panelSettings.textSettings != null)
        {
            var defaultFont = uiDocument.panelSettings.textSettings.defaultFontAsset;
            if (defaultFont != null)
            {
                root.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromSDFFont(defaultFont));
            }
        }

        // Destello de daño
        damageOverlay = root.Q<VisualElement>("damage-overlay");

        // Panel HP y Láser
        hpBarFill = root.Q<VisualElement>("hp-bar-fill");
        hpText = root.Q<Label>("hp-text");
        laserStatusText = root.Q<Label>("laser-text");

        // Panel Radar y Score
        radarCircle = root.Q<VisualElement>("radar-circle");
        scoreText = root.Q<Label>("score-text");

        // Botones de acción
        btnZoom = root.Q<Button>("btn-zoom");
        btnCam = root.Q<Button>("btn-cam");
        btnQuack = root.Q<Button>("btn-quack");

        if (btnZoom != null) btnZoom.clicked += OnZoomOutClicked;
        if (btnCam != null) btnCam.clicked += OnToggleCameraClicked;
        if (btnQuack != null) btnQuack.clicked += OnQuackClicked;

        // Modal de Game Over
        gameOverModal = root.Q<VisualElement>("game-over-modal");
        finalScoreText = root.Q<Label>("final-score-text");
        btnRestart = root.Q<Button>("btn-restart");

        if (btnRestart != null) btnRestart.clicked += OnRestartClicked;
        if (gameOverModal != null) gameOverModal.style.display = DisplayStyle.None;
    }

    private void Update()
    {
        if (root == null)
        {
            InitializeUI();
            if (root == null) return;
        }

        // Si el juego está en Game Over, escuchar teclas rápidas para reiniciar
        if (MechaController.Instance != null && MechaController.Instance.IsGameOver)
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            bool restartRequested = false;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame ||
                    keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame)
                {
                    restartRequested = true;
                }
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                restartRequested = true;
            }

            if (restartRequested)
            {
                OnRestartClicked();
                return;
            }
        }

        UpdateMechaStats();
        UpdateRadar();
        UpdateDamageFlash();
    }

    private void UpdateMechaStats()
    {
        if (MechaController.Instance == null) return;

        float hpPercent = Mathf.Clamp01((float)MechaController.Instance.CurrentHp / MechaController.Instance.MaxHp);

        if (hpBarFill != null)
        {
            hpBarFill.style.width = Length.Percent(hpPercent * 100f);

            // Transición de color de vida
            if (hpPercent > 0.5f)
                hpBarFill.style.backgroundColor = new StyleColor(new Color(0.06f, 0.72f, 0.51f)); // Verde esmeralda
            else if (hpPercent > 0.25f)
                hpBarFill.style.backgroundColor = new StyleColor(new Color(0.96f, 0.62f, 0.04f)); // Ámbar
            else
                hpBarFill.style.backgroundColor = new StyleColor(new Color(0.94f, 0.25f, 0.37f)); // Rojo alerta
        }

        if (hpText != null)
        {
            hpText.text = $"{Mathf.CeilToInt(hpPercent * 100f)}%";
        }

        if (scoreText != null)
        {
            scoreText.text = MechaController.Instance.Score.ToString("D3");
        }

        if (laserStatusText != null)
        {
            float cd = MechaController.Instance.LaserCooldown;
            if (cd > 0f)
            {
                laserStatusText.text = $"LÁSER: {cd:F1}s";
                laserStatusText.style.color = new StyleColor(new Color(0.96f, 0.62f, 0.04f));
            }
            else
            {
                laserStatusText.text = "LÁSER: LISTO";
                laserStatusText.style.color = new StyleColor(new Color(0.22f, 0.74f, 0.97f));
            }
        }
    }

    private void UpdateRadar()
    {
        if (radarCircle == null || MechaController.Instance == null) return;

        var enemies = EnemySpawner.Instance != null ? EnemySpawner.Instance.ActiveEnemies : null;
        int enemyCount = enemies != null ? enemies.Count : 0;

        // Asegurar suficientes blips en el pool
        while (pooledBlips.Count < enemyCount)
        {
            VisualElement blip = new VisualElement();
            blip.AddToClassList("radar-blip");
            radarCircle.Add(blip);
            pooledBlips.Add(blip);
        }

        Transform mechaTr = MechaController.Instance.transform;
        Vector3 mechaPos = mechaTr.position;
        float mechaYawRad = mechaTr.eulerAngles.y * Mathf.Deg2Rad;
        float halfSize = 38f; // Radio del radar (76 / 2)

        for (int i = 0; i < pooledBlips.Count; i++)
        {
            var blip = pooledBlips[i];

            if (i < enemyCount && enemies[i] != null)
            {
                Vector3 relPos = enemies[i].transform.position - mechaPos;
                float angle = Mathf.Atan2(relPos.x, relPos.z) - mechaYawRad;
                float distNorm = Mathf.Clamp01(Mathf.Sqrt(relPos.x * relPos.x + relPos.z * relPos.z) / maxRadarRange);
                float radiusPx = distNorm * (halfSize - 4f);

                float bx = halfSize + Mathf.Sin(angle) * radiusPx - 2.5f;
                float by = halfSize - Mathf.Cos(angle) * radiusPx - 2.5f;

                blip.style.left = bx;
                blip.style.top = by;
                blip.style.display = DisplayStyle.Flex;
            }
            else
            {
                blip.style.display = DisplayStyle.None;
            }
        }
    }

    private void UpdateDamageFlash()
    {
        if (damageOverlay == null) return;

        if (damageFlashAlpha > 0f)
        {
            damageFlashAlpha -= Time.deltaTime * 3.5f;
            damageOverlay.style.opacity = Mathf.Clamp01(damageFlashAlpha);
        }
    }

    public void TriggerDamageFlash()
    {
        damageFlashAlpha = 1.0f;
        if (damageOverlay != null) damageOverlay.style.opacity = 1.0f;
    }

    public void UpdateCameraModeLabel(string modeName)
    {
        if (btnCam != null)
        {
            btnCam.text = $"MODO (C): {modeName}";
        }
    }

    public void ShowGameOver(int finalScore)
    {
        if (gameOverModal != null)
        {
            gameOverModal.style.display = DisplayStyle.Flex;
            if (finalScoreText != null)
            {
                finalScoreText.text = $"BOTS ELIMINADOS: {finalScore}";
            }
        }
    }

    public void HideGameOver()
    {
        if (gameOverModal != null)
        {
            gameOverModal.style.display = DisplayStyle.None;
        }
    }

    public void OnRestartClicked()
    {
        ProceduralAudio.Instance?.PlayButtonClick();
        MechaController.Instance?.RestartGame();
    }

    public void OnToggleCameraClicked()
    {
        CockpitCameraController.Instance?.ToggleMode();
    }

    public void OnZoomOutClicked()
    {
        if (CockpitCameraController.Instance != null)
        {
            float targetDist = CockpitCameraController.Instance.CurrentDistance > 12f ? 4.8f : 24.0f;
            CockpitCameraController.Instance.SetTargetZoom(targetDist);
        }
    }

    public void OnQuackClicked()
    {
        var duck = FindFirstObjectByType<DuckController>();
        if (duck != null)
        {
            ProceduralAudio.Instance?.PlayQuack();
            FloatingText.Spawn(duck.transform.position + Vector3.up * 1.6f, "¡CUAC!", Color.yellow, 0.32f);
        }
    }
}
