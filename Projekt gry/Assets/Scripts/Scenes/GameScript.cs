using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class GameScript : MonoBehaviour
{
    public bool considerPlayerPrefs = true;

    [Header("Opcje gameplayu")]
    [Header("Wybór zespołu gracza")]
    public Teams playerTeamChoice = Teams.Terrorist;

    [Header("Ilość graczy AI w każdej drużynie")]
    [Range(0f, 10f)]
    [SerializeField]
    private int CtPlayersAmount = 5;
    [Range(0f, 10f)]
    [SerializeField]
    private int TtPlayersAmount = 4;

    [Header("Czas gry")]
    [Tooltip("Czas trwania rundy (w sekundach)")]
    [SerializeField]
    private float roundTime = 3f * 60;
    private float roundTimeCountdown;

    [Tooltip("Czas trwania przerwy pomiędzy rundami (w sekundach)")]
    public float TimeBetweenRounds = 5f;
    private float timeBetweenRoundsCountdown;
    [Space(10)]
    [Tooltip("Czy gracze jednego zespołu mogą się ranić?")]
    public bool isFriendlyFire = false;

    [Space(20)]
    [Header("Inne ustawienia")]
    [Header("Prefaby postaci gracza dla zespołów")]
    public GameObject PlayerCT;
    public GameObject PlayerTT;

    [Header("Prefaby postaci gracza AI dla zespołów")]
    public GameObject BotCT;
    public GameObject BotTT;
    public enum Teams
    {
        CounterTerrorist,
        Terrorist,
        Spectator
    }

    // listy graczy w grze
    private List<GameObject> playersCtList = new();
    private List<GameObject> playersTtList = new();
   
    // listy botów w grze
    private List<GameObject> botsCtList = new();
    private List<GameObject> botsTtList = new();

    // lista zwłok
    private List<GameObject> deadBodies = new();

    // lista poszczególnych elementów UI każdego gracza
    private List<TextMeshProUGUI> roundTimeText = new();
    private List<TextMeshProUGUI> scoreTTText = new();
    private List<TextMeshProUGUI> scoreCTText = new();
    
    private List<TextMeshProUGUI> playersLeftCTText = new();
    private List<TextMeshProUGUI> playersLeftTTText = new();
    
    private List<TextMeshProUGUI> allPlayersCTText = new();
    private List<TextMeshProUGUI> allPlayersTTText = new();

    private Transform respawnCT;
    private Transform respawnTT;

    public GameObject BombsiteA;
    public GameObject BombsiteB;

    private Camera waitingCamera;
    private Camera freeCamera;

    private bool isRoundRunning = false;

    // wynik gry
    private int roundsWonByCT = 0;
    private int roundsWonByTT = 0;

    // limit liczby rund
    private int roundLimit = 15;

    private Teams whoWonRound;
    private TextMeshProUGUI whoWonRoundText;

    private bool isPlayerAlive = false;

    /// <summary>
    /// Indeks aktywnej kamery.
    /// 0 to kamera `waitingCamera`, czyli widok z góry z wynikiem.
    /// kamery od 2 w górę to kamery botów
    /// </summary>
    public int activeCamera = 0;

    /// <summary>
    /// Lista kamer dostępnych do poglądu, gdy gracz nie żyje bądź jest widzem
    /// </summary>
    private List<Camera> cameras = new();

    public bool isBombPlanted = false;
    public float bombToExplodeTime = 45f;
    public float bombToExplodeTimeCountdown;
    public static GameObject chosenBombsite;

    private void Awake()
    {
        // zatrzymywanie muzyki, gdy rozpoczynamy rozgrywkę
        if (GameObject.FindGameObjectWithTag("Music"))
        {
            GameObject.FindGameObjectWithTag("Music").GetComponent<PlayMusicThroughScenes>().StopMusic();
        }

        if (considerPlayerPrefs)
        {
            string choice = PlayerPrefs.GetString("playerTeamChoice");

            if (choice == "Antyterroryści")
            {
                playerTeamChoice = Teams.CounterTerrorist;
            }
            else if (choice == "Terroryści")
            {
                playerTeamChoice = Teams.Terrorist;
            }
            else
            {
                playerTeamChoice = Teams.Spectator;
            }

            roundLimit = PlayerPrefs.GetInt("RoundLimit");
            roundTime = 60 * PlayerPrefs.GetInt("RoundTimeMinutes") + PlayerPrefs.GetInt("RoundTimeSeconds");
            TimeBetweenRounds = 60 * PlayerPrefs.GetInt("TimeBetweenRoundsMinutes") + PlayerPrefs.GetInt("TimeBetweenRoundsSeconds");
            TimeBetweenRounds = 60 * PlayerPrefs.GetInt("TimeBetweenRoundsMinutes") + PlayerPrefs.GetInt("TimeBetweenRoundsSeconds");
            CtPlayersAmount = PlayerPrefs.GetInt("CtPlayersAmount");
            TtPlayersAmount = PlayerPrefs.GetInt("TtPlayersAmount");
            // w PlayerPrefs nie ma SetBool wiec musialem to obejsc int'em
            isFriendlyFire = PlayerPrefs.GetInt("FriendlyFire", 0) != 0;
        }
    }

    void Start()
    {
        // kopiujemy oryginalny czas do drugiej zmiennej po to, żeby mieć dostęp do oryginalnej wartości między rundami
        // czas rundy polega na odejmowaniu Time.deltaTime od zadeklarowanej wartości, stąd kopia
        roundTimeCountdown = roundTime;
        // to samo poniżej, tylko dla odstępu między rundami
        timeBetweenRoundsCountdown = TimeBetweenRounds;

        // kamera, która będzie wyświetlać mapę od góry w trakcie oczekiwania na nową rundę
        GameObject waitingCameraObject = GameObject.FindGameObjectWithTag("WaitingCamera");
        GameObject freeCameraObject = GameObject.FindGameObjectWithTag("FreeCamera");
        waitingCamera = waitingCameraObject.GetComponent<Camera>();
        cameras.Add(waitingCamera);

        // tylko widz ma dostęp do wolnej kamery
        if (playerTeamChoice == Teams.Spectator)
        {
            freeCamera = freeCameraObject.GetComponent<Camera>();
            cameras.Add(freeCamera);
        }

        whoWonRoundText = GameObject.FindWithTag("WhoWonRound").GetComponent<TextMeshProUGUI>();

        RoundStart();
    }

    void Update()
    {
        // to jest w update bo gra blokowała kursor po kliknięciu
        if (!PauseMenu.isPaused)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        if (PauseMenu.isPaused) return;
        // z klatki na klatkę sprawdzam czy wszyscy żyją i aktualizuję im czas na UI
        SetTimeOnUi();
        SetAlivePlayersOnUi();
        UpdatePlayers();
        UpdateCameras();

        CheckIfBombIsPlanted();

        if (!isBombPlanted)
        {
            CheckIfAnyoneHasBomb();
        }
        CheckIfAnyoneHasDefuseKit();

        // koniec gry
        if (roundsWonByCT + roundsWonByTT == roundLimit)
        {
            isRoundRunning = false;
           
            // ustalamy kto wygrał, zapisujemy wynik końcowy oraz zwycięzcę, żeby wyświetlić te dane na scenie 3 (Game over scene)
            string WhoWon = (roundsWonByCT > roundsWonByTT) ? "CT" : (roundsWonByCT < roundsWonByTT) ? "TT" : "Remis";
            PlayerPrefs.SetString("WhoWon", WhoWon);
            PlayerPrefs.SetInt("ScoreCT", roundsWonByCT);
            PlayerPrefs.SetInt("ScoreTT", roundsWonByTT);

            SceneManager.LoadScene(3);
        }

        if (isRoundRunning)
        {
            // jeżeli gracz wybierze rolę widza to będzie miał ciągle widok "oczekiwania na rundę"
            if (playerTeamChoice != Teams.Spectator)
            {
                waitingCamera.enabled = false;
            }

            IsPlayerAlive();

            if (!isPlayerAlive)
            {
                // mamy dostęp do podglądania gry botów z widoku pierwszej osoby
                // zmiana kamer - LPM i PPM
                if (activeCamera == 0) waitingCamera.enabled = true;
                if (Input.GetMouseButtonDown(0))
                {
                    UpdateCameras();

                    int amountOfCameras = cameras.Count -1;
                    if (!cameras[activeCamera].IsUnityNull())
                    {
                        cameras[activeCamera].enabled = false;
                    }

                    if (activeCamera == amountOfCameras)
                    {
                        activeCamera = 0;
                    } else activeCamera++;

                    cameras[activeCamera].enabled = true;
                } 
                else if (Input.GetMouseButtonDown(1))
                {
                    UpdateCameras();

                    int amountOfCameras = cameras.Count -1;
                    if (!cameras[activeCamera].IsUnityNull())
                    {
                        cameras[activeCamera].enabled = false;
                    }

                    if (activeCamera -1 < 0)
                    {
                        activeCamera = amountOfCameras;
                    }
                    else activeCamera--;

                    cameras[activeCamera].enabled = true;
                }

                // zabezpieczenie przed tym, żeby iterator kamery nie wyszedł poza listę
                if (activeCamera > cameras.Count || activeCamera < 0)
                {
                    activeCamera = 0;
                }

                try
                {
                    if (!cameras[activeCamera].IsUnityNull())
                    {
                        cameras[activeCamera].enabled = true;
                    }
                }
                catch (System.Exception)
                {
                    //cameras.RemoveAt(activeCamera);
                    activeCamera = 0;
                    cameras[activeCamera].enabled = true;
                }
            }

            if (roundTimeCountdown > 0 && !isBombPlanted)
            {
                roundTimeCountdown -= Time.deltaTime;
            }
            else if (roundTimeCountdown < 0)
            {
                Debug.Log("CT wygrało na czas");
                RoundForCT();
            }

            // bomba jest podłożona i trwa odliczanie do eksplozji
            if (isBombPlanted && bombToExplodeTimeCountdown > 0 && !Defusing.isDefused)
            {
                bombToExplodeTimeCountdown -= Time.deltaTime;
                UpdateDefusor();
            }
            else if (bombToExplodeTimeCountdown < 0)
            {
                isRoundRunning = false;
                Debug.Log("TT wygrało poprzez wysadzenie bomby");
                RoundForTT();
            }

            // jeżeli w trakcie trwania rundy któryś z zespołów zdoła wyeliminować przeciwny to wygrywa rundę
            // if (playersCtList.Count == 0 || (playersTtList.Count == 0 && !isBombPlanted && !Defusing.isDefused))
            // {
            //     if (playersTtList.Count == 0) {
            //         Debug.Log("CT wygrało przez zlikwidowanie TT");
            //         RoundForCT();
            //     }
            //     else if (playersCtList.Count == 0) {
            //         Debug.Log("TT wygrało przez zlikwidowanie CT");
            //         RoundForTT();
            //     }
            // }

            if (playersCtList.Count == 0)
            {
                Debug.Log("TT wygrało przez zlikwidowanie CT");
                RoundForTT();
            }

            if (playersTtList.Count == 0 && !isBombPlanted && !Defusing.isDefused)
            {
                Debug.Log("CT wygrało przez zlikwidowanie TT");
                RoundForCT();
            }

            if (Defusing.isDefused)
            {
                isRoundRunning = false;
                Debug.Log("CT wygrało poprzez rozbrojenie bomby");
                RoundForCT();
            }
        }

        // jeżeli runda dobiegła końca
        else if (!isRoundRunning && roundsWonByCT + roundsWonByTT < roundLimit)
        {
            waitingCamera.enabled = true;
            activeCamera = 0;
            UpdateTextBetweenRounds();

            if (timeBetweenRoundsCountdown > 0)
            {
                timeBetweenRoundsCountdown -= Time.deltaTime;
            }
            else
            {
                timeBetweenRoundsCountdown = TimeBetweenRounds;
                RoundStart();
            }
        }

    }

    /// <summary>
    /// Funkcja do rozpoczynania rundy
    /// </summary>
    void RoundStart()
    {
        // czyścimy planszę ze zwłok i resetujemy czas trwania rundy
        RemoveDeadBodies();
        roundTimeCountdown = roundTime;
        bombToExplodeTimeCountdown = bombToExplodeTime;
        PlantingBomb.DestroyPlantedBomb();
        isBombPlanted = false;
        Defusing.isDefused = false;
        PlantingBomb.isPlanted = false;

        whoWonRoundText.text = "";

        respawnCT = GameObject.FindGameObjectWithTag("RespawnCT").transform;
        respawnTT = GameObject.FindGameObjectWithTag("RespawnTT").transform;

        // restartujemy wszystkich graczy - na wypadek, gdyby jacyś przeżyli
        DestroyEveryPlayerOfList(playersCtList);
        DestroyEveryPlayerOfList(playersTtList);

        playersCtList = new();
        playersTtList = new();

        if (playerTeamChoice == Teams.CounterTerrorist)
        {
            Vector3 randomSpawnPositionVector = RandomPositionOnPlane(respawnCT);
            GameObject player = Instantiate(PlayerCT, randomSpawnPositionVector, Quaternion.identity);
            playersCtList.Add(player);
            isPlayerAlive = true;
        }
        else if (playerTeamChoice == Teams.Terrorist)
        {
            Vector3 randomSpawnPositionVector = RandomPositionOnPlane(respawnTT);
            GameObject player = Instantiate(PlayerTT, randomSpawnPositionVector, Quaternion.identity);
            //player.GetComponent<PlantingBomb>().hasBomb = true; // TODO: do debugowania
            playersTtList.Add(player);
            isPlayerAlive = true;
        } 
        else
        {
            isPlayerAlive = false;
        }

        for (int i = 0; i < CtPlayersAmount; i++)
        {
            Vector3 randomSpawnPositionVector = RandomPositionOnPlane(respawnCT);
            GameObject newCtBot = Instantiate(BotCT, randomSpawnPositionVector, Quaternion.identity);
            playersCtList.Add(newCtBot);
            botsCtList.Add(newCtBot);

            if (playerTeamChoice == Teams.Spectator || playerTeamChoice == Teams.CounterTerrorist)
            {
                cameras.Add(newCtBot.GetComponentInChildren<Camera>());
            }
        }

        for (int i = 0; i < TtPlayersAmount; i++)
        {
            Vector3 randomSpawnPositionVector = RandomPositionOnPlane(respawnTT);
            GameObject newTtBot = Instantiate(BotTT, randomSpawnPositionVector, Quaternion.identity);
            playersTtList.Add(newTtBot);
            botsTtList.Add(newTtBot);

            if (playerTeamChoice == Teams.Spectator || playerTeamChoice == Teams.Terrorist)
            {
                cameras.Add(newTtBot.GetComponentInChildren<Camera>());
            }
        }

        // zniszczenie obiektów UI których już nie ma w grze i pobranie nowych z graczy
        DestroyAndAssignUiTexts();

        UpdateUiScore(Teams.CounterTerrorist);
        UpdateUiScore(Teams.Terrorist);

        UpdateAllPlayersCountOnUi();

        // przydzielenie bomby losowemu graczowi
        // zapisać gościa który ma bombę i jeżeli umrze to ją upuszcza, to samo z defuserem
        GameObject randomTerrorist = RandomElementFromList(playersTtList);
        randomTerrorist.GetComponent<PlantingBomb>().hasBomb = true;

        if (randomTerrorist.CompareTag("Terrorist"))
        {
            List<Transform> bombsites = new()
            {
                BombsiteA.transform,
                BombsiteB.transform
            };

            int randomBombsite = Random.Range(0, 2);

            chosenBombsite = bombsites[randomBombsite].gameObject;

            // bot który ma bombę ma wylosowany bombsite na który pójdzie podłożyć bombę
            //Vector3 randomPositionOnBombsite = RandomPositionOnPlane(bombsites[randomBombsite]);
            randomTerrorist.GetComponent<BotMove>().purpose = BotMove.BotPurpose.bombsite;

            // tutaj 70% zespołu pójdzie z nim
            List<GameObject> botsTtWithoutBomb = new(botsTtList);
            botsTtWithoutBomb.Remove(randomTerrorist);
            if (TtPlayersAmount == 2)
            {
                foreach (GameObject bot in botsTtWithoutBomb)
                {
                    if (!bot.GetComponent<BotMove>().IsUnityNull())
                    {
                        bot.GetComponent<BotMove>().purpose = BotMove.BotPurpose.bombsite;
                    }
                }
            }

            else if (botsTtWithoutBomb.Count > 2)
            {
                for (int i = 0; i < Mathf.Floor(botsTtWithoutBomb.Count * 0.7f); i++)
                {
                    if (!botsTtWithoutBomb[i].GetComponent<BotMove>().IsUnityNull())
                    {
                        botsTtWithoutBomb[i].GetComponent<BotMove>().purpose = BotMove.BotPurpose.bombsite;
                    }
                }
            }
        }

        if (randomTerrorist.CompareTag("TerroristPlayer"))
        {
            chosenBombsite = BombsiteA;
        }

        // przydzielenie losowemu antyterroryście zestawu do rozbrajania
        GameObject randomCounterTerrorist = RandomElementFromList(playersCtList);
        randomCounterTerrorist.GetComponent<Defusing>().hasDefuseKit = true;

        isRoundRunning = true;
    }

    /// <summary>
    /// Funkcja deklarująca zwycięstwo CT i wywołująca RoundsEnd()
    /// </summary>
    void RoundForCT()
    {
        RoundEnd();

        whoWonRound = Teams.CounterTerrorist;
        roundsWonByCT++;

        UpdateUiScore(Teams.CounterTerrorist);
        Debug.Log("CT wygrało rundę");
    }
    
    /// <summary>
    /// Funkcja deklarująca zwycięstwo TT i wywołująca RoundsEnd()
    /// </summary>
    void RoundForTT()
    {
        RoundEnd();

        whoWonRound = Teams.Terrorist;
        roundsWonByTT++;

        UpdateUiScore(Teams.Terrorist);
        Debug.Log("TT wygrało rundę");
    }

    /// <summary>
    /// Funkcja wstrzymująca rundę i odliczanie czasu do końca rundy.
    /// </summary>
    void RoundEnd()
    {
        isRoundRunning = false;
        roundTimeCountdown = 0;
        
        Debug.Log("Koniec rundy");
    }

    void CheckIfBombIsPlanted()
    {
        isBombPlanted = PlantingBomb.isPlanted;
    }

    /// <summary>
    /// Funkcja do losowania miejsca na obiekcie - używa do losowania miejsca na respawnie.
    /// Można byłoby zrobić sprawdzenie czy w danym miejscu już nie ma gracza
    /// </summary>
    /// <param name="respawnTransform">
    /// Obiekt na którym losujemy miejsce.
    /// </param>
    /// <returns>
    /// Współrzędne losowego miejsca na obiekcie
    /// </returns>
    public static Vector3 RandomPositionOnPlane(Transform plane)
    {
        float respawnX = plane.position.x;
        float respawnZ = plane.position.z;

        float halfX = plane.GetComponent<MeshRenderer>().bounds.size.x / 2;
        float halfZ = plane.GetComponent<MeshRenderer>().bounds.size.z / 2;

        // domyślna wysokość, po jakiej stąpają gracze
        float defaultHeight = 0.06f;

        Vector3 oneCorner = new(respawnX - halfX, defaultHeight, respawnZ - halfZ);
        Vector3 secondCorner = new(respawnX + halfX, defaultHeight, respawnZ + halfZ);

        float randomX = Random.Range(oneCorner.x, secondCorner.x);
        float randomZ = Random.Range(oneCorner.z, secondCorner.z);

        Vector3 randomPosition = new(randomX, defaultHeight, randomZ);

        return randomPosition;
    }

    /// <summary>
    /// Funkcja do uaktualnienia wyniku starcia
    /// </summary>
    /// <param name="whichTeam">
    /// Który zespół zdobył punkt - enum
    /// </param>
    void UpdateUiScore(Teams whichTeam)
    {
        UpdateUiTexts();

        if (whichTeam == Teams.Terrorist)
        {
            int score = roundsWonByTT;
            foreach (TextMeshProUGUI scoreText in scoreTTText)
            {
                scoreText.text = score.ToString();
            }
        }
        else if (whichTeam == Teams.CounterTerrorist)
        {
            int score = roundsWonByCT;
            foreach (TextMeshProUGUI scoreText in scoreCTText)
            {
                scoreText.text = score.ToString();
            }
        }
    }

    /// <summary>
    /// Funkcja wywoływana po zakończeniu rundy. Aktualizuje tekst wyświetlany między rundami - kto wygrał rundę
    /// </summary>
    void UpdateTextBetweenRounds()
    {
        if (whoWonRound == Teams.CounterTerrorist)
        {
            whoWonRoundText.text = "CT";
            whoWonRoundText.color = new Color32(39, 88, 224, 255);
        }
        else if (whoWonRound == Teams.Terrorist)
        {
            whoWonRoundText.text = "TT";
            whoWonRoundText.color = new Color32(224, 39, 39, 255);
        }
    }

    /// <summary>
    /// Funkcja, która iteruje po wszystkich czasach z UI i ustawia im aktualny czas rundy
    /// </summary>
    void SetTimeOnUi()
    {
        UpdateUiTexts();

        foreach (TextMeshProUGUI text in roundTimeText)
        {
            float minutes = Mathf.FloorToInt(roundTimeCountdown / 60);
            float seconds = Mathf.FloorToInt(roundTimeCountdown % 60);

            text.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    /// <summary>
    /// Funkcja ustawiająca liczbę żywych graczy na UI
    /// </summary>
    void SetAlivePlayersOnUi()
    {
        UpdateUiTexts();

        foreach (TextMeshProUGUI text in playersLeftCTText)
        {
            text.text = playersCtList.Count.ToString();
        }

        foreach (TextMeshProUGUI text in playersLeftTTText)
        {
            text.text = playersTtList.Count.ToString();
        }
    }

    /// <summary>
    /// Funkcja ustawiająca liczbę wszystkich graczy na UI
    /// </summary>
    void UpdateAllPlayersCountOnUi()
    {
        UpdateUiTexts();

        int ct = CtPlayersAmount;
        int tt = TtPlayersAmount;

        if (playerTeamChoice == Teams.CounterTerrorist) ct++;
        if (playerTeamChoice == Teams.Terrorist) tt++;

        foreach (TextMeshProUGUI text in allPlayersCTText)
        {
            text.text = ct.ToString();
        }

        foreach (TextMeshProUGUI text in allPlayersTTText)
        {
            text.text = tt.ToString();
        }
    }

    /// <summary>
    /// Funkcja do usunięcia nieistniejących elementów UI z list.
    /// Jeżeli gracz umrze, to następuje Destroy() jego obiektu. Razem z nim usuwane są elementy UI.
    /// W kodzie stają się nullami (Missing) - stąd ta funkcja
    /// </summary>
    void UpdateUiTexts()
    {
        for (int i = roundTimeText.Count - 1; i >= 0; i--)
        {
            if (!roundTimeText[i])
            {
                // ciekawostka przez którą gra powstawała dłużej niż powinna:
                // jeżeli przetrzymujemy obiekt, który po powstaniu dołączył do listy, i chcemy go zniszczyć Destroy()'em
                // to najpierw należy go usunąć z listy a potem wywołać Destroy()
                // w innym przypadku będą problemy z kolejnością, nullami (null pointer exception) i różne inne nieprawidłowości
                TextMeshProUGUI roundTimeTextObject = roundTimeText[i];
                roundTimeText.RemoveAt(i);
                Destroy(roundTimeTextObject);
            }
        }

        for (int i = scoreCTText.Count - 1; i >= 0; i--)
        {
            if (!scoreCTText[i])
            {
                TextMeshProUGUI scoreCTTextObject = scoreCTText[i];
                scoreCTText.RemoveAt(i);
                Destroy(scoreCTTextObject);
            }
        }

        for (int i = scoreTTText.Count - 1; i >= 0; i--)
        {
            if (!scoreTTText[i])
            {
                TextMeshProUGUI scoreTTTextObject = scoreTTText[i];
                scoreTTText.RemoveAt(i);
                Destroy(scoreTTTextObject);
            }
        }

        // players left
        for (int i = playersLeftCTText.Count - 1; i >= 0; i--)
        {
            if (!playersLeftCTText[i])
            {
                TextMeshProUGUI playersLeftCTTextObject = playersLeftCTText[i];
                playersLeftCTText.RemoveAt(i);
                Destroy(playersLeftCTTextObject);
            }
        }

        for (int i = playersLeftTTText.Count - 1; i >= 0; i--)
        {
            if (!playersLeftTTText[i])
            {
                TextMeshProUGUI playersLeftTTTextObject = playersLeftTTText[i];
                playersLeftTTText.RemoveAt(i);
                Destroy(playersLeftTTTextObject);
            }
        }

        // all players
        for (int i = allPlayersCTText.Count - 1; i >= 0; i--)
        {
            if (!allPlayersCTText[i])
            {
                TextMeshProUGUI allPlayersCTTextObject = allPlayersCTText[i];
                allPlayersCTText.RemoveAt(i);
                Destroy(allPlayersCTTextObject);
            }
        }

        for (int i = allPlayersTTText.Count - 1; i >= 0; i--)
        {
            if (!allPlayersTTText[i])
            {
                TextMeshProUGUI allPlayersTTTextObject = allPlayersTTText[i];
                allPlayersTTText.RemoveAt(i);
                Destroy(allPlayersTTTextObject);
            }
        }
    }

    /// <summary>
    /// Funkcja używana do usunięcia wszystkich graczy z podanej w argumencie listy (zespołu) graczy
    /// </summary>
    /// <param name="players">
    /// Lista graczy, w tym kodzie są dwie:
    /// • playersCtList,
    /// • playersTtList.
    /// </param>
    void DestroyEveryPlayerOfList(List<GameObject> players)
    {
        if (players.Count <= 0) return;

        for (int i = players.Count - 1; i >= 0; i--)
        {
            if (players[i] != null)
            {
                GameObject playerObjectToRemove = players[i];
                players.RemoveAt(i);
                Destroy(playerObjectToRemove);
            }
        }
    }

    /// <summary>
    /// Funkcja, która usuwa wszystkie obiekty z UI (dokładniej - z list),
    /// a następnie szuka ich w hierarchii (są na Canvasie postaci [przypiętym do kamery]) i na nowo przydziela do odpowiednich list
    /// </summary>
    void DestroyAndAssignUiTexts()
    {
        for (int i = roundTimeText.Count - 1; i >= 0; i--)
        {
            TextMeshProUGUI roundTimeTextObject = roundTimeText[i];
            roundTimeText.RemoveAt(i);
            Destroy(roundTimeTextObject);
        }

        for (int i = scoreCTText.Count - 1; i >= 0; i--)
        {
            if (scoreCTText[i] != null) continue;
            TextMeshProUGUI scoreCTTextObject = scoreCTText[i];
            scoreCTText.RemoveAt(i);
            Destroy(scoreCTTextObject);
        }

        for (int i = scoreTTText.Count - 1; i >= 0; i--)
        {
            if (scoreTTText[i] != null) continue;
            TextMeshProUGUI scoreTTTextObject = scoreTTText[i];
            scoreTTText.RemoveAt(i);
            Destroy(scoreTTTextObject);
        }

        // players left
        for (int i = playersLeftCTText.Count - 1; i >= 0; i--)
        {
            if (playersLeftCTText[i] != null) continue;
            TextMeshProUGUI playersLeftCTTextObject = playersLeftCTText[i];
            playersLeftCTText.RemoveAt(i);
            Destroy(playersLeftCTTextObject);
        }

        for (int i = playersLeftTTText.Count - 1; i >= 0; i--)
        {
            if (playersLeftTTText[i] != null) continue;
            TextMeshProUGUI playersLeftTTTextObject = playersLeftTTText[i];
            playersLeftTTText.RemoveAt(i);
            Destroy(playersLeftTTTextObject);
        }

        // all players
        for (int i = allPlayersCTText.Count - 1; i >= 0; i--)
        {
            if (allPlayersCTText[i] != null) continue;
            TextMeshProUGUI allPlayersCTTextObject = allPlayersCTText[i];
            allPlayersCTText.RemoveAt(i);
            Destroy(allPlayersCTTextObject);
        }

        for (int i = allPlayersTTText.Count - 1; i >= 0; i--)
        {
            if (allPlayersTTText[i] != null) continue;
            TextMeshProUGUI allPlayersTTTextObject = allPlayersTTText[i];
            allPlayersTTText.RemoveAt(i);
            Destroy(allPlayersTTTextObject);
        }

        List<GameObject> roundTimesGameObjects = new(GameObject.FindGameObjectsWithTag("RoundTime"));
        foreach (var textGameObject in roundTimesGameObjects)
        {
            TextMeshProUGUI text = textGameObject.transform.GetComponent<TextMeshProUGUI>();
            roundTimeText.Add(text);
        }

        List<GameObject> scoreCTGameObjects = new(GameObject.FindGameObjectsWithTag("ScoreCT"));
        foreach (var scoreCTGameObject in scoreCTGameObjects)
        {
            TextMeshProUGUI text = scoreCTGameObject.transform.GetComponent<TextMeshProUGUI>();
            scoreCTText.Add(text);
        }

        List<GameObject> scoreTTGameObjects = new(GameObject.FindGameObjectsWithTag("ScoreTT"));
        foreach (var scoreTTGameObject in scoreTTGameObjects)
        {
            TextMeshProUGUI text = scoreTTGameObject.transform.GetComponent<TextMeshProUGUI>();
            scoreTTText.Add(text);
        }

        // players left
        List<GameObject> playersLeftCTObjects = new(GameObject.FindGameObjectsWithTag("PlayersLeftCT"));
        foreach (var playersLeftCTObject in playersLeftCTObjects)
        {
            TextMeshProUGUI text = playersLeftCTObject.transform.GetComponent<TextMeshProUGUI>();
            playersLeftCTText.Add(text);
        }

        List<GameObject> playersLeftTTObjects = new(GameObject.FindGameObjectsWithTag("PlayersLeftTT"));
        foreach (var playersLeftTTObject in playersLeftTTObjects)
        {
            TextMeshProUGUI text = playersLeftTTObject.transform.GetComponent<TextMeshProUGUI>();
            playersLeftTTText.Add(text);
        }

        // all players
        List<GameObject> allPlayersCTObjects = new(GameObject.FindGameObjectsWithTag("AllPlayersCT"));
        foreach (var allPlayersCTObject in allPlayersCTObjects)
        {
            TextMeshProUGUI text = allPlayersCTObject.transform.GetComponent<TextMeshProUGUI>();
            allPlayersCTText.Add(text);
        }

        List<GameObject> allPlayersTTObjects = new(GameObject.FindGameObjectsWithTag("AllPlayersTT"));
        foreach (var allPlayersTTObject in allPlayersTTObjects)
        {
            TextMeshProUGUI text = allPlayersTTObject.transform.GetComponent<TextMeshProUGUI>();
            allPlayersTTText.Add(text);
        }
    }

    public List<GameObject> GetCtPlayers()
    {
        return playersCtList;
    }
    public List<GameObject> GetTtPlayers()
    {
        return playersTtList;
    }

    /// <summary>
    /// Funkcja sprawdzająca czy gracze żyją, jeśli nie - usuwa ich z listy graczy
    /// </summary>
    public void UpdatePlayers()
    {
        for (int i = playersTtList.Count - 1; i >= 0; i--)
        {
            if (!playersTtList[i]) break;

            float minHP = playersTtList[i].GetComponent<Unit>().MinHP;

            if (playersTtList[i].GetComponent<Unit>().HP <= minHP)
            {
                Debug.Log("Usuwanie martwego TT z listy graczy");
                playersTtList.RemoveAt(i);
            }
        }

        botsTtList = new(playersTtList);

        for (int i = playersCtList.Count - 1; i >= 0; i--)
        {
            if (!playersCtList[i]) break;

            float minHP = playersCtList[i].GetComponent<Unit>().MinHP;

            if (playersCtList[i].GetComponent<Unit>().HP <= minHP)
            {
                Debug.Log("Usuwanie martwego CT z listy graczy");
                playersCtList.RemoveAt(i);
            }
        }    
        
        botsCtList = new(playersCtList);
    }

    /// <summary>
    /// Funkcja do dodawania zwłok do listy.
    /// Używa jej Unit - jeśli umrze jednostka, to tworzy zwłoki i dodaje je do listy `deadBodies`
    /// </summary>
    /// <param name="deadBody">
    /// Obiekt zwłok, żeby można było je wtedy usunąć
    /// </param>
    public void AddDeadBodyToList(GameObject deadBody)
    {
        deadBodies.Add(deadBody);
    }

    /// <summary>
    /// Funkcja do usuwania zwłok z planszy
    /// </summary>
    private void RemoveDeadBodies()
    {
        if (deadBodies.Count == 0) return;

        for (int i = deadBodies.Count - 1; i >= 0; i--)
        {
            GameObject deadBodyToRemove = deadBodies[i];
            deadBodies.RemoveAt(i);
            Destroy(deadBodyToRemove);
        }
    }
    
    /// <summary>
    /// Funkcja do sprawdzania czy gracz jest w grze
    /// </summary>
    private void IsPlayerAlive()
    {
        GameObject playerCt = GameObject.FindGameObjectWithTag("CounterTerroristPlayer");
        GameObject playerTt = GameObject.FindGameObjectWithTag("TerroristPlayer");

        if ( (playerCt != null && !playerCt.Equals(null)) || (playerTt != null && !playerTt.Equals(null)) )
        {
            isPlayerAlive = true;
        } else
        {
            isPlayerAlive = false;
        }
    }

    /// <summary>
    /// Funkcja do usuwania nieaktywnych kamer
    /// </summary>
    private void UpdateCameras()
    {
        for (int i = 1; i < cameras.Count; i++)
        {
            if (cameras[i] == null && cameras[i].Equals(null))
            {
                if (activeCamera == i) activeCamera = 0;
                cameras.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Funkcja wywoływana na bieżąco w Update() aby wiedzieć który z botów jest najbliżej bomby, więc który powinien ją rozbrajać
    /// </summary>
    private void UpdateDefusor()
    {
        if (botsCtList.Count <= 0) return;
        if (botsCtList[0].IsUnityNull()) return;
        if (botsCtList[0].GetComponent<Defusing>().IsUnityNull()) return;
        if (botsCtList[0].GetComponent<BotMove>().IsUnityNull()) return;

        float minDistance = botsCtList[0].GetComponent<Defusing>().distanceToBomb;
        GameObject whichBot = botsCtList[0];

        foreach (GameObject ctBot in botsCtList)
        {
            ctBot.GetComponent<BotMove>().isDefusor = false;
            if (!ctBot.GetComponent<Defusing>().IsUnityNull())
            {
                float nextDistance = ctBot.GetComponent<Defusing>().distanceToBomb;
                if (nextDistance < minDistance)
                {
                    minDistance = nextDistance;
                    whichBot = ctBot;
                }
            }
        }
        whichBot.GetComponent<BotMove>().isDefusor = true;
    }
    private T RandomElementFromList<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
        {
            throw new System.ArgumentException("Lista nie może być pusta.");
        }

        int randomIndex = Random.Range(0, list.Count);
        return list[randomIndex];
    }

    private void CheckIfAnyoneHasBomb()
    {
        if (playersTtList.Count <= 0) return;

        foreach (GameObject player in playersTtList)
        {
            if (player.GetComponent<PlantingBomb>())
            {
                if (player.GetComponent<PlantingBomb>().hasBomb)
                {
                    return;
                }
            }
        }

        GameObject randomTerrorist = RandomElementFromList(playersTtList);
        if (!randomTerrorist.GetComponent<PlantingBomb>().IsUnityNull())
        {
            randomTerrorist.GetComponent<PlantingBomb>().hasBomb = true;
        }
    }
    private void CheckIfAnyoneHasDefuseKit() 
    {
        if (playersCtList.Count <= 0) return;

        foreach (GameObject player in playersCtList)
        {
            if (player.GetComponent<Defusing>())
            {
                if (player.GetComponent<Defusing>().hasDefuseKit)
                {
                    return;
                }
            }
        }

        GameObject randomCounterTerrorist = RandomElementFromList(playersCtList);
        if (!randomCounterTerrorist.GetComponent<Defusing>().IsUnityNull())
        {
            randomCounterTerrorist.GetComponent<Defusing>().hasDefuseKit = true;
        }
    }
}
