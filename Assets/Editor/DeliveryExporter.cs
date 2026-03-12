#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DeliveryExporter
{
    [MenuItem("Tools/Exportar Entrega")]
    public static void ExportCurrentScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Detén el Play Mode antes de exportar la entrega.");
            return;
        }

        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(activeScene.path))
        {
            Debug.LogError("Debes guardar la escena al menos una vez antes de exportarla.");
            return;
        }

        EditorSceneManager.SaveScene(activeScene);

        string exportPath = EditorUtility.SaveFilePanel(
            "Guardar Entrega",
            "",
            $"{activeScene.name}_Entrega.unitypackage",
            "unitypackage"
        );

        if (string.IsNullOrEmpty(exportPath))
            return;

        AssetDatabase.ExportPackage(activeScene.path, exportPath, ExportPackageOptions.Interactive);

        Debug.Log($"Entrega exportada exitosamente en: {exportPath}");
    }
}
#endif
