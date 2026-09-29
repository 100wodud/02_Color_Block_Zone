using System;
using Areung_Plugin.Core;
using Areung_Plugin.SDK.Ads;
using Areung_Plugin.SDK.AdMob;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Areung_Plugin.SDK
{
    public static class SDKManager
    {
        public static bool IsDoingIAP { get; set; }
        private static AdMobConfig _adKey;
#if ENABLE_ADMOB_SDK
        private static AdMobInitializer AdMob { get; set; }
        private static AdMobData _adKeyData;
        public static bool IsInitialized = false;

        // 전면 광고 시작 레벨
        private static int DefaultAdsLevel { get; set; }

        // 일반 쿨타임 전면 광고 쿨타임
        private static int CommonInterCool { get; set; }

        // 앱오픈 광고 쿨타임
        private static int AppOpenInterCool { get; set; }

        public static async void Initialized()
        {
            if(IsInitialized) return;

            try
            {
                ResourceRequest request = Resources.LoadAsync<AdMobConfig>("SettingSO/AdMobConfig");
                await request.ToUniTask();

                _adKey = request.asset as AdMobConfig;

                if (_adKey == null) return;

                AdMob = new AdMobInitializer();
                AdKeyInitialize();

                await AdMob.AdMobInit(_adKeyData);

                ApplicationEventSystem.OnAppStateForeground += ReturnAppForeground;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SDKManager] Init 실패: {e.Message}");
            }
            finally
            {
                IsInitialized = true;
            }
        }

        private static void AdKeyInitialize()
        {
            DefaultAdsLevel = _adKey.defaultAdsLevel;
            CommonInterCool = _adKey.commonInterCool;
            AppOpenInterCool = _adKey.appOpenInterCool;

            _adKeyData = new AdMobData
            {
                TestKey = _adKey.testDeviceIds,
#if UNITY_ANDROID
                BannerKey = AdMobConfig.OrTest(_adKey.androidBannerKey, AdMobConfig.TestAndroidBanner),
                InterstitialKey = AdMobConfig.OrTest(_adKey.androidInterstitialKey, AdMobConfig.TestAndroidInterstitial),
                RewardKey = AdMobConfig.OrTest(_adKey.androidRewardKey, AdMobConfig.TestAndroidReward),
                AppOpenKey = AdMobConfig.OrTest(_adKey.androidAppOpenKey, AdMobConfig.TestAndroidAppOpen),
#elif UNITY_IOS
                BannerKey = AdMobConfig.OrTest(_adKey.iosBannerKey, AdMobConfig.TestIosBanner),
                InterstitialKey = AdMobConfig.OrTest(_adKey.iosInterstitialKey, AdMobConfig.TestIosInterstitial),
                RewardKey = AdMobConfig.OrTest(_adKey.iosRewardKey, AdMobConfig.TestIosReward),
                AppOpenKey = AdMobConfig.OrTest(_adKey.iosAppOpenKey, AdMobConfig.TestIosAppOpen),
#endif
            };
        }

        #region Ads Manager

        // 전면 광고
        public static async void ShowInterstitial(Action action = null, Action failAction = null, string key = null)
        {
            await UniTask.Delay(300);

            if (PlayerData.AdsRemove)
            {
                action?.Invoke();
                return;
            }

            try
            {
                AdMob.ShowInterstitial(action, failAction, key);
            }
            catch (Exception e)
            {
                failAction?.Invoke();
                if(!string.IsNullOrEmpty(key)) AdExtension.UpdateLastCooldownTime(key);
                ApplicationEventSystem.IsWatchingAd = false;
                Debug.Log("Error Interstitial Ads: " + e.Message);
            }
        }

        // 리워드 광고
        public static async void ShowReward( Action action = null, Action failAction = null)
        {
            await UniTask.Delay(300);
            try
            {
                AdMob.ShowReward(action, failAction);
            }
            catch (Exception e)
            {
                failAction?.Invoke();
                ApplicationEventSystem.IsWatchingAd = false;
                Debug.Log("Error Reward Ads: " + e.Message);
            }
        }


        // 배너 광고
        public static bool isBanner = false;
        public static void ShowBanner()
        {
            if (PlayerData.AdsRemove) return;
            isBanner = true;
            AdMob.ShowBanner();
        }

        public static void HideBanner()
        {
            isBanner = false;
            AdMob.HideBanner();
        }

        // 앱 오픈 광고
        public static void ShowAppOpenAd()
        {
            if (PlayerData.AdsRemove) return;
            AdMob.ShowAppOpen();
        }

        // 전면광고 쿨타임 확인 및
        public static bool InterstitialCondition(string type, bool force = false)
        {
            if (IsDoingIAP) return false;
            if (force) return true;
            if (PlayerData.AdsRemove) return true;

            switch (type)
            {
                case AdsKey.AppOpenCooldown:
                    if (PlayerData.ClearLevel >= DefaultAdsLevel && AdExtension.InterCondition(AdsKey.AppOpenCooldown, AppOpenInterCool)) return true;
                    break;

                case AdsKey.CommonCooldown:
                    if (PlayerData.ClearLevel >= DefaultAdsLevel && AdExtension.InterCondition(AdsKey.CommonCooldown, CommonInterCool)) return true;
                    break;
                default:
                    return false;
            }
            return false;
        }

        private static void ReturnAppForeground()
        {
            if (InterstitialCondition(AdsKey.AppOpenCooldown))
            {
                // AdMob.ShowAppOpen();
                // AdExtension.UpdateLastCooldownTime(AdsKey.AppOpenCooldown);
                // or
                // ShowInterstitial(key : AdsKey.AppOpenCooldown);
            }
        }

        #endregion
#endif
    }
}
