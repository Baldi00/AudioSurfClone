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

    private void SpawnRays()
    {
        for (int i = 0; i < raysCount; i++)
        {
            LineRenderer ray = Instantiate(rayPrefab, transform).GetComponent<LineRenderer>();

            float angle = 2 * Mathf.PI * ((float)i / raysCount);

            ray.SetPosition(0, startRadius * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0));
            ray.SetPosition(1, endRadius * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) + distance * Vector3.forward);

            rays.Add(ray);
        }
    }

    void Update()
    {
        if (!gameManager.IsGameRunning)
            return;

        float currentAudioTimePercentage = gameManager.GetCurrentAudioTimePercentage();
        trackSpline.GetSubSplineIndexes(currentAudioTimePercentage, out int u, out _);

        Color currentColor = Color.Lerp(lowIntensityColor, highIntensityColor, normalizedIntensities[u]);
        float currentWidth = Mathf.Lerp(minWidth, maxWidth, normalizedIntensities[u]);
        float currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, normalizedIntensities[u]) * Time.deltaTime;

        bool doBeat = beatTimer <= 0 && previousU != u && u < normalizedIntensities.Length && normalizedIntensities[u] - normalizedIntensities[u + 1] <= -0.1f;

        if (doBeat)
        {
            beatTimer = beatDuration;
            previousU = u;
        }

        if (beatTimer > 0)
        {
            currentColor = Color.Lerp(lowIntensityColor, highIntensityColor, 0.5f);
            currentWidth = Mathf.Lerp(minWidth, maxWidth, 0.5f);
            currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, 0.5f) * Time.deltaTime;
            beatTimer -= Time.deltaTime;
        }

        foreach (LineRenderer ray in rays)
        {
            ray.startColor = ray.endColor = currentColor;
            ray.widthMultiplier = currentWidth;
        }

        transform.rotation *= Quaternion.Euler(0, 0, currentSpeed);
    }

    public void Initialize(BSpline trackSpline, float[] normalizedIntensities)
    {
        this.trackSpline = trackSpline;
        this.normalizedIntensities = normalizedIntensities;
    }
}
