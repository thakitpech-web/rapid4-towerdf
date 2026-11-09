using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the game phase state machine. Other systems react to it; they never change phase themselves.
/// Flow: StartLoop (Survival) -> EndLoop (Result) -> EnterBuildPhase (Build) -> StartLoop ...
/// The game ends in GameOver after the last loop is cleared or after the maximum number of losses.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private GameConfig config;
    [SerializeField] private DifficultyPreset difficulty;

    [Header("Scene references")]
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private PlayerCharacter player;
    [SerializeField] private Base baseUnit;
    [SerializeField] private UIManager uiManager;

    public GameConfig Config => config;
    public DifficultyPreset Difficulty => difficulty;
    public PlayerCharacter Player => player;
    public Base Base => baseUnit;
    public LoopTimer Timer { get; private set; }

    public int CurrentLoop { get; private set; } = 1;
    public int TotalLoops => enemySpawner.LoopCount;
    public int Losses { get; private set; }
    public int MaxLosses => config.maxLosses;
    public int Orbs { get; private set; }
    public GamePhase Phase { get; private set; } = GamePhase.Build;

    /// <summary>Only meaningful in GameOver: true if every loop was cleared.</summary>
    public bool IsVictory { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Timer = new LoopTimer(config.loopDuration);
        Timer.OnTimeUp += HandleTimeUp;
    }

    private void OnEnable()
    {
        player.OnDied += HandlePlayerDied;
        baseUnit.OnDied += HandleBaseDied;
    }

    private void OnDisable()
    {
        player.OnDied -= HandlePlayerDied;
        baseUnit.OnDied -= HandleBaseDied;
    }

    private void Start()
    {
        buildManager.UnlockPads(CurrentLoop);
        StartLoop();
    }

    private void Update()
    {
        if (Phase != GamePhase.Survival) return;

        Timer.Tick(Time.deltaTime);
        if (Phase == GamePhase.Survival)          // OnTimeUp may have ended the loop
            enemySpawner.Tick(Timer.Elapsed);
    }

    /// <summary>Starts (or replays) CurrentLoop: full HP everywhere, new spawn schedule, timer at 0.</summary>
    public void StartLoop()
    {
        if (Phase != GamePhase.Build) return;

        Timer.Reset();
        player.ResetForLoop();
        baseUnit.ResetForLoop(difficulty.BaseHP);   // base HP passive is added in step 4
        // towers revive here in step 6
        enemySpawner.BuildSchedule(CurrentLoop);

        Phase = GamePhase.Survival;
        uiManager.HidePanels();
    }

    /// <summary>Ends the current loop. Ignored unless a loop is running.</summary>
    public void EndLoop(bool won, string reason)
    {
        if (Phase != GamePhase.Survival && Phase != GamePhase.LevelUp) return;

        // pending level-ups are resolved first and orbs are added in step 4
        int orbsGained = 0;

        if (won)
        {
            if (CurrentLoop >= TotalLoops)
            {
                IsVictory = true;
                GameOver(reason);
                return;
            }
            CurrentLoop++;
        }
        else
        {
            Losses++;
            if (Losses >= MaxLosses)
            {
                IsVictory = false;
                GameOver(reason);
                return;
            }
        }

        Phase = GamePhase.Result;
        uiManager.ShowResult(won, reason, orbsGained);
    }

    /// <summary>Called by the result panel button.</summary>
    public void ContinueAfterResult()
    {
        if (Phase == GamePhase.Result) EnterBuildPhase();
        else if (Phase == GamePhase.GameOver) Restart();
    }

    public void EnterBuildPhase()
    {
        enemySpawner.ClearAll();
        Projectile.ClearAll();
        buildManager.UnlockPads(CurrentLoop);

        Phase = GamePhase.Build;
        uiManager.ShowBuild();
    }

    public void GameOver(string reason)
    {
        Phase = GamePhase.GameOver;
        uiManager.ShowGameOver(IsVictory, reason);
    }

    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleTimeUp() => EndLoop(true, $"Survived {Timer.Duration:0} seconds");
    private void HandleBaseDied(Unit _) => EndLoop(false, "The base was destroyed");
    private void HandlePlayerDied(Unit _) => EndLoop(false, "The character fell");

    private void OnDestroy()
    {
        if (Timer != null) Timer.OnTimeUp -= HandleTimeUp;
        if (Instance == this) Instance = null;
    }
}
