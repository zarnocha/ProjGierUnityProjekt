using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject musicInstance;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

        if (GameObject.FindWithTag("Music") == null)
        {
            // odtwarzanie muzyki po wejœciu do gry i zabezpieczenie przed tym, ¿eby nie powstawa³y nowe instancje
            Instantiate(musicInstance);
        }
        GameObject.FindWithTag("Music").GetComponent<PlayMusicThroughScenes>().PlayMusic();
    }

    public void OnPlayButton()
    {
        SceneManager.LoadScene(1);
    }

    public void OnSettingsButton()
    {
        SceneManager.LoadScene(4);
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }
}
