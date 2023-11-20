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

    void Update()
    {
        currentPitch = pitchAmplitude * (Mathf.Sin(pitchFrequency * Time.time) * 0.5f + 0.5f);
        currentFloating = floatingAmplitude * (Mathf.Sin(floatingFrequency * Time.time) * 0.5f + 0.5f);
        currentRoll = Mathf.Lerp(-Input.GetAxis("Mouse X") * Time.deltaTime * rollMultiplier, currentRoll, rollSmoothing);
        currentRoll = Mathf.Clamp(currentRoll, -maxRoll, maxRoll);

        transform.localPosition = Vector3.up * currentFloating + positionOffset;
        transform.localRotation = Quaternion.Euler(currentPitch, 0, currentRoll);
    }
}
