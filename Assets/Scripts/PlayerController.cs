using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float rotationToTangentSmoothness = 2.5f;
    [SerializeField] private float mouseSpeed = 3f;
    [SerializeField] private float maxInputOffset = 5f;
    [SerializeField] private Vector3 minCameraDistancePosition;
    [SerializeField] private Vector3 maxCameraDistancePosition;
    [SerializeField] private float minRocketFireDistance = 0f;
    [SerializeField] private float maxRocketFireDistance = 5f;

    [SerializeField] private Transform playerCameraTransform;
    [SerializeField] private List<ParticleSystem> rocketFires;
    [SerializeField] private Transform spaceShipTransform;

    [Header("Blocks collisions")]
    [SerializeField] private float pickSphereRadius;
    [SerializeField] private Vector3 pickSphereOffset;
    [SerializeField] private float missSphereRadius;
    [SerializeField] private Vector3 missSphereOffset;

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

        DoCollisionWithBlockChecks(transform.position, currentPoint + currentInputOffset);

        transform.position = currentPoint + currentInputOffset;
        transform.forward = Vector3.Lerp(transform.forward, currentTangent, rotationToTangentSmoothness * Time.deltaTime);

        Color.RGBToHSV(trackSpline.GetSplineColor(currentAudioTimePercentage), out currentColorHue, out _, out _);
        currentSpeed = Mathf.InverseLerp(0.83f, 0f, currentColorHue);

        playerCameraTransform.localPosition = Vector3.Lerp(maxCameraDistancePosition, minCameraDistancePosition, currentSpeed);

        trackSpline.GetSplineIndexes(currentAudioTimePercentage, out int u, out _);
        bool doBeat = !beatDone && previousU != u && u < normalizedIntensities.Length && normalizedIntensities[u] - normalizedIntensities[u + 1] <= -0.1f;

        for (int i = 0; i < rocketFires.Count; i++)
        {
            ParticleSystem.MainModule main = rocketFires[i].main;

            if (normalizedIntensities[u] <= 0.1f)
                rocketFires[i].Stop();
            else if (doBeat)
            {
                rocketFires[i].Stop();
                main.startDelay = 0.005f;
                beatDone = true;
                previousU = u;
            }
            else if (!rocketFires[i].isPlaying)
            {
                rocketFires[i].Play();
                main.startDelay = 0f;
                beatDone = false;
            }

            main.startSpeed = Mathf.Lerp(minRocketFireDistance, maxRocketFireDistance, normalizedIntensities[u]);
        }
    }

    public void StartFollowingTrack(BSpline trackSpline)
    {
        this.trackSpline = trackSpline;

        transform.position = trackSpline.GetSplinePoint(0);
        transform.forward = Vector3.right;

        followTrack = true;
    }

    public void StopFollowinTrack()
    {
        followTrack = false;
    }

    public void SetNormalizedIntensities(float[] normalizedIntensities)
    {
        this.normalizedIntensities = normalizedIntensities;
    }

    private void DoCollisionWithBlockChecks(Vector3 previousPosition, Vector3 nextPosition)
    {
        Vector3 direction = (nextPosition - previousPosition).normalized;
        float distance = (nextPosition - previousPosition).magnitude;

        Debug.DrawRay(previousPosition + spaceShipTransform.InverseTransformDirection(missSphereOffset), nextPosition - previousPosition, Color.magenta, 3);

        if (Physics.SphereCast(previousPosition + spaceShipTransform.InverseTransformDirection(pickSphereOffset), pickSphereRadius,
            direction, out RaycastHit pickHitInfo, distance) &&
            pickHitInfo.collider.gameObject.CompareTag("Block"))
        {
            gameManager.BlockPicked();
            pickHitInfo.collider.gameObject.GetComponent<BlockTriggerHandler>().Pick();
        }

        if (Physics.SphereCast(previousPosition + spaceShipTransform.InverseTransformDirection(missSphereOffset), missSphereRadius,
            direction, out RaycastHit missHitInfo, distance) &&
            missHitInfo.collider.gameObject.CompareTag("Block"))
            gameManager.BlockMissed();
    }
}
