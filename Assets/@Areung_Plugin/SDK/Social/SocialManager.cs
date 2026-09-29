using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_ANDROID && ENABLE_GPGS_SDK
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
#if UNITY_IOS && ENABLE_GPGS_SDK
using UnityEngine.SocialPlatforms;
using UnityEngine.SocialPlatforms.GameCenter;
#endif

#pragma warning disable 618

namespace Areung_Plugin.SDK.Social
{
    public static class SocialManager
    {
        public static bool IsInitialized { get; private set; }
        public static bool IsAuthenticated { get; private set; }
        public static string UserId { get; private set; }
        public static string UserName { get; private set; }
        public static bool IsSupported
        {
#if UNITY_ANDROID && ENABLE_GPGS_SDK
            get => true;
#elif UNITY_IOS && !UNITY_EDITOR && ENABLE_GPGS_SDK
            get => true;
#else
            get => false;
#endif
        }

        private static SocialConfig _config;
#if UNITY_ANDROID && ENABLE_GPGS_SDK
        private static bool _activated;
#endif

        private static SocialConfig Config
        {
            get
            {
                if (_config == null) _config = Resources.Load<SocialConfig>(SocialConfig.ResourcePath);
                return _config;
            }
        }

        #region Initialize

        public static async UniTask Initialized()
        {
            if (IsInitialized) return;

            try
            {
                var request = Resources.LoadAsync<SocialConfig>(SocialConfig.ResourcePath);
                await request.ToUniTask();
                _config = request.asset as SocialConfig;

                if (_config == null) return;
                if (!IsSupported) return;
                if (!_config.autoSignInOnLaunch) return;

                var done = false;
                PlatformSignIn(_ => done = true);
                await UniTask.WaitUntil(() => done).Timeout(TimeSpan.FromSeconds(Mathf.Max(1f, _config.signInTimeout)));
            }
            catch (TimeoutException)
            {
                Debug.LogWarning("[Social] 로그인 응답 지연 — 로딩을 계속 진행합니다.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Social] 초기화 실패: {e.Message}");
            }
            finally
            {
                IsInitialized = true;
            }
        }

        #endregion

        #region Social Manager

        // 로그인 재시도 (앱 시작 시 자동 로그인은 Initializer 가 처리하므로, 실패했을 때 버튼용)
        public static void ManualSignIn(Action<bool> onComplete = null)
        {
            if (IsAuthenticated)
            {
                onComplete?.Invoke(true);
                return;
            }

            PlatformManualSignIn(onComplete);
        }

        // 리더보드 점수 전송
        public static void ReportScore(string key, long score, Action<bool> onComplete = null)
        {
            var id = GetLeaderboardId(key);
            if (!EnsureReady(id, onComplete)) return;

            PlatformReportScore(id, score, ok => Complete(ok, "ReportScore", key, onComplete));
        }

        // 리더보드 UI — key 가 없으면 전체 목록
        public static void ShowLeaderboard(string key = null)
        {
            if (!EnsureAuthenticated()) return;

            PlatformShowLeaderboard(GetLeaderboardId(key));
        }

        // 업적 즉시 달성
        public static void UnlockAchievement(string key, Action<bool> onComplete = null) => ReportAchievementProgress(key, 100.0, onComplete);

        // 업적 진행도 갱신
        public static void ReportAchievementProgress(string key, double progress, Action<bool> onComplete = null)
        {
            var id = GetAchievementId(key);
            if (!EnsureReady(id, onComplete)) return;

            PlatformReportProgress(id, progress, ok => Complete(ok, "Achievement", key, onComplete));
        }

        // 누적형 업적 — GPGS 전용. Game Center 에는 누적형 업적 개념이 없어 iOS 에서는 무시됩니다.
        public static void IncrementAchievement(string key, int steps, Action<bool> onComplete = null)
        {
            var id = GetAchievementId(key);
            if (!EnsureReady(id, onComplete)) return;

            PlatformIncrementAchievement(id, steps, ok => Complete(ok, "Increment", key, onComplete));
        }

        // 업적 UI
        public static void ShowAchievements()
        {
            if (!EnsureAuthenticated()) return;

            PlatformShowAchievements();
        }

        #endregion

        #region Platform

        private static void PlatformSignIn(Action<bool> onComplete)
        {
#if UNITY_ANDROID && ENABLE_GPGS_SDK
            EnsureActivated();
            PlayGamesPlatform.Instance.Authenticate(status => HandleGpgsResult(status, onComplete));
#elif UNITY_IOS && ENABLE_GPGS_SDK
            UnityEngine.Social.localUser.Authenticate(success =>
            {
                IsAuthenticated = success;
                if (success)
                {
                    UserId = UnityEngine.Social.localUser.id;
                    UserName = UnityEngine.Social.localUser.userName;
                    if (Config != null && Config.showAchievementBanner) GameCenterPlatform.ShowDefaultAchievementCompletionBanner(true);
                    Debug.Log($"[Social] Game Center 로그인 성공: {UserName}");
                }
                else
                {
                    Debug.LogWarning("[Social] Game Center 로그인 실패");
                }
                onComplete?.Invoke(success);
            });
#else
            onComplete?.Invoke(false);
#endif
        }

