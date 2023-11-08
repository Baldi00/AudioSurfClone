using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class FileBrowser : MonoBehaviour
{
    [SerializeField]
    private GameObject buttonsContainer;
    [SerializeField]
    private GameObject buttonPrefab;

    private string currentPath = "C:\\";

    private UnityEvent<string> onAudioFileSelected;

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
            SelectFileButton backDir = Instantiate(buttonPrefab, buttonsContainer.transform).GetComponent<SelectFileButton>();
            backDir.SetButtonType(SelectFileButton.SelectButtonType.DIRECTORY);
            backDir.SetInnerText("..Back");
            backDir.AddListener(() => UpdateCurrentPath(currentPath[..currentPath.LastIndexOf("\\")]));
        }

        foreach (string dir in Directory.GetDirectories(currentPath))
        {
            SelectFileButton nextDir = Instantiate(buttonPrefab, buttonsContainer.transform).GetComponent<SelectFileButton>();
            nextDir.SetButtonType(SelectFileButton.SelectButtonType.DIRECTORY);
            nextDir.SetInnerText(dir[(dir.LastIndexOf("\\") + 1)..]);
            nextDir.AddListener(() => UpdateCurrentPath(Path.Combine(currentPath, dir)));
        }
        foreach (string file in Directory.GetFiles(currentPath))
        {
            if (file.EndsWith(".mp3") || file.EndsWith(".wav"))
            {
                SelectFileButton nextFile = Instantiate(buttonPrefab, buttonsContainer.transform).GetComponent<SelectFileButton>();
                nextFile.SetButtonType(SelectFileButton.SelectButtonType.FILE);
                nextFile.SetInnerText(file[(file.LastIndexOf("\\") + 1)..].Replace(".mp3", "").Replace(".wav", ""));
                nextFile.AddListener(() => onAudioFileSelected.Invoke(Path.Combine(currentPath, file)));
            }
        }
    }

    public void AddOnAudioFileSelectedListener(UnityAction<string> listener)
    {
        onAudioFileSelected ??= new UnityEvent<string>();
        onAudioFileSelected.AddListener(listener);
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
}
