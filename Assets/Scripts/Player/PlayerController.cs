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
        if (!gameManager.IsInTrackScene)
            return;

        currentAudioTimePercentage = gameManager.GetCurrentAudioTimePercentage();

        // Get Input
        currentPlayerInputX = -Input.GetAxis("Mouse X") * mouseSpeed * Time.timeScale;

        // Compute input offset
        currentInputOffset += currentPlayerInputX *
            trackSpline.GetBitangentPerpendicularToTangent(currentAudioTimePercentage, Vector3.forward);
        currentInputOffset = Vector3.ClampMagnitude(currentInputOffset, maxInputOffset);

        // Check if the player will hit or miss a block in the next movement
        currentPoint = trackSpline.GetPointAt(currentAudioTimePercentage);
        DoCollisionWithBlockChecks(transform.position, currentPoint + currentInputOffset);

        // Update player position and rotation
        currentTangent = trackSpline.GetTangentAt(currentAudioTimePercentage);
        transform.position = currentPoint + currentInputOffset;
        transform.forward =
            Vector3.Lerp(transform.forward, currentTangent, rotationToTangentSmoothness * Time.deltaTime);

        // Update camera position
        currentSpeed = Mathf.InverseLerp(0.83f, 0f, currentColorHue);
        playerCameraTransform.localPosition =
            Vector3.Lerp(maxCameraDistancePosition, minCameraDistancePosition, currentSpeed);

        // Update color and rocket fires
        Color.RGBToHSV(trackSpline.GetColorAt(currentAudioTimePercentage), out currentColorHue, out _, out _);
        UpdatesRocketFires();
    }

    /// <summary>
    /// Initializes the player controller
    /// </summary>
    public void Initialize()
    {
        trackSpline = gameManager.GetTrackData().spline;
        normalizedIntensities = gameManager.GetTrackData().normalizedIntensities;
        transform.position = trackSpline.GetPointAt(0);
        transform.forward = Vector3.right;
    }

    /// <summary>
    /// Checks if the player hits or misses a block from the previous position to the next one.
    /// If this is the case calls the related functions on Game Manager and on the hit/missed block
    /// </summary>
    /// <param name="startPosition">The start position of the player</param>
    /// <param name="endPosition">The end position of the player of the current movement</param>
    private void DoCollisionWithBlockChecks(Vector3 startPosition, Vector3 endPosition)
    {
        Vector3 direction = (endPosition - startPosition).normalized;
        float distance = (endPosition - startPosition).magnitude;

        if (Physics.SphereCast(startPosition + spaceShipTransform.InverseTransformDirection(pickSphereOffset),
            pickSphereRadius, direction, out RaycastHit pickHitInfo, distance) &&
            pickHitInfo.collider.gameObject.CompareTag("Block"))
        {
            Block blockManager = pickHitInfo.collider.gameObject.transform.parent.GetComponent<Block>();
            blockManager.Pick();
            gameManager.BlockPicked(blockManager.Position);
        }

        if (Physics.SphereCast(startPosition + spaceShipTransform.InverseTransformDirection(missSphereOffset),
            missSphereRadius, direction, out RaycastHit missHitInfo, distance) &&
            missHitInfo.collider.gameObject.CompareTag("Block"))
        {
            Block blockManager = missHitInfo.collider.gameObject.transform.parent.GetComponent<Block>();
            blockManager.DisableCollider();
            gameManager.BlockMissed();
        }
    }

    /// <summary>
    /// Updates rocket fires length and beats
    /// </summary>
    private void UpdatesRocketFires()
    {
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
}
