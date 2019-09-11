using UnityEngine;
using TMPro;

public enum SceneBenchmarkType
{
    Invalid = -1,

    OOPST,
    OOPMT,
    ECS, // < ECS is giving me errors due to scene loads so will do it in a isolated benchmark
    Compute,

    Count
}

public class BenchmarkSystem : MonoBehaviourSingleton<BenchmarkSystem>
{
    #region Public Attributes

    [Header("Parameters")]
    public int numberOfBoids = 8192;
    public float timePerSceneInSeconds = 32.0f;
    public float ignoreSecondsAfterSceneLoad = 2.0f;

    [Header("Text labels")]
    public TextMeshProUGUI sceneTitle = null;
    public TextMeshProUGUI currNumBoids = null;
    public TextMeshProUGUI avgFPS = null;
    public UnityEngine.UI.Text numBoids = null;

    [Header("ECS benchmark exclusive")]
    public SpawnRandomInSphereProxy spawner1;
    public SpawnRandomInSphereProxy spawner2;

    #endregion

    #region Private Attributes

    public string SceneTitle { set { sceneTitle.text = value; } }
    public string AvgFPS { set { avgFPS.text = value; } }

    private readonly SceneBenchmark[] benchmarks = new SceneBenchmark[(int) SceneBenchmarkType.Count];
    private SceneBenchmark currRunningBenchmark = null;

    #endregion

    public bool IsBenchmarkRunning { get { return currRunningBenchmark != null && !currRunningBenchmark.IsDone; } }
    public SceneBenchmark CurrRunningBenchmark
    {
        get { return currRunningBenchmark; }
        set { currRunningBenchmark = value; }
    }
    protected override bool DestroyOnLoad { get { return false; } }

    #region MonoBehaviour Methods

    private void Start()
    {
        // lazy but fast way to control this: ECS scene gave me issues when swapping scenes, so if we're in the standalone version of the ECS benchmark, launch the benchmark as soon as it starts
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Equals("BENCH_ECS"))
        {
            int spawned = spawner1.Value.count + spawner2.Value.count;
            currNumBoids.text = spawned.ToString();
            CreateBenchmarks();
            currRunningBenchmark = benchmarks[(int) SceneBenchmarkType.ECS];
            benchmarks[(int) SceneBenchmarkType.ECS].StartBenchmark(false);
        }
    }

    private void Update()
    {
        // someone provoked singleton to automatically instantiate itself but if we did not launch from the benchmark scene we do not want it
        if (sceneTitle == null || currNumBoids == null || avgFPS == null || numBoids == null)
            return;

        string s = numBoids.text;
        bool okNumBoids = int.TryParse(s, out numberOfBoids);

        if (Input.GetKeyDown(KeyCode.Space) && okNumBoids)
        {
            currNumBoids.text = numBoids.text;
            currRunningBenchmark?.ForceEndBenchmark(false);
            currRunningBenchmark = null;
            CreateBenchmarks();
            currRunningBenchmark = benchmarks[0];
            benchmarks[0].StartBenchmark(true);
        }

        if (currRunningBenchmark == null)
            return;

        float dt = Time.deltaTime;

        currRunningBenchmark.UpdateBenchmark(dt);

        if (currRunningBenchmark.IsDone)
        {
            int nextBench = (int) currRunningBenchmark.NextSceneBenchType;

            if (nextBench < benchmarks.Length && currRunningBenchmark.NextSceneBenchType != SceneBenchmarkType.Invalid)
            {
                currRunningBenchmark = benchmarks[nextBench];
                currRunningBenchmark.StartBenchmark(true);
            }
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Initialize singleton
    /// </summary>
    /// <param name="force"></param>
    /// <returns></returns>
    protected override bool Init(bool force)
    {
        if (sceneTitle == null || currNumBoids == null || avgFPS == null || numBoids == null)
            return false;

        // if already initialized and not forced won't init again
        bool didInitAgain = base.Init(force);

        if (didInitAgain)
        {
            Application.targetFrameRate = int.MaxValue;
            QualitySettings.vSyncCount = 0;
        }

        currNumBoids.text = numberOfBoids.ToString();

        return didInitAgain;
    }

    /// <summary>
    /// Create benchmarks
    /// </summary>
    private void CreateBenchmarks()
    {
        for (int i = 0; i < (int) SceneBenchmarkType.Count; i++)
        {
            SceneBenchmark bench = null;

            switch ((SceneBenchmarkType) i)
            {
                case SceneBenchmarkType.OOPST:
                    bench = new OOPSTSceneBenchmark(timePerSceneInSeconds, ignoreSecondsAfterSceneLoad);
                    break;
                case SceneBenchmarkType.OOPMT:
                    bench = new OOPMTSceneBenchmark(timePerSceneInSeconds, ignoreSecondsAfterSceneLoad);
                    break;
                case SceneBenchmarkType.ECS:
                    bench = new ECSSceneBenchmark(timePerSceneInSeconds, ignoreSecondsAfterSceneLoad);
                    break;
                case SceneBenchmarkType.Compute:
                    bench = new ComputeSceneBenchmark(timePerSceneInSeconds, ignoreSecondsAfterSceneLoad);
                    break;
            }

            benchmarks[i] = bench;
        }
    }

    #endregion
}