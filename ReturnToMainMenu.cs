using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class ReturnToMainMenu : MonoBehaviour
{
    [Header("Settings")]
    public KeyCode exitKey = KeyCode.Escape;
    public string mainMenuSceneName = "MainMenu";

    private bool isReturning = false;

    private void Update()
    {
        if (Input.GetKeyDown(exitKey))
        {
            GoToMainMenu();
        }
    }

    public void GoToMainMenu()
    {
        if (isReturning) return;
        isReturning = true;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Reset briefing flags
        BriefingBox.ResetAllBriefings();

        // Reset save loading flag
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.IsLoadingSavedGame = false;
        }

        // Shutdown + DESTROY NetworkManager (critical)
        if (NetworkManager.Singleton != null)
        {
            NetworkManager nm = NetworkManager.Singleton;

            if (nm.IsListening)
            {
                nm.Shutdown();
            }

            // This makes next session truly fresh
            Destroy(nm.gameObject);
        }

        // Destroy other persistent objects if they exist
        DestroyIfExists("PersistentZombieManager");
        DestroyIfExists("PersistentWaveUI");

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void DestroyIfExists(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj != null)
        {
            Destroy(obj);
        }
    }

    private void DestroyPersistentObject(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj != null)
        {
            Destroy(obj);
        }
    }
}