using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TrackManager : MonoBehaviour
{
    [SerializeField, Range(1, 100000)]
    private int trackMeshResolution = 256;
    [SerializeField, Range(0.01f, 10f)]
    private float trackMeshThickness = 5;
    [SerializeField]
    private Vector3 trackMeshBitangent = Vector3.forward;

    [SerializeField, Range(0.001f, 0.999f)]
    private float slopeSmoothness = 0.925f;
    [SerializeField]
    private float slopeIntensity = 0.5f;

    [SerializeField]
    private float minSpeed = 0.3f;
    [SerializeField]
    private float maxSpeed = 3f;

    private MeshFilter meshFilter;
    private BSpline trackSpline;

    void Awake()
    {
        trackSpline = new BSpline();
        meshFilter = GetComponent<MeshFilter>();
    }

    public BSpline GetTrackSpline()
    {
        return trackSpline;
    }

    public void GenerateTrack(AudioClip audioClip, int windowSize)
    {
        float[] intensities = AudioAnalyzer.GetAudioIntensities(audioClip, windowSize);

        Vector3[] intensityPoints = new Vector3[intensities.Length];
        Color[] colors = new Color[intensities.Length];
        float previousPointX = 0;
        float previousPointY = 0;

        float maxIntensity = 0;

        for (int i = 0; i < intensities.Length; i++)
            if (maxIntensity < intensities[i])
                maxIntensity = intensities[i];

        float[] slopes = new float[intensities.Length];
        slopes[0] = 0;
        float maxSlope = 0;
        for (int i = 1; i < slopes.Length; i++)
        {
            intensities[i] = Mathf.InverseLerp(0, maxIntensity, intensities[i]);
            slopes[i] = Mathf.Lerp(slopes[i - 1], intensities[i], 1 - slopeSmoothness);
            if (maxSlope < slopes[i])
                maxSlope = slopes[i];
        }

        for (int i = 0; i < slopes.Length; i++)
        {
            colors[i] = Color.HSVToRGB(Mathf.Clamp01(Mathf.Lerp(-0.2f, 0.83f, Mathf.InverseLerp(maxSlope, 0, slopes[i]))), 1f, 0.8f);
            slopes[i] = Mathf.Lerp(slopeIntensity, -slopeIntensity, Mathf.InverseLerp(0, maxSlope, slopes[i]));
        }

        for (int i = 0; i < intensities.Length; i++)
        {
            float speedInv = Mathf.Lerp(minSpeed, maxSpeed, intensities[i]);
            float currentPointX = previousPointX + speedInv;
            float currentPointY = previousPointY + slopes[i] * speedInv;
            intensityPoints[i] = new Vector3(currentPointX, currentPointY);

            previousPointX = currentPointX;
            previousPointY = currentPointY;
        }

        trackSpline.SetPoints(intensityPoints);
        trackSpline.SetColors(colors);
        meshFilter.mesh = trackSpline.GetSplineMesh(trackMeshResolution, trackMeshThickness, trackMeshBitangent);
    }

    private void VisualizeAudioSpectrum(AudioClip audioClip, int frequency, int windowSize)
    {
        double[][] spectrum = AudioAnalyzer.GetAudioSpectrum(audioClip, windowSize);
        int frequencyIndex = (int)(128f / (20000 - 20) * frequency);
        Vector3[] frequencyPoints = new Vector3[spectrum.Length];
        for (int i = 0; i < spectrum.Length; i++)
            frequencyPoints[i] = new Vector3(300f * i / spectrum.Length, (float)spectrum[i][frequencyIndex]);
        // Spline visualization
    }
}
