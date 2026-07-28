using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    // Change this to your actual menu scene name
    [SerializeField] private string menuSceneName = "MenuScene";

    void Start()
    {
        pausePanel.SetActive(false);
    }

    public void OnPauseClick()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void OnResumeClick()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // MAIN MENU BUTTON
    public void OnMainMenuClick()
    {
        Time.timeScale = 1f;

        // Destroy persistent singletons so they don't follow the player into the Main Menu
        if (PlayerController.Instance != null) Destroy(PlayerController.Instance.gameObject);
        if (InputHandler.Instance != null) Destroy(InputHandler.Instance.gameObject);
        if (NarrativeStateManager.Instance != null) Destroy(NarrativeStateManager.Instance.gameObject);

        // Clear static data so a new game doesn't inherit destroyed objects from the old one
        SaveController.ClearStaticData();

        SceneManager.LoadScene(menuSceneName);
    }

    // EXIT BUTTON
    public void OnExitClick()
    {
        Application.Quit();

        // Only works in Unity Editor
        Debug.Log("Game Closed");
    }
}