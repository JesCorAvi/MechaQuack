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

        // 1. Inicializar motor de texturizado procedural PBR y caché de materiales
        ProceduralTextureGenerator.SetupAllMaterials(true);

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        Material GetMat(string matId)
        {
            return ProceduralTextureGenerator.GetMaterial(matId) ?? AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{matId}.mat");
        }

        // Materiales de cabina, pato y entorno
        Material matDomeGlass = GetMat("M_CockpitDomeGlass");
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
        Color glassTint = new Color(0.92f, 0.98f, 1.0f, 0.04f); // 96% transparente
        matDomeGlass.color = glassTint;
        if (matDomeGlass.HasProperty("_BaseColor")) matDomeGlass.SetColor("_BaseColor", glassTint);
        if (matDomeGlass.HasProperty("_Smoothness")) matDomeGlass.SetFloat("_Smoothness", 0.95f);
        if (matDomeGlass.HasProperty("_Metallic")) matDomeGlass.SetFloat("_Metallic", 0.02f);
        EditorUtility.SetDirty(matDomeGlass);

        // Materiales Real Robot (Gundam / Armored Core)
        Material matArmorWhite = GetMat("M_MechaArmorWhite") ?? GetMat("M_MechaMilGreen");
        Material matCobaltBlue = GetMat("M_MechaCobaltBlue") ?? GetMat("M_MechaMilGreenDark");
        Material matInnerFrame = GetMat("M_MechaInnerFrame") ?? GetMat("M_MechaDarkSteel");
        Material matCrimsonRed = GetMat("M_MechaCrimsonRed") ?? GetMat("M_BtnRose");
        Material matChestVent = GetMat("M_MechaChestVent") ?? GetMat("M_HazardStripe");
        Material matGoldVFin = GetMat("M_MechaGoldVFin") ?? GetMat("M_MechaBrass");

        Material matMilGreen = GetMat("M_MechaMilGreen");
        Material matMilGreenDark = GetMat("M_MechaMilGreenDark");
        Material matGunmetal = GetMat("M_MechaGunmetal");
        Material matDarkSteel = GetMat("M_MechaDarkSteel");
        Material matChrome = GetMat("M_MechaChrome");
        Material matBrass = GetMat("M_MechaBrass");
        Material matHazard = GetMat("M_HazardStripe");

        // Materiales PBR dedicados por planta
        Material matFloor1Plate = GetMat("M_Floor1_DiamondPlate") ?? GetMat("M_CockpitFloor");
        Material matFloor1Armor = GetMat("M_Floor1_RivetedArmor") ?? GetMat("M_CockpitWall");
        Material matFloor1Reactor = GetMat("M_Floor1_ReactorCore") ?? GetMat("M_MechaCoreOrange");
        Material matFloor1Radar = GetMat("M_Floor1_CRTRadar") ?? GetMat("M_BtnGreen");

        Material matFloor2Deck = GetMat("M_Floor2_CarrierDeck") ?? GetMat("M_CockpitFloor");
        Material matFloor2Cryo = GetMat("M_Floor2_CryoFrost") ?? GetMat("M_MechaChrome");
        Material matFloor2Gauge = GetMat("M_Floor2_PressureGauge") ?? GetMat("M_MechaBrass");
        Material matFloor2Aim = GetMat("M_Floor2_AimPad") ?? GetMat("M_MechaDarkSteel");

        Material matFloor3Carbon = GetMat("M_Floor3_CarbonHex") ?? GetMat("M_CockpitWall");
        Material matFloor3Circuit = GetMat("M_Floor3_NeuralCircuit") ?? GetMat("M_MechaCyanOptics");
        Material matFloor3Server = GetMat("M_Floor3_Supercomputer") ?? GetMat("M_MechaGunmetal");
        Material matFloor3Holo = GetMat("M_Floor3_HoloDisc") ?? GetMat("M_MechaCyanOptics");

        Material matDuckYellow = GetMat("M_DuckRubberPBR") ?? GetMat("M_DuckYellow");
        Material matDuckOrange = GetMat("M_DuckBeakPBR") ?? GetMat("M_DuckOrange");
        Material matDuckBlack = GetMat("M_DuckTacticalPBR") ?? GetMat("M_DuckBlack");

        Material matBtnGreen = GetMat("M_Btn_FWD") ?? GetMat("M_BtnGreen");
        Material matBtnAmber = GetMat("M_Btn_BWD") ?? GetMat("M_BtnAmber");
        Material matBtnCyan = GetMat("M_Btn_LEFT") ?? GetMat("M_BtnCyan");
        Material matBtnRose = GetMat("M_Btn_SPAWN") ?? GetMat("M_BtnRose");
        Material matBtnBase = GetMat("M_BtnBase");

        Material matEnemyRed = GetMat("M_EnemyRed") ?? matMilGreenDark;
        Material matEnemyEye = GetMat("M_EnemyEye") ?? matBtnRose;

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
            torso.transform.SetParent(botRoot.transform, false);
            torso.transform.localPosition = new Vector3(0f, 6.0f, 0f);
            torso.transform.localScale = new Vector3(8.0f, 9.5f, 8.0f);
            torso.GetComponent<MeshRenderer>().material = matEnemyRed;
            Object.DestroyImmediate(torso.GetComponent<Collider>());

            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Visor";
            visor.transform.SetParent(torso.transform, false);
            visor.transform.localPosition = new Vector3(0f, 0.2f, 0.51f);
            visor.transform.localScale = new Vector3(0.8f, 0.2f, 0.15f);
            visor.GetComponent<MeshRenderer>().material = matEnemyEye;
            Object.DestroyImmediate(visor.GetComponent<Collider>());

            GameObject legL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legL.name = "Leg_L";
            legL.transform.SetParent(botRoot.transform, false);
            legL.transform.localPosition = new Vector3(-2.4f, 1.9f, 0f);
            legL.transform.localScale = new Vector3(2.5f, 3.8f, 3.2f);
            legL.GetComponent<MeshRenderer>().material = matDarkSteel;
            Object.DestroyImmediate(legL.GetComponent<Collider>());

            GameObject legR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legR.name = "Leg_R";
            legR.transform.SetParent(botRoot.transform, false);
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
        // PREFAB 2: DUCK PILOT PBR (Assets/Prefabs/Characters/DuckPilot.prefab)
        // Escala 0.38x para dar la sensación de complejidad masiva x2.5 interior
        // =====================================================================
        {
            GameObject duckRoot = new GameObject("DuckPilot");
            duckRoot.transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);
            duckRoot.tag = "Player";

            BoxCollider col = duckRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.25f, 0f);
            col.size = new Vector3(0.45f, 0.5f, 0.45f);

            Rigidbody rb = duckRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(duckRoot.transform, false);
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
            duckSO.FindProperty("cockpitHalfX").floatValue = 11.8f;
            duckSO.FindProperty("cockpitHalfZ").floatValue = 8.8f;
            duckSO.FindProperty("cockpitMaxY").floatValue = 16.5f;
            duckSO.FindProperty("moveSpeed").floatValue = 14.0f;
            duckSO.FindProperty("acceleration").floatValue = 70.0f;
            duckSO.FindProperty("jumpForce").floatValue = 8.2f;
            duckSO.FindProperty("stompSpeed").floatValue = 24.0f;
            duckSO.ApplyModifiedProperties();

            string prefabPath = Path.Combine(CHARACTERS_PATH, "DuckPilot.prefab").Replace("\\", "/");
            PrefabUtility.SaveAsPrefabAsset(duckRoot, prefabPath);
            Object.DestroyImmediate(duckRoot);
        }

        // =====================================================================
        // PREFABS 3: BOTONES FÍSICOS DE CABINA (ESCALA ADAPTADA A DUCK 0.4x)
        // =====================================================================
        void CreateButtonPrefab(string btnId, string label, CockpitFloorButton.ButtonType type, Material capMat, float diameter = 2.0f, float detectionRadius = 1.4f)
        {
            GameObject btnObj = new GameObject("Btn_" + btnId);
            CockpitFloorButton cfb = btnObj.AddComponent<CockpitFloorButton>();
            cfb.buttonId = btnId;
            cfb.label = label;
            cfb.type = type;
            cfb.detectionRadius = detectionRadius;

            GameObject housing = CreatePrimitive(PrimitiveType.Cylinder, "Housing", btnObj.transform, Vector3.zero, new Vector3(diameter * 1.15f, 0.18f, diameter * 1.15f), Quaternion.identity, matBtnBase);
            CreatePrimitive(PrimitiveType.Cylinder, "RimRing", housing.transform, new Vector3(0f, 0.55f, 0f), new Vector3(1.02f, 0.2f, 1.02f), Quaternion.identity, matHazard);

            GameObject cap = CreatePrimitive(PrimitiveType.Cylinder, "Cap", btnObj.transform, new Vector3(0f, 0.22f, 0f), new Vector3(diameter, 0.22f, diameter), Quaternion.identity, capMat);

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(cap.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            labelObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            labelObj.transform.localScale = Vector3.one * (0.24f / diameter);

            TextMesh tm = labelObj.AddComponent<TextMesh>();
            tm.text = label;
            tm.fontSize = 28;
            tm.characterSize = 0.28f;
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

        CreateButtonPrefab("FWD", "▲ AVANZAR", CockpitFloorButton.ButtonType.Hold, matBtnGreen, 2.0f, 1.4f);
        CreateButtonPrefab("BWD", "▼ ATRÁS", CockpitFloorButton.ButtonType.Hold, matBtnAmber, 2.0f, 1.4f);
        CreateButtonPrefab("LEFT", "◄ GIRO IZQ", CockpitFloorButton.ButtonType.Hold, matBtnCyan, 1.9f, 1.35f);
        CreateButtonPrefab("RIGHT", "GIRO DER ►", CockpitFloorButton.ButtonType.Hold, matBtnCyan, 1.9f, 1.35f);
        CreateButtonPrefab("GATLING", "🔫 GATLING\n[MANTENER / E]", CockpitFloorButton.ButtonType.Hold, matBtnAmber, 2.0f, 1.4f);
        CreateButtonPrefab("MISSILES", "🚀 MISILES\n(CULAZO E)", CockpitFloorButton.ButtonType.Stomp, matBtnCyan, 2.0f, 1.4f);
        CreateButtonPrefab("SPAWN", "🚨 ROBOTS\n[ACTIVAR / PAUSAR]", CockpitFloorButton.ButtonType.Toggle, matBtnRose, 1.9f, 1.35f);

        // Prefab de Plataforma Giroscópica de Puntería (AIM_PAD) con escala 1.2f
        GameObject aimPadObj = SetupGunnerAimPad.BuildAimPadHierarchy(null, Vector3.zero, matDarkSteel, matGunmetal, matChrome, matHazard, matBtnBase, matBtnCyan, matBtnGreen, matBtnAmber, 1.2f);
        string aimPadPrefabPath = Path.Combine(PROPS_BUTTONS_PATH, "CockpitButton_AIM_PAD.prefab").Replace("\\", "/");
        PrefabUtility.SaveAsPrefabAsset(aimPadObj, aimPadPrefabPath);
        Object.DestroyImmediate(aimPadObj);

        // =====================================================================
        // PREFABS 4: CONSOLAS TÁCTICAS SLEEK (Assets/Prefabs/Props/Consoles/)
        // =====================================================================
        {
            // Consola de Pilotaje (Planta 1: perfil bajo Y <= 0.60m)
            GameObject pConRoot = new GameObject("PilotConsole");
            CreatePrimitive(PrimitiveType.Cube, "Desk", pConRoot.transform, new Vector3(0f, 0.20f, 0f), new Vector3(3.6f, 0.40f, 0.8f), Quaternion.identity, matGunmetal);
            CreatePrimitive(PrimitiveType.Cube, "Pilot_Nav_Screen", pConRoot.transform, new Vector3(0f, 0.28f, 0.02f), new Vector3(2.0f, 0.20f, 0.05f), Quaternion.Euler(-25f, 0f, 0f), matFloor1Radar);

            string pathP = Path.Combine(PROPS_CONSOLES_PATH, "PilotConsole.prefab").Replace("\\", "/");
            PrefabUtility.SaveAsPrefabAsset(pConRoot, pathP);
            Object.DestroyImmediate(pConRoot);

            // Consola de Artillería (Planta 2: perfil bajo Y < 0.6m para visión despejada)
            GameObject gConRoot = new GameObject("GunnerConsole");
            CreatePrimitive(PrimitiveType.Cube, "Desk", gConRoot.transform, new Vector3(0f, 0.25f, 0f), new Vector3(3.8f, 0.45f, 0.7f), Quaternion.identity, matDarkSteel);
            CreatePrimitive(PrimitiveType.Cube, "Gunner_Ballistic_Screen", gConRoot.transform, new Vector3(0f, 0.55f, 0.05f), new Vector3(2.2f, 0.35f, 0.05f), Quaternion.Euler(-25f, 0f, 0f), matBtnRose);

            string pathG = Path.Combine(PROPS_CONSOLES_PATH, "GunnerConsole.prefab").Replace("\\", "/");
            PrefabUtility.SaveAsPrefabAsset(gConRoot, pathG);
            Object.DestroyImmediate(gConRoot);
        }

        // =====================================================================
        // PREFAB 5: CÚPULA PANORÁMICA COMPACTA (Assets/Prefabs/Cockpit/CockpitDome.prefab)
        // Dimensiones: 24m ancho x 18m profundidad x 14.5m altura, 3 Plantas, Escaleras 3.0m
        // =====================================================================
        {
            GameObject domeRoot = new GameObject("CockpitDome");

            void CreateStairs(string name, Transform parent, Vector3 startPos, Vector3 endPos, float width, Material stepMat, Material railMat, Material frameMat)
            {
                GameObject stairRoot = new GameObject(name);
                if (parent != null) stairRoot.transform.SetParent(parent, false);
                stairRoot.transform.localPosition = Vector3.zero;
                stairRoot.transform.localRotation = Quaternion.identity;
                stairRoot.transform.localScale = Vector3.one;

                Vector3 delta = endPos - startPos;
                Vector3 horizDelta = new Vector3(delta.x, 0f, delta.z);
                float horizDist = horizDelta.magnitude;
                float riseY = delta.y;
                float slopeLength = Mathf.Sqrt(horizDist * horizDist + riseY * riseY);
                Vector3 horizDir = horizDist > 0.001f ? horizDelta.normalized : Vector3.forward;
                float pitchAngle = Mathf.Atan2(riseY, horizDist) * Mathf.Rad2Deg;

                Quaternion rampRot = Quaternion.LookRotation(horizDir, Vector3.up) * Quaternion.Euler(-pitchAngle, 0f, 0f);

                int numSteps = 24;
                float stepH = riseY / numSteps;
                float stepD = (horizDist / numSteps) * 1.15f;

                for (int i = 0; i < numSteps; i++)
                {
                    float t = (i + 0.5f) / numSteps;
                    Vector3 stepPos = Vector3.Lerp(startPos, endPos, t);
                    stepPos.y = startPos.y + (i + 0.5f) * stepH;
                    GameObject st = CreatePrimitive(PrimitiveType.Cube, $"Step_{i}", stairRoot.transform, stepPos, new Vector3(width, stepH * 1.05f, stepD), Quaternion.identity, stepMat, true);
                    CreatePrimitive(PrimitiveType.Cube, "Hazard_Edge", st.transform, new Vector3(0f, 0.48f, 0.46f), new Vector3(0.98f, 0.12f, 0.12f), Quaternion.identity, matHazard, false);
                }

                // Aterrizajes horizontales
                Vector3 bLand = startPos - horizDir * 1.0f + new Vector3(0f, -0.1f, 0f);
                CreatePrimitive(PrimitiveType.Cube, "Stair_Bottom_Landing", stairRoot.transform, bLand, new Vector3(width, 0.28f, 2.0f), Quaternion.identity, stepMat, true);
                Vector3 tLand = endPos + horizDir * 1.0f + new Vector3(0f, -0.1f, 0f);
                CreatePrimitive(PrimitiveType.Cube, "Stair_Top_Landing", stairRoot.transform, tLand, new Vector3(width, 0.28f, 2.0f), Quaternion.identity, stepMat, true);

                // Pasamanos dobles tubulares
                float halfW = width * 0.5f;
                Vector3 sideCross = Vector3.Cross(Vector3.up, horizDir).normalized;
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 railOffset = sideCross * (side * (halfW + 0.15f));
                    Vector3 rStart = startPos + railOffset + new Vector3(0f, 1.25f, 0f);
                    Vector3 rEnd = endPos + railOffset + new Vector3(0f, 1.25f, 0f);
                    Vector3 rMid = (rStart + rEnd) * 0.5f;

                    CreatePrimitive(PrimitiveType.Cylinder, $"Handrail_{(side < 0 ? "L" : "R")}", stairRoot.transform, rMid, new Vector3(0.18f, slopeLength * 0.5f, 0.18f), rampRot * Quaternion.Euler(90f, 0f, 0f), railMat, false);

                    for (int p = 0; p <= 6; p++)
                    {
                        float t = p / 6.0f;
                        Vector3 postPos = Vector3.Lerp(rStart, rEnd, t) - new Vector3(0f, 0.6f, 0f);
                        CreatePrimitive(PrimitiveType.Cylinder, $"Post_{(side < 0 ? "L" : "R")}_{p}", stairRoot.transform, postPos, new Vector3(0.16f, 1.2f, 0.16f), Quaternion.identity, railMat, false);
                    }
                }

                // Colisionador de rampa suave impenetrable (1.4m de espesor)
                Vector3 rampMid = (startPos + endPos) * 0.5f;
                GameObject rampObj = new GameObject("Invisible_Ramp_Collider");
                rampObj.transform.SetParent(stairRoot.transform, false);
                rampObj.transform.localPosition = rampMid;
                rampObj.transform.localRotation = rampRot;
                rampObj.transform.localScale = Vector3.one;

                BoxCollider bc = rampObj.AddComponent<BoxCollider>();
                bc.size = new Vector3(width, 1.4f, slopeLength + 1.0f);
                bc.center = new Vector3(0f, -0.62f, 0f);
            }

            // -----------------------------------------------------------------
            // PLANTA 1 (Y = 0m): BÚNKER DIESELPUNK Y SALA DE CALDERAS (24m x 18m)
            // -----------------------------------------------------------------
            CreatePrimitive(PrimitiveType.Cube, "Floor_Level1", domeRoot.transform, new Vector3(0f, -0.25f, 0f), new Vector3(24.0f, 0.5f, 18.0f), Quaternion.identity, matFloor1Plate, true);

            // Muros acorazados perimetrales remachados
            CreatePrimitive(PrimitiveType.Cube, "Side_Armor_L1_L", domeRoot.transform, new Vector3(-12.0f, 2.0f, 0f), new Vector3(0.6f, 4.0f, 18.0f), Quaternion.identity, matFloor1Armor);
            CreatePrimitive(PrimitiveType.Cube, "Side_Armor_L1_R", domeRoot.transform, new Vector3(12.0f, 2.0f, 0f), new Vector3(0.6f, 4.0f, 18.0f), Quaternion.identity, matFloor1Armor);
            CreatePrimitive(PrimitiveType.Cube, "Rear_Armor_L1", domeRoot.transform, new Vector3(0f, 2.0f, -9.0f), new Vector3(24.0f, 4.0f, 0.6f), Quaternion.identity, matMilGreenDark);

            // 4 Pistones hidráulicos estructurales en las esquinas
            Vector2[] cornerPistons = new Vector2[] { new Vector2(-10.5f, -7.5f), new Vector2(10.5f, -7.5f), new Vector2(-10.5f, 7.5f), new Vector2(10.5f, 7.5f) };
            for (int i = 0; i < cornerPistons.Length; i++)
            {
                Vector2 cp = cornerPistons[i];
                CreatePrimitive(PrimitiveType.Cylinder, $"Corner_Piston_Casing_{i}", domeRoot.transform, new Vector3(cp.x, 2.0f, cp.y), new Vector3(1.2f, 4.0f, 1.2f), Quaternion.identity, matDarkSteel);
                CreatePrimitive(PrimitiveType.Cylinder, $"Corner_Piston_Rod_{i}", domeRoot.transform, new Vector3(cp.x, 2.0f, cp.y), new Vector3(0.7f, 4.1f, 0.7f), Quaternion.identity, matChrome);
            }

            // Popa: Núcleo de Reactor de Fusión Industrial (Z < -4.5m)
            GameObject reactor = CreatePrimitive(PrimitiveType.Cylinder, "Reactor_Fusion_Core", domeRoot.transform, new Vector3(0f, 1.8f, -6.5f), new Vector3(6.5f, 2.5f, 6.5f), Quaternion.identity, matDarkSteel);
            CreatePrimitive(PrimitiveType.Cylinder, "Reactor_Ring_1", reactor.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.05f, 0.18f, 1.05f), Quaternion.identity, matFloor1Reactor);
            CreatePrimitive(PrimitiveType.Cylinder, "Reactor_Ring_2", reactor.transform, new Vector3(0f, -0.5f, 0f), new Vector3(1.05f, 0.18f, 1.05f), Quaternion.identity, matFloor1Reactor);
            CreatePrimitive(PrimitiveType.Cylinder, "Reactor_Turbine_Spindle", reactor.transform, Vector3.zero, new Vector3(0.55f, 1.02f, 0.55f), Quaternion.identity, matFloor1Reactor);

            // Tuberías industriales en el suelo que van desde el reactor a los botones de navegación
            CreatePrimitive(PrimitiveType.Cylinder, "Pipe_Reactor_Center", domeRoot.transform, new Vector3(0f, 0.08f, -2.5f), new Vector3(0.35f, 5.0f, 0.35f), Quaternion.Euler(90f, 0f, 0f), matChrome);
            CreatePrimitive(PrimitiveType.Cylinder, "Pipe_Reactor_L", domeRoot.transform, new Vector3(-2.8f, 0.08f, -3.0f), new Vector3(0.28f, 4.5f, 0.28f), Quaternion.Euler(90f, 0f, 0f), matChrome);
            CreatePrimitive(PrimitiveType.Cylinder, "Pipe_Reactor_R", domeRoot.transform, new Vector3(2.8f, 0.08f, -3.0f), new Vector3(0.28f, 4.5f, 0.28f), Quaternion.Euler(90f, 0f, 0f), matChrome);

            // Frente: Estructura búnker de 4 piezas con ventana de tanque abierta en el centro (Y in [0.9f, 2.2f], X in [-8.0f, +8.0f])
            // Pieza 1 — Glacis / Faldón Blindado Inferior: de Y = 0.0f a Y = 0.9f (ancho 24.0f, inclinado 22°, nunca supera Y = 0.9f)
            CreatePrimitive(PrimitiveType.Cube, "Armor_Glacis_L1_Bottom", domeRoot.transform, new Vector3(0f, 0.40f, 8.85f), new Vector3(24.0f, 0.82f, 0.5f), Quaternion.Euler(22f, 0f, 0f), matMilGreen);

            // Pieza 2 — Glacis Superior / Visera bajo Planta 2: de Y = 2.2f a Y = 4.5f (ancho 24.0f, inclinado 25°, deja libre Y in [0.9f, 2.2f])
            CreatePrimitive(PrimitiveType.Cube, "Armor_Glacis_L1_Top", domeRoot.transform, new Vector3(0f, 3.40f, 8.40f), new Vector3(24.0f, 2.25f, 0.5f), Quaternion.Euler(25f, 0f, 0f), matMilGreen);

            // Piezas 3 & 4 — Pilares Blindados Laterales: en X = -10.0f y X = +10.0f (ancho 4.0f, alto Y in [0.9f, 2.2f]), centro libre
            CreatePrimitive(PrimitiveType.Cube, "Armor_Cheek_L", domeRoot.transform, new Vector3(-10.0f, 1.55f, 8.60f), new Vector3(4.0f, 1.30f, 0.5f), Quaternion.identity, matGunmetal);
            CreatePrimitive(PrimitiveType.Cube, "Armor_Cheek_R", domeRoot.transform, new Vector3(10.0f, 1.55f, 8.60f), new Vector3(4.0f, 1.30f, 0.5f), Quaternion.identity, matGunmetal);

            // Cristal Central (Slit_Visor_Glass): en (0, 1.55f, 8.60f), tamaño 16.0m x 1.3m x 0.1m, transparente
            CreatePrimitive(PrimitiveType.Cube, "Slit_Visor_Glass", domeRoot.transform, new Vector3(0f, 1.55f, 8.60f), new Vector3(16.0f, 1.30f, 0.10f), Quaternion.identity, matDomeGlass);

            // Tramo 1 (Planta 1 -> Planta 2): Estribor X = +7.5m, de Y = 0.0m a Y = 4.5m, ancho 2.8m
            CreateStairs("Stairs_P1_to_P2", domeRoot.transform, new Vector3(7.5f, 0.0f, -4.5f), new Vector3(7.5f, 4.5f, 3.5f), 2.8f, matDarkSteel, matHazard, matGunmetal);

            // -----------------------------------------------------------------
            // PLANTA 2 (Y = 4.5m): HANGAR DE ARTILLERÍA Y CUBIERTA PANORÁMICA (24m x 18m)
            // -----------------------------------------------------------------
            // Suelo con abertura de escalera 1 en estribor (X = 5.8m a 9.2m, Z = -5.8m a 4.8m)
            CreatePrimitive(PrimitiveType.Cube, "Floor_L2_Port", domeRoot.transform, new Vector3(-3.1f, 4.25f, 0f), new Vector3(17.8f, 0.5f, 18.0f), Quaternion.identity, matFloor2Deck, true);
            CreatePrimitive(PrimitiveType.Cube, "Floor_L2_Starboard_Outer", domeRoot.transform, new Vector3(10.6f, 4.25f, 0f), new Vector3(2.8f, 0.5f, 18.0f), Quaternion.identity, matFloor2Deck, true);
            CreatePrimitive(PrimitiveType.Cube, "Floor_L2_Starboard_Front", domeRoot.transform, new Vector3(7.5f, 4.25f, 6.9f), new Vector3(3.4f, 0.5f, 4.2f), Quaternion.identity, matFloor2Deck, true);
            CreatePrimitive(PrimitiveType.Cube, "Floor_L2_Starboard_Rear", domeRoot.transform, new Vector3(7.5f, 4.25f, -7.4f), new Vector3(3.4f, 0.5f, 3.2f), Quaternion.identity, matFloor2Deck, true);

            // Barandilla de seguridad bordeando el hueco de escalera 1 (abierta en la llegada Z > 3.5m)
            CreatePrimitive(PrimitiveType.Cube, "L2_Stair1_Safety_Rail", domeRoot.transform, new Vector3(5.75f, 5.0f, -0.8f), new Vector3(0.12f, 0.9f, 8.5f), Quaternion.identity, matHazard);

            // Gran ventanal panorámico frontal (20m de ancho, Y = 4.7m a 8.3m)
            CreatePrimitive(PrimitiveType.Cube, "Windshield_Level2_Panorame", domeRoot.transform, new Vector3(0f, 6.5f, 8.9f), new Vector3(20.0f, 3.8f, 0.14f), Quaternion.identity, matDomeGlass);
            CreatePrimitive(PrimitiveType.Cube, "Side_Glass_L2_L", domeRoot.transform, new Vector3(-11.9f, 6.5f, 0f), new Vector3(0.14f, 3.8f, 14.0f), Quaternion.identity, matDomeGlass);
            CreatePrimitive(PrimitiveType.Cube, "Side_Glass_L2_R", domeRoot.transform, new Vector3(11.9f, 6.5f, 0f), new Vector3(0.14f, 3.8f, 14.0f), Quaternion.identity, matDomeGlass);

            // Rieles de grúa de munición en el techo (Y = 8.2m) y carro polipasto
            CreatePrimitive(PrimitiveType.Cube, "Crane_Rail_L", domeRoot.transform, new Vector3(-3.5f, 8.2f, 0f), new Vector3(0.25f, 0.3f, 16.0f), Quaternion.identity, matHazard);
            CreatePrimitive(PrimitiveType.Cube, "Crane_Rail_R", domeRoot.transform, new Vector3(3.5f, 8.2f, 0f), new Vector3(0.25f, 0.3f, 16.0f), Quaternion.identity, matHazard);
            CreatePrimitive(PrimitiveType.Cube, "Crane_Trolley", domeRoot.transform, new Vector3(0f, 8.1f, 1.5f), new Vector3(7.5f, 0.3f, 1.2f), Quaternion.identity, matGunmetal);

            // Conductos de munición articulados hacia los hombros
            CreatePrimitive(PrimitiveType.Cylinder, "Ammo_Conduit_L", domeRoot.transform, new Vector3(-10.0f, 7.2f, 2.0f), new Vector3(0.6f, 3.5f, 0.6f), Quaternion.Euler(0f, 0f, 75f), matGunmetal);
            CreatePrimitive(PrimitiveType.Cylinder, "Ammo_Conduit_R", domeRoot.transform, new Vector3(10.0f, 7.2f, 2.0f), new Vector3(0.6f, 3.5f, 0.6f), Quaternion.Euler(0f, 0f, -75f), matGunmetal);

            // Baterías laterales de tanques criogénicos
            for (int t = -1; t <= 1; t += 2)
            {
                float tx = t * 10.5f;
                GameObject cTank = CreatePrimitive(PrimitiveType.Cylinder, $"Cryo_Tank_{t}", domeRoot.transform, new Vector3(tx, 6.0f, -5.5f), new Vector3(1.6f, 2.2f, 1.6f), Quaternion.identity, matFloor2Cryo);
                CreatePrimitive(PrimitiveType.Cylinder, "Frost_Ring", cTank.transform, Vector3.zero, new Vector3(1.04f, 0.25f, 1.04f), Quaternion.identity, matFloor2Gauge);
            }

            // Tramo 2 (Planta 2 -> Planta 3): Babor X = -7.5m, de Y = 4.5m a Y = 9.0m, ancho 2.8m
            CreateStairs("Stairs_P2_to_P3", domeRoot.transform, new Vector3(-7.5f, 4.5f, 3.5f), new Vector3(-7.5f, 9.0f, -4.5f), 2.8f, matDarkSteel, matHazard, matGunmetal);

            // -----------------------------------------------------------------
            // PLANTA 3 (Y = 9.0m): SANTUARIO NEURONAL DEL CAPITÁN (24m x 18m x 14.5m)
            // -----------------------------------------------------------------
            // Suelo con abertura de escalera 2 en babor (X = -9.2m a -5.8m, Z = -5.8m a 4.8m)
            CreatePrimitive(PrimitiveType.Cube, "Floor_L3_Starboard", domeRoot.transform, new Vector3(3.1f, 8.75f, 0f), new Vector3(17.8f, 0.5f, 18.0f), Quaternion.identity, matFloor3Carbon, true);
            CreatePrimitive(PrimitiveType.Cube, "Floor_L3_Port_Outer", domeRoot.transform, new Vector3(-10.6f, 8.75f, 0f), new Vector3(2.8f, 0.5f, 18.0f), Quaternion.identity, matFloor3Carbon, true);
            CreatePrimitive(PrimitiveType.Cube, "Floor_L3_Port_Front", domeRoot.transform, new Vector3(-7.5f, 8.75f, 6.9f), new Vector3(3.4f, 0.5f, 4.2f), Quaternion.identity, matFloor3Carbon, true);
            CreatePrimitive(PrimitiveType.Cube, "Floor_L3_Port_Rear", domeRoot.transform, new Vector3(-7.5f, 8.75f, -7.4f), new Vector3(3.4f, 0.5f, 3.2f), Quaternion.identity, matFloor3Carbon, true);

            // Barandilla bordeando escalera 2 en Planta 3 (abierta en la llegada Z < -3.5m)
            CreatePrimitive(PrimitiveType.Cube, "L3_Stair2_Safety_Rail", domeRoot.transform, new Vector3(-5.75f, 9.5f, 0.8f), new Vector3(0.12f, 0.9f, 8.5f), Quaternion.identity, matHazard);

            // Muros y cúpula ciega hexagonal sci-fi (sin ventanas)
            CreatePrimitive(PrimitiveType.Cube, "Hull_L3_Front", domeRoot.transform, new Vector3(0f, 11.5f, 8.95f), new Vector3(24.0f, 5.0f, 0.5f), Quaternion.identity, matFloor3Carbon);
            CreatePrimitive(PrimitiveType.Cube, "Hull_L3_Rear", domeRoot.transform, new Vector3(0f, 11.5f, -8.95f), new Vector3(24.0f, 5.0f, 0.5f), Quaternion.identity, matFloor3Carbon);
            CreatePrimitive(PrimitiveType.Cube, "Hull_L3_Side_L", domeRoot.transform, new Vector3(-11.95f, 11.5f, 0f), new Vector3(0.5f, 5.0f, 18.0f), Quaternion.identity, matFloor3Carbon);
            CreatePrimitive(PrimitiveType.Cube, "Hull_L3_Side_R", domeRoot.transform, new Vector3(11.95f, 11.5f, 0f), new Vector3(0.5f, 5.0f, 18.0f), Quaternion.identity, matFloor3Carbon);
            CreatePrimitive(PrimitiveType.Cube, "Hull_L3_Ceiling_Dome", domeRoot.transform, new Vector3(0f, 14.5f, 0f), new Vector3(24.0f, 0.5f, 18.0f), Quaternion.identity, matFloor3Carbon);

            // 7 Costillas estructurales arqueadas luminosas cian
            for (int r = -3; r <= 3; r++)
            {
                float zRib = r * 2.5f;
                CreatePrimitive(PrimitiveType.Cube, $"Arch_Rib_L_{r}", domeRoot.transform, new Vector3(-11.7f, 11.5f, zRib), new Vector3(0.3f, 4.8f, 0.25f), Quaternion.identity, matFloor3Circuit);
                CreatePrimitive(PrimitiveType.Cube, $"Arch_Rib_R_{r}", domeRoot.transform, new Vector3(11.7f, 11.5f, zRib), new Vector3(0.3f, 4.8f, 0.25f), Quaternion.identity, matFloor3Circuit);
                CreatePrimitive(PrimitiveType.Cube, $"Arch_Ceiling_Rib_{r}", domeRoot.transform, new Vector3(0f, 14.25f, zRib), new Vector3(23.2f, 0.25f, 0.25f), Quaternion.identity, matFloor3Circuit);
            }

            // Racks laterales de supercomputadoras neurales
            for (int sr = -2; sr <= 2; sr++)
            {
                float zS = sr * 2.5f;
                CreatePrimitive(PrimitiveType.Cube, $"Server_Rack_L_{sr}", domeRoot.transform, new Vector3(-11.2f, 10.5f, zS), new Vector3(0.8f, 2.8f, 2.0f), Quaternion.identity, matFloor3Server);
                CreatePrimitive(PrimitiveType.Cube, $"Server_Rack_R_{sr}", domeRoot.transform, new Vector3(11.2f, 10.5f, zS), new Vector3(0.8f, 2.8f, 2.0f), Quaternion.identity, matFloor3Server);
            }

            // Pedestal central de mando del Capitán (R = 2.2m) en (0, 9.2, +1.5)
            GameObject captainPedestal = CreatePrimitive(PrimitiveType.Cylinder, "Captain_Command_Pedestal", domeRoot.transform, new Vector3(0f, 9.2f, 1.5f), new Vector3(4.4f, 0.35f, 4.4f), Quaternion.identity, matGunmetal, true);
            captainPedestal.AddComponent<CaptainCommandPedestal>();
            CreatePrimitive(PrimitiveType.Cylinder, "Pedestal_Hazard_Ring", captainPedestal.transform, new Vector3(0f, 0.52f, 0f), new Vector3(1.02f, 0.2f, 1.02f), Quaternion.identity, matHazard);
            CreatePrimitive(PrimitiveType.Cylinder, "Captain_Holo_Disc", captainPedestal.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.85f, 0.05f, 0.85f), Quaternion.identity, matFloor3Holo);

            // Anillos holográficos flotantes sobre el pedestal
            CreatePrimitive(PrimitiveType.Cylinder, "Neural_Holo_Ring_Floating", domeRoot.transform, new Vector3(0f, 12.0f, 1.5f), new Vector3(5.5f, 0.06f, 5.5f), Quaternion.identity, matFloor3Circuit);

            // Iluminación táctica de alta fidelidad
            void AddLight(string name, Vector3 pos, Color c, float range, float intensity)
            {
                GameObject lo = new GameObject(name);
                lo.transform.SetParent(domeRoot.transform, false);
                lo.transform.localPosition = pos;
                Light l = lo.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = range;
                l.intensity = intensity;
                l.color = c;
            }
            AddLight("P1_Pilot_Light", new Vector3(0f, 3.5f, 1.0f), new Color(0.85f, 0.95f, 1.0f), 18f, 2.2f);
            AddLight("P2_Gunner_Light", new Vector3(0f, 7.8f, 2.0f), new Color(1.0f, 0.95f, 0.88f), 18f, 2.4f);
            AddLight("P3_Captain_Light", new Vector3(0f, 13.5f, 1.5f), new Color(0.85f, 0.98f, 1.0f), 22f, 2.8f);

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
        if (parent != null) go.transform.SetParent(parent, false);
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
