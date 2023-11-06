using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    [SerializeField]
    private float rotationToTangentSmoothness = 2.5f;
    [SerializeField]
    private float mouseSpeed = 3f;
    [SerializeField]
    private float maxInputOffset = 5f;

    private bool followTrack;

    // Cache
    private GameManager gameManager;
    private float currentAudioTimePercentage;
    private Vector3 currentPoint, currentTangent;
    private BSpline trackSpline;
    private float currentPlayerInputX;
    private Vector3 currentInputOffset;

    void Awake()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    void Update()
    {
        if (!followTrack)
            return;

        currentAudioTimePercentage = gameManager.GetCurrentAudioTimePercentage();

        // Get Input
        currentPlayerInputX = -Input.GetAxis("Mouse X") * Time.deltaTime * mouseSpeed;
        currentInputOffset += currentPlayerInputX *
            trackSpline.GetBitangentPerpendicularToTangent(currentAudioTimePercentage, Vector3.forward);
        currentInputOffset = Vector3.ClampMagnitude(currentInputOffset, maxInputOffset);

        currentPoint = trackSpline.GetSplinePoint(currentAudioTimePercentage);
        currentTangent = trackSpline.GetSplineTangent(currentAudioTimePercentage);
        transform.position = currentPoint + currentInputOffset;
        transform.forward = Vector3.Lerp(transform.forward, currentTangent, rotationToTangentSmoothness * Time.deltaTime);
    }

    public void StartFollowingTrack(BSpline trackSpline, AudioSource audioSource)
    {
        this.trackSpline = trackSpline;

        transform.position = trackSpline.GetSplinePoint(0);
        transform.forward = Vector3.right;

        followTrack = true;
    }
}
