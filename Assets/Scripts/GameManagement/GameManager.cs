using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private PlayerController playerController;
    [SerializeField] private FileBrowser fileBrowser;
    [SerializeField] private TrackGenerator trackManager;

    [SerializeField] private GameObject selectFileUi;

    [SerializeField] private ColorSyncher colorSyncher;

    [SerializeField] private BlocksManager blocksManager;
    [SerializeField] private Material blockMaterial;
    [SerializeField] private Material hexagonSubwooferMaterial;

    [Header("UI")]
    [SerializeField] private Transform gameUiContainer;
    [SerializeField] private TextMeshProUGUI songNameUiText;
    [SerializeField] private TextMeshProUGUI pointsUiText;
    [SerializeField] private TextMeshProUGUI pointsPercentageUiText;
    [SerializeField] private TextMeshProUGUI songTimeUi;
    [SerializeField] private GameObject pointsIncrementPrefab;
    [SerializeField] private float pointsIncrementDistanceFromCenter;

    [Header("Effects")]
    [SerializeField] private List<ParticleSystem> leftFireworks;
    [SerializeField] private List<ParticleSystem> centerFireworks;
    [SerializeField] private List<ParticleSystem> rightFireworks;
    [SerializeField] private RaysManager raysManager;
    [SerializeField] private HexagonsManager hexagonsManager;

    [SerializeField] private LineRenderer trackVisualizer;
    [SerializeField] private RectTransform trackCurrentPointVisualizer;

    [Header("End song UI")]
    [SerializeField] private TextMeshProUGUI endSongTitle;
    [SerializeField] private TextMeshProUGUI endSongScore;
    [SerializeField] private GameObject endSongUi;

    [Header("Track")]
    [SerializeField] private MeshFilter trackMeshFilter;
    [SerializeField] private MeshRenderer trackMeshRenderer;

    private TrackData trackData;

    private int totalTrackPoints;
    private int currentPoints;
    private int currentPointsIncrement = 1;
    private int pickedCount;
    private int missedCount;

    private int pointsIncrementUiSpawnPosition = 1;

    private float previousAudioSourcePercentage;
    private bool previousAudioSourcePlaying;

    private string songTitle;

    public bool IsInTrackScene { get; private set; }

    void Awake()
    {
        IsInTrackScene = false;
        fileBrowser.AddOnAudioFileSelectedListener((songPath) => StartCoroutine(LoadAudioAndStartGame(songPath)));
        endSongUi.SetActive(false);
    }

    void Update()
    {
        if (!IsInTrackScene)
            return;

        float currentPercentage = GetCurrentAudioTimePercentage();
        Color currentColor = trackData.spline.GetColorAt(currentPercentage);
        hexagonSubwooferMaterial.color = currentColor;
        blockMaterial.color = currentColor;
        blocksManager.UpdateBlocksPositions(currentPercentage);
        hexagonsManager.UpdateHexagonsScale(currentPercentage);

        float lerp = Mathf.Lerp(0, trackVisualizer.positionCount - 2, currentPercentage);
        int firstSubSplinePointIndex = (int)lerp;
        float subSplineInterpolator = lerp % 1;
        trackCurrentPointVisualizer.localPosition = Vector3.Lerp(trackVisualizer.GetPosition(firstSubSplinePointIndex), trackVisualizer.GetPosition(firstSubSplinePointIndex + 1), subSplineInterpolator);

        // Update song time
        songTimeUi.text = $"{(int)(audioSource.clip.length * currentPercentage / 60)}:{(int)(audioSource.clip.length * currentPercentage % 60):00} / {(int)(audioSource.clip.length / 60)}:{(int)(audioSource.clip.length % 60):00}";

        // End song detection
        if (previousAudioSourcePlaying && !audioSource.isPlaying && (previousAudioSourcePercentage > currentPercentage || currentPercentage >= 1))
        {
            IsInTrackScene = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            endSongTitle.text = songTitle;
            endSongScore.text = $"SCORE: {currentPoints}/{totalTrackPoints} ({currentPoints * 100f / totalTrackPoints:0.00}%)<br>PICKED: {pickedCount}/{pickedCount + missedCount}<br>MISSED: <color=red>{missedCount}</color>";
            endSongUi.SetActive(true);
        }

        previousAudioSourcePercentage = currentPercentage;
        previousAudioSourcePlaying = audioSource.isPlaying;
    }

    public static GameManager GetGameManager()
    {
        return GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    public void RestartSong()
    {
        endSongUi.SetActive(false);
        IsInTrackScene = true;

        audioSource.Stop();
        audioSource.Play();

        playerController.enabled = false;
        playerController.transform.position = Vector3.zero;

        blocksManager.ResetAllBlocks();

        currentPoints = 0;
        currentPointsIncrement = 1;
        pickedCount = 0;
        missedCount = 0;

        pointsUiText.text = $"{currentPoints}";
        pointsPercentageUiText.text = (currentPoints * 100f / totalTrackPoints).ToString("0.00") + "%";

        PointsIncrementUiMover[] pointsIncrementUiMovers = FindObjectsOfType<PointsIncrementUiMover>();

        foreach (PointsIncrementUiMover pointsMover in pointsIncrementUiMovers)
            DestroyImmediate(pointsMover.gameObject);

        playerController.enabled = true;
    }

    public void BackToSelectMenu()
    {
        endSongUi.SetActive(false);

        IsInTrackScene = false;

        selectFileUi.SetActive(true);

        blocksManager.RemoveAllBlocks();
        hexagonsManager.RemoveAllHexagons();
    }

    public void BlockPicked(BlockPosition blockPosition)
    {
        pickedCount++;

        currentPoints += currentPointsIncrement;

        pointsUiText.text = $"{currentPoints}";
        pointsPercentageUiText.text = (currentPoints * 100f / totalTrackPoints).ToString("0.00") + "%";

        PointsIncrementUiMover pointsMover = Instantiate(pointsIncrementPrefab,
            pointsUiText.transform.position + pointsIncrementUiSpawnPosition * pointsIncrementDistanceFromCenter * Vector3.right,
            Quaternion.identity,
            gameUiContainer.transform)
            .GetComponent<PointsIncrementUiMover>();

        pointsMover.Setup($"+{currentPointsIncrement}",
            pointsIncrementUiSpawnPosition * pointsIncrementDistanceFromCenter,
            pointsIncrementUiSpawnPosition,
            Color.white);

        pointsIncrementUiSpawnPosition = -pointsIncrementUiSpawnPosition;

        currentPointsIncrement = Mathf.Min(200, currentPointsIncrement + 4);

        EmitFireworks(blockPosition);
    }

    public void BlockMissed()
    {
        missedCount++;

        PointsIncrementUiMover pointsMover = Instantiate(pointsIncrementPrefab,
            pointsUiText.transform.position + pointsIncrementUiSpawnPosition * pointsIncrementDistanceFromCenter * Vector3.right,
            Quaternion.identity,
            gameUiContainer.transform)
            .GetComponent<PointsIncrementUiMover>();

        pointsMover.Setup($"-{Mathf.Min(currentPoints, 200)}",
            pointsIncrementUiSpawnPosition * pointsIncrementDistanceFromCenter,
            pointsIncrementUiSpawnPosition,
            Color.red);

        pointsIncrementUiSpawnPosition = -pointsIncrementUiSpawnPosition;

        currentPoints = Mathf.Max(0, currentPoints - 200);
        currentPointsIncrement = Mathf.Max(1, currentPointsIncrement - 50);

        if (currentPointsIncrement % 2 == 0)
            currentPointsIncrement++;

        pointsUiText.text = $"{currentPoints}";
        pointsPercentageUiText.text = (currentPoints * 100f / totalTrackPoints).ToString("0.00") + "%";
    }

    public void AddToColorSyncher(IColorSynchable colorSynchable)
    {
        colorSyncher.AddColorSynchable(colorSynchable);
    }

    public TrackData GetTrackData()
    {
        return trackData;
    }

    private IEnumerator LoadAudioAndStartGame(string songPath)
    {
        yield return StartCoroutine(AudioUtils.LoadAudio("file:\\\\" + songPath, audioSource));

        trackData = trackManager.GenerateTrack(audioSource.clip, 4096);

        songTitle = songPath[(songPath.LastIndexOf("\\") + 1)..].Replace(".mp3", "").Replace(".wav", "");
        songNameUiText.text = songTitle;

        float[][] spectrum = AudioUtils.GetAudioSpectrumAmplitudes(audioSource.clip, 4096);
        blocksManager.SpawnBlocksOnTrack(spectrum, audioSource.clip, trackData);
        hexagonsManager.SpawnHexagonsOnTrack(spectrum, audioSource.clip, trackData);

        // Track
        float trackVisualizerWidth = (trackVisualizer.transform as RectTransform).rect.width;
        float trackVisualizerHeight = (trackVisualizer.transform as RectTransform).rect.height;
        float minTrackX, minTrackY, maxTrackX, maxTrackY;
        minTrackX = minTrackY = float.MaxValue;
        maxTrackX = maxTrackY = float.MinValue;

        for (int i = 0; i < trackData.splinePoints.Length; i++)
        {
            if (trackData.splinePoints[i].x < minTrackX)
                minTrackX = trackData.splinePoints[i].x;
            if (trackData.splinePoints[i].x > maxTrackX)
                maxTrackX = trackData.splinePoints[i].x;
            if (trackData.splinePoints[i].y < minTrackY)
                minTrackY = trackData.splinePoints[i].y;
            if (trackData.splinePoints[i].y > maxTrackY)
                maxTrackY = trackData.splinePoints[i].y;
        }

        trackVisualizer.positionCount = (int)(trackData.splinePoints.Length / 50f);
        float step = 1f / trackVisualizer.positionCount;
        for (int i = 0; i < trackVisualizer.positionCount; i++)
            trackVisualizer.SetPosition(i, new Vector3(i * step * trackVisualizerWidth, -trackVisualizerHeight * (1 - Mathf.InverseLerp(minTrackY, maxTrackY, trackData.splinePoints[i * 50].y))));

        // Final setup
        selectFileUi.SetActive(false);
        ComputeTrackPoints();

        audioSource.Play();
        IsInTrackScene = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        playerController.Initialize();
        raysManager.Initialize();

        currentPoints = 0;
        currentPointsIncrement = 1;
        pickedCount = 0;
        missedCount = 0;

        pointsUiText.text = $"{currentPoints}";
        pointsPercentageUiText.text = (currentPoints * 100f / totalTrackPoints).ToString("0.00") + "%";

        trackMeshFilter.mesh = trackData.mesh;
        trackMeshRenderer.material.SetFloat("_LineSubdivisions", 3000f * audioSource.clip.length / 160f);
    }

    public float GetCurrentAudioTimePercentage()
    {
        if (!audioSource || !audioSource.clip)
            return 0;

        return audioSource.time / audioSource.clip.length;
    }

    public Color GetCurrentColor()
    {
        return trackData.spline.GetColorAt(GetCurrentAudioTimePercentage());
    }

    private void ComputeTrackPoints()
    {
        totalTrackPoints = 0;
        int increment = 1;
        for (int i = 0; i < blocksManager.GetTotalBlocksCount(); i++)
        {
            totalTrackPoints += increment;
            increment = Mathf.Min(200, increment + 4);
        }
    }

    private void EmitFireworks(BlockPosition blockPosition)
    {
        List<ParticleSystem> currentParticleSystem = blockPosition switch
        {
            BlockPosition.LEFT => leftFireworks,
            BlockPosition.CENTER => centerFireworks,
            BlockPosition.RIGHT => rightFireworks,
            _ => null,
        };

        for (int i = 0; i < currentParticleSystem.Count; i++)
            currentParticleSystem[i].Play();
    }
}
