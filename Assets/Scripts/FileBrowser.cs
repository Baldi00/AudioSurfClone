using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class FileBrowser : MonoBehaviour
{
    [SerializeField]
    private GameObject buttonsContainer;
    [SerializeField]
    private GameObject buttonPrefab;
    [SerializeField]
    private GameObject searchBarPrefab;

    private string currentPath = "C:\\";
    private int distanceFromStartMenu;

    private UnityEvent<string> onAudioFileSelected;

    private TMP_InputField searchBarInputField;
    private Dictionary<string, string> foundMusicFiles;

    void Start()
    {
        foundMusicFiles = new Dictionary<string, string>();
        SearchMusicFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Music"));
        SetupStartButtons();
    }

    public void UpdateCurrentPath(string newCurrentPath)
    {
        distanceFromStartMenu++;

        if (newCurrentPath == "C:")
            newCurrentPath = "C:\\";

        currentPath = newCurrentPath;

        DeleteChildrenOfGameObject(buttonsContainer, true);

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

    private void DeleteChildrenOfGameObject(GameObject parent, bool dontDestroyFirst)
    {
        int childCount = parent.transform.childCount;

        for (int i = childCount - 1; i >= 1; i--)
        {
            GameObject child = parent.transform.GetChild(i).gameObject;
            Destroy(child);
        }

        if (!dontDestroyFirst && childCount > 0)
        {
            GameObject child = parent.transform.GetChild(0).gameObject;
            Destroy(child);
        }
    }

    private void SetupStartButtons()
    {
        distanceFromStartMenu = 0;

        DeleteChildrenOfGameObject(buttonsContainer, false);

        searchBarInputField = Instantiate(searchBarPrefab, buttonsContainer.transform).GetComponent<TMP_InputField>();
        searchBarInputField.onValueChanged.AddListener(OnSearchBarValueChange);

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

    private void SearchMusicFiles(string startingPath)
    {
        foreach (string dir in Directory.GetDirectories(startingPath))
            SearchMusicFiles(Path.Combine(startingPath, dir));

        foreach (string file in Directory.GetFiles(startingPath))
            if (file.EndsWith(".mp3") || file.EndsWith(".wav"))
                foundMusicFiles.Add(
                    file[(file.LastIndexOf("\\") + 1)..].Replace(".mp3", "").Replace(".wav", ""),
                    Path.Combine(startingPath, file));
    }

    private void OnSearchBarValueChange(string currentValue)
    {
        if (currentValue.Length <= 2)
            return;

        DeleteChildrenOfGameObject(buttonsContainer, true);

        SelectFileButton backDir = Instantiate(buttonPrefab, buttonsContainer.transform).GetComponent<SelectFileButton>();
        backDir.InitializeButton(
            SelectFileButton.SelectButtonType.DIRECTORY,
            "..Back",
            () => SetupStartButtons());

        foreach (string fileName in foundMusicFiles.Keys)
        {
            if (fileName.ToLower().Contains(currentValue.ToLower()))
            {
                SelectFileButton songFile = Instantiate(buttonPrefab, buttonsContainer.transform).GetComponent<SelectFileButton>();
                songFile.InitializeButton(
                    SelectFileButton.SelectButtonType.FILE,
                    fileName,
                    () => onAudioFileSelected.Invoke(foundMusicFiles[fileName]));
            }
        }
    }
}
