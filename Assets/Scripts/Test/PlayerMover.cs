using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    [SerializeField]
    private Vector3 positionOffset;
    [SerializeField]
    private float rotationToTangentSmoothness = 2.5f;

    private bool followTrack;

    // Cache
    private AudioSource audioSource;
    private Vector3 currentPoint, currentTangent;
    private BSpline trackSpline;

    void Update()
    {
        if (!followTrack)
            return;

        currentPoint = trackSpline.GetSplinePoint(GetCurrentAudioTimePercentage());
        currentTangent = trackSpline.GetSplineTangent(GetCurrentAudioTimePercentage());
        transform.position = currentPoint + positionOffset;
        transform.forward = Vector3.Lerp(transform.forward, currentTangent, rotationToTangentSmoothness * Time.deltaTime);
    }

    public void StartFollowingTrack(BSpline trackSpline, AudioSource audioSource)
    {
        this.trackSpline = trackSpline;
        this.audioSource = audioSource;
        
        transform.position = trackSpline.GetSplinePoint(0) + positionOffset;
        transform.forward = Vector3.right;
        
        followTrack = true;
    }

    private float GetCurrentAudioTimePercentage()
    {
        return audioSource.time / audioSource.clip.length;
    }
}
