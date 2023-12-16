using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CheckIfAnyoneWonRound : MonoBehaviour
{
    public TextMeshProUGUI WinText;
    public TextMeshProUGUI WhoWonRound;

    // ten skrypt jest po to, �eby na WaitingCamera nie by�o napisu, kiedy jest on niepotrzebny
    // je�eli gracz rozpocz�� gr� jako widz i patrzy z widoku WaitingCamera, to nie b�dzie widzia� ca�y czas napisu "Rund� wygra�o"

    private void Update()
    {
        if (WhoWonRound.text == "")
        {
            WinText.text = "";
        } else
        {
            WinText.text = "Rund� wygra�o";
        }
    }
}
