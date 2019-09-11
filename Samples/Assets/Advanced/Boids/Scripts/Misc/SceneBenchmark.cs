using UnityEngine.SceneManagement;

public abstract class SceneBenchmark
{
    #region Private Attributes

    protected const float Frequency = 4.0f;
    protected const float SampleInterval = 1.0f / Frequency;

    private bool isRunning = false;
    private float timePassedSinceBenchmarkStart = 0.0f;
    private float ignoredFirstSeconds;
    private float duration;

    private int frames = 0;
    private float averageFps = 0.0f;
    private float accumFps = 0.0f;

    #endregion

    #region Properties

    public abstract string AssociatedSceneName { get; }
    public abstract SceneBenchmarkType SceneBenchType { get; }
    public abstract SceneBenchmarkType NextSceneBenchType { get; }
    public float AverageFps { get { return averageFps; } }

    public bool IsDone { get { return timePassedSinceBenchmarkStart > duration; } }

    #endregion

    #region Methods

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="ignoredFirstSeconds"></param>
    public SceneBenchmark(float duration, float ignoredFirstSeconds)
    {
        this.duration = duration;
        this.ignoredFirstSeconds = ignoredFirstSeconds;
    }

    /// <summary>
    /// Start the benchmarking
    /// </summary>
    public virtual void StartBenchmark(bool doLoadScene)
    {
        if (doLoadScene)
            SceneManager.LoadScene(AssociatedSceneName, LoadSceneMode.Single);

        ResetBenchmarkData();
        isRunning = true;
    }

    /// <summary>
    /// Do the benchmark updating
    /// </summary>
    /// <param name="dt"></param>
    public virtual void UpdateBenchmark(float dt)
    {
        if (!isRunning)
            return;

        if (timePassedSinceBenchmarkStart > duration)
        {
            ForceEndBenchmark(false);
            return;
        }

        timePassedSinceBenchmarkStart += dt;

        if (timePassedSinceBenchmarkStart < ignoredFirstSeconds)
            return;

        frames++;
        float frameFps = 1.0f / dt;
        accumFps += frameFps;
        averageFps = accumFps / frames;

        float avgRounded = (float) System.Math.Round(averageFps, 2);
        BenchmarkSystem.Instance.AvgFPS = avgRounded + " avg FPS";
    }

    /// <summary>
    /// Force the benchmark to end prematurely
    /// </summary>
    /// <param name="doResetData"></param>
    public virtual void ForceEndBenchmark(bool doResetData)
    {
        isRunning = false;

        if (doResetData)
            ResetBenchmarkData();
    }

    /// <summary>
    /// Reset the benchmark data
    /// </summary>
    public virtual void ResetBenchmarkData()
    {
        timePassedSinceBenchmarkStart = 0.0f;
        frames = 0;
        averageFps = 0.0f;
        accumFps = 0.0f;
    }

    #endregion
}

public abstract class OOPSceneBenchmark : SceneBenchmark
{
    protected const string sceneName = "BoidExampleOOP";

    public OOPSceneBenchmark(float duration, float ignoredFirstSeconds) : base(duration, ignoredFirstSeconds)
    {

    }

    public override string AssociatedSceneName { get { return sceneName; } }
}

public class OOPSTSceneBenchmark : OOPSceneBenchmark
{
    public OOPSTSceneBenchmark(float duration, float ignoredFirstSeconds) : base(duration, ignoredFirstSeconds)
    {

    }

    public override void StartBenchmark(bool doLoadScene)
    {
        base.StartBenchmark(doLoadScene);
        BenchmarkSystem.Instance.SceneTitle = sceneName + " Single thread";
    }

    public override SceneBenchmarkType SceneBenchType { get { return SceneBenchmarkType.OOPST; } }
    public override SceneBenchmarkType NextSceneBenchType { get { return SceneBenchmarkType.OOPMT; } }
}

public class OOPMTSceneBenchmark : OOPSceneBenchmark
{
    public OOPMTSceneBenchmark(float duration, float ignoredFirstSeconds) : base(duration, ignoredFirstSeconds)
    {

    }

    public override void StartBenchmark(bool doLoadScene)
    {
        base.StartBenchmark(doLoadScene);
        BenchmarkSystem.Instance.SceneTitle = sceneName + " Multithread";
    }

    public override SceneBenchmarkType SceneBenchType { get { return SceneBenchmarkType.OOPMT; } }
    //public override SceneBenchmarkType NextSceneBenchType { get { return SceneBenchmarkType.ECS; } }
    public override SceneBenchmarkType NextSceneBenchType { get { return SceneBenchmarkType.Compute; } }
}

public class ECSSceneBenchmark : SceneBenchmark
{
    private const string sceneName = "BENCHMARK_ECS";

    public ECSSceneBenchmark(float duration, float ignoredFirstSeconds) : base(duration, ignoredFirstSeconds)
    {

    }

    public override void StartBenchmark(bool doLoadScene)
    {
        base.StartBenchmark(doLoadScene);
        BenchmarkSystem.Instance.SceneTitle = sceneName;
    }

    public override string AssociatedSceneName { get { return sceneName; } }

    public override SceneBenchmarkType SceneBenchType { get { return SceneBenchmarkType.ECS; } }
    //public override SceneBenchmarkType NextSceneBenchType { get { return SceneBenchmarkType.Compute; } }
    public override SceneBenchmarkType NextSceneBenchType { get { return SceneBenchmarkType.Invalid; } }
}

public class ComputeSceneBenchmark : SceneBenchmark
{
    private const string sceneName = "BoidExampleCompute";

    public ComputeSceneBenchmark(float duration, float ignoredFirstSeconds) : base(duration, ignoredFirstSeconds)
    {

    }

    public override void StartBenchmark(bool doLoadScene)
    {
        base.StartBenchmark(doLoadScene);
        BenchmarkSystem.Instance.SceneTitle = sceneName;
    }

    public override string AssociatedSceneName { get { return sceneName; } }

    public override SceneBenchmarkType SceneBenchType { get { return SceneBenchmarkType.Compute; } }
    public override SceneBenchmarkType NextSceneBenchType { get { return SceneBenchmarkType.Invalid; } }
}