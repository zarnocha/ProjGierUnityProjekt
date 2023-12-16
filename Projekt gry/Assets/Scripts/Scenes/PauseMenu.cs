using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject settingsInGame;
    public static bool isPaused = false;

    void Start()
    {
        pauseMenu.SetActive(false);
    }

   void Update() 
    {
        // to jest w update bo gra blokowa³a kursor po klikniêciu
        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            } else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame() 
    {
        if (GameObject.FindGameObjectWithTag("Music"))
        {
            GameObject.FindGameObjectWithTag("Music").GetComponent<PlayMusicThroughScenes>().PlayMusic();
        }

        isPaused = true;
        pauseMenu.SetActive(true);
        Time.timeScale = 0;       
    }
    public void ResumeGame() 
    {
        if (GameObject.FindGameObjectWithTag("Music"))
        {
            GameObject.FindGameObjectWithTag("Music").GetComponent<PlayMusicThroughScenes>().StopMusic();
        }

        isPaused = false;
        pauseMenu.SetActive(false);
        gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1;
    }

    public void BackToMainMenu()
    {
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene(0);
    }

    public void OnSettingsButton()
    {
        gameObject.SetActive(false);
        settingsInGame.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
