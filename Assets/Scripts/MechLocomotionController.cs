using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coordinador de marcha bípeda procedural (Locomotion Controller).
/// Gestiona la alternancia de pasos (gait), el ritmo proporcional a la velocidad,
/// la prevención de que ambas patas estén en el aire a la vez y la recolocación a postura de reposo.
/// </summary>
public class MechLocomotionController : MonoBehaviour
{
    [Header("Patas Procedurales")]
    [SerializeField] private ProceduralLeg leftLeg;
    [SerializeField] private ProceduralLeg rightLeg;

    [Header("Cadencia y Velocidad de Marcha")]
    [Tooltip("Duración del paso a velocidad baja (paso pesado lento)")]
    [SerializeField] private float maxStepDuration = 0.46f;
    [Tooltip("Duración del paso a velocidad máxima")]
    [SerializeField] private float minStepDuration = 0.28f;
    [Tooltip("Velocidad de referencia para escalar la cadencia")]
    [SerializeField] private float referenceMaxSpeed = 14f;

    [Header("Postura de Reposo")]
    [Tooltip("Tiempo de inactividad antes de recolocar las patas a reposo simétrico")]
    [SerializeField] private float restDelay = 0.35f;
    [Tooltip("Tolerancia de distancia para considerar una pata en postura de reposo")]
    [SerializeField] private float restThreshold = 0.45f;

    [Header("Físicas de Soporte")]
    [SerializeField] private Rigidbody mechaRigidbody;

    // Estado interno
    private int lastSteppedLeg = 1; // 0 = Left, 1 = Right (para alternancia estricta)
    private float stoppedTimer = 0f;
    private bool isMoving = false;

    public ProceduralLeg LeftLeg => leftLeg;
    public ProceduralLeg RightLeg => rightLeg;
    public bool AnyLegStepping => (leftLeg != null && leftLeg.IsStepping) || (rightLeg != null && rightLeg.IsStepping);
    public bool BothLegsGrounded => (leftLeg != null && leftLeg.IsGrounded) && (rightLeg != null && rightLeg.IsGrounded);

    private void Awake()
    {
        if (mechaRigidbody == null)
            mechaRigidbody = GetComponent<Rigidbody>();

        if (leftLeg == null)
        {
            var legs = GetComponentsInChildren<ProceduralLeg>();
            foreach (var l in legs)
            {
                if (l.name.ToLower().Contains("left") || l.transform.localPosition.x < 0)
                    leftLeg = l;
                else
                    rightLeg = l;
            }
        }
    }

    private void Update()
    {
        if (leftLeg == null || rightLeg == null) return;

        Vector3 vel = mechaRigidbody != null ? mechaRigidbody.linearVelocity : Vector3.zero;
        Vector3 planarVel = new Vector3(vel.x, 0f, vel.z);
        float speed = planarVel.magnitude;
        float angSpeed = mechaRigidbody != null ? Mathf.Abs(mechaRigidbody.angularVelocity.y) : 0f;

        isMoving = speed > 0.3f || angSpeed > 0.15f;

        // Calcular duración dinámica del paso según la velocidad actual
        float speedRatio = Mathf.Clamp01(speed / Mathf.Max(0.1f, referenceMaxSpeed));
        float currentDuration = Mathf.Lerp(maxStepDuration, minStepDuration, speedRatio);

        // Coordinación de gait estricto: Bípedo pesado (nunca dos patas en el aire)
        if (!AnyLegStepping)
        {
            if (isMoving)
            {
                stoppedTimer = 0f;
                CoordinateMovingGait(currentDuration);
            }
            else
            {
                stoppedTimer += Time.deltaTime;
                if (stoppedTimer >= restDelay)
                {
                    CoordinateRestStance(maxStepDuration);
                }
            }
        }
    }

    /// <summary>
    /// Coordina qué pata debe dar el siguiente paso durante el desplazamiento.
    /// </summary>
    private void CoordinateMovingGait(float duration)
    {
        bool leftWants = leftLeg.WantsStep(out float leftUrgency);
        bool rightWants = rightLeg.WantsStep(out float rightUrgency);

        if (!leftWants && !rightWants) return;

        // Si ambas quieren dar un paso, priorizar la contraria a la que dio el último paso
        // o la que tenga urgencia significativamente mayor
        if (leftWants && rightWants)
        {
            if (Mathf.Abs(leftUrgency - rightUrgency) > 0.5f)
            {
                if (leftUrgency > rightUrgency)
                    ExecuteStep(leftLeg, 0, duration);
                else
                    ExecuteStep(rightLeg, 1, duration);
            }
            else
            {
                // Alternar rigurosamente
                if (lastSteppedLeg == 1)
                    ExecuteStep(leftLeg, 0, duration);
                else
                    ExecuteStep(rightLeg, 1, duration);
            }
        }
        else if (leftWants)
        {
            ExecuteStep(leftLeg, 0, duration);
        }
        else if (rightWants)
        {
            ExecuteStep(rightLeg, 1, duration);
        }
    }

    /// <summary>
    /// Cuando el Mecha se detiene, recoloca una pata a la vez hacia su posición de descanso ideal.
    /// </summary>
    private void CoordinateRestStance(float duration)
    {
        float leftDist = Vector3.Distance(leftLeg.CurrentFootPosition, leftLeg.HomeTargetPosition);
        float rightDist = Vector3.Distance(rightLeg.CurrentFootPosition, rightLeg.HomeTargetPosition);

        // Recolocar primero la pata que esté más desalineada
        if (leftDist > restThreshold && leftDist >= rightDist)
        {
            ExecuteStep(leftLeg, 0, duration * 1.15f);
        }
        else if (rightDist > restThreshold)
        {
            ExecuteStep(rightLeg, 1, duration * 1.15f);
        }
    }

    private void ExecuteStep(ProceduralLeg leg, int legIndex, float duration)
    {
        leg.UpdateHomeTarget(out Vector3 targetPos, out Vector3 targetNorm);
        leg.TakeStep(targetPos, targetNorm, duration);
        lastSteppedLeg = legIndex;
    }

    /// <summary>
    /// Obtiene el plano medio de soporte definido por la posición de ambos pies.
    /// </summary>
    public bool GetSupportPlane(out Vector3 center, out Vector3 normal, out float footHeightDelta)
    {
        center = Vector3.zero;
        normal = Vector3.up;
        footHeightDelta = 0f;

        if (leftLeg == null || rightLeg == null) return false;

        Vector3 pL = leftLeg.CurrentFootPosition;
        Vector3 pR = rightLeg.CurrentFootPosition;

        center = (pL + pR) * 0.5f;
        footHeightDelta = pR.y - pL.y;

        // Vector normal promedio del terreno bajo los dos pies
        normal = (leftLeg.GroundNormal + rightLeg.GroundNormal).normalized;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (leftLeg != null && rightLeg != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(leftLeg.CurrentFootPosition, rightLeg.CurrentFootPosition);
            Vector3 center = (leftLeg.CurrentFootPosition + rightLeg.CurrentFootPosition) * 0.5f;
            Gizmos.DrawWireSphere(center, 0.2f);
        }
    }
}
