using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Instala y configura la Plataforma Giroscópica de Puntería (Gunner Aiming Pad)
/// en el Ala de Artillería (P2) de la cabina de MechaQuack.
/// Sincroniza las referencias en MechaController y actualiza los prefabs estándar.
/// </summary>
public static class SetupGunnerAimPad
{
    [MenuItem("Mecha/Install Gunner Aiming Pad in Cockpit")]
    public static void InstallGunnerAimPad()
    {
        var mechaObj = GameObject.Find("Mecha_Root");
        if (mechaObj == null)
        {
            Debug.LogError("[AIM SETUP] No se encontró Mecha_Root en la escena abierta.");
            return;
        }

        Transform cockpit = mechaObj.transform.Find("Cockpit");
        if (cockpit == null)
        {
            Debug.LogError("[AIM SETUP] No se encontró Cockpit bajo Mecha_Root.");
            return;
        }

        MechaController mecha = mechaObj.GetComponent<MechaController>();
        if (mecha == null)
        {
            Debug.LogError("[AIM SETUP] No se encontró MechaController en Mecha_Root.");
            return;
        }

        // Cargar materiales URP
        Material matDarkSteel = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_MechaDarkSteel.mat");
        Material matGunmetal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_MechaGunmetal.mat");
        Material matChrome = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_MechaChrome.mat");
        Material matHazard = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_HazardStripe.mat");
        Material matBtnBase = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_BtnBase.mat");
        Material matBtnCyan = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_BtnCyan.mat");
        Material matBtnGreen = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_BtnGreen.mat");
        Material matBtnAmber = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_BtnAmber.mat");

        // Destruir versión previa si existía para reinstalar limpiamente
        Transform oldAimPad = cockpit.Find("Btn_AIM_PAD") ?? cockpit.Find("Cockpit_Aim_Pad");
        if (oldAimPad != null)
        {
            Object.DestroyImmediate(oldAimPad.gameObject);
        }

        GameObject padRoot = BuildAimPadHierarchy(cockpit, new Vector3(4.5f, 0.04f, 4.6f), matDarkSteel, matGunmetal, matChrome, matHazard, matBtnBase, matBtnCyan, matBtnGreen, matBtnAmber);

        // Configurar referencias de gimbal en MechaController
        SerializedObject mechaSO = new SerializedObject(mecha);
        Transform leftArm = cockpit.Find("Arm_Left_Gatling");
        Transform missilePod = cockpit.Find("Shoulder_Right/Missile_Pod");

        if (leftArm != null) mechaSO.FindProperty("gatlingGimbalTransform").objectReferenceValue = leftArm;
        if (missilePod != null) mechaSO.FindProperty("missilePodGimbalTransform").objectReferenceValue = missilePod;
        mechaSO.ApplyModifiedProperties();

        // Asegurar que WindshieldHUDCrosshair esté presente en la cabina
        Transform hud = cockpit.Find("Windshield_HUD");
        if (hud == null)
        {
            GameObject hudObj = new GameObject("Windshield_HUD");
            hudObj.transform.parent = cockpit;
            hudObj.transform.localPosition = new Vector3(0f, 4.4f, 8.0f);
            hudObj.AddComponent<WindshieldHUDCrosshair>();
        }

        // Marcar la escena como modificada y guardarla
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[AIM SETUP] ¡Plataforma Giroscópica de Puntería (Gunner Aiming Pad) instalada con éxito en la escena!");
    }

    public static GameObject BuildAimPadHierarchy(
        Transform parent,
        Vector3 localPos,
        Material matDarkSteel,
        Material matGunmetal,
        Material matChrome,
        Material matHazard,
        Material matBtnBase,
        Material matBtnCyan,
        Material matBtnGreen,
        Material matBtnAmber)
    {
        GameObject CreateCylinder(string name, Transform p, Vector3 lPos, Vector3 scale, Quaternion lRot, Material m)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            c.name = name;
            c.transform.parent = p;
            c.transform.localPosition = lPos;
            c.transform.localRotation = lRot;
            c.transform.localScale = scale;
            if (m != null) c.GetComponent<MeshRenderer>().material = m;
            Object.DestroyImmediate(c.GetComponent<Collider>());
            return c;
        }

