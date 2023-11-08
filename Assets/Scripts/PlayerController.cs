using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float rotationToTangentSmoothness = 2.5f;
    [SerializeField]
    private float mouseSpeed = 3f;
    [SerializeField]
    private float maxInputOffset = 5f;
    [SerializeField]
    private Vector3 minCameraDistancePosition;
    [SerializeField]
    private Vector3 maxCameraDistancePosition;
    [SerializeField]
    private float minRocketFireDistance = 0f;
    [SerializeField]
    private float maxRocketFireDistance = 5f;

    [SerializeField]
    private Transform playerCameraTransform;
    [SerializeField]
    private List<ParticleSystem> rocketFires;

    private bool followTrack;
    private float[] normalizedIntensities;

    private bool beatDone;
    private int previousU;

    // Cache
    private GameManager gameManager;
    private float currentAudioTimePercentage;
    private Vector3 currentPoint, currentTangent;
    private float currentColorHue, currentSpeed;
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

        Color.RGBToHSV(trackSpline.GetSplineColor(currentAudioTimePercentage), out currentColorHue, out _, out _);
        currentSpeed = Mathf.InverseLerp(0.83f, 0f, currentColorHue);

        playerCameraTransform.localPosition = Vector3.Lerp(maxCameraDistancePosition, minCameraDistancePosition, currentSpeed);

        trackSpline.GetSplineIndexes(currentAudioTimePercentage, out int u, out _);
        bool doBeat = !beatDone && previousU != u && u < normalizedIntensities.Length && normalizedIntensities[u] - normalizedIntensities[u + 1] <= -0.1f;
        rocketFires.ForEach(ps =>
        {
            ParticleSystem.MainModule main = ps.main;

            if (normalizedIntensities[u] <= 0.1f)
                ps.Stop();
            else if (doBeat)
            {
                ps.Stop();
                main.startDelay = 0.005f;
                beatDone = true;
                previousU = u;
            }
            else if (!ps.isPlaying)
            {
                ps.Play();
                main.startDelay = 0f;
                beatDone = false;
            }

            main.startSpeed = Mathf.Lerp(minRocketFireDistance, maxRocketFireDistance, normalizedIntensities[u]);
        });
    }

    public void StartFollowingTrack(BSpline trackSpline)
    {
        this.trackSpline = trackSpline;

        transform.position = trackSpline.GetSplinePoint(0);
        transform.forward = Vector3.right;

        followTrack = true;
    }

    public void SetNormalizedIntensities(float[] normalizedIntensities)
    {
        this.normalizedIntensities = normalizedIntensities;
    }

}
