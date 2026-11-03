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

        // --- 0. CIMIENTOS GLOBALES Y BARRERAS DEL HORIZONTE (1440m x 1440m) ---
        Transform tBedrock = CreateSubGroup("00_WorldBedrockAndPerimeter", root);
        CreateBlock("Bedrock_Foundation", tBedrock, new Vector3(0f, -28f, 100f), new Vector3(1440f, 4f, 1440f), Quaternion.identity, pal.matRock);

        // Muros perimetrales gigantes tipo cañón exterior (horizonte)
        CreateBlock("PerimeterWall_North", tBedrock, new Vector3(0f, 30f, 800f), new Vector3(1480f, 120f, 40f), Quaternion.identity, pal.matRock);
        CreateBlock("PerimeterWall_South", tBedrock, new Vector3(0f, 30f, -600f), new Vector3(1480f, 120f, 40f), Quaternion.identity, pal.matRock);
        CreateBlock("PerimeterWall_East", tBedrock, new Vector3(720f, 30f, 100f), new Vector3(40f, 120f, 1440f), Quaternion.identity, pal.matRock);
        CreateBlock("PerimeterWall_West", tBedrock, new Vector3(-720f, 30f, 100f), new Vector3(40f, 120f, 1440f), Quaternion.identity, pal.matRock);

        // Picos de roca lejanos decorativos en las esquinas
        CreateRockSpire(tBedrock, new Vector3(-620f, 50f, 680f), 36f, 110f, pal.matRock);
        CreateRockSpire(tBedrock, new Vector3(620f, 56f, 680f), 44f, 130f, pal.matRock);
        CreateRockSpire(tBedrock, new Vector3(-620f, 44f, -480f), 32f, 96f, pal.matRock);
        CreateRockSpire(tBedrock, new Vector3(620f, 64f, -480f), 40f, 140f, pal.matRock);

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
        // Plaza Central de calibración amplia (160m x 160m)
        CreateBlock("Hub_Plaza_Floor", parent, new Vector3(0f, -1.2f, 0f), new Vector3(160f, 2.4f, 160f), Quaternion.identity, pal.matAsphalt);

        // Bordillos perimetrales visuales (sin collider para evitar cualquier tropiezo)
        CreateBlock("Hub_Curb_N_Left", parent, new Vector3(-56f, 0.4f, 79.6f), new Vector3(48f, 0.8f, 0.8f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Hub_Curb_N_Right", parent, new Vector3(56f, 0.4f, 79.6f), new Vector3(48f, 0.8f, 0.8f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Hub_Curb_S_Left", parent, new Vector3(-56f, 0.4f, -79.6f), new Vector3(48f, 0.8f, 0.8f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Hub_Curb_S_Right", parent, new Vector3(56f, 0.4f, -79.6f), new Vector3(48f, 0.8f, 0.8f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Hub_Curb_E_Top", parent, new Vector3(79.6f, 0.4f, 56f), new Vector3(0.8f, 0.8f, 48f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Hub_Curb_E_Bottom", parent, new Vector3(79.6f, 0.4f, -56f), new Vector3(0.8f, 0.8f, 48f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Hub_Curb_W_Top", parent, new Vector3(-79.6f, 0.4f, 56f), new Vector3(0.8f, 0.8f, 48f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Hub_Curb_W_Bottom", parent, new Vector3(-79.6f, 0.4f, -56f), new Vector3(0.8f, 0.8f, 48f), Quaternion.identity, pal.matHazard, false);

        // Cuadrícula de calibración de pasos en el centro (marcas pintadas en el suelo, sin collider)
        for (int x = -3; x <= 3; x++)
        {
            if (x == 0) continue;
            CreateBlock($"Grid_Mark_X{x}", parent, new Vector3(x * 12.4f, 0.04f, 0f), new Vector3(0.7f, 0.08f, 48f), Quaternion.identity, pal.matHazard, false);
        }
        for (int z = -2; z <= 2; z++)
        {
            if (z == 0) continue;
            CreateBlock($"Grid_Mark_Z{z}", parent, new Vector3(0f, 0.04f, z * 12.4f), new Vector3(72f, 0.08f, 0.7f), Quaternion.identity, pal.matHazard, false);
        }

        // Gran Arco Gantry de Salida Norte: 96m de luz, 68m de altura libre (sin colliders, Mecha 50m pasa con holgura)
        CreateSignGantry("Gantry_ExitNorth", parent, new Vector3(0f, 0f, 76f), 96f, 68f, "▲ PISTA DE VELOCIDAD (600M)", pal.matWall, pal.matCyan);

        // Monolito de Señalización Central (sin collider)
        CreateDirectionPillar(parent, new Vector3(-24f, 0f, 24f), "▲ NORTE: VELOCIDAD 600M\n► ESTE: ESCALONES Y RAMPAS\n▼ SUR: FOSO DE VUELCO\n◄ OESTE: ROCAS Y ÁCIDO", pal);
    }
    #endregion

    #region Sector 1: Speedway (Pista de Velocidad 600m)
    private static void BuildSpeedwaySector(Transform parent, MaterialPalette pal)
    {
        const float START_Z = 76f;
        const float LENGTH = 580f;
        const float WIDTH = 68f; // Calzada ampliada al doble (68m)
        float centerZ = START_Z + LENGTH * 0.5f;

        // Autopista asfaltada principal (600m recta)
        CreateBlock("Speedway_Asphalt_Strip", parent, new Vector3(0f, -1.2f, centerZ), new Vector3(WIDTH, 2.4f, LENGTH), Quaternion.identity, pal.matAsphalt);

        // Bordillos y líneas de carril (sin collider para evitar atascos)
        CreateBlock("Speedway_Curb_L", parent, new Vector3(-WIDTH * 0.5f, 0.4f, centerZ), new Vector3(1.0f, 0.8f, LENGTH), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Speedway_Curb_R", parent, new Vector3(WIDTH * 0.5f, 0.4f, centerZ), new Vector3(1.0f, 0.8f, LENGTH), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Speedway_Divider", parent, new Vector3(0f, 0.04f, centerZ), new Vector3(0.8f, 0.08f, LENGTH), Quaternion.identity, pal.matHazard, false);

        // Arcos de telemetría de distancia cada 100 metros: TODOS a 52m de altura y 92m de ancho, sin collider
        float[] distances = new float[] { 100f, 200f, 300f, 400f, 500f, 570f };
        string[] labels = new string[] {
            "🏁 MARCA: 100 METROS",
            "⚡ MARCA: 200 METROS (PRUEBA ACELERACIÓN)",
            "〰 MARCA: 300 METROS (BANDAS SONORAS)",
            "🔄 MARCA: 400 METROS (ZONA SLALOM)",
            "⚠ MARCA: 500 METROS (ZONA DE FRENADA)",
            "🛑 FIN DE PISTA (600 METROS) - RAQUETA DE GIRO"
        };

        for (int i = 0; i < distances.Length; i++)
        {
            float z = START_Z + distances[i];
            CreateSignGantry($"Speedway_Gantry_{distances[i]:F0}m", parent, new Vector3(0f, 0f, z), 92f, 52f, labels[i], pal.matWall, i >= 4 ? pal.matAmber : pal.matCyan);
        }

        // --- SUBZONA A: BANDAS SONORAS Y BADENES (SUSPENSIÓN) ---
        // Situadas en el carril derecho (X = 17f), dejando el carril izquierdo completamente despejado
        for (int i = 0; i < 7; i++)
        {
            float z = START_Z + 250f + i * 13f;
            float bumpHeight = 0.36f + i * 0.06f;
            CreateBlock($"RumbleStrip_{i}", parent, new Vector3(17f, bumpHeight * 0.5f, z), new Vector3(24f, bumpHeight, 2.8f), Quaternion.identity, pal.matHazard);
        }

        // --- SUBZONA B: CIRCUITO DE SLALOM Y GIRO RÁPIDO ---
        // Postes de viraje colocados en carril derecho sin collider físico para evitar encallamientos
        for (int i = 0; i < 6; i++)
        {
            float z = START_Z + 370f + i * 28f;
            float xOffset = (i % 2 == 0) ? 12f : 22f;
            CreateSlalomPylon(parent, new Vector3(xOffset, 0f, z), pal);
        }

        // Raqueta de retorno y barrera de desaceleración al final (Z = 660)
        float endZ = START_Z + LENGTH;
        CreateBlock("End_Turnaround_Plaza", parent, new Vector3(0f, -1.2f, endZ + 48f), new Vector3(144f, 2.4f, 96f), Quaternion.identity, pal.matAsphalt);
        CreateBlock("End_Arrest_Barrier", parent, new Vector3(0f, 7.0f, endZ + 94f), new Vector3(144f, 14f, 5.0f), Quaternion.identity, pal.matHazard);
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
        // Conexión Boulevard Este desde el Hub (64m de ancho)
        CreateBlock("East_Boulevard_Entry", parent, new Vector3(100f, -1.2f, 0f), new Vector3(64f, 2.4f, 56f), Quaternion.identity, pal.matAsphalt);

        // Gran Plataforma del Sector de Escalones (280m x 260m)
        Vector3 sectorOrigin = new Vector3(270f, -1.2f, 0f);
        CreateBlock("Steps_Sector_Base", parent, sectorOrigin, new Vector3(280f, 2.4f, 260f), Quaternion.identity, pal.matAsphalt);

        CreateSignGantry("Gantry_Steps_Entry", parent, new Vector3(136f, 0f, 0f), 92f, 52f, "► CIRCUITO DE ESCALONES Y ADAPTACIÓN IK", pal.matWall, pal.matCyan);

        // --- SUBPRUEBA 1: ESCALERA PROGRESIVA DE ALTURA (STEP LADDER TEST) ---
        float[] stepHeights = new float[] { 0.50f, 1.00f, 1.60f, 2.20f, 2.80f, 3.20f, 3.90f };
        string[] stepLabels = new string[] { "0.50m (Bordillo)", "1.00m (Medio)", "1.60m (Rodilla)", "2.20f (Alto)", "2.80m (Límite)", "3.20m (Máximo IK)", "3.90m (Muro Infranqueable)" };

        float startStepX = 170f;
        float stepZ = 70f;
        float accumHeight = 0f;

        for (int i = 0; i < stepHeights.Length; i++)
        {
            accumHeight = stepHeights[i];
            float x = startStepX + i * 17f;
            CreateBlock($"StepLadder_Riser_{i}", parent, new Vector3(x, accumHeight * 0.5f, stepZ), new Vector3(17f, accumHeight, 36f), Quaternion.identity, pal.matAsphalt);
            CreateBlock($"StepLadder_Curb_{i}", parent, new Vector3(x - 8.2f, accumHeight + 0.3f, stepZ), new Vector3(0.6f, 0.6f, 36f), Quaternion.identity, pal.matHazard, false);

            CreateGroundLabel(parent, new Vector3(x, accumHeight + 0.1f, stepZ + 19f), stepLabels[i], pal);
        }

        // Rampa suave lateral de bajada
        CreateBlock("StepLadder_Return_Ramp", parent, new Vector3(230f, accumHeight * 0.5f, stepZ - 28f), new Vector3(130f, 1.6f, 24f), Quaternion.Euler(0f, 0f, -10f), pal.matAsphalt);

        // --- SUBPRUEBA 2: GRAN ESCALINATA BÍPEDE MONUMENTAL (+16.8M) ---
        float stairsStartX = 170f;
        float stairsZ = -50f;
        for (int i = 0; i < 12; i++)
        {
            float h = (i + 1) * 1.40f;
            float x = stairsStartX + i * 6.8f;
            CreateBlock($"GrandStairs_Step_{i}", parent, new Vector3(x, h * 0.5f, stairsZ), new Vector3(6.8f, h, 52f), Quaternion.identity, pal.matRubble);
            CreateBlock($"GrandStairs_Edge_{i}", parent, new Vector3(x - 3.2f, h + 0.2f, stairsZ), new Vector3(0.5f, 0.4f, 52f), Quaternion.identity, pal.matHazard, false);
        }

        // Terraza superior panorámica de la escalinata
        float terraceX = stairsStartX + 12 * 6.8f + 36f;
        float terraceH = 12 * 1.40f;
        CreateBlock("GrandStairs_TopTerrace", parent, new Vector3(terraceX, terraceH - 1.0f, stairsZ), new Vector3(76f, 2.0f, 68f), Quaternion.identity, pal.matAsphalt);

        // --- SUBPRUEBA 3: PISTA DE PERALTE ASIMÉTRICO (STRADDLE CAMBER) ---
        float camberX = 290f;
        float camberZ = -100f;
        CreateBlock("Camber_Raised_LeftFootTrack", parent, new Vector3(camberX - 6.4f, 0.85f, camberZ), new Vector3(7.0f, 1.7f, 100f), Quaternion.identity, pal.matHazard);
        CreateBlock("Camber_Normal_RightFootTrack", parent, new Vector3(camberX + 6.4f, 0.1f, camberZ), new Vector3(7.0f, 0.2f, 100f), Quaternion.identity, pal.matAsphalt);
        CreateGroundLabel(parent, new Vector3(camberX, 0.2f, camberZ + 54f), "⚠ TEST DESNIVEL LATERAL: PATA IZQ +1.70M", pal);

        // --- SUBPRUEBA 4: ZANJAS DE ZANCADA (TRENCH CROSSING) ---
        float trenchX = 290f;
        float trenchZ = 110f;
        CreateBlock("Trench_Platform_1", parent, new Vector3(trenchX, -1.2f, trenchZ), new Vector3(100f, 2.4f, 20f), Quaternion.identity, pal.matAsphalt);
        CreateBlock("Trench_Bed_1", parent, new Vector3(trenchX, -7.0f, trenchZ + 12.4f), new Vector3(100f, 2.4f, 3.0f), Quaternion.identity, pal.matRock);
        CreateBlock("Trench_Platform_2", parent, new Vector3(trenchX, -1.2f, trenchZ + 24f), new Vector3(100f, 2.4f, 18f), Quaternion.identity, pal.matAsphalt);
        CreateBlock("Trench_Bed_2", parent, new Vector3(trenchX, -7.0f, trenchZ + 36.4f), new Vector3(100f, 2.4f, 4.8f), Quaternion.identity, pal.matRock);
        CreateBlock("Trench_Platform_3", parent, new Vector3(trenchX, -1.2f, trenchZ + 50f), new Vector3(100f, 2.4f, 20f), Quaternion.identity, pal.matAsphalt);
    }
    #endregion

    #region Sector 3: Slope Mountain (Rampas y Montaña)
    private static void BuildSlopeMountainSector(Transform parent, MaterialPalette pal)
    {
        CreateSignGantry("Gantry_Slopes_Entry", parent, new Vector3(130f, 0f, -200f), 92f, 52f, "► COLINA DE PENDIENTES GRADUADAS Y CIMA +56M", pal.matWall, pal.matAmber);

        // --- BATERÍA DE RAMPAS DE ÁNGULO GRADUADO ---
        float[] angles = new float[] { 12f, 24f, 34f, 44f, 56f };
        string[] slopeLabels = new string[] { "12° (Fácil)", "24° (Moderada)", "34° (Fuerte)", "44° (Límite 48°)", "56° (PELIGRO: VUELCO)" };

        float rampBaseX = 150f;
        float rampZ = -240f;
        float rampWidth = 24f;
        float rampLength = 64f;

        for (int i = 0; i < angles.Length; i++)
        {
            float ang = angles[i];
            float rad = ang * Mathf.Deg2Rad;
            float height = Mathf.Sin(rad) * rampLength;
            float horizontal = Mathf.Cos(rad) * rampLength;

            Vector3 rampPos = new Vector3(rampBaseX + i * 30f, height * 0.5f, rampZ - horizontal * 0.5f);
            Quaternion rampRot = Quaternion.Euler(-ang, 0f, 0f);

            CreateBlock($"SlopeRamp_{ang}deg", parent, rampPos, new Vector3(rampWidth, 1.6f, rampLength), rampRot, (ang >= 50f) ? pal.matHazard : pal.matRock);
            CreateGroundLabel(parent, new Vector3(rampBaseX + i * 30f, 0.2f, rampZ + 7f), slopeLabels[i], pal);

            Vector3 topPos = new Vector3(rampBaseX + i * 30f, height - 0.8f, rampZ - horizontal - 12f);
            CreateBlock($"SlopeTop_{ang}deg", parent, topPos, new Vector3(rampWidth, 1.6f, 24f), Quaternion.identity, pal.matAsphalt);
        }

        // --- CARRETERA SERPENTINA A LA GRAN CIMA (+56M) ---
        CreateBlock("Serpentine_Pass_1", parent, new Vector3(300f, 10f, -390f), new Vector3(36f, 2.4f, 104f), Quaternion.Euler(11f, 0f, 0f), pal.matAsphalt);
        CreateBlock("Serpentine_Switchback_1", parent, new Vector3(300f, 20f, -450f), new Vector3(72f, 2.4f, 56f), Quaternion.identity, pal.matAsphalt);

        CreateBlock("Serpentine_Pass_2", parent, new Vector3(360f, 34f, -400f), new Vector3(96f, 2.4f, 36f), Quaternion.Euler(0f, 0f, -14f), pal.matAsphalt);
        CreateBlock("Serpentine_Switchback_2", parent, new Vector3(420f, 48f, -340f), new Vector3(64f, 2.4f, 68f), Quaternion.identity, pal.matAsphalt);

        // Cima panorámica máxima (+56m)
        Vector3 summitPos = new Vector3(350f, 56f, -280f);
        CreateBlock("Mountain_Summit_Plateau", parent, summitPos, new Vector3(116f, 5.0f, 116f), Quaternion.identity, pal.matRock);
        CreateBlock("Summit_HeliPad_Ring", parent, summitPos + Vector3.up * 2.6f, new Vector3(64f, 0.2f, 64f), Quaternion.identity, pal.matHazard, false);
        CreateSignGantry("Gantry_Summit", parent, summitPos + new Vector3(0f, 2.6f, 48f), 92f, 52f, "🏔 CIMA PANORÁMICA MECHAQUACK (+56 METROS)", pal.matWall, pal.matCyan);
    }
    #endregion

    #region Sector 4: Overturn & Pataleta Pit (Foso de Vuelco)
    private static void BuildOverturnRecoverySector(Transform parent, MaterialPalette pal)
    {
        CreateSignGantry("Gantry_Overturn_Entry", parent, new Vector3(0f, 0f, -76f), 92f, 52f, "▼ FOSO DE VUELCO Y PRUEBA DE PATALETA HIDRÁULICA", pal.matWall, pal.matRed);

        // Avenida Sur de aproximación (56m de ancho)
        CreateBlock("South_Boulevard", parent, new Vector3(0f, -1.2f, -136f), new Vector3(56f, 2.4f, 110f), Quaternion.identity, pal.matAsphalt);

        Vector3 pitCenter = new Vector3(0f, -9.0f, -290f);

        // Cuenca excavada del foso (150m x 150m, profundidad 9.0m)
        CreateBlock("Pit_Basin_Floor", parent, pitCenter, new Vector3(150f, 3.0f, 150f), Quaternion.identity, pal.matAsphalt);

        CreateBlock("Pit_Wall_South", parent, pitCenter + new Vector3(0f, 6f, -75f), new Vector3(150f, 12f, 4f), Quaternion.identity, pal.matHazard);
        CreateBlock("Pit_Wall_East", parent, pitCenter + new Vector3(75f, 6f, 0f), new Vector3(4f, 12f, 150f), Quaternion.identity, pal.matHazard);
        CreateBlock("Pit_Wall_West", parent, pitCenter + new Vector3(-75f, 6f, 0f), new Vector3(4f, 12f, 150f), Quaternion.identity, pal.matHazard);

        // Cuña de balanceo a 65 grados
        CreateBlock("Overturn_Tilt_Wedge", parent, new Vector3(-24f, 1.0f, -200f), new Vector3(48f, 5.0f, 56f), Quaternion.Euler(0f, 0f, 65f), pal.matHazard);
        CreateGroundLabel(parent, new Vector3(-24f, 6.4f, -164f), "⚠ BANCO DE VUELCO A 65°: CAMINA AQUÍ PARA PROBAR PATALETA", pal);

        CreateBlock("Pataleta_Sandbox_Mat", parent, pitCenter + Vector3.up * 1.6f, new Vector3(72f, 0.4f, 72f), Quaternion.identity, pal.matRubble);

        // Rampa suave de salida (44m de ancho)
        CreateBlock("Pit_Exit_Ramp", parent, new Vector3(36f, -4.4f, -196f), new Vector3(44f, 2.0f, 64f), Quaternion.Euler(-10f, 0f, 0f), pal.matAsphalt);
        CreateGroundLabel(parent, new Vector3(36f, 0.4f, -160f), "▲ SALIDA DEL FOSO HACIA EL HUB", pal);
    }
    #endregion

    #region Sector 5: Badlands Rock Crawler (Terreno Rugoso)
    private static void BuildBadlandsSector(Transform parent, MaterialPalette pal, System.Random rng)
    {
        CreateSignGantry("Gantry_Badlands_Entry", parent, new Vector3(-76f, 0f, 0f), 92f, 52f, "◄ CAMPO DE ROCAS Y BADLANDS (TEST SUSPENSIÓN E IK)", pal.matWall, pal.matRock);

        // Conexión Boulevard Oeste desde el Hub (64m de ancho)
        CreateBlock("West_Boulevard_Entry", parent, new Vector3(-100f, -1.2f, 0f), new Vector3(64f, 2.4f, 56f), Quaternion.identity, pal.matAsphalt);

        Vector3 basePos = new Vector3(-300f, -1.6f, -80f);
        CreateBlock("Badlands_Base_Plateau", parent, basePos, new Vector3(360f, 3.2f, 300f), Quaternion.identity, pal.matRock);

        for (int i = 0; i < 75; i++)
        {
            float rx = (float)(rng.NextDouble() * 300.0 - 150.0) - 300f;
            float rz = (float)(rng.NextDouble() * 240.0 - 120.0) - 80f;

            if (Mathf.Abs(rz - (-80f)) < 20f && rx > -330f && rx < -170f) continue;

            float w = (float)(rng.NextDouble() * 9.0 + 5.0);
            float h = (float)(rng.NextDouble() * 2.6 + 0.70);
            float d = (float)(rng.NextDouble() * 9.0 + 5.0);
            float rotY = (float)(rng.NextDouble() * 180.0);
            float tiltX = (float)(rng.NextDouble() * 20.0 - 10.0);
            float tiltZ = (float)(rng.NextDouble() * 20.0 - 10.0);

            CreateBlock($"Boulder_{i}", parent, new Vector3(rx, h * 0.5f, rz), new Vector3(w, h, d), Quaternion.Euler(tiltX, rotY, tiltZ), pal.matRock);
        }

        // Pasillo Rock Crawler amplio y dinámico
        float crawlerZ = -80f;
        for (int i = 0; i < 16; i++)
        {
            float cx = -150f - i * 11f;
            float sideOffset = (i % 2 == 0) ? -6.4f : 6.4f;
            float stepH = 0.90f + (i % 4) * 0.56f;
            CreateBlock($"RockCrawler_Obstacle_{i}", parent, new Vector3(cx, stepH * 0.5f, crawlerZ + sideOffset), new Vector3(8.4f, stepH, 7.6f), Quaternion.Euler(5f, i * 25f, -6f), pal.matRock);
        }

        CreateGroundLabel(parent, new Vector3(-140f, 0.2f, crawlerZ + 16f), "🪨 CANAL ROCK CRAWLER: PRUEBA DE PISADA DINÁMICA SOBRE ROCAS", pal);

        CreateRockSpire(parent, new Vector3(-450f, 30f, -200f), 28f, 80f, pal.matRock);
        CreateRockSpire(parent, new Vector3(-420f, 36f, 50f), 32f, 92f, pal.matRock);
    }
    #endregion

    #region Sector 6: Toxic Acid Canyon & Bridges (Cañón de Ácido Despejado)
    private static void BuildAcidCanyonSector(Transform parent, MaterialPalette pal)
    {
        Vector3 canyonCenter = new Vector3(-270f, -25.0f, 260f);
        float canyonLength = 440f;
        float canyonWidth = 124f;

        // --- LA GRAN FOSA DE ÁCIDO TÓXICO ---
        GameObject acidObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        acidObj.name = "ToxicAcidRiver_Mega";
        acidObj.transform.parent = parent;
        acidObj.transform.position = canyonCenter;
        acidObj.transform.localScale = new Vector3(canyonLength, 2.8f, canyonWidth);
        if (pal.matAcid != null) acidObj.GetComponent<MeshRenderer>().material = pal.matAcid;
        var acidCollider = acidObj.GetComponent<BoxCollider>();
        if (acidCollider != null) acidCollider.isTrigger = true;
        acidObj.AddComponent<AcidHazard>();

        // Lecho y muros del cañón
        CreateBlock("Acid_Basin_Bed", parent, canyonCenter + Vector3.down * 7.0f, new Vector3(canyonLength + 40f, 10f, canyonWidth + 20f), Quaternion.identity, pal.matRock);
        CreateBlock("Canyon_Wall_South", parent, new Vector3(canyonCenter.x, -10f, canyonCenter.z - canyonWidth * 0.5f - 3.0f), new Vector3(canyonLength, 28f, 6f), Quaternion.identity, pal.matRock);
        CreateBlock("Canyon_Wall_North", parent, new Vector3(canyonCenter.x, -10f, canyonCenter.z + canyonWidth * 0.5f + 3.0f), new Vector3(canyonLength, 28f, 6f), Quaternion.identity, pal.matRock);

        // Márgenes del cañón (amplias carreteras transitables de 72m de ancho)
        CreateBlock("Canyon_Rim_South_Road", parent, new Vector3(canyonCenter.x, -1.2f, canyonCenter.z - canyonWidth * 0.5f - 36f), new Vector3(canyonLength, 2.4f, 72f), Quaternion.identity, pal.matAsphalt);
        CreateBlock("Canyon_Rim_North_Road", parent, new Vector3(canyonCenter.x, -1.2f, canyonCenter.z + canyonWidth * 0.5f + 36f), new Vector3(canyonLength, 2.4f, 72f), Quaternion.identity, pal.matAsphalt);

        // Arco de entrada al cañón: 92m de ancho, 52m de altura, sin collider
        CreateSignGantry("Gantry_Canyon_Entry", parent, new Vector3(-160f, 0f, 170f), 92f, 52f, "☣ EL GRAN CAÑÓN DE ÁCIDO: 3 PUENTES TOTALMENTE DESPEJADOS", pal.matWall, pal.matHazard);

        // --- PUENTE 1: AUTOPISTA PESADA BLINDADA (48M DE ANCHO - TOTALMENTE DESPEJADA) ---
        float bridge1X = -130f;
        CreateBlock("Bridge1_Highway_Deck", parent, new Vector3(bridge1X, -1.0f, canyonCenter.z), new Vector3(48f, 2.8f, canyonWidth + 12f), Quaternion.identity, pal.matAsphalt);
        // Bordillos visuales sin collider físico para que nunca traben las patas ni las armas
        CreateBlock("Bridge1_Rail_L", parent, new Vector3(bridge1X - 23.2f, 0.6f, canyonCenter.z), new Vector3(1.2f, 1.2f, canyonWidth + 12f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Bridge1_Rail_R", parent, new Vector3(bridge1X + 23.2f, 0.6f, canyonCenter.z), new Vector3(1.2f, 1.2f, canyonWidth + 12f), Quaternion.identity, pal.matHazard, false);
        CreateBlock("Bridge1_Pillar_S", parent, new Vector3(bridge1X, -13.0f, canyonCenter.z - 32f), new Vector3(16f, 26f, 16f), Quaternion.identity, pal.matRubble);
        CreateBlock("Bridge1_Pillar_N", parent, new Vector3(bridge1X, -13.0f, canyonCenter.z + 32f), new Vector3(16f, 26f, 16f), Quaternion.identity, pal.matRubble);
        CreateGroundLabel(parent, new Vector3(bridge1X, 0.4f, canyonCenter.z - canyonWidth * 0.5f - 8f), "PUENTE 1: AUTOPISTA ANCHA (48M)", pal);

        // --- PUENTE 2: PUENTE FRACTURADO (36M DE ANCHO - CARRIL CENTRAL LIMPIO) ---
        float bridge2X = -270f;
        CreateBlock("Bridge2_Span_South", parent, new Vector3(bridge2X, -1.0f, canyonCenter.z - 36f), new Vector3(36f, 2.6f, 52f), Quaternion.identity, pal.matAsphalt);
        CreateBlock("Bridge2_Broken_Slab", parent, new Vector3(bridge2X, -0.4f, canyonCenter.z), new Vector3(34f, 2.4f, 28f), Quaternion.Euler(0f, 0f, 2.5f), pal.matAsphalt);
        // Escombro colocado en el borde exterior derecho (X offset +13f), dejando un carril central despejado de 24m
        CreateBlock("Bridge2_Rubble_Obstacle", parent, new Vector3(bridge2X + 13f, 1.6f, canyonCenter.z), new Vector3(6.4f, 2.8f, 8.0f), Quaternion.Euler(10f, 25f, -8f), pal.matRubble);
        CreateBlock("Bridge2_Span_North", parent, new Vector3(bridge2X, -1.0f, canyonCenter.z + 36f), new Vector3(36f, 2.6f, 52f), Quaternion.identity, pal.matAsphalt);
        CreateGroundLabel(parent, new Vector3(bridge2X, 0.4f, canyonCenter.z - canyonWidth * 0.5f - 8f), "PUENTE 2: FRACTURADO (36M)", pal);

        // --- PUENTE 3: LA PASARELA SUSPENDIDA (26M DE ANCHO - SIN BARANDILLAS) ---
        float bridge3X = -390f;
        CreateBlock("Bridge3_Razor_Catwalk", parent, new Vector3(bridge3X, -0.8f, canyonCenter.z), new Vector3(26f, 2.4f, canyonWidth + 12f), Quaternion.Euler(0f, 0f, -2.5f), pal.matAsphalt);
        CreateGroundLabel(parent, new Vector3(bridge3X, 0.4f, canyonCenter.z - canyonWidth * 0.5f - 8f), "⚠ PUENTE 3: PASARELA SUSPENDIDA (26M - SIN BARANDILLA)", pal);

        // Pilares/Islas de roca transitables en medio del ácido (36m y 40m)
        CreateBlock("Acid_Stepping_Island_1", parent, new Vector3(-210f, -2.0f, canyonCenter.z - 16f), new Vector3(36f, 28f, 36f), Quaternion.identity, pal.matRock);
        CreateBlock("Acid_Stepping_Island_2", parent, new Vector3(-330f, -2.0f, canyonCenter.z + 16f), new Vector3(40f, 28f, 40f), Quaternion.identity, pal.matRock);
    }
    #endregion

    #region Sector 7: Ruined City Combat District (Distrito Urbano)
    private static void BuildRuinedCityDistrict(Transform parent, MaterialPalette pal, System.Random rng)
    {
        Vector3 cityCenter = new Vector3(290f, -1.2f, 350f);

        CreateSignGantry("Gantry_City_Entry", parent, new Vector3(130f, 0f, 200f), 92f, 52f, "🚨 DISTRITO URBANO EN RUINAS Y COMBATE TÁCTICO", pal.matWall, pal.matRed);

        CreateBlock("City_Grid_Ground", parent, cityCenter, new Vector3(360f, 2.4f, 360f), Quaternion.identity, pal.matAsphalt);

        // --- AUTOPISTA ELEVADA DE TIRO SNIPER (+15M) ---
        CreateBlock("Highway_Approach_Ramp", parent, new Vector3(170f, 7.0f, 260f), new Vector3(36f, 2.4f, 88f), Quaternion.Euler(11f, 0f, 0f), pal.matAsphalt);
        CreateBlock("Highway_Elevated_Deck", parent, new Vector3(170f, 15.0f, 390f), new Vector3(36f, 2.4f, 170f), Quaternion.identity, pal.matAsphalt);
        CreateBlock("Highway_Collapsed_Edge", parent, new Vector3(170f, 10.0f, 490f), new Vector3(36f, 2.4f, 36f), Quaternion.Euler(18f, 0f, 0f), pal.matRubble);

        Vector3 arenaCenter = new Vector3(320f, 0.1f, 360f);
        CreateBlock("Combat_Plaza_Ring", parent, arenaCenter, new Vector3(110f, 0.2f, 110f), Quaternion.identity, pal.matHazard, false);
        CreateGroundLabel(parent, arenaCenter + new Vector3(0f, 0.2f, 58f), "⚔ PLAZA DE TIRO: ACTIVA EL BOTÓN SPAWN PARA TESTEAR GATLING Y MISILES", pal);

        Vector3[] towerOffsets = new Vector3[] {
            new Vector3(-90f, 56f, -80f), new Vector3(50f, 68f, -90f), new Vector3(-80f, 52f, 70f),
            new Vector3(90f, 60f, 70f), new Vector3(-30f, 76f, 130f), new Vector3(70f, 64f, -30f),
            new Vector3(-110f, 44f, 0f), new Vector3(110f, 48f, 0f), new Vector3(0f, 84f, 110f)
        };

        for (int i = 0; i < towerOffsets.Length; i++)
        {
            Vector3 pos = cityCenter + towerOffsets[i];
            float w = (float)(rng.NextDouble() * 18.0 + 32.0);
            float h = (float)(rng.NextDouble() * 70.0 + 95.0); // Rascacielos de 95m a 165m para sobrepasar al mecha de 50m
            float d = (float)(rng.NextDouble() * 18.0 + 32.0);
            float tiltX = (float)(rng.NextDouble() * 16.0 - 8.0);
            float tiltZ = (float)(rng.NextDouble() * 16.0 - 8.0);
            float rotY = (float)(rng.NextDouble() * 90.0);

            CreateBlock($"Skyscraper_{i}", parent, new Vector3(pos.x, h * 0.5f, pos.z), new Vector3(w, h, d), Quaternion.Euler(tiltX, rotY, tiltZ), pal.matWall);
        }

        for (int i = 0; i < 18; i++)
        {
            float rx = (float)(rng.NextDouble() * 240.0 - 120.0) + cityCenter.x;
            float rz = (float)(rng.NextDouble() * 240.0 - 120.0) + cityCenter.z;
            float s = (float)(rng.NextDouble() * 7.0 + 6.0);
            CreateBlock($"City_RubbleBarricade_{i}", parent, new Vector3(rx, s * 0.5f, rz), new Vector3(s, s, s), Quaternion.Euler((float)(rng.NextDouble() * 30.0 - 15.0), (float)(rng.NextDouble() * 180.0), 0f), pal.matRubble);
        }
    }
    #endregion

    #region Sector 9: Connecting Highways (Red de Conexión)
    private static void BuildConnectingHighways(Transform parent, MaterialPalette pal)
    {
        // Gran Bulevar Perimetral Este-Oeste amplio (52m de ancho)
        CreateBlock("Mid_Boulevard_EastWest", parent, new Vector3(0f, -1.2f, 120f), new Vector3(640f, 2.4f, 52f), Quaternion.identity, pal.matAsphalt);

        // Bulevar Perimetral Norte conectando la Ciudad en Ruinas con la salida de la Pista (52m de ancho)
        CreateBlock("North_Ring_Boulevard", parent, new Vector3(190f, -1.2f, 660f), new Vector3(300f, 2.4f, 52f), Quaternion.identity, pal.matAsphalt);

        // Autopista diagonal conectando el Cañón Norte con la Pista de Velocidad (52m de ancho)
        CreateBlock("Canyon_To_Speedway_Connector", parent, new Vector3(-90f, -1.2f, 400f), new Vector3(130f, 2.4f, 52f), Quaternion.Euler(0f, 45f, 0f), pal.matAsphalt);
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

        height = Mathf.Max(height, 68f); // 68m de altura libre para Mecha de 50m
        spanWidth = Mathf.Max(spanWidth, 96f);

        // Postes y vigas estructurales limpias sin textos flotantes
        CreateBlock("Pillar_L", gantry.transform, pos + new Vector3(-spanWidth * 0.5f, height * 0.5f, 0f), new Vector3(3.6f, height, 3.6f), Quaternion.identity, frameMat, false);
        CreateBlock("Pillar_R", gantry.transform, pos + new Vector3(spanWidth * 0.5f, height * 0.5f, 0f), new Vector3(3.6f, height, 3.6f), Quaternion.identity, frameMat, false);
        CreateBlock("Crossbeam", gantry.transform, pos + new Vector3(0f, height - 1.4f, 0f), new Vector3(spanWidth + 4.0f, 3.2f, 3.6f), Quaternion.identity, frameMat, false);
    }

    private static void CreateDirectionPillar(Transform parent, Vector3 pos, string text, MaterialPalette pal)
    {
        // Sin textos flotantes en el mapa
    }

    private static void CreateGroundLabel(Transform parent, Vector3 pos, string text, MaterialPalette pal)
    {
        // Sin textos flotantes en el mapa
    }
    #endregion
}
