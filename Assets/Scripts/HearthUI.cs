using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class HeartUI : MonoBehaviour
{
    [Header("Heart UI Setup")]
    [SerializeField] private Transform heartContainer; // Parent object containing all hearts
    [SerializeField] private GameObject heartPrefab; // Heart sprite prefab
    [SerializeField] private Transform fallingContainer; // Container for falling hearts (different layer)

    [Header("Animation Settings")]
    [SerializeField] private float heartFallDuration = 1f;
    [SerializeField] private float heartFallDistance = 200f;
    [SerializeField] private float heartSpacing = 60f; // Distance between hearts

    private List<Image> hearts = new List<Image>(); // Active hearts (right to left)
    private Image regeneratingHeart; // The heart that's currently regenerating
    private TypingGameNetwork gameNetwork;

    private int lastKnownHeartCount = 5;
    private float lastKnownRegenProgress = 0f;
    private bool wasTimerReset = false;

    void Start()
    {
        // Initialize hearts immediately
        InitializeHearts();

        // Start looking for the game network
        StartCoroutine(WaitForGameNetwork());
    }

    System.Collections.IEnumerator WaitForGameNetwork()
    {
        Debug.Log("[HeartUI] Waiting for TypingGameNetwork to spawn...");

        // Wait up to 10 seconds for the network to be ready
        float timeout = 10f;
        float timer = 0f;

        while (gameNetwork == null && timer < timeout)
        {
            gameNetwork = FindFirstObjectByType<TypingGameNetwork>();

            if (gameNetwork != null)
            {
                Debug.Log("[HeartUI] Found TypingGameNetwork! Initializing heart state...");
                lastKnownHeartCount = gameNetwork.GetCurrentHearts();
                UpdateVisibleHearts(lastKnownHeartCount);
                yield break;
            }

            timer += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (gameNetwork == null)
        {
            Debug.LogError("[HeartUI] Failed to find TypingGameNetwork after 10 seconds!");
        }
    }

    void Update()
    {
        if (gameNetwork == null)
        {
            Debug.LogWarning("[HeartUI] GameNetwork is null!");
            return;
        }

        int currentHearts = gameNetwork.GetCurrentHearts();
        float regenProgress = gameNetwork.GetHeartRegenProgress();
        bool isTimerReset = gameNetwork.WasHeartTimerReset();

        // Debug logging
        if (currentHearts != lastKnownHeartCount)
        {
            Debug.Log($"[HeartUI] Heart count changed: {lastKnownHeartCount} -> {currentHearts}");
        }

        // Check for heart loss
        if (currentHearts < lastKnownHeartCount)
        {
            int heartsLost = lastKnownHeartCount - currentHearts;
            Debug.Log($"[HeartUI] Hearts lost: {heartsLost}, making hearts fall");
            for (int i = 0; i < heartsLost; i++)
            {
                MakeHeartFall();
            }
        }

        // Check for heart gain
        if (currentHearts > lastKnownHeartCount)
        {
            int heartsGained = currentHearts - lastKnownHeartCount;
            Debug.Log($"[HeartUI] Hearts gained: {heartsGained}, restoring hearts");
            for (int i = 0; i < heartsGained; i++)
            {
                RestoreHeart();
            }
        }

        // Check for timer reset (typo occurred)
        if (isTimerReset && !wasTimerReset)
        {
            Debug.Log("[HeartUI] Timer reset detected, resetting regenerating heart");
            ResetRegeneratingHeart();
        }

        // Update regenerating heart alpha
        UpdateRegeneratingHeart(currentHearts, regenProgress);

        lastKnownHeartCount = currentHearts;
        lastKnownRegenProgress = regenProgress;
        wasTimerReset = isTimerReset;
    }

    void InitializeHearts()
    {
        Debug.Log("[HeartUI] Initializing hearts...");

        if (heartContainer == null)
        {
            Debug.LogError("[HeartUI] HeartContainer is null! Please assign it in the inspector.");
            return;
        }

        if (heartPrefab == null)
        {
            Debug.LogError("[HeartUI] HeartPrefab is null! Please assign it in the inspector.");
            return;
        }

        // Clear existing hearts
        foreach (Transform child in heartContainer)
        {
            Destroy(child.gameObject);
        }
        hearts.Clear();

        // Create 5 hearts from left to right (index 0 = leftmost, index 4 = rightmost)
        for (int i = 0; i < 5; i++)
        {
            GameObject heartGO = Instantiate(heartPrefab, heartContainer);
            Image heartImage = heartGO.GetComponent<Image>();

            if (heartImage == null)
            {
                Debug.LogError($"[HeartUI] Heart prefab doesn't have Image component! Heart {i} will not work properly.");
                continue;
            }

            // Position hearts (left to right)
            RectTransform rectTransform = heartGO.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(i * heartSpacing, 0);

            hearts.Add(heartImage);
            Debug.Log($"[HeartUI] Created heart {i} at position {rectTransform.anchoredPosition}");
        }

        Debug.Log($"[HeartUI] Created {hearts.Count} hearts successfully");

        // Create regenerating heart (initially hidden)
        CreateRegeneratingHeart();
    }

    void UpdateVisibleHearts(int heartCount)
    {
        // Update the visibility of hearts based on current count
        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] != null)
            {
                Color color = hearts[i].color;
                color.a = i < heartCount ? 1f : 0f;
                hearts[i].color = color;
            }
        }
    }

    void CreateRegeneratingHeart()
    {
        if (regeneratingHeart != null)
        {
            Destroy(regeneratingHeart.gameObject);
        }

        GameObject regenHeartGO = Instantiate(heartPrefab, heartContainer);
        regeneratingHeart = regenHeartGO.GetComponent<Image>();

        // Position it to the right of the rightmost heart
        RectTransform rectTransform = regenHeartGO.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(5 * heartSpacing, 0); // Right of the 5th heart

        // Start with alpha 0
        Color color = regeneratingHeart.color;
        color.a = 0f;
        regeneratingHeart.color = color;
    }

    void MakeHeartFall()
    {
        Debug.Log($"[HeartUI] MakeHeartFall called. Hearts count: {hearts.Count}");

        // Find the rightmost visible heart to make it fall
        Image heartToFall = null;
        int heartIndex = -1;

        // Search from right to left (index 4 to 0)
        for (int i = hearts.Count - 1; i >= 0; i--)
        {
            if (hearts[i] != null && hearts[i].color.a > 0.5f) // Visible heart
            {
                heartToFall = hearts[i];
                heartIndex = i;
                Debug.Log($"[HeartUI] Found heart to fall at index {i}, alpha: {hearts[i].color.a}");
                break;
            }
        }

        if (heartToFall == null)
        {
            Debug.LogWarning("[HeartUI] No visible heart found to make fall!");

            // Debug: Log all heart states
            for (int i = 0; i < hearts.Count; i++)
            {
                if (hearts[i] != null)
                {
                    Debug.Log($"[HeartUI] Heart {i}: alpha = {hearts[i].color.a}");
                }
                else
                {
                    Debug.Log($"[HeartUI] Heart {i}: NULL");
                }
            }
            return;
        }

        // Make the heart invisible immediately
        Color color = heartToFall.color;
        color.a = 0f;
        heartToFall.color = color;
        Debug.Log($"[HeartUI] Made heart {heartIndex} invisible");

        // Create a falling copy
        CreateFallingHeart(heartToFall.transform.position);

        // Reset regenerating heart alpha when a heart falls
        ResetRegeneratingHeart();
    }

    void CreateFallingHeart(Vector3 startPosition)
    {
        Debug.Log($"[HeartUI] Creating falling heart at position: {startPosition}");

        if (fallingContainer == null)
        {
            Debug.LogError("[HeartUI] FallingContainer is null! Cannot create falling heart.");
            return;
        }

        if (heartPrefab == null)
        {
            Debug.LogError("[HeartUI] HeartPrefab is null! Cannot create falling heart.");
            return;
        }

        GameObject fallingHeart = Instantiate(heartPrefab, fallingContainer);
        Image fallingImage = fallingHeart.GetComponent<Image>();
        RectTransform fallingRect = fallingHeart.GetComponent<RectTransform>();

        if (fallingImage == null)
        {
            Debug.LogError("[HeartUI] Falling heart prefab doesn't have Image component!");
            Destroy(fallingHeart);
            return;
        }

        // Set position to where the original heart was
        fallingRect.position = startPosition;

        Debug.Log($"[HeartUI] Falling heart created successfully. Starting fall animation.");

        // Animate falling
        Vector3 fallTarget = startPosition + Vector3.down * heartFallDistance;

        fallingRect.DOMove(fallTarget, heartFallDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => {
                Debug.Log("[HeartUI] Falling heart animation completed, destroying object");
                Destroy(fallingHeart);
            });

        // Optional: Add rotation while falling
        fallingRect.DORotate(new Vector3(0, 0, Random.Range(-360f, 360f)), heartFallDuration, RotateMode.FastBeyond360);

        // Optional: Fade out while falling
        fallingImage.DOFade(0f, heartFallDuration).SetEase(Ease.InQuad);
    }

    void RestoreHeart()
    {
        // Find the leftmost invisible heart to restore
        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] != null && hearts[i].color.a < 0.5f) // Invisible heart
            {
                // Make it visible again
                Color color = hearts[i].color;
                color.a = 1f;
                hearts[i].color = color;

                // Optional: Add a pop animation
                hearts[i].transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 10, 1f).SetEase(Ease.OutBounce);

                break;
            }
        }
    }

    void UpdateRegeneratingHeart(int currentHearts, float regenProgress)
    {
        if (regeneratingHeart == null) return;

        // Only show regenerating heart if we're missing hearts
        if (currentHearts >= 5)
        {
            // All hearts are full, hide regenerating heart
            Color color = regeneratingHeart.color;
            color.a = 0f;
            regeneratingHeart.color = color;
            return;
        }

        // Update alpha based on regeneration progress
        Color regenColor = regeneratingHeart.color;
        regenColor.a = regenProgress;
        regeneratingHeart.color = regenColor;

        // Position the regenerating heart to the right of the rightmost visible heart
        int rightmostVisibleIndex = GetRightmostVisibleHeartIndex();
        RectTransform regenRect = regeneratingHeart.GetComponent<RectTransform>();
        regenRect.anchoredPosition = new Vector2((rightmostVisibleIndex + 1) * heartSpacing, 0);
    }

    int GetRightmostVisibleHeartIndex()
    {
        // Find the rightmost visible heart
        for (int i = hearts.Count - 1; i >= 0; i--)
        {
            if (hearts[i] != null && hearts[i].color.a > 0.5f)
            {
                return i;
            }
        }
        return -1; // No visible hearts
    }

    void ResetRegeneratingHeart()
    {
        if (regeneratingHeart != null)
        {
            Color color = regeneratingHeart.color;
            color.a = 0f;
            regeneratingHeart.color = color;
        }
    }

    // Optional: Method to manually set heart count for testing
    [ContextMenu("Test Lose Heart")]
    void TestLoseHeart()
    {
        MakeHeartFall();
    }

    [ContextMenu("Test Restore Heart")]
    void TestRestoreHeart()
    {
        RestoreHeart();
    }
}