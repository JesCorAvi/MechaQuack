using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generador de oleadas de robots enemigos alrededor del Mecha.
/// Construye la geometría de los bots proceduralmente mediante primitivas para no depender de assets externos.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Configuración de Oleadas")]
    [SerializeField] private float minSpawnDistance = 85f;
    [SerializeField] private float maxSpawnDistance = 115f;
    [SerializeField] private int maxEnemies = 18;
    [SerializeField] private float baseSpawnInterval = 2.5f;
    [SerializeField] private bool isSpawningEnabled = false; // Por defecto en modo exploración hasta activar el botón

    private float spawnTimer = 1.0f;
    private Material enemyRedMat;
    private Material enemyDarkMat;
    private Material enemyEyeMat;

    private readonly List<EnemyBot> activeEnemies = new List<EnemyBot>();

    public IReadOnlyList<EnemyBot> ActiveEnemies => activeEnemies;
    public bool IsSpawningEnabled => isSpawningEnabled;

    private void Awake()
    {
        Instance = this;
        CreateMaterials();
    }

    private void CreateMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        enemyRedMat = new Material(urpLit);
        enemyRedMat.color = new Color(0.85f, 0.15f, 0.15f);
        if (enemyRedMat.HasProperty("_BaseColor")) enemyRedMat.SetColor("_BaseColor", enemyRedMat.color);

        enemyDarkMat = new Material(urpLit);
        enemyDarkMat.color = new Color(0.12f, 0.14f, 0.18f);
        if (enemyDarkMat.HasProperty("_BaseColor")) enemyDarkMat.SetColor("_BaseColor", enemyDarkMat.color);

        enemyEyeMat = new Material(urpLit);
        enemyEyeMat.color = new Color(1.0f, 0.8f, 0.1f);
        if (enemyEyeMat.HasProperty("_BaseColor")) enemyEyeMat.SetColor("_BaseColor", enemyEyeMat.color);
        if (enemyEyeMat.HasProperty("_EmissionColor"))
        {
            enemyEyeMat.SetColor("_EmissionColor", enemyEyeMat.color * 1.5f);
            enemyEyeMat.EnableKeyword("_EMISSION");
        }
    }

    private void Start()
    {
        MechaHUD.Instance?.UpdateSpawnerStatus(isSpawningEnabled);
    }

    /// <summary>
    /// Alterna el estado de invasión enemiga. Activado por el botón físico de cabina o el HUD.
    /// </summary>
    public bool ToggleSpawning()
    {
        isSpawningEnabled = !isSpawningEnabled;

        if (isSpawningEnabled)
        {
            ProceduralAudio.Instance?.PlayAlarm();
            Debug.Log("[GAME] Modo Invasión de Robots: ACTIVADA");
            SpawnEnemy();
            SpawnEnemy();
            spawnTimer = 2.0f;

            if (MechaController.Instance != null)
            {
                FloatingText.Spawn(MechaController.Instance.transform.position + Vector3.up * 8f, "¡ALERTA! INVASIÓN ROBOT ACTIVADA", Color.red, 0.42f);
            }
        }
        else
        {
            ProceduralAudio.Instance?.PlayButtonClick();
            Debug.Log("[GAME] Modo Invasión de Robots: PAUSADA");
            if (MechaController.Instance != null)
            {
                FloatingText.Spawn(MechaController.Instance.transform.position + Vector3.up * 8f, "EXPLORACIÓN: INVASIÓN PAUSADA", Color.cyan, 0.42f);
            }
        }

        MechaHUD.Instance?.UpdateSpawnerStatus(isSpawningEnabled);
        return isSpawningEnabled;
    }

    private void Update()
    {
        // Limpiar lista de nulos
        activeEnemies.RemoveAll(e => e == null);

        if (MechaController.Instance == null || MechaController.Instance.IsGameOver || !isSpawningEnabled)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f && activeEnemies.Count < maxEnemies)
        {
            SpawnEnemy();
            float difficultyScale = Mathf.Clamp(MechaController.Instance.Score * 0.03f, 0f, 1.4f);
            spawnTimer = Mathf.Max(1.0f, baseSpawnInterval - difficultyScale);
        }
    }

    public void SpawnEnemy()
    {
        if (MechaController.Instance == null) return;

        Vector3 mechaPos = MechaController.Instance.transform.position;
        float angleOffset = Random.Range(-0.55f, 0.55f) * Mathf.PI;
        float mechaYaw = MechaController.Instance.transform.eulerAngles.y * Mathf.Deg2Rad;
        float spawnAngle = mechaYaw + angleOffset;
        float dist = Random.Range(minSpawnDistance, maxSpawnDistance);

        Vector3 spawnPos = new Vector3(
            mechaPos.x - Mathf.Sin(spawnAngle) * dist,
            0f,
            mechaPos.z - Mathf.Cos(spawnAngle) * dist
        );

        // Proyectar altura al terreno
        if (Physics.Raycast(new Vector3(spawnPos.x, 80f, spawnPos.z), Vector3.down, out RaycastHit groundHit, 150f))
        {
            spawnPos.y = groundHit.point.y;
        }

        GameObject botObj = new GameObject("EnemyBot_" + Time.frameCount);
        botObj.transform.position = spawnPos;
        Debug.Log($"[GAME] Robot enemigo generado en ({spawnPos.x:F1}, {spawnPos.y:F1}, {spawnPos.z:F1})");

        // Torso
        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torso.name = "Torso";
        torso.transform.parent = botObj.transform;
        torso.transform.localPosition = new Vector3(0f, 3.5f, 0f);
        torso.transform.localScale = new Vector3(4.5f, 5.5f, 4.5f);
        if (enemyRedMat != null) torso.GetComponent<MeshRenderer>().material = enemyRedMat;
        Destroy(torso.GetComponent<Collider>());

        // Visor brillante
        GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visor.name = "Visor";
        visor.transform.parent = torso.transform;
        visor.transform.localPosition = new Vector3(0f, 0.2f, 0.5f);
        visor.transform.localScale = new Vector3(0.8f, 0.2f, 0.15f);
        if (enemyEyeMat != null) visor.GetComponent<MeshRenderer>().material = enemyEyeMat;
        Destroy(visor.GetComponent<Collider>());

        // Piernas
        GameObject legL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        legL.name = "Leg_L";
        legL.transform.parent = botObj.transform;
        legL.transform.localPosition = new Vector3(-1.3f, 1.1f, 0f);
        legL.transform.localScale = new Vector3(1.4f, 2.2f, 1.8f);
        if (enemyDarkMat != null) legL.GetComponent<MeshRenderer>().material = enemyDarkMat;
        Destroy(legL.GetComponent<Collider>());

        GameObject legR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        legR.name = "Leg_R";
        legR.transform.parent = botObj.transform;
        legR.transform.localPosition = new Vector3(1.3f, 1.1f, 0f);
        legR.transform.localScale = new Vector3(1.4f, 2.2f, 1.8f);
        if (enemyDarkMat != null) legR.GetComponent<MeshRenderer>().material = enemyDarkMat;
        Destroy(legR.GetComponent<Collider>());

        EnemyBot bot = botObj.AddComponent<EnemyBot>();
        float botSpeed = Random.Range(7.5f, 11.0f);
        bot.Initialize(MechaController.Instance, botSpeed);

        activeEnemies.Add(bot);
    }

    public void ClearAllEnemies()
    {
        foreach (var bot in activeEnemies)
        {
            if (bot != null) Destroy(bot.gameObject);
        }
        activeEnemies.Clear();
    }
}
