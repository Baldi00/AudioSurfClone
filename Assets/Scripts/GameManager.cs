using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
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

    private BSpline trackSpline;

    List<int> lowBeatIndexes;

    void Awake()
    {
        fileBrowser.AddOnAudioFileSelectedListener((songPath) => StartCoroutine(LoadAudioAndStartGame(songPath)));
        PlayerColorSyncher playerColorSyncher = playerController.GetComponent<PlayerColorSyncher>();
        colorSyncher.AddColorSynchable(playerColorSyncher);
        playerController.gameObject.SetActive(false);
        colorSyncher.enabled = false;
        pauseManager.enabled = false;
    }

    void Update()
    {
        if (lowBeatIndexes == null)
            return;

        trackSpline.GetSplineIndexes(GetCurrentAudioTimePercentage(), out int i, out _);
        if (lowBeatIndexes.Contains(i))
            Debug.Log("Beat " + i);
    }

    public void BackToSelectMenu()
    {
        selectFileUi.SetActive(true);
        colorSyncher.enabled = false;
        pauseManager.enabled = false;
        playerController.StopFollowinTrack();
        lowBeatIndexes = null;
    }

    private IEnumerator LoadAudioAndStartGame(string songPath)
    {
        yield return StartCoroutine(AudioLoader.LoadAudio("file:\\\\" + songPath, audioSource));

        float[][] spectrum = AudioAnalyzer.GetAudioSpectrum(audioSource.clip, 4096);
        //lowBeatIndexes = BeatDetector.GetBeatIndexes(spectrum, 4096, audioSource.clip, 7500, 0.025f, 0.15f); //high
        lowBeatIndexes = BeatDetector.GetBeatIndexes(spectrum, 4096, audioSource.clip, 20, 0.1f, 0.25f); //low

        trackManager.GenerateTrack(audioSource.clip, 4096);
        trackSpline = trackManager.GetTrackSpline();

        selectFileUi.SetActive(false);
        playerController.SetNormalizedIntensities(trackManager.GetNormalizedIntensities());
        playerController.StartFollowingTrack(trackSpline);

        colorSyncher.enabled = true;
        pauseManager.enabled = true;
        playerController.gameObject.SetActive(true);
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
