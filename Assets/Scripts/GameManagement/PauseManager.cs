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

    public void TriggerPause()
    {
        SetPaused(!isPaused);
        Utils.SetCursorVisibility(isPaused);
    }

    public void RestartSong()
    {
        gameManager.RestartSong();
        SetPaused(false);
        Utils.SetCursorVisibility(false);
    }

    public void BackToSelectSongMenu()
    {
        SetPaused(false);
        Utils.SetCursorVisibility(true);
        audioSource.Stop();
        audioSource.clip = null;
        gameManager.BackToSelectMenu();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

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
