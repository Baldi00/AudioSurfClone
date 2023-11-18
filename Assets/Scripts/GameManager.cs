using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class GameManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private PlayerController playerController;
    [SerializeField] private FileBrowser fileBrowser;
    [SerializeField] private TrackManager trackManager;
    [SerializeField] private PauseManager pauseManager;

    [SerializeField] private GameObject selectFileUi;

    [SerializeField] private ColorSyncher colorSyncher;

    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Material blockMaterial;

    [Header("Block spawn")]
    [SerializeField] private float maxDistanceFromCenter = 2.2f;
    [SerializeField] private int lowBeatFrequency = 20;
    [SerializeField] private float lowBeatThreshold = 0.1f;
    [SerializeField] private float lowBeatSkip = 0.5f;
    [SerializeField] private int highBeatFrequency = 7500;
    [SerializeField] private float highBeatThreshold = 0.025f;
    [SerializeField] private float highBeatSkip = 0.5f;

    [Header("UI")]
    [SerializeField] private Transform gameUiContainer;
    [SerializeField] private TextMeshProUGUI songNameUiText;
    [SerializeField] private TextMeshProUGUI pointsUiText;
    [SerializeField] private TextMeshProUGUI pointsPercentageUiText;
    [SerializeField] private GameObject pointsIncrementPrefab;
    [SerializeField] private float pointsIncrementDistanceFromCenter;

    [Header("Effects")]
    [SerializeField] private List<ParticleSystem> leftFireworks;
    [SerializeField] private List<ParticleSystem> centerFireworks;
    [SerializeField] private List<ParticleSystem> rightFireworks;
    [SerializeField] private RaysManager raysManager;
    [SerializeField] private GameObject hexagonSubwooferPrefab;
    [SerializeField] private Material hexagonSubwooferMaterial;
    [SerializeField] private float hexagonBeatDuration;

    private GameObject blocksContainer;
    private BSpline trackSpline;

    private List<Vector3> trackSplinePoints;

    private List<BlockManager> blocks;
    private List<BlockData> blocksData;
    private List<Transform> blocksTransforms;

    private NativeArray<Vector3> trackSplinePointsNativeArray;
    private NativeArray<BlockData> blocksDataNativeArray;
    private TransformAccessArray blocksTransformsAccessArray;
    private JobHandle updateBlocksPositionJobHandle;
    private UpdateBlockPositionsJob updateBlocksPositionJob;

    private int totalTrackPoints;
    private int currentPoints;
    private int currentPointsIncrement = 1;

    private int pointsIncrementUiSpawnPosition = 1;

    private List<int> lowBeatIndexes;
    private List<int> lowBeatIndexes2;
    private List<int> highBeatIndexes;
    private List<int> highBeatIndexes2;

    private GameObject hexagonContainer;
    private List<Transform> hexagonTransforms;
    private float hexagonTimer;
    private Vector3 hexagonStartScale;

    public bool IsGameRunning { get; private set; }

    void Awake()
    {
        IsGameRunning = false;

        fileBrowser.AddOnAudioFileSelectedListener((songPath) => StartCoroutine(LoadAudioAndStartGame(songPath)));
        PlayerColorSyncher playerColorSyncher = playerController.GetComponent<PlayerColorSyncher>();
        colorSyncher.AddColorSynchable(playerColorSyncher);
        colorSyncher.enabled = false;
        pauseManager.enabled = false;

        blocks = new List<BlockManager>();
        blocksData = new List<BlockData>();
        blocksTransforms = new List<Transform>();

        hexagonTransforms = new List<Transform>();
        hexagonStartScale = hexagonSubwooferPrefab.transform.localScale;
    }

    void Update()
    {
        if (!IsGameRunning)
            return;

        float currentPercentage = GetCurrentAudioTimePercentage();
        Color currentColor = trackSpline.GetSplineColor(currentPercentage);
        hexagonSubwooferMaterial.color = currentColor;
        blockMaterial.color = currentColor;
        UpdateBlocksPositions(currentPercentage);

        trackSpline.GetSplineIndexes(currentPercentage, out int currentIndex, out _);
        if (lowBeatIndexes2.Contains(currentIndex))
        {
            hexagonTimer = hexagonBeatDuration;
            foreach (Transform hexagonTransform in hexagonTransforms)
                hexagonTransform.localScale = hexagonStartScale * 1.5f;
        }
        else if (highBeatIndexes2.Contains(currentIndex) && hexagonTimer < hexagonBeatDuration / 2)
        {
            hexagonTimer = hexagonBeatDuration / 4;
            foreach (Transform hexagonTransform in hexagonTransforms)
                hexagonTransform.localScale = hexagonStartScale * 1.125f;
        }
        else if (hexagonTimer > 0)
        {
            hexagonTimer -= Time.deltaTime;

            foreach (Transform hexagonTransform in hexagonTransforms)
                hexagonTransform.localScale = hexagonStartScale * Mathf.Lerp(1f, 1.5f, hexagonTimer / hexagonBeatDuration);
        }
    }

    void OnDestroy()
    {
        if (trackSplinePointsNativeArray.IsCreated)
            trackSplinePointsNativeArray.Dispose();

        if (blocksDataNativeArray.IsCreated)
            blocksDataNativeArray.Dispose();

        if (blocksTransformsAccessArray.isCreated)
            blocksTransformsAccessArray.Dispose();
    }

    public void RestartSong()
    {
        audioSource.Stop();
        audioSource.Play();

        playerController.enabled = false;
        playerController.transform.position = Vector3.zero;

        foreach (BlockManager blockManager in blocks)
            blockManager.ResetBlock();

        currentPoints = 0;
        currentPointsIncrement = 1;

        pointsUiText.text = $"{currentPoints}";
        pointsPercentageUiText.text = (currentPoints * 100f / totalTrackPoints).ToString("0.00") + "%";

        PointsIncrementUiMover[] pointsIncrementUiMovers = FindObjectsOfType<PointsIncrementUiMover>();

        foreach (PointsIncrementUiMover pointsMover in pointsIncrementUiMovers)
            DestroyImmediate(pointsMover.gameObject);

        playerController.enabled = true;
    }

    public void BackToSelectMenu()
    {
        IsGameRunning = false;

        selectFileUi.SetActive(true);
        colorSyncher.enabled = false;
        pauseManager.enabled = false;
        playerController.StopFollowinTrack();
        Destroy(blocksContainer);
        blocksData.Clear();
        blocksTransforms.Clear();

        Destroy(hexagonContainer);
        hexagonTransforms.Clear();

        if (trackSplinePointsNativeArray.IsCreated)
            trackSplinePointsNativeArray.Dispose();

        if (blocksDataNativeArray.IsCreated)
            blocksDataNativeArray.Dispose();

        if (blocksTransformsAccessArray.isCreated)
            blocksTransformsAccessArray.Dispose();
    }

    public void BlockPicked(BlockPosition blockPosition)
    {
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

        currentPointsIncrement = Mathf.Min(200, currentPointsIncrement + 2);

        EmitFireworks(blockPosition);
    }

    public void BlockMissed()
    {
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
        currentPointsIncrement = 1;
        pointsUiText.text = $"{currentPoints}";
        pointsPercentageUiText.text = (currentPoints * 100f / totalTrackPoints).ToString("0.00") + "%";
    }

    private IEnumerator LoadAudioAndStartGame(string songPath)
    {
        yield return StartCoroutine(AudioUtils.LoadAudio("file:\\\\" + songPath, audioSource));

        trackManager.GenerateTrack(audioSource.clip, 4096);
        trackSpline = trackManager.GetTrackSpline();
        trackSplinePoints = trackSpline.GetSplinePoints();

        songNameUiText.text = songPath[(songPath.LastIndexOf("\\") + 1)..].Replace(".mp3", "").Replace(".wav", "");

        // Spawn blocks
        blocksContainer = new GameObject("Blocks container");
        float[][] spectrum = AudioUtils.GetAudioSpectrumAmplitudes(audioSource.clip, 4096);

        lowBeatIndexes = AudioUtils.GetBeatIndexes(spectrum, audioSource.clip.frequency, audioSource.clip.channels, lowBeatFrequency, lowBeatThreshold, lowBeatSkip);
        lowBeatIndexes2 = AudioUtils.GetBeatIndexes(spectrum, audioSource.clip.frequency, audioSource.clip.channels, lowBeatFrequency, lowBeatThreshold, 0);
        highBeatIndexes = AudioUtils.GetBeatIndexes(spectrum, audioSource.clip.frequency, audioSource.clip.channels, highBeatFrequency, highBeatThreshold, highBeatSkip);
        highBeatIndexes2 = AudioUtils.GetBeatIndexes(spectrum, audioSource.clip.frequency, audioSource.clip.channels, highBeatFrequency, 0.01f, 0);

        RemoveNearBeats(lowBeatIndexes, highBeatIndexes, 5);

        int spawnLocationNoise = 0;
        SpawnBlocks(lowBeatIndexes, ref spawnLocationNoise);
        SpawnBlocks(highBeatIndexes, ref spawnLocationNoise);

        trackSplinePointsNativeArray = new NativeArray<Vector3>(trackSplinePoints.ToArray(), Allocator.TempJob);
        blocksDataNativeArray = new NativeArray<BlockData>(blocksData.ToArray(), Allocator.TempJob);
        blocksTransformsAccessArray = new TransformAccessArray(blocksTransforms.ToArray());
        updateBlocksPositionJob = new UpdateBlockPositionsJob()
        {
            trackSplinePoints = trackSplinePointsNativeArray,
            blocksData = blocksDataNativeArray
        };

        // Hexagon subwoofer spawn
        hexagonContainer = new GameObject("Hexagon container");
        for (int i = 0; i < trackSplinePoints.Count; i += 64)
        {
            hexagonTransforms.Add(Instantiate(hexagonSubwooferPrefab, trackSplinePoints[i] + 50 * Vector3.forward + 4 * Vector3.up, Quaternion.Euler(0, 60, 0), hexagonContainer.transform).transform);
            hexagonTransforms.Add(Instantiate(hexagonSubwooferPrefab, trackSplinePoints[i] - 50 * Vector3.forward + 4 * Vector3.up, Quaternion.Euler(0, 120, 0), hexagonContainer.transform).transform);
        }

        // Final setup
        selectFileUi.SetActive(false);
        playerController.SetNormalizedIntensities(trackManager.GetNormalizedIntensities());
        playerController.StartFollowingTrack(trackSpline);

        ComputeTrackPoints();

        colorSyncher.enabled = true;
        pauseManager.enabled = true;
        audioSource.Play();
        IsGameRunning = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        raysManager.Initialize(trackSpline, trackManager.GetNormalizedIntensities());

        currentPoints = 0;
        currentPointsIncrement = 1;

        pointsUiText.text = $"{currentPoints}";
        pointsPercentageUiText.text = (currentPoints * 100f / totalTrackPoints).ToString("0.00") + "%";
    }

    private void SpawnBlocks(List<int> beatIndexes, ref int spawnLocationNoise)
    {
        foreach (int beatIndex in beatIndexes)
        {
            float percentage = trackSpline.GetSplinePercentageFromTrackIndex(beatIndex + 4);
            float blockSpawnZPosition = ((beatIndex + spawnLocationNoise) % 3 - 1) * maxDistanceFromCenter;

            GameObject block = Instantiate(blockPrefab,
                trackSpline.GetSplinePoint(percentage) + Vector3.forward * blockSpawnZPosition,
                Quaternion.LookRotation(trackSpline.GetSplineTangent(percentage), Vector3.up),
                blocksContainer.transform);

            BlockPosition blockPosition = BlockPosition.CENTER;
            if (blockSpawnZPosition > Mathf.Epsilon)
                blockPosition = BlockPosition.LEFT;
            else if (blockSpawnZPosition < -Mathf.Epsilon)
                blockPosition = BlockPosition.RIGHT;

            BlockManager blockManager = block.GetComponent<BlockManager>();
            blockManager.Initialize(blockPosition, percentage);

            blocks.Add(blockManager);

            blocksData.Add(blockManager.GetBlockData(maxDistanceFromCenter));

            blocksTransforms.Add(block.transform);

            spawnLocationNoise++;
        }
    }

    public float GetCurrentAudioTimePercentage()
    {
        if (!audioSource || !audioSource.clip)
            return 0;

        return audioSource.time / audioSource.clip.length;
    }

    public Color GetCurrentColor()
    {
        return trackSpline.GetSplineColor(GetCurrentAudioTimePercentage());
    }

    private void RemoveNearBeats(List<int> baseBeats, List<int> additiveBeats, int range)
    {
        List<int> toRemove = new List<int>();
        foreach (int additiveBeat in additiveBeats)
        {
            if (ListContainsInRange(baseBeats, additiveBeat, range))
                toRemove.Add(additiveBeat);
        }
        additiveBeats.RemoveAll(beat => toRemove.Contains(beat));
    }

    private bool ListContainsInRange(List<int> list, int toCheck, int range)
    {
        for (int i = toCheck - range; i < toCheck + range; i++)
            if (list.Contains(i))
                return true;
        return false;
    }

    private void UpdateBlocksPositions(float currentPercentage)
    {
        updateBlocksPositionJob.currentPercentage = currentPercentage;
        updateBlocksPositionJobHandle = updateBlocksPositionJob.Schedule(blocksTransformsAccessArray);
        updateBlocksPositionJobHandle.Complete();
    }

    [BurstCompile]
    private struct UpdateBlockPositionsJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<Vector3> trackSplinePoints;
        [ReadOnly] public NativeArray<BlockData> blocksData;
        [ReadOnly] public float currentPercentage;

        public void Execute(int index, TransformAccess transform)
        {
            float finalPercentage = blocksData[index].endPercentage;
            float currentBlockPercentage = Mathf.InverseLerp(finalPercentage - 0.5f, finalPercentage, currentPercentage);

            float currentBlockPositionPercentage =
                Mathf.Lerp(finalPercentage - 0.075f, finalPercentage, currentBlockPercentage);

            float lerp = math.lerp(0, trackSplinePoints.Length - 4, currentBlockPositionPercentage);
            int u = (int)lerp;
            float inter = lerp % 1;

            Vector3 position = Vector3.Lerp(trackSplinePoints[u], trackSplinePoints[u + 1], inter) + Vector3.forward * blocksData[index].zPosition;
            Quaternion rotation = Quaternion.LookRotation(trackSplinePoints[u + 1] - trackSplinePoints[u], Vector3.up);

            transform.position = position;
            transform.rotation = rotation;
        }
    }

    private void ComputeTrackPoints()
    {
        totalTrackPoints = 0;
        int increment = 1;
        for (int i = 0; i < blocksData.Count; i++)
        {
            totalTrackPoints += increment;
            increment = Mathf.Min(200, increment + 2);
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
