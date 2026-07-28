using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingSceneController : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(LoadGameScene());
    }

    private IEnumerator LoadGameScene()
    {
        // Optional: fake loading time so player sees screen
        yield return new WaitForSeconds(5f);

        // Load the actual game scene
        AsyncOperation operation = SceneManager.LoadSceneAsync("Palipi Bay");

        // Wait until fully loaded
        while (!operation.isDone)
        {
            yield return null;
        }
    }
}