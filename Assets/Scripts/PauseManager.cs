using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource audioSource;
    [SerializeField]
    private GameObject pauseCanvas;

    private bool isPaused;

    void Start()
    {
        isPaused = false;
        pauseCanvas.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SetPaused(!isPaused);
    }

    public void RestartSong()
    {
        audioSource.Stop();
        audioSource.Play();
        SetPaused(false);
    }

    public void BackToSelectSongMenu()
    {
        SetPaused(false);
        audioSource.Stop();
        audioSource.clip = null;
        GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().BackToSelectMenu();
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
