using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador principal del Pato-Mecha gigante.
/// Gestiona la locomoción por botones del suelo, las armas (Puño y Láser),
/// el balanceo e inercia de la cabina, las piernas articuladas y la vida del chasis.
/// </summary>
public class MechaController : MonoBehaviour
{
    public static MechaController Instance { get; private set; }

    [Header("Movimiento del Mecha")]
    [SerializeField] private float moveSpeed = 14f;
    [SerializeField] private float turnSpeed = 75f;

    [Header("Partes Físicas y Visuales")]
    [SerializeField] private Transform cockpitTransform;
    [SerializeField] private Transform leftLegTransform;
    [SerializeField] private Transform rightLegTransform;
    [SerializeField] private Transform leftArmTransform;
    [SerializeField] private Transform rightArmTransform;
    [SerializeField] private GameObject laserBeamObject;
    [SerializeField] private GameObject punchWaveObject;

    [Header("Chasis y Salud")]
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp = 100;

    [Header("Cooldowns y Tiempos")]
    [SerializeField] private float laserCooldownDuration = 2.0f;

    // Estado interno
    private float currentLaserCooldown = 0f;
    private int score = 0;
    private bool isGameOver = false;

    private float walkCycle = 0f;
    private float stepTimer = 0f;
    private float punchTimer = 0f;
    private float laserTimer = 0f;
    private float shakeIntensity = 0f;

    private Vector3 initialLeftArmPos;
    private Vector3 initialRightArmPos;
    private Vector3 initialCockpitLocalPos;

    private readonly List<CockpitFloorButton> buttons = new List<CockpitFloorButton>();
    private DuckController duckInCockpit;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public int Score => score;
    public bool IsGameOver => isGameOver;
    public float LaserCooldown => currentLaserCooldown;
    public float ShakeIntensity => shakeIntensity;

    private void Awake()
    {
        Instance = this;
        currentHp = maxHp;

        if (cockpitTransform != null) initialCockpitLocalPos = cockpitTransform.localPosition;
        if (leftArmTransform != null) initialLeftArmPos = leftArmTransform.localPosition;
        if (rightArmTransform != null) initialRightArmPos = rightArmTransform.localPosition;

        if (laserBeamObject != null) laserBeamObject.SetActive(false);
        if (punchWaveObject != null) punchWaveObject.SetActive(false);

        duckInCockpit = FindFirstObjectByType<DuckController>();
        buttons.AddRange(GetComponentsInChildren<CockpitFloorButton>());
    }

    private void Update()
    {
        if (isGameOver) return;

        UpdateCooldowns();
        ProcessButtonsLocomotion();
        AnimateArms();
        AnimateCockpitSway();
    }

    private void UpdateCooldowns()
    {
        if (currentLaserCooldown > 0f)
            currentLaserCooldown = MathfsMax(0f, currentLaserCooldown - Time.deltaTime);

        if (punchTimer > 0f)
            punchTimer = MathfsMax(0f, punchTimer - Time.deltaTime);

        if (laserTimer > 0f)
            laserTimer = MathfsMax(0f, laserTimer - Time.deltaTime);

        if (shakeIntensity > 0f)
            shakeIntensity = MathfsMax(0f, shakeIntensity - Time.deltaTime * 2.5f);
    }

    private float MathfsMax(float a, float b) => Mathf.Max(a, b);

