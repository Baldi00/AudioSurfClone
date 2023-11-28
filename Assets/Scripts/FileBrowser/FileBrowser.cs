using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class FileBrowser : MonoBehaviour
{
    [SerializeField] private GameObject buttonsContainer;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private TMP_InputField searchBarInputField;

    private string currentPath;
    private int distanceFromStartMenu;

    private UnityEvent<string> onAudioFileSelected;

    private Dictionary<string, string> foundMusicFiles;

    void Start()
    {
        foundMusicFiles = new Dictionary<string, string>();
        SearchMusicFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));

        searchBarInputField.onValueChanged.AddListener(OnSearchBarValueChange);
        SetupStartButtons();
    }

    /// <summary>
    /// Adds a listener to the audio file selected event
    /// </summary>
    /// <param name="listener">The listener to the audio file selected event</param>
    public void AddOnAudioFileSelectedListener(UnityAction<string> listener)
    {
        onAudioFileSelected ??= new UnityEvent<string>();
        onAudioFileSelected.AddListener(listener);
    }

    /// <summary>
    /// Recursivly searches for audio files starting from the given directory path
    /// </summary>
    /// <param name="startingPath"></param>
    private void SearchMusicFiles(string startingPath)
    {
        if (!Directory.Exists(startingPath))
            return;

        // Recursive call to sub directories
        foreach (string dirPath in Directory.GetDirectories(startingPath))
            SearchMusicFiles(dirPath);

        // Search for audio files
        foreach (string filePath in Directory.GetFiles(startingPath))
            if (Utils.IsAudioFile(filePath))
                foundMusicFiles.Add(Utils.GetNameFromPath(filePath), filePath);
    }

    /// <summary>
    /// Called when the value of the search bar changes.
    /// If the length of the current value is greater than a certain amout sets up the UI with found audio files
    /// that contains the current search bar value.
    /// </summary>
    /// <param name="currentValue">The current value of the search bar</param>
    private void OnSearchBarValueChange(string currentValue)
    {
        if (currentValue.Length <= 2)
            return;

        DeleteChildren(buttonsContainer);

        CreateButton("..Back", SelectButtonType.DIRECTORY, () => SetupStartButtons());

        foreach (string fileName in foundMusicFiles.Keys)
            if (fileName.ToLower().Contains(currentValue.ToLower()))
                CreateButton(foundMusicFiles[fileName], SelectButtonType.FILE);
    }

    /// <summary>
    /// Resets the file browser to the start menu.
    /// I.e. Resets the search bar, remove all buttons, then creates the music, desktop and drives directory buttons
    /// </summary>
    private void SetupStartButtons()
    {
        distanceFromStartMenu = 0;

        searchBarInputField.text = "";

        DeleteChildren(buttonsContainer);
        CreateButton(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), SelectButtonType.DIRECTORY);
        CreateButton(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), SelectButtonType.DIRECTORY);

        foreach (DriveInfo drive in DriveInfo.GetDrives())
            CreateButton(drive.RootDirectory.FullName, SelectButtonType.DIRECTORY);
    }

    /// <summary>
    /// Creates a button of the given type leading to the given path.
    /// In case of directory, clicking it will lead to the content of that directory,
    /// in case of an audio file, clicking it will start the game with that audio
    /// </summary>
    /// <param name="path">The path the button will lead you to</param>
    /// <param name="type">The type of the button</param>
    private void CreateButton(string path, SelectButtonType type)
    {
        UnityAction callback =
            type == SelectButtonType.FILE ?
            () => onAudioFileSelected.Invoke(path) :
            () => UpdateCurrentPathAndRefreshUi(path);

        string name = Utils.GetNameFromPath(path);

        CreateButton(name, type, callback);
    }

    /// <summary>
    /// Creates a button with the given name, of the given type that will call the given callback when clicked.
    /// </summary>
    /// <param name="name">The name of the button</param>
    /// <param name="type">The type of the button</param>
    /// <param name="callback">The callback that will be called when clicking the button</param>
    private void CreateButton(string name, SelectButtonType type, UnityAction callback)
    {
        SelectFileButton button =
            Instantiate(buttonPrefab, buttonsContainer.transform).GetComponent<SelectFileButton>();

        button.InitializeButton(type, name, callback);
    }

    /// <summary>
    /// Updates the current path of the file browser and refreshes the ui
    /// </summary>
    /// <param name="newCurrentPath">The current path the file browser will have after the refresh</param>
    private void UpdateCurrentPathAndRefreshUi(string newCurrentPath)
    {
        if (!Directory.Exists(newCurrentPath))
            return;

        // Update distance from start menu and current file path
        distanceFromStartMenu++;

        if (Regex.Match(newCurrentPath, "^[A-Z]:$").Success)
            newCurrentPath += Path.DirectorySeparatorChar;

        currentPath = newCurrentPath;

        // Refresh UI
        DeleteChildren(buttonsContainer);
        CreateBackButtonFor(currentPath);
        CreateDirectoryButtonsUnder(currentPath);
        CreateAudioFilesButtonsUnder(currentPath);
    }

    /// <summary>
    /// Creates the back button for the given path.
    /// The back button will lead to the previous folder or to the initial menu if there is no previous folder
    /// </summary>
    /// <param name="path">The path for which the back button will be created</param>
    private void CreateBackButtonFor(string path)
    {
        UnityAction backActionCallback;
        if (distanceFromStartMenu <= 1)
            backActionCallback = () => SetupStartButtons();
        else
            backActionCallback = () =>
            {
                distanceFromStartMenu -= 2;
                UpdateCurrentPathAndRefreshUi(path[..path.LastIndexOf(Path.DirectorySeparatorChar)]);
            };

        CreateButton("..Back", SelectButtonType.DIRECTORY, backActionCallback);
    }

    /// <summary>
    /// Creates the sub directory buttons for the given path
    /// </summary>
    /// <param name="path">The root path under which the directory buttons will be created</param>
    private void CreateDirectoryButtonsUnder(string path)
    {
        foreach (string dirPath in Directory.GetDirectories(path))
            CreateButton(dirPath, SelectButtonType.DIRECTORY);
    }

    /// <summary>
    /// Creates the audio file buttons under the directory at the given
    /// </summary>
    /// <param name="path">The root path under which the audio files buttons will be created</param>
    private void CreateAudioFilesButtonsUnder(string path)
    {
        foreach (string filePath in Directory.GetFiles(path))
            if (Utils.IsAudioFile(filePath))
                CreateButton(filePath, SelectButtonType.FILE);
    }

    /// <summary>
    /// Deletes all the children of the given gameObject
    /// </summary>
    /// <param name="parent">The gameObject you want to delete its children</param>
    private void DeleteChildren(GameObject parent)
    {
        int childCount = parent.transform.childCount;

        for (int i = childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.transform.GetChild(i).gameObject;
            Destroy(child);
        }
    }
}
