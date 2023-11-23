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
    public Camera playerCamera;
    public GameObject deadBody;
    public AudioSource shootingSound;

    private float lastShoot = 0;

    Animator unitAnimator;
    int howManyBulletsFired = 0;

    bool deadAnimationEnded = false;

    void Start()
    {
        unitAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            // zabezpieczenie, aby strza�y nie zosta�y wystrzelone w jednym momencie przy trzymaniu LPM
            // innymi s�owy - co ile czasu ma nast�powa� strza�
            // TODO: doda� reloading
            if (Time.time >= lastShoot)
            {
                Fire();
                shootingSound.Play();
                //Debug.Log("how many: " + howManyBulletsFired);
                unitAnimator.SetBool("isShooting", true);
                lastShoot = Time.time + shootingDelay;

                // je�eli gracz wystrzeliwuje pociski ca�� seri� (wi�cej ni� 2),
                // to b�dzie mu ci�ej trafi� poprzez 'odrzut'
                if(howManyBulletsFired > 2)
                {
                    if (transform.CompareTag("TerroristPlayer") || transform.CompareTag("CounterTerroristPlayer")) {
                        transform.GetComponent<PlayerMove>().playerSpineVerticalRotation -= Random.Range(0.5f, 0.6f);
                        transform.Rotate(new Vector3(0f, Random.Range(-0.2f, 0.2f), 0f));
                    }
                }
                else
                {
                    if (transform.CompareTag("TerroristPlayer") || transform.CompareTag("CounterTerroristPlayer"))
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
            // je�eli gracz ma mniej HP ni� MinHP to uruchamiamy animacj� umierania,
            // je�eli animacja umierania si� zako�czy (wywo�any Animation Event, wywo�uj�cy `SetDeadAnimationEnd()`), to
            // usuwamy gracza z planszy a na jego miejsce �mierci k�adziemy zw�oki
            unitAnimator.SetBool("isDead", true);

            if (deadAnimationEnded)
            {
                Destroy(transform.gameObject);
                Instantiate(deadBody, transform.position, transform.rotation);
            }

        }
    }

    private void Fire()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // sprawdzanie tag�w - w celu zabezpieczenia, aby terrorysta nie strzela� do innego terrorysty i analogicznie dla AT

            string whoIsShooting = "Not a player";
            if (transform.CompareTag("Terrorist") || transform.CompareTag("TerroristPlayer")) whoIsShooting = "Terrorist";
            if (transform.CompareTag("CounterTerrorist") || transform.CompareTag("CounterTerroristPlayer")) whoIsShooting = "CounterTerrorist";

            string whoIsShotAt = "Not a player";
            if (hit.transform.CompareTag("Terrorist") || hit.transform.CompareTag("TerroristPlayer")) whoIsShotAt = "Terrorist";
            if (hit.transform.CompareTag("CounterTerrorist") || hit.transform.CompareTag("CounterTerroristPlayer")) whoIsShotAt = "CounterTerrorist";

            // sprytniejszy zapis - mniej czytelny
            //string whoIsShooting = transform.CompareTag("Terrorist") || transform.CompareTag("TerroristPlayer") ? "Terrorist" :
            //   transform.CompareTag("CounterTerrorist") || transform.CompareTag("CounterTerroristPlayer") ? "CounterTerrorist" : "Not a player";
            //string whoIsShotAt = hit.transform.CompareTag("Terrorist") || hit.transform.CompareTag("TerroristPlayer") ? "Terrorist" :
            //   hit.transform.CompareTag("CounterTerrorist") || hit.transform.CompareTag("CounterTerroristPlayer") ? "CounterTerrorist" : "Not a player";

            Unit whoIsShotAtUnit = hit.transform.GetComponent<Unit>();

            if (whoIsShooting != "Not a player" && whoIsShotAt != "Not a player" && whoIsShooting != whoIsShotAt)
            {
                if (whoIsShotAtUnit.HP > MinHP)
                {
                    whoIsShotAtUnit.DealDamage(damage);
                }
            }

            Debug.Log(hit.transform.name);
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
