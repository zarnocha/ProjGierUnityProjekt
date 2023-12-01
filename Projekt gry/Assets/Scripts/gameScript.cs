using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

public class GameScript : MonoBehaviour
{
    [Header("Opcje gameplayu")]
    [Header("Wybór zespołu gracza")]
    public Teams playerTeamChoice = Teams.Terrorist;

    [Header("Ilość graczy AI w każdej drużynie")]
    [Range(0f, 5f)]
    [SerializeField]
    private int CtPlayersAmount = 5;
    [Range(0f, 5f)]
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
        Terrorist
    }

    // listy graczy w grze
    private List<GameObject> playersCtList = new();
    private List<GameObject> playersTtList = new();

    // lista zwłok
    private List<GameObject> deadBodies = new();

    // lista poszczególnych elementów UI każdego gracza
    private List<Text> roundTimeText = new();
    private List<Text> scoreTTText = new();
    private List<Text> scoreCTText = new();

    private Transform respawnCT;
    private Transform respawnTT;

    private bool isRoundRunning = false;

    // wynik gry
    private int roundsWonByCT = 0;
    private int roundsWonByTT = 0;

    void Start()
    {
        // kopiujemy oryginalny czas do drugiej zmiennej po to, żeby mieć dostęp do oryginalnej wartości między rundami
        // czas rundy polega na odejmowaniu Time.deltaTime od zadeklarowanej wartości, stąd kopia
        roundTimeCountdown = roundTime;
        // to samo poniżej, tylko dla odstępu między rundami
        timeBetweenRoundsCountdown = TimeBetweenRounds;

        RoundStart();
    }

    void Update()
    {
        // z klatki na klatkę sprawdzam czy wszyscy żyją i aktualizuję im czas na UI
        SetTimeOnUi();
        UpdatePlayers();

        if (isRoundRunning)
        {
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
        else if (!isRoundRunning)
        {
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
        }
        else if (playerTeamChoice == Teams.Terrorist)
        {
            Vector3 randomSpawnPositionVector = RandomSpawnPosition(respawnTT);
            GameObject player = Instantiate(PlayerTT, randomSpawnPositionVector, Quaternion.identity);
            playersTtList.Add(player);
        }

        for (int i = 0; i < CtPlayersAmount; i++)
        {
            Vector3 randomSpawnPositionVector = RandomSpawnPosition(respawnCT);
            GameObject newCtBot = Instantiate(BotCT, randomSpawnPositionVector, Quaternion.identity);
            playersCtList.Add(newCtBot);
        }

        for (int i = 0; i < TtPlayersAmount; i++)
        {
            Vector3 randomSpawnPositionVector = RandomSpawnPosition(respawnTT);
            GameObject newTtBot = Instantiate(BotTT, randomSpawnPositionVector, Quaternion.identity);
            playersTtList.Add(newTtBot);
        }

        // zniszczenie obiektów UI których już nie ma w grze i pobranie nowych z graczy
        DestroyAndAssignUiTexts();

        // ustawiamy graczom aktualny wynik gry
        UpdateUiScore(Teams.CounterTerrorist);
        UpdateUiScore(Teams.Terrorist);

        isRoundRunning = true;
    }

    /// <summary>
    /// Funkcja deklarująca zwycięstwo CT i wywołująca RoundsEnd()
    /// </summary>
    void RoundForCT()
    {
        RoundEnd();
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
            foreach (Text scoreText in scoreTTText)
            {
                scoreText.text = score.ToString();
            }
        }
        else if (whichTeam == Teams.CounterTerrorist)
        {
            int score = roundsWonByCT;
            foreach (Text scoreText in scoreCTText)
            {
                scoreText.text = score.ToString();
            }
        }
    }

    /// <summary>
    /// Funkcja, która iteruje po wszystkich czasach z UI i ustawia im aktualny czas rundy
    /// </summary>
    void SetTimeOnUi()
    {
        UpdateUiTexts();

        foreach (Text text in roundTimeText)
        {
            float minutes = Mathf.FloorToInt(roundTimeCountdown / 60);
            float seconds = Mathf.FloorToInt(roundTimeCountdown % 60);

            text.text = string.Format("{0:00}:{1:00}", minutes, seconds);
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
                Text roundTimeTextObject = roundTimeText[i];
                roundTimeText.RemoveAt(i);
                Destroy(roundTimeTextObject);
            }
        }

        for (int i = scoreCTText.Count - 1; i >= 0; i--)
        {
            if (!scoreCTText[i])
            {
                Text scoreCTTextObject = scoreCTText[i];
                scoreCTText.RemoveAt(i);
                Destroy(scoreCTTextObject);
            }
        }

        for (int i = scoreTTText.Count - 1; i >= 0; i--)
        {
            if (!scoreTTText[i])
            {
                Text scoreTTTextObject = scoreTTText[i];
                scoreTTText.RemoveAt(i);
                Destroy(scoreTTTextObject);
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
            Text roundTimeTextObject = roundTimeText[i];
            roundTimeText.RemoveAt(i);
            Destroy(roundTimeTextObject);
        }

        for (int i = scoreCTText.Count - 1; i >= 0; i--)
        {
            Text scoreCTTextObject = scoreCTText[i];
            scoreCTText.RemoveAt(i);
            Destroy(scoreCTTextObject);
        }

        for (int i = scoreTTText.Count - 1; i >= 0; i--)
        {
            Text scoreTTTextObject = scoreTTText[i];
            scoreTTText.RemoveAt(i);
            Destroy(scoreTTTextObject);
        }

        List<GameObject> roundTimesGameObjects = new(GameObject.FindGameObjectsWithTag("RoundTime"));
        foreach (var textGameObject in roundTimesGameObjects)
        {
            Text text = textGameObject.transform.GetComponent<Text>();
            roundTimeText.Add(text);
        }

        List<GameObject> scoreCTGameObjects = new(GameObject.FindGameObjectsWithTag("ScoreCT"));
        foreach (var scoreCTGameObject in scoreCTGameObjects)
        {
            Text text = scoreCTGameObject.transform.GetComponent<Text>();
            scoreCTText.Add(text);
        }

        List<GameObject> scoreTTGameObjects = new(GameObject.FindGameObjectsWithTag("ScoreTT"));
        foreach (var scoreTTGameObject in scoreTTGameObjects)
        {
            Text text = scoreTTGameObject.transform.GetComponent<Text>();
            scoreTTText.Add(text);
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
                GameObject playerToRemove = playersTtList[i];
                playersTtList.RemoveAt(i);
                Destroy(playerToRemove);
            }
        }

        for (int i = playersCtList.Count - 1; i >= 0; i--)
        {
            if (!playersCtList[i]) break;

            float minHP = playersCtList[i].GetComponent<Unit>().MinHP;

            if (playersCtList[i].GetComponent<Unit>().HP <= minHP)
            {
                Debug.Log("Usuwanie martwego CT z listy graczy");
                GameObject playerToRemove = playersCtList[i];
                playersCtList.RemoveAt(i);
                Destroy(playerToRemove);
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
}
