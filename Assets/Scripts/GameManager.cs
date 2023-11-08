using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private FileBrowser fileBrowser;
    [SerializeField]
    private TrackManager trackManager;

    [SerializeField]
    private GameObject selectFileUi;
    [SerializeField]
    private GameObject playerPrefab;

    [SerializeField]
    private ColorSyncher colorSyncher;

    private BSpline trackSpline;

    List<int> lowBeatIndexes;

    void Awake()
    {
        fileBrowser.AddOnAudioFileSelectedListener((songPath) => StartCoroutine(LoadAudioAndStartGame(songPath)));
        colorSyncher.enabled = false;

    }

    void Update()
    {
        if (lowBeatIndexes == null)
            return;

        trackSpline.GetSplineIndexes(GetCurrentAudioTimePercentage(), out int i, out _);
        if (lowBeatIndexes.Contains(i))
            Debug.Log("Beat " + i);
    }

    private IEnumerator LoadAudioAndStartGame(string songPath)
    {
        yield return StartCoroutine(AudioLoader.LoadAudio("file:\\\\" + songPath, audioSource));

        double[][] spectrum = AudioAnalyzer.GetAudioSpectrum(audioSource.clip, 4096);
        //lowBeatIndexes = BeatDetector.GetBeatIndexes(spectrum, 4096, audioSource.clip, 7500, 0.005f, 0.15f); //high
        lowBeatIndexes = BeatDetector.GetBeatIndexes(spectrum, 4096, audioSource.clip, 20, 0.2f, 0.15f); //low

        trackManager.GenerateTrack(audioSource.clip, 4096);
        trackSpline = trackManager.GetTrackSpline();

        selectFileUi.SetActive(false);
        GameObject player = Instantiate(playerPrefab);
        PlayerController playerController = player.GetComponent<PlayerController>();
        playerController.SetNormalizedIntensities(trackManager.GetNormalizedIntensities());
        playerController.StartFollowingTrack(trackSpline, audioSource);
        PlayerColorSyncher playerColorSyncher = player.GetComponent<PlayerColorSyncher>();
        colorSyncher.AddColorSynchable(playerColorSyncher);

        colorSyncher.enabled = true;
        audioSource.Play();
    }

    public float GetCurrentAudioTimePercentage()
    {
        return audioSource.time / audioSource.clip.length;
    }

    public Color GetCurrentColor()
    {
        return trackSpline.GetSplineColor(GetCurrentAudioTimePercentage());
    }
}
