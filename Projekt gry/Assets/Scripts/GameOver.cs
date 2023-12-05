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

    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;

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
            WinText.text = "Zwyciê¿y³ zespó³";
            WhoWon.text = "CT";
            WhoWon.color = new Color32(39, 88, 224, 255);
        }
        else if (WhoWonAsString == "TT")
        {
            WinText.text = "Zwyciê¿y³ zespó³";
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

    private void OnDisable()
    {
        PlayerPrefs.DeleteAll();
    }
}
