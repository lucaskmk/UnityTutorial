using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Controla a partida: cronômetro, gasolina, spawn de galões e viaturas, pausa e fim de jogo.
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Referências")]
    public PlayerMovement player;
    public GameObject gasPrefab;
    public GameObject policePrefab;
    public Transform[] chaserSpawnPoints;
    public GameObject pausePanel;
    public GameObject pauseFirstButton;

    [Header("Gasolina")]
    public float fuelDrainIdle = 2.5f;   // por segundo, parado
    public float fuelDrainMoving = 6f;   // por segundo, andando
    public float lowFuelThreshold = 25f;

    [Header("Desafio")]
    public int simultaneousCans = 3;
    public int[] chaserThresholds = { 2, 6, 10 }; // galões necessários para surgir cada nova viatura

    [Header("Áudio")]
    public AudioSource music;
    public AudioSource sfx;
    public AudioClip lowFuelClip;
    public AudioClip victoryClip;
    public AudioClip gameOverClip;

    int chasersSpawned;
    float lowFuelTimer;
    bool paused;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        GameController.Init();
    }

    void Start()
    {
        for (int i = 0; i < simultaneousCans; i++) SpawnGas();
        HUD.Instance.ShowMessage($"Colete {GameController.GasGoal} galões e fuja da polícia!", 3f);
    }

    void Update()
    {
        if (PausePressed() && GameController.IsPlaying) TogglePause();
        if (paused || !GameController.IsPlaying) return;

        // Cronômetro
        GameController.time += Time.deltaTime;

        // O tempo gasta a gasolina: mais rápido andando
        GameController.fuel -= (player.IsMoving ? fuelDrainMoving : fuelDrainIdle) * Time.deltaTime;

        if (GameController.fuel < lowFuelThreshold)
        {
            lowFuelTimer -= Time.deltaTime;
            if (lowFuelTimer <= 0f)
            {
                sfx.PlayOneShot(lowFuelClip);
                lowFuelTimer = 1f;
            }
        }

        if (GameController.fuel <= 0f)
        {
            GameController.fuel = 0f;
            EndGame(GameResult.OutOfFuel);
        }
        else if (GameController.lives <= 0)
        {
            EndGame(GameResult.Busted);
        }
    }

    static bool PausePressed()
    {
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;
        return (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame))
            || (gamepad != null && gamepad.startButton.wasPressedThisFrame);
    }

    public void OnGasCollected()
    {
        if (GameController.collected >= GameController.GasGoal)
        {
            EndGame(GameResult.Victory);
            return;
        }

        SpawnGas();

        if (chasersSpawned < chaserThresholds.Length && GameController.collected >= chaserThresholds[chasersSpawned])
        {
            chasersSpawned++;
            SpawnChaser();
        }
    }

    void SpawnGas()
    {
        Vector2 playerPos = player.transform.position;
        var cans = GameObject.FindGameObjectsWithTag("Coletavel");

        for (int attempt = 0; attempt < 100; attempt++)
        {
            var cell = new Vector2Int(Random.Range(0, CityMap.Width), Random.Range(0, CityMap.Height));
            if (!CityMap.IsRoad(cell)) continue;
            Vector2 pos = CityMap.CellToWorld(cell);
            if (Vector2.Distance(pos, playerPos) < 5f) continue;

            bool tooClose = false;
            foreach (var c in cans)
                if (c != null && Vector2.Distance(c.transform.position, pos) < 3f) tooClose = true;
            if (tooClose) continue;

            // Não nasce em cima de óleo
            bool onOil = false;
            foreach (var hit in Physics2D.OverlapCircleAll(pos, 0.6f))
                if (hit.CompareTag("Oleo")) onOil = true;
            if (onOil) continue;

            Instantiate(gasPrefab, pos, Quaternion.identity);
            return;
        }
    }

    void SpawnChaser()
    {
        Transform best = chaserSpawnPoints[0];
        foreach (var p in chaserSpawnPoints)
            if (Vector2.Distance(p.position, player.transform.position) > Vector2.Distance(best.position, player.transform.position))
                best = p;

        var police = Instantiate(policePrefab, best.position, Quaternion.identity).GetComponent<PoliceCar>();
        police.mode = PoliceMode.Chaser;
        HUD.Instance.ShowMessage("Mais uma viatura na sua cola!", 2.5f);
    }

    void EndGame(GameResult result)
    {
        GameController.result = result;
        music.Stop();
        sfx.PlayOneShot(result == GameResult.Victory ? victoryClip : gameOverClip);

        if (result == GameResult.Victory && (GameController.BestTime <= 0f || GameController.time < GameController.BestTime))
        {
            GameController.BestTime = GameController.time;
            GameController.newRecord = true;
        }

        HUD.Instance.ShowMessage(result switch
        {
            GameResult.Victory => "TANQUE CHEIO! VOCÊ ESCAPOU!",
            GameResult.OutOfFuel => "ACABOU A GASOLINA!",
            _ => "PRESO PELA POLÍCIA!",
        }, 10f);

        StartCoroutine(LoadEndScene());
    }

    IEnumerator LoadEndScene()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        SceneManager.LoadScene("EndGame");
    }

    public void TogglePause()
    {
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
        pausePanel.SetActive(paused);
        if (paused) SelectionKeeper.SetDefault(pauseFirstButton);
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene("Menu");
    }
}
