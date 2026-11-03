using UnityEngine;

/// <summary>
/// Utilidad para asegurar que los componentes TextMesh en 3D respeten el búfer de profundidad (ZTest LEqual).
/// Por defecto, Unity utiliza 'GUI/Text Shader' con 'ZTest Always', lo que provoca que el texto se dibuje
/// por encima de cualquier objeto 3D (atravesando el blindaje, torso y paredes sólidas del Mecha).
/// Esta utilidad asigna un shader con ZTest LEqual para que el texto sea ocluido correctamente por la geometría 3D.
/// </summary>
public static class TextShaderUtility
{
    private static Shader zTestedShader;

    public static Shader GetZTestedShader()
    {
        if (zTestedShader == null)
        {
            zTestedShader = Shader.Find("GUI/3D Text Shader (Z-Tested)");
        }
        return zTestedShader;
    }

    public static void ApplyZTesting(TextMesh tm)
    {
        if (tm == null) return;
        var mr = tm.GetComponent<MeshRenderer>();
        if (mr == null) return;

        Shader shader = GetZTestedShader();
        if (shader == null) return;

        // Si ya tiene el shader con prueba de profundidad, no reasignar
        if (mr.sharedMaterial != null && mr.sharedMaterial.shader == shader) return;

        Texture mainTex = mr.sharedMaterial != null ? mr.sharedMaterial.mainTexture : null;
        if (mainTex == null && tm.font != null && tm.font.material != null)
        {
            mainTex = tm.font.material.mainTexture;
        }

        Material mat = new Material(shader);
        mat.name = tm.name + "_ZTestedMat";
        if (mainTex != null) mat.mainTexture = mainTex;
        mat.color = tm.color;

        mr.material = mat;
    }

    public static void ApplyZTestingToAllChildren(GameObject root)
    {
        if (root == null) return;
        var textMeshes = root.GetComponentsInChildren<TextMesh>(true);
        for (int i = 0; i < textMeshes.Length; i++)
        {
            ApplyZTesting(textMeshes[i]);
        }
    }
}
