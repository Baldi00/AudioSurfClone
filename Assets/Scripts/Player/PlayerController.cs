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
        gameManager = GameManager.GetGameManager();
    }

    void Update()
    {
        if (!gameManager.IsGameRunning)
            return;

        currentAudioTimePercentage = gameManager.GetCurrentAudioTimePercentage();

        // Get Input
        currentPlayerInputX = -Input.GetAxis("Mouse X") * mouseSpeed * Time.timeScale;
        currentInputOffset += currentPlayerInputX *
            trackSpline.GetBitangentPerpendicularToTangent(currentAudioTimePercentage, Vector3.forward);
        currentInputOffset = Vector3.ClampMagnitude(currentInputOffset, maxInputOffset);

        currentPoint = trackSpline.GetPointAt(currentAudioTimePercentage);
        currentTangent = trackSpline.GetTangentAt(currentAudioTimePercentage);

        DoCollisionWithBlockChecks(transform.position, currentPoint + currentInputOffset);

        transform.position = currentPoint + currentInputOffset;
        transform.forward = Vector3.Lerp(transform.forward, currentTangent, rotationToTangentSmoothness * Time.deltaTime);

        Color.RGBToHSV(trackSpline.GetColorAt(currentAudioTimePercentage), out currentColorHue, out _, out _);
        currentSpeed = Mathf.InverseLerp(0.83f, 0f, currentColorHue);

        playerCameraTransform.localPosition = Vector3.Lerp(maxCameraDistancePosition, minCameraDistancePosition, currentSpeed);

        trackSpline.GetSubSplineIndexes(currentAudioTimePercentage, out int u, out _);
        u = Mathf.Min(u, normalizedIntensities.Length - 2);
        bool doBeat = !beatDone && previousU != u && u < normalizedIntensities.Length && normalizedIntensities[u] - normalizedIntensities[u + 1] <= -0.1f;

        foreach (ParticleSystem rocketFire in rocketFires)
        {
            ParticleSystem.MainModule main = rocketFire.main;

            if (normalizedIntensities[u] <= 0.1f)
                rocketFire.Stop();
            else if (doBeat)
            {
                rocketFire.Stop();
                main.startDelay = 0.005f;
                beatDone = true;
                previousU = u;
            }
            else if (!rocketFire.isPlaying)
            {
                rocketFire.Play();
                main.startDelay = 0f;
                beatDone = false;
            }

            main.startSpeed = Mathf.Lerp(minRocketFireDistance, maxRocketFireDistance, normalizedIntensities[u]);
        }
    }

    public void Initialize()
    {
        trackSpline = gameManager.GetTrackData().spline;
        normalizedIntensities = gameManager.GetTrackData().normalizedIntensities;
        transform.position = trackSpline.GetPointAt(0);
        transform.forward = Vector3.right;
    }

    private void DoCollisionWithBlockChecks(Vector3 previousPosition, Vector3 nextPosition)
    {
        Vector3 direction = (nextPosition - previousPosition).normalized;
        float distance = (nextPosition - previousPosition).magnitude;

        if (Physics.SphereCast(previousPosition + spaceShipTransform.InverseTransformDirection(pickSphereOffset), pickSphereRadius,
            direction, out RaycastHit pickHitInfo, distance) &&
            pickHitInfo.collider.gameObject.CompareTag("Block"))
        {
            BlockManager blockManager = pickHitInfo.collider.gameObject.transform.parent.GetComponent<BlockManager>();
            blockManager.Pick();
            gameManager.BlockPicked(blockManager.Position);
        }

        if (Physics.SphereCast(previousPosition + spaceShipTransform.InverseTransformDirection(missSphereOffset), missSphereRadius,
            direction, out RaycastHit missHitInfo, distance) &&
            missHitInfo.collider.gameObject.CompareTag("Block"))
        {
            BlockManager blockManager = missHitInfo.collider.gameObject.transform.parent.GetComponent<BlockManager>();
            blockManager.DisableCollider();
            gameManager.BlockMissed();
        }
    }
}
