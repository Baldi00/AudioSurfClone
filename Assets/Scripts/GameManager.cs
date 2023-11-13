using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private struct BlockData
    {
        public Transform blockTransform;
        public float endPercentage;
        public float zPosition;
    }

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
    [SerializeField] private int lowBeatFrequency = 20;
    [SerializeField] private float lowBeatThreshold = 0.1f;
    [SerializeField] private float lowBeatSkip = 0.5f;
    [SerializeField] private int highBeatFrequency = 7500;
    [SerializeField] private float highBeatThreshold = 0.025f;
    [SerializeField] private float highBeatSkip = 0.5f;

    private GameObject blocksContainer;
    private BSpline trackSpline;
    private List<BlockData> blocksData;

    public bool IsGameRunning { get; private set; }

    void Awake()
    {
        IsGameRunning = false;

        fileBrowser.AddOnAudioFileSelectedListener((songPath) => StartCoroutine(LoadAudioAndStartGame(songPath)));
        PlayerColorSyncher playerColorSyncher = playerController.GetComponent<PlayerColorSyncher>();
        colorSyncher.AddColorSynchable(playerColorSyncher);
        playerController.gameObject.SetActive(false);
        colorSyncher.enabled = false;
        pauseManager.enabled = false;

        blocksData = new List<BlockData>();
    }

    void Update()
    {
        if (!IsGameRunning)
            return;

        float currentPercentage = GetCurrentAudioTimePercentage();
        blockMaterial.color = trackSpline.GetSplineColor(currentPercentage);
        UpdateBlocksPositions(currentPercentage);
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
    }

    private IEnumerator LoadAudioAndStartGame(string songPath)
    {
        yield return StartCoroutine(AudioLoader.LoadAudio("file:\\\\" + songPath, audioSource));

        trackManager.GenerateTrack(audioSource.clip, 4096);
        trackSpline = trackManager.GetTrackSpline();

        // Spawn blocks
        blocksContainer = new GameObject("Blocks container");
        float[][] spectrum = AudioAnalyzer.GetAudioSpectrum(audioSource.clip, 4096);

        List<int> lowBeatIndexes =
            BeatDetector.GetBeatIndexes(spectrum, 4096, audioSource.clip, lowBeatFrequency, lowBeatThreshold, lowBeatSkip);
        List<int> highBeatIndexes =
            BeatDetector.GetBeatIndexes(spectrum, 4096, audioSource.clip, highBeatFrequency, highBeatThreshold, highBeatSkip);

        RemoveNearBeats(lowBeatIndexes, highBeatIndexes, 5);

        int spawnLocationNoise = 0;
        SpawnBlocks(lowBeatIndexes, ref spawnLocationNoise);
        SpawnBlocks(highBeatIndexes, ref spawnLocationNoise);

        // Final setup
        selectFileUi.SetActive(false);
        playerController.SetNormalizedIntensities(trackManager.GetNormalizedIntensities());
        playerController.StartFollowingTrack(trackSpline);

        colorSyncher.enabled = true;
        pauseManager.enabled = true;
        playerController.gameObject.SetActive(true);
        audioSource.Play();
        IsGameRunning = true;
    }

    private void SpawnBlocks(List<int> beatIndexes, ref int spawnLocationNoise)
    {
        foreach (int beatIndex in beatIndexes)
        {
            float percentage = trackSpline.GetSplinePercentageFromTrackIndex(beatIndex + 4);
            float blockSpawnZPosition = ((beatIndex + spawnLocationNoise) % 3 - 1) * 2.5f;

            GameObject block = Instantiate(blockPrefab,
                trackSpline.GetSplinePoint(percentage) + Vector3.forward * blockSpawnZPosition,
                Quaternion.LookRotation(trackSpline.GetSplineTangent(percentage), Vector3.up),
                blocksContainer.transform);

            blocksData.Add(new BlockData
            {
                blockTransform = block.transform,
                endPercentage = percentage,
                zPosition = blockSpawnZPosition
            });

            spawnLocationNoise++;
        }
    }

    public float GetCurrentAudioTimePercentage()
    {
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
        for (int i = 0; i < blocksData.Count; i++)
        {
            float finalPercentage = blocksData[i].endPercentage;
            float currentBlockPercentage = Mathf.InverseLerp(finalPercentage - 0.5f, finalPercentage, currentPercentage);

            if (currentBlockPercentage <= Mathf.Epsilon || currentBlockPercentage >= 1 - Mathf.Epsilon)
                continue;

            float currentBlockPositionPercentage =
                Mathf.Lerp(finalPercentage - 0.075f, finalPercentage, currentBlockPercentage);

            Vector3 position = trackSpline.GetSplinePoint(currentBlockPositionPercentage) + Vector3.forward * blocksData[i].zPosition;
            Quaternion rotation = Quaternion.LookRotation(trackSpline.GetSplineTangent(currentBlockPositionPercentage), Vector3.up);

            blocksData[i].blockTransform.SetPositionAndRotation(position, rotation);
        }
    }
}
