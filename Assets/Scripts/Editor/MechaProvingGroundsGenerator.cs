using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Generador procedural avanzado del Megaterreno de Pruebas (Mecha Proving Grounds) para MechaQuack.
/// Diseñado siguiendo los principios de 'level-design', 'procedural-gen', 'craft-game-floors' y 'physics-tuning'.
/// 
/// Todos los arcos, letreros y balizas tienen colisionadores desactivados y holguras monumentales (26m+ de altura
/// y 46m+ de anchura) para que el mecha gigante (13-14m de alto con cabina y 18m de envergadura con armas)
/// se desplace con fluidez total y sin ningún tipo de bloqueo ni tropiezo.
/// </summary>
public static class MechaProvingGroundsGenerator
{
    private const int WORLD_SEED = 2026;

    public class MaterialPalette
    {
        public Material matAsphalt;
        public Material matRock;
        public Material matRubble;
        public Material matHazard;
        public Material matAcid;
        public Material matWall;
        public Material matCyan;
        public Material matAmber;
        public Material matRed;
        public Material matBlue;
        public Material matBase;

        public static MaterialPalette LoadOrCreate()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material CreateMat(string path, Color col, float metallic = 0.2f, float smoothness = 0.5f, bool isEmission = false)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(urpLit);
                    AssetDatabase.CreateAsset(m, path);
                }
                m.shader = urpLit;
                m.color = col;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
                if (isEmission && m.HasProperty("_EmissionColor"))
                {
                    m.SetColor("_EmissionColor", col * 0.9f);
                    m.EnableKeyword("_EMISSION");
                }
                EditorUtility.SetDirty(m);
                return m;
            }

            var pal = new MaterialPalette();
            pal.matAsphalt = CreateMat("Assets/Materials/M_Asphalt.mat", new Color(0.13f, 0.16f, 0.22f), 0.25f, 0.45f);
            pal.matRock = CreateMat("Assets/Materials/M_WastelandRock.mat", new Color(0.36f, 0.26f, 0.22f), 0.1f, 0.2f);
            pal.matRubble = CreateMat("Assets/Materials/M_Rubble.mat", new Color(0.48f, 0.45f, 0.42f), 0.12f, 0.25f);
            pal.matHazard = CreateMat("Assets/Materials/M_HazardStripe.mat", new Color(0.96f, 0.72f, 0.05f), 0.2f, 0.8f, true);
            pal.matAcid = CreateMat("Assets/Materials/M_ToxicAcid.mat", new Color(0.18f, 0.95f, 0.18f, 0.92f), 0.1f, 0.95f, true);
            pal.matWall = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CockpitWall.mat") ??
                          CreateMat("Assets/Materials/M_CockpitWall.mat", new Color(0.24f, 0.27f, 0.32f), 0.3f, 0.4f);
            pal.matCyan = CreateMat("Assets/Materials/M_BtnCyan.mat", new Color(0.02f, 0.71f, 0.83f), 0.1f, 0.7f, true);
            pal.matAmber = CreateMat("Assets/Materials/M_BtnAmber.mat", new Color(0.96f, 0.62f, 0.04f), 0.1f, 0.7f, true);
            pal.matRed = CreateMat("Assets/Materials/M_MechaRed.mat", new Color(0.88f, 0.11f, 0.28f), 0.4f, 0.5f);
            pal.matBlue = CreateMat("Assets/Materials/M_MechaBlue.mat", new Color(0.14f, 0.39f, 0.92f), 0.4f, 0.5f);
            pal.matBase = CreateMat("Assets/Materials/M_BtnBase.mat", new Color(0.06f, 0.09f, 0.16f), 0.5f, 0.4f);
            return pal;
        }
    }

    [MenuItem("Mecha/Generate Mega Proving Grounds (Terreno Extendido)")]
    [MenuItem("PatoJuego/Generate Mega Proving Grounds")]
    public static void GenerateFromMenu()
    {
        var existing = GameObject.Find("OutsideCityView");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject outside = new GameObject("OutsideCityView");
        Undo.RegisterCreatedObjectUndo(outside, "Generate Mega Proving Grounds");

        var pal = MaterialPalette.LoadOrCreate();
        BuildWorld(outside.transform, pal);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("✅ [PROVING GROUNDS] ¡Megaterreno de Pruebas generado con éxito (600m x 600m, 8 sectores de movimiento sin bloqueos de arcos)! Pulsa Play para explorar.");
    }

    /// <summary>
    /// Construye la totalidad de la geografía, sectores, calzadas y desafíos físicos del megamapa.
    /// </summary>
    public static void BuildWorld(Transform root, MaterialPalette pal)
    {
        var rng = new System.Random(WORLD_SEED);

        // --- 0. CIMIENTOS GLOBALES Y BARRERAS DEL HORIZONTE ---
        Transform tBedrock = CreateSubGroup("00_WorldBedrockAndPerimeter", root);
        CreateBlock("Bedrock_Foundation", tBedrock, new Vector3(0f, -14.5f, 50f), new Vector3(720f, 2f, 720f), Quaternion.identity, pal.matRock);

        // Muros perimetrales gigantes tipo cañón exterior (horizonte)
        CreateBlock("PerimeterWall_North", tBedrock, new Vector3(0f, 15f, 400f), new Vector3(740f, 60f, 20f), Quaternion.identity, pal.matRock);
        CreateBlock("PerimeterWall_South", tBedrock, new Vector3(0f, 15f, -300f), new Vector3(740f, 60f, 20f), Quaternion.identity, pal.matRock);
        CreateBlock("PerimeterWall_East", tBedrock, new Vector3(360f, 15f, 50f), new Vector3(20f, 60f, 720f), Quaternion.identity, pal.matRock);
        CreateBlock("PerimeterWall_West", tBedrock, new Vector3(-360f, 15f, 50f), new Vector3(20f, 60f, 720f), Quaternion.identity, pal.matRock);

        // Picos de roca lejanos decorativos en las esquinas
        CreateRockSpire(tBedrock, new Vector3(-310f, 25f, 340f), 18f, 55f, pal.matRock);
        CreateRockSpire(tBedrock, new Vector3(310f, 28f, 340f), 22f, 65f, pal.matRock);
        CreateRockSpire(tBedrock, new Vector3(-310f, 22f, -240f), 16f, 48f, pal.matRock);
        CreateRockSpire(tBedrock, new Vector3(310f, 32f, -240f), 20f, 70f, pal.matRock);

        // --- 1. SECTOR 0: CENTRO DE CALIBRACIÓN Y SALIDA (SPAWN) ---
        Transform tHub = CreateSubGroup("01_Sector0_CentralHub", root);
        BuildCentralHub(tHub, pal);

        // --- 2. SECTOR 1: PISTA DE VELOCIDAD, ACELERACIÓN Y SLALOM (NORTE) ---
        Transform tSpeedway = CreateSubGroup("02_Sector1_Speedway_300M", root);
        BuildSpeedwaySector(tSpeedway, pal);

        // --- 3. SECTOR 2: CIRCUITO DE ESCALONES, ADAPTACIÓN Y ZANJAS (ESTE) ---
        Transform tSteps = CreateSubGroup("03_Sector2_StepObstacles_Course", root);
        BuildStepObstaclesSector(tSteps, pal);

        // --- 4. SECTOR 3: COLINA DE PENDIENTES GRADUADAS Y CIMA (SURESTE) ---
        Transform tSlopes = CreateSubGroup("04_Sector3_SlopeMountain", root);
        BuildSlopeMountainSector(tSlopes, pal);

        // --- 5. SECTOR 4: FOSO DE VUELCO Y PATALETA HIDRÁULICA (SUR) ---
        Transform tOverturn = CreateSubGroup("05_Sector4_OverturnAndRecoveryPit", root);
        BuildOverturnRecoverySector(tOverturn, pal);

        // --- 6. SECTOR 5: BADLANDS Y CAMPO DE ROCAS PROCEDURAL (OESTE) ---
        Transform tBadlands = CreateSubGroup("06_Sector5_BadlandsRockCrawler", root);
        BuildBadlandsSector(tBadlands, pal, rng);

        // --- 7. SECTOR 6: EL GRAN CAÑÓN DE ÁCIDO Y 3 PUENTES (NOROESTE) ---
        Transform tCanyon = CreateSubGroup("07_Sector6_ToxicAcidCanyon", root);
        BuildAcidCanyonSector(tCanyon, pal);

        // --- 8. SECTOR 7: DISTRITO URBANO DE RUINAS Y COMBATE (NORESTE) ---
        Transform tCity = CreateSubGroup("08_Sector7_RuinedCityCombatDistrict", root);
        BuildRuinedCityDistrict(tCity, pal, rng);

        // --- 9. RED DE BULEVARES CONECTORES Y SEÑALIZACIÓN PERIMETRAL ---
        Transform tRoads = CreateSubGroup("09_ConnectingBoulevardsAndHighways", root);
        BuildConnectingHighways(tRoads, pal);
    }

    #region Sector 0: Central Hub
    private static void BuildCentralHub(Transform parent, MaterialPalette pal)
    {
        // Plaza Central de calibración amplia (80m x 80m)
        CreateBlock("Hub_Plaza_Floor", parent, new Vector3(0f, -0.6f, 0f), new Vector3(80f, 1.2f, 80f), Quaternion.identity, pal.matAsphalt);

        // Bordillos perimetrales visuales (sin collider para evitar cualquier tropiezo)
        CreateBlock("Hub_Curb_N_Left", parent, new Vector3(-28f, 0.2f, 39.8f), new Vector3(24f, 0.4f, 0.4f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Hub_Curb_N_Right", parent, new Vector3(28f, 0.2f, 39.8f), new Vector3(24f, 0.4f, 0.4f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Hub_Curb_S_Left", parent, new Vector3(-28f, 0.2f, -39.8f), new Vector3(24f, 0.4f, 0.4f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Hub_Curb_S_Right", parent, new Vector3(28f, 0.2f, -39.8f), new Vector3(24f, 0.4f, 0.4f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Hub_Curb_E_Top", parent, new Vector3(39.8f, 0.2f, 28f), new Vector3(0.4f, 0.4f, 24f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Hub_Curb_E_Bottom", parent, new Vector3(39.8f, 0.2f, -28f), new Vector3(0.4f, 0.4f, 24f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Hub_Curb_W_Top", parent, new Vector3(-39.8f, 0.2f, 28f), new Vector3(0.4f, 0.4f, 24f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Hub_Curb_W_Bottom", parent, new Vector3(-39.8f, 0.2f, -28f), new Vector3(0.4f, 0.4f, 24f), Quaternion.identity, pal.matHazard, false);

        // Cuadrícula de calibración de pasos en el centro (marcas pintadas en el suelo, sin collider)
        for (int x = -3; x <= 3; x++)
        {
            if (x == 0) continue;
            CreateBlock($"Grid_Mark_X{x}", parent, new Vector3(x * 6.2f, 0.02f, 0f), new Vector3(0.35f, 0.04f, 24f), Quaternion.identity, pal.matHazard, false);
        }
        for (int z = -2; z <= 2; z++)
        {
            if (z == 0) continue;
            CreateBlock($"Grid_Mark_Z{z}", parent, new Vector3(0f, 0.02f, z * 6.2f), new Vector3(36f, 0.04f, 0.35f), Quaternion.identity, pal.matHazard, false);
        }

        // Gran Arco Gantry de Salida Norte: 46m de luz, 26m de alto, sin colliders
        CreateSignGantry("Gantry_ExitNorth", parent, new Vector3(0f, 0f, 38f), 46f, 26f, "▲ PISTA DE VELOCIDAD (300M)", pal.matWall, pal.matCyan);

        // Monolito de Señalización Central (sin collider)
        CreateDirectionPillar(parent, new Vector3(-12f, 0f, 12f), "▲ NORTE: VELOCIDAD 300M\n► ESTE: ESCALONES Y RAMPAS\n▼ SUR: FOSO DE VUELCO\n◄ OESTE: ROCAS Y ÁCIDO", pal);
    }
    #endregion

    #region Sector 1: Speedway (Pista de Velocidad 300m)
    private static void BuildSpeedwaySector(Transform parent, MaterialPalette pal)
    {
        const float START_Z = 38f;
        const float LENGTH = 290f;
        const float WIDTH = 34f; // Calzada ampliada a 34m
        float centerZ = START_Z + LENGTH * 0.5f;

        // Autopista asfaltada principal (300m recta)
        CreateBlock("Speedway_Asphalt_Strip", parent, new Vector3(0f, -0.6f, centerZ), new Vector3(WIDTH, 1.2f, LENGTH), Quaternion.identity, pal.matAsphalt);

        // Bordillos y líneas de carril (sin collider para evitar atascos)
        CreateBlock("Speedway_Curb_L", parent, new Vector3(-WIDTH * 0.5f, 0.2f, centerZ), new Vector3(0.5f, 0.4f, LENGTH), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Speedway_Curb_R", parent, new Vector3(WIDTH * 0.5f, 0.2f, centerZ), new Vector3(0.5f, 0.4f, LENGTH), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Speedway_Divider", parent, new Vector3(0f, 0.02f, centerZ), new Vector3(0.4f, 0.04f, LENGTH), Quaternion.identity, pal.matHazard, false);

        // Arcos de telemetría de distancia cada 50 metros: TODOS a 26m de altura y 46m de ancho, sin collider
        float[] distances = new float[] { 50f, 100f, 150f, 200f, 250f, 285f };
        string[] labels = new string[] {
            "🏁 MARCA: 50 METROS",
            "⚡ MARCA: 100 METROS (PRUEBA ACELERACIÓN)",
            "〰 MARCA: 150 METROS (BANDAS SONORAS)",
            "🔄 MARCA: 200 METROS (ZONA SLALOM)",
            "⚠ MARCA: 250 METROS (ZONA DE FRENADA)",
            "🛑 FIN DE PISTA (300 METROS) - RAQUETA DE GIRO"
        };

        for (int i = 0; i < distances.Length; i++)
        {
            float z = START_Z + distances[i];
            CreateSignGantry($"Speedway_Gantry_{distances[i]}m", parent, new Vector3(0f, 0f, z), 46f, 26f, labels[i], pal.matWall, i >= 4 ? pal.matAmber : pal.matCyan);
        }

        // --- SUBZONA A: BANDAS SONORAS Y BADENES (SUSPENSIÓN) (Z = 135 a 175) ---
        // Situadas en el carril derecho (X = 8f), dejando el carril izquierdo completamente despejado
        for (int i = 0; i < 7; i++)
        {
            float z = START_Z + 125f + i * 6.5f;
            float bumpHeight = 0.18f + i * 0.03f;
            CreateBlock($"RumbleStrip_{i}", parent, new Vector3(8.5f, bumpHeight * 0.5f, z), new Vector3(12f, bumpHeight, 1.4f), Quaternion.identity, pal.matHazard);
        }

        // --- SUBZONA B: CIRCUITO DE SLALOM Y GIRO RÁPIDO (Z = 190 a 265) ---
        // Postes de viraje colocados en carril derecho sin collider físico para evitar encallamientos
        for (int i = 0; i < 6; i++)
        {
            float z = START_Z + 185f + i * 14f;
            float xOffset = (i % 2 == 0) ? 6f : 11f;
            CreateSlalomPylon(parent, new Vector3(xOffset, 0f, z), pal);
        }

        // Raqueta de retorno y barrera de desaceleración al final (Z = 330)
        float endZ = START_Z + LENGTH;
        CreateBlock("End_Turnaround_Plaza", parent, new Vector3(0f, -0.6f, endZ + 24f), new Vector3(72f, 1.2f, 48f), Quaternion.identity, pal.matAsphalt);
        CreateBlock("End_Arrest_Barrier", parent, new Vector3(0f, 3.5f, endZ + 47f), new Vector3(72f, 7f, 2.5f), Quaternion.identity, pal.matHazard);
    }

    private static void CreateSlalomPylon(Transform parent, Vector3 pos, MaterialPalette pal)
    {
        GameObject pylon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pylon.name = $"SlalomPylon_{pos.z:F0}";
        pylon.transform.parent = parent;
        pylon.transform.position = pos + Vector3.up * 3.5f;
        pylon.transform.localScale = new Vector3(1.6f, 3.5f, 1.6f);
        if (pal.matHazard != null) pylon.GetComponent<MeshRenderer>().material = pal.matHazard;

        // Quitar collider para no atascar al mecha en caso de roce
        var col = pylon.GetComponent<Collider>();
        if (col != null) UnityEngine.Object.DestroyImmediate(col);

        // Baliza luminosa superior
        GameObject lightObj = new GameObject("Pylon_Light");
        lightObj.transform.parent = pylon.transform;
        lightObj.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = Color.yellow;
        l.range = 10f;
        l.intensity = 2.0f;
    }
    #endregion

    #region Sector 2: Step Obstacles & Terrain Adaptation (Escalones y Zanjas)
    private static void BuildStepObstaclesSector(Transform parent, MaterialPalette pal)
    {
        // Conexión Boulevard Este desde el Hub (30m de ancho)
        CreateBlock("East_Boulevard_Entry", parent, new Vector3(50f, -0.6f, 0f), new Vector3(32f, 1.2f, 28f), Quaternion.identity, pal.matAsphalt);

        // Gran Plataforma del Sector de Escalones (140m x 120m)
        Vector3 sectorOrigin = new Vector3(135f, -0.6f, 0f);
        CreateBlock("Steps_Sector_Base", parent, sectorOrigin, new Vector3(140f, 1.2f, 130f), Quaternion.identity, pal.matAsphalt);

        CreateSignGantry("Gantry_Steps_Entry", parent, new Vector3(68f, 0f, 0f), 46f, 26f, "► CIRCUITO DE ESCALONES Y ADAPTACIÓN IK", pal.matWall, pal.matCyan);

        // --- SUBPRUEBA 1: ESCALERA PROGRESIVA DE ALTURA (STEP LADDER TEST) ---
        float[] stepHeights = new float[] { 0.25f, 0.50f, 0.80f, 1.10f, 1.40f, 1.60f, 1.95f };
        string[] stepLabels = new string[] { "0.25m (Bordillo)", "0.50m (Medio)", "0.80m (Rodilla)", "1.10m (Alto)", "1.40m (Límite)", "1.60m (Máximo IK)", "1.95m (Muro Infranqueable)" };

        float startStepX = 85f;
        float stepZ = 35f;
        float accumHeight = 0f;

        for (int i = 0; i < stepHeights.Length; i++)
        {
            accumHeight = stepHeights[i];
            float x = startStepX + i * 8.5f;
            CreateBlock($"StepLadder_Riser_{i}", parent, new Vector3(x, accumHeight * 0.5f, stepZ), new Vector3(8.5f, accumHeight, 18f), Quaternion.identity, pal.matAsphalt);
            CreateBlock($"StepLadder_Curb_{i}", parent, new Vector3(x - 4.1f, accumHeight + 0.15f, stepZ), new Vector3(0.3f, 0.3f, 18f), Quaternion.identity, pal.matHazard, false);

            CreateGroundLabel(parent, new Vector3(x, accumHeight + 0.05f, stepZ + 9.5f), stepLabels[i], pal);
        }

        // Rampa suave lateral de bajada
        CreateBlock("StepLadder_Return_Ramp", parent, new Vector3(115f, accumHeight * 0.5f, stepZ - 14f), new Vector3(65f, 0.8f, 12f), Quaternion.Euler(0f, 0f, -10f), pal.matAsphalt);

        // --- SUBPRUEBA 2: GRAN ESCALINATA BÍPEDE MONUMENTAL (+8.4M) ---
        float stairsStartX = 85f;
        float stairsZ = -25f;
        for (int i = 0; i < 12; i++)
        {
            float h = (i + 1) * 0.70f;
            float x = stairsStartX + i * 3.4f;
            CreateBlock($"GrandStairs_Step_{i}", parent, new Vector3(x, h * 0.5f, stairsZ), new Vector3(3.4f, h, 26f), Quaternion.identity, pal.matRubble);
            CreateBlock($"GrandStairs_Edge_{i}", parent, new Vector3(x - 1.6f, h + 0.1f, stairsZ), new Vector3(0.25f, 0.2f, 26f), Quaternion.identity, pal.matHazard, false);
        }

        // Terraza superior panorámica de la escalinata
        float terraceX = stairsStartX + 12 * 3.4f + 18f;
        float terraceH = 12 * 0.70f;
        CreateBlock("GrandStairs_TopTerrace", parent, new Vector3(terraceX, terraceH - 0.5f, stairsZ), new Vector3(38f, 1.0f, 34f), Quaternion.identity, pal.matAsphalt);

        // --- SUBPRUEBA 3: PISTA DE PERALTE ASIMÉTRICO (STRADDLE CAMBER) ---
        float camberX = 145f;
        float camberZ = -50f;
        CreateBlock("Camber_Raised_LeftFootTrack", parent, new Vector3(camberX - 3.2f, 0.425f, camberZ), new Vector3(3.5f, 0.85f, 50f), Quaternion.identity, pal.matHazard);
        CreateBlock("Camber_Normal_RightFootTrack", parent, new Vector3(camberX + 3.2f, 0.05f, camberZ), new Vector3(3.5f, 0.1f, 50f), Quaternion.identity, pal.matAsphalt);
        CreateGroundLabel(parent, new Vector3(camberX, 0.1f, camberZ + 27f), "⚠ TEST DESNIVEL LATERAL: PATA IZQ +0.85M", pal);

        // --- SUBPRUEBA 4: ZANJAS DE ZANCADA (TRENCH CROSSING) ---
        float trenchX = 145f;
        float trenchZ = 55f;
        CreateBlock("Trench_Platform_1", parent, new Vector3(trenchX, -0.6f, trenchZ), new Vector3(50f, 1.2f, 10f), Quaternion.identity, pal.matAsphalt);
        CreateBlock("Trench_Bed_1", parent, new Vector3(trenchX, -3.5f, trenchZ + 6.2f), new Vector3(50f, 1.2f, 1.5f), Quaternion.identity, pal.matRock);
        CreateBlock("Trench_Platform_2", parent, new Vector3(trenchX, -0.6f, trenchZ + 12f), new Vector3(50f, 1.2f, 9f), Quaternion.identity, pal.matAsphalt);
        CreateBlock("Trench_Bed_2", parent, new Vector3(trenchX, -3.5f, trenchZ + 18.2f), new Vector3(50f, 1.2f, 2.4f), Quaternion.identity, pal.matRock);
        CreateBlock("Trench_Platform_3", parent, new Vector3(trenchX, -0.6f, trenchZ + 25f), new Vector3(50f, 1.2f, 10f), Quaternion.identity, pal.matAsphalt);
    }
    #endregion

    #region Sector 3: Slope Mountain (Rampas y Montaña)
    private static void BuildSlopeMountainSector(Transform parent, MaterialPalette pal)
    {
        CreateSignGantry("Gantry_Slopes_Entry", parent, new Vector3(65f, 0f, -100f), 46f, 26f, "► COLINA DE PENDIENTES GRADUADAS Y CIMA +28M", pal.matWall, pal.matAmber);

        // --- BATERÍA DE RAMPAS DE ÁNGULO GRADUADO ---
        float[] angles = new float[] { 12f, 24f, 34f, 44f, 56f };
        string[] slopeLabels = new string[] { "12° (Fácil)", "24° (Moderada)", "34° (Fuerte)", "44° (Límite 48°)", "56° (PELIGRO: VUELCO)" };

        float rampBaseX = 75f;
        float rampZ = -120f;
        float rampWidth = 12f;
        float rampLength = 32f;

        for (int i = 0; i < angles.Length; i++)
        {
            float ang = angles[i];
            float rad = ang * Mathf.Deg2Rad;
            float height = Mathf.Sin(rad) * rampLength;
            float horizontal = Mathf.Cos(rad) * rampLength;

            Vector3 rampPos = new Vector3(rampBaseX + i * 15f, height * 0.5f, rampZ - horizontal * 0.5f);
            Quaternion rampRot = Quaternion.Euler(-ang, 0f, 0f);

            CreateBlock($"SlopeRamp_{ang}deg", parent, rampPos, new Vector3(rampWidth, 0.8f, rampLength), rampRot, (ang >= 50f) ? pal.matHazard : pal.matRock);
            CreateGroundLabel(parent, new Vector3(rampBaseX + i * 15f, 0.1f, rampZ + 3.5f), slopeLabels[i], pal);

            Vector3 topPos = new Vector3(rampBaseX + i * 15f, height - 0.4f, rampZ - horizontal - 6f);
            CreateBlock($"SlopeTop_{ang}deg", parent, topPos, new Vector3(rampWidth, 0.8f, 12f), Quaternion.identity, pal.matAsphalt);
        }

        // --- CARRETERA SERPENTINA A LA GRAN CIMA (+28M) ---
        CreateBlock("Serpentine_Pass_1", parent, new Vector3(150f, 5f, -195f), new Vector3(18f, 1.2f, 52f), Quaternion.Euler(11f, 0f, 0f), pal.matAsphalt);
        CreateBlock("Serpentine_Switchback_1", parent, new Vector3(150f, 10f, -225f), new Vector3(36f, 1.2f, 28f), Quaternion.identity, pal.matAsphalt);

        CreateBlock("Serpentine_Pass_2", parent, new Vector3(180f, 17f, -200f), new Vector3(48f, 1.2f, 18f), Quaternion.Euler(0f, 0f, -14f), pal.matAsphalt);
        CreateBlock("Serpentine_Switchback_2", parent, new Vector3(210f, 24f, -170f), new Vector3(32f, 1.2f, 34f), Quaternion.identity, pal.matAsphalt);

        // Cima panorámica máxima (+28m)
        Vector3 summitPos = new Vector3(175f, 28f, -140f);
        CreateBlock("Mountain_Summit_Plateau", parent, summitPos, new Vector3(58f, 2.5f, 58f), Quaternion.identity, pal.matRock);
        CreateBlock("Summit_HeliPad_Ring", parent, summitPos + Vector3.up * 1.3f, new Vector3(32f, 0.1f, 32f), Quaternion.identity, pal.matHazard, false);
        CreateSignGantry("Gantry_Summit", parent, summitPos + new Vector3(0f, 1.3f, 24f), 46f, 26f, "🏔 CIMA PANORÁMICA MECHAQUACK (+28 METROS)", pal.matWall, pal.matCyan);
    }
    #endregion

    #region Sector 4: Overturn & Pataleta Pit (Foso de Vuelco)
    private static void BuildOverturnRecoverySector(Transform parent, MaterialPalette pal)
    {
        CreateSignGantry("Gantry_Overturn_Entry", parent, new Vector3(0f, 0f, -38f), 46f, 26f, "▼ FOSO DE VUELCO Y PRUEBA DE PATALETA HIDRÁULICA", pal.matWall, pal.matRed);

        // Avenida Sur de aproximación (28m de ancho)
        CreateBlock("South_Boulevard", parent, new Vector3(0f, -0.6f, -68f), new Vector3(28f, 1.2f, 55f), Quaternion.identity, pal.matAsphalt);

        Vector3 pitCenter = new Vector3(0f, -4.5f, -145f);

        // Cuenca excavada del foso (75m x 75m, profundidad 4.5m)
        CreateBlock("Pit_Basin_Floor", parent, pitCenter, new Vector3(75f, 1.5f, 75f), Quaternion.identity, pal.matAsphalt);

        CreateBlock("Pit_Wall_South", parent, pitCenter + new Vector3(0f, 3f, -37.5f), new Vector3(75f, 6f, 2f), Quaternion.identity, pal.matHazard);
        CreateBlock("Pit_Wall_East", parent, pitCenter + new Vector3(37.5f, 3f, 0f), new Vector3(2f, 6f, 75f), Quaternion.identity, pal.matHazard);
        CreateBlock("Pit_Wall_West", parent, pitCenter + new Vector3(-37.5f, 3f, 0f), new Vector3(2f, 6f, 75f), Quaternion.identity, pal.matHazard);

        // Cuña de balanceo a 65 grados
        CreateBlock("Overturn_Tilt_Wedge", parent, new Vector3(-12f, 0.5f, -100f), new Vector3(24f, 2.5f, 28f), Quaternion.Euler(0f, 0f, 65f), pal.matHazard);
        CreateGroundLabel(parent, new Vector3(-12f, 3.2f, -82f), "⚠ BANCO DE VUELCO A 65°: CAMINA AQUÍ PARA PROBAR PATALETA", pal);

        CreateBlock("Pataleta_Sandbox_Mat", parent, pitCenter + Vector3.up * 0.8f, new Vector3(36f, 0.2f, 36f), Quaternion.identity, pal.matRubble);

        // Rampa suave de salida (22m de ancho)
        CreateBlock("Pit_Exit_Ramp", parent, new Vector3(18f, -2.2f, -98f), new Vector3(22f, 1.0f, 32f), Quaternion.Euler(-10f, 0f, 0f), pal.matAsphalt);
        CreateGroundLabel(parent, new Vector3(18f, 0.2f, -80f), "▲ SALIDA DEL FOSO HACIA EL HUB", pal);
    }
    #endregion

    #region Sector 5: Badlands Rock Crawler (Terreno Rugoso)
    private static void BuildBadlandsSector(Transform parent, MaterialPalette pal, System.Random rng)
    {
        CreateSignGantry("Gantry_Badlands_Entry", parent, new Vector3(-38f, 0f, 0f), 46f, 26f, "◄ CAMPO DE ROCAS Y BADLANDS (TEST SUSPENSIÓN E IK)", pal.matWall, pal.matRock);

        // Conexión Boulevard Oeste desde el Hub (32m de ancho)
        CreateBlock("West_Boulevard_Entry", parent, new Vector3(-50f, -0.6f, 0f), new Vector3(32f, 1.2f, 28f), Quaternion.identity, pal.matAsphalt);

        Vector3 basePos = new Vector3(-150f, -0.8f, -40f);
        CreateBlock("Badlands_Base_Plateau", parent, basePos, new Vector3(180f, 1.6f, 150f), Quaternion.identity, pal.matRock);

        for (int i = 0; i < 75; i++)
        {
            float rx = (float)(rng.NextDouble() * 150.0 - 75.0) - 150f;
            float rz = (float)(rng.NextDouble() * 120.0 - 60.0) - 40f;

            if (Mathf.Abs(rz - (-40f)) < 10f && rx > -165f && rx < -85f) continue;

            float w = (float)(rng.NextDouble() * 4.5 + 2.5);
            float h = (float)(rng.NextDouble() * 1.3 + 0.35);
            float d = (float)(rng.NextDouble() * 4.5 + 2.5);
            float rotY = (float)(rng.NextDouble() * 180.0);
            float tiltX = (float)(rng.NextDouble() * 20.0 - 10.0);
            float tiltZ = (float)(rng.NextDouble() * 20.0 - 10.0);

            CreateBlock($"Boulder_{i}", parent, new Vector3(rx, h * 0.5f, rz), new Vector3(w, h, d), Quaternion.Euler(tiltX, rotY, tiltZ), pal.matRock);
        }

        // Pasillo Rock Crawler amplio y dinámico
        float crawlerZ = -40f;
        for (int i = 0; i < 16; i++)
        {
            float cx = -75f - i * 5.5f;
            float sideOffset = (i % 2 == 0) ? -3.2f : 3.2f;
            float stepH = 0.45f + (i % 4) * 0.28f;
            CreateBlock($"RockCrawler_Obstacle_{i}", parent, new Vector3(cx, stepH * 0.5f, crawlerZ + sideOffset), new Vector3(4.2f, stepH, 3.8f), Quaternion.Euler(5f, i * 25f, -6f), pal.matRock);
        }

        CreateGroundLabel(parent, new Vector3(-70f, 0.1f, crawlerZ + 8f), "🪨 CANAL ROCK CRAWLER: PRUEBA DE PISADA DINÁMICA SOBRE ROCAS", pal);

        CreateRockSpire(parent, new Vector3(-225f, 15f, -100f), 14f, 40f, pal.matRock);
        CreateRockSpire(parent, new Vector3(-210f, 18f, 25f), 16f, 46f, pal.matRock);
    }
    #endregion

    #region Sector 6: Toxic Acid Canyon & Bridges (Cañón de Ácido Despejado)
    private static void BuildAcidCanyonSector(Transform parent, MaterialPalette pal)
    {
        Vector3 canyonCenter = new Vector3(-135f, -12.5f, 130f);
        float canyonLength = 220f;
        float canyonWidth = 62f;

        // --- LA GRAN FOSA DE ÁCIDO TÓXICO ---
        GameObject acidObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        acidObj.name = "ToxicAcidRiver_Mega";
        acidObj.transform.parent = parent;
        acidObj.transform.position = canyonCenter;
        acidObj.transform.localScale = new Vector3(canyonLength, 1.4f, canyonWidth);
        if (pal.matAcid != null) acidObj.GetComponent<MeshRenderer>().material = pal.matAcid;
        var acidCollider = acidObj.GetComponent<BoxCollider>();
        if (acidCollider != null) acidCollider.isTrigger = true;
        acidObj.AddComponent<AcidHazard>();

        // Lecho y muros del cañón
        CreateBlock("Acid_Basin_Bed", parent, canyonCenter + Vector3.down * 3.5f, new Vector3(canyonLength + 20f, 5f, canyonWidth + 10f), Quaternion.identity, pal.matRock);
        CreateBlock("Canyon_Wall_South", parent, new Vector3(canyonCenter.x, -5f, canyonCenter.z - canyonWidth * 0.5f - 1.5f), new Vector3(canyonLength, 14f, 3f), Quaternion.identity, pal.matRock);
        CreateBlock("Canyon_Wall_North", parent, new Vector3(canyonCenter.x, -5f, canyonCenter.z + canyonWidth * 0.5f + 1.5f), new Vector3(canyonLength, 14f, 3f), Quaternion.identity, pal.matRock);

        // Márgenes del cañón (amplias carreteras transitables de 36m de ancho)
        CreateBlock("Canyon_Rim_South_Road", parent, new Vector3(canyonCenter.x, -0.6f, canyonCenter.z - canyonWidth * 0.5f - 18f), new Vector3(canyonLength, 1.2f, 36f), Quaternion.identity, pal.matAsphalt);
        CreateBlock("Canyon_Rim_North_Road", parent, new Vector3(canyonCenter.x, -0.6f, canyonCenter.z + canyonWidth * 0.5f + 18f), new Vector3(canyonLength, 1.2f, 36f), Quaternion.identity, pal.matAsphalt);

        // Arco de entrada al cañón: 46m de ancho, 26m de altura, sin collider
        CreateSignGantry("Gantry_Canyon_Entry", parent, new Vector3(-80f, 0f, 85f), 46f, 26f, "☣ EL GRAN CAÑÓN DE ÁCIDO: 3 PUENTES TOTALMENTE DESPEJADOS", pal.matWall, pal.matHazard);

        // --- PUENTE 1: AUTOPISTA PESADA BLINDADA (24M DE ANCHO - TOTALMENTE DESPEJADA) ---
        float bridge1X = -65f;
        CreateBlock("Bridge1_Highway_Deck", parent, new Vector3(bridge1X, -0.5f, canyonCenter.z), new Vector3(24f, 1.4f, canyonWidth + 6f), Quaternion.identity, pal.matAsphalt);
        // Bordillos visuales sin collider físico para que nunca traben las patas ni las armas
        CreateBlock("Bridge1_Rail_L", parent, new Vector3(bridge1X - 11.6f, 0.3f, canyonCenter.z), new Vector3(0.6f, 0.6f, canyonWidth + 6f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Bridge1_Rail_R", parent, new Vector3(bridge1X + 11.6f, 0.3f, canyonCenter.z), new Vector3(0.6f, 0.6f, canyonWidth + 6f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Bridge1_Pillar_S", parent, new Vector3(bridge1X, -6.5f, canyonCenter.z - 16f), new Vector3(8f, 13f, 8f), Quaternion.identity, pal.matRubble);
        CreateBlock("Bridge1_Pillar_N", parent, new Vector3(bridge1X, -6.5f, canyonCenter.z + 16f), new Vector3(8f, 13f, 8f), Quaternion.identity, pal.matRubble);
        CreateGroundLabel(parent, new Vector3(bridge1X, 0.2f, canyonCenter.z - canyonWidth * 0.5f - 4f), "PUENTE 1: AUTOPISTA ANCHA (24M)", pal);

        // --- PUENTE 2: PUENTE FRACTURADO (18M DE ANCHO - CARRIL CENTRAL LIMPIO) ---
        float bridge2X = -135f;
        CreateBlock("Bridge2_Span_South", parent, new Vector3(bridge2X, -0.5f, canyonCenter.z - 18f), new Vector3(18f, 1.3f, 26f), Quaternion.identity, pal.matAsphalt);
        CreateBlock("Bridge2_Broken_Slab", parent, new Vector3(bridge2X, -0.2f, canyonCenter.z), new Vector3(17f, 1.2f, 14f), Quaternion.Euler(0f, 0f, 2.5f), pal.matAsphalt);
        // Escombro colocado en el borde exterior derecho (X offset +6.5f), dejando un carril central despejado de 12m
        CreateBlock("Bridge2_Rubble_Obstacle", parent, new Vector3(bridge2X + 6.5f, 0.8f, canyonCenter.z), new Vector3(3.2f, 1.4f, 4.0f), Quaternion.Euler(10f, 25f, -8f), pal.matRubble);
        CreateBlock("Bridge2_Span_North", parent, new Vector3(bridge2X, -0.5f, canyonCenter.z + 18f), new Vector3(18f, 1.3f, 26f), Quaternion.identity, pal.matAsphalt);
        CreateGroundLabel(parent, new Vector3(bridge2X, 0.2f, canyonCenter.z - canyonWidth * 0.5f - 4f), "PUENTE 2: FRACTURADO (18M)", pal);

        // --- PUENTE 3: LA PASARELA SUSPENDIDA (13M DE ANCHO - SIN BARANDILLAS) ---
        float bridge3X = -195f;
        CreateBlock("Bridge3_Razor_Catwalk", parent, new Vector3(bridge3X, -0.4f, canyonCenter.z), new Vector3(13f, 1.2f, canyonWidth + 6f), Quaternion.Euler(0f, 0f, -2.5f), pal.matAsphalt);
        CreateGroundLabel(parent, new Vector3(bridge3X, 0.2f, canyonCenter.z - canyonWidth * 0.5f - 4f), "⚠ PUENTE 3: PASARELA SUSPENDIDA (13M - SIN BARANDILLA)", pal);

        // Pilares/Islas de roca transitables en medio del ácido (18m y 20m)
        CreateBlock("Acid_Stepping_Island_1", parent, new Vector3(-105f, -1.0f, canyonCenter.z - 8f), new Vector3(18f, 14f, 18f), Quaternion.identity, pal.matRock);
        CreateBlock("Acid_Stepping_Island_2", parent, new Vector3(-165f, -1.0f, canyonCenter.z + 8f), new Vector3(20f, 14f, 20f), Quaternion.identity, pal.matRock);
    }
    #endregion

    #region Sector 7: Ruined City Combat District (Distrito Urbano)
    private static void BuildRuinedCityDistrict(Transform parent, MaterialPalette pal, System.Random rng)
    {
        Vector3 cityCenter = new Vector3(145f, -0.6f, 175f);

        CreateSignGantry("Gantry_City_Entry", parent, new Vector3(65f, 0f, 100f), 46f, 26f, "🚨 DISTRITO URBANO EN RUINAS Y COMBATE TÁCTICO", pal.matWall, pal.matRed);

        CreateBlock("City_Grid_Ground", parent, cityCenter, new Vector3(180f, 1.2f, 180f), Quaternion.identity, pal.matAsphalt);

        // --- AUTOPISTA ELEVADA DE TIRO SNIPER (+8M) ---
        CreateBlock("Highway_Approach_Ramp", parent, new Vector3(85f, 3.5f, 130f), new Vector3(18f, 1.2f, 44f), Quaternion.Euler(11f, 0f, 0f), pal.matAsphalt);
        CreateBlock("Highway_Elevated_Deck", parent, new Vector3(85f, 7.5f, 195f), new Vector3(18f, 1.2f, 85f), Quaternion.identity, pal.matAsphalt);
        CreateBlock("Highway_Collapsed_Edge", parent, new Vector3(85f, 5.0f, 245f), new Vector3(18f, 1.2f, 18f), Quaternion.Euler(18f, 0f, 0f), pal.matRubble);

        Vector3 arenaCenter = new Vector3(160f, 0.05f, 180f);
        CreateBlock("Combat_Plaza_Ring", parent, arenaCenter, new Vector3(55f, 0.1f, 55f), Quaternion.identity, pal.matHazard, false);
        CreateGroundLabel(parent, arenaCenter + new Vector3(0f, 0.1f, 29f), "⚔ PLAZA DE TIRO: ACTIVA EL BOTÓN SPAWN PARA TESTEAR GATLING Y MISILES", pal);

        Vector3[] towerOffsets = new Vector3[] {
            new Vector3(-45f, 28f, -40f), new Vector3(25f, 34f, -45f), new Vector3(-40f, 26f, 35f),
            new Vector3(45f, 30f, 35f), new Vector3(-15f, 38f, 65f), new Vector3(35f, 32f, -15f),
            new Vector3(-55f, 22f, 0f), new Vector3(55f, 24f, 0f), new Vector3(0f, 42f, 55f)
        };

        for (int i = 0; i < towerOffsets.Length; i++)
        {
            Vector3 pos = cityCenter + towerOffsets[i];
            float w = (float)(rng.NextDouble() * 8.0 + 14.0);
            float h = (float)(rng.NextDouble() * 30.0 + 42.0);
            float d = (float)(rng.NextDouble() * 8.0 + 14.0);
            float tiltX = (float)(rng.NextDouble() * 16.0 - 8.0);
            float tiltZ = (float)(rng.NextDouble() * 16.0 - 8.0);
            float rotY = (float)(rng.NextDouble() * 90.0);

            CreateBlock($"Skyscraper_{i}", parent, new Vector3(pos.x, h * 0.5f, pos.z), new Vector3(w, h, d), Quaternion.Euler(tiltX, rotY, tiltZ), pal.matWall);
        }

        for (int i = 0; i < 18; i++)
        {
            float rx = (float)(rng.NextDouble() * 120.0 - 60.0) + cityCenter.x;
            float rz = (float)(rng.NextDouble() * 120.0 - 60.0) + cityCenter.z;
            float s = (float)(rng.NextDouble() * 3.5 + 3.0);
            CreateBlock($"City_RubbleBarricade_{i}", parent, new Vector3(rx, s * 0.5f, rz), new Vector3(s, s, s), Quaternion.Euler((float)(rng.NextDouble() * 30.0 - 15.0), (float)(rng.NextDouble() * 180.0), 0f), pal.matRubble);
        }
    }
    #endregion

    #region Sector 9: Connecting Highways (Red de Conexión)
    private static void BuildConnectingHighways(Transform parent, MaterialPalette pal)
    {
        // Gran Bulevar Perimetral Este-Oeste amplio (26m de ancho)
        CreateBlock("Mid_Boulevard_EastWest", parent, new Vector3(0f, -0.6f, 60f), new Vector3(320f, 1.2f, 26f), Quaternion.identity, pal.matAsphalt);

        // Bulevar Perimetral Norte conectando la Ciudad en Ruinas con la salida de la Pista (26m de ancho)
        CreateBlock("North_Ring_Boulevard", parent, new Vector3(95f, -0.6f, 330f), new Vector3(150f, 1.2f, 26f), Quaternion.identity, pal.matAsphalt);

        // Autopista diagonal conectando el Cañón Norte con la Pista de Velocidad (26m de ancho)
        CreateBlock("Canyon_To_Speedway_Connector", parent, new Vector3(-45f, -0.6f, 200f), new Vector3(65f, 1.2f, 26f), Quaternion.Euler(0f, 45f, 0f), pal.matAsphalt);
    }
    #endregion

    #region Helpers de Geometría y Decoración
    private static Transform CreateSubGroup(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.parent = parent;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        return go.transform;
    }

    private static GameObject CreateBlock(string name, Transform parent, Vector3 pos, Vector3 scale, Quaternion rot, Material mat, bool hasCollider = true)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.parent = parent;
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.transform.localScale = scale;
        if (mat != null) go.GetComponent<MeshRenderer>().material = mat;
        if (!hasCollider)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
        }
        return go;
    }

    private static void CreateRockSpire(Transform parent, Vector3 pos, float radius, float height, Material mat)
    {
        GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spire.name = $"RockSpire_{pos.x:F0}_{pos.z:F0}";
        spire.transform.parent = parent;
        spire.transform.position = pos;
        spire.transform.localScale = new Vector3(radius, height * 0.5f, radius);
        if (mat != null) spire.GetComponent<MeshRenderer>().material = mat;
    }

    /// <summary>
    /// Genera arcos gantry con información y telemetría.
    /// TODOS los elementos (postes, vigas y letreros) se generan SIN colisionador físico y a gran altura (26m)
    /// garantizando que el mecha nunca se bloquee ni roce al cruzar calzadas o puentes.
    /// </summary>
    private static void CreateSignGantry(string name, Transform parent, Vector3 pos, float spanWidth, float height, string text, Material frameMat, Material textMat)
    {
        GameObject gantry = new GameObject(name);
        gantry.transform.parent = parent;
        gantry.transform.position = pos;

        height = Mathf.Max(height, 26f);
        spanWidth = Mathf.Max(spanWidth, 46f);

        // Postes y vigas sin colisionador
        CreateBlock("Pillar_L", gantry.transform, pos + new Vector3(-spanWidth * 0.5f, height * 0.5f, 0f), new Vector3(1.6f, height, 1.6f), Quaternion.identity, frameMat, false);
        CreateBlock("Pillar_R", gantry.transform, pos + new Vector3(spanWidth * 0.5f, height * 0.5f, 0f), new Vector3(1.6f, height, 1.6f), Quaternion.identity, frameMat, false);
        CreateBlock("Crossbeam", gantry.transform, pos + new Vector3(0f, height - 0.7f, 0f), new Vector3(spanWidth + 2.0f, 1.4f, 1.6f), Quaternion.identity, frameMat, false);

        GameObject board = CreateBlock("SignBoard", gantry.transform, pos + new Vector3(0f, height - 0.7f, -0.9f), new Vector3(spanWidth - 4.0f, 2.5f, 0.35f), Quaternion.identity, frameMat, false);

        GameObject textObj = new GameObject("Label3D");
        textObj.transform.parent = board.transform;
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.6f);
        textObj.transform.localRotation = Quaternion.identity;
        textObj.transform.localScale = Vector3.one * 0.18f;

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 38;
        tm.characterSize = 0.38f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = textMat != null ? textMat.color : Color.cyan;
    }

    private static void CreateDirectionPillar(Transform parent, Vector3 pos, string text, MaterialPalette pal)
    {
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = "Central_Info_Kiosk";
        pillar.transform.parent = parent;
        pillar.transform.position = pos + Vector3.up * 3.5f;
        pillar.transform.localScale = new Vector3(2.5f, 3.5f, 2.5f);
        if (pal.matWall != null) pillar.GetComponent<MeshRenderer>().material = pal.matWall;

        var col = pillar.GetComponent<Collider>();
        if (col != null) UnityEngine.Object.DestroyImmediate(col);

        GameObject textObj = new GameObject("InfoText");
        textObj.transform.parent = pillar.transform;
        textObj.transform.localPosition = new Vector3(0f, 0.3f, -1.35f);
        textObj.transform.localRotation = Quaternion.identity;
        textObj.transform.localScale = Vector3.one * 0.15f;

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 32;
        tm.characterSize = 0.28f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = Color.white;
    }

    private static void CreateGroundLabel(Transform parent, Vector3 pos, string text, MaterialPalette pal)
    {
        GameObject labelObj = new GameObject($"Label_{pos.x:F0}_{pos.z:F0}");
        labelObj.transform.parent = parent;
        labelObj.transform.position = pos;
        labelObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        labelObj.transform.localScale = Vector3.one * 0.18f;

        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 30;
        tm.characterSize = 0.32f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = pal.matHazard != null ? pal.matHazard.color : Color.yellow;
    }
    #endregion
}
