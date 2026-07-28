using UnityEngine;
using UnityEditor;

public class AutoItemIDGenerator : EditorWindow
{
    [MenuItem("Tools/Item ID Generator")]
    public static void OpenWindow()
    {
        GetWindow<AutoItemIDGenerator>("Item ID Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto Item ID Generator", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate IDs for ALL ItemData"))
        {
            GenerateIDs();
        }
    }

    private void GenerateIDs()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData");

        int id = 1;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (item != null)
            {
                item.ID = id;
                EditorUtility.SetDirty(item);

                Debug.Log($"Assigned ID {id} → {item.name}");
                id++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ Finished generating Item IDs!");
    }
}