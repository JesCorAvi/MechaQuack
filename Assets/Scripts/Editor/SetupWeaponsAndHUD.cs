using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Asistente de editor para configurar o actualizar en 1 clic el HUD holográfico profesional
/// en el parabrisas y el nuevo arsenal de MechaQuack (Ametralladora Gatling y Lanzamisiles).
/// </summary>
public static class SetupWeaponsAndHUD
{
    [InitializeOnLoadMethod]
    private static void AutoRunOnEditorLoad()
    {
        EditorApplication.update += RunOnce;
    }

    private static void RunOnce()
    {
        EditorApplication.update -= RunOnce;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Debug.Log("[WEAPONS SETUP] Comprobando armas y HUD en escena...");
        var mechaObj = GameObject.Find("Mecha_Root");
        if (mechaObj != null && mechaObj.transform.Find("Cockpit/Windshield_HUD") == null)
        {
            ExecuteSetup();
        }
    }

    [MenuItem("Mecha/Setup Weapons and Windshield HUD")]
    public static void ExecuteSetup()
    {
        var mechaObj = GameObject.Find("Mecha_Root");
        var mecha = mechaObj != null ? mechaObj.GetComponent<MechaController>() : Object.FindFirstObjectByType<MechaController>();
        if (mecha == null)
        {
            Debug.LogError("[WEAPONS SETUP] No se encontró MechaController en la escena.");
            return;
        }

        Transform mechaRoot = mecha.transform;
        Transform cockpit = mechaRoot.Find("Cockpit");
        if (cockpit == null)
        {
            Debug.LogError("[WEAPONS SETUP] No se encontró 'Cockpit' bajo Mecha_Root.");
            return;
        }

        // Cargar materiales existentes
        Material matWall = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CockpitWall.mat");
        Material matRed = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_MechaRed.mat");
        Material matBlue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_MechaBlue.mat");
        Material matYellow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_DuckYellow.mat");
        Material matAmber = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_BtnAmber.mat");
        Material matCyan = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_BtnCyan.mat");
        Material matHazard = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_HazardStripe.mat");
        Material matBase = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_BtnBase.mat");

        // 1. LIMPIAR ANTIGUO CILINDRO AZUL (Crosshair_Ring) Y MONTAR HUD HOLOGRÁFICO PROFESIONAL
        var oldDot = cockpit.Find("Crosshair_Ring");
        if (oldDot != null) Object.DestroyImmediate(oldDot.gameObject);

        var existingHud = cockpit.Find("Windshield_HUD");
        if (existingHud != null) Object.DestroyImmediate(existingHud.gameObject);

        GameObject hudObj = new GameObject("Windshield_HUD");
        hudObj.transform.parent = cockpit;
        hudObj.transform.localPosition = new Vector3(0f, 2.4f, 3.82f);
        hudObj.transform.localRotation = Quaternion.identity;
        hudObj.transform.localScale = Vector3.one;
        hudObj.AddComponent<WindshieldHUDCrosshair>();

        // 2. CONFIGURAR BRAZO IZQUIERDO: AMETRALLADORA ROTATORIA GATLING
        Transform armLeft = cockpit.Find("Arm_Left_Punch") ?? cockpit.Find("Arm_Left_Gatling");
        if (armLeft == null)
        {
            armLeft = new GameObject("Arm_Left_Gatling").transform;
            armLeft.parent = cockpit;
            armLeft.localPosition = new Vector3(-6.8f, -0.6f, 0.5f);
        }
        else
        {
            armLeft.name = "Arm_Left_Gatling";
            for (int i = armLeft.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(armLeft.GetChild(i).gameObject);
            }
        }

        // Antebrazo blindado
        GameObject forearmL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        forearmL.name = "Forearm";
        forearmL.transform.parent = armLeft;
        forearmL.transform.localPosition = new Vector3(0f, 0f, 1.8f);
        forearmL.transform.localScale = new Vector3(2.0f, 1.9f, 6.2f);
        if (matRed != null) forearmL.GetComponent<MeshRenderer>().material = matRed;

        // Tambor de munición inferior
        GameObject drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        drum.name = "Ammo_Drum";
        drum.transform.parent = armLeft;
        drum.transform.localPosition = new Vector3(0f, -1.1f, 1.6f);
        drum.transform.localScale = new Vector3(2.4f, 0.9f, 2.4f);
        if (matWall != null) drum.GetComponent<MeshRenderer>().material = matWall;
        Object.DestroyImmediate(drum.GetComponent<Collider>());

        // Franjas de peligro en el tambor
        GameObject drumBand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        drumBand.name = "Drum_Band";
        drumBand.transform.parent = drum.transform;
        drumBand.transform.localPosition = Vector3.zero;
        drumBand.transform.localScale = new Vector3(1.02f, 0.25f, 1.02f);
        if (matHazard != null) drumBand.GetComponent<MeshRenderer>().material = matHazard;
        Object.DestroyImmediate(drumBand.GetComponent<Collider>());

        // Ensamblaje del rotor giratorio de 6 cañones
        GameObject rotorObj = new GameObject("Gatling_Rotor");
        rotorObj.transform.parent = armLeft;
        rotorObj.transform.localPosition = new Vector3(0f, 0f, 4.8f);

        // Husillo central
        GameObject spindle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spindle.name = "Spindle";
        spindle.transform.parent = rotorObj.transform;
        spindle.transform.localPosition = new Vector3(0f, 0f, 0.9f);
        spindle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        spindle.transform.localScale = new Vector3(0.75f, 1.6f, 0.75f);
        if (matWall != null) spindle.GetComponent<MeshRenderer>().material = matWall;
        Object.DestroyImmediate(spindle.GetComponent<Collider>());

        // Anillo frontal estabilizador de cañones
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Barrel_Ring";
        ring.transform.parent = rotorObj.transform;
        ring.transform.localPosition = new Vector3(0f, 0f, 2.2f);
        ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ring.transform.localScale = new Vector3(1.7f, 0.12f, 1.7f);
        if (matYellow != null) ring.GetComponent<MeshRenderer>().material = matYellow;
        Object.DestroyImmediate(ring.GetComponent<Collider>());

        // 6 Cañones en círculo
        float barrelRadius = 0.62f;
        for (int i = 0; i < 6; i++)
        {
            float ang = i * 60f * Mathf.Deg2Rad;
            Vector3 bLocalPos = new Vector3(Mathf.Cos(ang) * barrelRadius, Mathf.Sin(ang) * barrelRadius, 1.5f);

            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            b.name = $"Barrel_{i}";
            b.transform.parent = rotorObj.transform;
            b.transform.localPosition = bLocalPos;
            b.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            b.transform.localScale = new Vector3(0.24f, 2.1f, 0.24f);
            if (matWall != null) b.GetComponent<MeshRenderer>().material = matWall;
            Object.DestroyImmediate(b.GetComponent<Collider>());
        }

        // Boca de fuego / Muzzle
        GameObject muzzleObj = new GameObject("Gatling_Muzzle");
        muzzleObj.transform.parent = rotorObj.transform;
        muzzleObj.transform.localPosition = new Vector3(0f, 0f, 3.6f);

        // Luz de fogonazo
        GameObject muzzleLightObj = new GameObject("Muzzle_Light");
        muzzleLightObj.transform.parent = muzzleObj.transform;
        muzzleLightObj.transform.localPosition = Vector3.zero;
        Light ml = muzzleLightObj.AddComponent<Light>();
        ml.type = LightType.Point;
        ml.color = new Color(1.0f, 0.65f, 0.15f);
        ml.range = 8f;
        ml.intensity = 3.5f;
        ml.enabled = false;

        // Destruir cualquier resto de objetos legados (LaserBeam, PunchWave, etc.)
        var oldLaser = GameObject.Find("LaserBeam");
        if (oldLaser != null) Object.DestroyImmediate(oldLaser);
        var oldPunch = GameObject.Find("PunchWave");
        if (oldPunch != null) Object.DestroyImmediate(oldPunch);

        // 3. CONFIGURAR BRAZO DERECHO: POD LANZAMISILES DE 4 TUBOS
        Transform armRight = cockpit.Find("Arm_Right_Laser") ?? cockpit.Find("Arm_Right_MissilePod");
        if (armRight == null)
        {
            armRight = new GameObject("Arm_Right_MissilePod").transform;
            armRight.parent = cockpit;
            armRight.localPosition = new Vector3(6.8f, -0.6f, 0.5f);
        }
        else
        {
            armRight.name = "Arm_Right_MissilePod";
            for (int i = armRight.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(armRight.GetChild(i).gameObject);
            }
        }

        // Antebrazo derecho
        GameObject forearmR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        forearmR.name = "Forearm";
        forearmR.transform.parent = armRight;
        forearmR.transform.localPosition = new Vector3(0f, 0f, 1.8f);
        forearmR.transform.localScale = new Vector3(2.0f, 1.9f, 5.8f);
        if (matBlue != null) forearmR.GetComponent<MeshRenderer>().material = matBlue;

        // Pod lanzamisiles blindado
        GameObject podBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        podBox.name = "Missile_Pod";
        podBox.transform.parent = armRight;
        podBox.transform.localPosition = new Vector3(0f, 0.2f, 5.0f);
        podBox.transform.localScale = new Vector3(3.2f, 3.0f, 4.2f);
        if (matWall != null) podBox.GetComponent<MeshRenderer>().material = matWall;

        // Ribete frontal decorativo del pod
        GameObject podBezel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        podBezel.name = "Pod_Bezel";
        podBezel.transform.parent = podBox.transform;
        podBezel.transform.localPosition = new Vector3(0f, 0f, 0.51f);
        podBezel.transform.localScale = new Vector3(1.05f, 1.05f, 0.08f);
        if (matHazard != null) podBezel.GetComponent<MeshRenderer>().material = matHazard;
        Object.DestroyImmediate(podBezel.GetComponent<Collider>());

        // 4 Tubos lanzamisiles con ojivas visibles
        Transform[] tubes = new Transform[4];
        Vector2[] tubeOffsets = new Vector2[]
        {
            new Vector2(-0.75f, 0.70f),
            new Vector2(0.75f, 0.70f),
            new Vector2(-0.75f, -0.70f),
            new Vector2(0.75f, -0.70f)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject tObj = new GameObject($"LaunchTube_{i}");
            tObj.transform.parent = podBox.transform;
            tObj.transform.localPosition = new Vector3(tubeOffsets[i].x / 3.2f, tubeOffsets[i].y / 3.0f, 0.52f);
            tObj.transform.localRotation = Quaternion.identity;
            tubes[i] = tObj.transform;

            // Brocal del tubo
            GameObject collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            collar.name = "Collar";
            collar.transform.parent = tObj.transform;
            collar.transform.localPosition = Vector3.zero;
            collar.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            collar.transform.localScale = new Vector3(0.24f, 0.05f, 0.24f);
            if (matBase != null) collar.GetComponent<MeshRenderer>().material = matBase;
            Object.DestroyImmediate(collar.GetComponent<Collider>());

            // Ojiva visible dentro del tubo
            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "Warhead_Tip";
            tip.transform.parent = tObj.transform;
            tip.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            tip.transform.localScale = new Vector3(0.20f, 0.20f, 0.28f);
            if (matRed != null) tip.GetComponent<MeshRenderer>().material = matRed;
            Object.DestroyImmediate(tip.GetComponent<Collider>());
        }

        // 4. ACTUALIZAR BOTONES DEL SUELO DE LA CABINA
        Transform buttonsGroup = cockpit.Find("Cockpit_Buttons");
        if (buttonsGroup != null)
        {
            // Botón Gatling (Izquierda)
            Transform bGatling = buttonsGroup.Find("Button_PUNCH") ?? buttonsGroup.Find("Button_GATLING");
            if (bGatling != null)
            {
                bGatling.name = "Button_GATLING";
                var btnComp = bGatling.GetComponent<CockpitFloorButton>();
                if (btnComp != null)
                {
                    btnComp.buttonId = "GATLING";
                    btnComp.label = "🔫 GATLING\n[MANTENER / E]";
                    btnComp.type = CockpitFloorButton.ButtonType.Hold;
                    btnComp.buttonColor = new Color(1.0f, 0.45f, 0.1f);

                    Transform cap = bGatling.Find("Cap");
                    if (cap != null)
                    {
                        if (matAmber != null) cap.GetComponent<MeshRenderer>().material = matAmber;
                        Transform lbl = cap.Find("Label");
                        if (lbl != null && lbl.TryGetComponent<TextMesh>(out var tm))
                        {
                            tm.text = "🔫 GATLING\n[MANTENER / E]";
                        }
                    }
                }
            }

            // Botón Misiles (Derecha)
            Transform bMissiles = buttonsGroup.Find("Button_LASER") ?? buttonsGroup.Find("Button_MISSILES");
            if (bMissiles != null)
            {
                bMissiles.name = "Button_MISSILES";
                var btnComp = bMissiles.GetComponent<CockpitFloorButton>();
                if (btnComp != null)
                {
                    btnComp.buttonId = "MISSILES";
                    btnComp.label = "🚀 MISILES\n(CULAZO E)";
                    btnComp.type = CockpitFloorButton.ButtonType.Stomp;
                    btnComp.buttonColor = new Color(0.15f, 0.75f, 1.0f);

                    Transform cap = bMissiles.Find("Cap");
                    if (cap != null)
                    {
                        if (matCyan != null) cap.GetComponent<MeshRenderer>().material = matCyan;
                        Transform lbl = cap.Find("Label");
                        if (lbl != null && lbl.TryGetComponent<TextMesh>(out var tm))
                        {
                            tm.text = "🚀 MISILES\n(CULAZO E)";
                        }
                    }
                }
            }
        }

        // 5. ASIGNAR REFERENCIAS SERIALIZADAS EN MECHACONTROLLER
        SerializedObject so = new SerializedObject(mecha);
        so.FindProperty("leftArmTransform").objectReferenceValue = armLeft;
        so.FindProperty("rightArmTransform").objectReferenceValue = armRight;
        so.FindProperty("gatlingRotorTransform").objectReferenceValue = rotorObj.transform;
        so.FindProperty("gatlingMuzzleTransform").objectReferenceValue = muzzleObj.transform;
        so.FindProperty("gatlingMuzzleLight").objectReferenceValue = ml;
        so.FindProperty("missilePodTransform").objectReferenceValue = podBox.transform;

        SerializedProperty tubesProp = so.FindProperty("missileLaunchTubes");
        tubesProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            tubesProp.GetArrayElementAtIndex(i).objectReferenceValue = tubes[i];
        }

        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[WEAPONS SETUP] ¡Sistemas de armas y HUD holográfico instalados con éxito en la escena!");
    }
}