        private static void PlatformManualSignIn(Action<bool> onComplete)
        {
#if UNITY_ANDROID && ENABLE_GPGS_SDK
            EnsureActivated();
            PlayGamesPlatform.Instance.ManuallyAuthenticate(status => HandleGpgsResult(status, onComplete));
#else
            PlatformSignIn(onComplete);
#endif
        }

        private static void PlatformReportScore(string id, long score, Action<bool> onComplete)
        {
#if UNITY_ANDROID && ENABLE_GPGS_SDK
            PlayGamesPlatform.Instance.ReportScore(score, id, onComplete);
#elif UNITY_IOS && ENABLE_GPGS_SDK
            UnityEngine.Social.ReportScore(score, id, onComplete);
#else
            onComplete?.Invoke(false);
#endif
        }

        private static void PlatformShowLeaderboard(string id)
        {
#if UNITY_ANDROID && ENABLE_GPGS_SDK
            if (string.IsNullOrEmpty(id)) PlayGamesPlatform.Instance.ShowLeaderboardUI();
            else PlayGamesPlatform.Instance.ShowLeaderboardUI(id);
#elif UNITY_IOS && ENABLE_GPGS_SDK
            if (string.IsNullOrEmpty(id)) UnityEngine.Social.ShowLeaderboardUI();
            else GameCenterPlatform.ShowLeaderboardUI(id, TimeScope.AllTime);
#endif
        }

        private static void PlatformReportProgress(string id, double progress, Action<bool> onComplete)
        {
#if UNITY_ANDROID && ENABLE_GPGS_SDK
            PlayGamesPlatform.Instance.ReportProgress(id, progress, onComplete);
#elif UNITY_IOS && ENABLE_GPGS_SDK
            UnityEngine.Social.ReportProgress(id, progress, onComplete);
#else
            onComplete?.Invoke(false);
#endif
        }

        private static void PlatformIncrementAchievement(string id, int steps, Action<bool> onComplete)
        {
#if UNITY_ANDROID && ENABLE_GPGS_SDK
            PlayGamesPlatform.Instance.IncrementAchievement(id, steps, onComplete);
#else
            Debug.Log($"[Social] IncrementAchievement 는 GPGS 전용이라 무시합니다. (id: {id}, steps: {steps})");
            onComplete?.Invoke(false);
#endif
        }

        private static void PlatformShowAchievements()
        {
#if UNITY_ANDROID && ENABLE_GPGS_SDK
            PlayGamesPlatform.Instance.ShowAchievementsUI();
#elif UNITY_IOS && ENABLE_GPGS_SDK
            UnityEngine.Social.ShowAchievementsUI();
#endif
        }

#if UNITY_ANDROID && ENABLE_GPGS_SDK
        private static void EnsureActivated()
        {
            if (_activated) return;
            PlayGamesPlatform.Activate();
            _activated = true;
        }

        private static void HandleGpgsResult(SignInStatus status, Action<bool> onComplete)
        {
            IsAuthenticated = status == SignInStatus.Success;

            if (IsAuthenticated)
            {
                UserId = PlayGamesPlatform.Instance.localUser.id;
                UserName = PlayGamesPlatform.Instance.localUser.userName;
                Debug.Log($"[Social] GPGS 로그인 성공: {UserName}");
            }
            else
            {
                Debug.LogWarning($"[Social] GPGS 로그인 실패: {status}");
            }

            onComplete?.Invoke(IsAuthenticated);
        }
#endif

        #endregion

        #region Internal

        private static string GetLeaderboardId(string key) => Config != null ? Config.GetLeaderboardId(key) : null;

        private static string GetAchievementId(string key) => Config != null ? Config.GetAchievementId(key) : null;

        private static bool EnsureAuthenticated()
        {
            if (IsAuthenticated) return true;
            Debug.LogWarning("[Social] 로그인 상태가 아닙니다. ManualSignIn() 먼저 호출하세요.");
            return false;
        }

        private static bool EnsureReady(string id, Action<bool> onComplete)
        {
            if (!IsAuthenticated || string.IsNullOrEmpty(id))
            {
                if (!IsAuthenticated) Debug.LogWarning("[Social] 로그인 상태가 아니라 요청을 건너뜁니다.");
                onComplete?.Invoke(false);
                return false;
            }
            return true;
        }

        private static void Complete(bool ok, string action, string key, Action<bool> onComplete)
        {
            if (!ok) Debug.LogWarning($"[Social] {action} 실패 (key: {key})");
            onComplete?.Invoke(ok);
        }

        #endregion
    }
}
