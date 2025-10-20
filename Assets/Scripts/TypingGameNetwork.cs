using System;
using DG.Tweening;
using Fusion;
using UnityEngine;

public class TypingGameNetwork : NetworkBehaviour
{
    [Networked] public int SentenceIndex { get; set; } // which sentence we're on
    [Networked] public int Progress { get; set; }      // index inside current sentence

    // Heart System 
    [Networked] public int CurrentTypoCount { get; set; }  // Current number of typos
    [Networked] private float HeartRegenTimer { get; set; }  // Timer for heart regeneration
    [Networked] public NetworkBool IsGameOver { get; set; }

    // Lock mechanism for NeedToWait
    [Networked] private float LockUntilTimeSec { get; set; }  // seconds in runner time; 0 = unlocked

    private int _mashCountSoFar = 0;

    private int MAX_TYPOS = 5;
    private float HEART_REGEN_INTERVAL = 1f; // One heart every x seconds

    [Networked] private float score { get; set; } = 0;
    [Networked] private float comboCounter { get; set; }

    [Networked] private bool isTimerOn { get; set; }
    [Networked] private float currentTimeInSeconds { get; set; }

    private CanvasGroup combotTextCanvasGroup;
    private Tweener comboTweener;
    private Tweener scoreTweener;

    public LetterGridController grid { get; set; }

    public LetterUnit[] CurrentSentence => SentenceData.Sentences[
        Mathf.Clamp(SentenceIndex, 0, SentenceData.Sentences.Length - 1)
    ];

    public LetterUnit[] NextSentence => SentenceData.Sentences[Mathf.Clamp(SentenceIndex + 1, 0, SentenceData.Sentences.Length - 1)];

    public override void Spawned()
    {
        Debug.Log($"[TypingGameNetwork] Spawned() called. HasStateAuthority: {Object.HasStateAuthority}, IsClient: {Runner.IsClient}, IsServer: {Runner.IsServer}");

        grid = FindFirstObjectByType<LetterGridController>();
        if (grid != null)
        {
            Debug.Log("[TypingGameNetwork] Found LetterGridController, binding...");
            grid.Bind(this);
        }
        else
        {
            Debug.LogWarning("[TypingGameNetwork] LetterGridController not found!");
        }

        if (Object.HasStateAuthority)
        {
            Debug.Log("[TypingGameNetwork] Has state authority, initializing game state...");
            StartSentence(0);
            CurrentTypoCount = 0;
            HeartRegenTimer = HEART_REGEN_INTERVAL;
            IsGameOver = false;
        }
        else
        {
            Debug.Log("[TypingGameNetwork] No state authority, waiting for server state...");
        }

        if (grid != null && grid.comboTMPRO != null)
        {
            combotTextCanvasGroup = grid.comboTMPRO.gameObject.GetComponent<CanvasGroup>();
            StartTimer(grid.gameDurationInMinutes);
        }
        else
        {
            Debug.LogWarning("[TypingGameNetwork] Grid or comboTMPRO is null, delaying initialization...");
            // Try again after a short delay
            Invoke(nameof(DelayedInitialization), 0.1f);
        }
    }

    void DelayedInitialization()
    {
        if (grid == null)
        {
            grid = FindFirstObjectByType<LetterGridController>();
            if (grid != null)
            {
                Debug.Log("[TypingGameNetwork] Found LetterGridController on delayed initialization");
                grid.Bind(this);
            }
        }

        if (grid != null && grid.comboTMPRO != null)
        {
            combotTextCanvasGroup = grid.comboTMPRO.gameObject.GetComponent<CanvasGroup>();
            StartTimer(grid.gameDurationInMinutes);
            Debug.Log("[TypingGameNetwork] Delayed initialization completed");
        }
        else
        {
            Debug.LogError("[TypingGameNetwork] Still couldn't initialize after delay!");
        }
    }

    private void StartTimer(float gameDurationInMinutes)
    {
        isTimerOn = true;
        currentTimeInSeconds = gameDurationInMinutes * 60;
    }

    void StartSentence(int index)
    {
        SentenceIndex = index;
        Progress = 0;
        SetupTimeForCurrentUnit();
        Debug.Log($"[Host] Starting sentence {SentenceIndex}");
    }

