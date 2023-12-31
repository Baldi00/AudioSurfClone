using System.Collections.Generic;
using UnityEngine;

public class HexagonsManager : MonoBehaviour
{
    [SerializeField] private GameObject hexagonSubwooferPrefab;
    [SerializeField] private float hexagonBeatDuration;
    [SerializeField] private float horizontalOffset = 35f;
    [SerializeField] private float verticalOffset = 5f;
    [SerializeField] private float rotationAngle = 60f;

    private GameObject hexagonsContainer;
    private TrackData trackData;

    private List<int> lowBeatIndexes;
    private List<int> highBeatIndexes;

    private List<Transform> hexagonTransforms;
    private float hexagonTimer;
    private Vector3 hexagonStartScale;

    void Awake()
    {
        hexagonTransforms = new List<Transform>();
        hexagonStartScale = hexagonSubwooferPrefab.transform.localScale;
    }

    /// <summary>
    /// Spawns the hexagons on the track in the beat positions
    /// </summary>
    /// <param name="spectrum">The spectrum of the audio clip</param>
    /// <param name="audioClip">The audioClip containing the audio to reproduce</param>
    /// <param name="trackData">The data generated for the given audioClip</param>
    public void SpawnHexagonsOnTrack(float[][] spectrum, AudioClip audioClip, TrackData trackData)
    {
        this.trackData = trackData;

        lowBeatIndexes =
            AudioUtils.GetBeatIndexes(spectrum, audioClip.frequency, audioClip.channels, 20, 0.1f, 0);
        highBeatIndexes =
            AudioUtils.GetBeatIndexes(spectrum, audioClip.frequency, audioClip.channels, 7500, 0.01f, 0.15f);

        hexagonsContainer = new GameObject("Hexagon container");
        for (int i = 0; i < trackData.splinePoints.Length;
            i += (int)Mathf.Lerp(128, 32, trackData.normalizedIntensities[i] * trackData.normalizedIntensities[i]))
        {
            Transform hex1 = Instantiate(hexagonSubwooferPrefab,
                trackData.splinePoints[i] + horizontalOffset * Vector3.forward + verticalOffset * Vector3.up,
                Quaternion.Euler(0, rotationAngle, 0),
                hexagonsContainer.transform).transform;

            Transform hex2 = Instantiate(hexagonSubwooferPrefab,
                trackData.splinePoints[i] - horizontalOffset * Vector3.forward + verticalOffset * Vector3.up,
                Quaternion.Euler(0, 180 - rotationAngle, 0),
                hexagonsContainer.transform).transform;

            hexagonTransforms.Add(hex1);
            hexagonTransforms.Add(hex2);
        }
    }

    /// <summary>
    /// Updates the hexagons scale based on the current audio percentage
    /// </summary>
    /// <param name="currentPercentage">The current audio percentage</param>
    public void UpdateHexagonsScale(float currentPercentage)
    {
        trackData.spline.GetSubSplineIndexes(currentPercentage, out int currentIndex, out _);
        if (lowBeatIndexes.Contains(currentIndex) && hexagonTimer < hexagonBeatDuration / 2)
        {
            hexagonTimer = hexagonBeatDuration;
            foreach (Transform hexagonTransform in hexagonTransforms)
                hexagonTransform.localScale = hexagonStartScale * 1.5f;
        }
        else if (highBeatIndexes.Contains(currentIndex) && hexagonTimer < hexagonBeatDuration / 6)
        {
            hexagonTimer = hexagonBeatDuration / 6;
            foreach (Transform hexagonTransform in hexagonTransforms)
                hexagonTransform.localScale = hexagonStartScale * (1 + 0.5f / 6);
        }
        else if (hexagonTimer > 0)
        {
            hexagonTimer -= Time.deltaTime;

            foreach (Transform hexagonTransform in hexagonTransforms)
                hexagonTransform.localScale = hexagonStartScale *
                    Mathf.Lerp(1f, 1.5f, hexagonTimer / hexagonBeatDuration);
        }
    }

    /// <summary>
    /// Removes all the hexagons from the scene
    /// </summary>
    public void RemoveAllHexagons()
    {
        Destroy(hexagonsContainer);
        hexagonTransforms.Clear();
    }
}
