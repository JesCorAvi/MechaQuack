using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;

public static class AssembleMechaPrototype
{
    [MenuItem("Mecha/Assemble Complete Prototype")]
    [MenuItem("PatoJuego/Assemble Full Prototype")]
    public static void Build()
    {
        // 1. Inicializar texturas procedurales PBR y construir Prefabs estándar
        MechaPrefabBuilder.BuildAllPrefabs();

        // 2. Limpiar rigurosamente todos los objetos antiguos y huérfanos de la escena
        string[] toClear = new string[] {
            "Mecha_Root", "Cockpit", "Duck", "OutsideCityView",
            "EnemySpawner", "ProceduralAudio", "HUD_Canvas", "HUD", "EventSystem", "CinemachineCamera_ThirdPerson", "AutoPlayTester"
        };
        foreach (string name in toClear)
        {
            GameObject obj;
            while ((obj = GameObject.Find(name)) != null)
            {
                Object.DestroyImmediate(obj);
            }
        }
        var rootObjs = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in rootObjs)
        {
            if (r != null && (r.name.Contains("Mecha") || r.name.Contains("Pole_Target") || r.name.Contains("Pauldron") || r.name.Contains("Roof")))
            {
                Object.DestroyImmediate(r);
            }
        }

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        Material GetMat(string matId)
        {
            return ProceduralTextureGenerator.GetMaterial(matId) ?? AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{matId}.mat");
        }

        // Materiales PBR Estilo Real Robot (Gundam / Armored Core)
        Material matArmorWhite = GetMat("M_MechaArmorWhite") ?? GetMat("M_MechaMilGreen");
        Material matCobaltBlue = GetMat("M_MechaCobaltBlue") ?? GetMat("M_MechaMilGreenDark");
        Material matInnerFrame = GetMat("M_MechaInnerFrame") ?? GetMat("M_MechaDarkSteel");
        Material matCrimsonRed = GetMat("M_MechaCrimsonRed") ?? GetMat("M_BtnRose");
        Material matChestVent = GetMat("M_MechaChestVent") ?? GetMat("M_HazardStripe");
        Material matGoldVFin = GetMat("M_MechaGoldVFin") ?? GetMat("M_MechaBrass");

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
        Color glassTint = new Color(0.92f, 0.98f, 1.0f, 0.08f); // 92% transparente
        matDomeGlass.color = glassTint;
        if (matDomeGlass.HasProperty("_BaseColor")) matDomeGlass.SetColor("_BaseColor", glassTint);
        if (matDomeGlass.HasProperty("_Smoothness")) matDomeGlass.SetFloat("_Smoothness", 0.95f);
        if (matDomeGlass.HasProperty("_Metallic")) matDomeGlass.SetFloat("_Metallic", 0.02f);
        EditorUtility.SetDirty(matDomeGlass);
        Material matMilGreen = GetMat("M_MechaMilGreen");
        Material matMilGreenDark = GetMat("M_MechaMilGreenDark");
        Material matGunmetal = GetMat("M_MechaGunmetal");
        Material matDarkSteel = GetMat("M_MechaDarkSteel");
        Material matChrome = GetMat("M_MechaChrome");
        Material matBrass = GetMat("M_MechaBrass");
        Material matCyanOptics = GetMat("M_MechaCyanOptics");
        Material matCoreOrange = GetMat("M_MechaCoreOrange");
        Material matDecalWhite = GetMat("M_MechaDecalWhite");
        Material matHazard = GetMat("M_HazardStripe");

        // Materiales por planta
        Material matFloor1Plate = GetMat("M_Floor1_DiamondPlate");
        Material matFloor1Armor = GetMat("M_Floor1_RivetedArmor");
        Material matFloor1Reactor = GetMat("M_Floor1_ReactorCore");
        Material matFloor1Radar = GetMat("M_Floor1_CRTRadar");

        Material matFloor2Deck = GetMat("M_Floor2_CarrierDeck");
        Material matFloor2Cryo = GetMat("M_Floor2_CryoFrost");
        Material matFloor2Gauge = GetMat("M_Floor2_PressureGauge");
        Material matFloor2Aim = GetMat("M_Floor2_AimPad");

        Material matFloor3Carbon = GetMat("M_Floor3_CarbonHex");
        Material matFloor3Circuit = GetMat("M_Floor3_NeuralCircuit");
        Material matFloor3Server = GetMat("M_Floor3_Supercomputer");
        Material matFloor3Holo = GetMat("M_Floor3_HoloDisc");

        // Materiales del pato
        Material matDuckYellow = GetMat("M_DuckRubberPBR");
        Material matDuckOrange = GetMat("M_DuckBeakPBR");
        Material matDuckBlack = GetMat("M_DuckTacticalPBR");

        // Materiales de botones
        Material matBtnGreen = GetMat("M_Btn_FWD") ?? GetMat("M_BtnGreen");
        Material matBtnAmber = GetMat("M_Btn_BWD") ?? GetMat("M_BtnAmber");
        Material matBtnCyan = GetMat("M_Btn_LEFT") ?? GetMat("M_BtnCyan");
        Material matBtnRose = GetMat("M_Btn_SPAWN") ?? GetMat("M_BtnRose");
        Material matBtnBase = GetMat("M_BtnBase");

