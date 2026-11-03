using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Constructor de Prefabs estándar de Unity para MechaQuack.
/// Genera y serializa prefabs limpios en la estructura estándar:
/// Assets/Prefabs/Mecha/
/// Assets/Prefabs/Cockpit/
/// Assets/Prefabs/Characters/
/// Assets/Prefabs/Props/Buttons/
/// Assets/Prefabs/Props/Consoles/
/// Assets/Prefabs/Enemies/
/// Assets/Prefabs/Weapons/
/// </summary>
public static class MechaPrefabBuilder
{
    private const string PREFABS_ROOT = "Assets/Prefabs";
    private const string MECHA_PATH = "Assets/Prefabs/Mecha";
    private const string COCKPIT_PATH = "Assets/Prefabs/Cockpit";
    private const string CHARACTERS_PATH = "Assets/Prefabs/Characters";
    private const string PROPS_BUTTONS_PATH = "Assets/Prefabs/Props/Buttons";
    private const string PROPS_CONSOLES_PATH = "Assets/Prefabs/Props/Consoles";
    private const string ENEMIES_PATH = "Assets/Prefabs/Enemies";
    private const string WEAPONS_PATH = "Assets/Prefabs/Weapons";

    [MenuItem("Mecha/Build All Standard Prefabs")]
    [MenuItem("PatoJuego/Build Standard Prefabs")]
    public static void BuildAllPrefabs()
    {
        EnsureFolderStructure();

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        Material GetOrCreateMat(string path, Color col, float metallic = 0.2f, float smoothness = 0.5f, bool isEmission = false, Color? emissionCol = null)
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
                Color em = emissionCol ?? (col * 1.5f);
                m.SetColor("_EmissionColor", em);
                m.EnableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        // 1. Material de Cristal de Cúpula Panorámica Ultra-Transparente (Máxima visibilidad)
        Material matDomeGlass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CockpitDomeGlass.mat");
        if (matDomeGlass == null)
        {
            matDomeGlass = new Material(urpLit);
            AssetDatabase.CreateAsset(matDomeGlass, "Assets/Materials/M_CockpitDomeGlass.mat");
        }
        matDomeGlass.shader = urpLit;
        matDomeGlass.SetFloat("_Surface", 1f); // Transparent
        matDomeGlass.SetFloat("_Blend", 0f);   // Alpha blend
        matDomeGlass.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        matDomeGlass.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        matDomeGlass.SetFloat("_ZWrite", 0f);
        matDomeGlass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        matDomeGlass.DisableKeyword("_ALPHATEST_ON");
        matDomeGlass.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        Color glassTint = new Color(0.92f, 0.98f, 1.0f, 0.04f); // 96% transparente, sin tinte oscuro que tape la vista
        matDomeGlass.color = glassTint;
        if (matDomeGlass.HasProperty("_BaseColor")) matDomeGlass.SetColor("_BaseColor", glassTint);
        if (matDomeGlass.HasProperty("_Smoothness")) matDomeGlass.SetFloat("_Smoothness", 0.95f);
        if (matDomeGlass.HasProperty("_Metallic")) matDomeGlass.SetFloat("_Metallic", 0.02f);
        EditorUtility.SetDirty(matDomeGlass);

        // Materiales estándar
        Material matMilGreen = GetOrCreateMat("Assets/Materials/M_MechaMilGreen.mat", new Color(0.18f, 0.27f, 0.18f), 0.25f, 0.40f);
        Material matGunmetal = GetOrCreateMat("Assets/Materials/M_MechaGunmetal.mat", new Color(0.14f, 0.16f, 0.19f), 0.50f, 0.65f);
        Material matDarkSteel = GetOrCreateMat("Assets/Materials/M_MechaDarkSteel.mat", new Color(0.08f, 0.09f, 0.10f), 0.65f, 0.55f);
        Material matChrome = GetOrCreateMat("Assets/Materials/M_MechaChrome.mat", new Color(0.88f, 0.91f, 0.95f), 0.90f, 0.92f);
        Material matFloor = GetOrCreateMat("Assets/Materials/M_CockpitFloor.mat", new Color(0.22f, 0.24f, 0.28f), 0.15f, 0.35f);
        Material matWall = GetOrCreateMat("Assets/Materials/M_CockpitWall.mat", new Color(0.35f, 0.38f, 0.42f), 0.3f, 0.5f);
        Material matHazard = GetOrCreateMat("Assets/Materials/M_HazardStripe.mat", new Color(0.96f, 0.72f, 0.05f), 0.1f, 0.6f);
        Material matMilGreenDark = GetOrCreateMat("Assets/Materials/M_MechaMilGreenDark.mat", new Color(0.11f, 0.17f, 0.11f), 0.25f, 0.45f);

        Material matDuckYellow = GetOrCreateMat("Assets/Materials/M_DuckYellow.mat", new Color(1.0f, 0.82f, 0.05f), 0.05f, 0.35f);
        Material matDuckOrange = GetOrCreateMat("Assets/Materials/M_DuckOrange.mat", new Color(1.0f, 0.45f, 0.02f), 0.05f, 0.4f);
        Material matDuckBlack = GetOrCreateMat("Assets/Materials/M_DuckBlack.mat", new Color(0.08f, 0.08f, 0.08f), 0.1f, 0.8f);

        Material matBtnGreen = GetOrCreateMat("Assets/Materials/M_BtnGreen.mat", new Color(0.06f, 0.72f, 0.51f), 0.1f, 0.8f, true, new Color(0.06f, 0.72f, 0.51f) * 1.6f);
        Material matBtnAmber = GetOrCreateMat("Assets/Materials/M_BtnAmber.mat", new Color(0.96f, 0.62f, 0.04f), 0.1f, 0.8f, true, new Color(0.96f, 0.62f, 0.04f) * 1.6f);
        Material matBtnCyan = GetOrCreateMat("Assets/Materials/M_BtnCyan.mat", new Color(0.02f, 0.71f, 0.83f), 0.1f, 0.8f, true, new Color(0.02f, 0.71f, 0.83f) * 1.8f);
        Material matBtnRose = GetOrCreateMat("Assets/Materials/M_BtnRose.mat", new Color(0.95f, 0.25f, 0.37f), 0.1f, 0.8f, true, new Color(0.95f, 0.25f, 0.37f) * 1.6f);
        Material matBtnBase = GetOrCreateMat("Assets/Materials/M_BtnBase.mat", new Color(0.06f, 0.09f, 0.16f), 0.6f, 0.5f);

        Material matEnemyRed = GetOrCreateMat("Assets/Materials/M_EnemyRed.mat", new Color(0.85f, 0.15f, 0.15f), 0.2f, 0.45f);
        Material matEnemyEye = GetOrCreateMat("Assets/Materials/M_EnemyEye.mat", new Color(1.0f, 0.8f, 0.1f), 0.1f, 0.8f, true, new Color(1.0f, 0.8f, 0.1f) * 2f);

        // =====================================================================
        // PREFAB 1: ENEMY BOT (Assets/Prefabs/Enemies/EnemyBot.prefab)
        // =====================================================================
        {
            GameObject botRoot = new GameObject("EnemyBot");
            BoxCollider col = botRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 5.0f, 0f);
            col.size = new Vector3(8.0f, 10.0f, 8.0f);

            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.parent = botRoot.transform;
            torso.transform.localPosition = new Vector3(0f, 6.0f, 0f);
            torso.transform.localScale = new Vector3(8.0f, 9.5f, 8.0f);
            torso.GetComponent<MeshRenderer>().material = matEnemyRed;
            Object.DestroyImmediate(torso.GetComponent<Collider>());

            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Visor";
            visor.transform.parent = torso.transform;
            visor.transform.localPosition = new Vector3(0f, 0.2f, 0.51f);
            visor.transform.localScale = new Vector3(0.8f, 0.2f, 0.15f);
            visor.GetComponent<MeshRenderer>().material = matEnemyEye;
            Object.DestroyImmediate(visor.GetComponent<Collider>());

            GameObject legL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legL.name = "Leg_L";
            legL.transform.parent = botRoot.transform;
            legL.transform.localPosition = new Vector3(-2.4f, 1.9f, 0f);
            legL.transform.localScale = new Vector3(2.5f, 3.8f, 3.2f);
            legL.GetComponent<MeshRenderer>().material = matDarkSteel;
            Object.DestroyImmediate(legL.GetComponent<Collider>());

            GameObject legR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legR.name = "Leg_R";
            legR.transform.parent = botRoot.transform;
            legR.transform.localPosition = new Vector3(2.4f, 1.9f, 0f);
            legR.transform.localScale = new Vector3(2.5f, 3.8f, 3.2f);
            legR.GetComponent<MeshRenderer>().material = matDarkSteel;
            Object.DestroyImmediate(legR.GetComponent<Collider>());

            botRoot.AddComponent<EnemyBot>();

            string prefabPath = Path.Combine(ENEMIES_PATH, "EnemyBot.prefab").Replace("\\", "/");
            PrefabUtility.SaveAsPrefabAsset(botRoot, prefabPath);
            Object.DestroyImmediate(botRoot);
        }

