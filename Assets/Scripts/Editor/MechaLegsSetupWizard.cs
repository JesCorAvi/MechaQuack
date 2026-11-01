using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MechaLegsSetupWizard
{
    [MenuItem("Mecha/Setup Procedural Legs", false, 1)]
    [MenuItem("PatoJuego/Setup Procedural Legs", false, 20)]
    public static void SetupProceduralLegsMenu()
    {
        SetupProceduralLegs(false);
    }

    public static void SetupProceduralLegs(bool showDialog = false)
    {
        GameObject mechaRoot = GameObject.Find("Mecha_Root");
        if (mechaRoot == null)
        {
            var mechaCtrl = Object.FindFirstObjectByType<MechaController>();
            if (mechaCtrl != null) mechaRoot = mechaCtrl.gameObject;
        }

        if (mechaRoot == null)
        {
            Debug.LogError("[Setup Procedural Legs] No se encontró el objeto 'Mecha_Root' en la escena activa.");
            if (showDialog) EditorUtility.DisplayDialog("Setup Procedural Legs", "No se encontró el objeto 'Mecha_Root' en la escena activa.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(mechaRoot, "Setup Procedural Legs");

        // 1. Localizar o refactorizar piernas
        Transform leftLegTr = mechaRoot.transform.Find("Leg_Left");
        Transform rightLegTr = mechaRoot.transform.Find("Leg_Right");

        if (leftLegTr == null || rightLegTr == null)
        {
            Debug.LogError("[Setup Procedural Legs] No se encontraron 'Leg_Left' y 'Leg_Right' bajo Mecha_Root.");
            if (showDialog) EditorUtility.DisplayDialog("Setup Procedural Legs", "No se encontraron 'Leg_Left' y 'Leg_Right' bajo Mecha_Root.", "OK");
            return;
        }

        ProceduralLeg legL = SetupLegHierarchy(leftLegTr, -3.1f);
        ProceduralLeg legR = SetupLegHierarchy(rightLegTr, 3.1f);

        // 2. Configurar MechLocomotionController
        var locomotion = mechaRoot.GetComponent<MechLocomotionController>();
        if (locomotion == null) locomotion = mechaRoot.AddComponent<MechLocomotionController>();

        SerializedObject locoSO = new SerializedObject(locomotion);
        locoSO.FindProperty("leftLeg").objectReferenceValue = legL;
        locoSO.FindProperty("rightLeg").objectReferenceValue = legR;
        locoSO.FindProperty("mechaRigidbody").objectReferenceValue = mechaRoot.GetComponent<Rigidbody>();
        locoSO.ApplyModifiedProperties();

        // 3. Configurar MechBodyStabilizer
        var stabilizer = mechaRoot.GetComponent<MechBodyStabilizer>();
        if (stabilizer == null) stabilizer = mechaRoot.AddComponent<MechBodyStabilizer>();

        SerializedObject stabSO = new SerializedObject(stabilizer);
        stabSO.FindProperty("locomotionController").objectReferenceValue = locomotion;
        stabSO.FindProperty("mechaRigidbody").objectReferenceValue = mechaRoot.GetComponent<Rigidbody>();
        Transform pelvis = mechaRoot.transform.Find("Pelvis");
        if (pelvis != null) stabSO.FindProperty("chassisTransform").objectReferenceValue = pelvis;
        Transform cockpit = mechaRoot.transform.Find("Cockpit");
        if (cockpit != null) stabSO.FindProperty("cockpitTransform").objectReferenceValue = cockpit;
        stabSO.ApplyModifiedProperties();

        // 4. Configurar FootstepFX
        var fx = mechaRoot.GetComponent<FootstepFX>();
        if (fx == null) fx = mechaRoot.AddComponent<FootstepFX>();

        SerializedObject fxSO = new SerializedObject(fx);
        fxSO.FindProperty("leftLeg").objectReferenceValue = legL;
        fxSO.FindProperty("rightLeg").objectReferenceValue = legR;
        fxSO.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("¡[Mecha Procedural Legs] Configuración completada con éxito en Mecha_Root!");
        if (showDialog)
        {
            EditorUtility.DisplayDialog("Setup Procedural Legs", "¡Sistema de patas procedurales configurado con éxito!\n\n- Jerarquía Cadera->Rodilla->Tobillo articulada\n- TwoBoneIKSolver con Pole Targets\n- ProceduralLeg en cada pata\n- MechLocomotionController (gait bípedo alterno)\n- MechBodyStabilizer (suspensión y tilt)\n- FootstepFX (partículas, audio y shake)", "OK");
        }
    }

    private static ProceduralLeg SetupLegHierarchy(Transform legRoot, float xOffset)
    {
        Transform kneeJoint = legRoot.Find("Joint_Knee");
        Transform ankleJoint = kneeJoint != null ? kneeJoint.Find("Joint_Ankle") : null;
        Transform poleTarget = legRoot.Find("Pole_Target");
        Transform thighMesh = legRoot.Find("Thigh");
        Transform kneeMesh = kneeJoint != null ? kneeJoint.Find("Knee") : legRoot.Find("Knee");
        Transform shinMesh = kneeJoint != null ? kneeJoint.Find("Shin") : legRoot.Find("Shin");
        Transform footMesh = ankleJoint != null ? ankleJoint.Find("Foot") : legRoot.Find("Foot");

        if (kneeJoint == null)
        {
            GameObject kneeObj = new GameObject("Joint_Knee");
            kneeJoint = kneeObj.transform;
            kneeJoint.parent = legRoot;
        }

        if (ankleJoint == null)
        {
            GameObject ankleObj = new GameObject("Joint_Ankle");
            ankleJoint = ankleObj.transform;
            ankleJoint.parent = kneeJoint;
        }

        if (poleTarget == null)
        {
            GameObject poleObj = new GameObject("Pole_Target");
            poleTarget = poleObj.transform;
            poleTarget.parent = legRoot;
        }

        // Posiciones y escalas calibradas para alcance de 5.6m (apoyo firme en terreno y superación de rocas)
        kneeJoint.localPosition = new Vector3(0f, -3.0f, 0f);
        kneeJoint.localRotation = Quaternion.identity;
        ankleJoint.localPosition = new Vector3(0f, -2.6f, 0f);
        ankleJoint.localRotation = Quaternion.identity;
        poleTarget.localPosition = new Vector3(0f, -3.0f, 4.0f);
        poleTarget.localRotation = Quaternion.identity;

        if (thighMesh != null)
        {
            thighMesh.parent = legRoot;
            thighMesh.localPosition = new Vector3(0f, -1.5f, 0f);
            thighMesh.localScale = new Vector3(2.1f, 3.0f, 2.4f);
        }

        if (kneeMesh != null)
        {
            kneeMesh.parent = kneeJoint;
            kneeMesh.localPosition = Vector3.zero;
            kneeMesh.localScale = new Vector3(2.4f, 1.1f, 2.7f);
        }

        if (shinMesh != null)
        {
            shinMesh.parent = kneeJoint;
            shinMesh.localPosition = new Vector3(0f, -1.3f, 0f);
            shinMesh.localScale = new Vector3(1.9f, 2.6f, 2.2f);
        }

        if (footMesh != null)
        {
            footMesh.parent = ankleJoint;
            footMesh.localPosition = new Vector3(0f, 0f, 0.6f);
            footMesh.localScale = new Vector3(2.8f, 0.9f, 4.2f);
        }

        // Componente ProceduralLeg
        var procLeg = legRoot.GetComponent<ProceduralLeg>();
        if (procLeg == null) procLeg = legRoot.gameObject.AddComponent<ProceduralLeg>();

        SerializedObject so = new SerializedObject(procLeg);
        so.FindProperty("ikSolver.hip").objectReferenceValue = legRoot;
        so.FindProperty("ikSolver.knee").objectReferenceValue = kneeJoint;
        so.FindProperty("ikSolver.ankle").objectReferenceValue = ankleJoint;
        so.FindProperty("ikSolver.poleTarget").objectReferenceValue = poleTarget;
        so.FindProperty("bodyRoot").objectReferenceValue = legRoot.parent;
        so.FindProperty("bodyRigidbody").objectReferenceValue = legRoot.parent != null ? legRoot.parent.GetComponent<Rigidbody>() : null;
        so.FindProperty("localRestOffset").vector3Value = new Vector3(xOffset, 0f, 0f);
        so.ApplyModifiedProperties();

        procLeg.SetLocalRestOffset(new Vector3(xOffset, 0f, 0f));
        procLeg.IKSolver.Initialize();

        return procLeg;
    }
}
