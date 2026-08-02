using UnityEngine;
using UnityEngine.UI;

public sealed class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    private bool paused;

    public void Configure(
        GameObject panel,
        Button pause,
        Button resume,
        Button restart,
        Button menu)
    {
        pausePanel = panel;
        pauseButton = pause;
        resumeButton = resume;
        restartButton = restart;
        menuButton = menu;
        Bind();
        SetPaused(false);
    }

    private void Awake()
    {
        Bind();
    }

    private void Bind()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(Pause);
            pauseButton.onClick.AddListener(Pause);
        }
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(Resume);
            resumeButton.onClick.AddListener(Resume);
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(Restart);
            restartButton.onClick.AddListener(Restart);
        }
        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(Menu);
            menuButton.onClick.AddListener(Menu);
        }
    }

    public void Pause()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            return;
        }
        SetPaused(true);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    private void Restart()
    {
        SetPaused(false);
        GameManager.Instance?.RestartGame();
    }

    private void Menu()
    {
        SetPaused(false);
        GameManager.Instance?.ReturnToMainMenu();
    }

    private void SetPaused(bool value)
    {
        paused = value;
        if (pausePanel != null)
        {
            pausePanel.SetActive(paused);
            if (paused)
            {
                pausePanel.transform.SetAsLastSibling();
            }
        }
        Time.timeScale = paused ? 0f : 1f;
    }
}
