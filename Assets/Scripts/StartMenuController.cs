using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    private string saveLocation;

    private void Awake()
    {
        saveLocation = System.IO.Path.Combine(Application.persistentDataPath, "saveData.json");
    }

    // 🟢 NEW GAME
    public void OnStartClick()
    {
        Debug.Log("START NEW GAME");

        SaveController.shouldLoadGame = false;
        SaveController.loadRequestedFromMenu = false;

        StartCoroutine(LoadSceneFlow());
    }

    // 🔵 LOAD GAME
    public void OnLoadClick()
    {
        Debug.Log("LOAD BUTTON CLICKED");

        // ❗ CHECK SAVE FIRST (THIS IS THE KEY FIX)
        if (!File.Exists(saveLocation))
        {
            Debug.LogWarning("NO SAVE FOUND → STAY IN MAIN MENU");
            return; // 🛑 STOP HERE, NO SCENE CHANGE
        }

        Debug.Log("SAVE FOUND → LOADING GAME");

        SaveController.shouldLoadGame = true;
        SaveController.loadRequestedFromMenu = true;

        StartCoroutine(LoadSceneFlow());
    }

    private System.Collections.IEnumerator LoadSceneFlow()
    {
        yield return null;
        SceneManager.LoadScene("LoadingScene");
    }

    public void OnExitClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}