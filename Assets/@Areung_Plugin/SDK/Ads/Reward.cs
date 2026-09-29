#if ENABLE_ADMOB_SDK
using System;
using Areung_Plugin.Core;
using Areung_Plugin.SDK.Ads;
using GoogleMobileAds.Api;
using UnityEngine;

public class Rewards : IDisposable
{
    private readonly string _key;
    private RewardedAd _ad;
    private bool _isLoading;
    private bool _isRewardEarned;
    private Action _command;
    private Action _failCommand;

    public Rewards(string key)
    {
        _key = key;
    }

    public void Dispose()
    {
        DestroyAd();
    }

    public void InitializeRewardAds()
    {
        LoadRewardedAd();
    }

    public void LoadRewardedAd()
    {
        if (_isLoading) return;
        _isLoading = true;

        DestroyAd();

        RewardedAd.Load(_key, new AdRequest(), (ad, error) =>
        {
            _isLoading = false;

            if (error != null || ad == null)
            {
                Debug.Log($"[Reward] 로드 실패: {error}");
                return;
            }

            _ad = ad;
            RegisterEventHandlers(ad);
            Debug.Log("[Reward] 로드 완료");
        });
    }

    public void ShowReward(Action action = null, Action failAction = null)
    {
        ApplicationEventSystem.IsWatchingAd = true;
        _command = action;
        _failCommand = failAction;
        _isRewardEarned = false;

        if (_ad == null || !_ad.CanShowAd())
        {
            CompleteCommandWithFailure();
            return;
        }

        MobileAds.SetApplicationMuted(true);

        // 보상 지급은 여기서 확정하지 않는다 — 닫힘 이벤트에서 한 번에 처리해야
        // "보상은 줬는데 팝업이 안 닫힌다" 같은 어긋남이 안 생긴다
        _ad.Show(_ => _isRewardEarned = true);
    }

    private void CompleteCommandWithSuccess()
    {
        Action command = _command;
        _command = null;
        _failCommand = null;
        ApplicationEventSystem.IsWatchingAd = false;
        command?.Invoke();
    }

    private void CompleteCommandWithFailure()
    {
        Action fail = _failCommand;
        _command = null;
        _failCommand = null;
        ApplicationEventSystem.IsWatchingAd = false;
        fail?.Invoke();
    }

    private void RegisterEventHandlers(RewardedAd ad)
    {
        ad.OnAdPaid += adValue => AdRevenueLogger.Send("Reward", adValue, _key);

        ad.OnAdFullScreenContentClosed += () => AdMainThread.Run(() =>
        {
            if (_isRewardEarned)
            {
                Debug.Log("[Reward] 보상 지급 처리");
                CompleteCommandWithSuccess();
            }
            else
            {
                Debug.Log("[Reward] 광고 중단 -> 보상 없음");
                CompleteCommandWithFailure();
            }

            _isRewardEarned = false;
            LoadRewardedAd();
        });

        ad.OnAdFullScreenContentFailed += error => AdMainThread.Run(() =>
        {
            Debug.Log($"[Reward] 표시 실패: {error}");
            _isRewardEarned = false;
            CompleteCommandWithFailure();
            LoadRewardedAd();
        });
    }

    private void DestroyAd()
    {
        if (_ad == null) return;
        _ad.Destroy();
        _ad = null;
    }
}
#endif
