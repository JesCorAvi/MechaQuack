using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;

public static class AssembleMechaPrototype
{
    [MenuItem("Mecha/Assemble Complete Prototype")]
    [MenuItem("PatoJuego/Assemble Full Prototype")]
    public static void Build()
    {
        // Generar y actualizar la biblioteca de Prefabs estándar
        MechaPrefabBuilder.BuildAllPrefabs();

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

        Material CreateMat(string path, Color col, float metallic = 0.2f, float smoothness = 0.5f, bool isEmission = false, Color? emissionCol = null)
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

        // --- PALETA MILITAR Y MECÁNICA DE ALTA DEFINICIÓN (ESTILO IMAGEN 2) ---
        // Verde militar oliva para blindaje primario
        Material matMilGreen = CreateMat("Assets/Materials/M_MechaMilGreen.mat", new Color(0.18f, 0.27f, 0.18f), 0.22f, 0.45f);
        // Verde oscuro de sombra / huecos
        Material matMilGreenDark = CreateMat("Assets/Materials/M_MechaMilGreenDark.mat", new Color(0.11f, 0.17f, 0.11f), 0.25f, 0.40f);
        // Metal de cañón / Gunmetal para placas secundarias, biseles y carcasas
        Material matGunmetal = CreateMat("Assets/Materials/M_MechaGunmetal.mat", new Color(0.14f, 0.16f, 0.19f), 0.65f, 0.52f);
        // Acero oscuro fundido / Chasis interno y juntas estructurales
        Material matDarkSteel = CreateMat("Assets/Materials/M_MechaDarkSteel.mat", new Color(0.08f, 0.09f, 0.10f), 0.75f, 0.35f);
        // Cromo pulido / Vástagos de pistones hidráulicos, cañones Gatling y engranajes
        Material matChrome = CreateMat("Assets/Materials/M_MechaChrome.mat", new Color(0.88f, 0.91f, 0.95f), 0.96f, 0.90f);
        // Latón / Bronce industrial para válvulas de presión y tapas de cilindros
        Material matBrass = CreateMat("Assets/Materials/M_MechaBrass.mat", new Color(0.78f, 0.62f, 0.25f), 0.85f, 0.72f);
        // Ópticas y sensores cian emisivos (Visor de casco, cabezas de micromisiles)
        Material matCyanOptics = CreateMat("Assets/Materials/M_MechaCyanOptics.mat", new Color(0.0f, 0.95f, 1.0f), 0.1f, 0.95f, true, new Color(0.0f, 1.9f, 2.2f));
        // Reactor de energía naranja/ámbar en el pecho (Turbina central con emisión HDR)
        Material matCoreOrange = CreateMat("Assets/Materials/M_MechaCoreOrange.mat", new Color(1.0f, 0.42f, 0.02f), 0.1f, 0.95f, true, new Color(2.4f, 0.85f, 0.05f));
        // Marcas y franjas tácticas blancas de pelotón / estarcidos
        Material matDecalWhite = CreateMat("Assets/Materials/M_MechaDecalWhite.mat", new Color(0.92f, 0.93f, 0.95f), 0.15f, 0.50f);
        // Franjas de advertencia de peligro (Hazard stripes)
        Material matHazard = CreateMat("Assets/Materials/M_HazardStripe.mat", new Color(0.95f, 0.72f, 0.05f), 0.2f, 0.7f, true);

        // Materiales de cabina y pato
        Material matDuckYellow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_DuckYellow.mat");
        Material matDuckOrange = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_DuckOrange.mat");
        Material matDuckBlack = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_DuckBlack.mat");
        Material matFloor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CockpitFloor.mat");
        Material matWall = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CockpitWall.mat");
        Material matGlass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CockpitDomeGlass.mat");
        if (matGlass == null)
        {
            matGlass = new Material(urpLit);
            AssetDatabase.CreateAsset(matGlass, "Assets/Materials/M_CockpitDomeGlass.mat");
        }
        matGlass.shader = urpLit;
        matGlass.SetFloat("_Surface", 1f); // Transparent
        matGlass.SetFloat("_Blend", 0f);   // Alpha blend
        matGlass.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        matGlass.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        matGlass.SetFloat("_ZWrite", 0f);
        matGlass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        matGlass.DisableKeyword("_ALPHATEST_ON");
        matGlass.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        Color glassTint = new Color(0.92f, 0.98f, 1.0f, 0.04f); // 96% transparente para máxima visibilidad
        matGlass.color = glassTint;
        if (matGlass.HasProperty("_BaseColor")) matGlass.SetColor("_BaseColor", glassTint);
        if (matGlass.HasProperty("_Smoothness")) matGlass.SetFloat("_Smoothness", 0.95f);
        if (matGlass.HasProperty("_Metallic")) matGlass.SetFloat("_Metallic", 0.02f);
        EditorUtility.SetDirty(matGlass);

        // Botones
        Material matBtnGreen = CreateMat("Assets/Materials/M_BtnGreen.mat", new Color(0.06f, 0.72f, 0.51f), 0.1f, 0.7f, true);
        Material matBtnAmber = CreateMat("Assets/Materials/M_BtnAmber.mat", new Color(0.96f, 0.62f, 0.04f), 0.1f, 0.7f, true);
        Material matBtnCyan = CreateMat("Assets/Materials/M_BtnCyan.mat", new Color(0.02f, 0.71f, 0.83f), 0.1f, 0.7f, true);
        Material matBtnRose = CreateMat("Assets/Materials/M_BtnRose.mat", new Color(0.95f, 0.25f, 0.37f), 0.1f, 0.7f, true);
        Material matBtnBlue = CreateMat("Assets/Materials/M_BtnBlue.mat", new Color(0.23f, 0.51f, 0.96f), 0.1f, 0.7f, true);
        Material matBtnBase = CreateMat("Assets/Materials/M_BtnBase.mat", new Color(0.06f, 0.09f, 0.16f), 0.5f, 0.4f);

        // 2. SISTEMA DE AUDIO PROCEDURAL
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

        // 3. MEGATERRENO DE PRUEBAS DE MOVIMIENTO (600m x 600m, 8 sectores especializados)
        GameObject outside = new GameObject("OutsideCityView");
        var palette = MechaProvingGroundsGenerator.MaterialPalette.LoadOrCreate();
        MechaProvingGroundsGenerator.BuildWorld(outside.transform, palette);

        // --- HELPERS PARA MODELADO PROCEDURAL DE ALTO DETALLE ---
        GameObject CreateBlock(string name, Transform parent, Vector3 localPos, Vector3 scale, Quaternion localRot, Material m, bool hasCollider = false)
        {
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name;
            b.transform.parent = parent;
            b.transform.localPosition = localPos;
            b.transform.localRotation = localRot;
            b.transform.localScale = scale;
            if (m != null) b.GetComponent<MeshRenderer>().material = m;
            if (!hasCollider) Object.DestroyImmediate(b.GetComponent<Collider>());
            return b;
        }

        GameObject CreateCylinder(string name, Transform parent, Vector3 localPos, Vector3 scale, Quaternion localRot, Material m, bool hasCollider = false)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            c.name = name;
            c.transform.parent = parent;
            c.transform.localPosition = localPos;
            c.transform.localRotation = localRot;
            c.transform.localScale = scale;
            if (m != null) c.GetComponent<MeshRenderer>().material = m;
            if (!hasCollider) Object.DestroyImmediate(c.GetComponent<Collider>());
            return c;
        }

        GameObject CreateSphere(string name, Transform parent, Vector3 localPos, Vector3 scale, Quaternion localRot, Material m, bool hasCollider = false)
        {
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = name;
            s.transform.parent = parent;
            s.transform.localPosition = localPos;
            s.transform.localRotation = localRot;
            s.transform.localScale = scale;
            if (m != null) s.GetComponent<MeshRenderer>().material = m;
            if (!hasCollider) Object.DestroyImmediate(s.GetComponent<Collider>());
            return s;
        }

        // 4. EL MECHA TITÁN MILITAR (Escala colosal 50m de altura, torso multinivel con 3 plantas y física biomecánica)
        GameObject mechaRoot = new GameObject("Mecha_Root");
        MechaController mechaCtrl = mechaRoot.AddComponent<MechaController>();

        Rigidbody mechaRb = mechaRoot.GetComponent<Rigidbody>();
        mechaRb.mass = 48000f; // 48 toneladas de poder bélico colosal para el Mecha de 50m

        BoxCollider mechaCollider = mechaRoot.AddComponent<BoxCollider>();
        mechaCollider.size = new Vector3(20.0f, 50.0f, 18.0f);
        mechaCollider.center = new Vector3(0f, 25.0f, 0f);

        // --- CADERA / PELVIS BLINDADA (Y = 18.2m, bajo el suelo de cabina Y = 21.0m) ---
        // Toda la maquinaria motriz de cadera queda aislada por debajo del forjado inferior
        GameObject pelvis = new GameObject("Pelvis");
        pelvis.transform.parent = mechaRoot.transform;
        pelvis.transform.localPosition = new Vector3(0f, 18.2f, 0f);

        // Bloque central de chasis (ubicado por debajo del nivel de suelo de Planta 1)
        CreateBlock("Pelvis_Core", pelvis.transform, new Vector3(0f, -2.5f, 0f), new Vector3(13.0f, 4.8f, 9.6f), Quaternion.identity, matDarkSteel);
        // Anillo de rotación de cintura con estrías (sellando unión con fondo de cabina)
        CreateCylinder("Waist_Turret_Ring", pelvis.transform, new Vector3(0f, 1.4f, 0f), new Vector3(10.0f, 0.35f, 10.0f), Quaternion.identity, matGunmetal);
        // Blindaje frontal de entrepierna / Glacis inferior angular
        CreateBlock("Groin_Codpiece_Armor", pelvis.transform, new Vector3(0f, -3.6f, 5.0f), new Vector3(5.6f, 4.4f, 2.8f), Quaternion.Euler(-20f, 0f, 0f), matMilGreen);
        CreateBlock("Groin_Decal_Chevron", pelvis.transform, new Vector3(0f, -3.6f, 6.5f), new Vector3(2.4f, 0.8f, 0.16f), Quaternion.Euler(-20f, 0f, 0f), matDecalWhite);
        // Faldones protectores de cadera (Tassets frontales)
        CreateBlock("Tasset_Front_L", pelvis.transform, new Vector3(-4.4f, -4.0f, 4.6f), new Vector3(3.2f, 4.8f, 0.8f), Quaternion.Euler(-15f, 8f, 0f), matMilGreen);
        CreateBlock("Tasset_Front_R", pelvis.transform, new Vector3(4.4f, -4.0f, 4.6f), new Vector3(3.2f, 4.8f, 0.8f), Quaternion.Euler(-15f, -8f, 0f), matMilGreen);
        // Faldones laterales acampanados con rejillas de ventilación
        CreateBlock("Skirt_Side_L", pelvis.transform, new Vector3(-7.2f, -3.0f, 0f), new Vector3(1.0f, 5.2f, 8.4f), Quaternion.Euler(0f, 0f, 16f), matMilGreen);
        CreateBlock("Skirt_Vent_L", pelvis.transform, new Vector3(-7.7f, -3.0f, 0f), new Vector3(0.16f, 3.6f, 4.8f), Quaternion.Euler(0f, 0f, 16f), matGunmetal);
        CreateBlock("Skirt_Side_R", pelvis.transform, new Vector3(7.2f, -3.0f, 0f), new Vector3(1.0f, 5.2f, 8.4f), Quaternion.Euler(0f, 0f, -16f), matMilGreen);
        CreateBlock("Skirt_Vent_R", pelvis.transform, new Vector3(7.7f, -3.0f, 0f), new Vector3(0.16f, 3.6f, 4.8f), Quaternion.Euler(0f, 0f, -16f), matGunmetal);
        // Faldón posterior
        CreateBlock("Skirt_Rear", pelvis.transform, new Vector3(0f, -3.4f, -5.0f), new Vector3(11.6f, 4.4f, 1.2f), Quaternion.Euler(18f, 0f, 0f), matMilGreen);

