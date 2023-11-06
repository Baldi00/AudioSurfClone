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

    void Awake()
    {
        fileBrowser.AddOnAudioFileSelectedListener((songPath) => StartCoroutine(LoadAudioAndStartGame(songPath)));
        colorSyncher.enabled = false;
    }

    private IEnumerator LoadAudioAndStartGame(string songPath)
    {
        yield return StartCoroutine(AudioLoader.LoadAudio("file:\\\\" + songPath, audioSource));

        trackManager.GenerateTrack(audioSource.clip, 4096);
        trackSpline = trackManager.GetTrackSpline();

        selectFileUi.SetActive(false);
        GameObject player = Instantiate(playerPrefab);
        PlayerMover playerMover = player.GetComponent<PlayerMover>();
        PlayerColorSyncher playerColorSyncher = player.GetComponent<PlayerColorSyncher>();
        playerMover.StartFollowingTrack(trackSpline, audioSource);
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
