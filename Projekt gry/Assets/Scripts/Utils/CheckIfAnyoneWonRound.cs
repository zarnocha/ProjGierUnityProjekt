using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CheckIfAnyoneWonRound : MonoBehaviour
{
    public TextMeshProUGUI WinText;
    public TextMeshProUGUI WhoWonRound;

    // ten skrypt jest po to, ¿eby na WaitingCamera nie by³o napisu, kiedy jest on niepotrzebny
    // je¿eli gracz rozpocz¹³ grê jako widz i patrzy z widoku WaitingCamera, to nie bêdzie widzia³ ca³y czas napisu "Rundê wygra³o"

    private void Update()
    {
        if (WhoWonRound.text == "")
        {
            WinText.text = "";
        } else
        {
            WinText.text = "Rundê wygra³o";
        }
    }
}
