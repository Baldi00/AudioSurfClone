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
    private List<OnTrackMover> onTrackMovers;

    [SerializeField]
    private GameObject selectFileUi;
    [SerializeField]
    private GameObject gameScene;

    void Awake()
    {
        fileBrowser.AddOnAudioFileSelectedListener((songPath) => StartCoroutine(LoadAudioAndStartGame(songPath)));
    }

    public AudioSource GetAudioSource()
    {
        return audioSource;
    }

    private IEnumerator LoadAudioAndStartGame(string songPath)
    {
        yield return StartCoroutine(AudioLoader.LoadAudio("file:\\\\" + songPath, audioSource));
        
        trackManager.GenerateTrack(audioSource.clip, 4096);
        onTrackMovers.ForEach(mover => mover.StartFollowingTrack(trackManager.GetTrackSpline(), audioSource));

        selectFileUi.SetActive(false);
        gameScene.SetActive(true);
        
        audioSource.Play();
    }
}