        // --- PIERNAS ARTICULADAS PROCEDURALES (IK TWO-BONE, ALCANCE 21.6m) ---
        ProceduralLeg CreateMilitaryLeg(string name, float xPos)
        {
            float sideSign = Mathf.Sign(xPos);

            GameObject legRoot = new GameObject(name);
            legRoot.transform.parent = mechaRoot.transform;
            legRoot.transform.localPosition = new Vector3(xPos, 18.2f, 0f);

            // Rótula esférica de cadera y pistón horizontal estabilizador (quedan 1.0m bajo suelo)
            CreateSphere("Hip_Gimbal_Ball", legRoot.transform, Vector3.zero, Vector3.one * 3.6f, Quaternion.identity, matDarkSteel);
            CreateCylinder("Hip_Damper_Piston", legRoot.transform, new Vector3(-sideSign * 1.8f, 0.4f, 0f), new Vector3(0.7f, 1.6f, 0.7f), Quaternion.Euler(0f, 0f, 90f), matChrome);

            // 1. Muslo (Thigh) - Estructura pesada con pistones gemelos frontales y blindaje angular
            GameObject thigh = new GameObject("Thigh");
            thigh.transform.parent = legRoot.transform;
            thigh.transform.localPosition = Vector3.zero;

            // Hueso estructural
            CreateBlock("Thigh_Bone_Core", thigh.transform, new Vector3(0f, -5.2f, 0.4f), new Vector3(4.8f, 9.6f, 5.2f), Quaternion.identity, matDarkSteel);
            // Plancha de blindaje angular frontal verde militar
            CreateBlock("Thigh_Armor_Glacis", thigh.transform, new Vector3(0f, -5.0f, 3.2f), new Vector3(5.6f, 8.8f, 1.6f), Quaternion.Euler(-8f, 0f, 0f), matMilGreen);
            CreateBlock("Thigh_Bevel_L", thigh.transform, new Vector3(-2.6f, -5.0f, 2.8f), new Vector3(0.8f, 8.4f, 1.2f), Quaternion.Euler(-8f, 15f, 0f), matGunmetal);
            CreateBlock("Thigh_Bevel_R", thigh.transform, new Vector3(2.6f, -5.0f, 2.8f), new Vector3(0.8f, 8.4f, 1.2f), Quaternion.Euler(-8f, -15f, 0f), matGunmetal);
            // Pistones hidráulicos frontales cromados que bajan hacia la rodilla (escala calibrada para no penetrar el suelo)
            CreateCylinder("Piston_Front_L", thigh.transform, new Vector3(-1.5f, -5.2f, 3.8f), new Vector3(0.6f, 4.0f, 0.6f), Quaternion.identity, matChrome);
            CreateCylinder("Piston_Cap_L", thigh.transform, new Vector3(-1.5f, -1.4f, 3.8f), new Vector3(0.88f, 0.5f, 0.88f), Quaternion.identity, matBrass);
            CreateCylinder("Piston_Front_R", thigh.transform, new Vector3(1.5f, -5.2f, 3.8f), new Vector3(0.6f, 4.0f, 0.6f), Quaternion.identity, matChrome);
            CreateCylinder("Piston_Cap_R", thigh.transform, new Vector3(1.5f, -1.4f, 3.8f), new Vector3(0.88f, 0.5f, 0.88f), Quaternion.identity, matBrass);
            // Cables / mangueras laterales
            CreateCylinder("Thigh_Conduit_Outer", thigh.transform, new Vector3(sideSign * 2.7f, -4.5f, 0.2f), new Vector3(0.4f, 4.0f, 0.4f), Quaternion.identity, matDarkSteel);

            // 2. Articulación de Rodilla (Knee Joint) a -10.4m del hip
            GameObject kneeJoint = new GameObject("Joint_Knee");
            kneeJoint.transform.parent = legRoot.transform;
            kneeJoint.transform.localPosition = new Vector3(0f, -10.4f, 0.8f);

            // Discos laterales de articulación con tapas de latón
            CreateCylinder("Knee_Hinge_Disc_L", kneeJoint.transform, new Vector3(-2.6f, 0f, 0f), new Vector3(4.4f, 1.0f, 4.4f), Quaternion.Euler(0f, 0f, 90f), matDarkSteel);
            CreateCylinder("Knee_Pin_Cap_L", kneeJoint.transform, new Vector3(-3.16f, 0f, 0f), new Vector3(2.2f, 0.24f, 2.2f), Quaternion.Euler(0f, 0f, 90f), matBrass);
            CreateCylinder("Knee_Hinge_Disc_R", kneeJoint.transform, new Vector3(2.6f, 0f, 0f), new Vector3(4.4f, 1.0f, 4.4f), Quaternion.Euler(0f, 0f, 90f), matDarkSteel);
            CreateCylinder("Knee_Pin_Cap_R", kneeJoint.transform, new Vector3(3.16f, 0f, 0f), new Vector3(2.2f, 0.24f, 2.2f), Quaternion.Euler(0f, 0f, 90f), matBrass);
            // Rodillera blindada proyectada hacia adelante con cuña angulada
            CreateBlock("Knee_Armor_Guard", kneeJoint.transform, new Vector3(0f, 0.4f, 3.2f), new Vector3(6.0f, 4.4f, 3.6f), Quaternion.Euler(-22f, 0f, 0f), matMilGreen);
            CreateBlock("Knee_Center_Blade", kneeJoint.transform, new Vector3(0f, 0.4f, 4.9f), new Vector3(1.6f, 3.6f, 0.8f), Quaternion.Euler(-22f, 0f, 0f), matGunmetal);
            // Pistón hidráulico posterior de compresión
            CreateCylinder("Knee_Rear_Piston", kneeJoint.transform, new Vector3(0f, 0.6f, -2.4f), new Vector3(1.0f, 3.6f, 1.0f), Quaternion.identity, matChrome);

            // 3. Canilla / Tibia (Shin) con Maquinaria Expuesta (Estilo Foto 2)
            GameObject shin = new GameObject("Shin");
            shin.transform.parent = kneeJoint.transform;
            shin.transform.localPosition = Vector3.zero;

            // Hueso estructural interno
            CreateBlock("Shin_Bone_Core", shin.transform, new Vector3(0f, -4.8f, -0.4f), new Vector3(5.2f, 9.2f, 5.6f), Quaternion.identity, matDarkSteel);
            // Escudo frontal de canilla angulado con chevron táctico
            CreateBlock("Shin_Front_Armor", shin.transform, new Vector3(0f, -4.6f, 2.8f), new Vector3(5.6f, 8.8f, 1.8f), Quaternion.Euler(12f, 0f, 0f), matMilGreen);
            CreateBlock("Shin_Tactical_Chevron", shin.transform, new Vector3(0f, -4.2f, 3.76f), new Vector3(2.4f, 1.2f, 0.16f), Quaternion.Euler(12f, 0f, 0f), matDecalWhite);
            // Maquinaria expuesta en el lateral exterior (Cilindros apilados, válvulas y depósitos)
            CreateCylinder("Shin_Hydraulic_Stack_1", shin.transform, new Vector3(sideSign * 3.1f, -3.6f, 0f), new Vector3(0.76f, 5.2f, 0.76f), Quaternion.identity, matChrome);
            CreateCylinder("Shin_Hydraulic_Stack_2", shin.transform, new Vector3(sideSign * 3.1f, -3.6f, 1.0f), new Vector3(0.64f, 4.8f, 0.64f), Quaternion.identity, matChrome);
            CreateCylinder("Shin_Brass_Valve_A", shin.transform, new Vector3(sideSign * 3.2f, -1.4f, 1.2f), new Vector3(0.96f, 0.7f, 0.96f), Quaternion.identity, matBrass);
            CreateCylinder("Shin_Brass_Valve_B", shin.transform, new Vector3(sideSign * 3.2f, -5.8f, 1.2f), new Vector3(0.96f, 0.7f, 0.96f), Quaternion.identity, matBrass);
            CreateCylinder("Shin_Fluid_Canister", shin.transform, new Vector3(sideSign * 3.1f, -5.2f, -1.2f), new Vector3(1.1f, 3.6f, 1.1f), Quaternion.identity, matDarkSteel);
            CreateCylinder("Shin_Sprocket_Ring", shin.transform, new Vector3(sideSign * 3.2f, -4.0f, -0.2f), new Vector3(1.7f, 0.3f, 1.7f), Quaternion.Euler(0f, 0f, 90f), matChrome);
            // Carcasa de toberas de propulsión traseras en la pantorrilla
            CreateBlock("Shin_Rear_Thruster_Pod", shin.transform, new Vector3(0f, -4.0f, -3.6f), new Vector3(4.8f, 6.4f, 2.4f), Quaternion.identity, matGunmetal);
            CreateCylinder("Thruster_Nozzle_L", shin.transform, new Vector3(-1.2f, -6.4f, -3.6f), new Vector3(1.2f, 1.2f, 1.2f), Quaternion.Euler(-25f, 0f, 0f), matDarkSteel);
            CreateCylinder("Thruster_Nozzle_R", shin.transform, new Vector3(1.2f, -6.4f, -3.6f), new Vector3(1.2f, 1.2f, 1.2f), Quaternion.Euler(-25f, 0f, 0f), matDarkSteel);

            // 4. Tobillo (Ankle Joint) a -9.6m de la rodilla
            GameObject ankleJoint = new GameObject("Joint_Ankle");
            ankleJoint.transform.parent = kneeJoint.transform;
            ankleJoint.transform.localPosition = new Vector3(0f, -9.6f, -0.8f);

            // Rótula del tobillo y amortiguadores de balanceo
            CreateSphere("Ankle_Gimbal_Ball", ankleJoint.transform, Vector3.zero, Vector3.one * 2.8f, Quaternion.identity, matDarkSteel);
            CreateCylinder("Ankle_Shock_L", ankleJoint.transform, new Vector3(-2.6f, 0.8f, 0f), new Vector3(0.56f, 2.4f, 0.56f), Quaternion.identity, matChrome);
            CreateCylinder("Ankle_Shock_R", ankleJoint.transform, new Vector3(2.6f, 0.8f, 0f), new Vector3(0.56f, 2.4f, 0.56f), Quaternion.identity, matChrome);

            // 5. Pie Pesado 'Hazard Stomper' (Bota de combate blindada con garras y espolón)
            GameObject foot = new GameObject("Foot");
            foot.transform.parent = ankleJoint.transform;
            foot.transform.localPosition = Vector3.zero;

            // Base de la bota
            CreateBlock("Foot_Sole_Base", foot.transform, new Vector3(0f, -1.0f, 1.6f), new Vector3(6.8f, 2.0f, 11.2f), Quaternion.identity, matGunmetal);
            // Suela de tracción profunda
            CreateBlock("Foot_Tread_Sole", foot.transform, new Vector3(0f, -1.96f, 1.6f), new Vector3(7.0f, 0.4f, 11.6f), Quaternion.identity, matDarkSteel);
            // Doble puntera / garra articulada delantera con franjas hazard
            GameObject toeL = CreateBlock("Toe_Claw_L", foot.transform, new Vector3(-1.9f, -0.9f, 6.8f), new Vector3(2.6f, 1.8f, 3.6f), Quaternion.Euler(-14f, -6f, 0f), matMilGreen);
            CreateBlock("Toe_Cap_Hazard_L", toeL.transform, new Vector3(0f, 0f, 0.96f), new Vector3(2.04f, 0.7f, 0.5f), Quaternion.identity, matHazard);
            GameObject toeR = CreateBlock("Toe_Claw_R", foot.transform, new Vector3(1.9f, -0.9f, 6.8f), new Vector3(2.6f, 1.8f, 3.6f), Quaternion.Euler(-14f, 6f, 0f), matMilGreen);
            CreateBlock("Toe_Cap_Hazard_R", toeR.transform, new Vector3(0f, 0f, 0.96f), new Vector3(2.04f, 0.7f, 0.5f), Quaternion.identity, matHazard);
            // Espolón trasero de retroceso
            CreateBlock("Foot_Heel_Spur", foot.transform, new Vector3(0f, -0.6f, -3.6f), new Vector3(6.4f, 2.8f, 3.6f), Quaternion.Euler(20f, 0f, 0f), matMilGreen);

            // Pole Target para orientación frontal de la rodilla
            GameObject poleTarget = new GameObject("Pole_Target");
            poleTarget.transform.parent = legRoot.transform;
            poleTarget.transform.localPosition = new Vector3(0f, -10.0f, 12.0f);

            // Componente de cinemática inversa y locomoción predictiva
            ProceduralLeg pLeg = legRoot.AddComponent<ProceduralLeg>();
            SerializedObject pLegSO = new SerializedObject(pLeg);
            pLegSO.FindProperty("ikSolver.hip").objectReferenceValue = legRoot.transform;
            pLegSO.FindProperty("ikSolver.knee").objectReferenceValue = kneeJoint.transform;
            pLegSO.FindProperty("ikSolver.ankle").objectReferenceValue = ankleJoint.transform;
            pLegSO.FindProperty("ikSolver.poleTarget").objectReferenceValue = poleTarget.transform;
            pLegSO.FindProperty("bodyRoot").objectReferenceValue = mechaRoot.transform;
            pLegSO.FindProperty("bodyRigidbody").objectReferenceValue = mechaRoot.GetComponent<Rigidbody>();
            pLegSO.FindProperty("localRestOffset").vector3Value = new Vector3(xPos, 0f, 0f);

            pLegSO.FindProperty("stepDistance").floatValue = 6.0f;
            pLegSO.FindProperty("stepHeight").floatValue = 2.2f;
            pLegSO.FindProperty("maxClimbableStepHeight").floatValue = 4.8f;
            pLegSO.FindProperty("stepAnticipationDistance").floatValue = 6.0f;
            pLegSO.FindProperty("footToeForwardOffset").floatValue = 6.4f;
            pLegSO.FindProperty("footHalfWidth").floatValue = 3.6f;
            pLegSO.FindProperty("ankleToSoleOffset").floatValue = 1.6f;
            pLegSO.FindProperty("footRadius").floatValue = 1.3f;

            pLegSO.ApplyModifiedProperties();

            pLeg.SetLocalRestOffset(new Vector3(xPos, 0f, 0f));
            pLeg.IKSolver.Initialize();

            return pLeg;
        }

