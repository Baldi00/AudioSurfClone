using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    [SerializeField] private float startAudioDelay = 1f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI songNameUiText;
    [SerializeField] private TextMeshProUGUI songTimeUi;
    [SerializeField] private TrackVisualizer trackVisualizer;

    [Header("Effects")]
    [SerializeField] private FireworksManager fireworksManager;
    [SerializeField] private RaysManager raysManager;
    [SerializeField] private HexagonsManager hexagonsManager;
    [SerializeField] private List<ParticleSystem> particles;

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

#if UNITY_ANDROID && !UNITY_EDITOR
        Application.targetFrameRate = 60;
#else
        Application.targetFrameRate = 0;
#endif
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

    /// <summary>
    /// Returns a reference to the Game Manager
    /// </summary>
    /// <returns></returns>
    public static GameManager GetGameManager()
    {
        return GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    /// <summary>
    /// Restarts the song
    /// </summary>
    public void RestartSong()
    {
        endSongUi.SetActive(false);
        IsInTrackScene = true;

        audioSource.Stop();
        audioSource.PlayDelayed(startAudioDelay);

        playerController.enabled = false;
        playerController.transform.position = Vector3.zero;

        blocksManager.ResetAllBlocks();
        pointsManager.ResetPoints();
        particles.ForEach(particle => particle.Clear());

        playerController.enabled = true;
    }

    /// <summary>
    /// Returns to the select song menu
    /// </summary>
    public void BackToSelectMenu()
    {
        endSongUi.SetActive(false);

        IsInTrackScene = false;

        selectFileUi.SetActive(true);

        blocksManager.RemoveAllBlocks();
        hexagonsManager.RemoveAllHexagons();
        particles.ForEach(particle => particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear));
    }

    /// <summary>
    /// Increases points and emits fireworks at the given position
    /// </summary>
    /// <param name="blockPosition">The position of the picked block</param>
    public void BlockPicked(BlockPosition blockPosition)
    {
        pointsManager.BlockPicked();
        fireworksManager.EmitFireworks(blockPosition);
    }

    /// <summary>
    /// Decreases the points
    /// </summary>
    public void BlockMissed()
    {
        pointsManager.BlockMissed();
    }

    /// <summary>
    /// Adds the given color syncable to the list
    /// </summary>
    /// <param name="colorSynchable">The color syncable to add</param>
    public void AddToColorSyncher(IColorSynchable colorSynchable)
    {
        colorSyncher.AddColorSynchable(colorSynchable);
    }

    /// <summary>
    /// Returns the generated track data for the current audio
    /// </summary>
    /// <returns></returns>
    public TrackData GetTrackData()
    {
        return trackData;
    }

    /// <summary>
    /// Returns the current audio reproduction percentage
    /// </summary>
    /// <returns>The current audio reproduction percentage</returns>
    public float GetCurrentAudioTimePercentage()
    {
        if (!audioSource || !audioSource.clip)
            return 0;

        return audioSource.time / audioSource.clip.length;
    }

    /// <summary>
    /// Returns the color associated with the current audio percentage
    /// </summary>
    /// <returns>The color associated with the current audio percentage</returns>
    public Color GetCurrentColor()
    {
        return trackData.spline.GetColorAt(GetCurrentAudioTimePercentage());
    }

    /// <summary>
    /// Loads the audio at the given path inside the audioSource and than calls the onAudioLoaded callback
    /// </summary>
    /// <param name="songPath">The path of the song to load</param>
    /// <param name="onAudioLoaded">The callback to call after the audio has been loaded</param>
    private IEnumerator LoadAudio(string songPath, Action onAudioLoaded = null)
    {
        this.songPath = songPath;
        char separator = Path.DirectorySeparatorChar;
        yield return StartCoroutine(AudioUtils.LoadAudio($"file:{separator}{separator}{songPath}", audioSource));
        onAudioLoaded?.Invoke();
    }

    /// <summary>
    /// Instantiates and sets up all the parts of the game, then starts the audio reproduction
    /// </summary>
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
        Utils.SetCursorVisibility(false);
        selectFileUi.SetActive(false);
        trackVisualizer.ShowTrackUi(trackData);

        particles.ForEach(particle => particle.Play());

        // Start audio
        audioSource.PlayDelayed(startAudioDelay);
        IsInTrackScene = true;
    }

    /// <summary>
    /// Sets up the track mesh on the scene
    /// </summary>
    private void SetTrackMesh()
    {
        trackMeshFilter.mesh = trackData.mesh;
        trackMeshRenderer.material.SetFloat("_LineSubdivisions", 3000f * audioSource.clip.length / 160f);
    }

    /// <summary>
    /// Sets the song title ui
    /// </summary>
    private void SetSongTitle()
    {
        string songTitle = Utils.GetNameFromPath(songPath);
        endSongTitle.text = songTitle;
        songNameUiText.text = songTitle;
    }

    /// <summary>
    /// Checks if the end of the song has been reached
    /// </summary>
    /// <param name="currentPercentage">The current audio percentage</param>
    /// <returns>True if the end of the audio has been reached, false otherwise</returns>
    private bool EndOfAudioReached(float currentPercentage)
    {
        return previousAudioSourcePlaying && !audioSource.isPlaying &&
            (previousAudioSourcePercentage > currentPercentage || currentPercentage >= 1);
    }

    /// <summary>
    /// Shows the end song ui
    /// </summary>
    private void OnAudioEnded()
    {
        IsInTrackScene = false;
        Utils.SetCursorVisibility(true);
        endSongScore.text =
            $"SCORE: {pointsManager.CurrentPoints}/{pointsManager.TotalTrackPoints} " +
            $"({pointsManager.CurrentPoints * 100f / pointsManager.TotalTrackPoints:0.00}%)<br>" +
            $"PICKED: {pointsManager.PickedCount}/{pointsManager.PickedCount + pointsManager.MissedCount}<br>" +
            $"MISSED: <color=red>{pointsManager.MissedCount}</color>";
        endSongUi.SetActive(true);
    }

    /// <summary>
    /// Updates the song timer ui
    /// </summary>
    /// <param name="currentPercentage">The current audio percentage</param>
    private void UpdateSongTimeUi(float currentPercentage)
    {
        float audioLength = audioSource.clip.length;
        int currentMinutes = (int)(audioLength * currentPercentage / 60);
        int currentSeconds = (int)(audioLength * currentPercentage % 60);
        int totalMinutes = (int)(audioLength / 60);
        int totalSeconds = (int)(audioLength % 60);
        songTimeUi.text = $"{currentMinutes}:{currentSeconds:00} / {totalMinutes}:{totalSeconds:00}";
    }
}
