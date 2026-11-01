using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plataforma Giroscópica de Puntería (Targeting Gyro-Plate) instalada en el Ala de Artillería (P2).
/// Permite al pato apuntar analógicamente la ametralladora Gatling y el pod lanzamisiles
/// corriendo y posicionándose sobre sus cuadrantes (adelante=elevar, atrás=bajar, izq/der=azimut),
/// fijar blancos hostiles automáticamente mediante culazos/pisotones (Lock-On), o recentrar la mira.
/// </summary>
public class GunnerAimPad : CockpitFloorButton
{
    [Header("Mecánica de Puntería Giroscópica")]
    [SerializeField] private Transform padDiscTransform;
    [SerializeField] private Transform[] hydraulicPistons;
    [SerializeField] private float maxTiltAngle = 14f;
    [SerializeField] private float yawSlewSpeed = 38f;
    [SerializeField] private float pitchSlewSpeed = 24f;
    [SerializeField] private float deadzoneRadius = 0.18f;

    [Header("Smart Lock-On (Fijación Táctica)")]
    [SerializeField] private float smartLockRange = 320f;
    [SerializeField] private float smartLockConeAngle = 65f;

    private float servoAudioTimer = 0f;
    private Quaternion initialDiscLocalRot = Quaternion.identity;
    private readonly Vector3[] pistonInitialOffsets = new Vector3[4];

    private void Start()
    {
        buttonId = "AIM_PAD";
        label = "🎯 GIROSCOPIO DE PUNTERÍA";
        type = ButtonType.Hold;
        if (detectionRadius < 0.5f) detectionRadius = 1.75f;

        if (padDiscTransform == null)
        {
            padDiscTransform = transform.Find("Aim_Disc") ?? transform.Find("Cap") ?? transform;
        }

        if (padDiscTransform != null)
        {
            initialDiscLocalRot = padDiscTransform.localRotation;
        }

        if (hydraulicPistons != null)
        {
            for (int i = 0; i < hydraulicPistons.Length && i < 4; i++)
            {
                if (hydraulicPistons[i] != null)
                    pistonInitialOffsets[i] = hydraulicPistons[i].localPosition;
            }
        }
    }

    private void LateUpdate()
    {
        // Si el pato no está sobre la plataforma, amortiguar inclinación hacia la posición horizontal neutral
        if (!isPressed && padDiscTransform != null)
        {
            padDiscTransform.localRotation = Quaternion.Slerp(padDiscTransform.localRotation, initialDiscLocalRot, Time.deltaTime * 8f);
            AnimatePistons(0f, 0f);
        }
    }

    public override void OnDuckStanding(DuckController duck)
    {
        if (duck == null || MechaController.Instance == null) return;

        isPressed = true;

        Vector3 duckLocal = duck.LocalPosition;
        Vector3 padLocal = transform.localPosition;
        Vector2 delta = new Vector2(duckLocal.x - padLocal.x, duckLocal.z - padLocal.z);
        float dist = delta.magnitude;

        // Desviación normalizada (-1 a +1)
        float activeRadius = detectionRadius * 0.85f;
        float normX = Mathf.Clamp(delta.x / activeRadius, -1f, 1f);
        float normZ = Mathf.Clamp(delta.y / activeRadius, -1f, 1f);

        // Zona muerta central para mantener la puntería estable al estar en el centro
        if (Mathf.Abs(normX) < deadzoneRadius) normX = 0f;
        if (Mathf.Abs(normZ) < deadzoneRadius) normZ = 0f;

        // 1. Inclinación física de la plataforma bajo las patas del pato
        float tiltPitch = -normZ * maxTiltAngle;
        float tiltRoll = normX * maxTiltAngle;
        Quaternion targetTilt = initialDiscLocalRot * Quaternion.Euler(tiltPitch, 0f, -tiltRoll);

        if (padDiscTransform != null)
        {
            padDiscTransform.localRotation = Quaternion.Slerp(padDiscTransform.localRotation, targetTilt, Time.deltaTime * 14f);
        }

        AnimatePistons(normX, normZ);

        // 2. Variación angular de puntería en el Mecha
        if (normX != 0f || normZ != 0f)
        {
            float deltaYaw = normX * yawSlewSpeed * Time.deltaTime;
            float deltaPitch = normZ * pitchSlewSpeed * Time.deltaTime;
            MechaController.Instance.AdjustGunnerAim(deltaYaw, deltaPitch);

            // Efecto de sonido de servomotor hidráulico
            servoAudioTimer -= Time.deltaTime;
            if (servoAudioTimer <= 0f)
            {
                ProceduralAudio.Instance?.PlayAimServoTick();
                servoAudioTimer = 0.16f;
            }
        }
    }

