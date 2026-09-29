
#if ENABLE_FIREBASE_SDK
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase;
using UnityEngine;

namespace Areung_Plugin.SDK.Firebase
{
    public static class FirebaseInitializer
    {
        public static bool IsInitialized { get; private set; }
        public static async UniTask Initialized()
        {
            if (IsInitialized) return;
            try
            {
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (dependencyStatus == DependencyStatus.Available)
                {
#if UNITY_IOS && !UNITY_EDITOR
                    await UniTask.Delay(1000); // 최소 1초 이상
#endif
                    IsInitialized = true;
                    Debug.Log("[Firebase] Initialized");
                    await InitializeRemoteConfig();
                }
                else
                {
                    Debug.LogError("[Firebase] Fail Initialized: " + dependencyStatus);
                }
                await UniTask.WaitForSeconds(1f);
            }
            catch (Exception e)
            {
                Debug.LogError("[Firebase] Fail Initialized: " + e.Message);
                IsInitialized = true;
            } 
        }
        
        private static async UniTask InitializeRemoteConfig()
        {
            await UniTask.Yield();
            // try {
            //     Dictionary<string, object> defaults = new Dictionary<string, object>();
            //
            //     defaults.Add("Cabinet_Sprite", default값);
            //
            //     var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            //     await remoteConfig.SetDefaultsAsync(defaults);
            //     await remoteConfig.FetchAsync(TimeSpan.Zero);
            //     await remoteConfig.ActivateAsync();
            //
            //     remoteConfig.GetValue("Cabinet_Sprite").BooleanValue; // 원하는값
            // }
            // catch(Exception e)
            // {
            //     Debug.LogError($"[Firebase] InitializeRemoteConfig Error: {e.Message}");
            // }
        }
    }
}
#endif
