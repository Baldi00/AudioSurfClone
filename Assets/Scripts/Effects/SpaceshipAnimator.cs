using UnityEngine;

public class SpaceshipAnimator : MonoBehaviour
{
    [SerializeField] private Vector3 positionOffset = Vector3.up * 0.1f;
    [SerializeField] private float pitchAmplitude = 10f;
    [SerializeField] private float pitchFrequency = 1f;
    [SerializeField] private float floatingAmplitude = 0.2f;
    [SerializeField] private float floatingFrequency = 5f;
    [SerializeField] private float rollMultiplier = 30000f;
    [SerializeField] private float maxRoll = 30f;
    [SerializeField, Range(0, 1)] private float rollSmoothing = 0.975f;

    // Cache
    private float currentPitch;
    private float currentFloating;
    private float currentRoll;
    private float mouseX;

    void Update()
    {
        UpdateSpaceshipPositionAndRotation();
    }

    /// <summary>
    /// Updates the spaceship position and rotation according to animation values and user input
    /// </summary>
    private void UpdateSpaceshipPositionAndRotation()
    {
        mouseX = -Input.GetAxis("Mouse X");

        currentPitch = pitchAmplitude * (Mathf.Sin(pitchFrequency * Time.time) * 0.5f + 0.5f);
        currentFloating = floatingAmplitude * (Mathf.Sin(floatingFrequency * Time.time) * 0.5f + 0.5f);
        currentRoll = Mathf.Lerp(mouseX * Time.deltaTime * rollMultiplier, currentRoll, rollSmoothing);
        currentRoll = Mathf.Clamp(currentRoll, -maxRoll, maxRoll);

        transform.SetLocalPositionAndRotation(
            Vector3.up * currentFloating + positionOffset,
            Quaternion.Euler(currentPitch, 0, currentRoll));
    }
}
