using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class BotMove : MonoBehaviour
{
    public Unit botUnit;
    public NavMeshAgent agent;

    // kto jest na celowniku bota (najbli¿szy widoczny przeciwnik)
    public GameObject attackTarget;

    public Transform botSpine;
    public float botSpineVerticalRotation = 0f;

    private Camera botCamera;
    
    private GameObject gameScriptObject;
    private GameScript gameScript;

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

    private Defusing defusing;

    private PlantingBomb plantingBomb;
    public Vector3 bombPosition;

    /// <summary>
    /// Co ile czasu bot ma siê obracaæ
    /// </summary>
    private float lookTimer = 1;
    private float lookTimerCountdown = 1;

    /// <summary>
    /// Si³a obrotu
    /// </summary>
    float lookAroundForce;

    // w któr¹ stronê obraca siê bot
    private LookAroundState currentLookAround = LookAroundState.none;
    private enum LookAroundState
    {
        left,
        none,
        right
    }

    // domyœlnie boty maj¹ chodziæ losowo
    public BotPurpose purpose = BotPurpose.random;
    public enum BotPurpose
    {
        random,
        bombsite
    }
    
    /// <summary>
    /// Enum mówi¹cy o tym, co robi antyterroysta
    /// Ustala cel jako sprawdzenie bombsite'ów b¹dŸ losowe chodzenie
    /// </summary>
    public enum PlacesToDefend
    {
        A,
        B,
        random
    }

    // w domyœlnym przypadku bot CT chodzi po ca³ej mapie
    public PlacesToDefend botDefending = PlacesToDefend.random;

    public enum BotStates
    {
        // bot porusza siê po mapie
        walking,
        // bot zauwa¿y³ przeciwnika i do niego strzela
        firing,
        // bot wystrzeli³ wszystkie pociski i musi prze³adowaæ broñ
        reloading,
        // bot umiera
        dead 
    }

    /// <summary>
    /// aktualny stan bota (co robi)
    /// </summary>
    public BotStates state = BotStates.walking;

    /// <summary>
    /// Czy bot jest tym, który ma rozbrajaæ bombê
    /// </summary>
    public bool isDefusor = false;

    void Start()
    {
        // przydzielenie losowej mocy obrotu bota
        lookAroundForce = Random.Range(2.5f, 4f);
        lookTimerCountdown = lookTimer;
        animator = GetComponent<Animator>();
        botUnit = GetComponent<Unit>();
        gameScriptObject = GameObject.FindGameObjectWithTag("GameScript");
        gameScript = gameScriptObject.GetComponent<GameScript>();

        botCamera = transform.GetComponentInChildren<Camera>();

        // przydzielenie losowego czasu skanu pola widzenia
        enemyScanSpeed = Random.Range(0.2f, 1f);
        enemyScanSpeedCountdown = enemyScanSpeed;

        // "resetowanie" krêgos³upa bota - ¿eby patrzy³ przed siebie
        botSpineVerticalRotation = 0;

        if (transform.CompareTag("Terrorist"))
        {
            botSpine.localRotation = Quaternion.Euler(botSpineVerticalRotation, 0f, 0f);
            plantingBomb = GetComponent<PlantingBomb>();
        }
        else if (transform.CompareTag("CounterTerrorist"))
        {
            botSpine.localRotation = Quaternion.Euler(0f, 0f, -botSpineVerticalRotation);
            defusing = GetComponent<Defusing>();
        }

        // je¿eli misj¹ bota jest pójœcie na bombsite i ma bombê
        if (!plantingBomb.IsUnityNull() && plantingBomb.hasBomb && !PlantingBomb.isPlanted)
        {
            Vector3 posOnBombsite = GameScript.RandomPositionOnPlane(GameScript.chosenBombsite.transform);
            agent.SetDestination(posOnBombsite);
        }
    }

    void Update()
    {
        if (PauseMenu.isPaused) return;
        if (botUnit.isDead)
        {
            state = BotStates.dead;
            return;
        }

        // funkcja pobieraj¹ca ¿ywych przeciwników ze skryptu gry
        GetEnemies();

        // je¿eli bomba jest pod³o¿ona to wszystkie boty zmierzaj¹ do miejsca jej pod³o¿enia
        if (PlantingBomb.isPlanted)
        {
            purpose = BotPurpose.bombsite;
        }

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

        // je¿eli bot widzi przeciwnika to zmienia stan na strzelanie, przestaje iœæ i zaczyna strzelaæ
        if (attackTarget)
        {
            state = BotStates.firing;
        }
        else
        {
            state = BotStates.walking;
        }

        if (botUnit.Ammo <= 0 || botUnit.isReloading)
        {
            state = BotStates.reloading;
        }

        if (state == BotStates.walking)
        {
            animator.SetBool("isWalking", true);
            agent.isStopped = false;

            // kod do obracania siê bota
            if (currentLookAround == LookAroundState.left)
            {
                LookAround(-lookAroundForce);
            }

            if (currentLookAround == LookAroundState.right)
            {
                LookAround(lookAroundForce);
            }

            if (lookTimerCountdown <= 0)
            {
                currentLookAround = (LookAroundState)Random.Range(0, 3);

                lookTimerCountdown = Random.Range(1f, 2.5f);

                // je¿eli bot ma siê nie obracaæ, to niech siê nie obraca d³u¿ej ni¿ ma siê obracaæ
                if (currentLookAround == LookAroundState.none)
                {
                    lookTimerCountdown = Random.Range(4f, 6f);
                }

                lookAroundForce = Random.Range(4f, 7f);
            }
            else
            {
                lookTimerCountdown -= Time.deltaTime;
            }

            if (purpose == BotPurpose.random)
            {
                if (transform.CompareTag("Terrorist"))
                {
                    if (changePositionCountdown > 0)
                    {
                        changePositionCountdown -= Time.deltaTime;
                    }

                    else
                    {
                        changePosition = Random.Range(5f, 15f);
                        changePositionCountdown = changePosition;

                        // wartoœci w Range s¹ przybli¿onymi rogami mapy
                        Vector3 positionToGo = new Vector3(Random.Range(-41f, 113f), 0f, Random.Range(-80f, 71f));
                        agent.SetDestination(positionToGo);
                    }
                }

                if (transform.CompareTag("CounterTerrorist"))
                {
                    // agent z NavMesh na pocz¹tku ma miejsce docelowe (destination) w miejscu respawnu
                    // czyli ten if siê wykona, bo jest niedaleko miejsca docelowego, przez co zmieni mu siê destynacja
                    float distanceToDestination = Vector3.Distance(transform.position, agent.destination);
                    if (distanceToDestination <= 1f)
                    {
                        botDefending = (PlacesToDefend)Random.Range(0, 3);

                        if (botDefending == PlacesToDefend.random)
                        {
                            Vector3 positionToGo = new Vector3(Random.Range(-41f, 113f), 0f, Random.Range(-80f, 71f));
                            agent.SetDestination(positionToGo);
                        }

                        Transform bombsiteA = gameScript.BombsiteA.transform;
                        Transform bombsiteB = gameScript.BombsiteB.transform;

                        if (botDefending == PlacesToDefend.A)
                        {
                            agent.SetDestination(GameScript.RandomPositionOnPlane(bombsiteA));
                        }

                        if (botDefending == PlacesToDefend.B)
                        {
                            agent.SetDestination(GameScript.RandomPositionOnPlane(bombsiteB));
                        }
                    }
                }
            }

            else if (purpose == BotPurpose.bombsite)
            {
                if (transform.CompareTag("Terrorist"))
                {
                    if (!plantingBomb.hasBomb)
                    {
                        if (changePositionCountdown > 0)
                        {
                            changePositionCountdown -= Time.deltaTime;
                        }
                        else
                        {
                            changePosition = Random.Range(1f, 3f);
                            changePositionCountdown = changePosition;

                            Vector3 posOnBombsite = GameScript.RandomPositionOnPlane(GameScript.chosenBombsite.transform);
                            agent.SetDestination(posOnBombsite);
                        }
                    }
                }

                if (transform.CompareTag("CounterTerrorist"))
                {
                    if (changePositionCountdown > 0)
                    {
                        changePositionCountdown -= Time.deltaTime;
                    }

                    // je¿eli AT ma rozbrajaæ bombê to nie mo¿e mieæ zmienianej pozycji
                    else if (changePositionCountdown <= 0 && !isDefusor)
                    {
                        changePosition = Random.Range(1f, 3f);
                        changePositionCountdown = changePosition;

                        Vector3 posOnBombsite = GameScript.RandomPositionOnPlane(GameScript.chosenBombsite.transform);
                        agent.SetDestination(posOnBombsite);
                    }

                    // je¿eli ma rozbrajaæ bombê (jest jej najbli¿ej) to j¹ rozbraja
                    if (isDefusor)
                    {
                        agent.SetDestination(Defusing.bombPosition);
                        if (defusing.canDefuse)
                        {
                            agent.isStopped = true;
                            defusing.DefusingBomb();
                        }
                        else
                        {
                            agent.isStopped = false;
                        }
                    }
                }
            }
        }

        // jeœli bot ma skrypt PlantingBomb (czyli jest terroryst¹) i bombê i mo¿e j¹ pod³o¿yæ (jest na bombsite)
        if (!plantingBomb.IsUnityNull() && plantingBomb.canPlant && plantingBomb.hasBomb)
        {
            plantingBomb.Planting();
        }

        if (state == BotStates.firing)
        {
            // bot pod¹¿a za najbli¿szym przeciwnikiem kamer¹ poziomo
            Vector3 targetOnHorizontalAxis = new(attackTarget.transform.position.x, transform.position.y, attackTarget.transform.position.z);
            transform.LookAt(targetOnHorizontalAxis);

            animator.SetBool("isWalking", false);
            animator.SetBool("isShooting", true);

            agent.isStopped = true;
            
            botUnit.isShooting = true;
        }
        else
        {
            animator.SetBool("isShooting", false);
            botUnit.isShooting = false;

            botSpineVerticalRotation = 0f;
        }

        if (state == BotStates.reloading)
        {
            botUnit.Reload();
        }
    }

    private void LateUpdate() 
    {
        if (botUnit.isDead)
        {
            agent.isStopped = true;
            state = BotStates.dead;
            return;
        }

        if (state == BotStates.firing)
        {
            // bot pod¹¿a za najbli¿szym przeciwnikiem kamer¹ pionowo
            Vector3 direction = attackTarget.transform.position - transform.position;
            botSpineVerticalRotation = Quaternion.LookRotation(direction).eulerAngles.x;

            float distance = Vector3.Distance(attackTarget.transform.position, transform.position);

            // to naprawia b³¹d, ¿e mo¿na by³o wejœæ botowi-terroryœcie na g³owê a on nie by³ w stanie mnie trafiæ
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

            botUnit.AddRecoilToBot();
        }

        // Spine antyterrorysty trzeba obracaæ po innej osi w celu pochylenia, st¹d ten `if`
        // kod do obracania krêgos³upa po pionowej osi, gdy przeciwnik jest wy¿ej, np. podskakuje
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
    /// Funkcja przypisuj¹ca now¹ kopiê listy ¿ywych przeciwników
    /// </summary>
    void GetEnemies()
    { 
        if (transform.CompareTag("CounterTerrorist"))
        {
            enemies = gameScript.GetTtPlayers();
        }
        else if (transform.CompareTag("Terrorist"))
        {
            enemies = gameScript.GetCtPlayers();
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

    void LookAround(float degrees)
    {
        transform.Rotate(0f, degrees, 0f, Space.Self);
    }
}
