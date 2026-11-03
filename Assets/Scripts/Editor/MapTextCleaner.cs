using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Herramienta de Editor para eliminar automáticamente todos los textos flotantes 3D del mapa
/// (Label3D, InfoText, Label_*, SignBoard y Central_Info_Kiosk).
/// Funciona tanto en tiempo real en Play Mode como en Edit Mode permanentemente.
/// </summary>
[InitializeOnLoad]
public static class MapTextCleaner
{
    static MapTextCleaner()
    {
        EditorApplication.delayCall += () =>
        {
            CleanAllMapTexts(false);
        };

        EditorApplication.playModeStateChanged += (state) =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += () => CleanAllMapTexts(false);
            }
        };
    }

    [MenuItem("Mecha/Remove All Floating Map Texts")]
    public static void CleanAllMapTextsMenu()
    {
        CleanAllMapTexts(true);
    }

    public static int CleanAllMapTexts(bool logAlways)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.isLoaded) return 0;

        var rootObjects = activeScene.GetRootGameObjects();
        var toDestroy = new List<GameObject>();

        foreach (var root in rootObjects)
        {
            // Proteger componentes del Mecha y del Pato
            if (root.GetComponent<MechaController>() != null) continue;
            if (root.GetComponent<DuckController>() != null) continue;

            // 1. Componentes TextMesh en el mapa
            var textMeshes = root.GetComponentsInChildren<TextMesh>(true);
            foreach (var tm in textMeshes)
            {
                if (tm != null && tm.gameObject != null && !toDestroy.Contains(tm.gameObject))
                {
                    toDestroy.Add(tm.gameObject);
                }
            }

            // 2. Objetos cartel / soporte de textos flotantes
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var tr in transforms)
            {
                if (tr == null || tr.gameObject == null) continue;
                string name = tr.name;
                if (name == "SignBoard" || name == "Central_Info_Kiosk" || name == "Label3D" || name == "InfoText" || name.StartsWith("Label_"))
                {
                    if (!toDestroy.Contains(tr.gameObject))
                    {
                        toDestroy.Add(tr.gameObject);
                    }
                }
            }
        }

        int count = toDestroy.Count;
        if (count > 0)
        {
            if (Application.isPlaying)
            {
                // En Play Mode: Destruir inmediatamente para limpiar la pantalla en la sesión en vivo
                for (int i = 0; i < toDestroy.Count; i++)
                {
                    if (toDestroy[i] != null)
                    {
                        Object.Destroy(toDestroy[i]);
                    }
                }
                Debug.Log($"[MapTextCleaner] OK: Se han eliminado {count} textos y carteles flotantes del mapa en tiempo real.");
            }
            else
            {
                // En Edit Mode: Destruir con soporte de Undo y guardar permanentemente la escena
                for (int i = 0; i < toDestroy.Count; i++)
                {
                    if (toDestroy[i] != null)
                    {
                        Undo.DestroyObjectImmediate(toDestroy[i]);
                    }
                }

                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log($"[MapTextCleaner] OK: Se han eliminado permanentemente {count} textos y carteles flotantes del mapa. Escena guardada correctamente.");
            }
        }
        else if (logAlways)
        {
            Debug.Log("[MapTextCleaner] No se encontraron textos flotantes del mapa (el escenario ya está completamente limpio).");
        }

        // Asegurar que todos los TextMeshes de la cabina del Mecha respeten la oclusión ZTest
        var mechas = Object.FindObjectsByType<MechaController>(FindObjectsSortMode.None);
        for (int i = 0; i < mechas.Length; i++)
        {
            TextShaderUtility.ApplyZTestingToAllChildren(mechas[i].gameObject);
        }

        return count;
    }
}
