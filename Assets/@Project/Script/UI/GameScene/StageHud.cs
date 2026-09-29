using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class StageHud : MonoBehaviour
{
    [SerializeField] private Button setting;
    
    // 예전 레벨/타이머 텍스트 자리를 그대로 쓴다 — 씬 연결을 다시 하지 않아도 된다
    [Header("Score")]
    [FormerlySerializedAs("levelText")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private string scoreFormat = "{0}";

    [Header("Best")]
    [FormerlySerializedAs("timerText")]
    [SerializeField] private TextMeshProUGUI bestText;
    [SerializeField] private string bestFormat = "BEST {0}";

    // 씬에는 메뉴 Tools > Color Block Zone > Create Moves HUD 로 만든다
    [Header("Moves")]
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private string movesFormat = "{0}";
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warnColor = new(0.95f, 0.3f, 0.3f, 1f);

    // 구역이 지워진 자리에 "+2" 와 PERFECT 를 띄운다. 글꼴은 점수 글자 것을 쓴다
    [Header("Zone Clear FX")]
    [SerializeField] private float gainFontSize = 64f;
    [SerializeField] private float perfectFontSize = 88f;
    [SerializeField] private Color gainColor = new(0.55f, 1f, 0.55f, 1f);
    [SerializeField] private Color perfectColor = new(1f, 0.85f, 0.25f, 1f);

    private void OnEnable()
    {
        StageEvents.ScoreChanged += OnScoreChanged;
        StageEvents.MovesChanged += OnMovesChanged;
        StageEvents.ZoneCleared += OnZoneCleared;
        setting.AddEvent(EventTriggerType.PointerClick,(data) => GameManager.Instance.Popup.OpenPopup("Setting_Popup"));

        // 판이 HUD 보다 먼저 시작되면 첫 점수 이벤트를 놓친다 — 켜질 때 한 번 직접 읽는다
        var stage = GameScene.Instance != null ? GameScene.Instance.Stage : null;
        OnScoreChanged(stage != null ? stage.Score : 0, stage != null ? stage.Best : (PlayerData.IsInitialized ? PlayerData.BestScore : 0));
        OnMovesChanged(stage != null ? stage.Moves : DefaultKey.MOVES_START, 0);
    }

    private void OnDisable()
    {
        StageEvents.ScoreChanged -= OnScoreChanged;
        StageEvents.MovesChanged -= OnMovesChanged;
        StageEvents.ZoneCleared -= OnZoneCleared;
    }

    private void OnScoreChanged(int score, int best)
    {
        if (scoreText != null) scoreText.text = string.Format(scoreFormat, score);
        if (bestText != null) bestText.text = string.Format(bestFormat, best);
    }

    private void OnMovesChanged(int moves, int delta)
    {
        if (movesText == null) return;
        movesText.text = string.Format(movesFormat, moves);
        movesText.color = moves <= DefaultKey.MOVES_WARN ? warnColor : normalColor;

        // 받았을 때만 튄다 — 한 번 놓을 때마다 튀면 시끄럽다
        if (delta > 0)
        {
            var t = movesText.rectTransform;
            t.DOKill(true);
            t.DOPunchScale(Vector3.one * 0.25f, 0.3f, 6, 0.5f);
        }
    }

    private void OnZoneCleared(ZoneClearInfo info)
    {
        var canvas = GetComponentInParent<Canvas>();
        var stage = GameScene.Instance != null ? GameScene.Instance.Stage : null;
        var cam = stage != null ? stage.ViewCamera : Camera.main;
        if (canvas == null || cam == null) return;

        var root = (RectTransform)canvas.rootCanvas.transform;
        var uiCam = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
        Vector2 screen = cam.WorldToScreenPoint(info.WorldCenter);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, uiCam, out var local)) return;

        // PERFECT 는 +N 위에 띄우고, +N 은 살짝 늦게 나오게 해서 둘이 한 덩어리로 읽히게 한다
        if (info.Perfect)
        {
            SpawnFloat(root, local + new Vector2(0f, 70f), "PERFECT", perfectFontSize, perfectColor, 0f, 0.9f);
            SpawnFloat(root, local - new Vector2(0f, 10f), $"+{info.MovesGained}", gainFontSize, perfectColor, 0.1f, 0.8f);
        }
        else
        {
            SpawnFloat(root, local, $"+{info.MovesGained}", gainFontSize, gainColor, 0f, 0.7f);
        }
    }

    private void SpawnFloat(RectTransform root, Vector2 pos, string text, float size, Color color, float delay, float duration)
    {
        var go = new GameObject("ClearFx", typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(root, false);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(600f, size * 1.4f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (scoreText != null)
        {
            tmp.font = scoreText.font;
            tmp.fontSharedMaterial = scoreText.fontSharedMaterial;
        }
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.alpha = 0f;

        rect.localScale = Vector3.one * 0.6f;
        DOTween.Sequence()
            .AppendInterval(delay)
            .Append(tmp.DOFade(1f, 0.1f))
            .Join(rect.DOScale(1f, 0.25f).SetEase(Ease.OutBack))
            .Join(rect.DOAnchorPosY(pos.y + 120f, duration + 0.3f).SetEase(Ease.OutCubic))
            .Insert(delay + duration, tmp.DOFade(0f, 0.3f))
            .SetTarget(rect)
            .OnComplete(() => { if (go != null) Destroy(go); });
    }
}
