using Fusion;
using UnityEngine;

public class NetBootstrap : MonoBehaviour
{
    [SerializeField] 
    private NetworkObject typingGamePrefab;

    async void Start()
    {
        // Check if we have lobby info from MainMenu
        string sessionName = PlayerPrefs.GetString("LobbySessionName", "");
        string gameMode = PlayerPrefs.GetString("GameMode", "");

        if (string.IsNullOrEmpty(sessionName) || string.IsNullOrEmpty(gameMode))
        {
            Debug.LogWarning("[Bootstrap] No lobby info found, using default session");
            sessionName = "typing-room";
            gameMode = "AutoHostOrClient";
        }

        var runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        Debug.Log($"[Bootstrap] Starting Fusion with session: {sessionName}, mode: {gameMode}");

        GameMode fusionGameMode = gameMode switch
        {
            "Host" => GameMode.Host,
            "Client" => GameMode.Client,
            _ => GameMode.AutoHostOrClient
        };

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = fusionGameMode,
            SessionName = sessionName,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (!result.Ok)
        {
            Debug.LogError("[Bootstrap] StartGame failed: " + result.ShutdownReason);
            return;
        }

        // Host spawns the authoritative object; clients will receive it and then Spawned() runs
        if (runner.IsServer)
        {
            runner.Spawn(typingGamePrefab, Vector3.zero, Quaternion.identity);
            Debug.Log("[Bootstrap] Spawned TypingGame prefab.");
        }

        // Clear the lobby info after use
        PlayerPrefs.DeleteKey("LobbySessionName");
        PlayerPrefs.DeleteKey("GameMode");
    }
}