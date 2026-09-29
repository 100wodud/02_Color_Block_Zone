#if ENABLE_ADMOB_SDK
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using UnityEngine;

namespace Areung_Plugin.SDK.AdMob
{
    public struct AdMobData
    {
        public string BannerKey;
        public string InterstitialKey;
        public string RewardKey;
        public string AppOpenKey;
        public string[] TestKey;
    }

    public class AdMobInitializer
    {
        private string _adUnitBannerKey;
        private string _adUnitInterstitialKey;
        private string _adUnitRewardKey;
        private string _adUnitAppOpenKey;
        private string[] _testDeviceIds;
        private bool _initialize;

        private Interstitial Interstitial { get; set; }
        private Rewards Reward { get; set; }
        private Banner Banner { get; set; }
        private AppOpen AppOpen { get; set; }

        public async UniTask AdMobInit(AdMobData keyData)
        {
            await UniTask.Yield();

            _testDeviceIds = keyData.TestKey;
            _adUnitBannerKey = keyData.BannerKey;
            _adUnitInterstitialKey = keyData.InterstitialKey;
            _adUnitRewardKey = keyData.RewardKey;
            _adUnitAppOpenKey = keyData.AppOpenKey;

            await AdMobInitialize();
        }

        private async UniTask AdMobInitialize()
        {
            if (_initialize) return;
            _initialize = true;

            Interstitial = new Interstitial(_adUnitInterstitialKey);
            Reward = new Rewards(_adUnitRewardKey);
            Banner = new Banner(_adUnitBannerKey);
            AppOpen = new AppOpen(_adUnitAppOpenKey);

            if (_testDeviceIds is { Length: > 0 })
            {
                MobileAds.SetRequestConfiguration(new RequestConfiguration
                {
                    TestDeviceIds = new List<string>(_testDeviceIds)
                });
            }

            var sdkInitCompletionSource = new UniTaskCompletionSource<bool>();

            MobileAds.Initialize(_ =>
            {
                Interstitial.InitializeInterstitialAds();
                Reward.InitializeRewardAds();
                Banner.InitializeBannerAds();
                AppOpen.InitializeAppOpenAds();
                sdkInitCompletionSource.TrySetResult(true);
            });

            await sdkInitCompletionSource.Task;
        }

        public void ShowInterstitial(Action action = null, Action failAction = null, string key = null)
        {
            Interstitial.ShowInterstitial(action, failAction, key);
        }

        public void ShowReward(Action action = null, Action failAction = null)
        {
            Reward.ShowReward(action, failAction);
        }

        public void ShowBanner() => Banner.ShowBanner();
        public void HideBanner() => Banner.HideBanner();
        public void ShowAppOpen() => AppOpen.ShowAppOpenAd();
    }
}
#endif
