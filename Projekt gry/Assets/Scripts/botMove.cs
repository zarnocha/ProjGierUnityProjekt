using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class BotMove : MonoBehaviour
{
    public NavMeshAgent agent;
    
    public BotStates state = BotStates.walking;
    // kto jest na celowniku bota (najbli¿szy widoczny przeciwnik)
    public GameObject attackTarget;

    private Camera botCamera;
    
    private GameObject gameScriptObject;
    private GameScript scriptOfGame;

    private Animator animator;

    // lista ¿ywych zawodników przeciwnej dru¿yny
    private List<GameObject> enemies = new();
    // lista przeciwników, których widzi
    private List<GameObject> visibleEnemies = new();

    // co ile czasu bot ma skanowaæ pole widzenia - co ile czasu widzi przeciwnika
    private float enemyScanSpeed;
    private float enemyScanSpeedCountdown;

    // co ile czasu bot ma zmieniaæ pozycjê do której idzie
    private float changePosition;
    private float changePositionCountdown;

    public enum BotStates
    {
        // bot porusza siê po mapie
        walking, 
        // bot zauwa¿y³ przeciwnika i do niego strzela
        firing, 
        // zosta³a pod³o¿ona bomba i celem bota CT jest jej rozbrojenie a bota TT - prenwencja przed rozbrojeniem
        bomb, 
        // bot umiera
        dead 
    }


    void Start()
    {
        animator = GetComponent<Animator>();

        gameScriptObject = GameObject.FindGameObjectWithTag("GameScript");
        scriptOfGame = gameScriptObject.GetComponent<GameScript>();

        botCamera = transform.GetComponentInChildren<Camera>();

        // przydzielenie losowego czasu skanu pola widzenia
        enemyScanSpeed = Random.Range(0.2f, 1f);
        enemyScanSpeedCountdown = enemyScanSpeed;
    }

    void Update()
    {
        // funkcja pobieraj¹ca ¿ywych przeciwników ze skryptu gry
        GetEnemies();

        if (enemyScanSpeedCountdown > 0)
        {
            enemyScanSpeedCountdown -= Time.deltaTime;
        }
        else
        {
            ScanForVisibleEnemies();
            enemyScanSpeed = Random.Range(0.2f, 1f);
            enemyScanSpeedCountdown = enemyScanSpeed;
        }

        if (attackTarget)
        {
            state = BotStates.firing;
        }
        else
        {
            state = BotStates.walking;
        }

        if (state == BotStates.walking)
        {
            animator.SetBool("isWalking", true);
            agent.isStopped = false;
            if (changePositionCountdown > 0)
            {
                changePositionCountdown -= Time.deltaTime;
            }
            else
            {
                changePosition = Random.Range(5f, 15f);
                changePositionCountdown = changePosition;

                Vector3 newPositionToGo = new Vector3(Random.Range(-41f, 113f), 0f, Random.Range(-80f, 71f));

                agent.SetDestination(newPositionToGo);
            }
        }

        if (state == BotStates.firing)
        {
            // bot pod¹¿a za najbli¿szym przeciwnikiem kamer¹
            transform.LookAt(attackTarget.transform);

            animator.SetBool("isWalking", false);
            animator.SetBool("isShooting", true);
            agent.isStopped = true;
            
            transform.GetComponent<Unit>().isShooting = true;
        }
        else
        {
            animator.SetBool("isShooting", false);
            transform.GetComponent<Unit>().isShooting = false;
        }
    }

    /// <summary>
    /// Funkcja przypisuj¹ca now¹ kopiê listy ¿ywych przeciwników
    /// </summary>
    void GetEnemies()
    { 
        if (transform.CompareTag("CounterTerrorist"))
        {
            enemies = scriptOfGame.GetTtPlayers();
        }
        else if (transform.CompareTag("Terrorist"))
        {
            enemies = scriptOfGame.GetCtPlayers();
        }
    }

    /// <summary>
    /// Funkcja odpowiadaj¹ca za "widzenie" przeciwników przez bota - czy s¹ w jego polu widzenia i czy widzi ich bezpoœrednio. Ustala tak¿e cel ataku bota
    /// </summary>
    void ScanForVisibleEnemies()
    {
        List<GameObject> cameraVisibleEnemies = new();
        visibleEnemies = new();

        // ta pêtla sprawdza czy collider przeciwnika znajduje siê w zakresie widoku kamery bota
        // nie bierze to jednak pod uwagê poœrednich colliderów, wiêc bot móg³by widzieæ przeciwików przez œciany, st¹d Raycast ni¿ej
        foreach (GameObject enemy in enemies)
        {
            Collider enemyCollider = enemy.GetComponent<Collider>();
            Bounds bounds = enemyCollider.bounds;
            Plane[] cameraFrustum = GeometryUtility.CalculateFrustumPlanes(botCamera);
            if (GeometryUtility.TestPlanesAABB(cameraFrustum, bounds))
            {
                cameraVisibleEnemies.Add(enemy);
            }
        }

        // ta pêtla sprawdza czy na drodze od bota do przeciwnika nie stoi ¿adna przeszkoda (inny collider)
        foreach (GameObject enemy in cameraVisibleEnemies)
        {
            RaycastHit hit;

            // podajemy kierunek od kamery bota do pozycji przeciwnika.
            // jako, ¿e pozycja przeciwnika jest na ziemi, to podnoszê j¹ o 0.5f do góry
            Vector3 botCameraPosition = botCamera.transform.position;
            Vector3 enemyPosition = enemy.transform.position + new Vector3(0, 0.5f, 0);

            Physics.Raycast(botCameraPosition, enemyPosition - botCameraPosition, out hit);

            string whoIsShooting = "Not a player";
            if (transform.CompareTag("Terrorist") || transform.CompareTag("TerroristPlayer")) whoIsShooting = "Terrorist";
            if (transform.CompareTag("CounterTerrorist") || transform.CompareTag("CounterTerroristPlayer")) whoIsShooting = "CounterTerrorist";

            string whoIsShotAt = "Not a player";
            if (hit.transform.CompareTag("Terrorist") || hit.transform.CompareTag("TerroristPlayer")) whoIsShotAt = "Terrorist";
            if (hit.transform.CompareTag("CounterTerrorist") || hit.transform.CompareTag("CounterTerroristPlayer")) whoIsShotAt = "CounterTerrorist";

            // zabezpieczenie, aby bot nie celowa³ w inne obiekty ni¿ przeciwnik
            if (whoIsShooting != "Not a player" && whoIsShotAt != "Not a player" && whoIsShooting != whoIsShotAt)
            {
                // jeœli nie ma innych colliderów miêdzy botem a przeciwnikiem to dodajemy go do widocznych przeciwników
                visibleEnemies.Add(enemy);
            }
        }

        // skrypt sprawdza który przeciwnik jest najbli¿ej bota
        if (visibleEnemies.Count > 0)
        {
            float closestDistance = Vector3.Distance(transform.position, visibleEnemies[0].transform.position);
            attackTarget = visibleEnemies[0];
       
            foreach (GameObject enemy in visibleEnemies)
            {
                float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
                if (closestDistance > distanceToEnemy)
                {
                    closestDistance = distanceToEnemy;
                    attackTarget = enemy;
                }
            }
        }
        else
        {
            // jeœli bot nikogo nie widzi to do nikogo nie celuje
            attackTarget = null;
        }
    }

}
