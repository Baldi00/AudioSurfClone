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
    private int distanceFromStartMenu;

    private UnityEvent<string> onAudioFileSelected;

    void Start()
    {
        SetupStartButtons();
    }

    public void UpdateCurrentPath(string newCurrentPath)
    {
        distanceFromStartMenu++;

        if (newCurrentPath == "C:")
            newCurrentPath = "C:\\";

        currentPath = newCurrentPath;

        DeleteChildrenOfGameObject(buttonsContainer);

        UnityAction backActionCall;
        if (distanceFromStartMenu <= 1)
            backActionCall = () => SetupStartButtons();
        else
            backActionCall = () =>
            {
                distanceFromStartMenu -= 2;
                UpdateCurrentPath(currentPath[..currentPath.LastIndexOf("\\")]);
            };

        SelectFileButton backDir = Instantiate(buttonPrefab, buttonsContainer.transform).GetComponent<SelectFileButton>();
        backDir.InitializeButton(
            SelectFileButton.SelectButtonType.DIRECTORY,
            "..Back",
            backActionCall);

        foreach (string dir in Directory.GetDirectories(currentPath))
        {
            SelectFileButton nextDir = Instantiate(buttonPrefab, buttonsContainer.transform).GetComponent<SelectFileButton>();
            nextDir.InitializeButton(
                SelectFileButton.SelectButtonType.DIRECTORY,
                dir[(dir.LastIndexOf("\\") + 1)..],
                () => UpdateCurrentPath(Path.Combine(currentPath, dir)));
        }
        foreach (string file in Directory.GetFiles(currentPath))
        {
            if (file.EndsWith(".mp3") || file.EndsWith(".wav"))
            {
                SelectFileButton nextFile = Instantiate(buttonPrefab, buttonsContainer.transform).GetComponent<SelectFileButton>();
                nextFile.InitializeButton(
                    SelectFileButton.SelectButtonType.FILE,
                    file[(file.LastIndexOf("\\") + 1)..].Replace(".mp3", "").Replace(".wav", ""),
                    () => onAudioFileSelected.Invoke(Path.Combine(currentPath, file)));
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

    private void SetupStartButtons()
    {
        distanceFromStartMenu = 0;

        DeleteChildrenOfGameObject(buttonsContainer);

        SelectFileButton musicFolder = Instantiate(buttonPrefab, buttonsContainer.transform).GetComponent<SelectFileButton>();
        musicFolder.InitializeButton(
            SelectFileButton.SelectButtonType.DIRECTORY,
            "Music",
            () => UpdateCurrentPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Music")));

        SelectFileButton desktopFolder = Instantiate(buttonPrefab, buttonsContainer.transform).GetComponent<SelectFileButton>();
        desktopFolder.InitializeButton(
            SelectFileButton.SelectButtonType.DIRECTORY,
            "Desktop",
            () => UpdateCurrentPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop")));

        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            SelectFileButton currentDriveButton = Instantiate(buttonPrefab, buttonsContainer.transform).GetComponent<SelectFileButton>();
            currentDriveButton.InitializeButton(
                SelectFileButton.SelectButtonType.DIRECTORY,
                drive.Name,
                () => UpdateCurrentPath(drive.RootDirectory.FullName));
        }
    }
}
