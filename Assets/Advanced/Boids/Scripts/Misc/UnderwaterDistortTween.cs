using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Some practising with coroutines and compressed states code The effect might be nice to to look
/// at too
/// </summary>
public class UnderwaterDistortTween : MonoBehaviour
{
    private enum TweenState
    {
        Up,
        Down
    }

    [Range(-50.0f, 0.0f)]
    public float minIntensity;

    [Range(0.0f, 50.0f)]
    public float maxIntensity;

    [Range(-1.0f, 0.0f)]
    public float minCenterX;

    [Range(0.0f, 1.0f)]
    public float maxCenterX;

    [Range(-1.0f, 0.0f)]
    public float minCenterY;

    [Range(0.0f, 1.0f)]
    public float maxCenterY;

    [Range(0.0f, 10.0f)]
    public float tweenTime;

    public PostProcessVolume vol;

    private LensDistortion lens;
    private TweenState state = TweenState.Up;

    private void Start()
    {
        bool ok = vol.profile.TryGetSettings(out lens);

        if (!ok)
            Debug.Log("Missing LensDistortion in post processing profile settings");

        SetLens(minIntensity, minCenterX, minCenterY);
        StartCoroutine(TweenForever());
    }

    /// <summary>
    /// Coroutine that starts the actual process of tweening and keeps it running
    /// </summary>
    /// <returns></returns>
    private IEnumerator TweenForever()
    {
        while (true)
        {
            yield return StartCoroutine(Tween());
            state = state == TweenState.Up ? TweenState.Down : TweenState.Up;
        }
    }

    /// <summary>
    /// Do the actual tweening
    /// </summary>
    /// <returns></returns>
    private IEnumerator Tween()
    {
        float dt = Time.deltaTime;

        for (float i = 0.0f; i < tweenTime + dt; i += dt)
        {
            i = Mathf.Clamp(i, 0.0f, tweenTime);
            float perc = i / tweenTime;

            float newIntensity = 0.0f;
            float newCenterX = 0.0f;
            float newCenterY = 0.0f;

            // it could be a lot more randomized for each different cycle, and independant
            // interpolations too but anyways, the effect is not even going to be noticed for the
            // current purpose of the project so I don't care
            switch (state)
            {
                case TweenState.Up:
                    newIntensity = EaseInOutSine(minIntensity, maxIntensity, perc);
                    newCenterX = EaseInOutSine(minCenterX, maxCenterX, perc);
                    newCenterY = EaseInOutSine(minCenterY, maxCenterY, perc);
                    break;

                case TweenState.Down:
                    newIntensity = EaseInOutSine(maxIntensity, minIntensity, perc);
                    newCenterX = EaseInOutSine(maxCenterX, minCenterX, perc);
                    newCenterY = EaseInOutSine(maxCenterY, minCenterY, perc);
                    break;
            }

            SetLens(newIntensity, newCenterX, newCenterY);

            if (perc < 1.0f)
                yield return null;
            else
                yield break;
        }
    }

    /// <summary>
    /// Set the lens params
    /// </summary>
    /// <param name="intensity"></param>
    /// <param name="xCenter"></param>
    /// <param name="yCenter"></param>
    private void SetLens(float intensity, float xCenter, float yCenter)
    {
        lens.intensity.value = intensity;
        lens.centerX.value = xCenter;
        lens.centerY.value = yCenter;
    }

    /// <summary>
    /// https://gist.github.com/cjddmut/d789b9eb78216998e95c
    /// </summary>
    private static float EaseInOutSine(float start, float end, float value)
    {
        end -= start;

        return -end * 0.5f * (Mathf.Cos(Mathf.PI * value) - 1.0f) + start;
    }
}