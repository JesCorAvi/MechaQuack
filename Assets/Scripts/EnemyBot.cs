using UnityEngine;

/// <summary>
/// Robot enemigo exterior amenazante que avanza hacia MechaQuack.
/// </summary>
public class EnemyBot : MonoBehaviour
{
    [Header("Parámetros")]
    public float speed = 8.5f;
    public float attackDistance = 16.0f;
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

            // Ajustar altura al desnivel del terreno
            if (Physics.Raycast(transform.position + Vector3.up * 6f, Vector3.down, out RaycastHit groundHit, 16f))
            {
                Vector3 p = transform.position;
                p.y = Mathf.Lerp(p.y, groundHit.point.y, Time.deltaTime * 10f);
                transform.position = p;
            }
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
    /// Destrucción causada por armas del Mecha (Gatling, Misiles, Puño o Láser).
    /// </summary>
    public void DestroyByWeapon(string weaponType)
    {
        Color debrisColor;
        switch (weaponType)
        {
            case "GATLING": debrisColor = new Color(1.0f, 0.75f, 0.2f); break;
            case "MISSILES": debrisColor = new Color(1.0f, 0.35f, 0.12f); break;
            case "LASER": debrisColor = new Color(0.2f, 0.8f, 1f); break;
            default: debrisColor = new Color(1f, 0.3f, 0.4f); break;
        }
        VoxelExplosion.Spawn(transform.position + Vector3.up * 2f, debrisColor, 20, 0.55f, 14f);
        ProceduralAudio.Instance?.PlayExplosion();

        if (MechaController.Instance != null)
        {
            MechaController.Instance.AddScore(1);
        }

        Destroy(gameObject);
    }
}
