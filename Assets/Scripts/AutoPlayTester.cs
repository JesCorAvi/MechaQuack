using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Simulador y validador automatizado en modo Play.
/// Simula los inputs del jugador sobre los botones de la cabina y verifica
/// matemáticamente la respuesta de las mecánicas (avance, retroceso, giros,
/// armas, cooldowns, invasión de robots, vuelco/pataleta y UI Toolkit).
/// </summary>
public class AutoPlayTester : MonoBehaviour
{
    public static AutoPlayTester Instance { get; private set; }

    [Header("Configuración del Validador")]
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private float stepDuration = 0.95f;

    private int passedTests = 0;
    private int totalTests = 0;
    private bool isTesting = false;

    public bool IsTesting => isTesting;
    public int PassedTests => passedTests;
    public int TotalTests => totalTests;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (runOnStart)
        {
            StartCoroutine(RunFullTestSuiteRoutine());
        }
    }

    public void RunManualTests()
    {
        if (!isTesting)
        {
            StartCoroutine(RunFullTestSuiteRoutine());
        }
    }

    private IEnumerator RunFullTestSuiteRoutine()
    {
        isTesting = true;
        passedTests = 0;
        totalTests = 0;

        // Esperar a que la escena, físicas y HUD inicialicen
        yield return new WaitForSeconds(0.6f);

        Debug.Log("==========================================================================");
        Debug.Log("[AUTOTEST] >>> INICIANDO BATERÍA DE PRUEBAS AUTOMATIZADAS - MECHAQUACK <<<");
        Debug.Log("==========================================================================");

        var mecha = MechaController.Instance;
        var duck = Object.FindFirstObjectByType<DuckController>();
        var spawner = EnemySpawner.Instance;
        var hud = MechaHUD.Instance;

        if (mecha == null || duck == null)
        {
            Debug.LogError("[TEST] FAIL: Componentes principales (MechaController o DuckController) no encontrados");
            isTesting = false;
            yield break;
        }

        CockpitFloorButton GetButton(string id)
        {
            var all = mecha.GetComponentsInChildren<CockpitFloorButton>();
            foreach (var b in all)
            {
                if (b.buttonId == id) return b;
            }
            return null;
        }

        // =====================================================================
        // TEST 1: AVANCE DEL MECHA (BOTÓN FWD)
        // =====================================================================
        totalTests++;
        var btnFwd = GetButton("FWD");
        if (btnFwd != null)
        {
            if (mecha.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            float initZ = mecha.transform.position.z;
            duck.TeleportToLocal(new Vector3(0f, 0.45f, 1.8f));
            btnFwd.OnDuckStanding(duck);

            yield return new WaitForSeconds(stepDuration);
            btnFwd.OnDuckExit();
            duck.TeleportToLocal(new Vector3(0f, 0.45f, 0f));
            if (mecha.TryGetComponent<Rigidbody>(out var rbDone)) rbDone.linearVelocity = Vector3.zero;

            float deltaZ = mecha.transform.position.z - initZ;
            if (deltaZ > 0.35f)
            {
                passedTests++;
                Debug.Log($"[TEST] OK: Avance del mecha verificado (Delta Z: +{deltaZ:F2}m en {stepDuration:F1}s)");
            }
            else
            {
                Debug.LogWarning($"[TEST] FAIL: Avance insuficiente (Delta Z: {deltaZ:F2}m)");
            }
        }
        else
        {
            Debug.LogWarning("[TEST] FAIL: Botón FWD no encontrado en cabina");
        }

        yield return new WaitForSeconds(0.4f);

        // =====================================================================
        // TEST 2: RETROCESO DEL MECHA (BOTÓN BWD)
        // =====================================================================
        totalTests++;
        var btnBwd = GetButton("BWD");
        if (btnBwd != null)
        {
            if (mecha.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            float initZ = mecha.transform.position.z;
            duck.TeleportToLocal(new Vector3(0f, 0.45f, -1.4f));
            btnBwd.OnDuckStanding(duck);

            yield return new WaitForSeconds(stepDuration);
            btnBwd.OnDuckExit();
            duck.TeleportToLocal(new Vector3(0f, 0.45f, 0f));
            if (mecha.TryGetComponent<Rigidbody>(out var rbDone)) rbDone.linearVelocity = Vector3.zero;

            float deltaZ = mecha.transform.position.z - initZ;
            if (deltaZ < -0.25f)
            {
                passedTests++;
                Debug.Log($"[TEST] OK: Retroceso del mecha verificado (Delta Z: {deltaZ:F2}m en {stepDuration:F1}s)");
            }
            else
            {
                Debug.LogWarning($"[TEST] FAIL: Retroceso insuficiente (Delta Z: {deltaZ:F2}m)");
            }
        }
        else
        {
            Debug.LogWarning("[TEST] FAIL: Botón BWD no encontrado en cabina");
        }

        yield return new WaitForSeconds(0.4f);

        // =====================================================================
        // TEST 3: GIRO A LA IZQUIERDA (BOTÓN LEFT)
        // =====================================================================
        totalTests++;
        var btnLeft = GetButton("LEFT");
        if (btnLeft != null)
        {
            if (mecha.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            yield return new WaitForFixedUpdate();

            float initYaw = mecha.transform.eulerAngles.y;
            duck.TeleportToLocal(new Vector3(-1.7f, 0.45f, 0.2f));
            btnLeft.OnDuckStanding(duck);

            yield return new WaitForSeconds(stepDuration);
            btnLeft.OnDuckExit();
            duck.TeleportToLocal(new Vector3(0f, 0.45f, 0f));
            if (mecha.TryGetComponent<Rigidbody>(out var rbDone)) rbDone.angularVelocity = Vector3.zero;

            float deltaYaw = Mathf.DeltaAngle(initYaw, mecha.transform.eulerAngles.y);
            if (deltaYaw < -1.8f)
            {
                passedTests++;
                Debug.Log($"[TEST] OK: Giro izquierda verificado (Delta Yaw: {deltaYaw:F1}° en {stepDuration:F1}s)");
            }
            else
            {
                Debug.LogWarning($"[TEST] FAIL: Giro izquierda insuficiente (Delta Yaw: {deltaYaw:F1}°)");
            }
        }
        else
        {
            Debug.LogWarning("[TEST] FAIL: Botón LEFT no encontrado");
        }

        yield return new WaitForSeconds(0.4f);

        // =====================================================================
        // TEST 4: GIRO A LA DERECHA (BOTÓN RIGHT)
        // =====================================================================
        totalTests++;
        var btnRight = GetButton("RIGHT");
        if (btnRight != null)
        {
            if (mecha.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            yield return new WaitForFixedUpdate();

            float initYaw = mecha.transform.eulerAngles.y;
            duck.TeleportToLocal(new Vector3(1.7f, 0.45f, 0.2f));
            btnRight.OnDuckStanding(duck);

            yield return new WaitForSeconds(stepDuration);
            btnRight.OnDuckExit();
            duck.TeleportToLocal(new Vector3(0f, 0.45f, 0f));
            if (mecha.TryGetComponent<Rigidbody>(out var rbDone)) rbDone.angularVelocity = Vector3.zero;

            float deltaYaw = Mathf.DeltaAngle(initYaw, mecha.transform.eulerAngles.y);
            if (deltaYaw > 1.8f)
            {
                passedTests++;
                Debug.Log($"[TEST] OK: Giro derecha verificado (Delta Yaw: +{deltaYaw:F1}° en {stepDuration:F1}s)");
            }
            else
            {
                Debug.LogWarning($"[TEST] FAIL: Giro derecha insuficiente (Delta Yaw: {deltaYaw:F1}°)");
            }
        }
        else
        {
            Debug.LogWarning("[TEST] FAIL: Botón RIGHT no encontrado");
        }

        yield return new WaitForSeconds(0.4f);

        // =====================================================================
        // TEST 5: ARMA PUÑETAZO CON CULAZO AÉREO (BOTÓN PUNCH)
        // =====================================================================
        totalTests++;
        var btnPunch = GetButton("PUNCH");
        if (btnPunch != null)
        {
            duck.ForceTriggerStompAt(new Vector3(-3.7f, 0.5f, 0.5f));
            btnPunch.OnDuckStomp(duck);
            mecha.TriggerWeaponAction("PUNCH", btnPunch.transform.position);

            yield return new WaitForSeconds(0.15f);
            duck.TeleportToLocal(new Vector3(0f, 0.45f, 0f));

            bool waveActive = mecha.PunchWaveObject != null && mecha.PunchWaveObject.activeSelf;
            if (waveActive || mecha.ShakeIntensity > 0.1f)
            {
                passedTests++;
                Debug.Log($"[TEST] OK: Arma Mega Puño activada por culazo (Onda expansiva: {waveActive}, Shake: {mecha.ShakeIntensity:F2})");
            }
            else
            {
                Debug.LogWarning("[TEST] FAIL: Mega Puño no disparó onda o sacudida");
            }
        }
        else
        {
            Debug.LogWarning("[TEST] FAIL: Botón PUNCH no encontrado");
        }

        yield return new WaitForSeconds(0.4f);

        // =====================================================================
        // TEST 6: CAÑÓN LÁSER Y SISTEMA DE COOLDOWN (BOTÓN LASER)
        // =====================================================================
        totalTests++;
        var btnLaser = GetButton("LASER");
        if (btnLaser != null)
        {
            duck.ForceTriggerStompAt(new Vector3(3.7f, 0.5f, 0.5f));
            btnLaser.OnDuckStomp(duck);
            mecha.TriggerWeaponAction("LASER", btnLaser.transform.position);

            yield return new WaitForSeconds(0.12f);

            float initialCd = mecha.LaserCooldown;
            bool beamActive = mecha.LaserBeamObject != null && mecha.LaserBeamObject.activeSelf;

            yield return new WaitForSeconds(0.45f);
            duck.TeleportToLocal(new Vector3(0f, 0.45f, 0f));
            float afterCd = mecha.LaserCooldown;

            if (initialCd > 1.2f && afterCd < initialCd)
            {
                passedTests++;
                Debug.Log($"[TEST] OK: Cañón Láser y cooldown verificados (Rayo: {beamActive}, Cooldown: {initialCd:F1}s -> {afterCd:F1}s)");
            }
            else
            {
                Debug.LogWarning($"[TEST] FAIL: Comportamiento erróneo del Láser (Inicial: {initialCd:F1}s, Después: {afterCd:F1}s)");
            }
        }
        else
        {
            Debug.LogWarning("[TEST] FAIL: Botón LASER no encontrado");
        }

        yield return new WaitForSeconds(0.3f);

        // =====================================================================
        // TEST 7: CONMUTADOR DE INVASIÓN DE ROBOTS (BOTÓN SPAWN)
        // =====================================================================
        totalTests++;
        var btnSpawn = GetButton("SPAWN");
        if (btnSpawn != null && spawner != null)
        {
            // Activar invasión
            duck.ForceTriggerStompAt(new Vector3(0f, 0.5f, -2.85f));
            btnSpawn.OnDuckStomp(duck);

            yield return new WaitForSeconds(0.25f);

            bool enabledMode = spawner.IsSpawningEnabled;
            int count = spawner.ActiveEnemies.Count;

            // Desactivar invasión para restaurar paz
            duck.ForceTriggerStompAt(new Vector3(0f, 0.5f, -2.85f));
            btnSpawn.OnDuckStomp(duck);

            yield return new WaitForSeconds(0.25f);
            duck.TeleportToLocal(new Vector3(0f, 0.45f, 0f));
            bool disabledMode = !spawner.IsSpawningEnabled;

            if (enabledMode && count >= 1 && disabledMode)
            {
                passedTests++;
                Debug.Log($"[TEST] OK: Conmutador de oleadas de robots verificado (Activación: {enabledMode}, Bots generados: {count}, Desactivación: {disabledMode})");
            }
            else
            {
                Debug.LogWarning($"[TEST] FAIL: Conmutador de oleadas erróneo (Activo: {enabledMode}, Bots: {count}, Desactivado: {disabledMode})");
            }
        }
        else
        {
            Debug.LogWarning("[TEST] FAIL: Botón SPAWN o EnemySpawner no encontrado");
        }

        yield return new WaitForSeconds(0.3f);

        // =====================================================================
        // TEST 8: DETECCIÓN DE VUELCO FÍSICO Y PATALETA HIDRÁULICA
        // =====================================================================
        totalTests++;
        {
            // Simular vuelco pronunciado forzando rotación lateral a 75 grados
            Quaternion tiltRot = Quaternion.Euler(0f, 0f, 75f);
            mecha.transform.rotation = tiltRot;
            if (mecha.TryGetComponent<Rigidbody>(out var rbInit))
            {
                rbInit.rotation = tiltRot;
                rbInit.angularVelocity = Vector3.zero;
            }
            yield return new WaitForSeconds(0.08f);

            bool detectedOverturned = mecha.IsOverturned;

            // Simular salto/pisotón del pato en la cabina para desencadenar recuperación
            duck.PerformJump();
            yield return new WaitForFixedUpdate();

            float recoveryTilt = Vector3.Angle(mecha.transform.up, Vector3.up);
            bool uprighted = !mecha.IsOverturned && recoveryTilt < 15f;

            // Estabilizar el mecha para que aterrice firme en la plataforma
            yield return new WaitForSeconds(0.25f);
            if (mecha.TryGetComponent<Rigidbody>(out var rbTumble))
            {
                rbTumble.linearVelocity = Vector3.zero;
                rbTumble.angularVelocity = Vector3.zero;
            }
            duck.TeleportToLocal(new Vector3(0f, 0.45f, 0f));

            if (detectedOverturned && uprighted)
            {
                passedTests++;
                Debug.Log($"[TEST] OK: Físicas de vuelco (>60°) y ¡Pataleta Hidráulica! verificadas (Detectado: {detectedOverturned}, Inclinación enderezada: {recoveryTilt:F1}°)");
            }
            else
            {
                Debug.LogWarning($"[TEST] FAIL: Recuperación de vuelco errónea (Detectado: {detectedOverturned}, Inclinación: {recoveryTilt:F1}°)");
            }
        }

        yield return new WaitForSeconds(0.3f);

        // =====================================================================
        // TEST 9: PANTALLA DE MUERTE Y HUD CON FUENTES SDF (UI TOOLKIT)
        // =====================================================================
        totalTests++;
        if (hud != null)
        {
            var uiDoc = hud.GetComponent<UIDocument>();
            var root = uiDoc != null ? uiDoc.rootVisualElement : null;

            if (root != null)
            {
                // Disparar Game Over para comprobar el modal
                hud.ShowGameOver(77);

                var modal = root.Q<VisualElement>("game-over-modal");
                var title = root.Q<Label>(className: "modal-title");
                var score = root.Q<Label>("final-score-text");
                var btnRestart = root.Q<Button>("btn-restart");

                bool modalVisible = modal != null && modal.style.display.value == DisplayStyle.Flex;
                bool hasTitleText = title != null && !string.IsNullOrEmpty(title.text);
                bool hasScoreText = score != null && score.text.Contains("77");
                bool hasButtonText = btnRestart != null && !string.IsNullOrEmpty(btnRestart.text);

                // Comprobar que restart funciona y oculta el modal
                hud.OnRestartClicked();
                bool modalHidden = modal != null && modal.style.display.value == DisplayStyle.None;

                if (modalVisible && hasTitleText && hasScoreText && hasButtonText && modalHidden)
                {
                    passedTests++;
                    Debug.Log($"[TEST] OK: UI Toolkit verificado (Modal visible: {modalVisible}, Título: '{title.text}', Marcador: '{score.text}', Botón: '{btnRestart.text}', Reinicio correcto: {modalHidden})");
                }
                else
                {
                    Debug.LogWarning($"[TEST] FAIL: Error en UI Toolkit (Visible: {modalVisible}, Título: {hasTitleText}, Score: {hasScoreText}, Botón: {hasButtonText}, Hidden: {modalHidden})");
                }
            }
            else
            {
                Debug.LogWarning("[TEST] FAIL: rootVisualElement de UIDocument no encontrado");
            }
        }
        else
        {
            Debug.LogWarning("[TEST] FAIL: MechaHUD no encontrado");
        }

        // =====================================================================
        // RESUMEN FINAL
        // =====================================================================
        Debug.Log("==========================================================================");
        Debug.Log($"[AUTOTEST] RESULTADO FINAL: {passedTests}/{totalTests} PRUEBAS COMPLETADAS CON ÉXITO");
        Debug.Log("==========================================================================");

        isTesting = false;
    }
}
