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
    private float roundTime = 10f * 60;
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

    private Camera waitingCamera;

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
    /// 0 to kamera `waitingCamera`, czyli widok z góry z wynikiem
    /// kamery od 1 w górę to kamery botów
    /// </summary>
    private int activeCamera = 0;

    /// <summary>
    /// Lista kamer dostępnych do poglądu, gdy gracz nie żyje bądź jest widzem
    /// </summary>
    private List<Camera> cameras = new();

    private float howLongPressingEsc = 0f;
    private float requiredHoldTimeToExit = 10f;

    private void Awake()
    {
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
        waitingCamera = waitingCameraObject.GetComponent<Camera>();
        cameras.Add(waitingCamera);

        whoWonRoundText = GameObject.FindWithTag("WhoWonRound").GetComponent<TextMeshProUGUI>();

        RoundStart();
    }

    void Update()
    {
        // z klatki na klatkę sprawdzam czy wszyscy żyją i aktualizuję im czas na UI
        SetTimeOnUi();
        SetAlivePlayersOnUi();
        UpdatePlayers();
        UpdateCameras();

        // jeżeli trzymany jest ESC przez 10s to gracz wraca do menu głównego
        if (Input.GetKey(KeyCode.Escape))
        {
            howLongPressingEsc += Time.deltaTime;
            if (howLongPressingEsc >= requiredHoldTimeToExit)
            {
                SceneManager.LoadScene(0);
            }
        }
        else howLongPressingEsc = 0f;


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
                    int amountOfAllPlayers = playersCtList.Count + playersTtList.Count;
                    cameras[activeCamera].enabled = false;

                    if (activeCamera == amountOfAllPlayers)
                    {
                        activeCamera = 0;
                    } else activeCamera++;

                    cameras[activeCamera].enabled = true;
                } 
                else if (Input.GetMouseButtonDown(1))
                {
                    int amountOfAllPlayers = playersCtList.Count + playersTtList.Count;
                    cameras[activeCamera].enabled = false;

                    if (activeCamera -1 < 0)
                    {
                        activeCamera = amountOfAllPlayers;
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
                    cameras[activeCamera].enabled = true;
                }
                catch (System.Exception)
                {
                    cameras.RemoveAt(activeCamera);
                    activeCamera = 0;
                    cameras[activeCamera].enabled = true;
                }
            }

            if (roundTimeCountdown > 0)
            {
                roundTimeCountdown -= Time.deltaTime;
            }
            else
            {
                // jeżeli czas rundy dobiegnie końca i przeżyje co najmniej jeden gracz z każdej drużyny to rundę wygrywa CT
                // na razie to nie ma za bardzo sensu, nabieże go jak wdrożę bombę (misją antyterrorystów jest powstrzymanie terrorystów przed jej podłożeniem)
                // jeżeli nie podłożą - sukces CT
                Debug.Log("CT wygrało na czas");
                RoundForCT();
            }

            // jeżeli w trakcie trwania rundy któryś z zespołów zdoła wyeliminować przeciwny to wygrywa rundę
            if (playersCtList.Count == 0 || playersTtList.Count == 0)
            {
                if (playersTtList.Count == 0) {
                    Debug.Log("CT wygrało przez zlikwidowanie TT");
                    RoundForCT();
                }
                else if (playersCtList.Count == 0) {
                    Debug.Log("TT wygrało przez zlikwidowanie CT");
                    RoundForTT();
                }
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

        whoWonRoundText.text = "";

        respawnCT = GameObject.FindGameObjectWithTag("RespawnCT").transform;
        respawnTT = GameObject.FindGameObjectWithTag("RespawnTT").transform;

        // restartujemy wszystkich graczy - na wypadek, gdyby jacyś przeżyli
        DestroyEveryPlayerOfList(playersCtList);
        DestroyEveryPlayerOfList(playersTtList);

        if (playerTeamChoice == Teams.CounterTerrorist)
        {
            Vector3 randomSpawnPositionVector = RandomSpawnPosition(respawnCT);
            GameObject player = Instantiate(PlayerCT, randomSpawnPositionVector, Quaternion.identity);
            playersCtList.Add(player);
            isPlayerAlive = true;
        }
        else if (playerTeamChoice == Teams.Terrorist)
        {
            Vector3 randomSpawnPositionVector = RandomSpawnPosition(respawnTT);
            GameObject player = Instantiate(PlayerTT, randomSpawnPositionVector, Quaternion.identity);
            playersTtList.Add(player);
            isPlayerAlive = true;
        } else
        {
            isPlayerAlive = false;
        }

        for (int i = 0; i < CtPlayersAmount; i++)
        {
            Vector3 randomSpawnPositionVector = RandomSpawnPosition(respawnCT);
            GameObject newCtBot = Instantiate(BotCT, randomSpawnPositionVector, Quaternion.identity);
            playersCtList.Add(newCtBot);
            cameras.Add(newCtBot.GetComponentInChildren<Camera>());
        }

        for (int i = 0; i < TtPlayersAmount; i++)
        {
            Vector3 randomSpawnPositionVector = RandomSpawnPosition(respawnTT);
            GameObject newTtBot = Instantiate(BotTT, randomSpawnPositionVector, Quaternion.identity);
            playersTtList.Add(newTtBot);
            cameras.Add(newTtBot.GetComponentInChildren<Camera>());
        }

        // zniszczenie obiektów UI których już nie ma w grze i pobranie nowych z graczy
        DestroyAndAssignUiTexts();

        UpdateUiScore(Teams.CounterTerrorist);
        UpdateUiScore(Teams.Terrorist);

        UpdateAllPlayersCountOnUi();

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
    Vector3 RandomSpawnPosition(Transform respawnTransform)
    {
        float respawnX = respawnTransform.position.x;
        float respawnZ = respawnTransform.position.z;

        float halfX = respawnTransform.GetComponent<MeshRenderer>().bounds.size.x / 2;
        float halfZ = respawnTransform.GetComponent<MeshRenderer>().bounds.size.z / 2;

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
}
