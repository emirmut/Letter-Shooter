using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Fusion;

public class EscMenuManager : MonoBehaviour
{
    [Header("Menu UI")]
    public GameObject escMenuPanel;
    public Button restartSentenceButton;
    public Button skipSentenceButton;
    public Button backToMainMenuButton;

    [Header("Game References")]
    private TypingGameNetwork gameNetwork;
    private bool isMenuOpen = false;

    void Start()
    {
        // Hide menu initially
        if (escMenuPanel != null)
            escMenuPanel.SetActive(false);

        // Setup button listeners
        if (restartSentenceButton != null)
            restartSentenceButton.onClick.AddListener(RestartCurrentSentence);

        if (skipSentenceButton != null)
            skipSentenceButton.onClick.AddListener(SkipToNextSentence);

        if (backToMainMenuButton != null)
            backToMainMenuButton.onClick.AddListener(BackToMainMenu);

        // Try to find the game network, but don't rely on it being available immediately
        TryFindGameNetwork();
    }

    void Update()
    {
        // Toggle menu with ESC key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }

        // Continuously try to find the network if we don't have it yet
        if (gameNetwork == null)
        {
            TryFindGameNetwork();
        }
    }

    private void TryFindGameNetwork()
    {
        if (gameNetwork == null)
        {
            gameNetwork = FindFirstObjectByType<TypingGameNetwork>();
            if (gameNetwork != null)
            {
                Debug.Log("[EscMenu] Found TypingGameNetwork!");
            }
        }
    }


    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        if (escMenuPanel != null)
            escMenuPanel.SetActive(isMenuOpen);

        // Just show/hide the menu, don't pause the game
        Debug.Log($"[EscMenu] Menu {(isMenuOpen ? "opened" : "closed")}");
    }

    public void RestartCurrentSentence()
    {
        Debug.Log("[EscMenu] Restarting current sentence");

        // Try one more time to find the network
        if (gameNetwork == null)
        {
            TryFindGameNetwork();
        }

        if (gameNetwork != null)
        {
            Debug.Log($"[EscMenu] gameNetwork found. Object: {gameNetwork.Object}, HasStateAuthority: {gameNetwork.Object?.HasStateAuthority}");

            // Call the restart method on the network - let the RPC handle authority
            gameNetwork.RPC_RestartCurrentSentence();
            Debug.Log("[EscMenu] RPC_RestartCurrentSentence called");
        }
        else
        {
            Debug.LogError("[EscMenu] gameNetwork is still null! TypingGameNetwork might not be spawned yet.");
        }

        // Close the menu
        ToggleMenu();
    }

    public void SkipToNextSentence()
    {
        Debug.Log("[EscMenu] Skipping to next sentence");

        // Try one more time to find the network
        if (gameNetwork == null)
        {
            TryFindGameNetwork();
        }

        if (gameNetwork != null)
        {
            Debug.Log($"[EscMenu] gameNetwork found. Object: {gameNetwork.Object}, HasStateAuthority: {gameNetwork.Object?.HasStateAuthority}");

            // Call the skip method on the network - let the RPC handle authority
            gameNetwork.RPC_SkipToNextSentence();
            Debug.Log("[EscMenu] RPC_SkipToNextSentence called");
        }
        else
        {
            Debug.LogError("[EscMenu] gameNetwork is still null! TypingGameNetwork might not be spawned yet.");
        }

        // Close the menu
        ToggleMenu();
    }
    public void BackToMainMenu()
    {
        Debug.Log("[EscMenu] Going back to main menu");

        // Resume time before scene change
        Time.timeScale = 1f;

        // Shutdown network if it exists
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null)
        {
            runner.Shutdown();
        }

        // Load main menu scene
        SceneManager.LoadScene("MainMenu"); // Adjust scene name if different
    }

    void OnDestroy()
    {
        // Nothing to clean up since we're not pausing the game
    }
}