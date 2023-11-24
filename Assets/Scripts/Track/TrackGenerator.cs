using System;
using System.Linq;
using UnityEngine;

public class TrackGenerator : MonoBehaviour
{
    [SerializeField, Range(1, 100000)] private int trackMeshResolution = 256;
    [SerializeField, Range(0.01f, 10f)] private float trackMeshThickness = 5;
    [SerializeField] private Vector3 trackMeshBitangent = Vector3.forward;

    [SerializeField, Range(0.001f, 0.999f)] private float slopeSmoothness = 0.925f;
    [SerializeField] private float minSlopeIntensity = 0.6f;
    [SerializeField] private float maxSlopeIntensity = 1.2f;

    [SerializeField] private float minSpeed = 0.3f;
    [SerializeField] private float maxSpeed = 3f;

    /// <summary>
    /// Generates and returns the track data for the given audioClip
    /// </summary>
    /// <param name="audioClip">The audioClip to analyze for generating the track</param>
    /// <param name="windowSize">The audio window size used for the audio analysis</param>
    /// <returns>The generated track data</returns>
    public TrackData GenerateTrack(AudioClip audioClip, int windowSize)
    {
        float[] rawIntensities = AudioUtils.GetAudioIntensities(audioClip, windowSize);
        float[] normalizedIntensities = RemapArray(rawIntensities, 0, 1);

        float slopeIntensity = GetSlopeIntensity(rawIntensities);
        float[] slopes = GetSlopes(normalizedIntensities, slopeIntensity);
        Color[] colors = GetColors(slopes, slopeIntensity);
        Vector3[] splinePoints = GetSplinePoints(normalizedIntensities, slopes);

        var spline = new BSpline();
        spline.SetPoints(splinePoints);
        spline.SetColors(colors);

        Mesh mesh = spline.GetSplineMesh(trackMeshResolution, trackMeshThickness, trackMeshBitangent);

        return new TrackData
        {
            spline = spline,
            splinePoints = splinePoints,
            rawIntensities = rawIntensities,
            normalizedIntensities = normalizedIntensities,
            slopes = slopes,
            colors = colors,
            mesh = mesh
        };
    }

    /// <summary>
    /// Returns the remapped array (i.e. converts each item from the [inMin-inMax] range into the [outMin-outMax] range)
    /// </summary>
    /// <param name="original">The array in the [inMin-inMax] range to remap</param>
    /// <returns>The remapped array (in the [outMin-outMax] range)</returns>
    private float[] RemapArray(float[] original, float outMin, float outMax)
    {
        float[] remapped = new float[original.Length];

        float inMin = original.Min<float>();
        float inMax = original.Max<float>();

        for (int i = 0; i < original.Length; i++)
            remapped[i] = Mathf.Lerp(outMin, outMax, Mathf.InverseLerp(inMin, inMax, original[i]));

        return remapped;
    }

    /// <summary>
    /// Returns the adjusted max slope for this audio, greater if audio is more "agitated"
    /// (i.e. how many intensities are above 0.5 compared to the total number of intensities)
    /// </summary>
    /// <param name="rawIntensities">The raw intensities of the track</param>
    /// <returns>The adjusted max slope for this audio</returns>
    private float GetSlopeIntensity(float[] rawIntensities)
    {
        int highIntensitiesCount = rawIntensities.Count<float>(intensity => intensity > 0.5f);
        float audioAgitation = (float)highIntensitiesCount / rawIntensities.Length;
        return Mathf.Lerp(minSlopeIntensity, maxSlopeIntensity, audioAgitation);
    }

    /// <summary>
    /// Returns the slopes of the track based on the track intensities
    /// (High intensity: min slope, low intensity: max slope)
    /// </summary>
    /// <param name="normalizedIntensities">The normalized intensities of the track</param>
    /// <param name="slopeIntensity">The max slope of the track</param>
    /// <returns>The slopes of the track</returns>
    private float[] GetSlopes(float[] normalizedIntensities, float slopeIntensity)
    {
        float[] slopes = new float[normalizedIntensities.Length];

        // Smooths intensities
        slopes[0] = 0;
        for (int i = 1; i < slopes.Length; i++)
            slopes[i] = Mathf.Lerp(slopes[i - 1], normalizedIntensities[i], 1 - slopeSmoothness);

        slopes = RemapArray(slopes, slopeIntensity, -slopeIntensity);

        return slopes;
    }

    /// <summary>
    /// Returns the colors of the track based on the track slopes
    /// (max slope: purple->blue->green->yellow->red :min slope)
    /// </summary>
    /// <param name="slopes">The slopes of the track</param>
    /// <param name="slopeIntensity">The max slope of the track</param>
    /// <returns>The colors of the track</returns>
    private static Color[] GetColors(float[] slopes, float slopeIntensity)
    {
        Color[] colors = new Color[slopes.Length];

        for (int i = 0; i < slopes.Length; i++)
        {
            float hue = Mathf.Clamp01(
                Mathf.Lerp(-0.2f, 0.83f, Mathf.InverseLerp(-slopeIntensity, slopeIntensity, slopes[i])));

            colors[i] = Color.HSVToRGB(hue, 1f, 0.8f);
        }

        return colors;
    }

    /// <summary>
    /// Returns the spline points of the track based on the intensities and slopes
    /// </summary>
    /// <param name="normalizedIntensities">The normalized intensities of the track</param>
    /// <param name="slopes">The slopes of the track</param>
    /// <returns>The spline points of the track</returns>
    private Vector3[] GetSplinePoints(float[] normalizedIntensities, float[] slopes)
    {
        Vector3[] splinePoints = new Vector3[normalizedIntensities.Length];

        float previousPointX = 0;
        float previousPointY = 0;
        for (int i = 0; i < normalizedIntensities.Length; i++)
        {
            float speedInv = Mathf.Lerp(minSpeed, maxSpeed, normalizedIntensities[i]);
            float currentPointX = previousPointX + speedInv;
            float currentPointY = previousPointY + slopes[i] * speedInv;
            splinePoints[i] = new Vector3(currentPointX, currentPointY);

            previousPointX = currentPointX;
            previousPointY = currentPointY;
        }

        return splinePoints;
    }
}
