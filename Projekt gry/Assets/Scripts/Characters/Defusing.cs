using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Defusing : MonoBehaviour
{
    /// <summary>
    /// Bomba jest rozbrojona
    /// </summary>
    public static bool isDefused = false;

    /// <summary>
    /// Bomba jest rozbrajana przez jakiegokolwiek gracza (global scope)
    /// </summary>
    public static bool isBeingDefused = false;

    /// <summary>
    /// Pozycja bomby na mapie
    /// </summary>
    public static Vector3 bombPosition;

    /// <summary>
    /// Czy gracz mo¿e rozbrajaæ bombê
    /// </summary>
    public bool canDefuse = false;

    /// <summary>
    /// Czy bomba jest rozbrajana przez gracza podpiêtego do skryptu
    /// </summary>
    public bool isDefusing = false;

    /// <summary>
    /// Czy gracz ma zestaw do rozbrajania
    /// </summary>
    public bool hasDefuseKit = false;

    public GameObject BombsiteA;
    public GameObject BombsiteB;

    public Image defuseKitIcon;

    public GameScript gameScript;

    /// <summary>
    /// Czas rozbrajania bomby - 10 sekund
    /// </summary>
    public float defusingTime = 10f;
    public float defusingTimeCountdown;

    //private Slider defusingSlider;
    public float distanceToBomb;

    void Start()
    {
        defusingTimeCountdown = defusingTime;
        gameScript = FindAnyObjectByType<GameScript>();

        //if (GameObject.FindGameObjectWithTag("DefusingSlider"))
        //{
        //    defusingSlider = GameObject.FindGameObjectWithTag("DefusingSlider").GetComponent<Slider>();
        //    defusingSlider.GetComponent<DefusingSlider>().ToggleSliderVisibility(false);
        //}

        // jeœli antyterrorysta ma zestaw do rozbrajania to zajmuje to po³owê mniej czasu
        if (hasDefuseKit)
        {
            defusingTime = 5f;
            defusingTimeCountdown = defusingTime;
        }
    }

    void Update()
    {
        ToggleDefuseKitIcon(hasDefuseKit);

        if (PlantingBomb.isPlanted)
        {
            bombPosition = PlantingBomb.bombPosition;
            distanceToBomb = Vector3.Distance(bombPosition, transform.position);
        }

        if (distanceToBomb > 0 && distanceToBomb < 3)
        {
            canDefuse = true;
        }

        //if (isDefusing)
        //{
        //    defusingSlider.GetComponent<DefusingSlider>().IncrementProgress(Time.deltaTime);
        //}
    }

    public void DefusingBomb()
    {
        if (!canDefuse) return;

        //if (defusingSlider.GetComponent<DefusingSlider>())
        //{
        //    defusingSlider.GetComponent<DefusingSlider>().ToggleSliderVisibility(true);
        //}

        isDefusing = true;
        isBeingDefused = true;
        if (defusingTimeCountdown > 0)
        {
            defusingTimeCountdown -= Time.deltaTime;
        }
        else
        {
            Defused();
        }
    }

    public void InterruptedDefusing()
    {
        isDefusing = false;
        isBeingDefused = false;
        defusingTimeCountdown = defusingTime;
    }

    public void Defused()
    {
        PlantingBomb.isPlanted = false;
        isDefused = true;
        hasDefuseKit = false;
        isDefusing = false;
        canDefuse = false;

        //if (defusingSlider.GetComponent<DefusingSlider>())
        //{
        //    defusingSlider.GetComponent<DefusingSlider>().ToggleSliderVisibility(false);
        //    defusingSlider.GetComponent<DefusingSlider>().SetProgressToZero();
        //}

        PlantingBomb.DestroyPlantedBomb();
    }

    private void ToggleDefuseKitIcon(bool state)
    {
        defuseKitIcon.enabled = state;
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
}
