using UnityEngine;

/// <summary>
/// Configuración física y biomecánica centralizada para MechaQuack.
/// Elimina todos los números mágicos y garantiza coherencia dimensional en el motor físico.
/// Incluye calibraciones para el Mecha Colosal (~52m, 44t) y para escala compacta (~5m, 5t).
/// </summary>
[CreateAssetMenu(fileName = "MechaPhysicsConfig", menuName = "Mecha/Physics Configuration", order = 1)]
public class MechaPhysicsConfig : ScriptableObject
{
    [Header("1. Dimensiones y Masa Realista")]
    [Tooltip("Masa total del mecha en kilogramos (48 toneladas = 48000 kg para coloso 50m con torso multinivel de 3 plantas; 5000 kg para mecha 5m)")]
    public float mass = 48000f;
    [Tooltip("Altura total del mecha erguido en metros")]
    public float totalHeight = 50.0f;
    [Tooltip("Altura nominal de la cadera/pelvis sobre el suelo en postura normal (m)")]
    public float nominalRideHeight = 20.5f;
    [Tooltip("Extensión máxima física de la pierna desde la cadera hasta la suela (m). Más allá de esto NO hay contacto.")]
    public float maxLegExtension = 22.0f;
    [Tooltip("Compresión mínima de la pierna en sentadilla de impacto (m)")]
    public float minLegCompression = 14.0f;
    [Tooltip("Posición local del Centro de Masa (CoM) respecto a la raíz de la pelvis (m)")]
    public Vector3 localCenterOfMass = new Vector3(0f, 6.2f, 0.05f);
    [Tooltip("Tensor de inercia explícito (kg·m²) para evitar que colliders mal escalados distorsionen el vuelco")]
    public Vector3 inertiaTensor = new Vector3(880000f, 500000f, 950000f);

    [Header("2. Gravedad y Aerodinámica (Caída Libre Real)")]
    [Tooltip("Aceleración gravitacional estándar (m/s²). Con 9.81, 10m tardan exactamente 1.428s.")]
    public float gravity = 9.81f;
    [Tooltip("Multiplicador de escala de gravedad (1.0 = física terrestre exacta)")]
    public float gravityScale = 1.0f;
    [Tooltip("Resistencia lineal del aire en caída libre (casi cero para no flotar)")]
    public float airborneLinearDamping = 0.015f;
    [Tooltip("Resistencia lineal en el suelo (fricción interna de actuadores)")]
    public float groundedLinearDamping = 0.85f;
    [Tooltip("Amortiguación angular en el aire")]
    public float airborneAngularDamping = 0.15f;
    [Tooltip("Amortiguación angular en el suelo")]
    public float groundedAngularDamping = 1.80f;
    [Tooltip("Velocidad terminal máxima de caída (m/s)")]
    public float terminalVelocity = 65.0f;

    [Header("3. Suspensión Principal (Muelle-Amortiguador en FixedUpdate)")]
    [Tooltip("Rigidez del muelle por pierna (N/m).")]
    public float legSpringStiffness = 1900000f;
    [Tooltip("Amortiguación de la suspensión por pierna (N·s/m)")]
    public float legSpringDamping = 230000f;
    [Tooltip("Tiempo antes de pasar de Airborne a Falling puro (s)")]
    public float fallingStateTimeThreshold = 0.25f;

    [Header("4. Contacto con Terreno y Pendientes")]
    [Tooltip("Radio del SphereCast del pie para detección robusta sin colarse en grietas (m)")]
    public float footProbeRadius = 1.3f;
    [Tooltip("Semi-longitud del pie (del tobillo a la punta/talón) para definir el polígono de soporte (m)")]
    public float footHalfLength = 5.6f;
    [Tooltip("Semi-ancho del pie para definir el polígono de soporte (m)")]
    public float footHalfWidth = 3.4f;
    [Tooltip("Ángulo máximo de pendiente estable sin resbalar (grados)")]
    public float maxStableSlopeAngle = 32.0f;
    [Tooltip("Ángulo crítico de pendiente donde los pies pierden tracción por completo (grados)")]
    public float criticalSlideSlopeAngle = 46.0f;
    [Tooltip("Coeficiente de aceleración de deslizamiento en pendientes pronunciadas")]
    public float slopeSlideAcceleration = 7.5f;

