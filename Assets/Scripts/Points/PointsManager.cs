using TMPro;
using UnityEngine;

public class PointsManager : MonoBehaviour
{
    [SerializeField] private Transform gameUiContainer;
    [SerializeField] private TextMeshProUGUI pointsUiText;
    [SerializeField] private TextMeshProUGUI pointsPercentageUiText;
    [SerializeField] private GameObject pointsIncrementPrefab;
    [SerializeField] private float pointsIncrementDistanceFromCenter;

    private int currentPointsIncrement;
    private int pointsIncrementUiSpawnPosition = 1;

    public int TotalTrackPoints { get; private set; }
    public int CurrentPoints { get; private set; }
    public int PickedCount { get; private set; }
    public int MissedCount { get; private set; }

    /// <summary>
    /// Computes the total points of the current audio
    /// </summary>
    /// <param name="blocksCount">The total number of blocks for this audio</param>
    public void ComputeTrackTotalPoints(int blocksCount)
    {
        TotalTrackPoints = 0;
        int increment = 1;
        for (int i = 0; i < blocksCount; i++)
        {
            TotalTrackPoints += increment;
            increment = Mathf.Min(200, increment + 4);
        }

        UpdateUi();
    }

    /// <summary>
    /// Adds the points and spawn the related ui
    /// </summary>
    public void BlockPicked()
    {
        PickedCount++;
        CurrentPoints += currentPointsIncrement;
        currentPointsIncrement = Mathf.Min(200, currentPointsIncrement + 4);

        UpdateUi();
        SpawnPointsIncrementUi($"+{currentPointsIncrement}", Color.white);
    }

    /// <summary>
    /// Removes the points and spawns the related ui
    /// </summary>
    public void BlockMissed()
    {
        MissedCount++;
        CurrentPoints = Mathf.Max(0, CurrentPoints - 200);
        currentPointsIncrement = Mathf.Max(1, currentPointsIncrement - 50);

        if (currentPointsIncrement % 2 == 0)
            currentPointsIncrement++;

        UpdateUi();
        SpawnPointsIncrementUi($"-{Mathf.Min(CurrentPoints, 200)}", Color.red);
    }

    /// <summary>
    /// Resets the points system
    /// </summary>
    public void ResetPoints()
    {
        CurrentPoints = 0;
        currentPointsIncrement = 1;
        PickedCount = 0;
        MissedCount = 0;

        UpdateUi();
        DestroyAllPointsIncrementUi();
    }

    /// <summary>
    /// Updates the ui to match the inner model
    /// </summary>
    private void UpdateUi()
    {
        pointsUiText.text = $"{CurrentPoints}";
        pointsPercentageUiText.text = (CurrentPoints * 100f / TotalTrackPoints).ToString("0.00") + "%";
    }

    /// <summary>
    /// Spawns the point increments ui with the given text and color
    /// </summary>
    /// <param name="text">The inner text of the spawned label</param>
    /// <param name="color">The color of the spawned label</param>
    private void SpawnPointsIncrementUi(string text, Color color)
    {
        PointsIncrementUiMover pointsMover = Instantiate(pointsIncrementPrefab,
            pointsUiText.transform.position + pointsIncrementUiSpawnPosition * pointsIncrementDistanceFromCenter * Vector3.right,
            Quaternion.identity,
            gameUiContainer.transform)
            .GetComponent<PointsIncrementUiMover>();

        pointsMover.Setup(text,
            pointsIncrementUiSpawnPosition * pointsIncrementDistanceFromCenter,
            pointsIncrementUiSpawnPosition,
            color);

        // Invert spawn position
        pointsIncrementUiSpawnPosition = -pointsIncrementUiSpawnPosition;
    }

    /// <summary>
    /// Destroys all the remaining points increment ui left in the scene
    /// </summary>
    private void DestroyAllPointsIncrementUi()
    {
        PointsIncrementUiMover[] pointsIncrementUiMovers = FindObjectsOfType<PointsIncrementUiMover>();

        foreach (PointsIncrementUiMover pointsMover in pointsIncrementUiMovers)
            DestroyImmediate(pointsMover.gameObject);
    }
}
