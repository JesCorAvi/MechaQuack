using UnityEngine;

/// <summary>
/// Proyectil trazador de alta velocidad disparado por la ametralladora rotatoria (Gatling) del Mecha.
/// Vuela a gran velocidad, deja una estela incandescente y detona al impactar contra enemigos o terreno.
/// </summary>
public class GatlingTracer : MonoBehaviour
{
    [Header("Parámetros de Balística")]
    [SerializeField] private float speed = 160f;
    [SerializeField] private float maxDistance = 140f;
    [SerializeField] private float maxLifetime = 1.0f;

    private float lifeTimer = 0f;
    private float distanceTraveled = 0f;
    private static Material tracerMaterial;
    private static Material trailMaterial;

    public static GatlingTracer Spawn(Vector3 origin, Vector3 direction)
    {
        GameObject go = new GameObject("GatlingTracer");
        go.transform.position = origin;
        go.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        if (tracerMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            tracerMaterial = new Material(shader);
            tracerMaterial.color = new Color(1.0f, 0.85f, 0.25f);
        }

        // Trazador visual (Cápsula alargada incandescente)
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bullet.name = "TracerCore";
        bullet.transform.parent = go.transform;
        bullet.transform.localPosition = Vector3.zero;
        bullet.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        bullet.transform.localScale = new Vector3(0.12f, 0.55f, 0.12f);
        bullet.GetComponent<MeshRenderer>().material = tracerMaterial;
        Object.DestroyImmediate(bullet.GetComponent<Collider>());

        // Estela de fuego / Trail
        TrailRenderer tr = go.AddComponent<TrailRenderer>();
        tr.time = 0.12f;
        tr.startWidth = 0.22f;
        tr.endWidth = 0.01f;
        if (trailMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            trailMaterial = new Material(shader);
            trailMaterial.color = new Color(1.0f, 0.65f, 0.15f, 0.9f);
        }
        tr.material = trailMaterial;
        tr.startColor = new Color(1f, 0.85f, 0.2f, 0.95f);
        tr.endColor = new Color(1f, 0.3f, 0.05f, 0f);

        // Pequeño resplandor puntual
        GameObject lightObj = new GameObject("TracerLight");
        lightObj.transform.parent = go.transform;
        lightObj.transform.localPosition = Vector3.zero;
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 3.5f;
        l.intensity = 1.8f;
        l.color = new Color(1f, 0.75f, 0.2f);

        GatlingTracer tracer = go.AddComponent<GatlingTracer>();
        return tracer;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        lifeTimer += dt;
        if (lifeTimer >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        float stepDist = speed * dt;
        Vector3 stepVec = transform.forward * stepDist;

        // Comprobación de impacto por Raycast continuo
        int mask = ~LayerMask.GetMask("Player", "Ignore Raycast");
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, stepDist, mask, QueryTriggerInteraction.Ignore))
        {
            HandleImpact(hit);
            return;
        }

        transform.position += stepVec;
        distanceTraveled += stepDist;

        if (distanceTraveled >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void HandleImpact(RaycastHit hit)
    {
        // Verificar si impactó en un robot enemigo
        var enemy = hit.collider.GetComponentInParent<EnemyBot>();
        if (enemy != null)
        {
            enemy.DestroyByWeapon("GATLING");
            ProceduralAudio.Instance?.PlayPunch();
            VoxelExplosion.Spawn(hit.point, new Color(1f, 0.85f, 0.2f), 8, 0.35f, 8f);
        }
        else
        {
            // Chispas de impacto contra terreno / rocas
            VoxelExplosion.Spawn(hit.point + hit.normal * 0.1f, new Color(1f, 0.7f, 0.15f), 5, 0.2f, 6f);
        }

        Destroy(gameObject);
    }
}
