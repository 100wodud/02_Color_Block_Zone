using System;
using System.Collections.Generic;
using UnityEngine;

namespace Areung_Plugin.SDK.Social
{
    [Serializable]
    public class SocialIdEntry
    {
        [Tooltip("게임 코드에서 사용할 공통 키 (예: best_score)")]
        public string key;

        [Tooltip("Play Console 리더보드/업적 ID")]
        public string androidId;

        [Tooltip("App Store Connect 리더보드/업적 ID")]
        public string iosId;

        public string PlatformId
        {
#if UNITY_ANDROID
            get => androidId;
#elif UNITY_IOS
            get => iosId;
#else
            get => null;
#endif
        }
    }

    [CreateAssetMenu(fileName = "SocialConfig", menuName = "Areung/SDK/SocialConfig")]
    public class SocialConfig : ScriptableObject
    {
        public const string ResourcePath = "SettingSO/SocialConfig";

        [Header("동작 설정")]
        [Tooltip("로딩 시퀀스에서 자동 로그인 시도")]
        public bool autoSignInOnLaunch = true;

        [Tooltip("자동 로그인 대기 최대 시간(초). 초과하면 로딩을 막지 않고 넘어갑니다.")]
        public float signInTimeout = 3f;

        [Tooltip("업적 달성 시 iOS 기본 배너 표시")]
        public bool showAchievementBanner = true;

        [Tooltip("iOS 빌드 시 GameKit.framework + Game Center capability 자동 추가.\n" + "Apple Developer 의 App ID 에서 Game Center 가 켜져있어야 서명이 통과합니다.")]
        public bool addGameCenterCapabilityOnBuild = true;

        [Header("GPGS 셋업 (Android)")]
        [Tooltip("Play Console → 게임 세부정보 → 사용자 인증 정보 의 '웹 클라이언트 ID'\n" +
                 "형식: 000000000000-xxxxxxxx.apps.googleusercontent.com")]
        public string gpgsWebClientId;

        [Tooltip("Play Console → 실적/리더보드 → '리소스 가져오기' 에 나오는 XML 전체를 그대로 붙여넣으세요.")]
        [TextArea(6, 20)]
        public string gpgsResourcesDefinition;

        [Tooltip("셋업이 생성할 상수 클래스 이름. GPGS 기본값 그대로 두면 됩니다.")]
        public string gpgsConstantsClassName = "GPGSIds";

        [Tooltip("상수 클래스를 생성할 폴더. GPGS 기본값 그대로 두면 됩니다.")]
        public string gpgsConstantsDirectory = "Assets";

        [Header("리더보드")]
        public List<SocialIdEntry> leaderboards = new();

        [Header("업적")]
        public List<SocialIdEntry> achievements = new();

        private Dictionary<string, SocialIdEntry> _leaderboardMap;
        private Dictionary<string, SocialIdEntry> _achievementMap;

        public string GetLeaderboardId(string key) => Resolve(ref _leaderboardMap, leaderboards, key, "리더보드");

        public string GetAchievementId(string key) => Resolve(ref _achievementMap, achievements, key, "업적");

        private static string Resolve(ref Dictionary<string, SocialIdEntry> map, List<SocialIdEntry> source, string key, string label)
        {
            if (string.IsNullOrEmpty(key)) return null;

            if (map == null)
            {
                map = new Dictionary<string, SocialIdEntry>();
                foreach (var entry in source)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.key)) continue;
                    map[entry.key] = entry;
                }
            }

            if (!map.TryGetValue(key, out var found))
            {
                Debug.LogWarning($"[Social] SocialConfig에 {label} 키 '{key}' 가 없습니다.");
                return null;
            }

            var id = found.PlatformId;
            if (string.IsNullOrEmpty(id)) Debug.LogWarning($"[Social] {label} '{key}' 의 현재 플랫폼 ID가 비어있습니다.");

            return id;
        }

        private void OnDisable()
        {
            _leaderboardMap = null;
            _achievementMap = null;
        }
    }
}
