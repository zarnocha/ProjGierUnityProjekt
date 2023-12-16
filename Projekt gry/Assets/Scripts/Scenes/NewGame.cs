using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NewGame : MonoBehaviour
{
    public TMPro.TMP_Dropdown TeamChoice;

    public TMPro.TMP_InputField RoundLimit;

    public TMPro.TMP_InputField RoundTimeMinutes;
    public TMPro.TMP_InputField RoundTimeSeconds;

    public TMPro.TMP_InputField TimeBetweenRoundsMinutes;
    public TMPro.TMP_InputField TimeBetweenRoundsSeconds;

    public TMPro.TMP_InputField BotAmountCT;
    public TMPro.TMP_InputField BotAmountTT;

    public Toggle FriendlyFire;

    private void Awake()
    {
        TeamChoice.value = 1;
        Cursor.lockState = CursorLockMode.Confined;

        FriendlyFire.isOn = PlayerPrefs.GetInt("FriendlyFire", 0) != 0;
    }

    public void OnChangeTeamChoice() 
    {
        int selectedValue = TeamChoice.value;

        PlayerPrefs.SetString("playerTeamChoice", TeamChoice.options[selectedValue].text);
    }

    public void OnChangeRoundLimit()
    {
        int setValue = Int32.Parse(RoundLimit.text);
        PlayerPrefs.SetInt("RoundLimit", setValue);
    }
    public void ValidateRoundLimit()
    {
        int newValue;
        try
        {
            newValue = Int32.Parse(RoundLimit.text);

            if (newValue == 0)
            {
                RoundLimit.text = "1";
            }
            if (newValue > 999)
            {
                RoundLimit.text = "999";
            }
        }
        catch (FormatException)
        {
            RoundLimit.text = "1";
        }
    }

    public void OnChangeRoundTimeMinutes()
    {
        int setValue = Int32.Parse(RoundTimeMinutes.text);
        PlayerPrefs.SetInt("RoundTimeMinutes", setValue);
    }
    public void ValidateRoundTimeMinutes()
    {
        int newValue;
        try
        {
            newValue = Int32.Parse(RoundTimeMinutes.text);
            int secondsValue = Int32.Parse(RoundTimeSeconds.text);

            if (newValue == 0 && secondsValue == 0)
            {
                RoundTimeMinutes.text = "1";
            }
            if (newValue > 15)
            {
                RoundTimeMinutes.text = "15";
            }
        }
        catch (FormatException)
        {
            RoundTimeMinutes.text = "1";
        }
    }

    public void OnChangeRoundTimeSeconds()
    {
        int setValue = Int32.Parse(RoundTimeSeconds.text);
        PlayerPrefs.SetInt("RoundTimeSeconds", setValue);
    }
    public void ValidateRoundTimeSeconds()
    {
        int newValue;
        try
        {
            newValue = Int32.Parse(RoundTimeSeconds.text);
            int minutesValue = Int32.Parse(RoundTimeMinutes.text);

            if (newValue == 0 && minutesValue == 0)
            {
                RoundTimeSeconds.text = "30";
            }
            if (newValue > 59)
            {
                RoundTimeSeconds.text = "59";
            }
        }
        catch (FormatException)
        {
            RoundTimeSeconds.text = "30";
        }
    }

    public void OnChangeTimeBetweenRoundsMinutes()
    {
        int setValue = Int32.Parse(TimeBetweenRoundsMinutes.text);
        PlayerPrefs.SetInt("TimeBetweenRoundsMinutes", setValue);
    }
    public void ValidateTimeBetweenRoundsMinutes()
    {
        int newValue;
        try
        {
            newValue = Int32.Parse(TimeBetweenRoundsMinutes.text);
            int secondsValue = Int32.Parse(TimeBetweenRoundsSeconds.text);

            if (newValue == 0 && secondsValue == 0)
            {
                TimeBetweenRoundsMinutes.text = "1";
            }
            if (newValue > 5)
            {
                TimeBetweenRoundsMinutes.text = "5";
            }
        }
        catch (FormatException)
        {
            TimeBetweenRoundsMinutes.text = "1";
        }
    }
    public void OnChangeTimeBetweenRoundsSeconds()
    {
        int setValue = Int32.Parse(TimeBetweenRoundsSeconds.text);
        PlayerPrefs.SetInt("TimeBetweenRoundsSeconds", setValue);
    }
    public void ValidateTimeBetweenRoundsSeconds()
    {
        int newValue;
        try
        {
            newValue = Int32.Parse(TimeBetweenRoundsSeconds.text);
            int minutesValue = Int32.Parse(TimeBetweenRoundsMinutes.text);

            if (newValue == 0 && minutesValue == 0)
            {
                TimeBetweenRoundsSeconds.text = "5";
            }
            if (newValue > 59)
            {
                TimeBetweenRoundsSeconds.text = "59";
            }
        }
        catch (FormatException)
        {
            TimeBetweenRoundsSeconds.text = "5";
        }
    }
    public void OnChangeBotAmountCT()
    {
        int setValue = Int32.Parse(BotAmountCT.text);
        PlayerPrefs.SetInt("CtPlayersAmount", setValue);
    }
    
    public void ValidateBotAmountCT()
    {
        int newValue;
        try
        {
            newValue = Int32.Parse(BotAmountCT.text);

            if (newValue < 0)
            {
                BotAmountCT.text = "0";
            }
            if (newValue > 10)
            {
                BotAmountCT.text = "10";
            }
        }
        catch (FormatException)
        {
            BotAmountCT.text = "0";
        }
    }
    public void OnChangeBotAmountTT()
    {
        int setValue = Int32.Parse(BotAmountTT.text);
        PlayerPrefs.SetInt("TtPlayersAmount", setValue);
    }

    public void ValidateBotAmountTT()
    {
        int newValue;
        try
        {
            newValue = Int32.Parse(BotAmountTT.text);

            if (newValue < 0)
            {
                BotAmountTT.text = "0";
            }
            if (newValue > 10)
            {
                BotAmountTT.text = "10";
            }
        }
        catch (FormatException)
        {
            BotAmountTT.text = "0";
        }
    }

    public void OnChangeFriendlyFire()
    {
        bool selectedValue = FriendlyFire.isOn;

        PlayerPrefs.SetInt("FriendlyFire", selectedValue ? 1 : 0);
    }

    public void StartGame()
    {
        int teamChoiceValue = TeamChoice.value;
        PlayerPrefs.SetString("playerTeamChoice", TeamChoice.options[teamChoiceValue].text);

        if (RoundLimit.text == "")
        {
            PlayerPrefs.SetInt("RoundLimit", 15);
        } 
        else
        {
            PlayerPrefs.SetInt("RoundLimit", Int32.Parse(RoundLimit.text));
        }

        if (RoundTimeMinutes.text == "")
        {
            PlayerPrefs.SetInt("RoundTimeMinutes", 3);
        } 
        else
        {
            PlayerPrefs.SetInt("RoundTimeMinutes", Int32.Parse(RoundTimeMinutes.text));
        }

        if (RoundTimeSeconds.text == "")
        {
            PlayerPrefs.SetInt("RoundTimeSeconds", 0);
        }
        else
        {
            PlayerPrefs.SetInt("RoundTimeSeconds", Int32.Parse(RoundTimeSeconds.text));
        }

        if (TimeBetweenRoundsMinutes.text == "")
        {
            PlayerPrefs.SetInt("TimeBetweenRoundsMinutes", 0);
        }
        else
        {
            PlayerPrefs.SetInt("TimeBetweenRoundsMinutes", Int32.Parse(TimeBetweenRoundsMinutes.text));
        }

        if (TimeBetweenRoundsSeconds.text == "")
        {
            PlayerPrefs.SetInt("TimeBetweenRoundsSeconds", 5);
        }
        else
        {
            PlayerPrefs.SetInt("TimeBetweenRoundsSeconds", Int32.Parse(TimeBetweenRoundsSeconds.text));
        }

        if (BotAmountCT.text == "")
        {
            PlayerPrefs.SetInt("CtPlayersAmount", 5);
        }
        else
        {
            PlayerPrefs.SetInt("CtPlayersAmount", Int32.Parse(BotAmountCT.text));
        }
        
        if (BotAmountTT.text == "")
        {
            PlayerPrefs.SetInt("TtPlayersAmount", 4);
        }
        else
        {
            PlayerPrefs.SetInt("TtPlayersAmount", Int32.Parse(BotAmountTT.text));
        }

        SceneManager.LoadScene(2);
    }

    public void GoBackToMainMenu()
    {
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
}
