#if ENABLE_ADMOB_SDK
using System;
using Areung_Plugin.Core;
using Areung_Plugin.SDK.Ads;
using GoogleMobileAds.Api;
using UnityEngine;

public class Interstitial : IDisposable
{
    private readonly string _key;
    private InterstitialAd _ad;
    private bool _isLoading;
    private Action _command;
    private Action _failCommand;
    private string _adExtensionKey;

    public Interstitial(string key)
    {
        _key = key;
    }

    public void Dispose()
    {
        DestroyAd();
    }

    public void InitializeInterstitialAds()
    {
        LoadInterstitial();
    }

    private void LoadInterstitial()
    {
        if (_isLoading) return;
        _isLoading = true;

        DestroyAd();

        InterstitialAd.Load(_key, new AdRequest(), (ad, error) =>
        {
            _isLoading = false;

            if (error != null || ad == null)
            {
                Debug.Log($"[Interstitial] 로드 실패: {error}");
                return;
            }

            _ad = ad;
            RegisterEventHandlers(ad);
            Debug.Log("[Interstitial] 로드 완료");
        });
    }

    public void ShowInterstitial(Action action = null, Action failAction = null, string key = null)
    {
        ApplicationEventSystem.IsWatchingAd = true;
        _command = action;
        _failCommand = failAction;
        _adExtensionKey = key;

        if (_ad == null || !_ad.CanShowAd())
        {
            FailCommand();
            return;
        }

        // 광고가 게임 사운드를 덮어쓰지 않게 음소거한다
        MobileAds.SetApplicationMuted(true);
        _ad.Show();
    }

    private void CompleteCommand()
    {
        Action command = _command;
        ClearCommands();
        ApplicationEventSystem.IsWatchingAd = false;
        command?.Invoke();
        LoadInterstitial();
    }

    private void FailCommand()
    {
        Action fail = _failCommand;
        ClearCommands();
        ApplicationEventSystem.IsWatchingAd = false;
        fail?.Invoke();
        LoadInterstitial();
    }

    // WHY: 콜백을 먼저 비우고 나서 호출한다. 게임 콜백이 그 자리에서 다음 광고를 요청하는 경우가 있어
    // 호출 뒤에 비우면 방금 걸어 둔 콜백을 도로 지운다
    private void ClearCommands()
    {
        if (!string.IsNullOrEmpty(_adExtensionKey)) AdExtension.UpdateLastCooldownTime(_adExtensionKey);
        _command = null;
        _failCommand = null;
        _adExtensionKey = null;
    }

    private void RegisterEventHandlers(InterstitialAd ad)
    {
        ad.OnAdPaid += adValue => AdRevenueLogger.Send("Interstitial", adValue, _key);

        ad.OnAdFullScreenContentClosed += () => AdMainThread.Run(() =>
        {
            Debug.Log("[Interstitial] 닫힘");
            CompleteCommand();
        });

        ad.OnAdFullScreenContentFailed += error => AdMainThread.Run(() =>
        {
            Debug.Log($"[Interstitial] 표시 실패: {error}");
            FailCommand();
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