    void Update()
    {
        if (Runner == null || Progress >= CurrentSentence.Length || IsGameOver) return;

        int myId = Runner.IsServer ? 0 : 1;

        foreach (char pressedChar in Input.inputString)
        {
            ushort pressedCharShort = pressedChar;
            RPC_TryType(Runner.LocalPlayer, pressedCharShort, Progress, myId);
        }


    }

    public override void FixedUpdateNetwork()
    {
        ModifyMechanicTypeIndicatorText(CurrentSentence[Progress]);

        if (isTimerOn)
        {
            currentTimeInSeconds -= Time.fixedDeltaTime;
            TimeSpan timeLeft = TimeSpan.FromSeconds(currentTimeInSeconds);
            if (currentTimeInSeconds >= 0)
            {
                if (timeLeft.Seconds < 10)
                {
                    grid.currentTimeTextTMPRO.text = "Time left: " + timeLeft.Minutes.ToString() + ":" + "0" + timeLeft.Seconds.ToString();
                }
                else
                {
                    grid.currentTimeTextTMPRO.text = "Time left: " + timeLeft.Minutes.ToString() + ":" + timeLeft.Seconds.ToString();
                }
            }
            else
            {
                isTimerOn = false;
                IsGameOver = true;
                grid.currentTimeTextTMPRO.text = "Time is Over!";
                grid.currentTimeTextTMPRO.transform.DOShakePosition(2f, strength: 5, vibrato: 12, randomness: 90, randomnessMode: ShakeRandomnessMode.Harmonic);
            }
        }

        if (!Object.HasStateAuthority || IsGameOver) return;

        // Update heart regeneration timer
        if (Runner.IsForward && CurrentTypoCount > 0)
        {
            HeartRegenTimer -= Runner.DeltaTime;

            // Regenerate one heart every 5 seconds
            if (HeartRegenTimer <= 0)
            {
                CurrentTypoCount--;
                HeartRegenTimer = HEART_REGEN_INTERVAL;
                int remainingHearts = MAX_TYPOS - CurrentTypoCount;
                Debug.Log($"[Host] Heart regenerated! Hearts: {remainingHearts}/{MAX_TYPOS}");
            }
        }

        FadeComboText(1f, Time.fixedDeltaTime);
    }

