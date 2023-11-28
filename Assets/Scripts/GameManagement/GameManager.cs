using System;
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

    [SerializeField] private PointsManager pointsManager;
    [SerializeField] private ColorSyncher colorSyncher;

    [SerializeField] private BlocksManager blocksManager;
    [SerializeField] private Material blockMaterial;
    [SerializeField] private Material hexagonSubwooferMaterial;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI songNameUiText;
    [SerializeField] private TextMeshProUGUI songTimeUi;
    [SerializeField] private TrackVisualizer trackVisualizer;

    [Header("Effects")]
    [SerializeField] private FireworksManager fireworksManager;
    [SerializeField] private RaysManager raysManager;
    [SerializeField] private HexagonsManager hexagonsManager;

    [Header("End song UI")]
    [SerializeField] private TextMeshProUGUI endSongTitle;
    [SerializeField] private TextMeshProUGUI endSongScore;
    [SerializeField] private GameObject endSongUi;

    [Header("Track")]
    [SerializeField] private MeshFilter trackMeshFilter;
    [SerializeField] private MeshRenderer trackMeshRenderer;

    private TrackData trackData;

    private float previousAudioSourcePercentage;
    private bool previousAudioSourcePlaying;

    private string songPath;

    public bool IsInTrackScene { get; private set; }

    void Awake()
    {
        IsInTrackScene = false;
        fileBrowser.AddOnAudioFileSelectedListener((songPath) => StartCoroutine(LoadAudio(songPath, StartGame)));
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
        trackVisualizer.UpdateTrackVisualizerPosition(currentPercentage);

        UpdateSongTimeUi(currentPercentage);

        // End song detection
        if (EndOfAudioReached(currentPercentage))
            OnAudioEnded();

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

        pointsManager.ResetPoints();

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
        pointsManager.BlockPicked();
        fireworksManager.EmitFireworks(blockPosition);
    }

    public void BlockMissed()
    {
        pointsManager.BlockMissed();
    }

    public void AddToColorSyncher(IColorSynchable colorSynchable)
    {
        colorSyncher.AddColorSynchable(colorSynchable);
    }

    public TrackData GetTrackData()
    {
        return trackData;
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

    private IEnumerator LoadAudio(string songPath, Action onAudioLoaded = null)
    {
        this.songPath = songPath;
        yield return StartCoroutine(AudioUtils.LoadAudio("file:\\\\" + songPath, audioSource));
        onAudioLoaded?.Invoke();
    }

    private void StartGame()
    {
        // Track and spectrum generation
        trackData = trackManager.GenerateTrack(audioSource.clip, 4096);
        float[][] spectrum = AudioUtils.GetAudioSpectrumAmplitudes(audioSource.clip, 4096);

        // Setup track mesh and song title
        SetTrackMesh();
        SetSongTitle();

        // Spawn blocks and hexagons
        blocksManager.SpawnBlocksOnTrack(spectrum, audioSource.clip, trackData);
        hexagonsManager.SpawnHexagonsOnTrack(spectrum, audioSource.clip, trackData);

        // Reset points and compute new track total points
        pointsManager.ResetPoints();
        pointsManager.ComputeTrackTotalPoints(blocksManager.GetTotalBlocksCount());

        // Initialize player controller and rays manager
        playerController.Initialize();
        raysManager.Initialize();

        // Setup UI
        SetCursorVisibility(false);
        selectFileUi.SetActive(false);
        trackVisualizer.ShowTrackUi(trackData);

        // Start audio
        audioSource.Play();
        IsInTrackScene = true;
    }

    private void SetTrackMesh()
    {
        trackMeshFilter.mesh = trackData.mesh;
        trackMeshRenderer.material.SetFloat("_LineSubdivisions", 3000f * audioSource.clip.length / 160f);
    }

    private void SetSongTitle()
    {
        string songTitle = Utils.GetNameFromPath(songPath);
        endSongTitle.text = songTitle;
        songNameUiText.text = songTitle;
    }

    private bool EndOfAudioReached(float currentPercentage)
    {
        return previousAudioSourcePlaying && !audioSource.isPlaying &&
            (previousAudioSourcePercentage > currentPercentage || currentPercentage >= 1);
    }

    private void OnAudioEnded()
    {
        IsInTrackScene = false;
        SetCursorVisibility(true);
        endSongScore.text =
            $"SCORE: {pointsManager.CurrentPoints}/{pointsManager.TotalTrackPoints} " +
            $"({pointsManager.CurrentPoints * 100f / pointsManager.TotalTrackPoints:0.00}%)<br>" +
            $"PICKED: {pointsManager.PickedCount}/{pointsManager.PickedCount + pointsManager.MissedCount}<br>" +
            $"MISSED: <color=red>{pointsManager.MissedCount}</color>";
        endSongUi.SetActive(true);
    }

    private void UpdateSongTimeUi(float currentPercentage)
    {
        float audioLength = audioSource.clip.length;
        int currentMinutes = (int)(audioLength * currentPercentage / 60);
        int currentSeconds = (int)(audioLength * currentPercentage % 60);
        int totalMinutes = (int)(audioLength / 60);
        int totalSeconds = (int)(audioLength % 60);
        songTimeUi.text = $"{currentMinutes}:{currentSeconds:00} / {totalMinutes}:{totalSeconds:00}";
    }

    private void SetCursorVisibility(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
