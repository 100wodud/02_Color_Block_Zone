#if ENABLE_ADMOB_SDK
using GoogleMobileAds.Api;
#if ENABLE_FIREBASE_SDK
using Firebase.Analytics;
#endif

namespace Areung_Plugin.SDK.Ads
{
    /// <summary>
    /// 광고 수익 한 건을 애널리틱스로 보낸다. AdMob 의 OnAdPaid 가 유일한 호출처다.
    /// </summary>
    /// <remarks>
    /// 이벤트 이름과 파라미터는 구글이 정한 ad_impression 규약을 그대로 쓴다 —
    /// 이름을 바꾸면 Firebase 가 광고 수익으로 집계하지 않고 그냥 커스텀 이벤트로 흘린다.
    /// </remarks>
    public static class AdRevenueLogger
    {
        public static void Send(string placement, AdValue adValue, string adUnitId = null)
        {
            if (adValue == null) return;

            // AdValue.Value 는 마이크로 단위(백만분의 1)다. 그대로 보내면 수익이 백만 배로 찍힌다
            double revenue = adValue.Value / 1000000d;

#if ENABLE_FIREBASE_SDK
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventAdImpression,
                new Parameter("ad_platform", "admob"),
                new Parameter("ad_format", placement ?? string.Empty),
                new Parameter("ad_unit_name", adUnitId ?? string.Empty),
                new Parameter(FirebaseAnalytics.ParameterValue, revenue),
                new Parameter(FirebaseAnalytics.ParameterCurrency, adValue.CurrencyCode ?? "USD"));
#endif
        }
    }
}
#endif
