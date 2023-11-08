using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AudioLoader : MonoBehaviour
{
    public static IEnumerator LoadAudio(string songPath, AudioSource audioSource)
    {
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(songPath, AudioType.MPEG);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Failed to load MP3: " + request.error);
        }
        else
        {
            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
            audioSource.clip = audioClip;
        }
    }
}
