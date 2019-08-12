using UnityEngine;

public enum SceneBenchmarkType
{
    Invalid = -1,

    OOPST,
    OOPMT,
    ECS,
    Compute,

    Count
}

public class BenchmarkSystem : MonoBehaviourSingleton<BenchmarkSystem>
{
    #region Public Attributes

    [Header("Parameters")]
    public int numberOfBoids = 4096;
    public float timePerSceneInSeconds = 60.0f;
    public float ignoreSecondsAfterSceneLoad = 3.0f;

    #endregion

    #region Private Attributes

    private readonly SceneBenchmark[] benchmarks = new SceneBenchmark[(int) SceneBenchmarkType.Count];
    private SceneBenchmark currRunningBenchmark = null;

    protected override bool DestroyOnLoad { get { return false; } }

    #endregion

    #region MonoBehaviour Methods

    private void Update()
    {
        if (currRunningBenchmark == null)
            return;

        float dt = Time.deltaTime;

        currRunningBenchmark.UpdateBenchmark(dt);

        if (currRunningBenchmark.IsDone)
        {
            int nextBench = (int) currRunningBenchmark.NextSceneBenchType;
            currRunningBenchmark = benchmarks[nextBench];

            currRunningBenchmark.StartBenchmark();
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
        // if already initialized and not force won't init again
        bool didInitAgain = base.Init(force);

        if (didInitAgain)
        {
            Application.targetFrameRate = int.MaxValue;
            QualitySettings.vSyncCount = 0;
        }

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

    /// <summary>
    /// Called when the user clicks the button to start the benchmark
    /// </summary>
    public void OnClickStartBenchmarkButton()
    {
        CreateBenchmarks();
        currRunningBenchmark = benchmarks[0];
        benchmarks[0].StartBenchmark();
    }

    #endregion
}