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
    private static MechaHUD _instance;
    public static MechaHUD Instance
    {
        get
        {
            if (_instance == null) _instance = Object.FindFirstObjectByType<MechaHUD>();
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Configuración Radar")]
    [SerializeField] private float maxRadarRange = 120f;

    [Header("Fuente de la Interfaz")]
    [SerializeField] private UnityEngine.TextCore.Text.FontAsset hudFontAsset;

    private UIDocument uiDocument;
    private VisualElement root;

    // Elementos de la interfaz
    private VisualElement hpBarFill;
    private Label hpText;
    private Label missileStatusText;
    private Label scoreText;
    private Label spawnerStatusText;
    private Label floorIndicatorText;
    private int lastFloorLevel = -1;

    public UIDocument UIDocumentComponent => uiDocument;

    private VisualElement radarCircle;
    private readonly List<VisualElement> pooledBlips = new List<VisualElement>();

    private VisualElement damageOverlay;
    private Button btnCam;
    private Button btnZoom;
    private Button btnQuack;
    private Button btnSpawner;
    private Button btnCursor;

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
        ApplyFontRecursively(root);

        // Destello de daño
        damageOverlay = root.Q<VisualElement>("damage-overlay");

        // Panel HP y Armas
        hpBarFill = root.Q<VisualElement>("hp-bar-fill");
        hpText = root.Q<Label>("hp-text");
        missileStatusText = root.Q<Label>("missile-text") ?? root.Q<Label>("laser-text");
        spawnerStatusText = root.Q<Label>("spawner-status-text");
        floorIndicatorText = root.Q<Label>("floor-indicator-text");
        lastFloorLevel = -1;

        // Panel Radar y Score
        radarCircle = root.Q<VisualElement>("radar-circle");
        scoreText = root.Q<Label>("score-text");

        // Botones de acción
        btnCursor = root.Q<Button>("btn-cursor");
        btnZoom = root.Q<Button>("btn-zoom");
        btnCam = root.Q<Button>("btn-cam");
        btnQuack = root.Q<Button>("btn-quack");
        btnSpawner = root.Q<Button>("btn-spawner");

        if (btnCursor != null) btnCursor.clicked += OnToggleCursorClicked;
        if (btnZoom != null) btnZoom.clicked += OnZoomOutClicked;
        if (btnCam != null) btnCam.clicked += OnToggleCameraClicked;
        if (btnQuack != null) btnQuack.clicked += OnQuackClicked;
        if (btnSpawner != null) btnSpawner.clicked += OnToggleSpawnerClicked;

        // Sincronizar estado inicial de ratón
        if (CockpitCameraController.Instance != null)
        {
            UpdateCursorLockStatus(CockpitCameraController.Instance.IsCursorLocked);
        }

        // Modal de Game Over
        gameOverModal = root.Q<VisualElement>("game-over-modal");
        finalScoreText = root.Q<Label>("final-score-text");
        btnRestart = root.Q<Button>("btn-restart");

        if (btnRestart != null) btnRestart.clicked += OnRestartClicked;
        if (gameOverModal != null) gameOverModal.style.display = DisplayStyle.None;

        if (EnemySpawner.Instance != null)
        {
            UpdateSpawnerStatus(EnemySpawner.Instance.IsSpawningEnabled);
        }
    }

    public void ApplyFontRecursively(VisualElement el)
    {
        if (el == null) return;

        UnityEngine.TextCore.Text.FontAsset fontAsset = hudFontAsset;
        if (fontAsset == null && uiDocument != null && uiDocument.panelSettings != null && uiDocument.panelSettings.textSettings != null)
        {
            fontAsset = uiDocument.panelSettings.textSettings.defaultFontAsset;
        }

        if (fontAsset != null)
        {
            ApplyFontToTree(el, fontAsset);
        }
    }

    private void ApplyFontToTree(VisualElement el, UnityEngine.TextCore.Text.FontAsset fontAsset)
    {
        if (el == null) return;
        if (el is Label || el is Button || el is TextElement)
        {
            el.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromSDFFont(fontAsset));
        }
        for (int i = 0; i < el.childCount; i++)
        {
            ApplyFontToTree(el[i], fontAsset);
        }
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

        if (missileStatusText != null)
        {
            float cd = MechaController.Instance.MissileCooldown;
            if (cd > 0f)
            {
                missileStatusText.text = $"MISILES: {cd:F1}s";
                missileStatusText.style.color = new StyleColor(new Color(0.96f, 0.62f, 0.04f));
            }
            else
            {
                missileStatusText.text = "MISILES: [4/4] LISTOS";
                missileStatusText.style.color = new StyleColor(new Color(0.22f, 0.74f, 0.97f));
            }
        }

        // Actualizar indicador de planta / estación en tiempo real (0 allocations cuando no cambia)
        if (floorIndicatorText != null && DuckController.Instance != null)
        {
            int floorLevel = DuckController.Instance.CurrentFloorLevel;
            string floorName = DuckController.Instance.CurrentFloorName;
            if (floorLevel != lastFloorLevel || floorIndicatorText.text != floorName)
            {
                lastFloorLevel = floorLevel;
                floorIndicatorText.text = floorName;
                floorIndicatorText.RemoveFromClassList("floor-level-1");
                floorIndicatorText.RemoveFromClassList("floor-level-2");
                floorIndicatorText.RemoveFromClassList("floor-level-3");
                floorIndicatorText.AddToClassList($"floor-level-{floorLevel}");
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

    public void UpdateCursorLockStatus(bool isLocked)
    {
        if (btnCursor != null)
        {
            btnCursor.text = isLocked ? "🔒 RATÓN: ANCLADO (ALT/ESC)" : "🔓 RATÓN: LIBRE (CLIC/ALT)";
            if (isLocked)
            {
                btnCursor.RemoveFromClassList("btn-cursor-unlocked");
                btnCursor.AddToClassList("btn-cursor-locked");
            }
            else
            {
                btnCursor.RemoveFromClassList("btn-cursor-locked");
                btnCursor.AddToClassList("btn-cursor-unlocked");
            }
        }
    }

    private void OnToggleCursorClicked()
    {
        CockpitCameraController.Instance?.ToggleCursorLock();
    }

    public void ShowGameOver(int finalScore)
    {
        if (gameOverModal != null)
        {
            ApplyFontRecursively(gameOverModal);
            gameOverModal.style.display = DisplayStyle.Flex;
            if (finalScoreText != null)
            {
                finalScoreText.text = $"BOTS ELIMINADOS: {finalScore}";
            }
            CockpitCameraController.Instance?.SetCursorLock(false);
        }
    }

    public void HideGameOver()
    {
        if (gameOverModal != null)
        {
            gameOverModal.style.display = DisplayStyle.None;
        }
    }

    public void UpdateSpawnerStatus(bool isSpawning)
    {
        if (spawnerStatusText != null)
        {
            if (isSpawning)
            {
                spawnerStatusText.text = "🚨 INVASIÓN ACTIVA";
                spawnerStatusText.style.color = new StyleColor(new Color(0.95f, 0.25f, 0.37f));
                spawnerStatusText.style.backgroundColor = new StyleColor(new Color(0.95f, 0.25f, 0.37f, 0.2f));
                StyleColor bCol = new StyleColor(new Color(0.95f, 0.25f, 0.37f, 0.6f));
                spawnerStatusText.style.borderTopColor = bCol;
                spawnerStatusText.style.borderBottomColor = bCol;
                spawnerStatusText.style.borderLeftColor = bCol;
                spawnerStatusText.style.borderRightColor = bCol;
            }
            else
            {
                spawnerStatusText.text = "🛡️ EXPLORACIÓN SEGURA";
                spawnerStatusText.style.color = new StyleColor(new Color(0.06f, 0.72f, 0.51f));
                spawnerStatusText.style.backgroundColor = new StyleColor(new Color(0.06f, 0.72f, 0.51f, 0.15f));
                StyleColor bCol = new StyleColor(new Color(0.06f, 0.72f, 0.51f, 0.4f));
                spawnerStatusText.style.borderTopColor = bCol;
                spawnerStatusText.style.borderBottomColor = bCol;
                spawnerStatusText.style.borderLeftColor = bCol;
                spawnerStatusText.style.borderRightColor = bCol;
            }
        }

        if (btnSpawner != null)
        {
            btnSpawner.text = isSpawning ? "🚨 INVASIÓN: ACTIVA (PULSAR PARA DETENER)" : "⚔️ ACTIVAR INVASIÓN ROBOT";
            btnSpawner.style.color = isSpawning ? new StyleColor(new Color(1f, 0.4f, 0.4f)) : new StyleColor(Color.white);
        }
    }

    public void OnToggleSpawnerClicked()
    {
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.ToggleSpawning();
        }
    }

    public void OnRestartClicked()
    {
        ProceduralAudio.Instance?.PlayButtonClick();
        HideGameOver();
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
            if (!CockpitCameraController.Instance.IsCaptainExteriorUnlocked)
            {
                var duck = DuckController.Instance;
                if (duck != null)
                {
                    FloatingText.Spawn(duck.transform.position + Vector3.up * 1.5f, "⚠️ ENTRA EN EL CÍRCULO DEL CAPITÁN PARA VISTA EXTERIOR", Color.yellow, 0.4f);
                }
                CockpitCameraController.Instance.SetTargetZoom(5.2f);
            }
            else
            {
                float targetDist = CockpitCameraController.Instance.CurrentDistance > 12f ? 5.2f : 32.0f;
                CockpitCameraController.Instance.SetTargetZoom(targetDist);
            }
        }
    }

    public void OnQuackClicked()
    {
        var duck = FindFirstObjectByType<DuckController>();
        if (duck != null)
        {
            duck.PerformQuack();
        }
        else
        {
            ProceduralAudio.Instance?.PlayQuack();
        }
    }
}
