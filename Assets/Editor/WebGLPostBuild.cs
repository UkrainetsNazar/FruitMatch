#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

public class WebGLPostBuild
{
    [PostProcessBuild]
    public static void OnPostBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.WebGL) return;

        var swPath = Path.Combine(path, "ServiceWorker.js");
        if (!File.Exists(swPath)) return;

        var content = File.ReadAllText(swPath);

        content = content.Replace(
            "cache.put(event.request,",
            "if(event.request.method==='GET') cache.put(event.request,"
        );

        File.WriteAllText(swPath, content);
        UnityEngine.Debug.Log("[Build] ServiceWorker.js patched.");
    }
}
#endif