    public override void OnDuckExit()
    {
        isPressed = false;
        // La puntería alcanzada se retiene automáticamente en MechaController (Aim Angle Hold)
    }

    public override bool OnDuckStomp(DuckController duck)
    {
        if (MechaController.Instance == null) return false;

        stompCooldownTimer = 0.4f;

        // Búsqueda inteligente de objetivo hostil prioritario (Smart Lock-On)
        Transform bestTarget = FindPrioritaryEnemy();

        if (bestTarget != null)
        {
            Vector3 mechaPos = MechaController.Instance.transform.position + Vector3.up * 28f;
            Vector3 toTarget = (bestTarget.position + Vector3.up * 1.5f) - mechaPos;
            Vector3 localDir = MechaController.Instance.transform.InverseTransformDirection(toTarget.normalized);

            float targetYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            float horizDist = Mathf.Sqrt(localDir.x * localDir.x + localDir.z * localDir.z);
            float targetPitch = Mathf.Atan2(localDir.y, horizDist) * Mathf.Rad2Deg;

            float curYaw = MechaController.Instance.GunnerAimYaw;
            float curPitch = MechaController.Instance.GunnerAimPitch;

            // Si ya estábamos prácticamente apuntando a este blanco, el culazo dispara salva de misiles
            if (Mathf.Abs(targetYaw - curYaw) < 6f && Mathf.Abs(targetPitch - curPitch) < 6f)
            {
                MechaController.Instance.TriggerWeaponAction("MISSILES", transform.position);
                FloatingText.Spawn(transform.position + Vector3.up * 1.6f, "🚀 ¡DISPARO GUIADO AL BLANCO!", new Color(0.2f, 0.9f, 1f), 0.35f);
            }
            else
            {
                MechaController.Instance.SetGunnerAim(targetYaw, targetPitch);
                ProceduralAudio.Instance?.PlayTargetLock();
                FloatingText.Spawn(transform.position + Vector3.up * 1.6f, "🎯 ¡BLANCO FIJADO! (LOCK-ON)", Color.cyan, 0.35f);
            }
        }
        else
        {
            // Sin enemigos en cono frontal: recentrar al frente
            MechaController.Instance.ResetGunnerAim();
            ProceduralAudio.Instance?.PlayButtonClick();
            FloatingText.Spawn(transform.position + Vector3.up * 1.6f, "🎯 ¡MIRA RECENTRADAS (0°, 0°)!", Color.white, 0.35f);
        }

        if (capTransform != null)
        {
            capTransform.localPosition = initialCapLocalPos - Vector3.up * (pressDepth * 1.4f);
        }

        return true;
    }

    private Transform FindPrioritaryEnemy()
    {
        var bots = Object.FindObjectsByType<EnemyBot>(FindObjectsSortMode.None);
        if (bots == null || bots.Length == 0) return null;

        Vector3 mechaPos = MechaController.Instance.transform.position + Vector3.up * 28f;
        Vector3 aimDir = MechaController.Instance.GetGunnerAimDirection();

        Transform bestTarget = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < bots.Length; i++)
        {
            var bot = bots[i];
            if (bot == null) continue;

            Vector3 toBot = bot.transform.position - mechaPos;
            float dist = toBot.magnitude;
            if (dist > smartLockRange) continue;

            float angle = Vector3.Angle(aimDir, toBot);
            if (angle > smartLockConeAngle) continue;

            // Puntuación combinada de ángulo y distancia
            float score = angle * 2.2f + dist * 0.4f;
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = bot.transform;
            }
        }

        return bestTarget;
    }

    private void AnimatePistons(float normX, float normZ)
    {
        if (hydraulicPistons == null || hydraulicPistons.Length < 4) return;

        // Piston 0: Norte (+Z), Piston 1: Sur (-Z), Piston 2: Este (+X), Piston 3: Oeste (-X)
        float compression = 0.12f;
        if (hydraulicPistons[0] != null)
            hydraulicPistons[0].localPosition = pistonInitialOffsets[0] - Vector3.up * (normZ * compression);
        if (hydraulicPistons[1] != null)
            hydraulicPistons[1].localPosition = pistonInitialOffsets[1] + Vector3.up * (normZ * compression);
        if (hydraulicPistons[2] != null)
            hydraulicPistons[2].localPosition = pistonInitialOffsets[2] - Vector3.up * (normX * compression);
        if (hydraulicPistons[3] != null)
            hydraulicPistons[3].localPosition = pistonInitialOffsets[3] + Vector3.up * (normX * compression);
    }
}