    [Header("5. Modelo de Equilibrio (Péndulo Invertido / XcoM / ZMP)")]
    [Tooltip("Margen de seguridad interno del polígono de soporte (m)")]
    public float stabilityMarginThreshold = 0.65f;
    [Tooltip("Distancia fuera del polígono de soporte a partir de la cual el mecha entra en el Punto de No Retorno")]
    public float pointOfNoReturnDistance = 3.2f;
    [Tooltip("Ángulo de inclinación absoluto más allá del cual los actuadores ceden inevitablemente (grados)")]
    public float criticalTiltAngle = 42.0f;
    [Tooltip("Tiempo máximo que el XcoM puede estar fuera del polígono sin recuperar antes de colapsar (s)")]
    public float maxUnstableDuration = 0.65f;
    [Tooltip("Rigidez proporcional (Kp) del controlador PD de torso erguido (N·m/rad)")]
    public float uprightTorqueKp = 4500000f;
    [Tooltip("Amortiguación derivativa (Kd) del controlador PD de torso erguido (N·m·s/rad)")]
    public float uprightTorqueKd = 480000f;
    [Tooltip("Torque corrector máximo físico que pueden ejercer los tobillos/cadera (N·m). Evita correcciones mágicas.")]
    public float maxAnkleCorrectiveTorque = 1200000f;

    [Header("6. Recuperación Torpe (Pasos de Emergencia y Contrapeso)")]
    [Tooltip("Tiempo de reacción hidráulica/sensorial antes de poder disparar un paso de emergencia (s)")]
    public float recoveryReactionTime = 0.14f;
    [Tooltip("Duración mínima de un paso de emergencia (limitado por la inercia de la pierna pesada) (s)")]
    public float emergencyStepDuration = 0.38f;
    [Tooltip("Ángulo máximo de contrapeso del torso y brazos intentando compensar el CoM (grados)")]
    public float maxCounterweightLeanAngle = 18.0f;
    [Tooltip("Velocidad angular máxima a la que el torso puede inclinarse como contrapeso (grados/s)")]
    public float counterweightAngularSpeed = 55.0f;

    [Header("7. Aterrizaje e Impactos")]
    [Tooltip("Velocidad vertical mínima para activar compresión y tambaleo de aterrizaje (m/s)")]
    public float hardLandingSpeedThreshold = 5.5f;
    [Tooltip("Velocidad vertical de impacto que supera la capacidad de los amortiguadores y provoca caída forzosa (m/s)")]
    public float collapseImpactSpeedThreshold = 26.0f;
    [Tooltip("Inclinación máxima tolerable en el instante de un aterrizaje fuerte antes de volcar (grados)")]
    public float maxLandingTiltAngle = 32.0f;
    [Tooltip("Duración del bloqueo/recuperación de amortiguadores tras un aterrizaje pesado (s)")]
    public float landingRecoveryDuration = 0.65f;

    [Header("8. Caída (Active Ragdoll) y Secuencia de Levantarse")]
    [Tooltip("Multiplicador del par gravitacional de vuelco cuando se pierde el equilibrio")]
    public float gravityToppleMultiplier = 1.35f;
    [Tooltip("Rigidez PD residual de las articulaciones durante la caída (Active Ragdoll pesado)")]
    public float fallenJointSpring = 45000f;
    [Tooltip("Amortiguación PD de las articulaciones durante la caída")]
    public float fallenJointDamping = 8500f;
    [Tooltip("Tiempo mínimo que el mecha permanece caído en el suelo tras el impacto antes de poder levantarse (s)")]
    public float minFallenStunTime = 1.6f;
    [Tooltip("Duración total de la secuencia procedural pesada para levantarse del suelo (s)")]
    public float getUpSequenceDuration = 3.2f;

    [Header("9. Locomoción Voluntaria")]
    [Tooltip("Fuerza máxima de propulsión horizontal en marcha (N)")]
    public float moveForce = 1200000f;
    [Tooltip("Torque máximo de giro horizontal (N·m)")]
    public float turnTorque = 900000f;
    [Tooltip("Velocidad lineal máxima de caminata (m/s)")]
    public float maxWalkSpeed = 22.0f;

