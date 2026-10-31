using UnityEngine;

/// <summary>
/// Robot enemigo exterior amenazante que avanza hacia el Pato-Mecha.
/// </summary>
public class EnemyBot : MonoBehaviour
{
    [Header("Parámetros")]
    public float speed = 8.5f;
    public float attackDistance = 8.5f;
    public int damageToMecha = 14;

    private Transform mechaTransform;
    private MechaController mechaController;
    private float walkCycle = 0f;
    private Transform visualTorso;

    public void Initialize(MechaController mecha, float moveSpeed)
    {
        mechaController = mecha;
        if (mecha != null) mechaTransform = mecha.transform;
        speed = moveSpeed;
        walkCycle = Random.Range(0f, 10f);
    }

    private void Awake()
    {
        visualTorso = transform.Find("Torso") ?? transform;
    }

    private void Update()
    {
        if (mechaTransform == null)
        {
            if (MechaController.Instance != null)
            {
                Initialize(MechaController.Instance, speed);
            }
            return;
        }

        Vector3 targetPos = mechaTransform.position;
        Vector3 dir = (targetPos - transform.position);
        dir.y = 0f;
        float dist = dir.magnitude;

        if (dist > 0.1f)
        {
            Vector3 moveDir = dir.normalized;
            transform.position += moveDir * (speed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(moveDir, Vector3.up);
        }

        // Animación de caminata robótica (pequeño rebote)
        walkCycle += Time.deltaTime * 6f;
        if (visualTorso != null)
        {
            visualTorso.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(walkCycle)) * 0.5f, 0f);
        }

        // Colisión/Impacto contra el Mecha
        if (dist <= attackDistance)
        {
            ExplodeAndDamageMecha();
        }
    }

    private void ExplodeAndDamageMecha()
    {
        if (mechaController != null)
        {
            mechaController.TakeDamage(damageToMecha);
        }

        VoxelExplosion.Spawn(transform.position + Vector3.up * 2f, new Color(0.95f, 0.25f, 0.25f), 18, 0.5f, 12f);
        ProceduralAudio.Instance?.PlayPunch();
        Destroy(gameObject);
    }

    /// <summary>
    /// Destrucción causada por armas del Mecha (Puño o Láser).
    /// </summary>
    public void DestroyByWeapon(string weaponType)
    {
        Color debrisColor = weaponType == "LASER" ? new Color(0.2f, 0.8f, 1f) : new Color(1f, 0.3f, 0.4f);
        VoxelExplosion.Spawn(transform.position + Vector3.up * 2f, debrisColor, 20, 0.55f, 14f);
        ProceduralAudio.Instance?.PlayExplosion();

        if (MechaController.Instance != null)
        {
            MechaController.Instance.AddScore(1);
        }

        Destroy(gameObject);
    }
}
