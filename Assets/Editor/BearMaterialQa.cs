#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class BearMaterialQa
{
    [MenuItem("City Forge/QA/Bear Behaviors/Inspect Fur Material")]
    public static void Inspect()
    {
        var report = new StringBuilder();
        foreach (var renderer in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
        {
            report.AppendLine($"{renderer.name} shadow={renderer.shadowCastingMode} layer={renderer.gameObject.layer} uvCount={renderer.sharedMesh.uv.Length}");
            foreach (var m in renderer.sharedMaterials)
            {
                if (m == null) continue;
                report.AppendLine($"material={m.name} shader={m.shader.name} color={m.color} texture={AssetDatabase.GetAssetPath(m.mainTexture)} keywords={string.Join(",",m.shaderKeywords)}");
                report.AppendLine($"scale={m.mainTextureScale} offset={m.mainTextureOffset}");
                foreach (var p in new[]{"_Metallic", "_Glossiness", "_GlossMapScale"})
                    if (m.HasProperty(p)) report.AppendLine($"{p}={m.GetFloat(p)}");
                foreach (var p in new[]{"_BumpMap", "_MetallicGlossMap"})
                    if (m.HasProperty(p)) report.AppendLine($"{p}={AssetDatabase.GetAssetPath(m.GetTexture(p))}");
            }
        }
        report.AppendLine($"ambient={RenderSettings.ambientLight} colorspace={QualitySettings.activeColorSpace}");
        foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            report.AppendLine($"light={light.name} enabled={light.enabled} intensity={light.intensity} color={light.color}");
        Directory.CreateDirectory("QA/BearMaterialV04");
        File.WriteAllText("QA/BearMaterialV04/report.txt", report.ToString());
        Debug.Log(report.ToString());
    }
}
#endif