        // =====================================================================
        // PREFAB 2: DUCK PILOT (Assets/Prefabs/Characters/DuckPilot.prefab)
        // =====================================================================
        {
            GameObject duckRoot = new GameObject("DuckPilot");
            duckRoot.tag = "Player";

            BoxCollider col = duckRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(0.9f, 1.0f, 1.0f);

            Rigidbody rb = duckRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            GameObject visual = new GameObject("Visual");
            visual.transform.parent = duckRoot.transform;
            visual.transform.localPosition = Vector3.zero;

            GameObject body = CreatePrimitive(PrimitiveType.Sphere, "Duck_Body", visual.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.85f, 0.75f, 0.95f), Quaternion.identity, matDuckYellow);
            GameObject head = CreatePrimitive(PrimitiveType.Sphere, "Duck_Head", visual.transform, new Vector3(0f, 0.85f, 0.25f), new Vector3(0.55f, 0.55f, 0.55f), Quaternion.identity, matDuckYellow);
            CreatePrimitive(PrimitiveType.Cube, "Duck_Beak", visual.transform, new Vector3(0f, 0.8f, 0.58f), new Vector3(0.32f, 0.12f, 0.35f), Quaternion.identity, matDuckOrange);
            CreatePrimitive(PrimitiveType.Sphere, "Duck_Eye_L", visual.transform, new Vector3(-0.18f, 0.92f, 0.42f), Vector3.one * 0.12f, Quaternion.identity, matDuckBlack);
            CreatePrimitive(PrimitiveType.Sphere, "Duck_Eye_R", visual.transform, new Vector3(0.18f, 0.92f, 0.42f), Vector3.one * 0.12f, Quaternion.identity, matDuckBlack);
            GameObject wingL = CreatePrimitive(PrimitiveType.Sphere, "Duck_Wing_L", visual.transform, new Vector3(-0.44f, 0.42f, 0f), new Vector3(0.15f, 0.35f, 0.55f), Quaternion.Euler(0f, 0f, 20f), matDuckYellow);
            GameObject wingR = CreatePrimitive(PrimitiveType.Sphere, "Duck_Wing_R", visual.transform, new Vector3(0.44f, 0.42f, 0f), new Vector3(0.15f, 0.35f, 0.55f), Quaternion.Euler(0f, 0f, -20f), matDuckYellow);
            CreatePrimitive(PrimitiveType.Cube, "Duck_Foot_L", visual.transform, new Vector3(-0.25f, 0.05f, 0.05f), new Vector3(0.22f, 0.08f, 0.32f), Quaternion.identity, matDuckOrange);
            CreatePrimitive(PrimitiveType.Cube, "Duck_Foot_R", visual.transform, new Vector3(0.25f, 0.05f, 0.05f), new Vector3(0.22f, 0.08f, 0.32f), Quaternion.identity, matDuckOrange);

