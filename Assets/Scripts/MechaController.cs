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
    [SerializeField] private float suspensionStrength = 95000f;
    [SerializeField] private float suspensionDamping = 14000f;
    [SerializeField] private float targetRideHeight = 1.8f;
    [SerializeField] private float legRayLength = 3.8f;

    [Header("Partes Físicas y Visuales")]
    [SerializeField] private Transform cockpitTransform;
    [SerializeField] private Transform leftLegTransform;
    [SerializeField] private Transform rightLegTransform;
    [SerializeField] private Transform leftArmTransform;
    [SerializeField] private Transform rightArmTransform;

    [Header("Armas: Ametralladora (Gatling)")]
    [SerializeField] private Transform gatlingRotorTransform;
    [SerializeField] private Transform gatlingMuzzleTransform;
    [SerializeField] private Light gatlingMuzzleLight;
    [SerializeField] private float gatlingRoundsPerSecond = 12f;

    [Header("Armas: Lanzamisiles (Missile Pod)")]
    [SerializeField] private Transform missilePodTransform;
    [SerializeField] private Transform[] missileLaunchTubes;
    [SerializeField] private float missileCooldownDuration = 3.5f;

    [Header("Chasis y Salud")]
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp = 100;

    [Header("Cooldowns y Tiempos")]
    [SerializeField] private float laserCooldownDuration = 3.5f;

    // Estado interno
    private Rigidbody rb;
    private float currentLaserCooldown = 0f;
    private int score = 0;
    private bool isGameOver = false;
    private bool isOverturned = false;
    private float overturnPromptTimer = 0f;
    private float recoveryGraceTimer = 0f;

    private float walkCycle = 0f;
    private float stepTimer = 0f;
    private float punchTimer = 0f;
    private float laserTimer = 0f;
    private float shakeIntensity = 0f;

    private bool isGatlingFiring = false;
    private bool isGatlingHolding = false;
    private float gatlingFireCooldown = 0f;
    private float gatlingRotorSpeed = 0f;
    private float gatlingRecoilZ = 0f;
    private float missileRecoilZ = 0f;

    private float inputMove = 0f;
    private float inputTurn = 0f;

    private bool leftGrounded = false;
    private bool rightGrounded = false;

    private Vector3 initialLeftArmPos;
    private Vector3 initialRightArmPos;
    private Vector3 initialCockpitLocalPos;

    private readonly List<CockpitFloorButton> buttons = new List<CockpitFloorButton>();
    private DuckController duckInCockpit;
    private readonly RaycastHit[] groundRayHits = new RaycastHit[16];

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public int Score => score;
    public bool IsGameOver => isGameOver;
    public bool IsOverturned => isOverturned;
    public bool LeftGrounded => leftGrounded;
    public bool RightGrounded => rightGrounded;
    public bool IsGatlingFiring => isGatlingFiring || punchTimer > 0f;
    public float LaserCooldown => currentLaserCooldown;
    public float MissileCooldown => currentLaserCooldown;
    public float ShakeIntensity => shakeIntensity;
    public void TriggerFootstepShake(float intensity) => shakeIntensity = Mathf.Max(shakeIntensity, intensity);
    public GameObject PunchWaveObject => null;
    public GameObject LaserBeamObject => null;

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

        if (missileCooldownDuration <= 0f) missileCooldownDuration = laserCooldownDuration;
        else laserCooldownDuration = missileCooldownDuration;

        if (cockpitTransform != null) initialCockpitLocalPos = cockpitTransform.localPosition;
        if (leftArmTransform != null) initialLeftArmPos = leftArmTransform.localPosition;
        if (rightArmTransform != null) initialRightArmPos = rightArmTransform.localPosition;

        duckInCockpit = FindFirstObjectByType<DuckController>();
        buttons.AddRange(GetComponentsInChildren<CockpitFloorButton>());
    }

    private void Update()
    {
        if (isGameOver) return;

        UpdateCooldowns();
        ReadButtonsInput();
        UpdateGatlingMachineGun();
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
    /// Lee los botones pisados en la cabina para determinar dirección, giro y disparo continuo de ametralladora.
    /// </summary>
    private void ReadButtonsInput()
    {
        inputMove = 0f;
        inputTurn = 0f;
        isGatlingHolding = false;

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
                case "GATLING":
                case "PUNCH":
                    if (btn.type == CockpitFloorButton.ButtonType.Hold)
                    {
                        isGatlingHolding = true;
                    }
                    break;
            }
        }
    }

    private void UpdateGatlingMachineGun()
    {
        if (isGatlingHolding && !isGameOver)
        {
            gatlingRotorSpeed = Mathf.Lerp(gatlingRotorSpeed, 1400f, Time.deltaTime * 8f);
            gatlingFireCooldown -= Time.deltaTime;
            if (gatlingFireCooldown <= 0f)
            {
                gatlingFireCooldown = 1f / Mathf.Max(1f, gatlingRoundsPerSecond);
                FireSingleGatlingShot();
            }
        }
        else if (!isGatlingFiring)
        {
            gatlingRotorSpeed = Mathf.Lerp(gatlingRotorSpeed, 0f, Time.deltaTime * 3.5f);
        }

        if (gatlingRotorTransform != null && Mathf.Abs(gatlingRotorSpeed) > 0.5f)
        {
            gatlingRotorTransform.Rotate(Vector3.forward, gatlingRotorSpeed * Time.deltaTime, Space.Self);
        }

        // Recuperación de retroceso dinámico
        gatlingRecoilZ = Mathf.Lerp(gatlingRecoilZ, 0f, Time.deltaTime * 12f);
        missileRecoilZ = Mathf.Lerp(missileRecoilZ, 0f, Time.deltaTime * 8f);
    }

    public void FireSingleGatlingShot()
    {
        if (isGameOver) return;

        Vector3 muzzlePos;
        if (gatlingMuzzleTransform != null)
        {
            muzzlePos = gatlingMuzzleTransform.position;
        }
        else if (leftArmTransform != null)
        {
            muzzlePos = leftArmTransform.position + leftArmTransform.forward * 4.5f;
        }
        else
        {
            muzzlePos = transform.position + transform.forward * 4f - transform.right * 3.5f + Vector3.up * 2f;
        }

        Vector3 aimDir = transform.forward;
        // Ligera dispersión balística de ametralladora pesada
        Vector2 spread = Random.insideUnitCircle * 0.024f;
        aimDir = (aimDir + transform.right * spread.x + transform.up * spread.y).normalized;

        GatlingTracer.Spawn(muzzlePos, aimDir);
        ProceduralAudio.Instance?.PlayGatlingFire();
        WindshieldHUDCrosshair.Instance?.TriggerRecoil(0.08f);

        if (gatlingMuzzleLight != null)
        {
            StopCoroutine(nameof(MuzzleLightFlashRoutine));
            StartCoroutine(MuzzleLightFlashRoutine());
        }

        gatlingRecoilZ = Mathf.Clamp(gatlingRecoilZ + 0.35f, 0f, 1.2f);
        shakeIntensity = Mathf.Max(shakeIntensity, 0.12f);
    }

    private System.Collections.IEnumerator MuzzleLightFlashRoutine()
    {
        if (gatlingMuzzleLight != null) gatlingMuzzleLight.enabled = true;
        yield return new WaitForSeconds(0.04f);
        if (gatlingMuzzleLight != null) gatlingMuzzleLight.enabled = false;
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
        if (isOverturned) return;

        int groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

        // Origen de los rayos de soporte (raíz de cadera de cada pierna)
        Vector3 leftLegRoot = transform.TransformPoint(new Vector3(-3.1f, 1.8f, 0f));
        Vector3 rightLegRoot = transform.TransformPoint(new Vector3(3.1f, 1.8f, 0f));

        leftGrounded = CheckFootGrounded(leftLegRoot, out RaycastHit hitL, groundMask);
        rightGrounded = CheckFootGrounded(rightLegRoot, out RaycastHit hitR, groundMask);

        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);

        // Equilibrio gravitacional exacto por pata (peso = 8000kg * 9.81 / 2 = 39240N)
        float baseEquilibriumForce = (rb.mass * 9.81f) * 0.5f;

        // Integrar la cota de la pata si está apoyada en un escalón/roca para elevar el cuerpo
        float effectiveDistL = hitL.distance;
        if (leftLegTransform != null)
        {
            var pLegL = leftLegTransform.GetComponent<ProceduralLeg>();
            if (pLegL != null && pLegL.IsGrounded)
            {
                float footGroundY = pLegL.CurrentFootPosition.y - pLegL.AnkleToSoleOffset;
                float footDist = leftLegRoot.y - footGroundY;
                if (footDist < effectiveDistL) effectiveDistL = footDist;
            }
        }

        float effectiveDistR = hitR.distance;
        if (rightLegTransform != null)
        {
            var pLegR = rightLegTransform.GetComponent<ProceduralLeg>();
            if (pLegR != null && pLegR.IsGrounded)
            {
                float footGroundY = pLegR.CurrentFootPosition.y - pLegR.AnkleToSoleOffset;
                float footDist = rightLegRoot.y - footGroundY;
                if (footDist < effectiveDistR) effectiveDistR = footDist;
            }
        }

        // 1. APLICAR SUSPENSIÓN CALIBRADA (Mantiene el mecha a la cota exacta para que los pies pisen el suelo)
        if (leftGrounded && effectiveDistL < legRayLength)
        {
            float compression = targetRideHeight - effectiveDistL;
            float vertVel = Vector3.Dot(rb.GetPointVelocity(leftLegRoot), transform.up);
            float springForce = baseEquilibriumForce + (compression * suspensionStrength) - (vertVel * suspensionDamping);
            rb.AddForceAtPosition(transform.up * Mathf.Max(0f, springForce), leftLegRoot, ForceMode.Force);
        }

        if (rightGrounded && effectiveDistR < legRayLength)
        {
            float compression = targetRideHeight - effectiveDistR;
            float vertVel = Vector3.Dot(rb.GetPointVelocity(rightLegRoot), transform.up);
            float springForce = baseEquilibriumForce + (compression * suspensionStrength) - (vertVel * suspensionDamping);
            rb.AddForceAtPosition(transform.up * Mathf.Max(0f, springForce), rightLegRoot, ForceMode.Force);
        }

        // 2. FÍSICAS DE DESEQUILIBRIO (Solo si una pata está en el abismo/vacío, no durante pasos de caminata)
        bool legSteppingL = leftLegTransform != null && leftLegTransform.GetComponent<ProceduralLeg>() != null && leftLegTransform.GetComponent<ProceduralLeg>().IsStepping;
        bool legSteppingR = rightLegTransform != null && rightLegTransform.GetComponent<ProceduralLeg>() != null && rightLegTransform.GetComponent<ProceduralLeg>().IsStepping;

        if (leftGrounded && !rightGrounded && !legSteppingR && tiltAngle < 75f)
        {
            // Pata derecha en el vacío: el mecha pierde apoyo y el peso lo hace inclinarse hacia la derecha
            rb.AddTorque(transform.forward * 18000f, ForceMode.Force);
            rb.AddForce(transform.right * 9000f, ForceMode.Force);
        }
        else if (!leftGrounded && rightGrounded && !legSteppingL && tiltAngle < 75f)
        {
            // Pata izquierda en el vacío: el mecha se desestabiliza hacia la izquierda
            rb.AddTorque(-transform.forward * 18000f, ForceMode.Force);
            rb.AddForce(-transform.right * 9000f, ForceMode.Force);
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
                rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.z);
            }
        }
    }

    private bool CheckFootGrounded(Vector3 probeOrigin, out RaycastHit groundHit, int mask)
    {
        groundHit = default;
        int hitCount = Physics.RaycastNonAlloc(probeOrigin, -transform.up, groundRayHits, legRayLength, mask, QueryTriggerInteraction.Ignore);
        float closestDist = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            var hit = groundRayHits[i];
            if (hit.collider == null) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.distance < closestDist)
            {
                closestDist = hit.distance;
                groundHit = hit;
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
        if (recoveryGraceTimer > 0f)
        {
            recoveryGraceTimer -= Time.deltaTime;
            isOverturned = false;
            return;
        }

        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
        bool wasOverturned = isOverturned;
        isOverturned = tiltAngle > 60f;

        if (isOverturned)
        {
            if (!wasOverturned)
            {
                Debug.Log($"[GAME] ¡MECHA VOLCADO! Inclinación: {tiltAngle:F1}°");
            }

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
            Debug.Log("[GAME] Mecha equilibrado de nuevo");
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
        Debug.Log("[GAME] ¡PATALETA HIDRÁULICA ejecutada! Mecha reincorporado");

        // Impulso vertical y reseteo de velocidad angular
        rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.2f, 18f, rb.linearVelocity.z * 0.2f);
        rb.angularVelocity = Vector3.zero;

        // Orientar erguido en el aire tanto en transform como en Rigidbody
        Quaternion uprightRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        transform.rotation = uprightRot;
        rb.rotation = uprightRot;

        isOverturned = false;
        recoveryGraceTimer = 0.65f;
        overturnPromptTimer = 0f;
    }

    private void AnimateLegsAndArms()
    {
        bool hasProceduralLegs = (leftLegTransform != null && leftLegTransform.GetComponent<ProceduralLeg>() != null);
        if (!hasProceduralLegs)
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
        }

        AnimateArms();
    }

    private void AnimateCockpitSway()
    {
        if (cockpitTransform == null) return;

        if (GetComponent<MechBodyStabilizer>() != null)
        {
            if (shakeIntensity > 0.01f)
            {
                // Vibración armónica basada en trauma cuadrático (game-feel suave y contundente, sin ruido blanco estático)
                float trauma = Mathf.Clamp01(shakeIntensity);
                float shakeMag = trauma * trauma * 0.22f;
                float t = Time.time * 34f;
                float stabShakeX = Mathf.Sin(t * 1.7f) * shakeMag;
                float stabShakeY = Mathf.Cos(t * 2.3f) * shakeMag;
                cockpitTransform.localPosition = initialCockpitLocalPos + new Vector3(stabShakeX, stabShakeY, 0f);
            }
            else
            {
                cockpitTransform.localPosition = initialCockpitLocalPos;
            }
            return;
        }

        float bobY = Mathf.Abs(Mathf.Sin(walkCycle)) * 0.18f;
        float traumaLeg = Mathf.Clamp01(shakeIntensity);
        float shakeMagLeg = traumaLeg * traumaLeg * 0.32f;
        float tLeg = Time.time * 34f;
        float shakeX = Mathf.Sin(tLeg * 1.7f) * shakeMagLeg;
        float shakeY = Mathf.Cos(tLeg * 2.3f) * shakeMagLeg;

        cockpitTransform.localPosition = initialCockpitLocalPos + new Vector3(shakeX, bobY + shakeY, 0f);

        float tiltZ = Mathf.Sin(walkCycle) * 1.8f;
        cockpitTransform.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
    }

    private void AnimateArms()
    {
        // Brazo izquierdo (Ametralladora Gatling)
        if (leftArmTransform != null)
        {
            leftArmTransform.localPosition = initialLeftArmPos - new Vector3(0f, 0f, gatlingRecoilZ);
        }

        // Brazo derecho (Pod Lanzamisiles)
        if (rightArmTransform != null)
        {
            rightArmTransform.localPosition = initialRightArmPos - new Vector3(0f, 0f, missileRecoilZ);
        }
    }

    public void TriggerWeaponAction(string weaponId, Vector3 buttonWorldPos)
    {
        if (isGameOver) return;

        if (weaponId == "GATLING" || weaponId == "PUNCH")
        {
            StopCoroutine(nameof(GatlingBurstRoutine));
            StartCoroutine(GatlingBurstRoutine(buttonWorldPos));
        }
        else if (weaponId == "MISSILES" || weaponId == "LASER")
        {
            if (currentLaserCooldown > 0f)
            {
                FloatingText.Spawn(buttonWorldPos + Vector3.up * 1.5f,
                    $"⏳ RECARGANDO MISILES ({currentLaserCooldown:F1}s)", new Color(0.3f, 0.8f, 1f), 0.3f);
                return;
            }

            currentLaserCooldown = missileCooldownDuration;
            laserTimer = 0.45f;
            StopCoroutine(nameof(MissileSalvoRoutine));
            StartCoroutine(MissileSalvoRoutine(buttonWorldPos));
        }
    }

    private System.Collections.IEnumerator GatlingBurstRoutine(Vector3 buttonPos)
    {
        isGatlingFiring = true;
        punchTimer = 0.5f;
        gatlingRotorSpeed = 1600f;
        shakeIntensity = 0.35f;

        FloatingText.Spawn(buttonPos + Vector3.up * 1.5f, "💥 ¡RÁFAGA GATLING!", new Color(1f, 0.65f, 0.2f), 0.35f);

        int burstRounds = 10;
        float delay = 0.045f;

        for (int i = 0; i < burstRounds; i++)
        {
            FireSingleGatlingShot();
            yield return new WaitForSeconds(delay);
        }

        isGatlingFiring = false;
    }

    private System.Collections.IEnumerator MissileSalvoRoutine(Vector3 buttonPos)
    {
        FloatingText.Spawn(buttonPos + Vector3.up * 1.5f, "🚀 ¡SALVA DE MISILES!", new Color(0.2f, 0.9f, 1f), 0.35f);

        Transform lockedTarget = WindshieldHUDCrosshair.Instance != null ? WindshieldHUDCrosshair.Instance.CurrentLockedTarget : null;

        int numMissiles = (missileLaunchTubes != null && missileLaunchTubes.Length > 0) ? missileLaunchTubes.Length : 4;
        for (int i = 0; i < numMissiles; i++)
        {
            Vector3 spawnPos;
            Quaternion spawnRot = (missilePodTransform != null) ? missilePodTransform.rotation : transform.rotation;

            if (missileLaunchTubes != null && i < missileLaunchTubes.Length && missileLaunchTubes[i] != null)
            {
                spawnPos = missileLaunchTubes[i].position;
                spawnRot = missileLaunchTubes[i].rotation;
            }
            else if (rightArmTransform != null)
            {
                Vector3 offset = new Vector3((i % 2 == 0 ? -0.5f : 0.5f), (i < 2 ? 0.5f : -0.5f), 3.2f);
                spawnPos = rightArmTransform.TransformPoint(offset);
            }
            else
            {
                spawnPos = transform.position + transform.forward * 4f + transform.right * 4f + Vector3.up * 3f;
            }

            MissileProjectile.Spawn(spawnPos, spawnRot, lockedTarget);
            ProceduralAudio.Instance?.PlayMissileLaunch();
            WindshieldHUDCrosshair.Instance?.TriggerRecoil(0.25f);

            missileRecoilZ = Mathf.Clamp(missileRecoilZ + 0.45f, 0f, 1.5f);
            shakeIntensity = Mathf.Max(shakeIntensity, 0.35f);

            yield return new WaitForSeconds(0.085f);
        }
    }

    /// <summary>
    /// Microcongelación de fotogramas (Hit-Stop) según la técnica de game-feel
    /// para maximizar el impacto físico visceral de las armas pesadas.
    /// </summary>
    public void TriggerHitStop(float duration = 0.045f, float slow = 0.05f)
    {
        StopCoroutine(nameof(HitStopRoutine));
        StartCoroutine(HitStopRoutine(duration, slow));
    }

    private System.Collections.IEnumerator HitStopRoutine(float duration, float slow)
    {
        Time.timeScale = slow;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
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
        Debug.Log($"[GAME] Mecha recibió {damage} de daño. HP: {currentHp}/{maxHp}");

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
        Debug.Log($"[GAME] ¡MECHA DESTRUIDO! Score final: {score}");

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
        isGatlingFiring = false;
        isGatlingHolding = false;
        gatlingRotorSpeed = 0f;
        gatlingRecoilZ = 0f;
        missileRecoilZ = 0f;
        shakeIntensity = 0f;
        walkCycle = 0f;
        stepTimer = 0f;
        Debug.Log("[GAME] Partida reiniciada. Mecha restaurado en plataforma inicial");

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
            duckInCockpit.ResetPosition(new Vector3(0f, 0.6f, 0.35f));
        }

        CockpitCameraController.Instance?.ResetCamera();
        MechaHUD.Instance?.HideGameOver();
    }
}