    /// <summary>
    /// Lee los botones pisados en la cabina y mueve/gira el Mecha en el mundo.
    /// </summary>
    private void ProcessButtonsLocomotion()
    {
        float moveDir = 0f;
        float turnDir = 0f;

        for (int i = 0; i < buttons.Count; i++)
        {
            var btn = buttons[i];
            if (btn == null || !btn.isPressed) continue;

            switch (btn.buttonId)
            {
                case "FWD": moveDir += 1.0f; break;
                case "BWD": moveDir -= 0.7f; break;
                case "LEFT": turnDir -= 1.0f; break;
                case "RIGHT": turnDir += 1.0f; break;
            }
        }

        bool isWalking = Mathf.Abs(moveDir) > 0.01f || Mathf.Abs(turnDir) > 0.01f;

        if (Mathf.Abs(turnDir) > 0.01f)
        {
            transform.Rotate(Vector3.up, turnDir * turnSpeed * Time.deltaTime);
        }

        if (Mathf.Abs(moveDir) > 0.01f)
        {
            transform.position += transform.forward * (moveDir * moveSpeed * Time.deltaTime);
        }

        // Animación de caminata de las piernas del mecha
        if (isWalking)
        {
            walkCycle += Time.deltaTime * 7.5f;
            stepTimer += Time.deltaTime;

            if (stepTimer >= 0.42f)
            {
                stepTimer = 0f;
                ProceduralAudio.Instance?.PlayMechaStep();

                // Pequeña inercia cómica que empuja al pato con cada zancada
                if (duckInCockpit != null && duckInCockpit.IsGrounded)
                {
                    duckInCockpit.ApplyExternalNudge(new Vector3(Mathf.Sin(walkCycle) * 1.8f, 0f, 0f));
                }
            }

            float swing = Mathf.Sin(walkCycle) * 26f;
            if (leftLegTransform != null) leftLegTransform.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (rightLegTransform != null) rightLegTransform.localRotation = Quaternion.Euler(-swing, 0f, 0f);
        }
        else
        {
            // Retorno suave a reposo de piernas
            if (leftLegTransform != null) leftLegTransform.localRotation = Quaternion.Slerp(leftLegTransform.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            if (rightLegTransform != null) rightLegTransform.localRotation = Quaternion.Slerp(rightLegTransform.localRotation, Quaternion.identity, Time.deltaTime * 8f);
        }
    }

    /// <summary>
    /// Aplica balanceo e inclinación dinámica a la cabina con sacudidas de impacto.
    /// </summary>
    private void AnimateCockpitSway()
    {
        if (cockpitTransform == null) return;

        float bobY = Mathf.Abs(Mathf.Sin(walkCycle)) * 0.18f;
        float shakeX = Random.Range(-1f, 1f) * shakeIntensity * 0.3f;
        float shakeY = Random.Range(-1f, 1f) * shakeIntensity * 0.3f;

        cockpitTransform.localPosition = initialCockpitLocalPos + new Vector3(shakeX, bobY + shakeY, 0f);

        float tiltZ = Mathf.Sin(walkCycle) * 1.8f;
        cockpitTransform.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
    }

    /// <summary>
    /// Anima el retroceso y lanzamiento de los brazos del Mecha.
    /// </summary>
    private void AnimateArms()
    {
        // Brazo izquierdo (Puño)
        if (leftArmTransform != null)
        {
            if (punchTimer > 0f)
            {
                float progress = Mathf.Sin((punchTimer / 0.45f) * Mathf.PI);
                leftArmTransform.localPosition = initialLeftArmPos + new Vector3(0f, 0f, progress * 12f);
                if (punchWaveObject != null) punchWaveObject.SetActive(true);
            }
            else
            {
                leftArmTransform.localPosition = initialLeftArmPos;
                if (punchWaveObject != null) punchWaveObject.SetActive(false);
            }
        }

        // Brazo derecho (Cañón Láser)
        if (rightArmTransform != null)
        {
            if (laserTimer > 0f)
            {
                rightArmTransform.localPosition = initialRightArmPos - new Vector3(0f, 0f, 1.2f); // Retroceso
                if (laserBeamObject != null) laserBeamObject.SetActive(true);
            }
            else
            {
                rightArmTransform.localPosition = initialRightArmPos;
                if (laserBeamObject != null) laserBeamObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Dispara las armas cuando el pato aterriza con un culazo sobre los botones PUNCH o LASER.
    /// </summary>
    public void TriggerWeaponAction(string weaponId, Vector3 buttonWorldPos)
    {
        if (isGameOver) return;

        if (weaponId == "PUNCH")
        {
            punchTimer = 0.45f;
            shakeIntensity = 0.5f;
            ProceduralAudio.Instance?.PlayPunch();

            FloatingText.Spawn(buttonWorldPos + Vector3.up * 1.5f, "💥 ¡MEGA PUÑO!", new Color(1f, 0.25f, 0.4f), 0.35f);

            // Destruir enemigos en un cono frontal hasta 42 metros
            int destroyedCount = 0;
            var enemies = EnemySpawner.Instance != null ? EnemySpawner.Instance.ActiveEnemies : null;
            if (enemies != null)
            {
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    var e = enemies[i];
                    if (e == null) continue;

                    Vector3 toEnemy = e.transform.position - transform.position;
                    toEnemy.y = 0f;
                    float dist = toEnemy.magnitude;
                    float dot = Vector3.Dot(transform.forward, toEnemy.normalized);

                    if (dist < 42f && dot > 0.25f)
                    {
                        e.DestroyByWeapon("PUNCH");
                        destroyedCount++;
                    }
                }
            }

            if (destroyedCount > 0)
            {
                FloatingText.Spawn(transform.position + transform.forward * 4f + Vector3.up * 7f,
                    $"¡{destroyedCount} BOTS APLASTADOS!", Color.yellow, 0.4f);
            }
        }
        else if (weaponId == "LASER")
        {
            if (currentLaserCooldown > 0f)
            {
                FloatingText.Spawn(buttonWorldPos + Vector3.up * 1.5f,
                    $"⏳ RECARGANDO ({currentLaserCooldown:F1}s)", new Color(0.3f, 0.8f, 1f), 0.3f);
                return;
            }

            laserTimer = 0.45f;
            currentLaserCooldown = laserCooldownDuration;
            shakeIntensity = 0.4f;
            ProceduralAudio.Instance?.PlayLaser();

            FloatingText.Spawn(buttonWorldPos + Vector3.up * 1.5f, "⚡ ¡CAÑÓN LÁSER!", new Color(0.2f, 0.9f, 1f), 0.35f);

            // Destruir enemigos en una línea recta de 145 metros frente al mecha
            var enemies = EnemySpawner.Instance != null ? EnemySpawner.Instance.ActiveEnemies : null;
            if (enemies != null)
            {
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    var e = enemies[i];
                    if (e == null) continue;

                    Vector3 toEnemy = e.transform.position - transform.position;
                    toEnemy.y = 0f;
                    float forwardProj = Vector3.Dot(toEnemy, transform.forward);
                    float perpDist = Vector3.Cross(transform.forward, toEnemy).magnitude;

                    if (forwardProj > 0f && forwardProj < 145f && perpDist < 9.5f)
                    {
                        e.DestroyByWeapon("LASER");
                    }
                }
            }
        }
    }

    public void AddScore(int points)
    {
        score += points;
    }

    public void TakeDamage(int damage)
    {
        if (isGameOver) return;

        currentHp = Mathf.Max(0, currentHp - damage);
        shakeIntensity = 0.8f;

        // Empujar al pato dentro de la cabina por el impacto
        if (duckInCockpit != null)
        {
            Vector3 randomKnock = new Vector3(Random.Range(-12f, 12f), 8f, Random.Range(-12f, 12f));
            duckInCockpit.ApplyExternalNudge(randomKnock);
        }

        // Destello rojo de daño en el HUD
        MechaHUD.Instance?.TriggerDamageFlash();

        if (currentHp <= 0)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        shakeIntensity = 1.2f;
        ProceduralAudio.Instance?.PlayExplosion();

        FloatingText.Spawn(transform.position + Vector3.up * 8f, "¡MECHA DESTRUIDO!", Color.red, 0.5f);
        MechaHUD.Instance?.ShowGameOver(score);
    }

    public void RestartGame()
    {
        isGameOver = false;
        currentHp = maxHp;
        score = 0;
        currentLaserCooldown = 0f;
        punchTimer = 0f;
        laserTimer = 0f;
        shakeIntensity = 0f;
        walkCycle = 0f;
        stepTimer = 0f;

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        if (cockpitTransform != null) cockpitTransform.localPosition = initialCockpitLocalPos;
        if (leftArmTransform != null) leftArmTransform.localPosition = initialLeftArmPos;
        if (rightArmTransform != null) rightArmTransform.localPosition = initialRightArmPos;
        if (leftLegTransform != null) leftLegTransform.localRotation = Quaternion.identity;
        if (rightLegTransform != null) rightLegTransform.localRotation = Quaternion.identity;

        if (laserBeamObject != null) laserBeamObject.SetActive(false);
        if (punchWaveObject != null) punchWaveObject.SetActive(false);

        // Restablecer los botones de la cabina
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null) buttons[i].ResetButton();
        }

        EnemySpawner.Instance?.ClearAllEnemies();
        EnemySpawner.Instance?.SpawnEnemy();
        EnemySpawner.Instance?.SpawnEnemy();

        // Limpiar cualquier texto flotante residual
        var floatingTexts = Object.FindObjectsByType<FloatingText>(FindObjectsSortMode.None);
        for (int i = 0; i < floatingTexts.Length; i++)
        {
            if (floatingTexts[i] != null) Destroy(floatingTexts[i].gameObject);
        }

        if (duckInCockpit != null)
        {
            duckInCockpit.ResetPosition(new Vector3(0f, 0.6f, -0.2f));
        }

        CockpitCameraController.Instance?.ResetCamera();

        MechaHUD.Instance?.HideGameOver();
    }
}
