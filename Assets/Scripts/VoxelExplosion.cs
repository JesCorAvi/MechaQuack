using UnityEngine;

/// <summary>
/// Sistema de explosiones de cubos low-poly/voxel procedurales.
/// No requiere sistemas de partículas complejos ni texturas externas.
/// </summary>
public class VoxelExplosion : MonoBehaviour
{
    private static Material sharedDebrisMaterial;

    public static void Spawn(Vector3 position, Color color, int count = 16, float size = 0.35f, float force = 8f)
    {
        GameObject explosionRoot = new GameObject("VoxelExplosion");
        explosionRoot.transform.position = position;

        if (sharedDebrisMaterial == null)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            sharedDebrisMaterial = new Material(urpLit);
        }

        Material instancedMat = new Material(sharedDebrisMaterial);
        instancedMat.color = color;
        if (instancedMat.HasProperty("_BaseColor")) instancedMat.SetColor("_BaseColor", color);

        for (int i = 0; i < count; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.parent = explosionRoot.transform;
            cube.transform.position = position + Random.insideUnitSphere * 0.4f;
            cube.transform.localScale = Vector3.one * (size * Random.Range(0.7f, 1.3f));

            MeshRenderer mr = cube.GetComponent<MeshRenderer>();
            mr.material = instancedMat;

            // Rigidbody físico para que reboten cómicamente
            Rigidbody rb = cube.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            Vector3 randomDir = Random.insideUnitSphere + Vector3.up * 0.8f;
            rb.linearVelocity = randomDir.normalized * Random.Range(force * 0.5f, force * 1.5f);
            rb.angularVelocity = Random.insideUnitSphere * 15f;

            // Destruir collider para rendimiento óptimo
            Collider col = cube.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        explosionRoot.AddComponent<ExplosionCleaner>();
    }

    private class ExplosionCleaner : MonoBehaviour
    {
        private float life = 0.75f;
        private float timer = 0f;
        private Vector3 initialScale;

        private void Start()
        {
            initialScale = transform.localScale;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float progress = timer / life;

            // Encoger suavemente al expirar
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, progress);

            if (timer >= life)
            {
                Destroy(gameObject);
            }
        }
    }
}
