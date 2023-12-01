using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public int HP = 100;
    public int MaxHP = 100;
    public int MinHP = 0;

    public int damage = 10;
    public float shootingDelay = 0.05f;

    [Tooltip("prefab zw³ok postaci")]
    public GameObject deadBody;
    [Tooltip("audio strza³ów broni")]
    public AudioSource shootingSound;

    private Camera playerCamera;
    private float lastShoot = 0;

    private Animator unitAnimator;
    private int howManyBulletsFired = 0;

    bool deadAnimationEnded = false;

    public bool isShooting = false;

    private GameObject gameScriptObject;
    private GameScript scriptOfGame;

    void Start()
    {
        unitAnimator = GetComponent<Animator>();
        playerCamera = GetComponentInChildren<Camera>();

        // szukamy obiektu gry i pobieramy jego skrypt, ¿eby mieæ dostêp do funkcji gry
        gameScriptObject = GameObject.FindGameObjectWithTag("GameScript");
        scriptOfGame = gameScriptObject.GetComponent<GameScript>();
    }

    void Update()
    {
        if (isShooting)
        {
            // zabezpieczenie, aby strza³y nie zosta³y wystrzelone w jednym momencie przy trzymaniu LPM
            // innymi s³owy - co ile czasu ma nastêpowaæ strza³
            // TODO: dodaæ reloading

            if (Time.time >= lastShoot)
            {
                Fire();
                shootingSound.Play();
                unitAnimator.SetBool("isShooting", true);
                lastShoot = Time.time + shootingDelay;

                // je¿eli gracz wystrzeliwuje pociski ca³¹ seri¹ (wiêcej ni¿ 2),
                // to bêdzie mu ciê¿ej trafiæ poprzez 'odrzut'
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
                howManyBulletsFired++;
            }
        } 
        else
        {
            howManyBulletsFired = 0;
            unitAnimator.SetBool("isShooting", false);
        }
        
        if (HP <= MinHP)
        {
            // je¿eli gracz ma mniej HP ni¿ MinHP to uruchamiamy animacjê umierania
            unitAnimator.SetBool("isDead", true);

            // je¿eli animacja umierania siê zakoñczy (wywo³any Animation Event, wywo³uj¹cy `SetDeadAnimationEnd()`), to
            // na jego miejsce œmierci k³adziemy zw³oki i przekazujemy je do listy skryptu gry
            if (deadAnimationEnded)
            {
                GameObject deadBodyGameObject = Instantiate(deadBody, transform.position, transform.rotation);
                scriptOfGame.AddDeadBodyToList(deadBodyGameObject);
            }

        }
    }

    private void Fire()
    {
        RaycastHit ray;
        Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out ray);
        
        // w celu sprawdzenia gdzie strzelaj¹ boty
        //if (!transform.CompareTag("TerroristPlayer") && !transform.CompareTag("CounterTerroristPlayer"))
        //{
            // Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * 1000f, Color.red);
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

            // zabezpieczenie przed tym, ¿eby obiekt nie bêd¹cy ani graczem ani botem nie móg³ zabijaæ
            // doda³em opcjê friendly fire, ¿eby mo¿na by³o raniæ swoich cz³onków dru¿yny
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
        Debug.Log("Zadano damage: " + damage + "\n HP: " + HP);
    }

    public void SetDeadAnimationEnd()
    {
        deadAnimationEnded = true;
    }
}
