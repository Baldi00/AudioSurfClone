using UnityEngine;

public class TrackVisualizer : MonoBehaviour
{
    [SerializeField] private LineRenderer trackVisualizer;
    [SerializeField] private RectTransform trackCurrentPointVisualizer;

    public void ShowTrackUi(TrackData trackData)
    {
        float trackVisualizerWidth = (trackVisualizer.transform as RectTransform).rect.width;
        float trackVisualizerHeight = (trackVisualizer.transform as RectTransform).rect.height;
        float minTrackX, minTrackY, maxTrackX, maxTrackY;
        minTrackX = minTrackY = float.MaxValue;
        maxTrackX = maxTrackY = float.MinValue;

        for (int i = 0; i < trackData.splinePoints.Length; i++)
        {
            if (trackData.splinePoints[i].x < minTrackX)
                minTrackX = trackData.splinePoints[i].x;
            if (trackData.splinePoints[i].x > maxTrackX)
                maxTrackX = trackData.splinePoints[i].x;
            if (trackData.splinePoints[i].y < minTrackY)
                minTrackY = trackData.splinePoints[i].y;
            if (trackData.splinePoints[i].y > maxTrackY)
                maxTrackY = trackData.splinePoints[i].y;
        }

        trackVisualizer.positionCount = (int)(trackData.splinePoints.Length / 50f);
        float step = 1f / trackVisualizer.positionCount;
        for (int i = 0; i < trackVisualizer.positionCount; i++)
        {
            float positionX = i * step * trackVisualizerWidth;
            float positionY = -trackVisualizerHeight *
                (1 - Mathf.InverseLerp(minTrackY, maxTrackY, trackData.splinePoints[i * 50].y));

            trackVisualizer.SetPosition(i, new Vector3(positionX, positionY));
        }
    }

    public void UpdateTrackVisualizerPosition(float currentPercentage)
    {
        float lerp = Mathf.Lerp(0, trackVisualizer.positionCount - 2, currentPercentage);
        int firstSubSplinePointIndex = (int)lerp;
        float subSplineInterpolator = lerp % 1;

        trackCurrentPointVisualizer.localPosition = 
            Vector3.Lerp(
                trackVisualizer.GetPosition(firstSubSplinePointIndex), 
                trackVisualizer.GetPosition(firstSubSplinePointIndex + 1),
                subSplineInterpolator);
    }
}
