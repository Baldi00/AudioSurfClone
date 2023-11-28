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
        {
            SetPaused(!isPaused);
            SetCursorVisibility(isPaused);
        }
    }

    public void RestartSong()
    {
        gameManager.RestartSong();
        SetPaused(false);
        SetCursorVisibility(false);
    }

    public void BackToSelectSongMenu()
    {
        SetPaused(false);
        SetCursorVisibility(true);
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

    private void SetCursorVisibility(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