    // ESC Menu RPC Methods
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RestartCurrentSentence()
    {
        Debug.Log($"[Host] Restarting sentence {SentenceIndex}");

        // Reset progress to start of current sentence
        Progress = 0;

        // Reset combo and any active mechanics
        comboCounter = 0;
        _mashCountSoFar = 0;

        // Reset time locks
        LockUntilTimeSec = 0f;
        SetupTimeForCurrentUnit();

        // Force grid rebuild by triggering a change
        RPC_ForceGridRebuild();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ForceGridRebuild()
    {
        if (grid != null)
        {
            // Force the grid to rebuild by resetting the last known sentence index
            grid.ForceRebuild();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SkipToNextSentence()
    {
        Debug.Log($"[Host] Skipping from sentence {SentenceIndex} to next sentence");

        // Check if there's a next sentence
        if (SentenceIndex + 1 < SentenceData.Sentences.Length)
        {
            StartSentence(SentenceIndex + 1);
            Debug.Log($"[Host] Moved to sentence {SentenceIndex}");
        }
        else
        {
            Debug.Log("[Host] No more sentences, ending game");
            IsGameOver = true;
            grid.currentTimeTextTMPRO.text = "All Sentences Completed!";
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TryType(PlayerRef sender, ushort pressedCharShort, int claimedProgress, int claimedPlayerId)
    {
        if (claimedProgress != Progress || IsGameOver) return;
        if (Progress >= CurrentSentence.Length) return;

        var unit = CurrentSentence[Progress];
        char input = (char)pressedCharShort;

        // Check if it's the player's turn
        if (unit.owner != claimedPlayerId)
        {
            HandleTypo();
            return;
        }

        Animator ownerAnimator = grid.revolverAnimators[CurrentSentence[Progress].owner];
        ownerAnimator.SetTrigger("shoot");

        if (input != unit.character) // TODO : BURADA YANLIS BASMIS DEMEK CEZALARIN HEPSINI KOYABILIRIZ
        {
            // Wrong character typed - this is a typo
            HandleTypo();
            Debug.Log($"[Host] TYPO: Wrong character! Got '{input}', expected '{unit.character}'");
            return;
        }

        // Correct input - handle mechanics
        bool completedThisStep = false;
        MechanicType mechanic = unit.mechanic;

        switch (mechanic)
        {
            case MechanicType.Normal:
                completedThisStep = true;
                ResetComboTextFade();
                Debug.Log($"[Host] CORRECT '{input}' (Progress {Progress}/{CurrentSentence.Length})");
                break;

            case MechanicType.Locked:
                _mashCountSoFar++;
                Debug.Log($"[Host] Mash {unit.character}: {_mashCountSoFar}/{Mathf.Max(1, unit.param)}");

                if (_mashCountSoFar >= Mathf.Max(1, unit.param))
                {
                    completedThisStep = true;
                    ResetComboTextFade();
                    _mashCountSoFar = 0; // reset for next letter
                }
                break;

            case MechanicType.Finish:
                // Add your substitute logic here
                Progress = 99;
                completedThisStep = true;
                break;

            case MechanicType.NeedToWait:
                Debug.Log("waiting");

                // still waiting? reject input
                // Check if still waiting
                if (Runner.SimulationTime < LockUntilTimeSec)
                {
                    Debug.Log($"[Host] Still waiting... {LockUntilTimeSec - Runner.SimulationTime:F1}s left");
                    return; // Not a typo, just early input
                }

                completedThisStep = true;
                LockUntilTimeSec = 0f;
                ResetComboTextFade();
                Debug.Log($"[Host] Wait completed for '{input}'");
                break;

            case MechanicType.GoToEnd:
                if (unit.param == 2)
                {
                    // First press - animate current letter, then go to end
                    RPC_animation(Progress); // Pass current progress as letter index
                    CurrentSentence[Progress].param = 1;
                    Progress = CurrentSentence.Length - 1;
                    SetupTimeForCurrentUnit();
                    UpdateScore();
                    return; // Exit early, don't continue with normal flow
                }
                else if (unit.param == 1)
                {
                    // Second press - complete the sentence
                    completedThisStep = true;
                    ResetComboTextFade();
                }
                break;

            case MechanicType.GoBackwards:
                RPC_animation(Progress); // Pass current progress as letter index
                Progress = Mathf.Max(0, Progress - 1); // Don't go below 0
                completedThisStep = true;
                ResetComboTextFade();
                SetupTimeForCurrentUnit();
                UpdateScore();
                return; // Exit early, don't continue with normal flow
        }

        if (!completedThisStep) return;

        // Progress forward (except for GoBackwards which handles its own progress)
        if (mechanic != MechanicType.GoBackwards && mechanic != MechanicType.GoToEnd)
        {
            RPC_animation(Progress);
            Progress++;
        }
        else if (mechanic == MechanicType.GoToEnd && unit.param != 2)
        {
            RPC_animation(Progress);
        }

        UpdateScore();
        SetupTimeForCurrentUnit();


        if (Progress >= CurrentSentence.Length)
        {
            Debug.Log("[Host] Sentence complete!");
            // Advance to next sentence if exists
            if (SentenceIndex + 1 < SentenceData.Sentences.Length)
            {
                StartSentence(SentenceIndex + 1);
            }
            else
            {
                Debug.Log("[Host] All sentences completed!");
                IsGameOver = true;
            }
        }
    }

    void SetupTimeForCurrentUnit()
    {
        LockUntilTimeSec = 0f;
        if (Progress >= CurrentSentence.Length) return;

        var unit = CurrentSentence[Progress];
        if (unit.mechanic == MechanicType.NeedToWait)
        {
            float waitSec = Mathf.Max(0.01f, unit.param); // param = seconds
            LockUntilTimeSec = (float)Runner.SimulationTime + waitSec;
        }

        _mashCountSoFar = 0;
    }

    private void HandleTypo()
    {
        if (!Object.HasStateAuthority) return;

        CurrentTypoCount++;

        // Reset the heart regeneration timer whenever a typo occurs
        HeartRegenTimer = HEART_REGEN_INTERVAL;

        int remainingHearts = MAX_TYPOS - CurrentTypoCount;
        Debug.Log($"[Host] ✗ TYPO! Hearts remaining: {remainingHearts}/{MAX_TYPOS}");

        // Check for game over
        if (CurrentTypoCount >= MAX_TYPOS)
        {
            Debug.Log("[Host] === GAME OVER! No hearts left! ===");
            IsGameOver = true;
            grid.currentTimeTextTMPRO.text = "GAME OVER - No Hearts Left!";
            grid.currentTimeTextTMPRO.transform.DOShakePosition(2f, strength: 5, vibrato: 12, randomness: 90, randomnessMode: ShakeRandomnessMode.Harmonic);
        }

        // Reset combo on typo
        comboCounter = 0;
        combotTextCanvasGroup.alpha = 0f;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_animation(int letterIndex)
    {
        currentTimeInSeconds += 0.2f;
        AudioManager.Instance.Play(AudioManager.SoundType.GunshotHit);
        grid.UpdateFallingAnim(letterIndex);
    }

    private void IncreaseScore()
    {
        score += comboCounter switch
        {
            < 5 => 100,
            > 5 and < 10 => 300,
            > 10 and < 15 => 900,
            > 15 and < 20 => 2700,
            _ => 8100,
        };
    }

    private void UpdateScore()
    {
        if (comboTweener != null)
        {
            comboTweener.Kill();
        }

        if (scoreTweener != null)
        {
            scoreTweener.Kill();
        }

        comboCounter++;
        IncreaseScore();
        grid.scoreTMPRO.text = score.ToString();
        grid.comboTMPRO.text = "x" + comboCounter.ToString();
        comboTweener = grid.comboTMPRO.gameObject.transform.DOPunchScale(new Vector3(0.6f, 0.6f, 0f), 0.15f).SetEase(Ease.OutQuad).OnComplete(() => grid.comboTMPRO.gameObject.transform.localScale = Vector3.one);
        scoreTweener = grid.scoreTMPRO.GetComponentInParent<RectTransform>().transform.DOPunchScale(new Vector3(0.6f, 0.6f, 0f), 0.15f).SetEase(Ease.OutQuad).OnComplete(() => grid.scoreTMPRO.GetComponentInParent<RectTransform>().transform.localScale = Vector3.one);
    }

    private void FadeComboText(float fadingFactor, float deltaTime)
    {
        if (combotTextCanvasGroup.alpha > 0)
        {
            combotTextCanvasGroup.alpha -= fadingFactor * deltaTime;
        }

        ResetComboCounter();
    }

    private void ResetComboCounter()
    {
        if (combotTextCanvasGroup.alpha <= 0)
        {
            comboCounter = 0;
        }
    }

    private void ResetComboTextFade()
    {
        combotTextCanvasGroup.alpha = 1f;
    }

    // Helper method to get current hearts for UI display
    public int GetCurrentHearts()
    {
        return Mathf.Clamp(MAX_TYPOS - CurrentTypoCount, 0, MAX_TYPOS);
    }

    // Helper method to get heart regen progress for UI display (0 to 1)
    public float GetHeartRegenProgress()
    {
        // Only show progress if we're missing hearts and the timer is active
        if (CurrentTypoCount <= 0) return 0f;

        // Calculate progress: 0 = just started, 1 = about to regenerate
        float progress = 1f - (HeartRegenTimer / HEART_REGEN_INTERVAL);
        return Mathf.Clamp01(progress);
    }

    // Helper method to check if timer was reset (for UI to detect reset)
    public bool WasHeartTimerReset()
    {
        return HeartRegenTimer >= HEART_REGEN_INTERVAL - 0.1f; // Close to full timer
    }

    private void ModifyMechanicTypeIndicatorText(LetterUnit unit)
    {
        switch (unit.mechanic)
        {
            case MechanicType.Normal:
                grid.mechanicTypeIndicator.text = "";
                break;
            case MechanicType.Locked:
                int difference = unit.param - _mashCountSoFar;
                grid.mechanicTypeIndicator.text = "Kilidi kırmak için aynı harfe " + difference + " kere bas! ";
                break;
            case MechanicType.GoBackwards:
                grid.mechanicTypeIndicator.text = "Geri geri yazmaya devam!";
                break;
            case MechanicType.GoToEnd:
                grid.mechanicTypeIndicator.text = "Şimdi geri geri yazma zamanı!";
                break;
            case MechanicType.NeedToWait:
                grid.mechanicTypeIndicator.text = "Az yavaş! Yazmak için " + unit.param + " saniye bekle!";
                break;
            case MechanicType.Finish:
                grid.mechanicTypeIndicator.text = "";
                break;
        }
    }
}