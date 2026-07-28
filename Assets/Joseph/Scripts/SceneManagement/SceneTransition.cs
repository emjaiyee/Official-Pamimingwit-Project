using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public string sceneToLoad;
    public string targetSpawnID;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Write the "note" before leaving
            SceneTransferData.TargetSpawnID = targetSpawnID;
            
            // Load the next scene
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
