using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameObject pauseCanvas;

    private GameManager gameManager;
    private bool isPaused;

    void Awake()
    {
        gameManager = GameManager.GetGameManager();
    }

    void Start()
    {
        isPaused = false;
        pauseCanvas.SetActive(false);
    }

    void Update()
    {
        if (!gameManager.IsInTrackScene)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            TriggerPause();
    }

    /// <summary>
    /// Toggle the pause state of the game
    /// </summary>
    public void TriggerPause()
    {
        SetPaused(!isPaused);
        Utils.SetCursorVisibility(isPaused);
    }

    /// <summary>
    /// Restarts the song and unpauses the game
    /// </summary>
    public void RestartSong()
    {
        gameManager.RestartSong();
        SetPaused(false);
        Utils.SetCursorVisibility(false);
    }

    /// <summary>
    /// Stops the current audio reproduction, returns to the selects song menu and unpauses the game
    /// </summary>
    public void BackToSelectSongMenu()
    {
        SetPaused(false);
        Utils.SetCursorVisibility(true);
        audioSource.Stop();
        audioSource.clip = null;
        gameManager.BackToSelectMenu();
    }

    /// <summary>
    /// Closes the application
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }

    /// <summary>
    /// Sets the pause state of the game (pauses audio reproduction, timeScale and shows pause canvas)
    /// </summary>
    /// <param name="isPaused">The pause state of the game</param>
    private void SetPaused(bool isPaused)
    {
        this.isPaused = isPaused;

        if (isPaused)
            audioSource.Pause();
        else
            audioSource.UnPause();

        Time.timeScale = isPaused ? 0 : 1;
        pauseCanvas.SetActive(isPaused);
    }
}
