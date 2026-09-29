using UnityEngine;

namespace Areung_Plugin.SDK.AdMob
{
    [CreateAssetMenu(fileName = "AdMobConfig", menuName = "Areung/SDK/AdMobConfig")]
    public class AdMobConfig : ScriptableObject
    {
        // 구글 공식 테스트 광고 단위. 실서비스 ID 를 넣기 전까지 이 값으로 돌아간다 —
        // 빈 문자열로 두면 AdMob 이 로드 자체를 실패시켜 광고 흐름을 확인할 수가 없다
        public const string TestAndroidBanner = "ca-app-pub-3940256099942544/6300978111";
        public const string TestAndroidInterstitial = "ca-app-pub-3940256099942544/1033173712";
        public const string TestAndroidReward = "ca-app-pub-3940256099942544/5224354917";
        public const string TestAndroidAppOpen = "ca-app-pub-3940256099942544/9257395921";

        public const string TestIosBanner = "ca-app-pub-3940256099942544/2934735716";
        public const string TestIosInterstitial = "ca-app-pub-3940256099942544/4411468910";
        public const string TestIosReward = "ca-app-pub-3940256099942544/1712485313";
        public const string TestIosAppOpen = "ca-app-pub-3940256099942544/5575463023";

        [Header("광고가 시작 될 스테이지")]
        public int defaultAdsLevel;

        [Header("광고 쿨타임")]
        public int commonInterCool;

        [Header("앱오픈 쿨타임")]
        public bool isAppOpen;
        public int appOpenInterCool;

        [Header("Android Ad Unit ID")]
        public string androidBannerKey = TestAndroidBanner;
        public string androidInterstitialKey = TestAndroidInterstitial;
        public string androidRewardKey = TestAndroidReward;
        public string androidAppOpenKey = TestAndroidAppOpen;

        [Header("iOS Ad Unit ID")]
        public string iosBannerKey = TestIosBanner;
        public string iosInterstitialKey = TestIosInterstitial;
        public string iosRewardKey = TestIosReward;
        public string iosAppOpenKey = TestIosAppOpen;

#if UNITY_EDITOR
        [Header("Test Device IDs")]
        public TextAsset testDeviceCSV;
#endif
        public string[] testDeviceIds;

        // WHY: 빈 칸을 그대로 AdMob 에 넘기면 "invalid ad unit" 으로 조용히 실패한다.
        // 아직 실서비스 ID 를 안 받은 단계에서도 광고 자리가 도는 게 보여야 하므로 테스트 ID 로 떨어뜨린다
        public static string OrTest(string value, string test) =>
            string.IsNullOrWhiteSpace(value) ? test : value.Trim();
    }

    public abstract class AdsKey
    {
        public const string AppOpenCooldown = "AppOpen_Inter_Cool";
        public const string CommonCooldown = "Common_Inter_Cool";
    }
}
