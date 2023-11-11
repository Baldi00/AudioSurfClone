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

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private PlayerController playerController;
    [SerializeField]
    private FileBrowser fileBrowser;
    [SerializeField]
    private TrackManager trackManager;
    [SerializeField]
    private PauseManager pauseManager;

    [SerializeField]
    private GameObject selectFileUi;

    [SerializeField]
    private ColorSyncher colorSyncher;

    [SerializeField]
    private GameObject blockPrefab;
    [SerializeField]
    private Material blockMaterial;

    private GameObject blockContainer;
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

        for (int i = 0; i < blocksData.Count; i++)
        {
            float finalPercentage = blocksData[i].endPercentage;
            float currentBlockPositionPercentage = Mathf.Lerp(finalPercentage - 0.075f, finalPercentage,
                    Mathf.InverseLerp(finalPercentage - 0.5f, finalPercentage, currentPercentage));

            Vector3 position = trackSpline.GetSplinePoint(currentBlockPositionPercentage) + Vector3.forward * blocksData[i].zPosition;
            Quaternion rotation = Quaternion.LookRotation(trackSpline.GetSplineTangent(currentBlockPositionPercentage), Vector3.up);

            blocksData[i].blockTransform.parent.SetPositionAndRotation(position, rotation);
        }
    }

    public void BackToSelectMenu()
    {
        IsGameRunning = false;

        selectFileUi.SetActive(true);
        colorSyncher.enabled = false;
        pauseManager.enabled = false;
        playerController.StopFollowinTrack();
        Destroy(blockContainer);
        blocksData.Clear();
    }

    private IEnumerator LoadAudioAndStartGame(string songPath)
    {
        yield return StartCoroutine(AudioLoader.LoadAudio("file:\\\\" + songPath, audioSource));

        trackManager.GenerateTrack(audioSource.clip, 4096);
        trackSpline = trackManager.GetTrackSpline();

        // Spawn blocks
        blockContainer = new GameObject("Blocks container");
        float[][] spectrum = AudioAnalyzer.GetAudioSpectrum(audioSource.clip, 4096);
        List<int> lowBeatIndexes = BeatDetector.GetBeatIndexes(spectrum, 4096, audioSource.clip, 20, 0.1f, 0.5f); //low

        int spawnLocationNoise = 0;
        foreach (int lowBeatIndex in lowBeatIndexes)
        {
            float percentage = trackSpline.GetSplinePercentageFromTrackIndex(lowBeatIndex + 4);
            Renderer blockRenderer = Instantiate(
                blockPrefab,
                trackSpline.GetSplinePoint(percentage) + Vector3.forward * ((lowBeatIndex + spawnLocationNoise) % 3 - 1) * 2.5f,
                Quaternion.LookRotation(trackSpline.GetSplineTangent(percentage), Vector3.up),
                blockContainer.transform).GetComponentInChildren<Renderer>();
            blocksData.Add(new BlockData { blockTransform = blockRenderer.transform, endPercentage = percentage, zPosition = ((lowBeatIndex + spawnLocationNoise) % 3 - 1) * 2.5f });

            spawnLocationNoise++;
        }

        List<int> highBeatIndexes = BeatDetector.GetBeatIndexes(spectrum, 4096, audioSource.clip, 7500, 0.025f, 0.5f); //high
        foreach (int highBeatIndex in highBeatIndexes)
        {
            if (ListContainsInRange(lowBeatIndexes, highBeatIndex, 5))
                continue;

            float percentage = trackSpline.GetSplinePercentageFromTrackIndex(highBeatIndex + 4);
            Renderer blockRenderer = Instantiate(
                blockPrefab,
                trackSpline.GetSplinePoint(percentage) + Vector3.forward * ((highBeatIndex + spawnLocationNoise) % 3 - 1) * 2.5f,
                Quaternion.LookRotation(trackSpline.GetSplineTangent(percentage), Vector3.up),
                blockContainer.transform).GetComponentInChildren<Renderer>();
            blocksData.Add(new BlockData { blockTransform = blockRenderer.transform, endPercentage = percentage, zPosition = ((highBeatIndex + spawnLocationNoise) % 3 - 1) * 2.5f });

            spawnLocationNoise++;
        }

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

    public float GetCurrentAudioTimePercentage()
    {
        return audioSource.time / audioSource.clip.length;
    }

    public Color GetCurrentColor()
    {
        return trackSpline.GetSplineColor(GetCurrentAudioTimePercentage());
    }

    private bool ListContainsInRange(List<int> list, int toCheck, int range)
    {
        for (int i = toCheck - range; i < toCheck + range; i++)
            if (list.Contains(i))
                return true;
        return false;
    }
}
