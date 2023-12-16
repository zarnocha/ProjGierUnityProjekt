using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BombAndDefuserOnUI : MonoBehaviour
{
    /// <summary>
    /// Ikona bomby lub defusera na UI
    /// </summary>
    public Image DefuserOrBombIcon;

    void Update()
    {
        // Wyszukujemy kto jest w³aœcicielem ikony
        GameObject Terrorist = FindParentWithTag(gameObject, "Terrorist");
        GameObject TerroristPlayer = FindParentWithTag(gameObject, "TerroristPlayer");

        if (Terrorist != null)
        {
            PlantingBomb terroristPlantingBomb = Terrorist.GetComponent<PlantingBomb>();
            if (terroristPlantingBomb.hasBomb)
            {
                ToggleIcon(true);
            }
            else
            {
                ToggleIcon(false);
            }
        }
        if (TerroristPlayer != null)
        {
            PlantingBomb terroristPlantingBomb = TerroristPlayer.GetComponent<PlantingBomb>();

            if (terroristPlantingBomb.hasBomb)
            {
                ToggleIcon(true);
            }
            else
            {
                ToggleIcon(false);
            }
        }

        GameObject CounterTerrorist = FindParentWithTag(gameObject, "CounterTerrorist");
        GameObject CounterTerroristPlayer = FindParentWithTag(gameObject, "CounterTerroristPlayer");

        if (CounterTerrorist != null)
        {
            Defusing counterTerroristDefusing = CounterTerrorist.GetComponent<Defusing>();

            if (counterTerroristDefusing.hasDefuseKit)
            {
                ToggleIcon(true);
            }
            else
            {
                ToggleIcon(false);
            }
        }
        if (CounterTerroristPlayer != null)
        {
            Defusing counterTerroristDefusing = CounterTerroristPlayer.GetComponent<Defusing>();

            if (counterTerroristDefusing.hasDefuseKit)
            {
                ToggleIcon(true);
            }
            else
            {
                ToggleIcon(false);
            }
        }
    }
    public static GameObject FindParentWithTag(GameObject childObject, string tag)
    {
        Transform t = childObject.transform;
        while (t.parent != null)
        {
            if (t.parent.CompareTag(tag))
            {
                return t.parent.gameObject;
            }
            t = t.parent.transform;
        }
        return null;
    }

    /// <summary>
    /// Funkcja do w³¹czania/ wy³¹czania widocznoœci ikony na UI
    /// </summary>
    /// <param name="state">Czy ikona ma byæ w³¹czona czy nie</param>
    private void ToggleIcon(bool state)
    {
        DefuserOrBombIcon.enabled = state;
    }
}
