using UnityEngine;

/// <summary>
/// Estabilizador de chasis y suspensión biomecánica del Mecha.
/// Ajusta la altura del cuerpo según la media de los pies, inclina el torso (pitch/roll)
/// adaptándose al plano de apoyo del terreno, añade balanceo de peso (bobbing) e inercia
/// en aceleraciones/frenadas con amortiguación tipo muelle para una cámara suave.
/// </summary>
public class MechBodyStabilizer : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private MechLocomotionController locomotionController;
    [SerializeField] private Rigidbody mechaRigidbody;
    [SerializeField] private Transform chassisTransform;
    [SerializeField] private Transform cockpitTransform;

    [Header("Altura y Suspensión")]
    [Tooltip("Altura objetivo del cuerpo sobre el plano medio de los pies")]
    [SerializeField] private float baseRideHeight = 11.8f;
    [Tooltip("Tiempo de suavizado para ajuste vertical de altura (SmoothDamp)")]
    [SerializeField] private float heightSmoothTime = 0.18f;

    [Header("Inclinación de Torso (Pitch y Roll)")]
    [Tooltip("Inclinación máxima permitida por desnivel del terreno (grados)")]
    [SerializeField] private float maxTerrainTilt = 25f;
    [Tooltip("Tiempo de suavizado para rotación según el terreno")]
    [SerializeField] private float tiltSmoothTime = 0.16f;

    [Header("Inercia y Dinámica de Peso")]
    [Tooltip("Inclinación longitudinal al acelerar o frenar")]
    [SerializeField] private float accelLeanFactor = 0.85f;
    [Tooltip("Inclinación transversal al girar (centrífuga)")]
    [SerializeField] private float turnLeanFactor = 0.6f;
    [Tooltip("Amplitud del balanceo vertical de la marcha (bobbing)")]
    [SerializeField] private float walkBobAmplitude = 0.12f;
    [Tooltip("Amplitud del balanceo lateral de la marcha (sway)")]
    [SerializeField] private float walkSwayAngle = 1.4f;

    // Estado interno de amortiguación
    private float currentHeightVelocity = 0f;
    private float currentPitchVelocity = 0f;
    private float currentRollVelocity = 0f;

    private float smoothHeight = 0f;
    private float smoothPitch = 0f;
    private float smoothRoll = 0f;

    private Vector3 lastVelocity = Vector3.zero;
    private Vector3 currentAccel = Vector3.zero;
    private float walkCycle = 0f;

    private Vector3 initialChassisLocalPos;
    private Quaternion initialChassisLocalRot;

    public float SmoothPitch => smoothPitch;
    public float SmoothRoll => smoothRoll;

    private void Awake()
    {
        if (locomotionController == null)
            locomotionController = GetComponent<MechLocomotionController>();

        if (mechaRigidbody == null)
            mechaRigidbody = GetComponent<Rigidbody>();

        if (chassisTransform == null)
        {
            Transform p = transform.Find("Pelvis");
            chassisTransform = p != null ? p : transform;
        }

        if (cockpitTransform == null)
        {
            cockpitTransform = transform.Find("Cockpit");
        }

        initialChassisLocalPos = chassisTransform != null ? chassisTransform.localPosition : Vector3.zero;
        initialChassisLocalRot = chassisTransform != null ? chassisTransform.localRotation : Quaternion.identity;
        smoothHeight = baseRideHeight;
    }

    private void LateUpdate()
    {
        if (locomotionController == null) return;

        UpdateAcceleration();
        CalculateTargetHeightAndTilt(out float targetHeight, out float targetPitch, out float targetRoll);
        ApplyStabilizedTransform(targetHeight, targetPitch, targetRoll, Time.deltaTime);
    }

    private void UpdateAcceleration()
    {
        if (mechaRigidbody != null)
        {
            Vector3 vel = mechaRigidbody.linearVelocity;
            currentAccel = (vel - lastVelocity) / Mathf.Max(Time.deltaTime, 0.001f);
            lastVelocity = vel;
        }
    }

    private void CalculateTargetHeightAndTilt(out float targetHeight, out float targetPitch, out float targetRoll)
    {
        targetHeight = baseRideHeight;
        targetPitch = 0f;
        targetRoll = 0f;

        if (locomotionController.GetSupportPlane(out Vector3 footCenter, out Vector3 avgNormal, out float heightDelta))
        {
            // 1. Altura base relativa al plano medio de los pies
            targetHeight = baseRideHeight;

            // 2. Roll (inclinación lateral por diferencia de altura entre pies)
            // Ancho aproximado entre patas = 6.2m
            float rollRad = Mathf.Atan2(heightDelta, 6.2f);
            float terrainRoll = Mathf.Clamp(rollRad * Mathf.Rad2Deg, -maxTerrainTilt, maxTerrainTilt);

            // 3. Pitch (inclinación longitudinal según la normal del terreno)
            Vector3 localNormal = transform.InverseTransformDirection(avgNormal);
            float terrainPitch = Mathf.Clamp(-localNormal.z * 45f, -maxTerrainTilt, maxTerrainTilt);

            // 4. Inercia de aceleración y frenada
            Vector3 localAccel = transform.InverseTransformDirection(currentAccel);
            float accelPitch = Mathf.Clamp(-localAccel.z * accelLeanFactor * 0.1f, -6f, 6f);
            float turnRoll = Mathf.Clamp(-localAccel.x * turnLeanFactor * 0.1f, -5f, 5f);

            // 5. Balanceo y bobbing cuando las patas se mueven
            float bobOffset = 0f;
            float swayOffset = 0f;
            if (locomotionController.AnyLegStepping || (mechaRigidbody != null && mechaRigidbody.linearVelocity.magnitude > 0.5f))
            {
                walkCycle += Time.deltaTime * 6.5f;
                bobOffset = Mathf.Sin(walkCycle * 2f) * walkBobAmplitude;
                swayOffset = Mathf.Sin(walkCycle) * walkSwayAngle;
            }
            else
            {
                walkCycle = 0f;
            }

            targetHeight += bobOffset;
            targetPitch = terrainPitch + accelPitch;
            targetRoll = terrainRoll + turnRoll + swayOffset;
        }
    }

    private void ApplyStabilizedTransform(float targetHeight, float targetPitch, float targetRoll, float deltaTime)
    {
        // Amortiguación suave tipo muelle-amortiguador
        smoothHeight = Mathf.SmoothDamp(smoothHeight, targetHeight, ref currentHeightVelocity, heightSmoothTime, Mathf.Infinity, deltaTime);
        smoothPitch = Mathf.SmoothDampAngle(smoothPitch, targetPitch, ref currentPitchVelocity, tiltSmoothTime, Mathf.Infinity, deltaTime);
        smoothRoll = Mathf.SmoothDampAngle(smoothRoll, targetRoll, ref currentRollVelocity, tiltSmoothTime, Mathf.Infinity, deltaTime);

        Quaternion stabilizedRot = Quaternion.Euler(smoothPitch, 0f, smoothRoll);

        // Aplicar inclinación y compensación a la pelvis
        if (chassisTransform != null && chassisTransform != transform)
        {
            chassisTransform.localRotation = initialChassisLocalRot * stabilizedRot;
        }

        // Aplicar a la cabina con suavizado adicional para evitar cualquier vibración en cámara
        if (cockpitTransform != null)
        {
            // La cabina recibe una versión sutilmente más amortiguada para confort del jugador
            cockpitTransform.localRotation = Quaternion.Euler(smoothPitch * 0.65f, 0f, smoothRoll * 0.65f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (locomotionController != null && locomotionController.GetSupportPlane(out Vector3 center, out Vector3 norm, out _))
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(center, norm * 2f);
            Gizmos.DrawWireCube(center, new Vector3(6.5f, 0.1f, 3.5f));
        }
    }
}
