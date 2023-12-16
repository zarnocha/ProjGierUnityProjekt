using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlantingBomb : MonoBehaviour
{
    public static bool isPlanted = false;
    public static Vector3 bombPosition;
    public bool canPlant = false;
    public bool isPlanting = false;
    public bool hasBomb = false;

    public GameObject BombsiteA;
    public GameObject BombsiteB;

    public Image bombIcon;
    public GameObject bombPrefab;
    public static GameObject plantedBomb;

    private Animator animator;

    public GameScript gameScript;

    /// <summary>
    /// Czas podk³adania bomby - 5 sekund
    /// </summary>
    public float plantingTime = 5f;
    public float plantingTimeCountdown;

    private Slider plantingSlider;

    void Start()
    {
        animator = GetComponent<Animator>();
        plantingTimeCountdown = plantingTime;
        gameScript = FindAnyObjectByType<GameScript>();

        if (GameObject.FindGameObjectWithTag("PlantingSlider"))
        {
            plantingSlider = GameObject.FindGameObjectWithTag("PlantingSlider").GetComponent<Slider>();
            //plantingSlider.GetComponent<PlantingSlider>().ToggleSliderVisibility(false);
        }
        BombsiteA = gameScript.BombsiteA;
        BombsiteB = gameScript.BombsiteB;

        ShowWeaponHideBomb();
    }

    void Update()
    {
        ToggleBombIcon(hasBomb);

        //if (isPlanting)
        //{
            //plantingSlider.GetComponent<PlantingSlider>().IncrementProgress(Time.deltaTime);
        //}
    }

    public void Planting()
    {
        if (!canPlant || !hasBomb) return;

        HideWeaponShowBomb();

        //if (plantingSlider)
        //{
        //  plantingSlider.GetComponent<PlantingSlider>().ToggleSliderVisibility(true);
        //}

        isPlanting = true;
        animator.SetBool("isPlanting", isPlanting);
        if (plantingTimeCountdown > 0)
        {
            plantingTimeCountdown -= Time.deltaTime;
        }
        else
        {
            Planted();
        }
    }

    public void InterruptedPlanting()
    {
        ShowWeaponHideBomb();
        isPlanting = false;
        plantingTimeCountdown = plantingTime;
        animator.SetBool("isPlanting", isPlanting);
    }

    public void Planted()
    {
        ShowWeaponHideBomb();
        isPlanted = true;
        hasBomb = false;
        canPlant = false;

        isPlanting = false;
        animator.SetBool("isPlanting", isPlanting);

        //if (plantingSlider.GetComponent<PlantingSlider>())
        //{
            //plantingSlider.GetComponent<PlantingSlider>().ToggleSliderVisibility(false);
            //plantingSlider.GetComponent<PlantingSlider>().SetProgressToZero();
        //}

        plantedBomb = Instantiate(bombPrefab, new Vector3(transform.position.x, transform.position.y + 0.04f, transform.position.z), Quaternion.identity);
        bombPosition = plantedBomb.transform.position;

        GameScript.chosenBombsite = WhereIsBombPlanted();
    }

    public static void DestroyPlantedBomb()
    {
        if (plantedBomb != null)
        {
            Destroy(plantedBomb);
        }

        bombPosition = Vector3.zero;
    }

    /// <summary>
    /// Funkcja oblicza dystans od bomby do miejsca jej pod³o¿enia i na podstawie dystansu zwraca miejsce gdzie le¿y bomba
    /// </summary>
    /// <returns>GameObject na którym le¿y bomba</returns>
    public GameObject WhereIsBombPlanted()
    {
        float distanceToA = Vector3.Distance(plantedBomb.transform.position, BombsiteA.transform.position);
        float distanceToB = Vector3.Distance(plantedBomb.transform.position, BombsiteB.transform.position);

        return (distanceToA < distanceToB) ? BombsiteA : BombsiteB;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bombsite") && hasBomb)
        {   
            canPlant = true;
            Debug.Log("Terrorysta mo¿e podk³adaæ bombê");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Bombsite") && hasBomb)
        {
            canPlant = false;
            isPlanting = false;
            plantingTimeCountdown = plantingTime;
            Debug.Log("Terrorysta nie mo¿e pod³o¿yæ bomby");
        }
    }

    private void ToggleBombIcon(bool state)
    {
        bombIcon.enabled = state;
    }
    private void ToggleBombState(bool state)
    {
        isPlanted = state;
    }

    public void HideWeaponShowBomb()
    {
        GameObject weapon = GameObject.FindWithTag("Weapon");
        weapon.GetComponent<Renderer>().enabled = false;

        GameObject bomb = GameObject.FindWithTag("Bomb");
        bomb.GetComponent<Renderer>().enabled = true;
    }

    public void ShowWeaponHideBomb()
    {
        GameObject weapon = GameObject.FindWithTag("Weapon");
        weapon.GetComponent<Renderer>().enabled = true;

        GameObject bomb = GameObject.FindWithTag("Bomb");
        bomb.GetComponent<Renderer>().enabled = false;
    }

}
