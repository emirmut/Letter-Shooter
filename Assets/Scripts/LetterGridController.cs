using TMPro;
using UnityEngine;

public class LetterGridController : MonoBehaviour
{
    [SerializeField] Transform gridParent;          // GridLayoutGroup parent

    [SerializeField] GameObject redButtonPrefab;    // owner == 0
    [SerializeField] GameObject blueButtonPrefab;   // owner == 1
    [SerializeField] GameObject cloneObjects;

    public Animator[] revolverAnimators;

    public TextMeshProUGUI scoreTMPRO;
    public TextMeshProUGUI comboTMPRO;
    public TextMeshProUGUI currentTimeTextTMPRO;
    public TextMeshProUGUI mechanicTypeIndicator;
    public float gameDurationInMinutes;

    private TypingGameNetwork net;
    private LetterTileView[] tiles;
    private int lastSentenceIndex = -1, lastProgress = -1;

    // Called from TypingGameNetwork.Spawned()
    public void Bind(TypingGameNetwork n)
    {
        Debug.Log($"[LetterGridController] Bind() called with network: {(n != null ? "Valid" : "NULL")}");

        net = n;

        if (net != null)
        {
            Debug.Log($"[LetterGridController] Network bound. SentenceIndex: {net.SentenceIndex}, Progress: {net.Progress}");

            // Force a rebuild to ensure letters appear
            lastSentenceIndex = -1; // Reset to force rebuild
            lastProgress = -1;

            Rebuild();
        }
        else
        {
            Debug.LogError("[LetterGridController] Attempted to bind with null network!");
        }
    }

    void Start()
    {
        AudioManager.Instance.ChangeMusic(AudioManager.SoundType.Music_Gameplay);
    }

    void Update()
    {
        if (net == null)
        {
            // Try to find the network if we lost reference
            net = FindFirstObjectByType<TypingGameNetwork>();
            if (net != null)
            {
                Debug.Log("[LetterGridController] Reconnected to TypingGameNetwork in Update");
                lastSentenceIndex = -1; // Force rebuild
            }
            return;
        }

        if (net.SentenceIndex != lastSentenceIndex)
        {
            Debug.Log($"[LetterGridController] Sentence changed from {lastSentenceIndex} to {net.SentenceIndex}");
            Rebuild();
        }
        UpdateProgress();
    }

    void Rebuild()
    {
        Debug.Log($"[LetterGridController] Rebuilding grid for sentence {net.SentenceIndex}");

        // clear
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        var seq = SentenceData.Sentences[net.SentenceIndex]; // LetterUnit[]
        tiles = new LetterTileView[seq.Length];

        for (int i = 0; i < seq.Length; i++)
        {
            var prefab = (seq[i].owner == 0) ? redButtonPrefab : blueButtonPrefab;
            var go = Instantiate(prefab, gridParent);
            var view = go.GetComponent<LetterTileView>();        // TMP-only script
            if (!view) view = go.AddComponent<LetterTileView>();
            view.SetChar(seq[i].character);
            tiles[i] = view;
        }

        lastSentenceIndex = net.SentenceIndex;
        lastProgress = -1;
        UpdateProgress();

        Debug.Log($"[LetterGridController] Rebuild complete. Created {tiles.Length} tiles");
    }

    // Force rebuild method for ESC menu restart functionality
    public void ForceRebuild()
    {
        Debug.Log("[LetterGridController] Force rebuild requested");
        lastSentenceIndex = -1; // Reset to force rebuild
        lastProgress = -1;

        if (net != null)
        {
            Rebuild();
        }
    }

    void UpdateProgress()
    {
        if (tiles == null) return;
        int progress = Mathf.Clamp(net.Progress, 0, tiles.Length);
        if (progress == lastProgress) return;

        if (progress < tiles.Length)
        {
            tiles[progress].SetCurrentBliinkingAnim();
            Debug.Log($"[LetterGridController] Updated progress to {progress}, blinking tile {progress}");
        }
        lastProgress = progress;
    }

    public void UpdateFallingAnim(int letterIndex)
    {
        if (tiles == null) return;

        Debug.Log($"[LetterGridController] UpdateFallingAnim called with letterIndex: {letterIndex}, tiles length: {tiles.Length}");

        if (letterIndex >= 0 && letterIndex < tiles.Length)
        {
            Debug.Log($"[LetterGridController] Making tile {letterIndex} fall (character: '{net.CurrentSentence[letterIndex].character}')");

            tiles[letterIndex].ShadowOriginalObject();

            GameObject cloneTile = Instantiate(net.CurrentSentence[letterIndex].owner == 0 ? redButtonPrefab : blueButtonPrefab, tiles[letterIndex].rectTransform.anchoredPosition, Quaternion.identity, cloneObjects.transform);

            LetterTileView letterTileView = cloneTile.GetComponent<LetterTileView>();
            letterTileView.label = tiles[letterIndex].label;

            letterTileView.rectTransform.sizeDelta = tiles[letterIndex].rectTransform.sizeDelta;
            letterTileView.rectTransform.anchorMin = new Vector2(0f, 1f); // to anchor the game object to the top left
            letterTileView.rectTransform.anchorMax = new Vector2(0f, 1f); // to anchor the game object to the top left
            letterTileView.rectTransform.anchoredPosition = tiles[letterIndex].rectTransform.anchoredPosition;

            letterTileView.gameObject.layer = 6; // Clone layer

            letterTileView.SetCurrentFallingAnim();
        }
        else
        {
            Debug.LogWarning($"[LetterGridController] Invalid letterIndex: {letterIndex} (tiles length: {tiles.Length})");
        }
    }

    // Keep the old method for backward compatibility (just in case)
    public void UpdateFallingAnim()
    {
        UpdateFallingAnim(net.Progress);
    }
}