            GameObject dropShadow = CreatePrimitive(PrimitiveType.Cylinder, "DropShadow", duckRoot.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.75f, 0.005f, 0.75f), Quaternion.identity, matBtnBase);

            DuckController duckCtrl = duckRoot.AddComponent<DuckController>();
            SerializedObject duckSO = new SerializedObject(duckCtrl);
            duckSO.FindProperty("visualTransform").objectReferenceValue = visual.transform;
            duckSO.FindProperty("leftWingTransform").objectReferenceValue = wingL.transform;
            duckSO.FindProperty("rightWingTransform").objectReferenceValue = wingR.transform;
            duckSO.FindProperty("dropShadowTransform").objectReferenceValue = dropShadow.transform;
            duckSO.FindProperty("cockpitHalfX").floatValue = 10.2f;
            duckSO.FindProperty("cockpitHalfZ").floatValue = 7.8f;
            duckSO.FindProperty("cockpitMaxY").floatValue = 16.0f;
            duckSO.FindProperty("moveSpeed").floatValue = 14.0f;
            duckSO.FindProperty("jumpForce").floatValue = 10.5f;
            duckSO.FindProperty("stompSpeed").floatValue = 32.0f;
            duckSO.ApplyModifiedProperties();

            string prefabPath = Path.Combine(CHARACTERS_PATH, "DuckPilot.prefab").Replace("\\", "/");
            PrefabUtility.SaveAsPrefabAsset(duckRoot, prefabPath);
            Object.DestroyImmediate(duckRoot);
        }

        // =====================================================================
        // PREFABS 3: BOTONES FÍSICOS DE CABINA (Assets/Prefabs/Props/Buttons/)
        // =====================================================================
        void CreateButtonPrefab(string btnId, string label, CockpitFloorButton.ButtonType type, Material capMat, float diameter = 2.4f, float detectionRadius = 1.6f)
        {
            GameObject btnObj = new GameObject("Btn_" + btnId);
            CockpitFloorButton cfb = btnObj.AddComponent<CockpitFloorButton>();
            cfb.buttonId = btnId;
            cfb.label = label;
            cfb.type = type;
            cfb.detectionRadius = detectionRadius;

            GameObject housing = CreatePrimitive(PrimitiveType.Cylinder, "Housing", btnObj.transform, Vector3.zero, new Vector3(diameter * 1.15f, 0.22f, diameter * 1.15f), Quaternion.identity, matBtnBase);
            CreatePrimitive(PrimitiveType.Cylinder, "RimRing", housing.transform, new Vector3(0f, 0.55f, 0f), new Vector3(1.02f, 0.2f, 1.02f), Quaternion.identity, matHazard);

            GameObject cap = CreatePrimitive(PrimitiveType.Cylinder, "Cap", btnObj.transform, new Vector3(0f, 0.28f, 0f), new Vector3(diameter, 0.28f, diameter), Quaternion.identity, capMat);

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.parent = cap.transform;
            labelObj.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            labelObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            labelObj.transform.localScale = Vector3.one * (0.24f / diameter);

            TextMesh tm = labelObj.AddComponent<TextMesh>();
            tm.text = label;
            tm.fontSize = 24;
            tm.characterSize = 0.26f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = Color.white;
            TextShaderUtility.ApplyZTesting(tm);

            SerializedObject btnSO = new SerializedObject(cfb);
            btnSO.FindProperty("buttonId").stringValue = btnId;
            btnSO.FindProperty("label").stringValue = label;
            btnSO.FindProperty("type").enumValueIndex = (int)type;
            btnSO.FindProperty("detectionRadius").floatValue = detectionRadius;
            btnSO.FindProperty("capTransform").objectReferenceValue = cap.transform;
            btnSO.ApplyModifiedProperties();

            string prefabPath = Path.Combine(PROPS_BUTTONS_PATH, $"CockpitButton_{btnId}.prefab").Replace("\\", "/");
            PrefabUtility.SaveAsPrefabAsset(btnObj, prefabPath);
            Object.DestroyImmediate(btnObj);
        }

        CreateButtonPrefab("FWD", "▲ AVANZAR", CockpitFloorButton.ButtonType.Hold, matBtnGreen, 2.7f, 1.7f);
        CreateButtonPrefab("BWD", "▼ ATRÁS", CockpitFloorButton.ButtonType.Hold, matBtnAmber, 2.7f, 1.7f);
        CreateButtonPrefab("LEFT", "◄ GIRO IZQ", CockpitFloorButton.ButtonType.Hold, matBtnCyan, 2.6f, 1.6f);
        CreateButtonPrefab("RIGHT", "GIRO DER ►", CockpitFloorButton.ButtonType.Hold, matBtnCyan, 2.6f, 1.6f);
        CreateButtonPrefab("GATLING", "GATLING\n[MANTENER / E]", CockpitFloorButton.ButtonType.Hold, matBtnAmber, 3.2f, 1.9f);
        CreateButtonPrefab("MISSILES", "MISILES\n(CULAZO E)", CockpitFloorButton.ButtonType.Stomp, matBtnCyan, 3.2f, 1.9f);
        CreateButtonPrefab("SPAWN", "ROBOTS\n[ACTIVAR / PAUSAR]", CockpitFloorButton.ButtonType.Toggle, matBtnRose, 2.6f, 1.7f);

        // Prefab de Plataforma Giroscópica de Puntería (Gunner Aim Pad)
        GameObject aimPadObj = SetupGunnerAimPad.BuildAimPadHierarchy(null, Vector3.zero, matDarkSteel, matGunmetal, matChrome, matHazard, matBtnBase, matBtnCyan, matBtnGreen, matBtnAmber);
        string aimPadPrefabPath = Path.Combine(PROPS_BUTTONS_PATH, "CockpitButton_AIM_PAD.prefab").Replace("\\", "/");
        PrefabUtility.SaveAsPrefabAsset(aimPadObj, aimPadPrefabPath);
        Object.DestroyImmediate(aimPadObj);

        // =====================================================================
        // PREFABS 4: CONSOLAS TÁCTICAS SLEEK (Assets/Prefabs/Props/Consoles/)
        // =====================================================================
        {
            // Consola de Pilotaje
            GameObject pConRoot = new GameObject("PilotConsole");
            GameObject desk = CreatePrimitive(PrimitiveType.Cube, "Desk", pConRoot.transform, new Vector3(0f, 0.35f, 0f), new Vector3(5.5f, 0.7f, 1.0f), Quaternion.identity, matGunmetal);
            GameObject screen = CreatePrimitive(PrimitiveType.Cube, "Pilot_Nav_Screen", pConRoot.transform, new Vector3(0f, 0.85f, 0.05f), new Vector3(2.8f, 0.5f, 0.08f), Quaternion.Euler(-25f, 0f, 0f), matBtnGreen);

            string pathP = Path.Combine(PROPS_CONSOLES_PATH, "PilotConsole.prefab").Replace("\\", "/");
            PrefabUtility.SaveAsPrefabAsset(pConRoot, pathP);
            Object.DestroyImmediate(pConRoot);

            // Consola de Artillería
            GameObject gConRoot = new GameObject("GunnerConsole");
            GameObject gDesk = CreatePrimitive(PrimitiveType.Cube, "Desk", gConRoot.transform, new Vector3(0f, 0.35f, 0f), new Vector3(5.5f, 0.7f, 1.0f), Quaternion.identity, matDarkSteel);
            GameObject gScreen = CreatePrimitive(PrimitiveType.Cube, "Gunner_Ballistic_Screen", gConRoot.transform, new Vector3(0f, 0.85f, 0.05f), new Vector3(2.8f, 0.5f, 0.08f), Quaternion.Euler(-25f, 0f, 0f), matBtnRose);

            string pathG = Path.Combine(PROPS_CONSOLES_PATH, "GunnerConsole.prefab").Replace("\\", "/");
            PrefabUtility.SaveAsPrefabAsset(gConRoot, pathG);
            Object.DestroyImmediate(gConRoot);
        }

        // =====================================================================
        // PREFAB 5: PUESTO ELEVADO DEL MAQUINISTA (Assets/Prefabs/Characters/)
        // =====================================================================
        {
            GameObject stationRoot = new GameObject("MachinistGunnerStation");
            CreatePrimitive(PrimitiveType.Cube, "Station_Platform", stationRoot.transform, new Vector3(0f, 0.3f, 0f), new Vector3(5.2f, 0.6f, 3.8f), Quaternion.identity, matWall);
            CreatePrimitive(PrimitiveType.Cube, "Platform_Safety_Rail", stationRoot.transform, new Vector3(0f, 0.9f, 1.7f), new Vector3(5.2f, 0.6f, 0.2f), Quaternion.identity, matHazard);
            GameObject console = CreatePrimitive(PrimitiveType.Cube, "Machinist_Console", stationRoot.transform, new Vector3(0f, 1.2f, 0.8f), new Vector3(3.6f, 0.8f, 1.0f), Quaternion.identity, matGunmetal);
            GameObject radarScreen = CreatePrimitive(PrimitiveType.Cube, "Screen_Radar_Holo", stationRoot.transform, new Vector3(0f, 1.85f, 0.95f), new Vector3(1.6f, 0.9f, 0.1f), Quaternion.Euler(-20f, 0f, 0f), matBtnCyan);

            GameObject joyL = CreatePrimitive(PrimitiveType.Cylinder, "Joystick_Left", stationRoot.transform, new Vector3(-1.2f, 1.7f, 0.7f), new Vector3(0.12f, 0.35f, 0.12f), Quaternion.Euler(15f, -10f, 0f), matBtnAmber);
            GameObject joyR = CreatePrimitive(PrimitiveType.Cylinder, "Joystick_Right", stationRoot.transform, new Vector3(1.2f, 1.7f, 0.7f), new Vector3(0.12f, 0.35f, 0.12f), Quaternion.Euler(15f, 10f, 0f), matBtnAmber);

            CreatePrimitive(PrimitiveType.Cube, "Machinist_Bucket_Seat", stationRoot.transform, new Vector3(0f, 1.0f, -0.3f), new Vector3(1.4f, 0.8f, 1.4f), Quaternion.identity, matMilGreenDark);
            CreatePrimitive(PrimitiveType.Cube, "Seat_Backrest", stationRoot.transform, new Vector3(0f, 1.8f, -0.9f), new Vector3(1.4f, 1.2f, 0.3f), Quaternion.identity, matGunmetal);

            // Patito Artillero
            GameObject gBody = CreatePrimitive(PrimitiveType.Sphere, "Gunner_Body", stationRoot.transform, new Vector3(0f, 1.6f, -0.1f), new Vector3(0.85f, 0.75f, 0.95f), Quaternion.identity, matDuckYellow);
            GameObject gWingL = CreatePrimitive(PrimitiveType.Sphere, "Gunner_Wing_L", gBody.transform, new Vector3(-0.65f, 0.07f, 0.14f), new Vector3(0.2f, 0.5f, 0.7f), Quaternion.Euler(0f, 0f, 25f), matDuckYellow);
            GameObject gWingR = CreatePrimitive(PrimitiveType.Sphere, "Gunner_Wing_R", gBody.transform, new Vector3(0.65f, 0.07f, 0.14f), new Vector3(0.2f, 0.5f, 0.7f), Quaternion.Euler(0f, 0f, -25f), matDuckYellow);
            GameObject gHead = CreatePrimitive(PrimitiveType.Sphere, "Gunner_Head", gBody.transform, new Vector3(0f, 0.85f, 0.25f), new Vector3(0.6f, 0.6f, 0.6f), Quaternion.identity, matDuckYellow);

            CreatePrimitive(PrimitiveType.Cylinder, "Machinist_Cap", gHead.transform, new Vector3(0f, 0.45f, -0.07f), new Vector3(0.65f, 0.2f, 0.65f), Quaternion.identity, matDuckBlack);
            CreatePrimitive(PrimitiveType.Cube, "Cap_Visor", gHead.transform, new Vector3(0f, 0.42f, 0.43f), new Vector3(0.55f, 0.05f, 0.35f), Quaternion.identity, matHazard);
            CreatePrimitive(PrimitiveType.Cube, "Tactical_Goggles", gHead.transform, new Vector3(0f, 0.08f, 0.45f), new Vector3(0.52f, 0.18f, 0.14f), Quaternion.identity, matBtnCyan);
            CreatePrimitive(PrimitiveType.Cube, "Gunner_Beak", gHead.transform, new Vector3(0f, -0.11f, 0.50f), new Vector3(0.35f, 0.12f, 0.38f), Quaternion.identity, matDuckOrange);

            GameObject lightObj = new GameObject("Screen_Tactical_Light");
            lightObj.transform.parent = radarScreen.transform;
            lightObj.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            Light sl = lightObj.AddComponent<Light>();
            sl.type = LightType.Point;
            sl.range = 6f;
            sl.intensity = 1.5f;
            sl.color = Color.cyan;

            MechaMachinistGunner gunnerComp = stationRoot.AddComponent<MechaMachinistGunner>();
            SerializedObject gunnerSO = new SerializedObject(gunnerComp);
            gunnerSO.FindProperty("gunnerBodyTransform").objectReferenceValue = gBody.transform;
            gunnerSO.FindProperty("gunnerHeadTransform").objectReferenceValue = gHead.transform;
            gunnerSO.FindProperty("joystickLeft").objectReferenceValue = joyL.transform;
            gunnerSO.FindProperty("joystickRight").objectReferenceValue = joyR.transform;
            gunnerSO.FindProperty("targetingScreenTransform").objectReferenceValue = radarScreen.transform;
            gunnerSO.FindProperty("tacticalScreenLight").objectReferenceValue = sl;
            gunnerSO.ApplyModifiedProperties();

            string pathS = Path.Combine(CHARACTERS_PATH, "MachinistGunner.prefab").Replace("\\", "/");
            PrefabUtility.SaveAsPrefabAsset(stationRoot, pathS);
            Object.DestroyImmediate(stationRoot);
        }

        // =====================================================================
        // PREFAB 6: CÚPULA PANORÁMICA DE CABINA MULTINIVEL (Assets/Prefabs/Cockpit/CockpitDome.prefab)
        // Edificio interior vertical de 3 plantas conectado exclusivamente por escaleras físicas
        // =====================================================================
        {
            GameObject domeRoot = new GameObject("CockpitDome");

            // Función local para construir escaleras industriales con colisionador de rampa invisible suave
            void CreatePrefabStaircase(string name, Transform parent, Vector3 startPos, Vector3 endPos, float width, Material stepMat, Material railMat, Material frameMat)
            {
                GameObject stairRoot = new GameObject(name);
                stairRoot.transform.parent = parent;
                stairRoot.transform.localPosition = Vector3.zero;
                stairRoot.transform.localRotation = Quaternion.identity;

                Vector3 delta = endPos - startPos;
                Vector3 horizDelta = new Vector3(delta.x, 0f, delta.z);
                float horizDist = horizDelta.magnitude;
                float riseY = delta.y;
                float slopeLength = Mathf.Sqrt(horizDist * horizDist + riseY * riseY);
                Vector3 horizDir = horizDist > 0.001f ? horizDelta.normalized : Vector3.forward;
                float pitchAngle = Mathf.Atan2(riseY, horizDist) * Mathf.Rad2Deg;

                Quaternion rampRot = Quaternion.LookRotation(horizDir, Vector3.up) * Quaternion.Euler(-pitchAngle, 0f, 0f);

                int numSteps = 16;
                float stepH = riseY / numSteps;
                float stepD = (horizDist / numSteps) * 1.15f;

                for (int i = 0; i < numSteps; i++)
                {
                    float t = (i + 0.5f) / numSteps;
                    Vector3 stepPos = Vector3.Lerp(startPos, endPos, t);
                    stepPos.y = startPos.y + (i + 0.5f) * stepH;
                    CreatePrimitive(PrimitiveType.Cube, $"Step_{i}", stairRoot.transform, stepPos, new Vector3(width, stepH * 1.05f, stepD), Quaternion.identity, stepMat, true);
                }

                // Plataformas horizontales de aterrizaje/embarque
                Vector3 bottomLanding = startPos - horizDir * 0.75f + new Vector3(0f, -0.1f, 0f);
                CreatePrimitive(PrimitiveType.Cube, "Stair_Bottom_Landing", stairRoot.transform, bottomLanding, new Vector3(width, 0.25f, 1.5f), Quaternion.identity, stepMat, true);

                Vector3 topLanding = endPos + horizDir * 0.75f + new Vector3(0f, -0.1f, 0f);
                CreatePrimitive(PrimitiveType.Cube, "Stair_Top_Landing", stairRoot.transform, topLanding, new Vector3(width, 0.25f, 1.5f), Quaternion.identity, stepMat, true);

                // Pasamanos tubulares
                float halfW = width * 0.5f;
                Vector3 sideCross = Vector3.Cross(Vector3.up, horizDir).normalized;
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 railOffset = sideCross * (side * (halfW + 0.12f));
                    Vector3 rStart = startPos + railOffset + new Vector3(0f, 1.1f, 0f);
                    Vector3 rEnd = endPos + railOffset + new Vector3(0f, 1.1f, 0f);
                    Vector3 rMid = (rStart + rEnd) * 0.5f;

                    CreatePrimitive(PrimitiveType.Cylinder, $"Handrail_{(side < 0 ? "L" : "R")}", stairRoot.transform, rMid, new Vector3(0.12f, slopeLength * 0.5f, 0.12f), rampRot * Quaternion.Euler(90f, 0f, 0f), railMat, false);

                    for (int p = 0; p <= 4; p++)
                    {
                        float t = p / 4.0f;
                        Vector3 postPos = Vector3.Lerp(rStart, rEnd, t) - new Vector3(0f, 0.55f, 0f);
                        CreatePrimitive(PrimitiveType.Cylinder, $"Post_{(side < 0 ? "L" : "R")}_{p}", stairRoot.transform, postPos, new Vector3(0.12f, 0.55f, 0.12f), Quaternion.identity, railMat, false);
                    }
                }

                // Colisionador de rampa invisible grueso (1.2m) e impenetrable
                Vector3 rampMid = (startPos + endPos) * 0.5f;
                GameObject rampColliderObj = new GameObject("Invisible_Ramp_Collider");
                rampColliderObj.transform.parent = stairRoot.transform;
                rampColliderObj.transform.localPosition = rampMid;
                rampColliderObj.transform.localRotation = rampRot;

                BoxCollider bc = rampColliderObj.AddComponent<BoxCollider>();
                bc.size = new Vector3(width, 1.2f, slopeLength + 0.6f);
                bc.center = new Vector3(0f, -0.52f, 0f);
            }

            // --- PLANTA 1: CONDUCTOR / NAVEGACIÓN (Y = 0m) - BÚNKER BLINDADO ---
            CreatePrimitive(PrimitiveType.Cube, "Floor_Level1", domeRoot.transform, new Vector3(0f, -0.2f, 0f), new Vector3(20.0f, 0.4f, 15.0f), Quaternion.identity, matFloor, true);
            CreatePrimitive(PrimitiveType.Cube, "Rim_L1_L", domeRoot.transform, new Vector3(-10.1f, 0.3f, 0f), new Vector3(0.4f, 0.6f, 15.0f), Quaternion.identity, matWall);
            CreatePrimitive(PrimitiveType.Cube, "Rim_L1_R", domeRoot.transform, new Vector3(10.1f, 0.3f, 0f), new Vector3(0.4f, 0.6f, 15.0f), Quaternion.identity, matWall);
            CreatePrimitive(PrimitiveType.Cube, "Rim_L1_F", domeRoot.transform, new Vector3(0f, 0.3f, 7.4f), new Vector3(20.0f, 0.6f, 0.4f), Quaternion.identity, matHazard);
            CreatePrimitive(PrimitiveType.Cube, "Rim_L1_B", domeRoot.transform, new Vector3(0f, 0.3f, -7.4f), new Vector3(20.0f, 0.6f, 0.4f), Quaternion.identity, matWall);

            // Muros perimetrales blindados de Planta 1 (Visión reducida: guiado por el Capitán)
            CreatePrimitive(PrimitiveType.Cube, "Side_Armor_L1_L", domeRoot.transform, new Vector3(-10.0f, 2.2f, 0f), new Vector3(0.4f, 4.0f, 15.0f), Quaternion.identity, matGunmetal);
            CreatePrimitive(PrimitiveType.Cube, "Side_Armor_L1_R", domeRoot.transform, new Vector3(10.0f, 2.2f, 0f), new Vector3(0.4f, 4.0f, 15.0f), Quaternion.identity, matGunmetal);
            CreatePrimitive(PrimitiveType.Cube, "Rear_Armor_L1", domeRoot.transform, new Vector3(0f, 2.2f, -7.4f), new Vector3(20.0f, 4.0f, 0.4f), Quaternion.identity, matMilGreenDark);

            // Glacis blindado frontal y tronera estrecha de observación
            CreatePrimitive(PrimitiveType.Cube, "Armor_Glacis_L1_Bottom", domeRoot.transform, new Vector3(0f, 0.35f, 7.5f), new Vector3(20.0f, 0.7f, 0.4f), Quaternion.identity, matMilGreen);
            CreatePrimitive(PrimitiveType.Cube, "Armor_Glacis_L1_Top", domeRoot.transform, new Vector3(0f, 2.95f, 7.5f), new Vector3(20.0f, 2.7f, 0.4f), Quaternion.identity, matMilGreen);
            CreatePrimitive(PrimitiveType.Cube, "Armor_Cheek_L1_L", domeRoot.transform, new Vector3(-8.5f, 1.15f, 7.5f), new Vector3(3.0f, 0.9f, 0.4f), Quaternion.identity, matGunmetal);
            CreatePrimitive(PrimitiveType.Cube, "Armor_Cheek_L1_R", domeRoot.transform, new Vector3(8.5f, 1.15f, 7.5f), new Vector3(3.0f, 0.9f, 0.4f), Quaternion.identity, matGunmetal);
            CreatePrimitive(PrimitiveType.Cube, "Windshield_Level1", domeRoot.transform, new Vector3(0f, 1.15f, 7.5f), new Vector3(14.0f, 0.9f, 0.12f), Quaternion.identity, matDomeGlass);
            CreatePrimitive(PrimitiveType.Cube, "Slit_Frame_Top", domeRoot.transform, new Vector3(0f, 1.63f, 7.55f), new Vector3(14.2f, 0.12f, 0.25f), Quaternion.identity, matDarkSteel);
            CreatePrimitive(PrimitiveType.Cube, "Slit_Frame_Bottom", domeRoot.transform, new Vector3(0f, 0.67f, 7.55f), new Vector3(14.2f, 0.12f, 0.25f), Quaternion.identity, matDarkSteel);

            // Tramo de Escaleras 1 (Planta 1 -> Planta 2): de Y=0m a Y=4.5m
            CreatePrefabStaircase("Stairs_Level1_to_Level2", domeRoot.transform, new Vector3(7.2f, 0.0f, -4.5f), new Vector3(7.2f, 4.5f, 3.5f), 2.6f, matDarkSteel, matHazard, matGunmetal);

            // --- PLANTA 2: ARTILLERO / ARSENAL (Y = 4.5m) - MÁXIMA VISIBILIDAD PANORÁMICA ---
            CreatePrimitive(PrimitiveType.Cube, "Floor_Level2_Center", domeRoot.transform, new Vector3(0f, 4.3f, 0f), new Vector3(11.8f, 0.4f, 15.0f), Quaternion.identity, matFloor, true);
            CreatePrimitive(PrimitiveType.Cube, "Floor_Level2_Front", domeRoot.transform, new Vector3(0f, 4.3f, 5.5f), new Vector3(20.0f, 0.4f, 4.0f), Quaternion.identity, matFloor, true);
            CreatePrimitive(PrimitiveType.Cube, "Floor_Level2_Rear", domeRoot.transform, new Vector3(0f, 4.3f, -6.0f), new Vector3(20.0f, 0.4f, 3.0f), Quaternion.identity, matFloor, true);
            CreatePrimitive(PrimitiveType.Cube, "Floor_Level2_LeftWing", domeRoot.transform, new Vector3(-7.95f, 4.3f, -0.5f), new Vector3(4.1f, 0.4f, 8.0f), Quaternion.identity, matFloor, true);
            CreatePrimitive(PrimitiveType.Cube, "Floor_Level2_RightOuter", domeRoot.transform, new Vector3(9.25f, 4.3f, -0.5f), new Vector3(1.5f, 0.4f, 8.0f), Quaternion.identity, matFloor, true);

            CreatePrimitive(PrimitiveType.Cube, "L2_Stair1_Safety_Rail", domeRoot.transform, new Vector3(5.8f, 5.0f, -0.5f), new Vector3(0.12f, 1.0f, 8.0f), Quaternion.identity, matHazard);
            CreatePrimitive(PrimitiveType.Cube, "Windshield_Level2", domeRoot.transform, new Vector3(0f, 6.8f, 7.5f), new Vector3(19.6f, 4.6f, 0.12f), Quaternion.identity, matDomeGlass);
            CreatePrimitive(PrimitiveType.Cube, "Side_Glass_L2_L", domeRoot.transform, new Vector3(-10.0f, 6.8f, 0f), new Vector3(0.12f, 4.6f, 15.0f), Quaternion.identity, matDomeGlass);
            CreatePrimitive(PrimitiveType.Cube, "Side_Glass_L2_R", domeRoot.transform, new Vector3(10.0f, 6.8f, 0f), new Vector3(0.12f, 4.6f, 15.0f), Quaternion.identity, matDomeGlass);

            // Tramo de Escaleras 2 (Planta 2 -> Planta 3): de Y=4.5m a Y=9.0m
            CreatePrefabStaircase("Stairs_Level2_to_Level3", domeRoot.transform, new Vector3(-7.2f, 4.5f, 3.5f), new Vector3(-7.2f, 9.0f, -4.5f), 2.6f, matDarkSteel, matHazard, matGunmetal);

            // --- PLANTA 3: CABEZA DEL MECHA / CÁPSULA DEL CAPITÁN (Y = 9.0m) ---
            // Interior blindado cerebral: pasa automáticamente a 3ª persona exterior
            CreatePrimitive(PrimitiveType.Cube, "Floor_Level3_Center", domeRoot.transform, new Vector3(0f, 8.8f, 0f), new Vector3(11.8f, 0.4f, 15.0f), Quaternion.identity, matFloor, true);
            CreatePrimitive(PrimitiveType.Cube, "Floor_Level3_Front", domeRoot.transform, new Vector3(0f, 8.8f, 5.5f), new Vector3(20.0f, 0.4f, 4.0f), Quaternion.identity, matFloor, true);
            CreatePrimitive(PrimitiveType.Cube, "Floor_Level3_Rear", domeRoot.transform, new Vector3(0f, 8.8f, -6.0f), new Vector3(20.0f, 0.4f, 3.0f), Quaternion.identity, matFloor, true);
            CreatePrimitive(PrimitiveType.Cube, "Floor_Level3_RightWing", domeRoot.transform, new Vector3(7.95f, 8.8f, -0.5f), new Vector3(4.1f, 0.4f, 8.0f), Quaternion.identity, matFloor, true);
            CreatePrimitive(PrimitiveType.Cube, "Floor_Level3_LeftOuter", domeRoot.transform, new Vector3(-9.25f, 8.8f, -0.5f), new Vector3(1.5f, 0.4f, 8.0f), Quaternion.identity, matFloor, true);

            CreatePrimitive(PrimitiveType.Cube, "L3_Stair2_Safety_Rail", domeRoot.transform, new Vector3(-5.8f, 9.5f, -0.5f), new Vector3(0.12f, 1.0f, 8.0f), Quaternion.identity, matHazard);

            // Estructura blindada de la Cabeza del Mecha
            CreatePrimitive(PrimitiveType.Cube, "Head_Hull_Front", domeRoot.transform, new Vector3(0f, 11.5f, 7.2f), new Vector3(19.6f, 5.2f, 0.4f), Quaternion.identity, matMilGreen);
            CreatePrimitive(PrimitiveType.Cube, "Head_Hull_Roof", domeRoot.transform, new Vector3(0f, 14.5f, 0f), new Vector3(19.2f, 0.4f, 14.5f), Quaternion.identity, matMilGreen);
            CreatePrimitive(PrimitiveType.Cube, "Head_Hull_Side_L", domeRoot.transform, new Vector3(-9.8f, 11.5f, 0f), new Vector3(0.4f, 5.2f, 14.5f), Quaternion.identity, matGunmetal);
            CreatePrimitive(PrimitiveType.Cube, "Head_Hull_Side_R", domeRoot.transform, new Vector3(9.8f, 11.5f, 0f), new Vector3(0.4f, 5.2f, 14.5f), Quaternion.identity, matGunmetal);
            CreatePrimitive(PrimitiveType.Cube, "Head_Hull_Rear", domeRoot.transform, new Vector3(0f, 11.5f, -7.2f), new Vector3(19.6f, 5.2f, 0.4f), Quaternion.identity, matMilGreenDark);

            // Nervaduras y pilares de la Cabeza
            CreatePrimitive(PrimitiveType.Cube, "Head_Rib_Front_Brow", domeRoot.transform, new Vector3(0f, 14.4f, 7.2f), new Vector3(20.0f, 0.35f, 0.35f), Quaternion.identity, matDarkSteel);
            CreatePrimitive(PrimitiveType.Cube, "Head_Rib_Mid_Hoop", domeRoot.transform, new Vector3(0f, 14.6f, 0f), new Vector3(20.0f, 0.35f, 0.35f), Quaternion.identity, matDarkSteel);
            CreatePrimitive(PrimitiveType.Cube, "Head_Rib_Rear_Hoop", domeRoot.transform, new Vector3(0f, 14.4f, -7.2f), new Vector3(20.0f, 0.35f, 0.35f), Quaternion.identity, matDarkSteel);

            // Anillo de telemetría neural holográfica
            CreatePrimitive(PrimitiveType.Cylinder, "Neural_Holo_Ring", domeRoot.transform, new Vector3(0f, 13.8f, 1.5f), new Vector3(6.5f, 0.08f, 6.5f), Quaternion.identity, matBtnCyan);

            // Pedestal de mando del Capitán
            GameObject captainPedestal = CreatePrimitive(PrimitiveType.Cylinder, "Captain_Command_Pedestal", domeRoot.transform, new Vector3(0f, 9.2f, 1.5f), new Vector3(4.4f, 0.2f, 4.4f), Quaternion.identity, matGunmetal, true);
            captainPedestal.AddComponent<CaptainCommandPedestal>();
            CreatePrimitive(PrimitiveType.Cylinder, "Pedestal_Hazard_Ring", captainPedestal.transform, new Vector3(0f, 0.52f, 0f), new Vector3(1.02f, 0.2f, 1.02f), Quaternion.identity, matHazard);
            CreatePrimitive(PrimitiveType.Cylinder, "Captain_Holo_Disc", captainPedestal.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 0.05f, 0.7f), Quaternion.identity, matBtnCyan);

            // Iluminación táctica multinivel
            void AddDomeLight(string name, Vector3 pos, Color c, float range, float intensity)
            {
                GameObject lo = new GameObject(name);
                lo.transform.parent = domeRoot.transform;
                lo.transform.localPosition = pos;
                Light l = lo.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = range;
                l.intensity = intensity;
                l.color = c;
            }
            AddDomeLight("Pilot_Dome_Light", new Vector3(0f, 3.8f, 1.0f), new Color(0.85f, 0.95f, 1.0f), 16f, 1.8f);
            AddDomeLight("Gunner_Dome_Light", new Vector3(0f, 8.2f, 1.8f), new Color(1.0f, 0.92f, 0.85f), 16f, 1.8f);
            AddDomeLight("Captain_Dome_Light", new Vector3(0f, 14.0f, 0f), new Color(0.9f, 0.95f, 1.0f), 20f, 2.2f);

            string pathD = Path.Combine(COCKPIT_PATH, "CockpitDome.prefab").Replace("\\", "/");
            PrefabUtility.SaveAsPrefabAsset(domeRoot, pathD);
            Object.DestroyImmediate(domeRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ ¡Todos los Prefabs estándar de Unity para MechaQuack han sido construidos con éxito en Assets/Prefabs/!");
    }

    private static void EnsureFolderStructure()
    {
        void EnsureDir(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        EnsureDir("Assets", "Prefabs");
        EnsureDir("Assets/Prefabs", "Mecha");
        EnsureDir("Assets/Prefabs", "Cockpit");
        EnsureDir("Assets/Prefabs", "Characters");
        EnsureDir("Assets/Prefabs", "Props");
        EnsureDir("Assets/Prefabs/Props", "Buttons");
        EnsureDir("Assets/Prefabs/Props", "Consoles");
        EnsureDir("Assets/Prefabs", "Enemies");
        EnsureDir("Assets/Prefabs", "Weapons");
    }

    private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Quaternion localRot, Material mat, bool addCollider = false)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.parent = parent;
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.transform.localRotation = localRot;
        if (mat != null) go.GetComponent<MeshRenderer>().material = mat;
        if (!addCollider)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
        return go;
    }
}
