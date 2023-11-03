using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class FileBrowser : MonoBehaviour
{
    [SerializeField]
    private GameObject buttonsContainer;
    [SerializeField]
    private GameObject buttonPrefab;
    [SerializeField]
    private AudioLoader audioLoader;
    [SerializeField]
    private GameObject gameScene;
    [SerializeField]
    private GameObject selectUiScene;

    private string currentPath = "C:\\";

    void Start()
    {
        UpdateCurrentPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Music"));
    }

    public void UpdateCurrentPath(string newCurrentPath)
    {
        if (newCurrentPath == "C:")
            newCurrentPath = "C:\\";

        currentPath = newCurrentPath;

        DeleteChildrenOfGameObject(buttonsContainer);

        if (currentPath != "C:\\")
        {
            GameObject backDir = Instantiate(buttonPrefab, buttonsContainer.transform);
            backDir.GetComponent<Button>().onClick.AddListener(() => UpdateCurrentPath(currentPath[..currentPath.LastIndexOf("\\")]));
            backDir.GetComponentInChildren<Text>().text = "..";
        }

        foreach (string dir in Directory.GetDirectories(currentPath))
        {
            GameObject nextDir = Instantiate(buttonPrefab, buttonsContainer.transform);
            nextDir.GetComponent<Button>().onClick.AddListener(() => UpdateCurrentPath(Path.Combine(currentPath, dir)));
            nextDir.GetComponentInChildren<Text>().text = dir.Substring(dir.LastIndexOf("\\") + 1);
        }
        foreach (string file in Directory.GetFiles(currentPath))
        {
            if (file.EndsWith(".mp3") || file.EndsWith(".wav"))
            {
                GameObject nextFile = Instantiate(buttonPrefab, buttonsContainer.transform);
                nextFile.GetComponent<Button>().onClick.AddListener(() => StartCoroutine(LoadAudioAndStartGame(Path.Combine(currentPath, file))));
                nextFile.GetComponentInChildren<Text>().text = file.Substring(file.LastIndexOf("\\") + 1);
            }
        }
    }

    private void DeleteChildrenOfGameObject(GameObject parent)
    {
        int childCount = parent.transform.childCount;

        for (int i = childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.transform.GetChild(i).gameObject;
            Destroy(child);
        }
    }

    private IEnumerator LoadAudioAndStartGame(string songPath)
    {
        yield return StartCoroutine(audioLoader.LoadAudio("file:\\\\" + songPath));
        selectUiScene.SetActive(false);
        gameScene.SetActive(true);
    }
}