        ProceduralLeg legLeft = CreateMilitaryLeg("Leg_Left", -8.4f);
        ProceduralLeg legRight = CreateMilitaryLeg("Leg_Right", 8.4f);

        // Locomoción, estabilización de chasis y efectos de pisada
        MechLocomotionController locoCtrl = mechaRoot.AddComponent<MechLocomotionController>();
        SerializedObject locoSO = new SerializedObject(locoCtrl);
        locoSO.FindProperty("leftLeg").objectReferenceValue = legLeft;
        locoSO.FindProperty("rightLeg").objectReferenceValue = legRight;
        locoSO.FindProperty("mechaRigidbody").objectReferenceValue = mechaRoot.GetComponent<Rigidbody>();
        locoSO.ApplyModifiedProperties();

        MechBodyStabilizer stabCtrl = mechaRoot.AddComponent<MechBodyStabilizer>();
        SerializedObject stabSO = new SerializedObject(stabCtrl);
        stabSO.FindProperty("locomotionController").objectReferenceValue = locoCtrl;
        stabSO.FindProperty("mechaRigidbody").objectReferenceValue = mechaRoot.GetComponent<Rigidbody>();
        stabSO.FindProperty("chassisTransform").objectReferenceValue = pelvis.transform;
        stabSO.FindProperty("baseRideHeight").floatValue = 18.2f;
        stabSO.ApplyModifiedProperties();

        FootstepFX footFX = mechaRoot.AddComponent<FootstepFX>();
        SerializedObject fxSO = new SerializedObject(footFX);
        fxSO.FindProperty("leftLeg").objectReferenceValue = legLeft;
        fxSO.FindProperty("rightLeg").objectReferenceValue = legRight;
        fxSO.ApplyModifiedProperties();

        // 5. CABINA MULTINIVEL (3 INSTANCIAS VERTICALES CONECTADAS EXCLUSIVAMENTE POR ESCALERAS FÍSICAS)
        // Posicionada sobre la pelvis (Y = 21.0m, Z = 0.4m) alcanzando 50m de altura total erguido
        GameObject cockpit = new GameObject("Cockpit");
        cockpit.transform.parent = mechaRoot.transform;
        cockpit.transform.localPosition = new Vector3(0f, 21.0f, 0.4f);

        // --- HELPER PARA CONSTRUCCIÓN DE ESCALERAS INDUSTRIALES CON BARANDILLAS Y RAMPA SUAVE ---
        void CreateIndustrialStaircase(string name, Transform parent, Vector3 startPos, Vector3 endPos, float width, Material stepMat, Material hazardMat, Material railMat)
        {
            GameObject stairRoot = new GameObject(name);
            stairRoot.transform.parent = parent;
            stairRoot.transform.localPosition = Vector3.zero;

            Vector3 delta = endPos - startPos;
            Vector3 horizDelta = new Vector3(delta.x, 0f, delta.z);
            float horizDist = horizDelta.magnitude;
            float riseY = delta.y;
            float slopeLength = Mathf.Sqrt(horizDist * horizDist + riseY * riseY);
            Vector3 horizDir = horizDist > 0.001f ? horizDelta.normalized : Vector3.forward;
            float pitchAngle = Mathf.Atan2(riseY, horizDist) * Mathf.Rad2Deg;

            // Rotación exacta de la rampa orientada horizontalmente y cabeceada hacia arriba
            Quaternion rampRot = Quaternion.LookRotation(horizDir, Vector3.up) * Quaternion.Euler(-pitchAngle, 0f, 0f);

            int numSteps = 16;
            float stepH = riseY / numSteps;
            float stepD = (horizDist / numSteps) * 1.15f;

            for (int i = 0; i < numSteps; i++)
            {
                float t = (i + 0.5f) / numSteps;
                Vector3 sPos = Vector3.Lerp(startPos, endPos, t);
                sPos.y = startPos.y + (i + 0.5f) * stepH;

                // Peldaño base con colisionador propio
                GameObject step = CreateBlock($"Step_{i}", stairRoot.transform, sPos, new Vector3(width, stepH * 1.05f, stepD), Quaternion.identity, stepMat, true);
                // Franja hazard en la arista de impacto
                CreateBlock("Hazard_Nosing", step.transform, new Vector3(0f, 0.48f, 0.46f), new Vector3(0.98f, 0.12f, 0.12f), Quaternion.identity, hazardMat, false);
            }

            // Plataformas horizontales de aterrizaje/embarque en extremos para transición 100% suave
            Vector3 bottomLanding = startPos - horizDir * 0.75f + new Vector3(0f, -0.1f, 0f);
            CreateBlock("Stair_Bottom_Landing", stairRoot.transform, bottomLanding, new Vector3(width, 0.25f, 1.5f), Quaternion.identity, stepMat, true);

            Vector3 topLanding = endPos + horizDir * 0.75f + new Vector3(0f, -0.1f, 0f);
            CreateBlock("Stair_Top_Landing", stairRoot.transform, topLanding, new Vector3(width, 0.25f, 1.5f), Quaternion.identity, stepMat, true);

            // Barandillas tubulares de seguridad
            float halfW = width * 0.5f;
            Vector3 sideCross = Vector3.Cross(Vector3.up, horizDir).normalized;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 railOffset = sideCross * (side * (halfW + 0.08f));
                Vector3 rStart = startPos + railOffset + new Vector3(0f, 1.1f, 0f);
                Vector3 rEnd = endPos + railOffset + new Vector3(0f, 1.1f, 0f);
                Vector3 rMid = (rStart + rEnd) * 0.5f;

                CreateCylinder($"Handrail_{(side < 0 ? "L" : "R")}", stairRoot.transform, rMid, new Vector3(0.12f, slopeLength * 0.5f, 0.12f), rampRot * Quaternion.Euler(90f, 0f, 0f), railMat, false);

                for (int p = 0; p <= 4; p++)
                {
                    float t = p / 4.0f;
                    Vector3 postPos = Vector3.Lerp(rStart, rEnd, t) - new Vector3(0f, 0.55f, 0f);
                    CreateCylinder($"Post_{(side < 0 ? "L" : "R")}_{p}", stairRoot.transform, postPos, new Vector3(0.12f, 1.1f, 0.12f), Quaternion.identity, railMat, false);
                }
            }

