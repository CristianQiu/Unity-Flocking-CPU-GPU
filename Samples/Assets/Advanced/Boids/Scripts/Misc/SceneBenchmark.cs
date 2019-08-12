using System.Diagnostics;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using RunMode = BoidsOOP.BoidManager.RunMode;

public abstract class SceneBenchmark
{
    #region Private Attributes

    protected const float Frequency = 4.0f;
    protected const float SampleInterval = 1.0f / Frequency;

    protected bool isRunning = false;
    protected float timePassedSinceBenchmarkStart = 0.0f;
    protected float ignoredFirstSeconds;
    protected float duration;

    protected int frame = 0;
    protected float averageFps = 0.0f;
    protected float accumFps = 0.0f;

    protected Stopwatch sw = new Stopwatch();

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
    public virtual void StartBenchmark()
    {
        SceneManager.LoadScene(AssociatedSceneName, LoadSceneMode.Single);
        ResetBenchmarkData();
        isRunning = true;
        sw.Start();
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

        float frameFps = 1.0f / dt;
        accumFps += frameFps;
        frame++;
        averageFps = accumFps / frame;
        Debug.Log("stopwatch0 " + frame / ((sw.ElapsedMilliseconds / 1000.0f) / frame));
        // Debug.Log("avg " + averageFps);
        // Debug.Log("time.time " + Time.frameCount / Time.timeSinceLevelLoad);
    }

    /// <summary>
    /// Force the benchmark to end prematurely
    /// </summary>
    /// <param name="doResetData"></param>
    public virtual void ForceEndBenchmark(bool doResetData)
    {
        sw.Stop();
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
        frame = 0;
        averageFps = 0.0f;
        accumFps = 0.0f;
        sw.Reset();
    }

    #endregion
}

public abstract class OOPSceneBenchmark : SceneBenchmark
{
    private const string sceneName = "BoidExampleOOP";

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

    public override SceneBenchmarkType SceneBenchType { get { return SceneBenchmarkType.OOPST; } }
    public override SceneBenchmarkType NextSceneBenchType { get { return SceneBenchmarkType.OOPMT; } }
}

public class OOPMTSceneBenchmark : OOPSceneBenchmark
{
    public OOPMTSceneBenchmark(float duration, float ignoredFirstSeconds) : base(duration, ignoredFirstSeconds)
    {

    }

    public override SceneBenchmarkType SceneBenchType { get { return SceneBenchmarkType.OOPMT; } }
    public override SceneBenchmarkType NextSceneBenchType { get { return SceneBenchmarkType.ECS; } }
}

public class ECSSceneBenchmark : SceneBenchmark
{
    private const string sceneName = "BoidExampleECS";

    public ECSSceneBenchmark(float duration, float ignoredFirstSeconds) : base(duration, ignoredFirstSeconds)
    {

    }

    public override string AssociatedSceneName { get { return sceneName; } }

    public override SceneBenchmarkType SceneBenchType { get { return SceneBenchmarkType.ECS; } }
    public override SceneBenchmarkType NextSceneBenchType { get { return SceneBenchmarkType.Compute; } }
}

public class ComputeSceneBenchmark : SceneBenchmark
{
    private const string sceneName = "BoidExampleCompute";

    public ComputeSceneBenchmark(float duration, float ignoredFirstSeconds) : base(duration, ignoredFirstSeconds)
    {

    }

    public override string AssociatedSceneName { get { return sceneName; } }

    public override SceneBenchmarkType SceneBenchType { get { return SceneBenchmarkType.Compute; } }
    public override SceneBenchmarkType NextSceneBenchType { get { return SceneBenchmarkType.Invalid; } }
}