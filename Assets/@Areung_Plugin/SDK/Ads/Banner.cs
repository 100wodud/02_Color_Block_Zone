#if ENABLE_ADMOB_SDK
using System;
using Areung_Plugin.SDK.Ads;
using GoogleMobileAds.Api;
using UnityEngine;

public class Banner : IDisposable
{
    private readonly string _key;
    private BannerView _view;
    private bool _showBanner;

    public Banner(string key)
    {
        _key = key;
    }

    public void InitializeBannerAds()
    {
        CreateBannerView();

        // 만들자마자 로드해 두고 숨겨 둔다 — ShowBanner 때 로드부터 시작하면 배너가 늦게 뜬다
        _view.LoadAd(new AdRequest());
        _view.Hide();
    }

    private void CreateBannerView()
    {
        DestroyBannerView();

        // WHY: 고정 320x50 은 넓은 기기에서 좌우가 비어 보인다. 앵커드 어댑티브는
        // 기기 폭에 맞춰 높이를 정해 주므로 화면 하단에 꽉 찬 띠로 앉는다
        AdSize size = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(
            MobileAds.Utils.GetDeviceSafeWidth());

        _view = new BannerView(_key, size, AdPosition.Bottom);
        _view.OnAdPaid += adValue => AdRevenueLogger.Send("Banner", adValue, _key);
        _view.OnBannerAdLoadFailed += error => Debug.Log($"[Banner] 로드 실패: {error}");
    }

    public void ShowBanner()
    {
        if (_showBanner) return;

        if (_view == null) InitializeBannerAds();

        _view.Show();
        _showBanner = true;
    }

    public void HideBanner()
    {
        _showBanner = false;
        _view?.Hide();
    }

    public void Dispose() => DestroyBannerView();

    private void DestroyBannerView()
    {
        if (_view == null) return;
        _view.Destroy();
        _view = null;
        _showBanner = false;
    }
}
#endif
