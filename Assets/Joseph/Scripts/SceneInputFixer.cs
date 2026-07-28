using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SceneInputFixer : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 🔁 Re-enable PlayerInput (this is the REAL fix)
        var playerInput = FindFirstObjectByType<PlayerInput>();

        if (playerInput != null)
        {
            playerInput.enabled = false;
            playerInput.enabled = true;
        }

        // 🔁 Reset time scale just in case UI paused the game
        Time.timeScale = 1f;
    }
}