        GameObject CreateBlock(string name, Transform p, Vector3 lPos, Vector3 scale, Quaternion lRot, Material m)
        {
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name;
            b.transform.parent = p;
            b.transform.localPosition = lPos;
            b.transform.localRotation = lRot;
            b.transform.localScale = scale;
            if (m != null) b.GetComponent<MeshRenderer>().material = m;
            Object.DestroyImmediate(b.GetComponent<Collider>());
            return b;
        }

        GameObject CreateSphere(string name, Transform p, Vector3 lPos, Vector3 scale, Quaternion lRot, Material m)
        {
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = name;
            s.transform.parent = p;
            s.transform.localPosition = lPos;
            s.transform.localRotation = lRot;
            s.transform.localScale = scale;
            if (m != null) s.GetComponent<MeshRenderer>().material = m;
            Object.DestroyImmediate(s.GetComponent<Collider>());
            return s;
        }

        GameObject padRoot = new GameObject("Btn_AIM_PAD");
        padRoot.transform.parent = parent;
        padRoot.transform.localPosition = localPos;
        padRoot.transform.localRotation = Quaternion.identity;

        // Base y anillo de seguridad con franjas hazard
        GameObject housing = CreateCylinder("Housing", padRoot.transform, new Vector3(0f, 0.08f, 0f), new Vector3(3.5f, 0.16f, 3.5f), Quaternion.identity, matBtnBase);
        CreateCylinder("RimRing", housing.transform, new Vector3(0f, 0.52f, 0f), new Vector3(1.02f, 0.22f, 1.02f), Quaternion.identity, matHazard);

        // Rótula esférica central (gimbal ball)
        CreateSphere("Gimbal_Ball", padRoot.transform, new Vector3(0f, 0.18f, 0f), Vector3.one * 1.4f, Quaternion.identity, matChrome);

        // 4 Pistones hidráulicos amortiguadores (N, S, E, O)
        Transform[] pistons = new Transform[4];
        pistons[0] = CreateCylinder("Piston_N", padRoot.transform, new Vector3(0f, 0.18f, 1.25f), new Vector3(0.28f, 0.26f, 0.28f), Quaternion.identity, matChrome).transform;
        pistons[1] = CreateCylinder("Piston_S", padRoot.transform, new Vector3(0f, 0.18f, -1.25f), new Vector3(0.28f, 0.26f, 0.28f), Quaternion.identity, matChrome).transform;
        pistons[2] = CreateCylinder("Piston_E", padRoot.transform, new Vector3(1.25f, 0.18f, 0f), new Vector3(0.28f, 0.26f, 0.28f), Quaternion.identity, matChrome).transform;
        pistons[3] = CreateCylinder("Piston_W", padRoot.transform, new Vector3(-1.25f, 0.18f, 0f), new Vector3(0.28f, 0.26f, 0.28f), Quaternion.identity, matChrome).transform;

        // Disco superior orientable / inclinable (Aim Disc)
        GameObject aimDisc = new GameObject("Aim_Disc");
        aimDisc.transform.parent = padRoot.transform;
        aimDisc.transform.localPosition = new Vector3(0f, 0.28f, 0f);

        // Plancha de soporte con acabado de acero militar
        CreateCylinder("Plate", aimDisc.transform, Vector3.zero, new Vector3(3.2f, 0.14f, 3.2f), Quaternion.identity, matDarkSteel);
        CreateCylinder("Plate_Border", aimDisc.transform, new Vector3(0f, 0.08f, 0f), new Vector3(3.24f, 0.04f, 3.24f), Quaternion.identity, matGunmetal);

