using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Main Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;
    public GameObject statsPanel;
    public GameObject joinLobbyPanel;

    [Header("UI Elements")]
    public TMP_InputField lobbyCodeInput;

    public Button joinLobbyButton;
    public Button createLobbyButton;

    [Header("Network")]
    public NetworkRunner networkRunnerPrefab;

    [Header("Scene Management")]
    public string gameSceneName = "GameScene"; // Your actual game scene name

    private NetworkRunner _runner;
    private string _currentSessionName;

    private void Start()
    {
        AudioManager.Instance.Play(AudioManager.SoundType.Music_Menu);
        ShowMainMenu();

        // Add listeners to buttons
        if (joinLobbyButton != null)
            joinLobbyButton.onClick.AddListener(OnJoinLobby);

        if (createLobbyButton != null)
            createLobbyButton.onClick.AddListener(OnCreateLobby);
    }

    #region Menu Navigation
    public void ShowMainMenu()
    {
        SetActivePanel(mainMenuPanel);
    }

    public void ShowOptions()
    {
        SetActivePanel(optionsPanel);
    }

    public void ShowStats()
    {
        SetActivePanel(statsPanel);
    }


    public void ShowJoinLobby()
    {
        SetActivePanel(joinLobbyPanel);
        if (lobbyCodeInput != null)
            lobbyCodeInput.text = "";
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void SetActivePanel(GameObject activePanel)
    {
        mainMenuPanel.SetActive(activePanel == mainMenuPanel);
        optionsPanel.SetActive(activePanel == optionsPanel);
        statsPanel.SetActive(activePanel == statsPanel);
        joinLobbyPanel.SetActive(activePanel == joinLobbyPanel);
    }
    #endregion

    #region Networking
    public void OnCreateLobby()
    {
        // Generate 6-digit session name
        _currentSessionName = GenerateSessionCode();


        // Store lobby info for NetBootstrap to use
        PlayerPrefs.SetString("LobbySessionName", _currentSessionName);
        PlayerPrefs.SetString("GameMode", "Host");

        Debug.Log($"Lobby will be created with code: {_currentSessionName}");

        // Load game scene - NetBootstrap will handle networking
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
        Destroy(AudioManager.Instance.musicSource.gameObject);
    }

    public void OnJoinLobby()
    {
        if (lobbyCodeInput == null || string.IsNullOrEmpty(lobbyCodeInput.text))
        {
            Debug.LogWarning("Please enter a lobby code");
            return;
        }

        string sessionName = lobbyCodeInput.text.ToUpper();

        if (sessionName.Length != 6)
        {
            Debug.LogWarning("Lobby code must be 6 characters");
            return;
        }

        // Store lobby info for NetBootstrap to use
        PlayerPrefs.SetString("LobbySessionName", sessionName);
        PlayerPrefs.SetString("GameMode", "Client");

        Debug.Log($"Will join lobby: {sessionName}");

        // Load game scene - NetBootstrap will handle networking
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
        Destroy(AudioManager.Instance.musicSource.gameObject);
    }
    private string GenerateSessionCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        string result = "";
        for (int i = 0; i < 6; i++)
        {
            result += chars[Random.Range(0, chars.Length)];
        }
        return result;
    }

    private int GetSceneIndex(string sceneName)
    {
        // Find the scene index in build settings
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (name == sceneName)
            {
                return i;
            }
        }

        Debug.LogError($"Scene '{sceneName}' not found in build settings!");
        return 0; // Return main menu scene as fallback
    }
    #endregion

    private void OnDestroy()
    {
        if (_runner != null)
        {
            _runner.Shutdown();
        }
    }
}