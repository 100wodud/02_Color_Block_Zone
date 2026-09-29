#if ENABLE_ADMOB_SDK
using System;
using Areung_Plugin.Core;
using Areung_Plugin.SDK.Ads;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using UnityEngine;

public class AppOpen : IDisposable
{
    private readonly string _key;
    private AppOpenAd _appOpenAd;

    public AppOpen(string key)
    {
        _key = key;
    }

    // MobileAds.Initialize 는 AdMobInitializer 가 한 번만 부른다 —
    // 포맷마다 초기화를 부르면 콜백이 중복으로 걸려 광고를 두 번 로드한다
    public void InitializeAppOpenAds()
    {
        LoadAppOpenAd();
    }

    public void Dispose()
    {
        DestroyAd();
    }

    public async void ShowAppOpenAd()
    {
        ApplicationEventSystem.IsWatchingAd = true;

        await UniTask.Delay(500);

        if (_appOpenAd != null && _appOpenAd.CanShowAd())
        {
            MobileAds.SetApplicationMuted(true);
            _appOpenAd.Show();
        }
        else
        {
            ApplicationEventSystem.IsWatchingAd = false;
            Debug.LogWarning("[AppOpen] 아직 로드되지 않았습니다.");
        }
    }

    public void LoadAppOpenAd()
    {
        DestroyAd();

        AppOpenAd.Load(_key, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"[AppOpen] 로드 실패: {error}");
                return;
            }

            _appOpenAd = ad;
            RegisterEventHandlers(_appOpenAd);
        });
    }

    private void RegisterEventHandlers(AppOpenAd ad)
    {
        ad.OnAdPaid += adValue => AdRevenueLogger.Send("AppOpen", adValue, _key);

        ad.OnAdFullScreenContentClosed += () => AdMainThread.Run(() =>
        {
            ApplicationEventSystem.IsWatchingAd = false;
            LoadAppOpenAd();
        });

        ad.OnAdFullScreenContentFailed += error => AdMainThread.Run(() =>
        {
            ApplicationEventSystem.IsWatchingAd = false;
            LoadAppOpenAd();
            Debug.LogError($"[AppOpen] 표시 실패: {error}");
        });
    }

    private void DestroyAd()
    {
        if (_appOpenAd == null) return;
        _appOpenAd.Destroy();
        _appOpenAd = null;
    }
}
#endif
