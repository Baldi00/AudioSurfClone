using UnityEngine;

public class SpaceshipAnimator : MonoBehaviour
{
    [SerializeField] private Vector3 positionOffset = Vector3.up * 0.1f;
    [SerializeField] private float pitchAmplitude = 10f;
    [SerializeField] private float pitchFrequency = 1f;
    [SerializeField] private float floatingAmplitude = 0.2f;
    [SerializeField] private float floatingFrequency = 5f;
    [SerializeField] private float maxRoll = 30f;
    [SerializeField, Tooltip("Degrees/s")] private float rollRotationSpeed = 30f;

    // Cache
    private float currentPitch;
    private float currentFloating;
    private float currentRoll;

    void Update()
    {
        UpdateSpaceshipPositionAndRotation();
    }

    /// <summary>
    /// Updates the spaceship position and rotation according to animation values and user input
    /// </summary>
    private void UpdateSpaceshipPositionAndRotation()
    {
        currentPitch = pitchAmplitude * (Mathf.Sin(pitchFrequency * Time.time) * 0.5f + 0.5f);
        currentFloating = floatingAmplitude * (Mathf.Sin(floatingFrequency * Time.time) * 0.5f + 0.5f);

        currentRoll += -Input.GetAxis("Mouse X") * Time.timeScale * (rollRotationSpeed * 0.016f);

        if (currentRoll > 1)
            currentRoll -= rollRotationSpeed / 3.5f * Time.deltaTime;
        if (currentRoll < -1)
            currentRoll += rollRotationSpeed / 3.5f * Time.deltaTime;

        currentRoll = Mathf.Clamp(currentRoll, -maxRoll, maxRoll);

        transform.SetLocalPositionAndRotation(
            Vector3.up * currentFloating + positionOffset,
            Quaternion.Euler(currentPitch, 0, currentRoll));
    }
}
