using UnityEngine;
using UnityEditor;

public class ConvertToURPShader
{
    [MenuItem("Tools/Convert Materials to URP")]
    public static void Convert()
    {
        string folder = "Assets/SimplePoly City - Low Poly Assets/Materials";
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });

        Shader urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (urpShader == null)
        {
            Debug.LogError("URP Simple Lit shader not found!");
            return;
        }

        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            Texture mainTex = mat.GetTexture("_MainTex");
            Color color = mat.GetColor("_Color");

            mat.shader = urpShader;
            mat.SetTexture("_BaseMap", mainTex);
            mat.SetColor("_BaseColor", color);

            EditorUtility.SetDirty(mat);
            count++;
            Debug.Log($"Converted: {mat.name}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Done! Converted {count} materials to URP Simple Lit.");
    }
}
