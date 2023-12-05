using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class BotMove : MonoBehaviour
{
    public NavMeshAgent agent;
    
    // aktualny stan bota (co robi)
    public BotStates state = BotStates.walking;

    // kto jest na celowniku bota (najbli�szy widoczny przeciwnik)
    public GameObject attackTarget;

    public Transform botSpine;
    public float botSpineVerticalRotation = 0f;

    private Camera botCamera;
    
    private GameObject gameScriptObject;
    private GameScript scriptOfGame;

    private Animator animator;

    // lista �ywych zawodnik�w przeciwnej dru�yny
    private List<GameObject> enemies = new();
    // lista przeciwnik�w, kt�rych widzi
    private List<GameObject> visibleEnemies = new();

    // co ile czasu bot ma skanowa� pole widzenia - co ile czasu widzi przeciwnika
    private float enemyScanSpeed;
    private float enemyScanSpeedCountdown;

    // co ile czasu bot ma zmienia� pozycj� do kt�rej idzie
    private float changePosition;
    private float changePositionCountdown;

    public enum BotStates
    {
        // bot porusza si� po mapie
        walking, 
        // bot zauwa�y� przeciwnika i do niego strzela
        firing, 
        // zosta�a pod�o�ona bomba i celem bota CT jest jej rozbrojenie a bota TT - prenwencja przed rozbrojeniem
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

        // "resetowanie" kr�gos�upa bota - �eby patrzy� przed siebie

        botSpineVerticalRotation = 0;

        if (transform.CompareTag("Terrorist"))
        {
            botSpine.localRotation = Quaternion.Euler(botSpineVerticalRotation, 0f, 0f);
        }
        else if (transform.CompareTag("CounterTerrorist"))
        {
            botSpine.localRotation = Quaternion.Euler(0f, 0f, -botSpineVerticalRotation);
        }
    }

    void Update()
    {
        // funkcja pobieraj�ca �ywych przeciwnik�w ze skryptu gry
        GetEnemies();
        
        // TODO:
        // time.deltatime > 5f
        // LookAround();

        // TODO:
        // je�li dostaje damage to odwraca si� dop�ki nie napotka przeciwnika

        // TODO:
        // pomys�: je�eli liczba kul bota jest 25 - 30 to bot ucieka �eby j� prze�adowa�

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

        // je�eli bot widzi przeciwnika to zmienia stan na strzelanie, przestaje i�� i zaczyna strzela�
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

            // po up�ywie pewnego czasu bot przemieszcza si� na inn� pozycj� na mapie
            // do zast�pienia poruszaniem si� po strefach
            if (changePositionCountdown > 0)
            {
                changePositionCountdown -= Time.deltaTime;
            }
            else
            {
                changePosition = Random.Range(5f, 15f);
                changePositionCountdown = changePosition;

                // warto�ci w Range s� przybli�onymi rogami mapy
                Vector3 newPositionToGo = new Vector3(Random.Range(-41f, 113f), 0f, Random.Range(-80f, 71f));

                agent.SetDestination(newPositionToGo);
            }
        }

        if (state == BotStates.firing)
        {
            // bot pod��a za najbli�szym przeciwnikiem kamer� poziomo
            Vector3 targetOnHorizontalAxis = new(attackTarget.transform.position.x, transform.position.y, attackTarget.transform.position.z);
            transform.LookAt(targetOnHorizontalAxis);

            animator.SetBool("isWalking", false);
            animator.SetBool("isShooting", true);

            agent.isStopped = true;
            
            transform.GetComponent<Unit>().isShooting = true;
        }
        else
        {
            animator.SetBool("isShooting", false);
            transform.GetComponent<Unit>().isShooting = false;

            botSpineVerticalRotation = 0f;
        }
    }

    private void LateUpdate() 
    {
        if (state == BotStates.firing)
        {
            // bot pod��a za najbli�szym przeciwnikiem kamer� pionowo
            Vector3 direction = attackTarget.transform.position - transform.position;
            botSpineVerticalRotation = Quaternion.LookRotation(direction).eulerAngles.x;

            float distance = Vector3.Distance(attackTarget.transform.position, transform.position);

            // to naprawia b��d, �e mo�na by�o wej�� botowi-terrory�cie na g�ow� a on nie by� w stanie mnie trafi�
            if (distance < 2.8f)
            {
                if (transform.CompareTag("Terrorist"))
                {
                    botCamera.transform.localRotation = Quaternion.Euler(-11, 0, 0);
                }
            }
            else
            {
                if (transform.CompareTag("Terrorist"))
                {
                    botCamera.transform.localRotation = Quaternion.Euler(-12.5f, 0, 0);
                }
            }
            transform.GetComponent<Unit>().AddRecoilToBot();
        } 

        // Spine antyterrorysty trzeba obraca� po innej osi w celu pochylenia, st�d ten `if`
        // kod do obracania kr�gos�upa po pionowej osi, gdy przeciwnik jest wy�ej, np. podskakuje
        if (transform.CompareTag("Terrorist"))
        {
            botSpine.localRotation = Quaternion.Euler(botSpineVerticalRotation, 0f, 0f);
        }
        else if (transform.CompareTag("CounterTerrorist"))
        {
            botSpine.localRotation = Quaternion.Euler(0f, 0f, -botSpineVerticalRotation);
        } 
    }

    /// <summary>
    /// Funkcja przypisuj�ca now� kopi� listy �ywych przeciwnik�w
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
    /// Funkcja odpowiadaj�ca za "widzenie" przeciwnik�w przez bota - czy s� w jego polu widzenia i czy widzi ich bezpo�rednio. Ustala tak�e cel ataku bota
    /// </summary>
    void ScanForVisibleEnemies()
    {
        List<GameObject> cameraVisibleEnemies = new();
        visibleEnemies = new();

        // ta p�tla sprawdza czy collider przeciwnika znajduje si� w zakresie widoku kamery bota
        // nie bierze to jednak pod uwag� po�rednich collider�w, wi�c bot m�g�by widzie� przeciwik�w przez �ciany, st�d Raycast ni�ej
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

        // ta p�tla sprawdza czy na drodze od bota do przeciwnika nie stoi �adna przeszkoda (inny collider)
        foreach (GameObject enemy in cameraVisibleEnemies)
        {
            RaycastHit hit;

            // podajemy kierunek od kamery bota do pozycji przeciwnika.
            // jako, �e pozycja przeciwnika jest na ziemi, to podnosz� j� o 0.5f do g�ry
            Vector3 botCameraPosition = botCamera.transform.position;
            Vector3 enemyPosition = enemy.transform.position + new Vector3(0, 0.5f, 0);

            Physics.Raycast(botCameraPosition, enemyPosition - botCameraPosition, out hit);

            string whoIsShooting = "Not a player";
            if (transform.CompareTag("Terrorist") || transform.CompareTag("TerroristPlayer")) whoIsShooting = "Terrorist";
            if (transform.CompareTag("CounterTerrorist") || transform.CompareTag("CounterTerroristPlayer")) whoIsShooting = "CounterTerrorist";

            string whoIsShotAt = "Not a player";
            if (hit.transform.CompareTag("Terrorist") || hit.transform.CompareTag("TerroristPlayer")) whoIsShotAt = "Terrorist";
            if (hit.transform.CompareTag("CounterTerrorist") || hit.transform.CompareTag("CounterTerroristPlayer")) whoIsShotAt = "CounterTerrorist";

            // zabezpieczenie, aby bot nie celowa� w inne obiekty ni� przeciwnik
            if (whoIsShooting != "Not a player" && whoIsShotAt != "Not a player" && whoIsShooting != whoIsShotAt)
            {
                // je�li nie ma innych collider�w mi�dzy botem a przeciwnikiem to dodajemy go do widocznych przeciwnik�w
                visibleEnemies.Add(enemy);
            }
        }

        // skrypt sprawdza kt�ry przeciwnik jest najbli�ej bota
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
            // je�li bot nikogo nie widzi to do nikogo nie celuje
            attackTarget = null;
        }
    }
}
