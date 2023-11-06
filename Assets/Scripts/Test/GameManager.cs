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

        selectFileUi.SetActive(false);
        PlayerMover playerMover = Instantiate(playerPrefab).GetComponent<PlayerMover>();
        playerMover.StartFollowingTrack(trackManager.GetTrackSpline(), audioSource);

        audioSource.Play();
    }
}
