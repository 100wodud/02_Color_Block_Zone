using Areung_Plugin.SDK;
using Areung_Plugin.SDK.AdMob;
using DG.Tweening;
using Firebase.Analytics;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageFail_Popup : BasePopup
{
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button continueBtn;

    [Header("Result")]
    [SerializeField] private TextMeshProUGUI bestText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI newBestText;
    [SerializeField] private string bestFormat = "BEST {0}";
    [SerializeField] private float countUpDuration = 0.6f;

    private bool _busy;

    protected override void OnOpen()
    {
        _busy = false;
        Initialized();

        var stage = GameScene.Instance.Stage;
        continueBtn.gameObject.SetActive(stage.CanRevive);
        ShowResult(stage);
        FirebaseAnalytics.LogEvent("game_over", new Parameter("score", stage.Score));
    }

    protected override void OnClose()
    {
        if (scoreText != null) scoreText.DOKill();
    }

    // 점수는 0 부터 올라가며 보여 준다. 신기록이면 BEST 줄도 이번 점수가 된다
    private void ShowResult(StageController stage)
    {
        if (bestText != null) bestText.text = string.Format(bestFormat, stage.Best);
        if (newBestText != null) newBestText.gameObject.SetActive(stage.IsNewBest);
        if (scoreText == null) return;

        int target = stage.Score;
        scoreText.DOKill();
        scoreText.text = "0";
        DOVirtual.Int(0, target, countUpDuration, v => scoreText.text = v.ToString())
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .SetTarget(scoreText);
    }

    private void Initialized()
    {
        retryBtn.AddEvent(EventTriggerType.PointerClick, (_) => OnClickRetryBtn());
        continueBtn.AddEvent(EventTriggerType.PointerClick, (_) => OnClickContinueBtn());
    }

    // 새 판은 전면 광고 쿨타임이 찼을 때만 광고를 거친다
    private void OnClickRetryBtn()
    {
        if (_busy) return;
        _busy = true;

        void Load() => SceneLoader.LoadSceneWithLoading(SceneName.GameScene);

        if (SDKManager.IsInitialized && SDKManager.InterstitialCondition(AdsKey.CommonCooldown))
            SDKManager.ShowInterstitial(action: Load, failAction: Load, key: AdsKey.CommonCooldown);
        else Load();
    }

    private void OnClickContinueBtn()
    {
        if (_busy) return;

        void Revive()
        {
            if (GameScene.Instance.Stage.Revive()) Close();
            else _busy = false;
        }

#if ENABLE_ADMOB_SDK
        _busy = true;
        SDKManager.ShowReward(action: Revive, failAction: () => _busy = false);
#else
        Revive(); 
#endif
    }
}
