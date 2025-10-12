using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    // Loads the first level or main gameplay scene
    public void PlayGame()
    {
        // Make sure "test" is spelled exactly like your scene name
        SceneManager.LoadScene("test");
    }

    // Opens the settings menu (replace with actual logic later)
    public void OpenSettings()
    {
        Debug.Log("Settings button pressed!");
        // SceneManager.LoadScene("Settings"); // Uncomment if you add a settings scene
    }

    // Quits the game
    public void QuitGame()
    {
        Debug.Log("Quit button pressed!");
        Application.Quit();

#if UNITY_EDITOR
        // This makes Quit work in the Unity editor for testing
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
