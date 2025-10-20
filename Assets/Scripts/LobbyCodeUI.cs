using UnityEngine;
using TMPro;
using Fusion;
using System.Collections;

public class LobbyCodeUI : MonoBehaviour
{
    public TextMeshProUGUI lobbyCodeText;

    void Start()
    {
        StartCoroutine(WaitForNetworkRunner());
    }

    IEnumerator WaitForNetworkRunner()
    {
        NetworkRunner runner = null;

        // Wait much longer and be more specific about what we're waiting for
        int attempts = 0;
        while (attempts < 100) // Try for 10 seconds
        {
            runner = FindFirstObjectByType<NetworkRunner>();

            // Check if runner exists AND has proper session info AND is connected
            if (runner != null &&
                runner.SessionInfo != null &&
                !string.IsNullOrEmpty(runner.SessionInfo.Name) &&
                runner.SessionInfo.Name != "typing-room") // Not the default session
            {
                break;
            }

            attempts++;
            yield return new WaitForSeconds(0.1f);
        }

        if (lobbyCodeText != null && runner != null && runner.SessionInfo != null)
        {
            string lobbyCode = runner.SessionInfo.Name;
            lobbyCodeText.text = $"Lobby Code: {lobbyCode}";
            Debug.Log($"Lobby code displayed: {lobbyCode}");
        }
        else
        {
            // Fallback: try to get from PlayerPrefs
            string fallbackCode = PlayerPrefs.GetString("LobbySessionName", "Unknown");
            if (lobbyCodeText != null)
            {
                lobbyCodeText.text = $"Lobby Code: {fallbackCode}";
                Debug.Log($"Using fallback lobby code: {fallbackCode}");
            }
        }
    }
}