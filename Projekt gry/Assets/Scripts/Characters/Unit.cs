using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour
{
    public int HP = 100;
    public int MaxHP = 100;
    public int MinHP = 0;

    public bool isDead = false;
    public bool isShooting = false;
    public bool isReloading = false;

    private int howManyBulletsFired = 0;
    public int Ammo = 30;

    public int damage = 10;
    public float shootingDelay = 0.05f;

    [Tooltip("prefab zw³ok postaci")]
    public GameObject deadBody;

    [Tooltip("audio strza³ów broni")]
    public AudioClip weaponClip;

    public Camera playerCamera;
    private float lastShoot = 0;

    private Animator unitAnimator;

    bool deadAnimationEnded = false;

    private GameObject gameScriptObject;
    private GameScript scriptOfGame;

    private TextMeshProUGUI HpText;
    private TextMeshProUGUI BulletsLeftText;

    private Slider reloadingSlider;
    //private Slider defusingSlider;
    //private Slider plantingSlider;

    void Start()
    {
        unitAnimator = GetComponent<Animator>();
        playerCamera = GetComponentInChildren<Camera>();

        if (GameObject.FindGameObjectWithTag("ReloadingSlider"))
        {
            reloadingSlider = GameObject.FindGameObjectWithTag("ReloadingSlider").GetComponent<Slider>();
            reloadingSlider.GetComponent<ReloadingSlider>().ToggleSliderVisibility(false);  
        }

        //if (GameObject.FindGameObjectWithTag("DefusingSlider"))
        //{
        //    defusingSlider = GameObject.FindGameObjectWithTag("DefusingSlider").GetComponent<Slider>();
        //    defusingSlider.GetComponent<DefusingSlider>().ToggleSliderVisibility(false);
        //}

        //if (GameObject.FindGameObjectWithTag("PlantingSlider"))
        //{
        //    plantingSlider = GameObject.FindGameObjectWithTag("PlantingSlider").GetComponent<Slider>();
        //    plantingSlider.GetComponent<PlantingSlider>().ToggleSliderVisibility(false);
        //}

        FindHPOnUI();
        FindBulletsLeftOnUI();

        // szukamy obiektu gry i pobieramy jego skrypt, ¿eby mieæ dostêp do funkcji gry
        gameScriptObject = GameObject.FindGameObjectWithTag("GameScript");
        scriptOfGame = gameScriptObject.GetComponent<GameScript>();
    }

    void Update()
    {
        if (PauseMenu.isPaused) return;

        SetHPOnUi();
        SetBulletsLeftOnUi();
         
        if (HP <= MinHP)
        {
            // je¿eli gracz ma mniej HP ni¿ MinHP to uruchamiamy animacjê umierania
            unitAnimator.SetBool("isDead", true);
            isDead = true;

            // je¿eli animacja umierania siê zakoñczy (wywo³any Animation Event, wywo³uj¹cy `SetDeadAnimationEnd()`), to
            // na jego miejsce œmierci k³adziemy zw³oki i przekazujemy je do listy skryptu gry
            if (deadAnimationEnded)
            {
                GameObject deadBodyGameObject = Instantiate(deadBody, transform.position, transform.rotation);
                scriptOfGame.AddDeadBodyToList(deadBodyGameObject);
                Destroy(transform.gameObject);
            }
        }

        // ¿eby terrorysta mia³ dzia³aj¹cy recoil (przez jego niedzia³aj¹cy spine w LateUpdate()) to musi byæ w Update(), a CT w LateUpdate()
        if (transform.CompareTag("Terrorist"))
        {
            if (isShooting && Ammo != 0 && !isReloading)
            {
                // zabezpieczenie, aby strza³y nie zosta³y wystrzelone w jednym momencie przy trzymaniu LPM
                // innymi s³owy - co ile czasu ma nastêpowaæ strza³

                if (Time.time >= lastShoot)
                {
                    Fire();
                    float volume = PlayerPrefs.GetFloat("Volume", 0.5f);
                    AudioSource.PlayClipAtPoint(weaponClip, transform.position, volume);
                    unitAnimator.SetBool("isShooting", true);
                    lastShoot = Time.time + shootingDelay;

                    AddRecoilToPlayer();

                    howManyBulletsFired++;
                    Ammo--;
                }
            }
            else
            {
                howManyBulletsFired = 0;
                unitAnimator.SetBool("isShooting", false);
            }
        }
    }

    private void LateUpdate()
    {
        if (isReloading)
        {
            reloadingSlider.GetComponent<ReloadingSlider>().IncrementProgress(Time.deltaTime);
        }

        if (!transform.CompareTag("Terrorist"))
        {
            if (isShooting && Ammo != 0 && !isReloading)
            {
                // zabezpieczenie, aby strza³y nie zosta³y wystrzelone w jednym momencie przy trzymaniu LPM
                // innymi s³owy - co ile czasu ma nastêpowaæ strza³

                if (Time.time >= lastShoot)
                {
                    Fire();
                    float volume = PlayerPrefs.GetFloat("Volume", 0.5f);
                    AudioSource.PlayClipAtPoint(weaponClip, transform.position, volume);
                    unitAnimator.SetBool("isShooting", true);
                    lastShoot = Time.time + shootingDelay;

                    AddRecoilToPlayer();

                    howManyBulletsFired++;
                    Ammo--;
                }
            }
            else
            {
                howManyBulletsFired = 0;
                unitAnimator.SetBool("isShooting", false);
            }
        }
    }

    private void Fire()
    {
        RaycastHit ray;
        Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out ray);

        // w celu sprawdzenia gdzie strzelaj¹ boty
        // if (!transform.CompareTag("TerroristPlayer") && !transform.CompareTag("CounterTerroristPlayer"))
        // {
        //     Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * 1000f, Color.red, .1f);
        // } 

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
    }

    /// <summary>
    /// Funkcja prze³adowuj¹ca broñ.
    /// W przypadku gracza wywo³ywana jest po naciœniêciu R.
    /// U bota - po wystrzeleniu magazynka.
    /// </summary>
    public void Reload()
    {
        if (Ammo == 30) return;
        if (!reloadingSlider.IsUnityNull())
        {
            reloadingSlider.GetComponent<ReloadingSlider>().ToggleSliderVisibility(true);
        }
        isReloading = true;
        unitAnimator.SetBool("isReloading", true);
    }

    /// <summary>
    /// Funkcja wywo³ywana w koñcowej klatce animacji prze³adowania.
    /// </summary>
    public void SetReloadAnimationEnd()
    {
        Ammo = 30;
        isReloading = false;
        if (!reloadingSlider.IsUnityNull())
        {
            reloadingSlider.GetComponent<ReloadingSlider>().ToggleSliderVisibility(false);
            reloadingSlider.GetComponent<ReloadingSlider>().SetProgressToZero();
        }
        unitAnimator.SetBool("isReloading", false);
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
        BulletsLeftText.text = Ammo.ToString();
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

    // Funkcja s³u¿¹ca do przesuwania obiektów o tagu "MovingBox". Chodzi o drewniane skrzynki na mapie
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        float pushPower = 2f;
        if (hit.transform.CompareTag("MovingBox"))
        {
            
            if (!hit.collider.TryGetComponent<Rigidbody>(out var box))
            {
                return;
            }

            Vector3 pushDirection = new Vector3(hit.moveDirection.x, hit.moveDirection.y, hit.moveDirection.z);
            box.velocity = pushDirection * pushPower;
        }
    }
}
