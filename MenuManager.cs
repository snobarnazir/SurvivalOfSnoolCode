using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject playerModePanel;

    void Start()
    {
        mainMenuPanel.SetActive(true);
        playerModePanel.SetActive(false);
    }

    public void OpenPlayerModePanel()
    {
        mainMenuPanel.SetActive(false);
        playerModePanel.SetActive(true);
    }

    public void ClosePlayerModePanel()
    {
        playerModePanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void PlaySinglePlayer()
    {
        SceneManager.LoadScene("MazeLevel1");
    }

    public void OpenMultiplayerOptions()
    {
        Debug.Log("Multiplayer selected - coming soon");
    }

    public void QuitGame()
    {
        Debug.Log("Quit button pressed");
        Application.Quit();
    }
}