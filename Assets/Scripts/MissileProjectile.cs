using UnityEngine;

/// <summary>
/// Proyectil de micromisil guiado lanzado desde el pod lanzamisiles del Mecha.
/// Acelera hacia adelante, cuenta con ligera autoguía hacia robots enemigos,
/// emite estela de humo y detona con daño en área masivo (AOE).
/// </summary>
public class MissileProjectile : MonoBehaviour
{
    [Header("Parámetros de Vuelo")]
    [SerializeField] private float initialSpeed = 22f;
    [SerializeField] private float maxSpeed = 65f;
    [SerializeField] private float acceleration = 75f;
    [SerializeField] private float homingTurnRate = 120f;
    [SerializeField] private float explosionRadius = 9.5f;
    [SerializeField] private float maxLifetime = 4.5f;

    private float currentSpeed;
    private float lifeTimer = 0f;
    private Transform homingTarget;
    private bool hasExploded = false;

    private TrailRenderer trailRenderer;

    public static MissileProjectile Spawn(Vector3 spawnPos, Quaternion spawnRot, Transform target = null)
    {
        GameObject go = new GameObject("MicroMissile");
        go.transform.position = spawnPos;
        go.transform.rotation = spawnRot;

        // Cuerpo cilíndrico del cohete
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "MissileBody";
        body.transform.parent = go.transform;
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(0.32f, 0.65f, 0.32f);

        // Material del cuerpo (metal oscuro o blanco militar)
        var matBody = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        matBody.color = new Color(0.85f, 0.85f, 0.9f);
        body.GetComponent<MeshRenderer>().material = matBody;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // Ojiva / Punta cónica
        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nose.name = "NoseCone";
        nose.transform.parent = go.transform;
        nose.transform.localPosition = new Vector3(0f, 0f, 0.65f);
        nose.transform.localScale = new Vector3(0.32f, 0.32f, 0.45f);
        var matNose = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        matNose.color = new Color(0.95f, 0.22f, 0.15f); // Ojiva roja
        nose.GetComponent<MeshRenderer>().material = matNose;
        Object.DestroyImmediate(nose.GetComponent<Collider>());

        // 4 Aletas estabilizadoras traseras
        for (int i = 0; i < 4; i++)
        {
            GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fin.name = "Fin_" + i;
            fin.transform.parent = go.transform;
            float angle = i * 90f;
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);
            fin.transform.localPosition = rot * new Vector3(0.24f, 0f, -0.45f);
            fin.transform.localRotation = rot;
            fin.transform.localScale = new Vector3(0.18f, 0.04f, 0.28f);
            fin.GetComponent<MeshRenderer>().material = matBody;
            Object.DestroyImmediate(fin.GetComponent<Collider>());
        }

        // Fuego propulsor / Luz trasera
        GameObject thruster = new GameObject("ThrusterLight");
        thruster.transform.parent = go.transform;
        thruster.transform.localPosition = new Vector3(0f, 0f, -0.65f);
        Light l = thruster.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.55f, 0.15f);
        l.range = 5.5f;
        l.intensity = 2.5f;

        // Estela de humo / Trail
        TrailRenderer tr = go.AddComponent<TrailRenderer>();
        tr.time = 0.45f;
        tr.startWidth = 0.35f;
        tr.endWidth = 0.02f;
        var matTrail = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
        matTrail.color = new Color(1f, 0.65f, 0.2f, 0.85f);
        tr.material = matTrail;
        tr.startColor = new Color(1f, 0.65f, 0.2f, 0.9f);
        tr.endColor = new Color(0.4f, 0.4f, 0.4f, 0f);

        // Componente lógica
        MissileProjectile mp = go.AddComponent<MissileProjectile>();
        mp.homingTarget = target;
        mp.trailRenderer = tr;

        return mp;
    }

    private void Start()
    {
        currentSpeed = initialSpeed;
    }

    private void Update()
    {
        if (hasExploded) return;

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            Explode(transform.position);
            return;
        }

        // Aceleración propulsora
        currentSpeed = Mathf.Min(maxSpeed, currentSpeed + acceleration * Time.deltaTime);

        // Guiado hacia objetivo (si hay uno asignado y está en frente)
        if (homingTarget != null)
        {
            Vector3 toTarget = homingTarget.position - transform.position;
            float angle = Vector3.Angle(transform.forward, toTarget);
            if (angle < 85f && toTarget.sqrMagnitude > 1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, homingTurnRate * Time.deltaTime);
            }
        }

        // Desplazamiento y comprobación de colisión con Raycast para máxima precisión
        float stepDist = currentSpeed * Time.deltaTime;
        int hitMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, stepDist + 0.3f, hitMask, QueryTriggerInteraction.Ignore))
        {
            Explode(hit.point);
        }
        else
        {
            transform.position += transform.forward * stepDist;
        }
    }

    private void Explode(Vector3 explosionPoint)
    {
        if (hasExploded) return;
        hasExploded = true;

        // Efecto visual de explosión de escombros voxel
        VoxelExplosion.Spawn(explosionPoint, new Color(1f, 0.45f, 0.1f), 35, 0.65f, 18f);

        // Sonido de explosión y sacudida de pantalla
        ProceduralAudio.Instance?.PlayExplosion();
        if (MechaController.Instance != null)
        {
            MechaController.Instance.TriggerFootstepShake(0.45f);
        }

        // Daño en área (AOE): buscar robots enemigos en el radio de impacto
        Collider[] colliders = Physics.OverlapSphere(explosionPoint, explosionRadius);
        int hitEnemies = 0;
        for (int i = 0; i < colliders.Length; i++)
        {
            var bot = colliders[i].GetComponentInParent<EnemyBot>();
            if (bot != null)
            {
                bot.DestroyByWeapon("MISSILES");
                hitEnemies++;
            }
        }

        if (hitEnemies > 0)
        {
            FloatingText.Spawn(explosionPoint + Vector3.up * 2.5f, $"💥 SALVA: ¡{hitEnemies} BOTS DESTRUIDOS!", Color.yellow, 0.42f);
            if (MechaController.Instance != null)
            {
                MechaController.Instance.TriggerHitStop(0.045f, 0.05f);
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
