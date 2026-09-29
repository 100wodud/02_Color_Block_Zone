#if ENABLE_ADMOB_SDK
using System;
using GoogleMobileAds.Common;

namespace Areung_Plugin.SDK.Ads
{
    /// <summary>
    /// 광고 콜백을 유니티 메인 스레드로 넘긴다.
    /// </summary>
    /// <remarks>
    /// WHY: AdMob 의 이벤트는 네이티브 쪽 스레드에서 올라온다. 이 콜백들이 팝업을 열고
    /// 아이템을 지급하고 씬을 넘기므로, 그대로 두면 유니티 API 를 메인 스레드 밖에서 건드려 터진다.
    /// MobileAds.RaiseAdEventsOnUnityMainThread 는 11.x 에서 폐기됐고 이 방식이 대체다.
    /// </remarks>
    internal static class AdMainThread
    {
        public static void Run(Action action)
        {
            if (action == null) return;
            MobileAdsEventExecutor.ExecuteInUpdate(action);
        }
    }
}
#endif
