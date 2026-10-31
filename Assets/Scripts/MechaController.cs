using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador físico principal del mecha gigante MechaQuack.
/// Gestiona la locomoción con Rigidbody 3D, suspensión en dos patas independientes,
/// pérdida de equilibrio y vuelcos al salirse de puentes o desniveles, armas (Puño y Láser),
/// y recuperación hidráulica ("¡PATALETA!").
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MechaController : MonoBehaviour
{
    public static MechaController Instance { get; private set; }

    [Header("Físicas y Locomoción")]
    [SerializeField] private float movePower = 48000f;
    [SerializeField] private float turnTorque = 36000f;
    [SerializeField] private float maxLinearVelocity = 16f;
    [SerializeField] private float suspensionStrength = 82000f;
    [SerializeField] private float suspensionDamping = 7500f;
    [SerializeField] private float legRayLength = 6.4f;

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
    private Rigidbody rb;
    private float currentLaserCooldown = 0f;
    private int score = 0;
    private bool isGameOver = false;
    private bool isOverturned = false;
    private float overturnPromptTimer = 0f;

    private float walkCycle = 0f;
    private float stepTimer = 0f;
    private float punchTimer = 0f;
    private float laserTimer = 0f;
    private float shakeIntensity = 0f;

    private float inputMove = 0f;
    private float inputTurn = 0f;

    private bool leftGrounded = false;
    private bool rightGrounded = false;

    private Vector3 initialLeftArmPos;
    private Vector3 initialRightArmPos;
    private Vector3 initialCockpitLocalPos;

    private readonly List<CockpitFloorButton> buttons = new List<CockpitFloorButton>();
    private DuckController duckInCockpit;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public int Score => score;
    public bool IsGameOver => isGameOver;
    public bool IsOverturned => isOverturned;
    public bool LeftGrounded => leftGrounded;
    public bool RightGrounded => rightGrounded;
    public float LaserCooldown => currentLaserCooldown;
    public float ShakeIntensity => shakeIntensity;

    private void Awake()
    {
        Instance = this;
        Application.runInBackground = true;
        currentHp = maxHp;

        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.mass = 8000f;
        rb.linearDamping = 1.2f;
        rb.angularDamping = 2.2f;
        rb.centerOfMass = new Vector3(0f, 3.2f, 0f); // Centro de masa elevado para inclinación física realista
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

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
        ReadButtonsInput();
        AnimateLegsAndArms();
        AnimateCockpitSway();
        CheckOverturnedState();
    }

    private void UpdateCooldowns()
    {
        if (currentLaserCooldown > 0f)
            currentLaserCooldown = Mathf.Max(0f, currentLaserCooldown - Time.deltaTime);

        if (punchTimer > 0f)
            punchTimer = Mathf.Max(0f, punchTimer - Time.deltaTime);

        if (laserTimer > 0f)
            laserTimer = Mathf.Max(0f, laserTimer - Time.deltaTime);

        if (shakeIntensity > 0f)
            shakeIntensity = Mathf.Max(0f, shakeIntensity - Time.deltaTime * 2.5f);
    }

    /// <summary>
    /// Lee los botones pisados en la cabina para determinar dirección y giro deseados.
    /// </summary>
    private void ReadButtonsInput()
    {
        inputMove = 0f;
        inputTurn = 0f;

        for (int i = 0; i < buttons.Count; i++)
        {
            var btn = buttons[i];
            if (btn == null || !btn.isPressed) continue;

            switch (btn.buttonId)
            {
                case "FWD": inputMove += 1.0f; break;
                case "BWD": inputMove -= 0.7f; break;
                case "LEFT": inputTurn -= 1.0f; break;
                case "RIGHT": inputTurn += 1.0f; break;
            }
        }
    }

    private void FixedUpdate()
    {
        if (isGameOver) return;

        ApplyLocomotionAndSuspension();
    }

    /// <summary>
    /// Simulación de físicas: Proyección de patas independientes, suspensión,
    /// desequilibrio si una pata queda en el aire, y fuerzas de locomoción.
    /// </summary>
    private void ApplyLocomotionAndSuspension()
    {
        int groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

        // Origen de los rayos de soporte (raíz de cadera de cada pierna)
        Vector3 leftLegRoot = transform.TransformPoint(new Vector3(-3.1f, 1.8f, 0f));
        Vector3 rightLegRoot = transform.TransformPoint(new Vector3(3.1f, 1.8f, 0f));

        leftGrounded = CheckFootGrounded(leftLegRoot, out RaycastHit hitL, groundMask);
        rightGrounded = CheckFootGrounded(rightLegRoot, out RaycastHit hitR, groundMask);

        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);

        // 1. APLICAR SUSPENSIÓN INDEPENDIENTE A CADA PATA
        if (leftGrounded)
        {
            float compression = Mathf.Clamp01((legRayLength - hitL.distance) / 1.8f);
            float vertVel = Vector3.Dot(rb.GetPointVelocity(leftLegRoot), transform.up);
            float springForce = (compression * suspensionStrength) - (vertVel * suspensionDamping);
            rb.AddForceAtPosition(transform.up * Mathf.Max(0f, springForce), leftLegRoot, ForceMode.Force);
        }

        if (rightGrounded)
        {
            float compression = Mathf.Clamp01((legRayLength - hitR.distance) / 1.8f);
            float vertVel = Vector3.Dot(rb.GetPointVelocity(rightLegRoot), transform.up);
            float springForce = (compression * suspensionStrength) - (vertVel * suspensionDamping);
            rb.AddForceAtPosition(transform.up * Mathf.Max(0f, springForce), rightLegRoot, ForceMode.Force);
        }

        // 2. FÍSICAS DE DESEQUILIBRIO (SI UNA PATA QUEDA FUERA DEL TERRENO/PUENTE)
        if (leftGrounded && !rightGrounded && tiltAngle < 75f)
        {
            // Pata derecha en el vacío: el mecha pierde apoyo y el peso lo hace inclinarse y caer hacia la derecha
            rb.AddTorque(transform.forward * 28000f, ForceMode.Force);
            rb.AddForce(transform.right * 14000f, ForceMode.Force); // Deslizamiento hacia el borde

            if (duckInCockpit != null && duckInCockpit.IsGrounded)
            {
                duckInCockpit.ApplyExternalNudge(new Vector3(2.4f, 0f, 0f));
            }
        }
        else if (!leftGrounded && rightGrounded && tiltAngle < 75f)
        {
            // Pata izquierda en el vacío: el mecha se desestabiliza hacia la izquierda
            rb.AddTorque(-transform.forward * 28000f, ForceMode.Force);
            rb.AddForce(-transform.right * 14000f, ForceMode.Force);

            if (duckInCockpit != null && duckInCockpit.IsGrounded)
            {
                duckInCockpit.ApplyExternalNudge(new Vector3(-2.4f, 0f, 0f));
            }
        }

        // 3. ESTABILIZACIÓN VERTICAL (Solo cuando ambas patas están firmemente apoyadas y no está volcado)
        if (leftGrounded && rightGrounded && tiltAngle < 38f)
        {
            Vector3 rotAxis = Vector3.Cross(transform.up, Vector3.up);
            rb.AddTorque(rotAxis * (tiltAngle * 1400f) - rb.angularVelocity * 450f, ForceMode.Force);
        }

        // 4. FUERZAS DE LOCOMOCIÓN (Solo si al menos una pata toca suelo y no está totalmente volcado)
        if ((leftGrounded || rightGrounded) && tiltAngle < 55f)
        {
            if (Mathf.Abs(inputMove) > 0.01f)
            {
                Vector3 moveForce = transform.forward * (inputMove * movePower);
                rb.AddForce(moveForce, ForceMode.Force);
            }

            if (Mathf.Abs(inputTurn) > 0.01f)
            {
                Vector3 turn = Vector3.up * (inputTurn * turnTorque);
                rb.AddTorque(turn, ForceMode.Force);
            }

            // Limitar velocidad máxima en suelo para control preciso
            Vector3 horizVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (horizVel.magnitude > maxLinearVelocity)
            {
                Vector3 clamped = horizVel.normalized * maxLinearVelocity;
                rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.y);
            }
        }
    }

    private bool CheckFootGrounded(Vector3 probeOrigin, out RaycastHit groundHit, int mask)
    {
        groundHit = default;
        var hits = Physics.RaycastAll(probeOrigin, -transform.up, legRayLength, mask);
        float closestDist = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.transform.IsChildOf(transform)) continue;
            if (hits[i].distance < closestDist)
            {
                closestDist = hits[i].distance;
                groundHit = hits[i];
                found = true;
            }
        }
        return found;
    }

    /// <summary>
    /// Comprueba si el Mecha ha volcado y gestiona avisos en pantalla.
    /// </summary>
    private void CheckOverturnedState()
    {
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
        bool wasOverturned = isOverturned;
        isOverturned = tiltAngle > 60f;

        if (isOverturned)
        {
            overturnPromptTimer -= Time.deltaTime;
            if (overturnPromptTimer <= 0f)
            {
                overturnPromptTimer = 2.8f;
                FloatingText.Spawn(transform.position + Vector3.up * 6f, "¡MECHA VOLCADO! PISOTÓN (E) PARA ENDEREZAR", Color.yellow, 0.45f);
                ProceduralAudio.Instance?.PlayQuack();
            }
        }
        else if (wasOverturned)
        {
            overturnPromptTimer = 0f;
        }
    }

    /// <summary>
    /// Maniobra cómica de emergencia ("¡PATALETA!"): Salto hidráulico que voltea
    /// el Mecha de vuelta sobre sus pies cuando está tumbado en el suelo.
    /// </summary>
    public void RecoverFromTumble()
    {
        if (!isOverturned) return;

        ProceduralAudio.Instance?.PlayHydraulicRecover();
        FloatingText.Spawn(transform.position + Vector3.up * 7f, "¡PATALETA HIDRÁULICA!", Color.yellow, 0.45f);

        // Impulso vertical y reseteo de velocidad angular
        rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.2f, 18f, rb.linearVelocity.z * 0.2f);
        rb.angularVelocity = Vector3.zero;

        // Orientar erguido en el aire
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        isOverturned = false;
        overturnPromptTimer = 0f;
    }

    private void AnimateLegsAndArms()
    {
        bool isWalking = (Mathf.Abs(inputMove) > 0.01f || Mathf.Abs(inputTurn) > 0.01f) && (leftGrounded || rightGrounded) && !isOverturned;

        if (isWalking)
        {
            walkCycle += Time.deltaTime * 7.5f;
            stepTimer += Time.deltaTime;

            if (stepTimer >= 0.42f)
            {
                stepTimer = 0f;
                ProceduralAudio.Instance?.PlayMechaStep();

                if (duckInCockpit != null && duckInCockpit.IsGrounded)
                {
                    duckInCockpit.ApplyExternalNudge(new Vector3(Mathf.Sin(walkCycle) * 1.6f, 0f, 0f));
                }
            }

            float swing = Mathf.Sin(walkCycle) * 26f;
            if (leftLegTransform != null) leftLegTransform.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (rightLegTransform != null) rightLegTransform.localRotation = Quaternion.Euler(-swing, 0f, 0f);
        }
        else
        {
            if (leftLegTransform != null) leftLegTransform.localRotation = Quaternion.Slerp(leftLegTransform.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            if (rightLegTransform != null) rightLegTransform.localRotation = Quaternion.Slerp(rightLegTransform.localRotation, Quaternion.identity, Time.deltaTime * 8f);
        }

        AnimateArms();
    }

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
                rightArmTransform.localPosition = initialRightArmPos - new Vector3(0f, 0f, 1.2f);
                if (laserBeamObject != null) laserBeamObject.SetActive(true);
            }
            else
            {
                rightArmTransform.localPosition = initialRightArmPos;
                if (laserBeamObject != null) laserBeamObject.SetActive(false);
            }
        }
    }

    public void TriggerWeaponAction(string weaponId, Vector3 buttonWorldPos)
    {
        if (isGameOver) return;

        if (weaponId == "PUNCH")
        {
            punchTimer = 0.45f;
            shakeIntensity = 0.5f;
            ProceduralAudio.Instance?.PlayPunch();

            FloatingText.Spawn(buttonWorldPos + Vector3.up * 1.5f, "💥 ¡MEGA PUÑO!", new Color(1f, 0.25f, 0.4f), 0.35f);

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

        if (duckInCockpit != null)
        {
            Vector3 randomKnock = new Vector3(Random.Range(-10f, 10f), 6f, Random.Range(-10f, 10f));
            duckInCockpit.ApplyExternalNudge(randomKnock);
        }

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
        isOverturned = false;
        currentHp = maxHp;
        score = 0;
        currentLaserCooldown = 0f;
        punchTimer = 0f;
        laserTimer = 0f;
        shakeIntensity = 0f;
        walkCycle = 0f;
        stepTimer = 0f;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = new Vector3(0f, 1.2f, 0f);
        transform.rotation = Quaternion.identity;

        if (cockpitTransform != null) cockpitTransform.localPosition = initialCockpitLocalPos;
        if (leftArmTransform != null) leftArmTransform.localPosition = initialLeftArmPos;
        if (rightArmTransform != null) rightArmTransform.localPosition = initialRightArmPos;
        if (leftLegTransform != null) leftLegTransform.localRotation = Quaternion.identity;
        if (rightLegTransform != null) rightLegTransform.localRotation = Quaternion.identity;

        if (laserBeamObject != null) laserBeamObject.SetActive(false);
        if (punchWaveObject != null) punchWaveObject.SetActive(false);

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null) buttons[i].ResetButton();
        }

        EnemySpawner.Instance?.ClearAllEnemies();

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