        // 3. SISTEMA DE AUDIO PROCEDURAL
        GameObject audioObj = new GameObject("ProceduralAudio");
        var procAudio = audioObj.AddComponent<ProceduralAudio>();
        string[] quackGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Quacks" });
        if (quackGuids.Length > 0)
        {
            var quackClips = new System.Collections.Generic.List<AudioClip>();
            foreach (var g in quackGuids)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g));
                if (clip != null) quackClips.Add(clip);
            }
            var soAudio = new SerializedObject(procAudio);
            var propClips = soAudio.FindProperty("customQuackClips");
            if (propClips != null)
            {
                propClips.arraySize = quackClips.Count;
                for (int i = 0; i < quackClips.Count; i++) propClips.GetArrayElementAtIndex(i).objectReferenceValue = quackClips[i];
                soAudio.ApplyModifiedProperties();
            }
        }

        // 4. MEGATERRENO DE PRUEBAS DE MOVIMIENTO
        GameObject outside = new GameObject("OutsideCityView");
        var palette = MechaProvingGroundsGenerator.MaterialPalette.LoadOrCreate();
        MechaProvingGroundsGenerator.BuildWorld(outside.transform, palette);

        // --- HELPERS PARA MODELADO PROCEDURAL DE ALTO DETALLE ---
        GameObject CreateBlock(string name, Transform parent, Vector3 localPos, Vector3 scale, Quaternion localRot, Material m, bool hasCollider = false)
        {
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name;
            b.transform.SetParent(parent, false);
            b.transform.localPosition = localPos;
            b.transform.localScale = scale;
            b.transform.localRotation = localRot;
            if (m != null) b.GetComponent<MeshRenderer>().material = m;
            if (!hasCollider)
            {
                Collider c = b.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }
            return b;
        }

        GameObject CreateCylinder(string name, Transform parent, Vector3 localPos, Vector3 scale, Quaternion localRot, Material m, bool hasCollider = false)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            c.name = name;
            c.transform.SetParent(parent, false);
            c.transform.localPosition = localPos;
            c.transform.localScale = scale;
            c.transform.localRotation = localRot;
            if (m != null) c.GetComponent<MeshRenderer>().material = m;
            if (!hasCollider)
            {
                Collider col = c.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
            return c;
        }

        GameObject CreateSphere(string name, Transform parent, Vector3 localPos, Vector3 scale, Quaternion localRot, Material m, bool hasCollider = false)
        {
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = name;
            s.transform.SetParent(parent, false);
            s.transform.localPosition = localPos;
            s.transform.localScale = scale;
            s.transform.localRotation = localRot;
            if (m != null) s.GetComponent<MeshRenderer>().material = m;
            if (!hasCollider)
            {
                Collider col = s.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
            return s;
        }

        // =====================================================================
        // 5. MECHA ROOT Y CHASIS PRINCIPAL (50M ROBUSTO - ESTILO GUNDAM / REAL ROBOT)
        // =====================================================================
        GameObject mechaRoot = new GameObject("Mecha_Root");
        mechaRoot.transform.position = new Vector3(0f, 20.5f, 0f); // Pelvis a Y = 20.5m con flexión atlética IK

        Rigidbody mechaRb = mechaRoot.AddComponent<Rigidbody>();
        mechaRb.mass = 48000f;
        mechaRb.linearDamping = 0.05f;
        mechaRb.angularDamping = 8.0f;
        mechaRb.centerOfMass = new Vector3(0f, 6.2f, 0.05f);
        mechaRb.inertiaTensor = new Vector3(880000f, 500000f, 950000f);
        mechaRb.inertiaTensorRotation = Quaternion.identity;

        BoxCollider bodyCol = mechaRoot.AddComponent<BoxCollider>();
        bodyCol.center = new Vector3(0f, 6.5f, 0.5f);
        bodyCol.size = new Vector3(17.0f, 26.0f, 13.0f); // Silueta esbelta con torso en V y cintura estrecha

        MechaController mechaCtrl = mechaRoot.AddComponent<MechaController>();

        // Pelvis robusta que cubre de X = -6.5m a +6.5m y envuelve las caderas en X = +-6.0m
        GameObject pelvis = new GameObject("Pelvis");
        pelvis.transform.SetParent(mechaRoot.transform, false);
        pelvis.transform.localPosition = new Vector3(0f, -1.8f, 0f);

        CreateBlock("Pelvis_Core_Block", pelvis.transform, Vector3.zero, new Vector3(13.0f, 3.4f, 7.2f), Quaternion.identity, matInnerFrame);
        CreateBlock("Pelvis_Socket_L", pelvis.transform, new Vector3(-6.0f, 0f, 0f), new Vector3(4.8f, 3.6f, 6.8f), Quaternion.identity, matInnerFrame);
        CreateBlock("Pelvis_Socket_R", pelvis.transform, new Vector3(6.0f, 0f, 0f), new Vector3(4.8f, 3.6f, 6.8f), Quaternion.identity, matInnerFrame);

        // Cintura mecánica estrecha (8.5m) con pistones abdominales visibles en acero oscuro y cromo
        GameObject waist = new GameObject("Waist_Abdomen");
        waist.transform.SetParent(mechaRoot.transform, false);
        waist.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        CreateBlock("Waist_Core", waist.transform, Vector3.zero, new Vector3(8.5f, 3.6f, 7.0f), Quaternion.identity, matInnerFrame);
        CreateCylinder("Abdominal_Piston_FL", waist.transform, new Vector3(-2.6f, 0f, 2.9f), new Vector3(0.75f, 1.7f, 0.75f), Quaternion.Euler(6f, 0f, 0f), matChrome);
        CreateCylinder("Abdominal_Piston_FR", waist.transform, new Vector3(2.6f, 0f, 2.9f), new Vector3(0.75f, 1.7f, 0.75f), Quaternion.Euler(6f, 0f, 0f), matChrome);
        CreateCylinder("Spine_Damper_L", waist.transform, new Vector3(-3.8f, 0f, -1.5f), new Vector3(0.85f, 1.7f, 0.85f), Quaternion.identity, matInnerFrame);
        CreateCylinder("Spine_Damper_R", waist.transform, new Vector3(3.8f, 0f, -1.5f), new Vector3(0.85f, 1.7f, 0.85f), Quaternion.identity, matInnerFrame);

        // Faldón frontal compacto táctico azul cobalto liso
        CreateBlock("Pelvis_Front_Plate", pelvis.transform, new Vector3(0f, -0.6f, 3.7f), new Vector3(3.2f, 2.2f, 0.4f), Quaternion.identity, matCobaltBlue);

        // --- PIERNAS ROBUSTAS Y MUSCULOSAS (FÉMUR 10.5m x 3.8m x 4.4m, TIBIA 10.0m x 4.4m x 5.2m, BOTA 5.2m x 7.5m x 2.4m, CADERAS X = +-6.0m) ---
        ProceduralLeg CreateMilitaryLeg(string name, float xPos)
        {
            float sideSign = Mathf.Sign(xPos);

            GameObject legRoot = new GameObject(name);
            legRoot.transform.SetParent(mechaRoot.transform, false);
            legRoot.transform.localPosition = new Vector3(xPos, -1.8f, 0f); // Caderas en X = +-6.0m

            CreateSphere("Hip_Gimbal_Ball", legRoot.transform, Vector3.zero, Vector3.one * 3.8f, Quaternion.identity, matInnerFrame);
            CreateCylinder("Hip_Damper_Piston", legRoot.transform, new Vector3(-sideSign * 1.8f, 0.4f, 0f), new Vector3(0.85f, 2.2f, 0.85f), Quaternion.Euler(0f, 0f, 90f), matChrome);

            // 1. Muslo Robusto (Fémur: 10.5m largo x 3.8m ancho x 4.4m fondo)
            GameObject thigh = new GameObject("Thigh");
            thigh.transform.SetParent(legRoot.transform, false);
            thigh.transform.localPosition = Vector3.zero;

            CreateBlock("Thigh_Bone_Core", thigh.transform, new Vector3(0f, -5.25f, 0f), new Vector3(3.0f, 10.5f, 3.6f), Quaternion.identity, matInnerFrame);
            CreateBlock("Thigh_Armor_White", thigh.transform, new Vector3(0f, -5.2f, 0.2f), new Vector3(3.8f, 9.8f, 4.4f), Quaternion.identity, matArmorWhite);
            CreateCylinder("Piston_Front_L", thigh.transform, new Vector3(-1.2f, -5.2f, 2.4f), new Vector3(0.65f, 5.5f, 0.65f), Quaternion.identity, matChrome);
            CreateCylinder("Piston_Front_R", thigh.transform, new Vector3(1.2f, -5.2f, 2.4f), new Vector3(0.65f, 5.5f, 0.65f), Quaternion.identity, matChrome);
            CreateBlock("Thigh_Side_Armor", thigh.transform, new Vector3(sideSign * 1.95f, -5.2f, 0.2f), new Vector3(0.4f, 8.5f, 3.8f), Quaternion.identity, matCobaltBlue);

            // 2. Rodilla (Knee Joint) a -10.5m con articulación cilíndrica y Rodillera Robusta (Knee Guard: 4.6m x 1.5m x 3.0m)
            GameObject kneeJoint = new GameObject("Joint_Knee");
            kneeJoint.transform.SetParent(legRoot.transform, false);
            kneeJoint.transform.localPosition = new Vector3(0f, -10.5f, 0f);

            CreateCylinder("Knee_Joint_Cylinder", kneeJoint.transform, Vector3.zero, new Vector3(3.8f, 3.2f, 3.8f), Quaternion.Euler(0f, 0f, 90f), matInnerFrame);
            CreateBlock("Knee_Guard", kneeJoint.transform, new Vector3(0f, 0.6f, 2.0f), new Vector3(4.6f, 1.5f, 3.0f), Quaternion.Euler(-20f, 0f, 0f), matCobaltBlue);
            CreateBlock("Knee_Guard_Trim", kneeJoint.transform, new Vector3(0f, 0.6f, 3.4f), new Vector3(4.4f, 0.4f, 0.3f), Quaternion.Euler(-20f, 0f, 0f), matGoldVFin);

            // 3. Espinilla Musculosa (Tibia: 10.0m largo x 4.4m ancho x 5.2m fondo, más ancha que el muslo)
            GameObject shin = new GameObject("Shin");
            shin.transform.SetParent(kneeJoint.transform, false);
            shin.transform.localPosition = Vector3.zero;

            CreateBlock("Shin_Bone_Core", shin.transform, new Vector3(0f, -5.0f, 0f), new Vector3(3.6f, 10.0f, 4.2f), Quaternion.identity, matInnerFrame);
            CreateBlock("Shin_Armor_White", shin.transform, new Vector3(0f, -5.0f, 0.2f), new Vector3(4.4f, 9.5f, 5.2f), Quaternion.identity, matArmorWhite);
            CreateBlock("Shin_Front_Plating", shin.transform, new Vector3(0f, -4.5f, 2.7f), new Vector3(3.6f, 7.5f, 1.2f), Quaternion.identity, matCobaltBlue);
            CreateBlock("Shin_Thruster_Cowling", shin.transform, new Vector3(sideSign * 2.3f, -4.5f, -0.6f), new Vector3(1.2f, 4.5f, 2.8f), Quaternion.identity, matCobaltBlue);
            CreateCylinder("Shin_Thruster_Nozzle", shin.transform, new Vector3(sideSign * 2.3f, -6.8f, -0.6f), new Vector3(0.9f, 0.8f, 0.9f), Quaternion.identity, matChrome);

            // 4. Tobillo (Ankle Joint) a -10.0m respecto a la rodilla
            GameObject ankleJoint = new GameObject("Joint_Ankle");
            ankleJoint.transform.SetParent(kneeJoint.transform, false);
            ankleJoint.transform.localPosition = new Vector3(0f, -10.0f, 0f);

            CreateSphere("Ankle_Gimbal_Ball", ankleJoint.transform, Vector3.zero, Vector3.one * 2.8f, Quaternion.identity, matInnerFrame);
            CreateBlock("Ankle_Guard_L", ankleJoint.transform, new Vector3(-2.6f, 0.6f, 0f), new Vector3(0.5f, 2.4f, 3.2f), Quaternion.Euler(0f, 0f, 12f), matArmorWhite);
            CreateBlock("Ankle_Guard_R", ankleJoint.transform, new Vector3(2.6f, 0.6f, 0f), new Vector3(0.5f, 2.4f, 3.2f), Quaternion.Euler(0f, 0f, -12f), matArmorWhite);

            // 5. Bota de Combate Maciza (Hazard Stomper: 5.2m ancho x 7.5m largo x 2.4m alto)
            GameObject foot = new GameObject("Foot");
            foot.transform.SetParent(ankleJoint.transform, false);
            foot.transform.localPosition = Vector3.zero;

            // Suela base maciza
            CreateBlock("Foot_Sole_Base", foot.transform, new Vector3(0f, 0.0f, 0.8f), new Vector3(5.2f, 2.4f, 7.5f), Quaternion.identity, matCobaltBlue);
            // Piso de contacto antideslizante (oruga dark steel)
            CreateBlock("Foot_Tread", foot.transform, new Vector3(0f, -1.1f, 0.8f), new Vector3(5.3f, 0.2f, 7.6f), Quaternion.identity, matDarkSteel);
            // Puntera biselada
            CreateBlock("Toe_Beveled", foot.transform, new Vector3(0f, -0.4f, 4.5f), new Vector3(5.0f, 1.4f, 1.8f), Quaternion.Euler(-25f, 0f, 0f), matCobaltBlue);
            // Talón trasero
            CreateBlock("Heel_Block", foot.transform, new Vector3(0f, -0.2f, -2.6f), new Vector3(5.0f, 2.0f, 1.8f), Quaternion.identity, matDarkSteel);
            // Empeine blindado elevado
            CreateBlock("Instep_Elevated", foot.transform, new Vector3(0f, 1.1f, 1.2f), new Vector3(4.2f, 1.2f, 3.8f), Quaternion.Euler(-15f, 0f, 0f), matArmorWhite);
            // Franjas hazard
            CreateBlock("Hazard_Stripe_L", foot.transform, new Vector3(-2.62f, 0.0f, 0.8f), new Vector3(0.08f, 0.6f, 6.0f), Quaternion.identity, matHazard);
            CreateBlock("Hazard_Stripe_R", foot.transform, new Vector3(2.62f, 0.0f, 0.8f), new Vector3(0.08f, 0.6f, 6.0f), Quaternion.identity, matHazard);
            CreateBlock("Hazard_Stripe_Toe", foot.transform, new Vector3(0f, 0.0f, 4.56f), new Vector3(4.0f, 0.5f, 0.08f), Quaternion.identity, matHazard);

            // Pole Target parented to mechaRoot para control de orientación de rodilla
            GameObject poleTarget = new GameObject($"Pole_Target_{(xPos < 0 ? "L" : "R")}");
            poleTarget.transform.SetParent(mechaRoot.transform, false);
            poleTarget.transform.localPosition = new Vector3(xPos, -5.0f, 12.0f);

            ProceduralLeg pLeg = legRoot.AddComponent<ProceduralLeg>();
            SerializedObject pLegSO = new SerializedObject(pLeg);
            pLegSO.FindProperty("ikSolver.hip").objectReferenceValue = legRoot.transform;
            pLegSO.FindProperty("ikSolver.knee").objectReferenceValue = kneeJoint.transform;
            pLegSO.FindProperty("ikSolver.ankle").objectReferenceValue = ankleJoint.transform;
            pLegSO.FindProperty("ikSolver.poleTarget").objectReferenceValue = poleTarget.transform;
            pLegSO.FindProperty("bodyRoot").objectReferenceValue = mechaRoot.transform;
            pLegSO.FindProperty("bodyRigidbody").objectReferenceValue = mechaRb;
            pLegSO.FindProperty("localRestOffset").vector3Value = new Vector3(xPos, 0f, 0f);

            pLegSO.FindProperty("stepDistance").floatValue = 6.0f;
            pLegSO.FindProperty("stepHeight").floatValue = 2.2f;
            pLegSO.FindProperty("maxClimbableStepHeight").floatValue = 5.0f;
            pLegSO.FindProperty("stepAnticipationDistance").floatValue = 6.0f;
            pLegSO.FindProperty("footToeForwardOffset").floatValue = 4.2f;
            pLegSO.FindProperty("footHalfWidth").floatValue = 2.6f;
            pLegSO.FindProperty("ankleToSoleOffset").floatValue = 1.2f;
            pLegSO.FindProperty("footRadius").floatValue = 1.6f;
            pLegSO.FindProperty("femurLength").floatValue = 10.5f;
            pLegSO.FindProperty("tibiaLength").floatValue = 10.0f;
            pLegSO.ApplyModifiedProperties();

            pLeg.SetLocalRestOffset(new Vector3(xPos, 0f, 0f));
            pLeg.FemurLength = 10.5f;
            pLeg.TibiaLength = 10.0f;
            pLeg.IKSolver.UpperLength = 10.5f;
            pLeg.IKSolver.LowerLength = 10.0f;
            pLeg.IKSolver.Initialize();

            // Solución inicial para colocar las suelas apoyadas perfectamente sobre el suelo (Y = 0m) en Editor
            Vector3 footTarget = new Vector3(xPos, 1.2f, 0.4f);
            pLeg.IKSolver.Solve(footTarget, Quaternion.identity);

            return pLeg;
        }

        ProceduralLeg legLeft = CreateMilitaryLeg("Leg_Left", -6.0f);
        ProceduralLeg legRight = CreateMilitaryLeg("Leg_Right", 6.0f);

        MechLocomotionController locoCtrl = mechaRoot.AddComponent<MechLocomotionController>();
        SerializedObject locoSO = new SerializedObject(locoCtrl);
        locoSO.FindProperty("leftLeg").objectReferenceValue = legLeft;
        locoSO.FindProperty("rightLeg").objectReferenceValue = legRight;
        locoSO.FindProperty("mechaRigidbody").objectReferenceValue = mechaRb;
        locoSO.ApplyModifiedProperties();

        MechBodyStabilizer stabCtrl = mechaRoot.AddComponent<MechBodyStabilizer>();
        SerializedObject stabSO = new SerializedObject(stabCtrl);
        stabSO.FindProperty("locomotionController").objectReferenceValue = locoCtrl;
        stabSO.FindProperty("mechaRigidbody").objectReferenceValue = mechaRb;
        stabSO.FindProperty("chassisTransform").objectReferenceValue = pelvis.transform;
        stabSO.FindProperty("baseRideHeight").floatValue = 20.5f;
        stabSO.ApplyModifiedProperties();

        FootstepFX footFX = mechaRoot.AddComponent<FootstepFX>();
        SerializedObject fxSO = new SerializedObject(footFX);
        fxSO.FindProperty("leftLeg").objectReferenceValue = legLeft;
        fxSO.FindProperty("rightLeg").objectReferenceValue = legRight;
        fxSO.ApplyModifiedProperties();

        // =====================================================================
        // 6. CABINA MULTINIVEL INTERIOR (TARDIS Desacoplamiento Interior 0.48x)
        // Ocupa ~11.5m x 8.6m en el espacio del mundo, encajando en el torso en V sin sobresalir
        // =====================================================================
        GameObject cockpit = new GameObject("Cockpit");
        cockpit.transform.SetParent(mechaRoot.transform, false);
        cockpit.transform.localPosition = new Vector3(0f, 4.0f, 0.0f);
        cockpit.transform.localRotation = Quaternion.identity;
        cockpit.transform.localScale = Vector3.one * 0.48f;

        // Escaleras Industriales con colisionador suave impenetrable
        void CreateIndustrialStairs(string name, Transform parent, Vector3 startPos, Vector3 endPos, float width, Material stepMat, Material hazardMat, Material railMat)
        {
            GameObject stairRoot = new GameObject(name);
            stairRoot.transform.SetParent(parent, false);
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
                GameObject st = CreateBlock($"Step_{i}", stairRoot.transform, stepPos, new Vector3(width, stepH * 1.05f, stepD), Quaternion.identity, stepMat, true);
                CreateBlock("Hazard_Edge", st.transform, new Vector3(0f, 0.48f, 0.46f), new Vector3(0.98f, 0.12f, 0.12f), Quaternion.identity, hazardMat, false);
            }

            Vector3 bLand = startPos - horizDir * 1.0f + new Vector3(0f, -0.1f, 0f);
            CreateBlock("Stair_Bottom_Landing", stairRoot.transform, bLand, new Vector3(width, 0.28f, 2.0f), Quaternion.identity, stepMat, true);
            Vector3 tLand = endPos + horizDir * 1.0f + new Vector3(0f, -0.1f, 0f);
            CreateBlock("Stair_Top_Landing", stairRoot.transform, tLand, new Vector3(width, 0.28f, 2.0f), Quaternion.identity, stepMat, true);

            float halfW = width * 0.5f;
            Vector3 sideCross = Vector3.Cross(Vector3.up, horizDir).normalized;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 railOffset = sideCross * (side * (halfW + 0.15f));
                Vector3 rStart = startPos + railOffset + new Vector3(0f, 1.25f, 0f);
                Vector3 rEnd = endPos + railOffset + new Vector3(0f, 1.25f, 0f);
                Vector3 rMid = (rStart + rEnd) * 0.5f;

                CreateCylinder($"Handrail_{(side < 0 ? "L" : "R")}", stairRoot.transform, rMid, new Vector3(0.18f, slopeLength * 0.5f, 0.18f), rampRot * Quaternion.Euler(90f, 0f, 0f), railMat, false);

                for (int p = 0; p <= 6; p++)
                {
                    float t = p / 6.0f;
                    Vector3 postPos = Vector3.Lerp(rStart, rEnd, t) - new Vector3(0f, 0.6f, 0f);
                    CreateCylinder($"Post_{(side < 0 ? "L" : "R")}_{p}", stairRoot.transform, postPos, new Vector3(0.16f, 1.2f, 0.16f), Quaternion.identity, railMat, false);
                }
            }

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

        // =====================================================================
        // PLANTA 1: CONDUCTOR / NAVEGACIÓN (Y = 0.0m) - BÚNKER DIESELPUNK
        // =====================================================================
        CreateBlock("Floor_Level1", cockpit.transform, new Vector3(0f, -0.25f, 0f), new Vector3(24.0f, 0.5f, 18.0f), Quaternion.identity, matFloor1Plate, true);

        // Muros perimetrales blindados remachados
        CreateBlock("Side_Armor_L1_L", cockpit.transform, new Vector3(-12.0f, 2.0f, 0f), new Vector3(0.6f, 4.0f, 18.0f), Quaternion.identity, matFloor1Armor);
        CreateBlock("Side_Armor_L1_R", cockpit.transform, new Vector3(12.0f, 2.0f, 0f), new Vector3(0.6f, 4.0f, 18.0f), Quaternion.identity, matFloor1Armor);
        CreateBlock("Rear_Armor_L1", cockpit.transform, new Vector3(0f, 2.0f, -9.0f), new Vector3(24.0f, 4.0f, 0.6f), Quaternion.identity, matMilGreenDark);

        // 4 Pistones hidráulicos en esquinas
        Vector2[] cornerP1 = new Vector2[] { new Vector2(-10.5f, -7.5f), new Vector2(10.5f, -7.5f), new Vector2(-10.5f, 7.5f), new Vector2(10.5f, 7.5f) };
        for (int i = 0; i < cornerP1.Length; i++)
        {
            Vector2 cp = cornerP1[i];
            CreateCylinder($"Corner_Piston_Casing_{i}", cockpit.transform, new Vector3(cp.x, 2.0f, cp.y), new Vector3(1.2f, 4.0f, 1.2f), Quaternion.identity, matDarkSteel);
            CreateCylinder($"Corner_Piston_Rod_{i}", cockpit.transform, new Vector3(cp.x, 2.0f, cp.y), new Vector3(0.7f, 4.1f, 0.7f), Quaternion.identity, matChrome);
        }

        // Popa: Núcleo de Reactor de Fusión Industrial (Z < -4.5m)
        GameObject reactorObj = CreateCylinder("Reactor_Fusion_Core", cockpit.transform, new Vector3(0f, 1.8f, -6.5f), new Vector3(6.5f, 2.5f, 6.5f), Quaternion.identity, matDarkSteel);
        CreateCylinder("Reactor_Ring_1", reactorObj.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.05f, 0.18f, 1.05f), Quaternion.identity, matFloor1Reactor);
        CreateCylinder("Reactor_Ring_2", reactorObj.transform, new Vector3(0f, -0.5f, 0f), new Vector3(1.05f, 0.18f, 1.05f), Quaternion.identity, matFloor1Reactor);
        CreateCylinder("Reactor_Turbine_Spindle", reactorObj.transform, Vector3.zero, new Vector3(0.55f, 1.02f, 0.55f), Quaternion.identity, matFloor1Reactor);

        // Tuberías en el suelo hacia los controles
        CreateCylinder("Pipe_Reactor_Center", cockpit.transform, new Vector3(0f, 0.08f, -2.5f), new Vector3(0.35f, 5.0f, 0.35f), Quaternion.Euler(90f, 0f, 0f), matChrome);
        CreateCylinder("Pipe_Reactor_L", cockpit.transform, new Vector3(-2.8f, 0.08f, -3.0f), new Vector3(0.28f, 4.5f, 0.28f), Quaternion.Euler(90f, 0f, 0f), matChrome);
        CreateCylinder("Pipe_Reactor_R", cockpit.transform, new Vector3(2.8f, 0.08f, -3.0f), new Vector3(0.28f, 4.5f, 0.28f), Quaternion.Euler(90f, 0f, 0f), matChrome);

        // =====================================================================
        // FACHADA FRONTAL DE PLANTA 1: ESTRUCTURA BÚNKER DE 4 PIEZAS CON VENTANA DE TANQUE
        // Hueco real abierto en el centro: Y in [0.9f, 2.2f], X in [-8.0f, +8.0f]
        // =====================================================================
        // Pieza 1 — Glacis / Faldón Blindado Inferior: de Y = 0.0f a Y = 0.9f (ancho 24.0f, inclinado 22°, nunca supera Y = 0.9f)
        CreateBlock("Armor_Glacis_L1_Bottom", cockpit.transform, new Vector3(0f, 0.40f, 8.85f), new Vector3(24.0f, 0.82f, 0.5f), Quaternion.Euler(22f, 0f, 0f), matMilGreen);

        // Pieza 2 — Glacis Superior / Visera bajo Planta 2: de Y = 2.2f a Y = 4.5f (ancho 24.0f, inclinado 25°, deja libre Y in [0.9f, 2.2f])
        CreateBlock("Armor_Glacis_L1_Top", cockpit.transform, new Vector3(0f, 3.40f, 8.40f), new Vector3(24.0f, 2.25f, 0.5f), Quaternion.Euler(25f, 0f, 0f), matMilGreen);

        // Piezas 3 & 4 — Pilares Blindados Laterales: en X = -10.0f y X = +10.0f (ancho 4.0f, alto Y in [0.9f, 2.2f]), centro libre
        CreateBlock("Armor_Cheek_L", cockpit.transform, new Vector3(-10.0f, 1.55f, 8.60f), new Vector3(4.0f, 1.30f, 0.5f), Quaternion.identity, matGunmetal);
        CreateBlock("Armor_Cheek_R", cockpit.transform, new Vector3(10.0f, 1.55f, 8.60f), new Vector3(4.0f, 1.30f, 0.5f), Quaternion.identity, matGunmetal);

        // Cristal Central (Slit_Visor_Glass): en (0, 1.55f, 8.60f), tamaño 16.0m x 1.3m x 0.1m, transparente
        CreateBlock("Slit_Visor_Glass", cockpit.transform, new Vector3(0f, 1.55f, 8.60f), new Vector3(16.0f, 1.30f, 0.10f), Quaternion.identity, matDomeGlass);

        // Consola de navegación de pilotaje de perfil bajo (borde superior Y <= 0.60m, despejando Y in [1.0f, 2.2f])
        GameObject pConsole = CreateBlock("Pilot_Console", cockpit.transform, new Vector3(0f, 0.20f, 7.2f), new Vector3(3.6f, 0.40f, 0.8f), Quaternion.identity, matGunmetal);
        CreateBlock("Pilot_Nav_Screen", pConsole.transform, new Vector3(0f, 0.28f, 0.02f), new Vector3(2.0f, 0.20f, 0.05f), Quaternion.Euler(-25f, 0f, 0f), matFloor1Radar);

        // Tramo 1 (Planta 1 -> Planta 2): Estribor X = +7.5m, de Y = 0.0m a Y = 4.5m, ancho 2.8m
        CreateIndustrialStairs("Stairs_Level1_to_Level2", cockpit.transform, new Vector3(7.5f, 0.0f, -4.5f), new Vector3(7.5f, 4.5f, 3.5f), 2.8f, matDarkSteel, matHazard, matGunmetal);

        // =====================================================================
        // PLANTA 2: ARTILLERO / ARSENAL (Y = 4.5m) - HANGAR Y CUBIERTA PANORÁMICA
        // =====================================================================
        // Suelo modular de Planta 2 (con hueco para escalera 1 en estribor X = 5.8m a 9.2m, Z = -5.8m a 4.8m)
        CreateBlock("Floor_L2_Port", cockpit.transform, new Vector3(-3.1f, 4.25f, 0f), new Vector3(17.8f, 0.5f, 18.0f), Quaternion.identity, matFloor2Deck, true);
        CreateBlock("Floor_L2_Starboard_Outer", cockpit.transform, new Vector3(10.6f, 4.25f, 0f), new Vector3(2.8f, 0.5f, 18.0f), Quaternion.identity, matFloor2Deck, true);
        CreateBlock("Floor_L2_Starboard_Front", cockpit.transform, new Vector3(7.5f, 4.25f, 6.9f), new Vector3(3.4f, 0.5f, 4.2f), Quaternion.identity, matFloor2Deck, true);
        CreateBlock("Floor_L2_Starboard_Rear", cockpit.transform, new Vector3(7.5f, 4.25f, -7.4f), new Vector3(3.4f, 0.5f, 3.2f), Quaternion.identity, matFloor2Deck, true);

        // Barandilla hueco escalera 1 (abierta en la llegada Z > 3.5m)
        CreateBlock("L2_Stair1_Safety_Rail", cockpit.transform, new Vector3(5.75f, 5.0f, -0.8f), new Vector3(0.12f, 0.9f, 8.5f), Quaternion.identity, matHazard);

        // Gran ventanal panorámico frontal (20m de ancho, Y = 4.7m a 8.3m)
        CreateBlock("Windshield_Level2_Panorame", cockpit.transform, new Vector3(0f, 6.5f, 8.9f), new Vector3(20.0f, 3.8f, 0.14f), Quaternion.identity, matDomeGlass);
        CreateBlock("Side_Glass_L2_L", cockpit.transform, new Vector3(-11.9f, 6.5f, 0f), new Vector3(0.14f, 3.8f, 14.0f), Quaternion.identity, matDomeGlass);
        CreateBlock("Side_Glass_L2_R", cockpit.transform, new Vector3(11.9f, 6.5f, 0f), new Vector3(0.14f, 3.8f, 14.0f), Quaternion.identity, matDomeGlass);

        // Rieles de grúa de munición en techo y carro
        CreateBlock("Crane_Rail_L", cockpit.transform, new Vector3(-3.5f, 8.2f, 0f), new Vector3(0.25f, 0.3f, 16.0f), Quaternion.identity, matHazard);
        CreateBlock("Crane_Rail_R", cockpit.transform, new Vector3(3.5f, 8.2f, 0f), new Vector3(0.25f, 0.3f, 16.0f), Quaternion.identity, matHazard);
        CreateBlock("Crane_Trolley", cockpit.transform, new Vector3(0f, 8.1f, 1.5f), new Vector3(7.5f, 0.3f, 1.2f), Quaternion.identity, matGunmetal);

        // Conductos de munición articulados hacia los hombros
        CreateCylinder("Ammo_Conduit_L", cockpit.transform, new Vector3(-10.0f, 7.2f, 2.0f), new Vector3(0.6f, 3.5f, 0.6f), Quaternion.Euler(0f, 0f, 75f), matGunmetal);
        CreateCylinder("Ammo_Conduit_R", cockpit.transform, new Vector3(10.0f, 7.2f, 2.0f), new Vector3(0.6f, 3.5f, 0.6f), Quaternion.Euler(0f, 0f, -75f), matGunmetal);

        // Baterías laterales de tanques criogénicos
        for (int t = -1; t <= 1; t += 2)
        {
            float tx = t * 10.5f;
            GameObject cTank = CreateCylinder($"Cryo_Tank_{t}", cockpit.transform, new Vector3(tx, 6.0f, -5.5f), new Vector3(1.6f, 2.2f, 1.6f), Quaternion.identity, matFloor2Cryo);
            CreateCylinder("Frost_Ring", cTank.transform, Vector3.zero, new Vector3(1.04f, 0.25f, 1.04f), Quaternion.identity, matFloor2Gauge);
        }

        // HUD Holográfico de Puntería elevado a Y = 6.8m
        GameObject windshieldHudObj = new GameObject("Windshield_HUD");
        windshieldHudObj.transform.SetParent(cockpit.transform, false);
        windshieldHudObj.transform.localPosition = new Vector3(0f, 6.8f, 8.8f);
        windshieldHudObj.AddComponent<WindshieldHUDCrosshair>();

        // Consola de artillería de perfil bajo (Y < 0.6m respecto al suelo de P2)
        GameObject gConsole = CreateBlock("Gunner_Console", cockpit.transform, new Vector3(0f, 4.75f, 7.2f), new Vector3(3.8f, 0.45f, 0.7f), Quaternion.identity, matDarkSteel);
        CreateBlock("Gunner_Ballistic_Screen", gConsole.transform, new Vector3(0f, 0.3f, 0.05f), new Vector3(2.2f, 0.35f, 0.05f), Quaternion.Euler(-25f, 0f, 0f), matBtnRose);

        // Tramo 2 (Planta 2 -> Planta 3): Babor X = -7.5m, de Y = 4.5m a Y = 9.0m, ancho 2.8m
        CreateIndustrialStairs("Stairs_Level2_to_Level3", cockpit.transform, new Vector3(-7.5f, 4.5f, 3.5f), new Vector3(-7.5f, 9.0f, -4.5f), 2.8f, matDarkSteel, matHazard, matGunmetal);

        // =====================================================================
        // PLANTA 3: CABEZA DEL MECHA / SANTUARIO DEL CAPITÁN (Y = 9.0m)
        // =====================================================================
        // Suelo modular de Planta 3 (con hueco para escalera 2 en babor X = -9.2m a -5.8m, Z = -5.8m a 4.8m)
        CreateBlock("Floor_L3_Starboard", cockpit.transform, new Vector3(3.1f, 8.75f, 0f), new Vector3(17.8f, 0.5f, 18.0f), Quaternion.identity, matFloor3Carbon, true);
        CreateBlock("Floor_L3_Port_Outer", cockpit.transform, new Vector3(-10.6f, 8.75f, 0f), new Vector3(2.8f, 0.5f, 18.0f), Quaternion.identity, matFloor3Carbon, true);
        CreateBlock("Floor_L3_Port_Front", cockpit.transform, new Vector3(-7.5f, 8.75f, 6.9f), new Vector3(3.4f, 0.5f, 4.2f), Quaternion.identity, matFloor3Carbon, true);
        CreateBlock("Floor_L3_Port_Rear", cockpit.transform, new Vector3(-7.5f, 8.75f, -7.4f), new Vector3(3.4f, 0.5f, 3.2f), Quaternion.identity, matFloor3Carbon, true);

        // Barandilla hueco escalera 2 en Planta 3 (abierta en la llegada Z < -3.5m)
        CreateBlock("L3_Stair2_Safety_Rail", cockpit.transform, new Vector3(-5.75f, 9.5f, 0.8f), new Vector3(0.12f, 0.9f, 8.5f), Quaternion.identity, matHazard);

        // Muros y cúpula ciega hexagonal sci-fi (sin ventanas)
        CreateBlock("Hull_L3_Front", cockpit.transform, new Vector3(0f, 11.5f, 8.95f), new Vector3(24.0f, 5.0f, 0.5f), Quaternion.identity, matFloor3Carbon);
        CreateBlock("Hull_L3_Rear", cockpit.transform, new Vector3(0f, 11.5f, -8.95f), new Vector3(24.0f, 5.0f, 0.5f), Quaternion.identity, matFloor3Carbon);
        CreateBlock("Hull_L3_Side_L", cockpit.transform, new Vector3(-11.95f, 11.5f, 0f), new Vector3(0.5f, 5.0f, 18.0f), Quaternion.identity, matFloor3Carbon);
        CreateBlock("Hull_L3_Side_R", cockpit.transform, new Vector3(11.95f, 11.5f, 0f), new Vector3(0.5f, 5.0f, 18.0f), Quaternion.identity, matFloor3Carbon);
        CreateBlock("Hull_L3_Ceiling_Dome", cockpit.transform, new Vector3(0f, 14.5f, 0f), new Vector3(24.0f, 0.5f, 18.0f), Quaternion.identity, matFloor3Carbon);

        // 7 Costillas estructurales arqueadas luminosas cian
        for (int r = -3; r <= 3; r++)
        {
            float zRib = r * 2.5f;
            CreateBlock($"Arch_Rib_L_{r}", cockpit.transform, new Vector3(-11.7f, 11.5f, zRib), new Vector3(0.3f, 4.8f, 0.25f), Quaternion.identity, matFloor3Circuit);
            CreateBlock($"Arch_Rib_R_{r}", cockpit.transform, new Vector3(11.7f, 11.5f, zRib), new Vector3(0.3f, 4.8f, 0.25f), Quaternion.identity, matFloor3Circuit);
            CreateBlock($"Arch_Ceiling_Rib_{r}", cockpit.transform, new Vector3(0f, 14.25f, zRib), new Vector3(23.2f, 0.25f, 0.25f), Quaternion.identity, matFloor3Circuit);
        }

        // Racks laterales de supercomputadoras neurales
        for (int sr = -2; sr <= 2; sr++)
        {
            float zS = sr * 2.5f;
            CreateBlock($"Server_Rack_L_{sr}", cockpit.transform, new Vector3(-11.2f, 10.5f, zS), new Vector3(0.8f, 2.8f, 2.0f), Quaternion.identity, matFloor3Server);
            CreateBlock($"Server_Rack_R_{sr}", cockpit.transform, new Vector3(11.2f, 10.5f, zS), new Vector3(0.8f, 2.8f, 2.0f), Quaternion.identity, matFloor3Server);
        }

        // PEDESTAL DE MANDO DEL CAPITÁN (R = 2.2m) en (0, 9.2, +1.5)
        GameObject captainPedestal = CreateCylinder("Captain_Command_Pedestal", cockpit.transform, new Vector3(0f, 9.2f, 1.5f), new Vector3(4.4f, 0.35f, 4.4f), Quaternion.identity, matGunmetal, true);
        captainPedestal.AddComponent<CaptainCommandPedestal>();
        CreateCylinder("Pedestal_Hazard_Ring", captainPedestal.transform, new Vector3(0f, 0.52f, 0f), new Vector3(1.02f, 0.2f, 1.02f), Quaternion.identity, matHazard);
        CreateCylinder("Captain_Holo_Disc", captainPedestal.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.85f, 0.05f, 0.85f), Quaternion.identity, matFloor3Holo);

        // Anillos holográficos flotantes sobre el pedestal
        CreateCylinder("Neural_Holo_Ring_Floating", cockpit.transform, new Vector3(0f, 12.0f, 1.5f), new Vector3(5.5f, 0.06f, 5.5f), Quaternion.identity, matFloor3Circuit);

        // Consola táctica de mando del Capitán
        GameObject captainConsole = CreateBlock("Captain_Tactical_Console", cockpit.transform, new Vector3(0f, 9.25f, 5.2f), new Vector3(2.6f, 0.45f, 0.6f), Quaternion.identity, matDarkSteel);
        CreateBlock("Captain_Screen", captainConsole.transform, new Vector3(0f, 0.35f, 0.05f), new Vector3(1.8f, 0.3f, 0.05f), Quaternion.Euler(-20f, 0f, 0f), matFloor3Circuit);

        // Iluminación táctica multinivel
        void AddDownlight(string lName, Vector3 localPos, Color color, float range = 18f, float intensity = 2.4f)
        {
            GameObject lObj = new GameObject(lName);
            lObj.transform.SetParent(cockpit.transform, false);
            lObj.transform.localPosition = localPos;
            Light l = lObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = color;
        }
        AddDownlight("Level1_Pilot_Light", new Vector3(0f, 3.5f, 1.0f), new Color(0.85f, 0.95f, 1.0f), 18f, 2.2f);
        AddDownlight("Level2_Gunner_Light", new Vector3(0f, 7.8f, 2.0f), new Color(1.0f, 0.95f, 0.88f), 18f, 2.4f);
        AddDownlight("Level3_Captain_Light", new Vector3(0f, 13.5f, 1.5f), new Color(0.85f, 0.98f, 1.0f), 22f, 2.8f);

        // =====================================================================
        // 7. CHASIS EXTERIOR ESTILO REAL ROBOT (TORSO EN V, HOMBRERAS ALADAS Y CASCO V-FIN)
        // =====================================================================
        Material matHull = matArmorWhite;
        Material matHyd = matChrome;
        Material matArsenal = GetMat("M_MechaArsenalBarrels") ?? matDarkSteel;

        // Armazón del Torso en V (starts at waist Y = 3.6m width 8.5m, flares to Y = 13.5m width 16.5m)
        GameObject torsoFrame = new GameObject("Torso_V_Frame");
        torsoFrame.transform.SetParent(mechaRoot.transform, false);
        torsoFrame.transform.localPosition = Vector3.zero;

        // 1. Franja Inferior: Blindaje de Planta 1 (Búnker) tipo Glacis de Tanque inclinado con tronera abierta de 8.0m x 0.75m
        // Dos placas blindadas (inferior Y <= 4.35m y superior Y >= 5.08m) que dejan una tronera libre horizontal en el medio a nivel de ojos
        Quaternion glacisExtRot = Quaternion.Euler(22f, 0f, 0f);
        CreateBlock("Chest_Glacis_Plate_Lower", torsoFrame.transform, new Vector3(0f, 3.95f, 4.45f), new Vector3(11.5f, 0.65f, 0.55f), glacisExtRot, matCobaltBlue);
        CreateBlock("Chest_Glacis_Plate_Upper", torsoFrame.transform, new Vector3(0f, 5.60f, 4.15f), new Vector3(11.5f, 0.90f, 0.55f), Quaternion.Euler(25f, 0f, 0f), matCobaltBlue);

        // Marco metálico exterior biselado de la tronera enmarcando la abertura sin obstruir la línea de visión del conductor
        CreateBlock("Chest_Glacis_Visor_Top", torsoFrame.transform, new Vector3(0f, 5.18f, 4.30f), new Vector3(8.0f, 0.10f, 0.35f), Quaternion.Euler(25f, 0f, 0f), matInnerFrame);
        CreateBlock("Chest_Glacis_Visor_Bottom", torsoFrame.transform, new Vector3(0f, 4.32f, 4.50f), new Vector3(8.0f, 0.10f, 0.35f), glacisExtRot, matInnerFrame);
        CreateBlock("Chest_Glacis_Visor_Post_L", torsoFrame.transform, new Vector3(-4.0f, 4.75f, 4.40f), new Vector3(0.35f, 0.76f, 0.35f), Quaternion.identity, matInnerFrame);
        CreateBlock("Chest_Glacis_Visor_Post_R", torsoFrame.transform, new Vector3(4.0f, 4.75f, 4.40f), new Vector3(0.35f, 0.76f, 0.35f), Quaternion.identity, matInnerFrame);

        // Flancos acorazados laterales que cierran los extremos
        CreateBlock("Chest_Glacis_Flank_L", torsoFrame.transform, new Vector3(-4.95f, 4.75f, 4.38f), new Vector3(1.6f, 0.76f, 0.45f), Quaternion.identity, matCobaltBlue);
        CreateBlock("Chest_Glacis_Flank_R", torsoFrame.transform, new Vector3(4.95f, 4.75f, 4.38f), new Vector3(1.6f, 0.76f, 0.45f), Quaternion.identity, matCobaltBlue);

        // 2. Centro de Planta 2 (Y = 6.1m a 8.2m): COMPLETAMENTE DESPEJADO Y ACRISTALADO
        // El gran ventanal panorámico de Planta 2 (Windshield_Level2_Panorame) es transparente al 96%
        // de lado a lado del pecho, permitiendo ver el interior y el exterior sin muros azules centrales.

        // 3. Marco Superior: Collar de Techo de Planta 2 / Planta 3 (Y = 8.6m a 10.0m)
        CreateBlock("Chest_Upper_Collar", torsoFrame.transform, new Vector3(0f, 8.6f, 4.35f), new Vector3(11.5f, 1.4f, 0.8f), Quaternion.Euler(-12f, 0f, 0f), matCobaltBlue);

        // Tomas de Aire Frontales Amarillas/Naranjas (Chest Vents) montadas en el collar superior (no tapan el ventanal)
        CreateBlock("Chest_Vent_L", torsoFrame.transform, new Vector3(-3.8f, 8.7f, 4.7f), new Vector3(2.4f, 1.0f, 0.5f), Quaternion.Euler(-12f, 12f, 0f), matChestVent);
        CreateBlock("Vent_Slats_L", torsoFrame.transform, new Vector3(-3.8f, 8.7f, 4.85f), new Vector3(2.0f, 0.8f, 0.2f), Quaternion.Euler(-12f, 12f, 0f), matDarkSteel);
        CreateBlock("Chest_Vent_R", torsoFrame.transform, new Vector3(3.8f, 8.7f, 4.7f), new Vector3(2.4f, 1.0f, 0.5f), Quaternion.Euler(-12f, -12f, 0f), matChestVent);
        CreateBlock("Vent_Slats_R", torsoFrame.transform, new Vector3(3.8f, 8.7f, 4.85f), new Vector3(2.0f, 0.8f, 0.2f), Quaternion.Euler(-12f, -12f, 0f), matDarkSteel);

        // Placas Pectorales Laterales (en los flancos X = +-6.6m, enmarcan el ventanal sin tapar el centro)
        CreateBlock("Pectoral_Armor_L", torsoFrame.transform, new Vector3(-6.6f, 8.6f, 4.2f), new Vector3(2.8f, 7.8f, 1.8f), Quaternion.Euler(-14f, 18f, 0f), matArmorWhite);
        CreateBlock("Pectoral_Armor_R", torsoFrame.transform, new Vector3(6.6f, 8.6f, 4.2f), new Vector3(2.8f, 7.8f, 1.8f), Quaternion.Euler(-14f, -18f, 0f), matArmorWhite);

        // Muros Laterales en V que envuelven los costados de las 3 plantas
        CreateBlock("Torso_Flank_Armor_L", torsoFrame.transform, new Vector3(-7.6f, 8.5f, 0f), new Vector3(1.4f, 9.5f, 9.6f), Quaternion.Euler(0f, 0f, 12f), matArmorWhite);
        CreateBlock("Torso_Flank_Armor_R", torsoFrame.transform, new Vector3(7.6f, 8.5f, 0f), new Vector3(1.4f, 9.5f, 9.6f), Quaternion.Euler(0f, 0f, -12f), matArmorWhite);

        // Mochila Posterior (Backpack) con toberas de escape gemelas
        CreateBlock("Backpack_Main_Bay", torsoFrame.transform, new Vector3(0f, 8.5f, -5.2f), new Vector3(11.5f, 10.0f, 3.2f), Quaternion.identity, matCobaltBlue);
        CreateBlock("Backpack_Thruster_Nacelle_L", torsoFrame.transform, new Vector3(-4.4f, 8.8f, -6.4f), new Vector3(3.0f, 11.0f, 3.0f), Quaternion.identity, matArmorWhite);
        CreateCylinder("Backpack_Exhaust_Nozzle_L", torsoFrame.transform, new Vector3(-4.4f, 2.8f, -6.4f), new Vector3(2.2f, 1.8f, 2.2f), Quaternion.identity, matChrome);
        CreateBlock("Backpack_Thruster_Nacelle_R", torsoFrame.transform, new Vector3(4.4f, 8.8f, -6.4f), new Vector3(3.0f, 11.0f, 3.0f), Quaternion.identity, matArmorWhite);
        CreateCylinder("Backpack_Exhaust_Nozzle_R", torsoFrame.transform, new Vector3(4.4f, 2.8f, -6.4f), new Vector3(2.2f, 1.8f, 2.2f), Quaternion.identity, matChrome);

        // Cabeza Icónica de Mecha Japonés con V-Fin (Planta 3) conectada sólidamente al techo del torso
        GameObject mechaHead = new GameObject("Mecha_Head");
        mechaHead.transform.SetParent(mechaRoot.transform, false);
        mechaHead.transform.localPosition = new Vector3(0f, 12.0f, 1.2f);

        // Collarín blindado trapezoidal en la base del cuello que se encastra en el techo del torso (4.5m x 1.8m x 4.5m)
        CreateBlock("Head_Neck_Base_Collar", mechaHead.transform, new Vector3(0f, -0.9f, 0f), new Vector3(4.5f, 1.8f, 4.5f), Quaternion.identity, matCobaltBlue);
        CreateCylinder("Head_Neck_Collar_Ring", mechaHead.transform, new Vector3(0f, 0.0f, 0f), new Vector3(4.2f, 0.35f, 4.2f), Quaternion.identity, matInnerFrame);

        // Cuello mecánico central reforzado y pistones hidráulicos laterales y traseros
        CreateCylinder("Head_Neck_Joint", mechaHead.transform, new Vector3(0f, 0.5f, 0f), new Vector3(2.8f, 1.2f, 2.8f), Quaternion.identity, matInnerFrame);
        CreateCylinder("Neck_Damper_L", mechaHead.transform, new Vector3(-1.4f, 0.4f, 0.4f), new Vector3(0.45f, 1.2f, 0.45f), Quaternion.identity, matChrome);
        CreateCylinder("Neck_Damper_R", mechaHead.transform, new Vector3(1.4f, 0.4f, 0.4f), new Vector3(0.45f, 1.2f, 0.45f), Quaternion.identity, matChrome);
        CreateCylinder("Neck_Damper_Rear", mechaHead.transform, new Vector3(0f, 0.4f, -1.2f), new Vector3(0.55f, 1.2f, 0.55f), Quaternion.identity, matChrome);

        CreateBlock("Head_Chin_Guard", mechaHead.transform, new Vector3(0f, 1.2f, 1.4f), new Vector3(1.8f, 1.2f, 1.6f), Quaternion.Euler(-25f, 0f, 0f), matCrimsonRed);
        CreateBlock("Head_Cheek_L", mechaHead.transform, new Vector3(-1.6f, 1.8f, 0.9f), new Vector3(0.7f, 1.8f, 1.8f), Quaternion.Euler(0f, -15f, 0f), matArmorWhite);
        CreateBlock("Head_Cheek_R", mechaHead.transform, new Vector3(1.6f, 1.8f, 0.9f), new Vector3(0.7f, 1.8f, 1.8f), Quaternion.Euler(0f, 15f, 0f), matArmorWhite);
        CreateBlock("Head_Visor_Optics", mechaHead.transform, new Vector3(0f, 2.1f, 1.6f), new Vector3(2.4f, 0.55f, 0.4f), Quaternion.identity, matCyanOptics);
        CreateBlock("Head_Forehead_Brow", mechaHead.transform, new Vector3(0f, 2.7f, 1.3f), new Vector3(2.6f, 0.9f, 1.8f), Quaternion.identity, matCrimsonRed);
        CreateBlock("Head_Helmet_Dome", mechaHead.transform, new Vector3(0f, 2.8f, -0.2f), new Vector3(3.2f, 2.4f, 3.4f), Quaternion.identity, matArmorWhite);
        CreateBlock("Head_Crest", mechaHead.transform, new Vector3(0f, 4.0f, 0.2f), new Vector3(0.9f, 1.2f, 2.8f), Quaternion.identity, matCobaltBlue);

        // Antena Dorada en 'V' (V-Fin Antenna)
        CreateBlock("VFin_Center_Mount", mechaHead.transform, new Vector3(0f, 3.2f, 1.7f), new Vector3(0.8f, 0.8f, 0.45f), Quaternion.Euler(-15f, 0f, 0f), matCrimsonRed);
        CreateBlock("VFin_Blade_L", mechaHead.transform, new Vector3(-1.6f, 4.2f, 1.4f), new Vector3(0.32f, 3.4f, 0.22f), Quaternion.Euler(-18f, 0f, -38f), matGoldVFin);
        CreateBlock("VFin_Blade_R", mechaHead.transform, new Vector3(1.6f, 4.2f, 1.4f), new Vector3(0.32f, 3.4f, 0.22f), Quaternion.Euler(-18f, 0f, 38f), matGoldVFin);

        // Hombrera Izquierda Cúbica Normal Pegada al Brazo (máximo 3.5m x 1.5m x 4.0m, sin tejados flotantes)
        GameObject shoulderL = new GameObject("Shoulder_Left");
        shoulderL.transform.SetParent(mechaRoot.transform, false);
        shoulderL.transform.localPosition = new Vector3(-10.5f, 13.5f, 0.5f);

        CreateSphere("Shoulder_Joint_L", shoulderL.transform, Vector3.zero, Vector3.one * 3.0f, Quaternion.identity, matInnerFrame);
        CreateBlock("Shoulder_Armor_L", shoulderL.transform, new Vector3(0f, 0.4f, 0f), new Vector3(3.4f, 1.5f, 3.8f), Quaternion.identity, matInnerFrame);

        // Brazo Izquierdo Esbelto: Ametralladora Rotatoria Gatling
        GameObject armLeft = new GameObject("Arm_Left_Gatling");
        armLeft.transform.SetParent(mechaRoot.transform, false);
        armLeft.transform.localPosition = new Vector3(-10.5f, 11.5f, 0.5f);

        CreateCylinder("Bicep_Core_L", armLeft.transform, new Vector3(0f, -2.8f, 0f), new Vector3(2.4f, 3.8f, 2.4f), Quaternion.identity, matInnerFrame);
        CreateCylinder("Bicep_Piston_L", armLeft.transform, new Vector3(0.9f, -2.8f, 0.5f), new Vector3(0.65f, 3.4f, 0.65f), Quaternion.identity, matChrome);
        CreateCylinder("Elbow_Hinge_L", armLeft.transform, new Vector3(0f, -4.8f, 0f), new Vector3(2.8f, 0.8f, 2.8f), Quaternion.Euler(0f, 0f, 90f), matDarkSteel);
        CreateBlock("Forearm_Gauntlet_L", armLeft.transform, new Vector3(0f, -6.6f, 1.8f), new Vector3(3.2f, 3.8f, 5.8f), Quaternion.identity, matArmorWhite);
        CreateBlock("Gauntlet_Trim_L", armLeft.transform, new Vector3(0f, -6.6f, 4.4f), new Vector3(3.3f, 3.6f, 0.8f), Quaternion.identity, matCobaltBlue);

        // Tambor de munición estilizado con franja hazard
        GameObject drum = CreateCylinder("Gatling_Ammo_Drum", armLeft.transform, new Vector3(0f, -9.0f, 1.8f), new Vector3(4.0f, 2.2f, 4.0f), Quaternion.identity, matDarkSteel);
        CreateCylinder("Drum_Band", drum.transform, Vector3.zero, new Vector3(1.02f, 0.28f, 1.02f), Quaternion.identity, matHazard);

        // Rotor y 6 cañones cromados con heat tint
        GameObject rotorObj = new GameObject("Gatling_Rotor");
        rotorObj.transform.SetParent(armLeft.transform, false);
        rotorObj.transform.localPosition = new Vector3(0f, -6.6f, 5.8f);

        CreateCylinder("Spindle", rotorObj.transform, new Vector3(0f, 0f, 2.0f), new Vector3(1.5f, 4.6f, 1.5f), Quaternion.Euler(90f, 0f, 0f), matDarkSteel);
        CreateCylinder("Barrel_Ring", rotorObj.transform, new Vector3(0f, 0f, 5.4f), new Vector3(3.2f, 0.35f, 3.2f), Quaternion.Euler(90f, 0f, 0f), matDarkSteel);

        float barrelRadius = 1.2f;
        for (int i = 0; i < 6; i++)
        {
            float ang = i * 60f * Mathf.Deg2Rad;
            Vector3 bLocalPos = new Vector3(Mathf.Cos(ang) * barrelRadius, Mathf.Sin(ang) * barrelRadius, 3.0f);
            CreateCylinder($"Barrel_{i}", rotorObj.transform, bLocalPos, new Vector3(0.42f, 6.2f, 0.42f), Quaternion.Euler(90f, 0f, 0f), matArsenal);
        }

        GameObject muzzleObj = new GameObject("Gatling_Muzzle");
        muzzleObj.transform.SetParent(rotorObj.transform, false);
        muzzleObj.transform.localPosition = new Vector3(0f, 0f, 7.5f);

        GameObject muzzleLightObj = new GameObject("Muzzle_Light");
        muzzleLightObj.transform.SetParent(muzzleObj.transform, false);
        muzzleLightObj.transform.localPosition = Vector3.zero;
        Light ml = muzzleLightObj.AddComponent<Light>();
        ml.type = LightType.Point;
        ml.color = new Color(1.0f, 0.65f, 0.15f);
        ml.range = 35f;
        ml.intensity = 5.5f;
        ml.enabled = false;

        // Hombrera Derecha Cúbica Normal Pegada al Brazo (máximo 3.5m x 1.5m x 4.0m)
        GameObject shoulderR = new GameObject("Shoulder_Right");
        shoulderR.transform.SetParent(mechaRoot.transform, false);
        shoulderR.transform.localPosition = new Vector3(10.5f, 13.5f, 0.5f);

        CreateSphere("Shoulder_Joint_R", shoulderR.transform, Vector3.zero, Vector3.one * 3.0f, Quaternion.identity, matInnerFrame);
        CreateBlock("Shoulder_Armor_R", shoulderR.transform, new Vector3(0f, 0.4f, 0f), new Vector3(3.4f, 1.5f, 3.8f), Quaternion.identity, matInnerFrame);

        // Pod Lanzamisiles Orientable y Aerodinámico sobre el soporte dorsal / hombro derecho
        GameObject podBox = CreateBlock("Missile_Pod", mechaRoot.transform, new Vector3(7.4f, 15.0f, -0.8f), new Vector3(4.4f, 3.6f, 6.4f), Quaternion.Euler(-6f, 0f, 0f), matArmorWhite);
        CreateBlock("Pod_Front_Bezel", podBox.transform, new Vector3(0f, 0f, 0.51f), new Vector3(1.02f, 1.02f, 0.08f), Quaternion.identity, matCobaltBlue);

        Transform[] tubes = new Transform[4];
        int tubeIdx = 0;
        for (int row = 0; row < 2; row++)
        {
            float yOff = row == 0 ? 0.32f : -0.32f;
            for (int colIdx = -1; colIdx <= 1; colIdx++)
            {
                float xOff = colIdx * 0.62f;
                GameObject tube = CreateCylinder($"Tube_{row}_{colIdx}", podBox.transform, new Vector3(xOff / 2.2f, yOff / 1.7f, 0.42f), new Vector3(0.16f, 0.16f, 0.16f), Quaternion.Euler(90f, 0f, 0f), matDarkSteel);
                CreateSphere("Warhead_Cyan_Lens", tube.transform, new Vector3(0f, 0.65f, 0f), Vector3.one * 0.85f, Quaternion.identity, matCyanOptics);
                if (tubeIdx < 4)
                {
                    tubes[tubeIdx++] = tube.transform;
                }
            }
        }

        // Brazo Derecho Articulado Completo: Manipulador de Combate Real Robot, Escudo Táctico y Cañón de Pulso
        GameObject armRight = new GameObject("Arm_Right_MissilePod");
        armRight.transform.SetParent(mechaRoot.transform, false);
        armRight.transform.localPosition = new Vector3(10.5f, 11.5f, 0.5f);

        // 1. Bíceps mecánico esbelto y pistón hidráulico
        CreateCylinder("Bicep_Core_R", armRight.transform, new Vector3(0f, -2.8f, 0f), new Vector3(2.4f, 3.8f, 2.4f), Quaternion.identity, matInnerFrame);
        CreateCylinder("Bicep_Piston_R", armRight.transform, new Vector3(-0.9f, -2.8f, 0.5f), new Vector3(0.65f, 3.4f, 0.65f), Quaternion.identity, matChrome);
        CreateBlock("Bicep_Armor_R", armRight.transform, new Vector3(0f, -2.8f, -0.2f), new Vector3(2.8f, 3.2f, 2.8f), Quaternion.identity, matCobaltBlue);

        // 2. Codo articulado cilíndrico
        CreateCylinder("Elbow_Hinge_R", armRight.transform, new Vector3(0f, -4.8f, 0f), new Vector3(2.8f, 0.8f, 2.8f), Quaternion.Euler(0f, 0f, 90f), matDarkSteel);

        // 3. Antebrazo acorazado blanco con bisel cobalto
        CreateBlock("Forearm_Gauntlet_R", armRight.transform, new Vector3(0f, -6.6f, 1.8f), new Vector3(3.2f, 3.8f, 5.8f), Quaternion.identity, matArmorWhite);
        CreateBlock("Gauntlet_Trim_R", armRight.transform, new Vector3(0f, -6.6f, 4.4f), new Vector3(3.3f, 3.6f, 0.8f), Quaternion.identity, matCobaltBlue);

        // 4. Muñeca cilíndrica articulada con pistones
        CreateCylinder("Wrist_Joint_R", armRight.transform, new Vector3(0f, -6.6f, 5.0f), new Vector3(2.4f, 0.7f, 2.4f), Quaternion.Euler(90f, 0f, 0f), matInnerFrame);
        CreateCylinder("Wrist_Piston_Top", armRight.transform, new Vector3(0f, -5.7f, 5.0f), new Vector3(0.45f, 0.9f, 0.45f), Quaternion.Euler(90f, 0f, 0f), matChrome);
        CreateCylinder("Wrist_Piston_Bottom", armRight.transform, new Vector3(0f, -7.5f, 5.0f), new Vector3(0.45f, 0.9f, 0.45f), Quaternion.Euler(90f, 0f, 0f), matChrome);

        // 5. Mano robótica articulada de Mecha Japonés (dorso blindado azul/blanco y nudillos dorados)
        GameObject handRoot = new GameObject("Mecha_Hand_R");
        handRoot.transform.SetParent(armRight.transform, false);
        handRoot.transform.localPosition = Vector3.zero;

        CreateBlock("Hand_Palm_Core", handRoot.transform, new Vector3(0f, -6.6f, 6.0f), new Vector3(2.5f, 2.0f, 1.8f), Quaternion.identity, matInnerFrame);
        CreateBlock("Hand_Back_Armor", handRoot.transform, new Vector3(0f, -5.5f, 6.0f), new Vector3(2.7f, 0.5f, 2.0f), Quaternion.identity, matCobaltBlue);
        CreateBlock("Hand_Plate_White", handRoot.transform, new Vector3(0f, -5.35f, 6.0f), new Vector3(1.8f, 0.25f, 1.4f), Quaternion.identity, matArmorWhite);
        CreateBlock("Knuckle_Guard", handRoot.transform, new Vector3(0f, -5.6f, 7.0f), new Vector3(2.8f, 0.7f, 0.5f), Quaternion.identity, matGoldVFin);

        // 4 Dedos mecánicos cerrados en puño de combate pesado
        float[] fingerXs = new float[] { -0.9f, -0.3f, 0.3f, 0.9f };
        for (int f = 0; f < fingerXs.Length; f++)
        {
            float fx = fingerXs[f];
            CreateBlock($"Finger_Proximal_{f}", handRoot.transform, new Vector3(fx, -6.4f, 7.3f), new Vector3(0.48f, 0.65f, 0.95f), Quaternion.identity, matInnerFrame);
            CreateCylinder($"Finger_Knuckle_{f}", handRoot.transform, new Vector3(fx, -6.4f, 7.75f), new Vector3(0.42f, 0.45f, 0.42f), Quaternion.Euler(0f, 90f, 0f), matDarkSteel);
            CreateBlock($"Finger_Distal_{f}", handRoot.transform, new Vector3(fx, -7.0f, 7.6f), new Vector3(0.46f, 0.85f, 0.6f), Quaternion.identity, matDarkSteel);
        }

        // Pulgar hidráulico opuesto reforzado
        CreateBlock("Thumb_Base", handRoot.transform, new Vector3(-1.25f, -6.9f, 5.8f), new Vector3(0.6f, 0.7f, 1.1f), Quaternion.Euler(15f, -25f, 20f), matCobaltBlue);
        CreateCylinder("Thumb_Knuckle", handRoot.transform, new Vector3(-1.15f, -7.2f, 6.35f), new Vector3(0.45f, 0.5f, 0.45f), Quaternion.identity, matInnerFrame);
        CreateBlock("Thumb_Claw", handRoot.transform, new Vector3(-0.95f, -7.5f, 6.7f), new Vector3(0.55f, 0.75f, 0.6f), Quaternion.Euler(20f, -15f, 0f), matDarkSteel);

        // 6. Escudo Táctico Lateral (Tactical Strike Shield con franja hazard y cresta dorada)
        GameObject shieldObj = new GameObject("Forearm_Tactical_Shield");
        shieldObj.transform.SetParent(armRight.transform, false);
        shieldObj.transform.localPosition = Vector3.zero;

        CreateBlock("Shield_Mount_Bracket", shieldObj.transform, new Vector3(1.7f, -6.6f, 2.0f), new Vector3(0.5f, 1.2f, 2.2f), Quaternion.identity, matInnerFrame);
        CreateBlock("Shield_Main_Blade", shieldObj.transform, new Vector3(2.05f, -6.6f, 2.0f), new Vector3(0.45f, 3.4f, 8.2f), Quaternion.identity, matCobaltBlue);
        CreateBlock("Shield_Center_Armor", shieldObj.transform, new Vector3(2.25f, -6.6f, 2.0f), new Vector3(0.35f, 2.5f, 6.0f), Quaternion.identity, matArmorWhite);
        CreateBlock("Shield_Prow_Crest", shieldObj.transform, new Vector3(2.35f, -6.6f, 4.8f), new Vector3(0.45f, 1.8f, 1.6f), Quaternion.identity, matGoldVFin);
        CreateBlock("Shield_Hazard_Stripes", shieldObj.transform, new Vector3(2.36f, -6.6f, 1.5f), new Vector3(0.06f, 0.55f, 4.2f), Quaternion.identity, matHazard);

        // 7. Cañón de Pulso Gemelo Integrado bajo el antebrazo
        GameObject pulseObj = new GameObject("Forearm_Pulse_Cannon");
        pulseObj.transform.SetParent(armRight.transform, false);
        pulseObj.transform.localPosition = Vector3.zero;

        CreateBlock("Pulse_Cannon_Housing", pulseObj.transform, new Vector3(0f, -8.3f, 2.6f), new Vector3(2.2f, 1.1f, 4.5f), Quaternion.identity, matDarkSteel);
        CreateCylinder("Pulse_Barrel_L", pulseObj.transform, new Vector3(-0.55f, -8.3f, 5.0f), new Vector3(0.38f, 2.6f, 0.38f), Quaternion.Euler(90f, 0f, 0f), matArsenal);
        CreateCylinder("Pulse_Barrel_R", pulseObj.transform, new Vector3(0.55f, -8.3f, 5.0f), new Vector3(0.38f, 2.6f, 0.38f), Quaternion.Euler(90f, 0f, 0f), matArsenal);
        CreateCylinder("Pulse_Optic_Tip_L", pulseObj.transform, new Vector3(-0.55f, -8.3f, 6.35f), new Vector3(0.32f, 0.15f, 0.32f), Quaternion.Euler(90f, 0f, 0f), matCyanOptics);
        CreateCylinder("Pulse_Optic_Tip_R", pulseObj.transform, new Vector3(0.55f, -8.3f, 6.35f), new Vector3(0.32f, 0.15f, 0.32f), Quaternion.Euler(90f, 0f, 0f), matCyanOptics);

        // =====================================================================
        // 8. BOTONES FÍSICOS REPOSICIONADOS (ESCALA ADAPTADA CON ICONOS DEDICADOS)
        // =====================================================================
        void CreateFloorButton(string id, string label, Vector3 localPos, float radius, Material capMat, CockpitFloorButton.ButtonType bType, float detectionRadius = 1.4f)
        {
            GameObject bObj = new GameObject($"Btn_{id}");
            bObj.transform.SetParent(cockpit.transform, false);
            bObj.transform.localPosition = localPos;
            bObj.transform.localRotation = Quaternion.identity;
            bObj.transform.localScale = Vector3.one;

            GameObject bHousing = CreateCylinder("Housing", bObj.transform, Vector3.zero, new Vector3(radius * 2.3f, 0.18f, radius * 2.3f), Quaternion.identity, matBtnBase);
            CreateCylinder("RimRing", bHousing.transform, new Vector3(0f, 0.55f, 0f), new Vector3(1.02f, 0.2f, 1.02f), Quaternion.identity, matHazard);

            GameObject bCap = CreateCylinder("Cap", bObj.transform, new Vector3(0f, 0.22f, 0f), new Vector3(radius * 2.0f, 0.22f, radius * 2.0f), Quaternion.identity, capMat);

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(bCap.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            labelObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            labelObj.transform.localScale = Vector3.one * 0.18f;
            TextMesh tm = labelObj.AddComponent<TextMesh>();
            tm.text = label;
            tm.fontSize = 32;
            tm.characterSize = 0.28f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = Color.white;
            TextShaderUtility.ApplyZTesting(tm);

            CockpitFloorButton comp = bObj.AddComponent<CockpitFloorButton>();
            comp.buttonId = id;
            comp.label = label;
            comp.type = bType;
            comp.detectionRadius = detectionRadius;
            comp.buttonColor = capMat != null ? capMat.color : Color.white;

            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("capTransform").objectReferenceValue = bCap.transform;
            so.ApplyModifiedProperties();
        }

        // --- BOTONES DE PLANTA 1 (Y = 0.0m: CONDUCTOR) ---
        CreateFloorButton("FWD", "▲ AVANZAR", new Vector3(0.0f, 0f, 4.2f), 1.0f, matBtnGreen, CockpitFloorButton.ButtonType.Hold, 1.4f);
        CreateFloorButton("BWD", "▼ ATRÁS", new Vector3(0.0f, 0f, -2.2f), 1.0f, matBtnAmber, CockpitFloorButton.ButtonType.Hold, 1.4f);
        CreateFloorButton("LEFT", "◄ GIRO IZQ", new Vector3(-4.2f, 0f, 1.0f), 0.95f, matBtnCyan, CockpitFloorButton.ButtonType.Hold, 1.35f);
        CreateFloorButton("RIGHT", "GIRO DER ►", new Vector3(4.2f, 0f, 1.0f), 0.95f, matBtnCyan, CockpitFloorButton.ButtonType.Hold, 1.35f);

        // --- BOTONES DE PLANTA 2 (Y = 4.5m: ARTILLERO) ---
        SetupGunnerAimPad.BuildAimPadHierarchy(cockpit.transform, new Vector3(0.0f, 4.54f, 5.2f), matDarkSteel, matGunmetal, matChrome, matHazard, matBtnBase, matBtnCyan, matBtnGreen, matBtnAmber, 1.2f);
        CreateFloorButton("GATLING", "🔫 GATLING\n[MANTENER / E]", new Vector3(-4.2f, 4.5f, 1.6f), 1.0f, matBtnAmber, CockpitFloorButton.ButtonType.Hold, 1.4f);
        CreateFloorButton("MISSILES", "🚀 MISILES\n(CULAZO E)", new Vector3(4.2f, 4.5f, 1.6f), 1.0f, matBtnCyan, CockpitFloorButton.ButtonType.Stomp, 1.4f);

        // --- BOTÓN DE PLANTA 3 (Y = 9.0m: CAPITÁN) ---
        CreateFloorButton("SPAWN", "🚨 ROBOTS\n[ACTIVAR]", new Vector3(0.0f, 9.0f, -3.2f), 0.95f, matBtnRose, CockpitFloorButton.ButtonType.Toggle, 1.35f);

        // =====================================================================
        // 9. EL PATO PILOTO PBR (Escala 0.38x - Spawnea en Planta 1)
        // =====================================================================
        GameObject duck = new GameObject("Duck");
        duck.transform.SetParent(cockpit.transform, false);
        duck.transform.localPosition = new Vector3(0.0f, 0.25f, 1.0f);
        duck.transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);
        duck.tag = "Player";

        Rigidbody rb = duck.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        CapsuleCollider duckCollider = duck.AddComponent<CapsuleCollider>();
        duckCollider.center = new Vector3(0f, 0.25f, 0f);
        duckCollider.radius = 0.22f;
        duckCollider.height = 0.5f;
        duckCollider.isTrigger = true;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(duck.transform, false);
        visual.transform.localPosition = Vector3.zero;

        GameObject body = CreateSphere("Duck_Body", visual.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.85f, 0.75f, 0.95f), Quaternion.identity, matDuckYellow);
        GameObject head = CreateSphere("Duck_Head", visual.transform, new Vector3(0f, 0.85f, 0.25f), new Vector3(0.55f, 0.55f, 0.55f), Quaternion.identity, matDuckYellow);
        CreateBlock("Duck_Beak", visual.transform, new Vector3(0f, 0.8f, 0.58f), new Vector3(0.32f, 0.12f, 0.35f), Quaternion.identity, matDuckOrange);
        CreateSphere("Duck_Eye_L", visual.transform, new Vector3(-0.18f, 0.92f, 0.42f), Vector3.one * 0.12f, Quaternion.identity, matDuckBlack);
        CreateSphere("Duck_Eye_R", visual.transform, new Vector3(0.18f, 0.92f, 0.42f), Vector3.one * 0.12f, Quaternion.identity, matDuckBlack);
        GameObject wingL = CreateSphere("Duck_Wing_L", visual.transform, new Vector3(-0.44f, 0.42f, 0f), new Vector3(0.15f, 0.35f, 0.55f), Quaternion.Euler(0f, 0f, 20f), matDuckYellow);
        GameObject wingR = CreateSphere("Duck_Wing_R", visual.transform, new Vector3(0.44f, 0.42f, 0f), new Vector3(0.15f, 0.35f, 0.55f), Quaternion.Euler(0f, 0f, -20f), matDuckYellow);
        CreateBlock("Duck_Foot_L", visual.transform, new Vector3(-0.25f, 0.05f, 0.05f), new Vector3(0.22f, 0.08f, 0.32f), Quaternion.identity, matDuckOrange);
        CreateBlock("Duck_Foot_R", visual.transform, new Vector3(0.25f, 0.05f, 0.05f), new Vector3(0.22f, 0.08f, 0.32f), Quaternion.identity, matDuckOrange);

        GameObject dropShadow = CreateCylinder("DropShadow", duck.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.75f, 0.005f, 0.75f), Quaternion.identity, matBtnBase);

        DuckController duckCtrl = duck.AddComponent<DuckController>();
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

        // 10. CONECTAR REFERENCIAS EN MECHACONTROLLER
        SerializedObject mechaSO = new SerializedObject(mechaCtrl);
        string physConfigPath = "Assets/Prefabs/Mecha/MechaPhysicsConfig.asset";
        MechaPhysicsConfig physConfig = AssetDatabase.LoadAssetAtPath<MechaPhysicsConfig>(physConfigPath);
        if (physConfig != null)
        {
            mechaSO.FindProperty("physicsConfig").objectReferenceValue = physConfig;
        }
        mechaSO.FindProperty("leftLeg").objectReferenceValue = legLeft;
        mechaSO.FindProperty("rightLeg").objectReferenceValue = legRight;
        mechaSO.FindProperty("cockpitTransform").objectReferenceValue = cockpit.transform;
        mechaSO.FindProperty("leftLegTransform").objectReferenceValue = legLeft.transform;
        mechaSO.FindProperty("rightLegTransform").objectReferenceValue = legRight.transform;
        mechaSO.FindProperty("leftArmTransform").objectReferenceValue = armLeft.transform;
        mechaSO.FindProperty("rightArmTransform").objectReferenceValue = armRight.transform;
        mechaSO.FindProperty("gatlingRotorTransform").objectReferenceValue = rotorObj.transform;
        mechaSO.FindProperty("gatlingMuzzleTransform").objectReferenceValue = muzzleObj.transform;
        mechaSO.FindProperty("gatlingMuzzleLight").objectReferenceValue = ml;
        mechaSO.FindProperty("missilePodTransform").objectReferenceValue = podBox.transform;
        mechaSO.FindProperty("gatlingGimbalTransform").objectReferenceValue = armLeft.transform;
        mechaSO.FindProperty("missilePodGimbalTransform").objectReferenceValue = podBox.transform;

        SerializedProperty tubesProp = mechaSO.FindProperty("missileLaunchTubes");
        tubesProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            tubesProp.GetArrayElementAtIndex(i).objectReferenceValue = tubes[i];
        }
        mechaSO.ApplyModifiedProperties();

        // 11. GENERADOR DE ENEMIGOS
        GameObject spawnerObj = new GameObject("EnemySpawner");
        EnemySpawner spawnerComp = spawnerObj.AddComponent<EnemySpawner>();
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/EnemyBot.prefab");
        if (enemyPrefab != null)
        {
            SerializedObject spawnerSO = new SerializedObject(spawnerComp);
            spawnerSO.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
            spawnerSO.ApplyModifiedProperties();
        }

        // 12. CÁMARA ORBITAL Y LIBRE
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
            camSO.FindProperty("maxDistance").floatValue = 160.0f;
            camSO.FindProperty("maxInteriorDistance").floatValue = 5.2f;
            camSO.FindProperty("currentDistance").floatValue = 4.2f;
            camSO.FindProperty("targetDistance").floatValue = 4.2f;
            camSO.FindProperty("captainCircleCenter").vector3Value = new Vector3(0f, 9.2f, 1.5f);
            camSO.FindProperty("captainCircleRadius").floatValue = 2.2f;
            camSO.FindProperty("captainCircleExitRadius").floatValue = 2.5f;
            camSO.ApplyModifiedProperties();

            Vector3 camPos = mechaRoot.transform.position + new Vector3(0f, 36.0f, -14.0f);
            Vector3 lookTarget = mechaRoot.transform.position + new Vector3(0f, 30.0f, 6.0f);
            mainCam.transform.position = camPos;
            mainCam.transform.rotation = Quaternion.LookRotation(lookTarget - camPos);
        }

        // 13. HUD COMPLETO CON UI TOOLKIT (UIDocument + UXML + USS)
        GameObject hudObj = new GameObject("HUD");
        UIDocument uiDoc = hudObj.AddComponent<UIDocument>();
        uiDoc.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/MechaPanelSettings.asset");
        uiDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/MechaHUD.uxml");
        MechaHUD hud = hudObj.AddComponent<MechaHUD>();

        var fontAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.FontAsset>("Assets/UI/MechaFontAsset.asset");
        SerializedObject hudSO = new SerializedObject(hud);
        hudSO.FindProperty("hudFontAsset").objectReferenceValue = fontAsset;
        hudSO.ApplyModifiedProperties();

        // 14. AUTO PLAY TESTER
        GameObject testerObj = new GameObject("AutoPlayTester");
        var testerComp = testerObj.AddComponent<AutoPlayTester>();
        SerializedObject testerSO = new SerializedObject(testerComp);
        var propAutoRun = testerSO.FindProperty("autoRunOnStart");
        if (propAutoRun != null) propAutoRun.boolValue = false;
        testerSO.ApplyModifiedProperties();

        // 15. GUARDAR PREFAB ESTÁNDAR MECHATITAN
        string mechaPrefabPath = "Assets/Prefabs/Mecha/MechaTitan.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(mechaRoot, mechaPrefabPath, InteractionMode.AutomatedAction);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("🦆 ¡Mecha Titán Colosal (50m, Complejo 60m x 45m x 28m en 3 Plantas, Texturizado PBR y Prefabs) ensamblado con éxito!");
    }
}
