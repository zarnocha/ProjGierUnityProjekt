using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public int HP = 100;
    public int MaxHP = 100;
    public int MinHP = 0;

    public int damage = 10;
    public float shootingDelay = 0.05f;

    [Tooltip("prefab zw�ok postaci")]
    public GameObject deadBody;

    [Tooltip("audio strza��w broni")]
    public AudioClip weaponClip;

    public Camera playerCamera;
    private float lastShoot = 0;

    private Animator unitAnimator;
    private int howManyBulletsFired = 0;

    bool deadAnimationEnded = false;

    public bool isShooting = false;

    private GameObject gameScriptObject;
    private GameScript scriptOfGame;

    private TextMeshProUGUI HpText;
    private TextMeshProUGUI BulletsLeftText;

    void Start()
    {
        unitAnimator = GetComponent<Animator>();
        playerCamera = GetComponentInChildren<Camera>();

        FindHPOnUI();
        FindBulletsLeftOnUI();

        // szukamy obiektu gry i pobieramy jego skrypt, �eby mie� dost�p do funkcji gry
        gameScriptObject = GameObject.FindGameObjectWithTag("GameScript");
        scriptOfGame = gameScriptObject.GetComponent<GameScript>();
    }

    void Update()
    {
        SetHPOnUi();
        SetBulletsLeftOnUi();
        
        if (HP <= MinHP)
        {
            // je�eli gracz ma mniej HP ni� MinHP to uruchamiamy animacj� umierania
            unitAnimator.SetBool("isDead", true);

            // je�eli animacja umierania si� zako�czy (wywo�any Animation Event, wywo�uj�cy `SetDeadAnimationEnd()`), to
            // na jego miejsce �mierci k�adziemy zw�oki i przekazujemy je do listy skryptu gry
            if (deadAnimationEnded)
            {
                GameObject deadBodyGameObject = Instantiate(deadBody, transform.position, transform.rotation);
                scriptOfGame.AddDeadBodyToList(deadBodyGameObject);
                Destroy(transform.gameObject);
            }
        }
    }

    private void LateUpdate()
    {
        if (isShooting)
        {
            // zabezpieczenie, aby strza�y nie zosta�y wystrzelone w jednym momencie przy trzymaniu LPM
            // innymi s�owy - co ile czasu ma nast�powa� strza�

            if (Time.time >= lastShoot)
            {
                Fire();
                AudioSource.PlayClipAtPoint(weaponClip, transform.position);
                unitAnimator.SetBool("isShooting", true);
                lastShoot = Time.time + shootingDelay;

                AddRecoilToPlayer();

                howManyBulletsFired++;
            }
        }
        else
        {
            howManyBulletsFired = 0;
            unitAnimator.SetBool("isShooting", false);
        }

        if (howManyBulletsFired >= 30)
        {
            // TODO: reloading
            howManyBulletsFired = 0;
        }

    }
    private void Fire()
    {
        RaycastHit ray;
        Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out ray);

        // w celu sprawdzenia gdzie strzelaj� boty
        //if (!transform.CompareTag("TerroristPlayer") && !transform.CompareTag("CounterTerroristPlayer"))
        //{
        //    Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * 1000f, Color.red, .5f);
        //} 

        if (ray.collider)
        {
            string whoIsShooting = "Not a player";
            if (transform.CompareTag("Terrorist") || transform.CompareTag("TerroristPlayer")) whoIsShooting = "Terrorist";
            if (transform.CompareTag("CounterTerrorist") || transform.CompareTag("CounterTerroristPlayer")) whoIsShooting = "CounterTerrorist";

            string whoIsShotAt = "Not a player";
            if (ray.transform.CompareTag("Terrorist") || ray.transform.CompareTag("TerroristPlayer")) whoIsShotAt = "Terrorist";
            if (ray.transform.CompareTag("CounterTerrorist") || ray.transform.CompareTag("CounterTerroristPlayer")) whoIsShotAt = "CounterTerrorist";

            Unit whoIsShotAtUnit = ray.transform.GetComponent<Unit>();
            bool isFriendlyFire = scriptOfGame.isFriendlyFire;

            // zabezpieczenie przed tym, �eby obiekt nie b�d�cy ani graczem ani botem nie m�g� zabija�
            // doda�em opcj� friendly fire, �eby mo�na by�o rani� swoich cz�onk�w dru�yny
            if (whoIsShooting != "Not a player" && whoIsShotAt != "Not a player" && (isFriendlyFire || whoIsShooting != whoIsShotAt))
            {
                if (whoIsShotAtUnit.HP > MinHP)
                {
                    whoIsShotAtUnit.DealDamage(damage);
                }
            }
        }
    }


    public void DealDamage(int damage)
    {
        HP -= damage;
        //Debug.Log("Zadano damage: " + damage + "\n HP: " + HP);
    }

    public void SetDeadAnimationEnd()
    {
        deadAnimationEnded = true;
    }

    private void FindHPOnUI()
    {
        bool found = false;
        foreach (Transform child in playerCamera.transform)
        {
            if (child.CompareTag("UI"))
            {
                foreach (Transform childForHP in child)
                {
                    if (childForHP.CompareTag("HP"))
                    {
                        HpText = childForHP.GetComponent<TextMeshProUGUI>();
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
        }
    }

    private void SetHPOnUi()
    {
        HpText.text = HP.ToString();
    }

    private void FindBulletsLeftOnUI()
    {
        bool found = false;
        foreach (Transform child in playerCamera.transform)
        {
            if (child.CompareTag("UI"))
            {
                foreach (Transform childForBulletsLeft in child)
                {
                    if (childForBulletsLeft.CompareTag("BulletsLeft"))
                    {
                        BulletsLeftText = childForBulletsLeft.GetComponent<TextMeshProUGUI>();
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
        }
    }

    private void SetBulletsLeftOnUi()
    {
        BulletsLeftText.text = howManyBulletsFired.ToString();
    }

    public void AddRecoilToBot()
    {
        if (transform.CompareTag("Terrorist") || transform.CompareTag("CounterTerrorist"))
        {
            transform.GetComponent<BotMove>().botSpineVerticalRotation -= Random.Range(howManyBulletsFired/15f, howManyBulletsFired/10f);
            transform.Rotate(new Vector3(0f, Random.Range(-howManyBulletsFired / 40f, howManyBulletsFired / 40f), 0f));
        }
    }

    public void AddRecoilToPlayer()
    {
        if (transform.CompareTag("TerroristPlayer") || transform.CompareTag("CounterTerroristPlayer"))
        {
            if (howManyBulletsFired > 2)
            {
                transform.GetComponent<PlayerMove>().playerSpineVerticalRotation -= Random.Range(0.5f, 0.6f);
                transform.Rotate(new Vector3(0f, Random.Range(-0.2f, 0.2f), 0f));
            }
            else
            {
                transform.GetComponent<PlayerMove>().playerSpineVerticalRotation -= Random.Range(0.2f, 0.3f);
                transform.Rotate(new Vector3(0f, Random.Range(-0.1f, 0.1f), 0f));
            }
        }
    }
}
