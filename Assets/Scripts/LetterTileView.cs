using TMPro;
using UnityEngine;
using DG.Tweening;

public class LetterTileView : MonoBehaviour
{
    public TMP_Text label;

    [Header("Jumping Parameters")]
    [SerializeField] private Vector2 jumpoffset;
    [SerializeField] private float jumpPower;
    [SerializeField] private float jumpDuration;

    [Header("Popping Parameters")]
    [SerializeField] private float popDuration;

    [SerializeField] private CanvasGroup canvasGroup;
    [HideInInspector] public RectTransform rectTransform;

    private Tweener shakingTween;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetChar(char c)
    {
        label.text = c.ToString();
    }

    public void ShadowOriginalObject()
    {
        canvasGroup.alpha = 0;
        shakingTween.Kill();
    }

    public void SetCurrentBliinkingAnim()
    {
        shakingTween = rectTransform.DOShakeScale(popDuration, strength: new Vector3(0f, 0.2f, 0f), vibrato: 7, randomness: 70, randomnessMode: ShakeRandomnessMode.Harmonic).SetEase(Ease.OutQuad).SetLoops(int.MaxValue);
    }

    public void SetCurrentFallingAnim()
    {
        if (transform != null && rectTransform != null)
        {
            // jump a bit while falling
            // rectTransform.DOJumpAnchorPos(new Vector2(rectTransform.anchoredPosition.x + jumpoffset.x, rectTransform.anchoredPosition.y + jumpoffset.y), jumpPower, 1, jumpDuration).SetEase(Ease.OutQuad);

            // fall down animation of a UI Element by using DOTween
            rectTransform.DOAnchorPosY(-600f, 0.6f).SetEase(Ease.InQuad); // -600f because it is maximum the value that the object gets out of the screen

            // rotate while falling
            int rand = Random.Range(0, 2);
            int factor = rand == 0 ? -1 : 1;
            rectTransform.DOLocalRotate(new Vector3(0f, 0f, factor * 360f), 2f, RotateMode.FastBeyond360).OnComplete(() => Destroy(gameObject));
        }
    }
}