        // Núcleo central táctico con óptica cian de Lock-On
        CreateCylinder("Hub_LockOn", aimDisc.transform, new Vector3(0f, 0.09f, 0f), new Vector3(1.25f, 0.08f, 1.25f), Quaternion.identity, matBtnCyan);

        // Indicadores direccionales tácticos en relieve sobre la plataforma
        CreateBlock("Chevron_Up", aimDisc.transform, new Vector3(0f, 0.10f, 1.05f), new Vector3(0.85f, 0.05f, 0.38f), Quaternion.identity, matBtnGreen);
        CreateBlock("Chevron_Down", aimDisc.transform, new Vector3(0f, 0.10f, -1.05f), new Vector3(0.85f, 0.05f, 0.38f), Quaternion.identity, matBtnAmber);
        CreateBlock("Chevron_Left", aimDisc.transform, new Vector3(-1.05f, 0.10f, 0f), new Vector3(0.38f, 0.05f, 0.85f), Quaternion.identity, matBtnCyan);
        CreateBlock("Chevron_Right", aimDisc.transform, new Vector3(1.05f, 0.10f, 0f), new Vector3(0.38f, 0.05f, 0.85f), Quaternion.identity, matBtnCyan);

        // Rótulo central con texto 3D de control
        GameObject labelCenterObj = new GameObject("Label_Center");
        labelCenterObj.transform.parent = aimDisc.transform;
        labelCenterObj.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        labelCenterObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        labelCenterObj.transform.localScale = Vector3.one * 0.18f;
        TextMesh ctm = labelCenterObj.AddComponent<TextMesh>();
        ctm.text = "🎯 MIRA\n[LOCK / CULAZO]";
        ctm.fontSize = 28;
        ctm.characterSize = 0.28f;
        ctm.anchor = TextAnchor.MiddleCenter;
        ctm.alignment = TextAlignment.Center;
        ctm.fontStyle = FontStyle.Bold;
        ctm.color = Color.white;

        // Rótulos cardinales de puntería
        void AddDirectionLabel(string text, Vector3 lPos, Color col)
        {
            GameObject lObj = new GameObject($"Label_{text}");
            lObj.transform.parent = aimDisc.transform;
            lObj.transform.localPosition = lPos;
            lObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lObj.transform.localScale = Vector3.one * 0.14f;
            TextMesh tm = lObj.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 24;
            tm.characterSize = 0.28f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = col;
        }

        AddDirectionLabel("▲ ELEVAR (+)", new Vector3(0f, 0.15f, 1.25f), Color.green);
        AddDirectionLabel("▼ BAJAR (-)", new Vector3(0f, 0.15f, -1.25f), new Color(1f, 0.7f, 0.2f));
        AddDirectionLabel("◄ APUNTAR IZQ", new Vector3(-1.18f, 0.15f, 0f), Color.cyan);
        AddDirectionLabel("APUNTAR DER ►", new Vector3(1.18f, 0.15f, 0f), Color.cyan);

        // Configurar componente GunnerAimPad
        GunnerAimPad aimPadComp = padRoot.AddComponent<GunnerAimPad>();
        aimPadComp.buttonId = "AIM_PAD";
        aimPadComp.label = "🎯 GIROSCOPIO DE PUNTERÍA";
        aimPadComp.type = CockpitFloorButton.ButtonType.Hold;
        aimPadComp.detectionRadius = 1.75f;
        aimPadComp.buttonColor = Color.cyan;

        SerializedObject padSO = new SerializedObject(aimPadComp);
        padSO.FindProperty("padDiscTransform").objectReferenceValue = aimDisc.transform;
        padSO.FindProperty("capTransform").objectReferenceValue = aimDisc.transform;

        SerializedProperty pistonsProp = padSO.FindProperty("hydraulicPistons");
        pistonsProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            pistonsProp.GetArrayElementAtIndex(i).objectReferenceValue = pistons[i];
        }
        padSO.ApplyModifiedProperties();

        return padRoot;
    }
}
