using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    private const string SoundPreferenceKey = "BalloonDog.SoundEnabled";
    private const string VibrationPreferenceKey = "BalloonDog.VibrationEnabled";

    public static bool StartImmediatelyOnNextSceneLoad { get; set; }
    public static bool VibrationEnabled { get; private set; } = true;

    private GameObject mainMenuPanel;
    private GameObject settingsPanel;
    private PlayerRunner runner;
    private PlayerHorizontalController horizontalController;
    private PlayerFormController formController;
    private BalloonSizeController sizeController;
    private AirController airController;
    private Rigidbody playerBody;

    private TMP_Text soundButtonLabel;
    private TMP_Text vibrationButtonLabel;
    private bool soundEnabled;
    private StartCountdownController countdownController;

    private void Awake()
    {
        Time.timeScale = 0f;
        ResolveReferences();
        BindButtons();
        LoadPreferences();
    }

    private void Start()
    {
        if (StartImmediatelyOnNextSceneLoad)
        {
            StartImmediatelyOnNextSceneLoad = false;
            StartGame();
            return;
        }

        ShowMainMenu();
    }

    public void StartGame()
    {
        airController?.ResetToFull();
        sizeController?.SnapToCurrentAir();

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        SetGameplayUiVisible(true);
        SetPlayerControlsEnabled(false);

        countdownController ??= FindAnyObjectByType<StartCountdownController>();
        if (countdownController != null)
        {
            countdownController.Begin(() =>
            {
                SetPlayerControlsEnabled(true);
                Time.timeScale = 1f;
            });
        }
        else
        {
            SetPlayerControlsEnabled(true);
            Time.timeScale = 1f;
        }
    }

    public void ShowMainMenu()
    {
        Time.timeScale = 0f;
        SetPlayerControlsEnabled(false);
        SetGameplayUiVisible(false);

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.transform.SetAsLastSibling();
            mainMenuPanel.SetActive(true);
        }
    }

    public void ShowSettings()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.transform.SetAsLastSibling();
            settingsPanel.SetActive(true);
        }
    }

    public void ToggleSound()
    {
        soundEnabled = !soundEnabled;
        ApplySoundPreference();
        PlayerPrefs.SetInt(SoundPreferenceKey, soundEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleVibration()
    {
        VibrationEnabled = !VibrationEnabled;
        PlayerPrefs.SetInt(VibrationPreferenceKey, VibrationEnabled ? 1 : 0);
        PlayerPrefs.Save();
        RefreshPreferenceLabels();
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ResolveReferences()
    {
        mainMenuPanel = FindNamedObject("MainMenuPanel");
        settingsPanel = FindNamedObject("SettingsPanel");

        runner = FindAnyObjectByType<PlayerRunner>();
        horizontalController = FindAnyObjectByType<PlayerHorizontalController>();
        formController = FindAnyObjectByType<PlayerFormController>();
        sizeController = FindAnyObjectByType<BalloonSizeController>();
        airController = FindAnyObjectByType<AirController>();
        playerBody = runner != null ? runner.GetComponent<Rigidbody>() : null;
        countdownController = FindAnyObjectByType<StartCountdownController>();

        soundButtonLabel = FindNamedComponent<TMP_Text>("SoundButtonLabel");
        vibrationButtonLabel = FindNamedComponent<TMP_Text>("VibrationButtonLabel");
    }

    private void BindButtons()
    {
        BindButton("PlayButton", StartGame);
        BindButton("SettingsButton", ShowSettings);
        BindButton("SettingsBackButton", ShowMainMenu);
        BindButton("SoundToggleButton", ToggleSound);
        BindButton("VibrationToggleButton", ToggleVibration);
        BindButton("ExitButton", ExitGame);

        GameManager manager = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
        if (manager != null)
        {
            BindButton("RestartButton", manager.RestartGame);
            BindButton("EndMenuButton", manager.ReturnToMainMenu);
        }
    }

    private static void BindButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        Button button = FindNamedComponent<Button>(objectName);
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void LoadPreferences()
    {
        soundEnabled = PlayerPrefs.GetInt(SoundPreferenceKey, 1) == 1;
        VibrationEnabled = PlayerPrefs.GetInt(VibrationPreferenceKey, 1) == 1;
        ApplySoundPreference();
    }

    private void ApplySoundPreference()
    {
        AudioListener.volume = soundEnabled ? 1f : 0f;
        RefreshPreferenceLabels();
    }

    private void RefreshPreferenceLabels()
    {
        if (soundButtonLabel != null)
        {
            soundButtonLabel.text = soundEnabled ? "SES: AÇIK" : "SES: KAPALI";
        }

        if (vibrationButtonLabel != null)
        {
            vibrationButtonLabel.text = VibrationEnabled
                ? "TİTREŞİM: AÇIK"
                : "TİTREŞİM: KAPALI";
        }
    }

    private void SetPlayerControlsEnabled(bool enabled)
    {
        if (runner != null)
        {
            runner.SetMovementEnabled(enabled);
        }

        if (horizontalController != null)
        {
            horizontalController.enabled = enabled;
        }

        if (formController != null)
        {
            if (!enabled)
            {
                formController.ForceBalloonForm();
            }

            formController.enabled = enabled;
        }

        if (sizeController != null)
        {
            sizeController.SetShrinkRequested(false);
            sizeController.enabled = enabled;
        }

        if (!enabled && playerBody != null)
        {
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
        }
    }

    private static void SetGameplayUiVisible(bool visible)
    {
        string[] names =
        {
            "SpeedText",
            "AirSlider",
            "ScoreText",
            "ControlHint",
            "ShrinkButton",
            "ProgressSlider",
            "V5GameplayHud"
        };

        foreach (string objectName in names)
        {
            GameObject gameObject = FindNamedObject(objectName);
            if (gameObject != null)
            {
                gameObject.SetActive(visible);
            }
        }
    }

    private static GameObject FindNamedObject(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform candidate in transforms)
        {
            if (candidate == null || candidate.name != objectName)
            {
                continue;
            }

            if (!candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            return candidate.gameObject;
        }

        return null;
    }

    private static T FindNamedComponent<T>(string objectName) where T : Component
    {
        GameObject gameObject = FindNamedObject(objectName);
        return gameObject != null ? gameObject.GetComponent<T>() : null;
    }
}