            // COLISIONADOR DE RAMPA INVISIBLE GRUESO (1.2m) - IMPENETRABLE A 14 m/s
            Vector3 rampMid = (startPos + endPos) * 0.5f;
            GameObject rampColliderObj = new GameObject("Invisible_Ramp_Collider");
            rampColliderObj.transform.parent = stairRoot.transform;
            rampColliderObj.transform.localPosition = rampMid;
            rampColliderObj.transform.localRotation = rampRot;

            BoxCollider bc = rampColliderObj.AddComponent<BoxCollider>();
            bc.size = new Vector3(width, 1.2f, slopeLength + 0.6f);
            bc.center = new Vector3(0f, -0.52f, 0f);
        }

        // =====================================================================
        // PLANTA 1 (NIVEL 1 - PLANTA INFERIOR: CONDUCTOR / NAVEGACIÓN - Y = 0m)
        // Búnker Blindado con visión militar reducida (el conductor es guiado por el Capitán)
        // =====================================================================
        // Suelo continuo de Planta 1 ampliado (24.0m de ancho x 18.0m de fondo)
        CreateBlock("Floor_Level1", cockpit.transform, new Vector3(0f, -0.2f, 0f), new Vector3(24.0f, 0.4f, 18.0f), Quaternion.identity, matFloor, true);

        // Bordillos perimetrales y blindaje lateral de Planta 1 (Búnker Acorazado)
        CreateBlock("Rim_L1_L", cockpit.transform, new Vector3(-12.1f, 0.3f, 0f), new Vector3(0.4f, 0.6f, 18.0f), Quaternion.identity, matWall);
        CreateBlock("Rim_L1_R", cockpit.transform, new Vector3(12.1f, 0.3f, 0f), new Vector3(0.4f, 0.6f, 18.0f), Quaternion.identity, matWall);
        CreateBlock("Rim_L1_F", cockpit.transform, new Vector3(0f, 0.3f, 8.9f), new Vector3(24.0f, 0.6f, 0.4f), Quaternion.identity, matHazard);
        CreateBlock("Rim_L1_B", cockpit.transform, new Vector3(0f, 0.3f, -8.9f), new Vector3(24.0f, 0.6f, 0.4f), Quaternion.identity, matWall);

        // Muros acorazados de Planta 1 (Blindaje militar grueso)
        CreateBlock("Side_Armor_L1_L", cockpit.transform, new Vector3(-12.0f, 2.2f, 0f), new Vector3(0.4f, 4.0f, 18.0f), Quaternion.identity, matGunmetal);
        CreateBlock("Side_Armor_L1_R", cockpit.transform, new Vector3(12.0f, 2.2f, 0f), new Vector3(0.4f, 4.0f, 18.0f), Quaternion.identity, matGunmetal);
        CreateBlock("Rear_Armor_L1", cockpit.transform, new Vector3(0f, 2.2f, -8.9f), new Vector3(24.0f, 4.0f, 0.4f), Quaternion.identity, matMilGreenDark);

        // Fachada frontal de búnker con blindaje inferior a ras de visión y tronera militar estrecha
        CreateBlock("Armor_Glacis_L1_Bottom", cockpit.transform, new Vector3(0f, 0.35f, 8.9f), new Vector3(24.0f, 0.7f, 0.4f), Quaternion.identity, matMilGreen);
        CreateBlock("Armor_Glacis_L1_Top", cockpit.transform, new Vector3(0f, 2.95f, 8.9f), new Vector3(24.0f, 2.7f, 0.4f), Quaternion.identity, matMilGreen);

        // Tronera de observación militar estrecha centrada a nivel de ojos del pato piloto (Y = 1.15m)
        CreateBlock("Windshield_Level1", cockpit.transform, new Vector3(0f, 1.15f, 8.95f), new Vector3(16.0f, 0.9f, 0.12f), Quaternion.identity, matGlass);
        CreateBlock("Slit_Frame_Top", cockpit.transform, new Vector3(0f, 1.63f, 9.0f), new Vector3(16.2f, 0.12f, 0.25f), Quaternion.identity, matDarkSteel);
        CreateBlock("Slit_Frame_Bottom", cockpit.transform, new Vector3(0f, 0.67f, 9.0f), new Vector3(16.2f, 0.12f, 0.25f), Quaternion.identity, matDarkSteel);
        CreateBlock("Armor_Cheek_L1_L", cockpit.transform, new Vector3(-10.0f, 1.15f, 8.9f), new Vector3(4.0f, 0.9f, 0.4f), Quaternion.identity, matGunmetal);
        CreateBlock("Armor_Cheek_L1_R", cockpit.transform, new Vector3(10.0f, 1.15f, 8.9f), new Vector3(4.0f, 0.9f, 0.4f), Quaternion.identity, matGunmetal);

        // Aspilleras periscópicas laterales reforzadas
        CreateBlock("Slit_Side_L1_L", cockpit.transform, new Vector3(-12.0f, 1.15f, 2.0f), new Vector3(0.12f, 0.7f, 3.6f), Quaternion.identity, matGlass);
        CreateBlock("Slit_Side_L1_R", cockpit.transform, new Vector3(12.0f, 1.15f, 2.0f), new Vector3(0.12f, 0.7f, 3.6f), Quaternion.identity, matGlass);

        // Plataforma central de pilotaje y consola de navegación del Conductor
        GameObject pilotPlatform = CreateBlock("Station_Pilot_FloorPad", cockpit.transform, new Vector3(0f, 0.04f, 1.0f), new Vector3(13.0f, 0.08f, 9.5f), Quaternion.identity, matMilGreenDark);
        CreateBlock("Pilot_Station_Border", pilotPlatform.transform, new Vector3(0f, 0.15f, 0f), new Vector3(1.02f, 0.2f, 1.02f), Quaternion.identity, matHazard, false);

        GameObject pilotConsole = CreateBlock("Pilot_Console", cockpit.transform, new Vector3(0f, 0.45f, 7.2f), new Vector3(5.2f, 0.7f, 1.0f), Quaternion.identity, matGunmetal);
        CreateBlock("Pilot_Nav_Screen", cockpit.transform, new Vector3(0f, 0.92f, 7.25f), new Vector3(2.8f, 0.5f, 0.08f), Quaternion.Euler(-25f, 0f, 0f), matBtnGreen);

        GameObject pilotLabelObj = new GameObject("Pilot_Station_Label");
        pilotLabelObj.transform.parent = cockpit.transform;
        pilotLabelObj.transform.localPosition = new Vector3(0f, 0.08f, 6.4f);
        pilotLabelObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        pilotLabelObj.transform.localScale = Vector3.one * 0.22f;
        TextMesh ptm = pilotLabelObj.AddComponent<TextMesh>();
        ptm.text = "ESTACIÓN DE PILOTAJE [P1 - CONDUCTOR]\n(BÚNKER BLINDADO - GUIADO POR CAPITÁN)";
        ptm.fontSize = 28;
        ptm.characterSize = 0.28f;
        ptm.anchor = TextAnchor.MiddleCenter;
        ptm.alignment = TextAlignment.Center;
        ptm.fontStyle = FontStyle.Bold;
        ptm.color = Color.green;
        TextShaderUtility.ApplyZTesting(ptm);

        // TRAMO DE ESCALERAS 1 (Planta 1 -> Planta 2): Ubicado en ala estribor, sube de Y=0m a Y=4.5m
        CreateIndustrialStaircase("Stairs_Level1_to_Level2", cockpit.transform, new Vector3(8.5f, 0f, -5.0f), new Vector3(8.5f, 4.5f, 4.0f), 3.0f, matDarkSteel, matHazard, matGunmetal);

        // =====================================================================
        // PLANTA 2 (NIVEL 2 - PLANTA INTERMEDIA: ARTILLERO / ARSENAL - Y = 4.5m)
        // MÁXIMA VISIBILIDAD PANORÁMICA PARA PUNTERÍA Y DISPARO
        // =====================================================================
        // Suelo modular de Planta 2 (Totalmente sellado, sin agujeros al vacío)
        CreateBlock("Floor_Level2_Center", cockpit.transform, new Vector3(0f, 4.3f, 0f), new Vector3(14.0f, 0.4f, 18.0f), Quaternion.identity, matFloor, true);
        CreateBlock("Floor_Level2_LeftWing", cockpit.transform, new Vector3(-9.5f, 4.3f, 0f), new Vector3(5.0f, 0.4f, 18.0f), Quaternion.identity, matFloor, true);
        CreateBlock("Floor_Level2_RightFront", cockpit.transform, new Vector3(9.5f, 4.3f, 6.75f), new Vector3(5.0f, 0.4f, 4.5f), Quaternion.identity, matFloor, true);
        CreateBlock("Floor_Level2_RightRear", cockpit.transform, new Vector3(9.5f, 4.3f, -7.25f), new Vector3(5.0f, 0.4f, 3.5f), Quaternion.identity, matFloor, true);
        CreateBlock("Floor_Level2_RightOuter", cockpit.transform, new Vector3(11.0f, 4.3f, -0.5f), new Vector3(2.0f, 0.4f, 10.0f), Quaternion.identity, matFloor, true);

        // Barandilla de seguridad bordeando el hueco de escalera 1 en Planta 2
        CreateBlock("L2_Stair1_Safety_Rail", cockpit.transform, new Vector3(6.85f, 5.0f, -0.5f), new Vector3(0.12f, 1.0f, 9.5f), Quaternion.identity, matHazard);

        // Ventanales panorámicos de Planta 2 (Gran angular 96% transparente con visión total de armas y horizonte)
        CreateBlock("Windshield_Level2", cockpit.transform, new Vector3(0f, 6.8f, 8.9f), new Vector3(23.6f, 4.4f, 0.12f), Quaternion.identity, matGlass);
        CreateBlock("Side_Glass_L2_L", cockpit.transform, new Vector3(-12.0f, 6.8f, 0f), new Vector3(0.12f, 4.4f, 18.0f), Quaternion.identity, matGlass);
        CreateBlock("Side_Glass_L2_R", cockpit.transform, new Vector3(12.0f, 6.8f, 0f), new Vector3(0.12f, 4.4f, 18.0f), Quaternion.identity, matGlass);

        // HUD Holográfico sobre el cristal frontal de Planta 2 (Artillero)
        GameObject windshieldHudObj = new GameObject("Windshield_HUD");
        windshieldHudObj.transform.parent = cockpit.transform;
        windshieldHudObj.transform.localPosition = new Vector3(0f, 6.8f, 9.1f);
        windshieldHudObj.AddComponent<WindshieldHUDCrosshair>();

        // Consola de artillería al frente de Planta 2
        GameObject gunnerConsole = CreateBlock("Gunner_Console", cockpit.transform, new Vector3(0f, 4.95f, 7.2f), new Vector3(5.2f, 0.7f, 1.0f), Quaternion.identity, matDarkSteel);
        CreateBlock("Gunner_Ballistic_Screen", cockpit.transform, new Vector3(0f, 5.42f, 7.25f), new Vector3(2.8f, 0.5f, 0.08f), Quaternion.Euler(-25f, 0f, 0f), matBtnRose);

        GameObject gunnerLabelObj = new GameObject("Gunner_Station_Label");
        gunnerLabelObj.transform.parent = cockpit.transform;
        gunnerLabelObj.transform.localPosition = new Vector3(0f, 4.58f, 6.4f);
        gunnerLabelObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        gunnerLabelObj.transform.localScale = Vector3.one * 0.22f;
        TextMesh gtm = gunnerLabelObj.AddComponent<TextMesh>();
        gtm.text = "ESTACIÓN DE ARTILLERÍA [P2 - ARTILLERO]\n(VENTANALES PANORÁMICOS - PUNTERÍA TOTAL)";
        gtm.fontSize = 28;
        gtm.characterSize = 0.28f;
        gtm.anchor = TextAnchor.MiddleCenter;
        gtm.alignment = TextAlignment.Center;
        gtm.fontStyle = FontStyle.Bold;
        gtm.color = new Color(1.0f, 0.45f, 0.1f);
        TextShaderUtility.ApplyZTesting(gtm);

        // TRAMO DE ESCALERAS 2 (Planta 2 -> Planta 3): Ubicado en ala babor, sube de Y=4.5m a Y=9.0m
        CreateIndustrialStaircase("Stairs_Level2_to_Level3", cockpit.transform, new Vector3(-8.5f, 4.5f, 4.0f), new Vector3(-8.5f, 9.0f, -5.0f), 3.0f, matDarkSteel, matHazard, matGunmetal);

        // =====================================================================
        // PLANTA 3 (NIVEL 3 - PLANTA SUPERIOR: CABEZA DEL MECHA / CÁPSULA DEL CAPITÁN - Y = 9.0m)
        // El Capitán sube a la cabeza del mecha y no necesita ver, porque al entrar
        // pasa AUTOMÁTICAMENTE a vista exterior en 3ª persona del titán de 50 metros.
        // =====================================================================
        // Suelo modular de Planta 3 (Totalmente sellado, sin agujeros al vacío)
        CreateBlock("Floor_Level3_Center", cockpit.transform, new Vector3(0f, 8.8f, 0f), new Vector3(14.0f, 0.4f, 18.0f), Quaternion.identity, matFloor, true);
        CreateBlock("Floor_Level3_RightWing", cockpit.transform, new Vector3(9.5f, 8.8f, 0f), new Vector3(5.0f, 0.4f, 18.0f), Quaternion.identity, matFloor, true);
        CreateBlock("Floor_Level3_LeftFront", cockpit.transform, new Vector3(-9.5f, 8.8f, 6.75f), new Vector3(5.0f, 0.4f, 4.5f), Quaternion.identity, matFloor, true);
        CreateBlock("Floor_Level3_LeftRear", cockpit.transform, new Vector3(-9.5f, 8.8f, -7.25f), new Vector3(5.0f, 0.4f, 3.5f), Quaternion.identity, matFloor, true);
        CreateBlock("Floor_Level3_LeftOuter", cockpit.transform, new Vector3(-11.0f, 8.8f, -0.5f), new Vector3(2.0f, 0.4f, 10.0f), Quaternion.identity, matFloor, true);

        // Barandilla de seguridad protegiendo el hueco de la escalera 2 en Planta 3
        CreateBlock("L3_Stair2_Safety_Rail", cockpit.transform, new Vector3(-6.85f, 9.5f, -0.5f), new Vector3(0.12f, 1.0f, 9.5f), Quaternion.identity, matHazard);

        // ESTRUCTURA BLINDADA DE LA CABEZA DEL MECHA (Blindaje militar, sin necesidad de grandes cristales)
        CreateBlock("Head_Hull_Front", cockpit.transform, new Vector3(0f, 11.5f, 8.8f), new Vector3(23.6f, 5.2f, 0.4f), Quaternion.identity, matMilGreen);
        CreateBlock("Head_Hull_Roof", cockpit.transform, new Vector3(0f, 14.5f, 0f), new Vector3(23.6f, 0.4f, 17.6f), Quaternion.identity, matMilGreen);
        CreateBlock("Head_Hull_Side_L", cockpit.transform, new Vector3(-11.8f, 11.5f, 0f), new Vector3(0.4f, 5.2f, 17.6f), Quaternion.identity, matGunmetal);
        CreateBlock("Head_Hull_Side_R", cockpit.transform, new Vector3(11.8f, 11.5f, 0f), new Vector3(0.4f, 5.2f, 17.6f), Quaternion.identity, matGunmetal);
        CreateBlock("Head_Hull_Rear", cockpit.transform, new Vector3(0f, 11.5f, -8.8f), new Vector3(23.6f, 5.2f, 0.4f), Quaternion.identity, matMilGreenDark);

        // Arcos tubulares y nervaduras blindadas de la cabeza
        CreateBlock("Head_Rib_Front_Brow", cockpit.transform, new Vector3(0f, 14.4f, 8.8f), new Vector3(24.0f, 0.35f, 0.35f), Quaternion.identity, matDarkSteel);
        CreateBlock("Head_Rib_Mid_Hoop", cockpit.transform, new Vector3(0f, 14.6f, 0f), new Vector3(24.0f, 0.35f, 0.35f), Quaternion.identity, matDarkSteel);
        CreateBlock("Head_Rib_Rear_Hoop", cockpit.transform, new Vector3(0f, 14.4f, -8.8f), new Vector3(24.0f, 0.35f, 0.35f), Quaternion.identity, matDarkSteel);
        CreateBlock("Head_Pillar_L", cockpit.transform, new Vector3(-11.8f, 11.5f, 8.8f), new Vector3(0.4f, 5.2f, 0.4f), Quaternion.identity, matDarkSteel);
        CreateBlock("Head_Pillar_R", cockpit.transform, new Vector3(11.8f, 11.5f, 8.8f), new Vector3(0.4f, 5.2f, 0.4f), Quaternion.identity, matDarkSteel);

        // Anillo de telemetría neural holográfica en la corona de la cabeza
        CreateCylinder("Neural_Holo_Ring", cockpit.transform, new Vector3(0f, 13.8f, 1.5f), new Vector3(6.5f, 0.12f, 6.5f), Quaternion.identity, matCyanOptics);

        // PEDESTAL DE MANDO DEL CAPITÁN (En el centro de la Cabeza del Mecha)
        GameObject captainPedestal = CreateCylinder("Captain_Command_Pedestal", cockpit.transform, new Vector3(0f, 9.2f, 1.5f), new Vector3(4.4f, 0.4f, 4.4f), Quaternion.identity, matGunmetal, true);
        captainPedestal.AddComponent<CaptainCommandPedestal>();
        CreateCylinder("Pedestal_Hazard_Ring", captainPedestal.transform, new Vector3(0f, 0.52f, 0f), new Vector3(1.02f, 0.2f, 1.02f), Quaternion.identity, matHazard);
        CreateCylinder("Captain_Holo_Disc", captainPedestal.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 0.05f, 0.7f), Quaternion.identity, matCyanOptics);

        // Consola táctica de mando del Capitán (de perfil bajo Y < 0.6m para ergonomía de cabina)
        GameObject captainConsole = CreateBlock("Captain_Tactical_Console", cockpit.transform, new Vector3(0f, 9.45f, 4.5f), new Vector3(3.6f, 0.5f, 0.8f), Quaternion.identity, matDarkSteel);
        CreateBlock("Captain_Screen", captainConsole.transform, new Vector3(0f, 0.45f, 0.1f), new Vector3(2.4f, 0.4f, 0.08f), Quaternion.Euler(-20f, 0f, 0f), matCyanOptics);

        GameObject capLabelObj = new GameObject("Captain_Station_Label");
        capLabelObj.transform.parent = cockpit.transform;
        capLabelObj.transform.localPosition = new Vector3(0f, 9.08f, 5.5f);
        capLabelObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        capLabelObj.transform.localScale = Vector3.one * 0.22f;
        TextMesh ctm = capLabelObj.AddComponent<TextMesh>();
        ctm.text = "★ CABEZA DEL MECHA - PUENTE DE MANDO DEL CAPITÁN ★\n[ENTRA AL CÍRCULO MARCADO PARA 3ª PERSONA EXTERIOR]";
        ctm.fontSize = 28;
        ctm.characterSize = 0.28f;
        ctm.anchor = TextAnchor.MiddleCenter;
        ctm.alignment = TextAlignment.Center;
        ctm.fontStyle = FontStyle.Bold;
        ctm.color = new Color(0.0f, 0.95f, 1.0f);
        TextShaderUtility.ApplyZTesting(ctm);

        // --- AMBIENTACIÓN Y DECORACIÓN CON TEMÁTICA INDUSTRIAL (Zero External Assets) ---
        // 1. Columnas estructurales de mamparo en los 4 vértices del hangar (totalmente despejadas de las escaleras)
        float[] indColZ = new float[] { -8.2f, 8.2f };
        foreach (float z in indColZ)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 11.85f;
                CreateBlock($"Column_Main_{(side < 0 ? "L" : "R")}_{z:0.0}", cockpit.transform, new Vector3(x, 7.2f, z), new Vector3(0.4f, 14.8f, 0.7f), Quaternion.identity, matGunmetal);
                CreateBlock($"Column_Flange_{(side < 0 ? "L" : "R")}_{z:0.0}", cockpit.transform, new Vector3(x - side * 0.22f, 7.2f, z), new Vector3(0.1f, 14.8f, 0.9f), Quaternion.identity, matDarkSteel);
                for (int fl = 0; fl < 3; fl++)
                {
                    float yFl = fl * 4.5f;
                    CreateBlock($"Column_Hazard_Base_{(side < 0 ? "L" : "R")}_P{fl + 1}_{z:0.0}", cockpit.transform, new Vector3(x, yFl + 0.25f, z), new Vector3(0.5f, 0.5f, 0.8f), Quaternion.identity, matHazard);
                }
            }
        }

        // 2. Tuberías hidráulicas perimetrales dobles con bridas de unión y manómetros analógicos
        for (int fl = 0; fl < 3; fl++)
        {
            float yFl = fl * 4.5f;
            CreateCylinder($"Pipe_Chrome_L_P{fl + 1}", cockpit.transform, new Vector3(-11.6f, yFl + 2.1f, 0f), new Vector3(0.24f, 8.8f, 0.24f), Quaternion.Euler(90f, 0f, 0f), matChrome);
            CreateCylinder($"Pipe_Chrome_R_P{fl + 1}", cockpit.transform, new Vector3(11.6f, yFl + 2.1f, 0f), new Vector3(0.24f, 8.8f, 0.24f), Quaternion.Euler(90f, 0f, 0f), matChrome);
            CreateCylinder($"Pipe_Chrome_Rear_P{fl + 1}", cockpit.transform, new Vector3(0f, yFl + 2.1f, -8.6f), new Vector3(0.24f, 11.8f, 0.24f), Quaternion.Euler(0f, 0f, 90f), matChrome);

            CreateCylinder($"Pipe_Dark_L_P{fl + 1}", cockpit.transform, new Vector3(-11.6f, yFl + 1.6f, 0f), new Vector3(0.28f, 8.8f, 0.28f), Quaternion.Euler(90f, 0f, 0f), matGunmetal);
            CreateCylinder($"Pipe_Dark_R_P{fl + 1}", cockpit.transform, new Vector3(11.6f, yFl + 1.6f, 0f), new Vector3(0.28f, 8.8f, 0.28f), Quaternion.Euler(90f, 0f, 0f), matGunmetal);
            CreateCylinder($"Pipe_Dark_Rear_P{fl + 1}", cockpit.transform, new Vector3(0f, yFl + 1.6f, -8.6f), new Vector3(0.28f, 11.8f, 0.28f), Quaternion.Euler(0f, 0f, 90f), matGunmetal);

            float[] flangeZ = new float[] { -5.0f, 0.0f, 5.0f };
            foreach (float fz in flangeZ)
            {
                CreateCylinder($"Flange_L_P{fl + 1}_{fz}", cockpit.transform, new Vector3(-11.6f, yFl + 2.1f, fz), new Vector3(0.38f, 0.08f, 0.38f), Quaternion.Euler(90f, 0f, 0f), matHazard);
                CreateCylinder($"Flange_R_P{fl + 1}_{fz}", cockpit.transform, new Vector3(11.6f, yFl + 2.1f, fz), new Vector3(0.38f, 0.08f, 0.38f), Quaternion.Euler(90f, 0f, 0f), matHazard);
            }

            GameObject gaugeL = CreateCylinder($"Pressure_Gauge_L_P{fl + 1}", cockpit.transform, new Vector3(-11.45f, yFl + 2.6f, 1.5f), new Vector3(0.55f, 0.08f, 0.55f), Quaternion.Euler(0f, 0f, -90f), matBrass);
            CreateCylinder("Dial_Face", gaugeL.transform, new Vector3(0f, 0.52f, 0f), new Vector3(0.9f, 0.1f, 0.9f), Quaternion.identity, matDecalWhite);
            CreateBlock("Gauge_Needle", gaugeL.transform, new Vector3(0f, 0.65f, 0.18f), new Vector3(0.08f, 0.08f, 0.45f), Quaternion.identity, matBtnRose);

            GameObject gaugeR = CreateCylinder($"Pressure_Gauge_R_P{fl + 1}", cockpit.transform, new Vector3(11.45f, yFl + 2.6f, 1.5f), new Vector3(0.55f, 0.08f, 0.55f), Quaternion.Euler(0f, 0f, 90f), matBrass);
            CreateCylinder("Dial_Face", gaugeR.transform, new Vector3(0f, 0.52f, 0f), new Vector3(0.9f, 0.1f, 0.9f), Quaternion.identity, matDecalWhite);
            CreateBlock("Gauge_Needle", gaugeR.transform, new Vector3(0f, 0.65f, 0.18f), new Vector3(0.08f, 0.08f, 0.45f), Quaternion.identity, matBtnRose);
        }

        // 3. Celdas de acumuladores de energía en Popa de Planta 1 (Z = -8.2f)
        for (int b = -2; b <= 2; b++)
        {
            if (b == 0) continue;
            float bX = b * 3.4f;
            GameObject batt = CreateBlock($"Power_Cell_P1_{b}", cockpit.transform, new Vector3(bX, 1.4f, -8.2f), new Vector3(2.2f, 2.6f, 1.0f), Quaternion.identity, matGunmetal);
            CreateBlock("Louver_Vent", batt.transform, new Vector3(0f, 0.1f, 0.51f), new Vector3(0.85f, 0.65f, 0.06f), Quaternion.identity, matDarkSteel);
            CreateSphere("Status_LED", batt.transform, new Vector3(0.35f, 0.42f, 0.52f), Vector3.one * 0.18f, Quaternion.identity, matBtnAmber);
        }

        // 4. Tanques criogénicos de refrigerante para artillería en Popa de Planta 2 (Z = -8.2f)
        for (int t = -1; t <= 1; t += 2)
        {
            float tX = t * 4.5f;
            GameObject tank = CreateCylinder($"Coolant_Tank_P2_{t}", cockpit.transform, new Vector3(tX, 6.2f, -8.1f), new Vector3(1.8f, 1.6f, 1.8f), Quaternion.identity, matChrome);
            CreateCylinder("Tank_Rim_Top", tank.transform, new Vector3(0f, 0.95f, 0f), new Vector3(1.05f, 0.1f, 1.05f), Quaternion.identity, matHazard);
            CreateCylinder("Tank_Rim_Bot", tank.transform, new Vector3(0f, -0.95f, 0f), new Vector3(1.05f, 0.1f, 1.05f), Quaternion.identity, matHazard);
            CreateCylinder("Sight_Glass", tank.transform, new Vector3(0f, 0f, 0.95f), new Vector3(0.12f, 0.7f, 0.12f), Quaternion.identity, matCyanOptics);
        }

        // 5. Conductos superiores de ventilación industrial en techos (P1 y P2)
        for (int fl = 0; fl < 2; fl++)
        {
            float yFl = fl * 4.5f;
            CreateBlock($"Vent_Duct_Center_P{fl + 1}", cockpit.transform, new Vector3(0f, yFl + 4.15f, 0f), new Vector3(1.6f, 0.4f, 16.0f), Quaternion.identity, matDarkSteel);
            CreateBlock($"Vent_Grille_Front_P{fl + 1}", cockpit.transform, new Vector3(0f, yFl + 3.92f, 5.0f), new Vector3(1.2f, 0.06f, 1.8f), Quaternion.identity, matGunmetal);
            CreateBlock($"Vent_Grille_Mid_P{fl + 1}", cockpit.transform, new Vector3(0f, yFl + 3.92f, 0.0f), new Vector3(1.2f, 0.06f, 1.8f), Quaternion.identity, matGunmetal);
            CreateBlock($"Vent_Grille_Rear_P{fl + 1}", cockpit.transform, new Vector3(0f, yFl + 3.92f, -5.0f), new Vector3(1.2f, 0.06f, 1.8f), Quaternion.identity, matGunmetal);
        }

        // 6. Líneas Hazard de señalización en suelo: guían el flujo cómodo de paso hacia las escaleras
        CreateBlock("Walkway_Line_P1_A", cockpit.transform, new Vector3(5.6f, 0.01f, -0.5f), new Vector3(0.18f, 0.02f, 9.0f), Quaternion.identity, matHazard);
        CreateBlock("Walkway_Line_P1_B", cockpit.transform, new Vector3(6.8f, 0.01f, -0.5f), new Vector3(0.18f, 0.02f, 9.0f), Quaternion.identity, matHazard);

        CreateBlock("Walkway_Avenue_P2_Front", cockpit.transform, new Vector3(0f, 4.51f, 3.8f), new Vector3(14.0f, 0.02f, 0.18f), Quaternion.identity, matHazard);
        CreateBlock("Walkway_Avenue_P2_Rear", cockpit.transform, new Vector3(0f, 4.51f, 2.4f), new Vector3(14.0f, 0.02f, 0.18f), Quaternion.identity, matHazard);

        // Iluminación táctica suave en cada nivel
        void AddCockpitDownlight(string lName, Vector3 localPos, Color color, float range = 18f, float intensity = 2.0f)
        {
            GameObject lObj = new GameObject(lName);
            lObj.transform.parent = cockpit.transform;
            lObj.transform.localPosition = localPos;
            Light l = lObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = color;
        }
        AddCockpitDownlight("Level1_Pilot_Light", new Vector3(0f, 3.8f, 1.0f), new Color(0.85f, 0.95f, 1.0f), 18f, 1.8f);
        AddCockpitDownlight("Level2_Gunner_Light", new Vector3(0f, 8.2f, 1.8f), new Color(1.0f, 0.92f, 0.85f), 18f, 1.8f);
        AddCockpitDownlight("Level3_Captain_Light", new Vector3(0f, 14.0f, 0f), new Color(0.9f, 0.95f, 1.0f), 22f, 2.2f);

        // --- ENVOLVENTE EXTERIOR DEL TORSO Y BLINDAJE (Posicionados estrictamente en el exterior X = +-13.8m sin penetrar cabina ni escaleras) ---
        CreateBlock("Pectoral_Armor_L", cockpit.transform, new Vector3(-13.8f, 5.2f, 6.4f), new Vector3(3.2f, 8.4f, 5.2f), Quaternion.Euler(-18f, 8f, 0f), matMilGreen);
        CreateBlock("Pectoral_Intake_L", cockpit.transform, new Vector3(-13.8f, 5.2f, 9.2f), new Vector3(2.2f, 2.8f, 0.4f), Quaternion.Euler(-18f, 8f, 0f), matGunmetal);
        CreateBlock("Pectoral_Armor_R", cockpit.transform, new Vector3(13.8f, 5.2f, 6.4f), new Vector3(3.2f, 8.4f, 5.2f), Quaternion.Euler(-18f, -8f, 0f), matMilGreen);
        CreateBlock("Pectoral_Intake_R", cockpit.transform, new Vector3(13.8f, 5.2f, 9.2f), new Vector3(2.2f, 2.8f, 0.4f), Quaternion.Euler(-18f, -8f, 0f), matGunmetal);

        // MOCHILA POSTERIOR Y TOBERAS DE ESCAPE (Backpack Thrusters enrasadas tras pared Z = -8.9m)
        CreateBlock("Backpack_Main_Bay", cockpit.transform, new Vector3(0f, 8.0f, -11.4f), new Vector3(21.2f, 14.5f, 4.6f), Quaternion.identity, matMilGreenDark);
        CreateBlock("Backpack_Thruster_Nacelle_L", cockpit.transform, new Vector3(-6.8f, 9.0f, -13.4f), new Vector3(4.4f, 15.0f, 4.4f), Quaternion.identity, matMilGreen);
        CreateCylinder("Backpack_Exhaust_Nozzle_L", cockpit.transform, new Vector3(-6.8f, 0.8f, -13.4f), new Vector3(3.2f, 2.2f, 3.2f), Quaternion.identity, matChrome);
        CreateBlock("Backpack_Thruster_Nacelle_R", cockpit.transform, new Vector3(6.8f, 9.0f, -13.4f), new Vector3(4.4f, 15.0f, 4.4f), Quaternion.identity, matMilGreen);
        CreateCylinder("Backpack_Exhaust_Nozzle_R", cockpit.transform, new Vector3(6.8f, 0.8f, -13.4f), new Vector3(3.2f, 2.2f, 3.2f), Quaternion.identity, matChrome);

        // 6. CABEZA Y CASCO DE COMBATE (Mecha Head con Visor Cian y Antena V-Fin a 50m de altura)
        GameObject mechaHead = new GameObject("Mecha_Head");
        mechaHead.transform.parent = cockpit.transform;
        mechaHead.transform.localPosition = new Vector3(0f, 15.2f, 4.0f);

        CreateCylinder("Head_Neck_Joint", mechaHead.transform, new Vector3(0f, 0.8f, 0f), new Vector3(3.6f, 1.6f, 3.6f), Quaternion.identity, matDarkSteel);
        CreateBlock("Head_Chin_Guard", mechaHead.transform, new Vector3(0f, 2.0f, 2.2f), new Vector3(2.8f, 1.8f, 2.4f), Quaternion.Euler(-22f, 0f, 0f), matGunmetal);
        CreateBlock("Head_Cheek_L", mechaHead.transform, new Vector3(-2.2f, 2.8f, 1.4f), new Vector3(1.0f, 2.8f, 2.4f), Quaternion.Euler(0f, -15f, 0f), matMilGreen);
        CreateBlock("Head_Cheek_R", mechaHead.transform, new Vector3(2.2f, 2.8f, 1.4f), new Vector3(1.0f, 2.8f, 2.4f), Quaternion.Euler(0f, 15f, 0f), matMilGreen);
        CreateBlock("Head_Visor_Optics", mechaHead.transform, new Vector3(0f, 3.1f, 2.4f), new Vector3(3.2f, 0.7f, 0.5f), Quaternion.identity, matCyanOptics);
        CreateBlock("Head_Forehead_Brow", mechaHead.transform, new Vector3(0f, 4.0f, 2.0f), new Vector3(4.0f, 1.4f, 2.8f), Quaternion.identity, matMilGreen);
        CreateBlock("Head_VFin_Antenna", mechaHead.transform, new Vector3(0f, 5.6f, 1.6f), new Vector3(0.7f, 3.8f, 1.6f), Quaternion.Euler(-18f, 0f, 0f), matBrass);
        CreateBlock("Head_Helmet_Dome", mechaHead.transform, new Vector3(0f, 4.0f, -0.2f), new Vector3(4.8f, 3.2f, 5.2f), Quaternion.identity, matMilGreen);
        CreateCylinder("Head_Ear_Horn_L", mechaHead.transform, new Vector3(-2.8f, 3.6f, -0.4f), new Vector3(0.8f, 2.4f, 0.8f), Quaternion.Euler(-25f, 0f, 0f), matGunmetal);
        CreateCylinder("Head_Ear_Horn_R", mechaHead.transform, new Vector3(2.8f, 3.6f, -0.4f), new Vector3(0.8f, 2.4f, 0.8f), Quaternion.Euler(-25f, 0f, 0f), matGunmetal);

        // 7. HOMBROS, PAULDRONS Y ARSENAL MILITAR
        // --- HOMBRO IZQUIERDO Y BRAZO GATLING ---
        GameObject shoulderL = new GameObject("Shoulder_Left");
        shoulderL.transform.parent = cockpit.transform;
        shoulderL.transform.localPosition = new Vector3(-15.6f, 10.0f, 1.0f);

        CreateSphere("Shoulder_Joint_L", shoulderL.transform, Vector3.zero, Vector3.one * 4.4f, Quaternion.identity, matDarkSteel);
        CreateBlock("Pauldron_L", shoulderL.transform, new Vector3(-1.2f, 1.8f, 0f), new Vector3(6.8f, 4.8f, 8.4f), Quaternion.Euler(0f, 0f, 15f), matMilGreen);
        CreateBlock("Pauldron_Trim_L", shoulderL.transform, new Vector3(-1.2f, -0.6f, 0f), new Vector3(7.0f, 1.2f, 8.6f), Quaternion.Euler(0f, 0f, 15f), matGunmetal);
        CreateBlock("Pauldron_Decal_L", shoulderL.transform, new Vector3(-1.2f, 4.24f, 0f), new Vector3(3.6f, 0.16f, 3.6f), Quaternion.Euler(0f, 0f, 15f), matDecalWhite);
        CreateBlock("Pauldron_Side_Vent_L", shoulderL.transform, new Vector3(-4.2f, 1.2f, 0f), new Vector3(0.8f, 2.4f, 2.4f), Quaternion.identity, matDarkSteel);

        // Brazo izquierdo (Ametralladora Rotatoria Gatling)
        GameObject armLeft = new GameObject("Arm_Left_Gatling");
        armLeft.transform.parent = cockpit.transform;
        armLeft.transform.localPosition = new Vector3(-16.8f, 7.6f, 1.0f);

        CreateCylinder("Bicep_Core_L", armLeft.transform, new Vector3(0f, -3.6f, 0f), new Vector3(3.6f, 4.8f, 3.6f), Quaternion.identity, matDarkSteel);
        CreateCylinder("Bicep_Piston_L", armLeft.transform, new Vector3(1.4f, -3.6f, 0.8f), new Vector3(0.8f, 4.4f, 0.8f), Quaternion.identity, matChrome);
        CreateBlock("Bicep_Armor_L", armLeft.transform, new Vector3(-1.6f, -3.6f, 0f), new Vector3(1.2f, 4.4f, 4.0f), Quaternion.identity, matMilGreen);
        CreateCylinder("Elbow_Hinge_L", armLeft.transform, new Vector3(0f, -6.4f, 0f), new Vector3(4.4f, 1.2f, 4.4f), Quaternion.Euler(0f, 0f, 90f), matDarkSteel);
        CreateBlock("Forearm_Gauntlet_L", armLeft.transform, new Vector3(0f, -8.4f, 2.8f), new Vector3(5.2f, 4.8f, 8.8f), Quaternion.identity, matMilGreen);

        // Tambor de munición inferior masivo con franjas hazard
        GameObject drum = CreateCylinder("Gatling_Ammo_Drum", armLeft.transform, new Vector3(0f, -11.6f, 2.8f), new Vector3(6.4f, 2.8f, 6.4f), Quaternion.identity, matGunmetal);
        CreateCylinder("Drum_Band", drum.transform, Vector3.zero, new Vector3(1.02f, 0.28f, 1.02f), Quaternion.identity, matHazard);
        CreateBlock("Ammo_Feed_Chute", armLeft.transform, new Vector3(2.8f, -8.4f, 1.0f), new Vector3(3.6f, 2.4f, 3.2f), Quaternion.identity, matGunmetal);

        // Rotor y conjunto de 6 cañones cromados rotatorios
        GameObject rotorObj = new GameObject("Gatling_Rotor");
        rotorObj.transform.parent = armLeft.transform;
        rotorObj.transform.localPosition = new Vector3(0f, -8.4f, 8.8f);

        CreateCylinder("Spindle", rotorObj.transform, new Vector3(0f, 0f, 3.0f), new Vector3(2.2f, 6.0f, 2.2f), Quaternion.Euler(90f, 0f, 0f), matGunmetal);
        CreateCylinder("Barrel_Ring", rotorObj.transform, new Vector3(0f, 0f, 7.2f), new Vector3(4.8f, 0.44f, 4.8f), Quaternion.Euler(90f, 0f, 0f), matGunmetal);

        float barrelRadius = 1.7f;
        for (int i = 0; i < 6; i++)
        {
            float ang = i * 60f * Mathf.Deg2Rad;
            Vector3 bLocalPos = new Vector3(Mathf.Cos(ang) * barrelRadius, Mathf.Sin(ang) * barrelRadius, 4.4f);
            CreateCylinder($"Barrel_{i}", rotorObj.transform, bLocalPos, new Vector3(0.64f, 8.4f, 0.64f), Quaternion.Euler(90f, 0f, 0f), matChrome);
        }

        GameObject muzzleObj = new GameObject("Gatling_Muzzle");
        muzzleObj.transform.parent = rotorObj.transform;
        muzzleObj.transform.localPosition = new Vector3(0f, 0f, 10.8f);

        GameObject muzzleLightObj = new GameObject("Muzzle_Light");
        muzzleLightObj.transform.parent = muzzleObj.transform;
        muzzleLightObj.transform.localPosition = Vector3.zero;
        Light ml = muzzleLightObj.AddComponent<Light>();
        ml.type = LightType.Point;
        ml.color = new Color(1.0f, 0.65f, 0.15f);
        ml.range = 32f;
        ml.intensity = 5.5f;
        ml.enabled = false;

        // --- HOMBRO DERECHO, POD LANZAMISILES DE 6 TUBOS Y BRAZO MANIPULADOR ---
        GameObject shoulderR = new GameObject("Shoulder_Right");
        shoulderR.transform.parent = cockpit.transform;
        shoulderR.transform.localPosition = new Vector3(15.6f, 10.0f, 1.0f);

        CreateSphere("Shoulder_Joint_R", shoulderR.transform, Vector3.zero, Vector3.one * 4.4f, Quaternion.identity, matDarkSteel);
        CreateBlock("Pauldron_R", shoulderR.transform, new Vector3(1.2f, 1.8f, 0f), new Vector3(6.8f, 4.8f, 8.4f), Quaternion.Euler(0f, 0f, -15f), matMilGreen);
        CreateBlock("Pauldron_Trim_R", shoulderR.transform, new Vector3(1.2f, -0.6f, 0f), new Vector3(7.0f, 1.2f, 8.6f), Quaternion.Euler(0f, 0f, -15f), matGunmetal);

        // POD LANZAMISILES DE 6 TUBOS
        GameObject podBox = CreateBlock("Missile_Pod", shoulderR.transform, new Vector3(0f, 4.4f, 0.8f), new Vector3(7.6f, 5.2f, 9.6f), Quaternion.identity, matMilGreen);
        CreateBlock("Pod_Bezel", podBox.transform, new Vector3(0f, 0f, 0.51f), new Vector3(1.02f, 1.02f, 0.08f), Quaternion.identity, matGunmetal);
        CreateBlock("Pod_Optics_Sensor", podBox.transform, new Vector3(0f, 0.56f, 0.1f), new Vector3(0.45f, 0.25f, 0.6f), Quaternion.identity, matCyanOptics);

        // 6 tubos con cabezas ojivales cian luminosas (2 filas de 3)
        Transform[] tubes = new Transform[4];
        int tubeIdx = 0;
        for (int row = 0; row < 2; row++)
        {
            float yOff = row == 0 ? 0.55f : -0.55f;
            for (int tubeCol = -1; tubeCol <= 1; tubeCol++)
            {
                float xOff = tubeCol * 1.05f;
                GameObject tube = CreateCylinder($"Tube_{row}_{tubeCol}", podBox.transform, new Vector3(xOff / 3.8f, yOff / 2.6f, 0.42f), new Vector3(0.24f, 0.18f, 0.24f), Quaternion.Euler(90f, 0f, 0f), matGunmetal);
                CreateSphere("Warhead_Cyan_Lens", tube.transform, new Vector3(0f, 0.65f, 0f), Vector3.one * 0.85f, Quaternion.identity, matCyanOptics);
                if (tubeIdx < 4)
                {
                    tubes[tubeIdx++] = tube.transform;
                }
            }
        }

        // Brazo derecho (Manipulador de combate con mano robótica articulada)
        GameObject armRight = new GameObject("Arm_Right_MissilePod");
        armRight.transform.parent = cockpit.transform;
        armRight.transform.localPosition = new Vector3(16.8f, 7.6f, 1.0f);

        CreateCylinder("Bicep_Core_R", armRight.transform, new Vector3(0f, -3.6f, 0f), new Vector3(3.6f, 4.8f, 3.6f), Quaternion.identity, matDarkSteel);
        CreateCylinder("Bicep_Piston_R", armRight.transform, new Vector3(-1.4f, -3.6f, 0.8f), new Vector3(0.8f, 4.4f, 0.8f), Quaternion.identity, matChrome);
        CreateBlock("Bicep_Armor_R", armRight.transform, new Vector3(1.6f, -3.6f, 0f), new Vector3(1.2f, 4.4f, 4.0f), Quaternion.identity, matMilGreen);
        CreateCylinder("Elbow_Hinge_R", armRight.transform, new Vector3(0f, -6.4f, 0f), new Vector3(4.4f, 1.2f, 4.4f), Quaternion.Euler(0f, 0f, 90f), matDarkSteel);
        CreateBlock("Forearm_Gauntlet_R", armRight.transform, new Vector3(0f, -8.4f, 2.8f), new Vector3(5.2f, 4.8f, 8.8f), Quaternion.identity, matMilGreen);

        // Mano robótica de combate articulada
        GameObject hand = CreateBlock("Mecha_Hand_Palm", armRight.transform, new Vector3(0f, -8.4f, 8.0f), new Vector3(3.6f, 2.8f, 3.2f), Quaternion.identity, matDarkSteel);
        for (int f = -1; f <= 2; f++)
        {
            float fX = (f - 0.5f) * 0.76f;
            GameObject finger = CreateBlock($"Finger_{f}", hand.transform, new Vector3(fX, -0.2f, 1.8f), new Vector3(0.64f, 0.8f, 2.2f), Quaternion.Euler(15f, 0f, 0f), matGunmetal);
            CreateSphere("Knuckle", finger.transform, new Vector3(0f, 0.3f, -0.8f), Vector3.one * 0.76f, Quaternion.identity, matChrome);
        }
        CreateBlock("Thumb", hand.transform, new Vector3(-1.7f, -0.6f, 0.8f), new Vector3(0.7f, 0.8f, 1.6f), Quaternion.Euler(0f, -35f, 20f), matGunmetal);

        // 8. PUESTO DE ARTILLERÍA: La planta 2 queda diáfana y despejada frente a los ventanales panorámicos y el HUD.


        // 9. BOTONES FÍSICOS DISTRIBUIDOS VERTICALMENTE EN LAS 3 PLANTAS
        void CreateFloorButton(string id, string label, Vector3 localPos, float radius, Material capMat, CockpitFloorButton.ButtonType bType, float detectionRadius = 1.6f)
        {
            GameObject bObj = new GameObject($"Btn_{id}");
            bObj.transform.parent = cockpit.transform;
            bObj.transform.localPosition = localPos;

            GameObject bHousing = CreateCylinder("Housing", bObj.transform, Vector3.zero, new Vector3(radius * 2.3f, 0.22f, radius * 2.3f), Quaternion.identity, matBtnBase);
            CreateCylinder("RimRing", bHousing.transform, new Vector3(0f, 0.55f, 0f), new Vector3(1.02f, 0.2f, 1.02f), Quaternion.identity, matHazard);

            GameObject bCap = CreateCylinder("Cap", bObj.transform, new Vector3(0f, 0.28f, 0f), new Vector3(radius * 2.0f, 0.28f, radius * 2.0f), Quaternion.identity, capMat);

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.parent = bCap.transform;
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

        // --- BOTONES DE PLANTA 1 (NIVEL 1: NAVEGACIÓN Y MARCHA - CONDUCTOR) ---
        // Rombo ergonómico espacioso dejando un pasillo libre de más de 4m hacia la escalera de estribor
        CreateFloorButton("FWD", "▲ AVANZAR", new Vector3(0.0f, 0f, 4.2f), 1.35f, matBtnGreen, CockpitFloorButton.ButtonType.Hold, 1.7f);
        CreateFloorButton("BWD", "▼ ATRÁS", new Vector3(0.0f, 0f, -2.2f), 1.35f, matBtnAmber, CockpitFloorButton.ButtonType.Hold, 1.7f);
        CreateFloorButton("LEFT", "◄ GIRO IZQ", new Vector3(-4.2f, 0f, 1.0f), 1.30f, matBtnCyan, CockpitFloorButton.ButtonType.Hold, 1.6f);
        CreateFloorButton("RIGHT", "GIRO DER ►", new Vector3(4.2f, 0f, 1.0f), 1.30f, matBtnCyan, CockpitFloorButton.ButtonType.Hold, 1.6f);

        // --- BOTONES DE PLANTA 2 (NIVEL 2: ARTILLERÍA Y ARSENAL) ---
        // Gran avenida transversal central libre (Z entre 2.4 y 3.8); Aim Pad al frente y armas en X = +-4.2m dejando paso libre a escaleras
        CreateFloorButton("GATLING", "🔫 GATLING\n[MANTENER / E]", new Vector3(-4.2f, 4.5f, 1.6f), 1.35f, matBtnAmber, CockpitFloorButton.ButtonType.Hold, 1.65f);
        CreateFloorButton("MISSILES", "🚀 MISILES\n(CULAZO E)", new Vector3(4.2f, 4.5f, 1.6f), 1.35f, matBtnCyan, CockpitFloorButton.ButtonType.Stomp, 1.65f);
        SetupGunnerAimPad.BuildAimPadHierarchy(cockpit.transform, new Vector3(0.0f, 4.54f, 5.2f), matDarkSteel, matGunmetal, matChrome, matHazard, matBtnBase, matBtnCyan, matBtnGreen, matBtnAmber);

        // --- BOTÓN DE PLANTA 3 (NIVEL 3: PUENTE DE MANDO DEL CAPITÁN) ---
        CreateFloorButton("SPAWN", "🚨 ROBOTS\n[ACTIVAR]", new Vector3(0.0f, 9.0f, -3.2f), 1.30f, matBtnRose, CockpitFloorButton.ButtonType.Toggle, 1.7f);

        // 10. EL PATO PILOTO DENTRO DE LA CABINA (Spawnea en Planta 1 - Estación del Conductor)
        GameObject duck = new GameObject("Duck");
        duck.transform.parent = cockpit.transform;
        duck.transform.localPosition = new Vector3(0.0f, 0.55f, 0.65f);
        duck.tag = "Player";

        Rigidbody rb = duck.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        CapsuleCollider duckCollider = duck.AddComponent<CapsuleCollider>();
        duckCollider.center = new Vector3(0f, 0.45f, 0f);
        duckCollider.radius = 0.45f;
        duckCollider.height = 1.0f;
        duckCollider.isTrigger = true;

        GameObject visual = new GameObject("Visual");
        visual.transform.parent = duck.transform;
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
        duckSO.FindProperty("jumpForce").floatValue = 10.5f;
        duckSO.ApplyModifiedProperties();
        duckSO.FindProperty("moveSpeed").floatValue = 14.0f;
        duckSO.FindProperty("jumpForce").floatValue = 10.5f;
        duckSO.ApplyModifiedProperties();

        // 11. CONECTAR REFERENCIAS EN MECHACONTROLLER
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

        // 12. GENERADOR DE ENEMIGOS
        GameObject spawnerObj = new GameObject("EnemySpawner");
        EnemySpawner spawnerComp = spawnerObj.AddComponent<EnemySpawner>();
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/EnemyBot.prefab");
        if (enemyPrefab != null)
        {
            SerializedObject spawnerSO = new SerializedObject(spawnerComp);
            spawnerSO.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
            spawnerSO.ApplyModifiedProperties();
        }

        // 13. CÁMARA ORBITAL Y LIBRE
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
            camSO.FindProperty("currentDistance").floatValue = 7.5f;
            camSO.FindProperty("targetDistance").floatValue = 7.5f;
            camSO.ApplyModifiedProperties();

            // Alinear en modo edición detrás de la cabina colosal
            Vector3 camPos = mechaRoot.transform.position + new Vector3(0f, 36.0f, -14.0f);
            Vector3 lookTarget = mechaRoot.transform.position + new Vector3(0f, 30.0f, 6.0f);
            mainCam.transform.position = camPos;
            mainCam.transform.rotation = Quaternion.LookRotation(lookTarget - camPos);
        }

        // 14. HUD COMPLETO CON UI TOOLKIT (UIDocument + UXML + USS)
        GameObject hudObj = new GameObject("HUD");
        UIDocument uiDoc = hudObj.AddComponent<UIDocument>();
        uiDoc.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/MechaPanelSettings.asset");
        uiDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/MechaHUD.uxml");
        MechaHUD hud = hudObj.AddComponent<MechaHUD>();

        var fontAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.FontAsset>("Assets/UI/MechaFontAsset.asset");
        SerializedObject hudSO = new SerializedObject(hud);
        hudSO.FindProperty("hudFontAsset").objectReferenceValue = fontAsset;
        hudSO.ApplyModifiedProperties();

        // 15. AUTO PLAY TESTER
        GameObject testerObj = new GameObject("AutoPlayTester");
        testerObj.AddComponent<AutoPlayTester>();

        // Guardar Mecha Titán como Prefab estándar de Unity
        string mechaPrefabPath = "Assets/Prefabs/Mecha/MechaTitan.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(mechaRoot, mechaPrefabPath, InteractionMode.AutomatedAction);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("🦆 ¡Mecha Titán Militar de Élite (~52m, Escala x2, Cúpula Panorámica Ultra-Transparente y Prefabs Estándar de Unity) ensamblado con éxito!");
    }
}
