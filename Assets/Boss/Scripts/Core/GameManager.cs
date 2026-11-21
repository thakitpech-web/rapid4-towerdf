using UnityEngine;
using UnityEngine.SceneManagement;

/// Owns the game phase state machine. Other systems react to it; they never change phase themselves.
/// Flow: StartLoop (Survival) -> [LevelUp ...] -> EndLoop (Result) -> EnterBuildPhase (Build) -> StartLoop ...
/// The game ends in GameOver after the last loop is cleared or after the maximum number of losses.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private GameConfig config;
    [SerializeField] private DifficultyPreset difficulty;

    [Header("Scene references")]
    [SerializeField] private GridMap grid;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private LevelSystem levelSystem;
    [SerializeField] private UpgradeManager upgradeManager;
    [SerializeField] private PlayerCharacter player;
    [SerializeField] private Base baseUnit;
    [SerializeField] private UIManager uiManager;

    public GameConfig Config => config;
    public DifficultyPreset Difficulty => difficulty;
    public LevelSystem Levels => levelSystem;
    public UpgradeManager Upgrades => upgradeManager;
    public PlayerCharacter Player => player;
    public Base Base => baseUnit;
    public LoopTimer Timer { get; private set; }
    public GridMap Grid => grid;
    public CameraController CameraView => cameraController;
    public BuildManager Builder => buildManager;
    public EnemySpawner Spawner => enemySpawner;

    public int CurrentLoop { get; private set; } = 1;
    public int TotalLoops => enemySpawner.LoopCount;
    public int Losses { get; private set; }
    public int MaxLosses => config.maxLosses;
    public int Orbs { get; private set; }
    public GamePhase Phase { get; private set; } = GamePhase.Build;

    /// Only meaningful in GameOver: true if every loop was cleared.
    public bool IsVictory { get; private set; }

    /// Base max HP from the difficulty preset plus the base HP passive.
    public float BaseMaxHP =>
        Mathf.Round(difficulty.BaseHP * (1f + upgradeManager.GetModifier(UpgradeType.BaseMaxHP)));

    /// Pays for a tower. Returns false (and spends nothing) if there are not enough orbs.
    public bool TrySpendOrbs(int amount)
    {
        if (amount < 0 || Orbs < amount) return false;
        Orbs -= amount;
        return true;
    }

    public void AddOrbs(int amount) => Orbs += amount;

    // A loop end that waits for pending level-ups to be resolved first.
    private bool hasPendingEnd;
    private bool pendingEndWon;
    private string pendingEndReason;

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

        grid.Init(config);
        baseUnit.PlaceOn(grid);
        cameraController.Init(config, grid);
    }

    private void OnEnable()
    {
        player.OnDied += HandlePlayerDied;
        baseUnit.OnDied += HandleBaseDied;
        levelSystem.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        player.OnDied -= HandlePlayerDied;
        baseUnit.OnDied -= HandleBaseDied;
        levelSystem.OnLevelUp -= HandleLevelUp;
    }

    private void Start()
    {
        StartLoop();
    }

    private void Update()
    {
        if (Phase != GamePhase.Survival) return;

        Timer.Tick(Time.deltaTime);
        if (Phase != GamePhase.Survival) return;  // OnTimeUp may have ended the loop

        enemySpawner.Tick(Timer.Elapsed);
    }

    private void LateUpdate()
    {
        if (Phase == GamePhase.Survival || Phase == GamePhase.LevelUp)
            cameraController.FitWholeMap();   // fixed view; also follows window resizes
    }

    // ---------- Loop ----------

    /// Starts (or replays) CurrentLoop: full HP everywhere, new spawn schedule, timer at 0.
    public void StartLoop()
    {
        if (Phase != GamePhase.Build) return;

        Timer.Reset();
        player.ResetForLoop();
        baseUnit.ResetForLoop(BaseMaxHP);
        buildManager.ExitBuild();
        buildManager.ResetTowersForLoop();
        cameraController.FitWholeMap();
        grid.RefreshVisuals(CurrentLoop);
        enemySpawner.BuildSchedule(CurrentLoop);

        Phase = GamePhase.Survival;
        uiManager.HidePanels();
    }

    /// Ends the current loop. Ignored unless a loop is running.
    /// If level-ups are pending, they are resolved first and the result is shown afterwards.
    public void EndLoop(bool won, string reason)
    {
        if (hasPendingEnd) return;
        if (Phase != GamePhase.Survival && Phase != GamePhase.LevelUp) return;

        hasPendingEnd = true;
        pendingEndWon = won;
        pendingEndReason = reason;

        if (levelSystem.PendingLevelUps > 0)
        {
            if (Phase != GamePhase.LevelUp) OpenLevelUp();
        }
        else
        {
            FinishLoop();
        }
    }

    private void FinishLoop()
    {
        hasPendingEnd = false;
        bool won = pendingEndWon;
        string reason = pendingEndReason;

        int levelOrbs = levelSystem.ConvertToOrbs();
        int bonusOrbs = Mathf.Max(0, config.bonusOrbsPerLoop);
        AddOrbs(levelOrbs + bonusOrbs);

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
        uiManager.ShowResult(won, reason, levelOrbs, bonusOrbs);
    }

    /// Called by the result panel button.
    public void ContinueAfterResult()
    {
        if (Phase == GamePhase.Result) EnterBuildPhase();
        else if (Phase == GamePhase.GameOver) Restart();
    }

    public void EnterBuildPhase()
    {
        enemySpawner.ClearAll();
        Projectile.ClearAll();
        ExpGem.ClearAll();
        cameraController.FitWholeMap();
        grid.RefreshVisuals(CurrentLoop);       // lanes and pads for the loop about to be played

        Phase = GamePhase.Build;
        buildManager.EnterBuild();
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

    // ---------- Level-up ----------

    private void HandleLevelUp()
    {
        if (Phase == GamePhase.Survival) OpenLevelUp();
        // Already in LevelUp: the extra level waits in PendingLevelUps.
    }

    private void OpenLevelUp()
    {
        Phase = GamePhase.LevelUp;
        uiManager.ShowLevelUp(upgradeManager.RollChoices(config.upgradeChoices));
    }

    /// Called by the level-up panel when a card is picked.
    public void ChooseUpgrade(Upgrade upgrade)
    {
        if (Phase != GamePhase.LevelUp) return;

        upgradeManager.Apply(upgrade);
        levelSystem.ResolveLevelUp();
        uiManager.HideLevelUp();

        if (levelSystem.PendingLevelUps > 0) OpenLevelUp();
        else if (hasPendingEnd) FinishLoop();
        else Phase = GamePhase.Survival;
    }

    // ---------- Event handlers ----------

    private void HandleTimeUp() => EndLoop(true, $"Survived {Timer.Duration:0} seconds");
    private void HandleBaseDied(Unit _) => EndLoop(false, "The base was destroyed");
    private void HandlePlayerDied(Unit _) => EndLoop(false, "The character fell");

    private void OnDestroy()
    {
        if (Timer != null) Timer.OnTimeUp -= HandleTimeUp;
        if (Instance == this) Instance = null;
    }
}