    [ContextMenu("Set 50m Scale (MechaQuack 50m Titan Colossus)")]
    public void Set50mColossusScale()
    {
        mass = 48000f;
        totalHeight = 50.0f;
        nominalRideHeight = 20.5f;
        maxLegExtension = 22.0f;
        minLegCompression = 14.0f;
        localCenterOfMass = new Vector3(0f, 6.2f, 0.05f);
        inertiaTensor = new Vector3(880000f, 500000f, 950000f);

        gravity = 9.81f;
        gravityScale = 1.0f;
        airborneLinearDamping = 0.015f;
        groundedLinearDamping = 0.85f;
        airborneAngularDamping = 0.15f;
        groundedAngularDamping = 1.80f;
        terminalVelocity = 65.0f;

        legSpringStiffness = 1900000f;
        legSpringDamping = 230000f;
        fallingStateTimeThreshold = 0.25f;

        footProbeRadius = 1.3f;
        footHalfLength = 5.6f;
        footHalfWidth = 3.6f;
        maxStableSlopeAngle = 32.0f;
        criticalSlideSlopeAngle = 46.0f;
        slopeSlideAcceleration = 7.5f;

        stabilityMarginThreshold = 0.65f;
        pointOfNoReturnDistance = 3.2f;
        criticalTiltAngle = 42.0f;
        maxUnstableDuration = 0.65f;
        uprightTorqueKp = 4600000f;
        uprightTorqueKd = 490000f;
        maxAnkleCorrectiveTorque = 1250000f;

        recoveryReactionTime = 0.14f;
        emergencyStepDuration = 0.38f;
        maxCounterweightLeanAngle = 18.0f;
        counterweightAngularSpeed = 55.0f;

        hardLandingSpeedThreshold = 5.5f;
        collapseImpactSpeedThreshold = 26.0f;
        maxLandingTiltAngle = 32.0f;
        landingRecoveryDuration = 0.65f;

        gravityToppleMultiplier = 1.35f;
        fallenJointSpring = 45000f;
        fallenJointDamping = 8500f;
        minFallenStunTime = 1.6f;
        getUpSequenceDuration = 3.2f;

        moveForce = 1250000f;
        turnTorque = 950000f;
        maxWalkSpeed = 22.0f;
    }

    // Mantener alias para compatibilidad
    [ContextMenu("Set 52m Scale")]
    public void Set52mColossusScale() => Set50mColossusScale();

    [ContextMenu("Set 5m Scale (Small Mecha Prototype)")]
    public void Set5mPrototypeScale()
    {
        mass = 5000f;
        totalHeight = 5.0f;
        nominalRideHeight = 2.60f;
        maxLegExtension = 2.90f;
        minLegCompression = 1.45f;
        localCenterOfMass = new Vector3(0f, 0.85f, 0.05f);
        inertiaTensor = new Vector3(6800f, 4200f, 7100f);

        gravity = 9.81f;
        gravityScale = 1.0f;
        airborneLinearDamping = 0.015f;
        groundedLinearDamping = 0.85f;
        airborneAngularDamping = 0.15f;
        groundedAngularDamping = 1.80f;
        terminalVelocity = 55.0f;

        legSpringStiffness = 185000f;
        legSpringDamping = 28000f;
        fallingStateTimeThreshold = 0.25f;

        footProbeRadius = 0.32f;
        footHalfLength = 0.65f;
        footHalfWidth = 0.42f;
        maxStableSlopeAngle = 32.0f;
        criticalSlideSlopeAngle = 46.0f;
        slopeSlideAcceleration = 7.5f;

        stabilityMarginThreshold = 0.08f;
        pointOfNoReturnDistance = 0.48f;
        criticalTiltAngle = 34.0f;
        maxUnstableDuration = 0.55f;
        uprightTorqueKp = 260000f;
        uprightTorqueKd = 42000f;
        maxAnkleCorrectiveTorque = 68000f;

        recoveryReactionTime = 0.14f;
        emergencyStepDuration = 0.34f;
        maxCounterweightLeanAngle = 18.0f;
        counterweightAngularSpeed = 55.0f;

        hardLandingSpeedThreshold = 5.5f;
        collapseImpactSpeedThreshold = 15.8f;
        maxLandingTiltAngle = 20.0f;
        landingRecoveryDuration = 0.65f;

        gravityToppleMultiplier = 1.35f;
        fallenJointSpring = 4500f;
        fallenJointDamping = 850f;
        minFallenStunTime = 1.6f;
        getUpSequenceDuration = 3.2f;

        moveForce = 42000f;
        turnTorque = 28000f;
        maxWalkSpeed = 5.5f;
    }
}
