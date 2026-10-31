using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;

public static class AssembleMechaPrototype
{
    [MenuItem("PatoJuego/Assemble Full Prototype")]
    public static void Build()
    {
        // 1. Limpiar objetos antiguos
        string[] toClear = new string[] {
            "Mecha_Root", "Cockpit", "Duck", "OutsideCityView",
            "EnemySpawner", "ProceduralAudio", "HUD_Canvas", "HUD", "EventSystem", "CinemachineCamera_ThirdPerson", "AutoPlayTester"
        };
        foreach (string name in toClear)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null) Object.DestroyImmediate(obj);
        }

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
                m.SetColor("_EmissionColor", col * 0.8f);
                m.EnableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        Material matDuckYellow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_DuckYellow.mat");
        Material matDuckOrange = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_DuckOrange.mat");
        Material matDuckBlack = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_DuckBlack.mat");
        Material matFloor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CockpitFloor.mat");
        Material matWall = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CockpitWall.mat");
        Material matGlass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Glass.mat");

        Material matBtnGreen = CreateMat("Assets/Materials/M_BtnGreen.mat", new Color(0.06f, 0.72f, 0.51f), 0.1f, 0.7f, true);
        Material matBtnAmber = CreateMat("Assets/Materials/M_BtnAmber.mat", new Color(0.96f, 0.62f, 0.04f), 0.1f, 0.7f, true);
        Material matBtnCyan = CreateMat("Assets/Materials/M_BtnCyan.mat", new Color(0.02f, 0.71f, 0.83f), 0.1f, 0.7f, true);
        Material matBtnRose = CreateMat("Assets/Materials/M_BtnRose.mat", new Color(0.95f, 0.25f, 0.37f), 0.1f, 0.7f, true);
        Material matBtnBlue = CreateMat("Assets/Materials/M_BtnBlue.mat", new Color(0.23f, 0.51f, 0.96f), 0.1f, 0.7f, true);
        Material matBtnBase = CreateMat("Assets/Materials/M_BtnBase.mat", new Color(0.06f, 0.09f, 0.16f), 0.5f, 0.4f);
        Material matMechaRed = CreateMat("Assets/Materials/M_MechaRed.mat", new Color(0.88f, 0.11f, 0.28f), 0.4f, 0.5f);
        Material matMechaBlue = CreateMat("Assets/Materials/M_MechaBlue.mat", new Color(0.14f, 0.39f, 0.92f), 0.4f, 0.5f);
        Material matBeamCyan = CreateMat("Assets/Materials/M_LaserBeam.mat", new Color(0.22f, 0.74f, 0.97f, 0.85f), 0.1f, 0.9f, true);
        Material matAcid = CreateMat("Assets/Materials/M_ToxicAcid.mat", new Color(0.18f, 0.92f, 0.18f, 0.92f), 0.1f, 0.9f, true);
        Material matWastelandRock = CreateMat("Assets/Materials/M_WastelandRock.mat", new Color(0.36f, 0.26f, 0.22f), 0.1f, 0.2f);
        Material matAsphalt = CreateMat("Assets/Materials/M_Asphalt.mat", new Color(0.13f, 0.16f, 0.22f), 0.2f, 0.4f);
        Material matRubble = CreateMat("Assets/Materials/M_Rubble.mat", new Color(0.48f, 0.45f, 0.42f), 0.1f, 0.25f);
        Material matHazard = CreateMat("Assets/Materials/M_HazardStripe.mat", new Color(0.95f, 0.72f, 0.05f), 0.2f, 0.7f, true);

        // 2. SISTEMA DE AUDIO PROCEDURAL
        GameObject audioObj = new GameObject("ProceduralAudio");
        audioObj.AddComponent<ProceduralAudio>();

        // 3. MUNDO POST-APOCALÍPTICO (Zona urbana en ruinas, fosa ácida, puente roto y colinas escarpadas)
        GameObject outside = new GameObject("OutsideCityView");

        GameObject CreateBlock(string bName, Transform bParent, Vector3 bPos, Vector3 bScale, Quaternion bRot, Material bMat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = bName;
            go.transform.parent = bParent;
            go.transform.position = bPos;
            go.transform.rotation = bRot;
            go.transform.localScale = bScale;
            if (bMat != null) go.GetComponent<MeshRenderer>().material = bMat;
            return go;
        }

        // --- PLATAFORMA DE SALIDA (Punto de partida del Mecha) ---
        CreateBlock("StartPlateau", outside.transform, new Vector3(0f, -1f, 0f), new Vector3(36f, 2f, 28f), Quaternion.identity, matAsphalt);
        CreateBlock("StartCurb_South", outside.transform, new Vector3(0f, 0.2f, -13.8f), new Vector3(36f, 0.4f, 0.4f), Quaternion.identity, matHazard);
        CreateBlock("StartCurb_East", outside.transform, new Vector3(17.8f, 0.2f, 0f), new Vector3(0.4f, 0.4f, 28f), Quaternion.identity, matHazard);

        // --- LA GRAN FOSA DE ÁCIDO TÓXICO ---
        GameObject acidObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        acidObj.name = "ToxicAcidRiver";
        acidObj.transform.parent = outside.transform;
        acidObj.transform.position = new Vector3(0f, -11.6f, 41f);
        acidObj.transform.localScale = new Vector3(260f, 1.2f, 54f);
        if (matAcid != null) acidObj.GetComponent<MeshRenderer>().material = matAcid;
        var acidCollider = acidObj.GetComponent<BoxCollider>();
        if (acidCollider != null) acidCollider.isTrigger = true;
        acidObj.AddComponent<AcidHazard>();

        CreateBlock("AcidBasinBed", outside.transform, new Vector3(0f, -14.2f, 41f), new Vector3(260f, 4f, 54f), Quaternion.identity, matWastelandRock);
        CreateBlock("CanyonWall_South", outside.transform, new Vector3(0f, -6f, 14f), new Vector3(260f, 12f, 2f), Quaternion.identity, matWastelandRock);
        CreateBlock("CanyonWall_North", outside.transform, new Vector3(0f, -6f, 68f), new Vector3(260f, 12f, 2f), Quaternion.identity, matWastelandRock);

        // --- PUENTE QUEBRADO (Desafío físico sobre el abismo) ---
        GameObject bridgeGroup = new GameObject("Broken_Bridge");
        bridgeGroup.transform.parent = outside.transform;

        // Tramo 1: Entrada ancha con bordillos guía
        CreateBlock("Bridge_Span_1", bridgeGroup.transform, new Vector3(0f, -0.6f, 21f), new Vector3(10.5f, 1.2f, 14f), Quaternion.identity, matAsphalt);
        CreateBlock("Bridge_Span_1_RailL", bridgeGroup.transform, new Vector3(-5.1f, 0.4f, 21f), new Vector3(0.35f, 0.8f, 14f), Quaternion.identity, matHazard);
        CreateBlock("Bridge_Span_1_RailR", bridgeGroup.transform, new Vector3(5.1f, 0.4f, 21f), new Vector3(0.35f, 0.8f, 14f), Quaternion.identity, matHazard);

        // Tramo 2: Estrechamiento peligroso (8.0m de ancho para patas de 6.2m = 0.9m de margen a cada lado)
        CreateBlock("Bridge_Span_2_Narrow", bridgeGroup.transform, new Vector3(0f, -0.6f, 35f), new Vector3(8.0f, 1.2f, 14f), Quaternion.identity, matAsphalt);
        CreateBlock("Bridge_Fallen_Chunk", bridgeGroup.transform, new Vector3(4.8f, -9.5f, 35f), new Vector3(3.2f, 1.2f, 12f), Quaternion.Euler(30f, 15f, -20f), matRubble);
        CreateBlock("Bridge_Span_2_WarnL", bridgeGroup.transform, new Vector3(-3.9f, 0.3f, 35f), new Vector3(0.3f, 0.6f, 14f), Quaternion.identity, matHazard);

        // Tramo 3: Losa con leve inclinación (3.5 grados de balanceo sutil)
        CreateBlock("Bridge_Span_3_Tilted", bridgeGroup.transform, new Vector3(0f, -0.5f, 49f), new Vector3(9.5f, 1.2f, 14f), Quaternion.Euler(0f, 0f, 3.5f), matAsphalt);

        // Tramo 4: Desembarco seguro en la ciudad en ruinas
        CreateBlock("Bridge_Span_4_Exit", bridgeGroup.transform, new Vector3(0f, -0.6f, 62f), new Vector3(11.0f, 1.2f, 12f), Quaternion.identity, matAsphalt);

        CreateBlock("Bridge_Pillar_1", bridgeGroup.transform, new Vector3(0f, -6f, 21f), new Vector3(5f, 12f, 5f), Quaternion.identity, matRubble);
        CreateBlock("Bridge_Pillar_2", bridgeGroup.transform, new Vector3(0f, -6f, 49f), new Vector3(5f, 12f, 5f), Quaternion.identity, matRubble);

        // --- SECTOR NORTE: CIUDAD EN RUINAS (Z > 68) ---
        CreateBlock("CityGround", outside.transform, new Vector3(0f, -1f, 118f), new Vector3(160f, 2f, 100f), Quaternion.identity, matAsphalt);
        CreateBlock("Highway_Ramp", outside.transform, new Vector3(18f, 5.5f, 92f), new Vector3(10f, 1.2f, 42f), Quaternion.Euler(14f, 0f, 0f), matAsphalt);

        Vector3[] ruinPositions = new Vector3[] {
            new Vector3(-35f, 28f, 95f), new Vector3(-18f, 22f, 125f), new Vector3(38f, 32f, 105f),
            new Vector3(55f, 20f, 85f), new Vector3(-55f, 18f, 85f), new Vector3(25f, 35f, 135f),
            new Vector3(-40f, 25f, 145f), new Vector3(0f, 24f, 155f), new Vector3(60f, 22f, 140f)
        };
        Vector3[] ruinScales = new Vector3[] {
            new Vector3(14f, 56f, 14f), new Vector3(16f, 44f, 16f), new Vector3(18f, 64f, 18f),
            new Vector3(12f, 40f, 12f), new Vector3(15f, 36f, 15f), new Vector3(20f, 70f, 20f),
            new Vector3(16f, 50f, 16f), new Vector3(14f, 48f, 14f), new Vector3(16f, 44f, 16f)
        };
        Quaternion[] ruinRotations = new Quaternion[] {
            Quaternion.Euler(15f, 12f, 8f), Quaternion.Euler(-18f, 45f, -10f), Quaternion.Euler(12f, -25f, -14f),
            Quaternion.Euler(0f, 30f, 0f), Quaternion.Euler(22f, 0f, -12f), Quaternion.Euler(-14f, 20f, 10f),
            Quaternion.Euler(8f, -40f, 0f), Quaternion.Euler(16f, 15f, -15f), Quaternion.Euler(-10f, -10f, 18f)
        };

        for (int i = 0; i < ruinPositions.Length; i++)
        {
            CreateBlock("RuinSkyscraper_" + i, outside.transform, ruinPositions[i], ruinScales[i], ruinRotations[i], matWall);
        }

        for (int i = 0; i < 12; i++)
        {
            float rx = Random.Range(-45f, 45f);
            float rz = Random.Range(78f, 140f);
            CreateBlock("RubblePile_" + i, outside.transform, new Vector3(rx, 1.5f, rz), new Vector3(Random.Range(4f, 8f), Random.Range(2f, 4f), Random.Range(4f, 8f)), Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 180f), Random.Range(-15f, 15f)), matRubble);
        }

        // --- SECTOR SUR Y OESTE: COLINAS Y DESIERTOS ESCARPADOS (Z < 14, X < -18) ---
        CreateBlock("WastelandPlateau_Tier1", outside.transform, new Vector3(-44f, 1f, -15f), new Vector3(32f, 4f, 30f), Quaternion.identity, matWastelandRock);
        CreateBlock("SlopeRamp_Tier1", outside.transform, new Vector3(-27f, 1f, -15f), new Vector3(12f, 1.8f, 16f), Quaternion.Euler(0f, 0f, -18f), matWastelandRock);

        CreateBlock("WastelandPlateau_Tier2", outside.transform, new Vector3(-76f, 4.25f, -32f), new Vector3(28f, 8.5f, 26f), Quaternion.identity, matWastelandRock);
        CreateBlock("SteepSlope_Tier2", outside.transform, new Vector3(-59f, 3.2f, -24f), new Vector3(10f, 2f, 14f), Quaternion.Euler(22f, 0f, -25f), matWastelandRock);

        CreateBlock("HazardCliff_Tier3", outside.transform, new Vector3(-96f, 6.5f, 0f), new Vector3(30f, 15f, 36f), Quaternion.identity, matWastelandRock);
        CreateBlock("RockSpire_1", outside.transform, new Vector3(-88f, 12f, 22f), new Vector3(8f, 28f, 8f), Quaternion.Euler(8f, 15f, -6f), matWastelandRock);
        CreateBlock("RockSpire_2", outside.transform, new Vector3(-45f, 9f, -40f), new Vector3(7f, 22f, 7f), Quaternion.Euler(-12f, 30f, 10f), matWastelandRock);

        CreateBlock("FarWastelandHorizon", outside.transform, new Vector3(0f, -12.5f, 0f), new Vector3(450f, 1f, 450f), Quaternion.identity, matWastelandRock);

        // 4. EL PATO-MECHA GIGANTE (Mecha Root con Físicas)
        GameObject mechaRoot = new GameObject("Mecha_Root");
        MechaController mechaCtrl = mechaRoot.AddComponent<MechaController>();

        BoxCollider mechaCollider = mechaRoot.AddComponent<BoxCollider>();
        mechaCollider.size = new Vector3(7.2f, 5.0f, 6.0f);
        mechaCollider.center = new Vector3(0f, 4.5f, 0f);

        // Cadera / Pelvis
        GameObject pelvis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pelvis.name = "Pelvis";
        pelvis.transform.parent = mechaRoot.transform;
        pelvis.transform.localPosition = new Vector3(0f, 4.8f, 0f);
        pelvis.transform.localScale = new Vector3(7.2f, 1.6f, 5.2f);
        if (matBtnBase != null) pelvis.GetComponent<MeshRenderer>().material = matBtnBase;

        // Piernas articuladas
        GameObject CreateLeg(string name, float xPos)
        {
            GameObject legRoot = new GameObject(name);
            legRoot.transform.parent = mechaRoot.transform;
            legRoot.transform.localPosition = new Vector3(xPos, 4.5f, 0f);

            GameObject thigh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            thigh.name = "Thigh";
            thigh.transform.parent = legRoot.transform;
            thigh.transform.localPosition = new Vector3(0f, -1.2f, 0f);
            thigh.transform.localScale = new Vector3(2.1f, 2.8f, 2.4f);
            if (matWall != null) thigh.GetComponent<MeshRenderer>().material = matWall;
            Object.DestroyImmediate(thigh.GetComponent<Collider>());

            GameObject knee = GameObject.CreatePrimitive(PrimitiveType.Cube);
            knee.name = "Knee";
            knee.transform.parent = legRoot.transform;
            knee.transform.localPosition = new Vector3(0f, -2.5f, 0f);
            knee.transform.localScale = new Vector3(2.4f, 1.1f, 2.7f);
            if (matDuckYellow != null) knee.GetComponent<MeshRenderer>().material = matDuckYellow;
            Object.DestroyImmediate(knee.GetComponent<Collider>());

            GameObject shin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shin.name = "Shin";
            shin.transform.parent = legRoot.transform;
            shin.transform.localPosition = new Vector3(0f, -3.5f, 0f);
            shin.transform.localScale = new Vector3(1.9f, 2.2f, 2.2f);
            if (matBtnBase != null) shin.GetComponent<MeshRenderer>().material = matBtnBase;
            Object.DestroyImmediate(shin.GetComponent<Collider>());

            GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foot.name = "Foot";
            foot.transform.parent = legRoot.transform;
            foot.transform.localPosition = new Vector3(0f, -4.1f, 0.6f);
            foot.transform.localScale = new Vector3(2.8f, 0.9f, 4.2f);
            if (matWall != null) foot.GetComponent<MeshRenderer>().material = matWall;
            Object.DestroyImmediate(foot.GetComponent<Collider>());

            return legRoot;
        }

        GameObject legLeft = CreateLeg("Leg_Left", -3.1f);
        GameObject legRight = CreateLeg("Leg_Right", 3.1f);

        // CABINA ELEVADA (Interior + Exterior)
        const float COCKPIT_ELEV = 6.5f;
        GameObject cockpit = new GameObject("Cockpit");
        cockpit.transform.parent = mechaRoot.transform;
        cockpit.transform.localPosition = new Vector3(0f, COCKPIT_ELEV, 0f);

        // Suelo de cabina (11 x 8)
        GameObject cockpitFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cockpitFloor.name = "Floor";
        cockpitFloor.transform.parent = cockpit.transform;
        cockpitFloor.transform.localPosition = new Vector3(0f, -0.3f, 0f);
        cockpitFloor.transform.localScale = new Vector3(11f, 0.6f, 8f);
        if (matFloor != null) cockpitFloor.GetComponent<MeshRenderer>().material = matFloor;

        // Bordillos perimetrales bajos (360 despejado como en el prototipo)
        GameObject CreateRim(string name, Vector3 pos, Vector3 scale, Material m)
        {
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rim.name = name;
            rim.transform.parent = cockpit.transform;
            rim.transform.localPosition = pos;
            rim.transform.localScale = scale;
            if (m != null) rim.GetComponent<MeshRenderer>().material = m;
            return rim;
        }
        CreateRim("Rim_Left", new Vector3(-5.4f, 0.11f, 0f), new Vector3(0.25f, 0.22f, 8f), matWall);
        CreateRim("Rim_Right", new Vector3(5.4f, 0.11f, 0f), new Vector3(0.25f, 0.22f, 8f), matWall);
        CreateRim("Rim_Front", new Vector3(0f, 0.12f, 3.95f), new Vector3(11f, 0.25f, 0.3f), matDuckYellow);
        CreateRim("Rim_Back", new Vector3(0f, 0.12f, -3.95f), new Vector3(11f, 0.25f, 0.3f), matWall);

        // Techo acristalado / Canopy frontal mecha
        GameObject canopyGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
        canopyGlass.name = "Canopy_Glass";
        canopyGlass.transform.parent = cockpit.transform;
        canopyGlass.transform.localPosition = new Vector3(0f, 5.2f, 1.5f);
        canopyGlass.transform.localScale = new Vector3(11f, 0.08f, 5f);
        if (matGlass != null) canopyGlass.GetComponent<MeshRenderer>().material = matGlass;

        // Gran parabrisas frontal
        GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windshield.name = "Windshield";
        windshield.transform.parent = cockpit.transform;
        windshield.transform.localPosition = new Vector3(0f, 2.6f, 3.9f);
        windshield.transform.localScale = new Vector3(10.8f, 4.8f, 0.1f);
        if (matGlass != null) windshield.GetComponent<MeshRenderer>().material = matGlass;

        // Mirilla holográfica frontal
        GameObject crosshair = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        crosshair.name = "Crosshair_Ring";
        crosshair.transform.parent = cockpit.transform;
        crosshair.transform.localPosition = new Vector3(0f, 2.2f, 3.8f);
        crosshair.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        crosshair.transform.localScale = new Vector3(1.1f, 0.02f, 1.1f);
        if (matBtnCyan != null) crosshair.GetComponent<MeshRenderer>().material = matBtnCyan;
        Object.DestroyImmediate(crosshair.GetComponent<Collider>());

        // Luz interior de cabina
        GameObject lightObj = new GameObject("Cockpit_Light");
        lightObj.transform.parent = cockpit.transform;
        lightObj.transform.localPosition = new Vector3(0f, 4.8f, 0f);
        Light cl = lightObj.AddComponent<Light>();
        cl.type = LightType.Point;
        cl.range = 16f;
        cl.intensity = 2.2f;
        cl.color = new Color(0.85f, 0.95f, 1f);

        // BRAZOS EXTERIORES DEL MECHA
        // Brazo Izquierdo (Puño Rojo)
        GameObject armLeft = new GameObject("Arm_Left_Punch");
        armLeft.transform.parent = cockpit.transform;
        armLeft.transform.localPosition = new Vector3(-6.8f, -0.6f, 0.5f);

        GameObject forearmL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        forearmL.name = "Forearm";
        forearmL.transform.parent = armLeft.transform;
        forearmL.transform.localPosition = new Vector3(0f, 0f, 1.8f);
        forearmL.transform.localScale = new Vector3(1.9f, 1.8f, 6.5f);
        if (matMechaRed != null) forearmL.GetComponent<MeshRenderer>().material = matMechaRed;

        GameObject fistL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fistL.name = "Fist";
        fistL.transform.parent = armLeft.transform;
        fistL.transform.localPosition = new Vector3(0f, 0f, 5.5f);
        fistL.transform.localScale = new Vector3(2.6f, 2.3f, 2.4f);
        if (matDuckYellow != null) fistL.GetComponent<MeshRenderer>().material = matDuckYellow;

        GameObject punchWave = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        punchWave.name = "PunchWave";
        punchWave.transform.parent = armLeft.transform;
        punchWave.transform.localPosition = new Vector3(0f, 0f, 8.5f);
        punchWave.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        punchWave.transform.localScale = new Vector3(5f, 0.1f, 5f);
        if (matBtnRose != null) punchWave.GetComponent<MeshRenderer>().material = matBtnRose;
        Object.DestroyImmediate(punchWave.GetComponent<Collider>());
        punchWave.SetActive(false);

        // Brazo Derecho (Cañón Láser Azul)
        GameObject armRight = new GameObject("Arm_Right_Laser");
        armRight.transform.parent = cockpit.transform;
        armRight.transform.localPosition = new Vector3(6.8f, -0.6f, 0.5f);

        GameObject forearmR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        forearmR.name = "Forearm";
        forearmR.transform.parent = armRight.transform;
        forearmR.transform.localPosition = new Vector3(0f, 0f, 1.8f);
        forearmR.transform.localScale = new Vector3(1.9f, 1.8f, 6.5f);
        if (matMechaBlue != null) forearmR.GetComponent<MeshRenderer>().material = matMechaBlue;

        GameObject barrelR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrelR.name = "Barrel";
        barrelR.transform.parent = armRight.transform;
        barrelR.transform.localPosition = new Vector3(0f, 0f, 5.8f);
        barrelR.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        barrelR.transform.localScale = new Vector3(1.5f, 1.8f, 1.5f);
        if (matBtnCyan != null) barrelR.GetComponent<MeshRenderer>().material = matBtnCyan;

        GameObject laserBeam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        laserBeam.name = "LaserBeam";
        laserBeam.transform.parent = armRight.transform;
        laserBeam.transform.localPosition = new Vector3(0f, 0f, 75f);
        laserBeam.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        laserBeam.transform.localScale = new Vector3(2.5f, 70f, 2.5f);
        if (matBeamCyan != null) laserBeam.GetComponent<MeshRenderer>().material = matBeamCyan;
        Object.DestroyImmediate(laserBeam.GetComponent<Collider>());
        laserBeam.SetActive(false);

        // 5. BOTONES FÍSICOS DEL SUELO DE LA CABINA
        GameObject buttonsGroup = new GameObject("Cockpit_Buttons");
        buttonsGroup.transform.parent = cockpit.transform;
        buttonsGroup.transform.localPosition = Vector3.zero;

        void CreateFloorButton(string id, string label, Vector3 localPos, float radius, Material capMat, CockpitFloorButton.ButtonType bType)
        {
            GameObject bObj = new GameObject("Button_" + id);
            bObj.transform.parent = buttonsGroup.transform;
            bObj.transform.localPosition = localPos;

            GameObject bBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bBase.name = "Base";
            bBase.transform.parent = bObj.transform;
            bBase.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            bBase.transform.localScale = new Vector3(radius * 2.2f, 0.16f, radius * 2.2f);
            if (matBtnBase != null) bBase.GetComponent<MeshRenderer>().material = matBtnBase;
            Object.DestroyImmediate(bBase.GetComponent<Collider>());

            GameObject bCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bCap.name = "Cap";
            bCap.transform.parent = bObj.transform;
            bCap.transform.localPosition = new Vector3(0f, 0.24f, 0f);
            bCap.transform.localScale = new Vector3(radius * 2.0f, 0.24f, radius * 2.0f);
            if (capMat != null) bCap.GetComponent<MeshRenderer>().material = capMat;
            Object.DestroyImmediate(bCap.GetComponent<Collider>());

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.parent = bCap.transform;
            labelObj.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            labelObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            labelObj.transform.localScale = Vector3.one * 0.15f;
            TextMesh tm = labelObj.AddComponent<TextMesh>();
            tm.text = label;
            tm.fontSize = 32;
            tm.characterSize = 0.28f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = Color.white;

            CockpitFloorButton comp = bObj.AddComponent<CockpitFloorButton>();
            comp.buttonId = id;
            comp.label = label;
            comp.type = bType;
            comp.buttonColor = capMat != null ? capMat.color : Color.white;

            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("capTransform").objectReferenceValue = bCap.transform;
            so.ApplyModifiedProperties();
        }

        CreateFloorButton("FWD", "▲ AVANZAR", new Vector3(0f, 0f, 1.8f), 1.05f, matBtnGreen, CockpitFloorButton.ButtonType.Hold);
        CreateFloorButton("BWD", "▼ ATRÁS", new Vector3(0f, 0f, -1.4f), 1.05f, matBtnAmber, CockpitFloorButton.ButtonType.Hold);
        CreateFloorButton("LEFT", "◄ GIRO IZQ", new Vector3(-1.7f, 0f, 0.2f), 1.0f, matBtnCyan, CockpitFloorButton.ButtonType.Hold);
        CreateFloorButton("RIGHT", "GIRO DER ►", new Vector3(1.7f, 0f, 0.2f), 1.0f, matBtnCyan, CockpitFloorButton.ButtonType.Hold);
        CreateFloorButton("PUNCH", "💥 PUÑO\n(CULAZO)", new Vector3(-3.7f, 0f, 0.5f), 1.35f, matBtnRose, CockpitFloorButton.ButtonType.Stomp);
        CreateFloorButton("LASER", "⚡ LÁSER\n(CULAZO)", new Vector3(3.7f, 0f, 0.5f), 1.35f, matBtnBlue, CockpitFloorButton.ButtonType.Stomp);
        CreateFloorButton("SPAWN", "🚨 ROBOTS\n[ACTIVAR]", new Vector3(0f, 0f, -2.85f), 0.95f, matBtnRose, CockpitFloorButton.ButtonType.Toggle);

        // 6. CREAR EL PATO DENTRO DE LA CABINA
        GameObject duck = new GameObject("Duck");
        duck.transform.parent = cockpit.transform;
        duck.transform.localPosition = new Vector3(0f, 0.6f, -0.2f);
        duck.tag = "Player";

        Rigidbody rb = duck.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        CapsuleCollider col = duck.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.45f, 0f);
        col.radius = 0.45f;
        col.height = 1.0f;
        col.isTrigger = true;

        GameObject visual = new GameObject("Visual");
        visual.transform.parent = duck.transform;
        visual.transform.localPosition = Vector3.zero;

        // Cuerpo
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Duck_Body";
        body.transform.parent = visual.transform;
        body.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        body.transform.localScale = new Vector3(0.85f, 0.75f, 0.95f);
        if (matDuckYellow != null) body.GetComponent<MeshRenderer>().material = matDuckYellow;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // Cabeza
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Duck_Head";
        head.transform.parent = visual.transform;
        head.transform.localPosition = new Vector3(0f, 0.85f, 0.25f);
        head.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
        if (matDuckYellow != null) head.GetComponent<MeshRenderer>().material = matDuckYellow;
        Object.DestroyImmediate(head.GetComponent<Collider>());

        // Pico
        GameObject beak = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beak.name = "Duck_Beak";
        beak.transform.parent = visual.transform;
        beak.transform.localPosition = new Vector3(0f, 0.8f, 0.58f);
        beak.transform.localScale = new Vector3(0.32f, 0.12f, 0.35f);
        if (matDuckOrange != null) beak.GetComponent<MeshRenderer>().material = matDuckOrange;
        Object.DestroyImmediate(beak.GetComponent<Collider>());

        // Ojos
        GameObject eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeL.name = "Duck_Eye_L";
        eyeL.transform.parent = visual.transform;
        eyeL.transform.localPosition = new Vector3(-0.18f, 0.92f, 0.42f);
        eyeL.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
        if (matDuckBlack != null) eyeL.GetComponent<MeshRenderer>().material = matDuckBlack;
        Object.DestroyImmediate(eyeL.GetComponent<Collider>());

        GameObject eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeR.name = "Duck_Eye_R";
        eyeR.transform.parent = visual.transform;
        eyeR.transform.localPosition = new Vector3(0.18f, 0.92f, 0.42f);
        eyeR.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
        if (matDuckBlack != null) eyeR.GetComponent<MeshRenderer>().material = matDuckBlack;
        Object.DestroyImmediate(eyeR.GetComponent<Collider>());

        // Alas
        GameObject wingL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        wingL.name = "Duck_Wing_L";
        wingL.transform.parent = visual.transform;
        wingL.transform.localPosition = new Vector3(-0.44f, 0.42f, 0f);
        wingL.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
        wingL.transform.localScale = new Vector3(0.15f, 0.35f, 0.55f);
        if (matDuckYellow != null) wingL.GetComponent<MeshRenderer>().material = matDuckYellow;
        Object.DestroyImmediate(wingL.GetComponent<Collider>());

        GameObject wingR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        wingR.name = "Duck_Wing_R";
        wingR.transform.parent = visual.transform;
        wingR.transform.localPosition = new Vector3(0.44f, 0.42f, 0f);
        wingR.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
        wingR.transform.localScale = new Vector3(0.15f, 0.35f, 0.55f);
        if (matDuckYellow != null) wingR.GetComponent<MeshRenderer>().material = matDuckYellow;
        Object.DestroyImmediate(wingR.GetComponent<Collider>());

        // Patas
        GameObject footL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        footL.name = "Duck_Foot_L";
        footL.transform.parent = visual.transform;
        footL.transform.localPosition = new Vector3(-0.25f, 0.05f, 0.05f);
        footL.transform.localScale = new Vector3(0.22f, 0.08f, 0.32f);
        if (matDuckOrange != null) footL.GetComponent<MeshRenderer>().material = matDuckOrange;
        Object.DestroyImmediate(footL.GetComponent<Collider>());

        GameObject footR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        footR.name = "Duck_Foot_R";
        footR.transform.parent = visual.transform;
        footR.transform.localPosition = new Vector3(0.25f, 0.05f, 0.05f);
        footR.transform.localScale = new Vector3(0.22f, 0.08f, 0.32f);
        if (matDuckOrange != null) footR.GetComponent<MeshRenderer>().material = matDuckOrange;
        Object.DestroyImmediate(footR.GetComponent<Collider>());

        // Sombra proyectada
        GameObject dropShadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dropShadow.name = "DropShadow";
        dropShadow.transform.parent = duck.transform;
        dropShadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        dropShadow.transform.localScale = new Vector3(0.75f, 0.005f, 0.75f);
        if (matBtnBase != null) dropShadow.GetComponent<MeshRenderer>().material = matBtnBase;
        Object.DestroyImmediate(dropShadow.GetComponent<Collider>());

        DuckController duckCtrl = duck.AddComponent<DuckController>();
        SerializedObject duckSO = new SerializedObject(duckCtrl);
        duckSO.FindProperty("visualTransform").objectReferenceValue = visual.transform;
        duckSO.FindProperty("leftWingTransform").objectReferenceValue = wingL.transform;
        duckSO.FindProperty("rightWingTransform").objectReferenceValue = wingR.transform;
        duckSO.FindProperty("dropShadowTransform").objectReferenceValue = dropShadow.transform;
        duckSO.ApplyModifiedProperties();

        // Conectar referencias en MechaController
        SerializedObject mechaSO = new SerializedObject(mechaCtrl);
        mechaSO.FindProperty("cockpitTransform").objectReferenceValue = cockpit.transform;
        mechaSO.FindProperty("leftLegTransform").objectReferenceValue = legLeft.transform;
        mechaSO.FindProperty("rightLegTransform").objectReferenceValue = legRight.transform;
        mechaSO.FindProperty("leftArmTransform").objectReferenceValue = armLeft.transform;
        mechaSO.FindProperty("rightArmTransform").objectReferenceValue = armRight.transform;
        mechaSO.FindProperty("laserBeamObject").objectReferenceValue = laserBeam;
        mechaSO.FindProperty("punchWaveObject").objectReferenceValue = punchWave;
        mechaSO.ApplyModifiedProperties();

        // 7. GENERADOR DE ENEMIGOS
        GameObject spawnerObj = new GameObject("EnemySpawner");
        EnemySpawner spawner = spawnerObj.AddComponent<EnemySpawner>();

        // 8. CÁMARA ORBITAL Y LIBRE
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            var oldBrain = mainCam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            if (oldBrain != null) Object.DestroyImmediate(oldBrain);

            CockpitCameraController camCtrl = mainCam.gameObject.GetComponent<CockpitCameraController>();
            if (camCtrl == null) camCtrl = mainCam.gameObject.AddComponent<CockpitCameraController>();

            SerializedObject camSO = new SerializedObject(camCtrl);
            camSO.FindProperty("duckTarget").objectReferenceValue = duck.transform;
            camSO.FindProperty("mechaRoot").objectReferenceValue = mechaRoot.transform;
            camSO.ApplyModifiedProperties();

            // Alinear en modo edición la cámara detrás y por encima del pato
            Vector3 camPos = mechaRoot.transform.position + new Vector3(0f, 9.6f, -3.8f);
            Vector3 lookTarget = mechaRoot.transform.position + new Vector3(0f, 7.5f, 2.0f);
            mainCam.transform.position = camPos;
            mainCam.transform.rotation = Quaternion.LookRotation(lookTarget - camPos);
        }

        // 9. HUD COMPLETO CON UI TOOLKIT (UIDocument + UXML + USS)
        GameObject hudObj = new GameObject("HUD");
        UIDocument uiDoc = hudObj.AddComponent<UIDocument>();
        uiDoc.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/MechaPanelSettings.asset");
        uiDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/MechaHUD.uxml");
        MechaHUD hud = hudObj.AddComponent<MechaHUD>();

        var fontAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.FontAsset>("Assets/UI/MechaFontAsset.asset");
        SerializedObject hudSO = new SerializedObject(hud);
        hudSO.FindProperty("hudFontAsset").objectReferenceValue = fontAsset;
        hudSO.ApplyModifiedProperties();

        // 10. AUTO PLAY TESTER (Simulador y Verificador Automatizado)
        GameObject testerObj = new GameObject("AutoPlayTester");
        testerObj.AddComponent<AutoPlayTester>();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("¡Prototipo completo ensamblado! Mecha con cabina, 6 botones físicos, enemigos, armas (puño/láser), HUD, radar y audio procedural listos.");
    }
}
