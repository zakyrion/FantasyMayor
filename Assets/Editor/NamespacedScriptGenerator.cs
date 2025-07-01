using System.IO;
using UnityEditor;
using UnityEngine;
/// <summary>
///     Post processor that adds namespaces to C# scripts based on their file path
/// </summary>
public class NamespacePostProcessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (var assetPath in importedAssets)
        {
            // Обробляємо лише C# скрипти
            if (assetPath.EndsWith(".cs") && !assetPath.Contains("NamespacedScriptGenerator"))
            {
                // Зчитуємо вміст скрипту
                var content = File.ReadAllText(assetPath);

                // Якщо у скрипті міститься маркер, який потрібно замінити
                if (content.Contains("#NAMESPACE#"))
                {
                    // Заміна маркера на бажаний код
                    var namespaceName = GetNamespaceFromPath(assetPath);
                    content = content.Replace("#NAMESPACE#", namespaceName);

                    // Записуємо змінений вміст назад у файл
                    File.WriteAllText(assetPath, content);
                    Debug.Log($"Modified script: {assetPath}");

                    // Примусова повторна імпортізація, щоб зміни врахувалися
                    AssetDatabase.ImportAsset(assetPath);
                }
            }
        }
    }
    public static void OnWillCreateAsset(string path)
    {
        path = path.Replace(".meta", "");
        if (!path.EndsWith(".cs")) return;

        var fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), path);
        var content = File.ReadAllText(fullPath);

        // Replace namespace placeholder with actual namespace based on path
        var namespaceName = GetNamespaceFromPath(path);
        content = content.Replace("#NAMESPACE#", namespaceName);

        File.WriteAllText(fullPath, content);
        AssetDatabase.Refresh();
    }

    private static string GetNamespaceFromPath(string assetPath)
    {
        var relativePath = Path.GetDirectoryName(assetPath).Replace("Assets/Features/", "").Replace("/", ".");
        var baseNamespace = "YourProject"; // Replace with your project's root namespace

        if (string.IsNullOrEmpty(relativePath))
            return baseNamespace;

        return $"{relativePath}";
    }
}
