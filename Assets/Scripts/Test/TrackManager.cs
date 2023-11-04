using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(MeshFilter))]
public class TrackManager : MonoBehaviour
{
    [SerializeField, Range(1, 100000)]
    private int trackMeshResolution = 256;
    [SerializeField, Range(0.01f, 10f)]
    private float trackMeshThickness = 5;
    [SerializeField]
    private Vector3 trackMeshBitangent = Vector3.forward;

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
        float[] slopePoints = new float[intensities.Length];
        float previousPointX = 0;
        float previousPointY = 0;

        for (int i = 0; i < intensities.Length; i++)
        {
            float slope = Mathf.Lerp(0.5f, -0.5f, Mathf.InverseLerp(0, 0.5f, intensities[i]));
            float speedInv = Mathf.Lerp(0.3f, 3f, Mathf.InverseLerp(0, 0.5f, intensities[i]));
            float currentPointX = previousPointX + speedInv;
            float currentPointY = previousPointY + slope * (intensities[i] + 0.25f);
            intensityPoints[i] = new Vector3(currentPointX, currentPointY);
            if (i == 0)
                slopePoints[i] = slope;
            else
                slopePoints[i] = Mathf.Lerp(slopePoints[i - 1], slope, 0.05f);

            previousPointX = currentPointX;
            previousPointY = currentPointY;
        }

        trackSpline.SetPoints(intensityPoints, slopePoints);
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
