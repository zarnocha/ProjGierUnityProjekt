using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    public TextMeshProUGUI WinText;
    public TextMeshProUGUI Draw;
    public TextMeshProUGUI WhoWon;
    public TextMeshProUGUI ScoreCT;
    public TextMeshProUGUI ScoreTT;

    private void Awake()
    {
        // odtwarzanie muzyki, gdy gra siê zakoñczy
        if (GameObject.FindGameObjectWithTag("Music"))
        {
            GameObject.FindGameObjectWithTag("Music").GetComponent<PlayMusicThroughScenes>().PlayMusic();
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

        string WhoWonAsString = PlayerPrefs.GetString("WhoWon");
        int ScoreCTAsInt = PlayerPrefs.GetInt("ScoreCT");
        int ScoreTTAsInt = PlayerPrefs.GetInt("ScoreTT");

        ScoreCT.text = ScoreCTAsInt.ToString();
        ScoreTT.text = ScoreTTAsInt.ToString();

        if (WhoWonAsString == "Remis")
        {
            Draw.text = "Remis";
        }
        else if (WhoWonAsString == "CT")
        {
            WinText.text = "Wygrana";
            WhoWon.text = "CT";
            WhoWon.color = new Color32(39, 88, 224, 255);
        }
        else if (WhoWonAsString == "TT")
        {
            WinText.text = "Wygrana";
            WhoWon.text = "TT";
            WhoWon.color = new Color32(224, 39, 39, 255);
        }
    }

    public void GoBackToMainMenu()
    {
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }
}
