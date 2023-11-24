using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RaysManager : MonoBehaviour
{
    [SerializeField] private int raysCount;
    [SerializeField] private float startRadius;
    [SerializeField] private float endRadius;
    [SerializeField] private float distance;
    [SerializeField] private GameObject rayPrefab;
    [SerializeField] private Color lowIntensityColor;
    [SerializeField] private Color highIntensityColor;
    [SerializeField] private float minWidth;
    [SerializeField] private float maxWidth;
    [SerializeField] private float minSpeed;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float beatDuration;

    private List<LineRenderer> rays;
    private GameManager gameManager;

    private BSpline trackSpline;
    private float[] normalizedIntensities;

    private int previousU;
    private float beatTimer;

    void Awake()
    {
        rays = new List<LineRenderer>();
        gameManager = GameManager.GetGameManager();
        SpawnRays();
    }

    void Update()
    {
        if (!gameManager.IsGameRunning)
            return;

        UpdateRaysColorWidthAndSpeed();
    }

    /// <summary>
    /// Initializes the rays manager
    /// </summary>
    public void Initialize()
    {
        trackSpline = gameManager.GetTrackData().spline;
        normalizedIntensities = gameManager.GetTrackData().normalizedIntensities;
    }

    /// <summary>
    /// Spawns the line renderers as rays
    /// </summary>
    private void SpawnRays()
    {
        for (int i = 0; i < raysCount; i++)
        {
            LineRenderer ray = Instantiate(rayPrefab, transform).GetComponent<LineRenderer>();

            float angle = 2 * Mathf.PI * ((float)i / raysCount);
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            ray.SetPosition(0, startRadius * new Vector3(cos, sin));
            ray.SetPosition(1, endRadius * new Vector3(cos, sin) + distance * Vector3.forward);

            rays.Add(ray);
        }
    }

    /// <summary>
    /// Updates the color, width and speed of the rays based on the current song intensity
    /// </summary>
    private void UpdateRaysColorWidthAndSpeed()
    {
        int index = GetCurrentIntensityIndex();

        Color currentColor = Color.Lerp(lowIntensityColor, highIntensityColor, normalizedIntensities[index]);
        float currentWidth = Mathf.Lerp(minWidth, maxWidth, normalizedIntensities[index]);
        float currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, normalizedIntensities[index]) * Time.deltaTime;

        // Detect a beat
        bool doBeat = DetectBeat(index);
        if (doBeat)
        {
            beatTimer = beatDuration;
            previousU = index;
        }

        // If during a beat, set the color, width and speed to the average between min and max
        if (beatTimer > 0)
        {
            currentColor = Color.Lerp(lowIntensityColor, highIntensityColor, 0.5f);
            currentWidth = Mathf.Lerp(minWidth, maxWidth, 0.5f);
            currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, 0.5f) * Time.deltaTime;
            beatTimer -= Time.deltaTime;
        }

        // Update color, width and speed
        foreach (LineRenderer ray in rays)
        {
            ray.startColor = ray.endColor = currentColor;
            ray.widthMultiplier = currentWidth;
        }

        transform.rotation *= Quaternion.Euler(0, 0, currentSpeed);
    }

    /// <summary>
    /// Returns the current intensity index of the track
    /// </summary>
    /// <returns>The current intensity index of the track</returns>
    private int GetCurrentIntensityIndex()
    {
        float currentAudioTimePercentage = gameManager.GetCurrentAudioTimePercentage();
        trackSpline.GetSubSplineIndexes(currentAudioTimePercentage, out int index, out _);
        index = Mathf.Min(index, normalizedIntensities.Length - 2);
        return index;
    }

    /// <summary>
    /// Detects if there is a beat at the given index
    /// </summary>
    /// <param name="index">The index of the intensity array</param>
    /// <returns>True if a beat was detected, false otherwise</returns>
    private bool DetectBeat(int index)
    {
        return
            beatTimer <= 0 &&
            previousU != index &&
            index < normalizedIntensities.Length &&
            normalizedIntensities[index] - normalizedIntensities[index + 1] <= -0.1f;
    }
}
