using System;
using Areung_Plugin.Localize;
using Areung_Plugin.Settings;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if ENABLE_ADMOB_SDK
using Areung_Plugin.SDK;
#endif
#if ENABLE_FIREBASE_SDK
using Areung_Plugin.SDK.Firebase;
#endif
#if ENABLE_IN_APP_PURCHASE
using Areung_Plugin.IAP;
#endif

namespace Areung_Plugin.Core
{
    public class Initializer : MonoBehaviour
    {
        public static Initializer Instance { get; private set; }
        public Camera mainCamera;
#if ENABLE_IN_APP_PURCHASE
        public IAPManager IAP;
#endif
        public bool AppFirstOpen  { get; set; } = false;

        #region Initialize

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            ConfigureLogger();
            Initialized();
        }

        private void Initialized()
        {
            SceneLoader.LoadSceneWithLoading(SceneName.GameScene,null, OnLoadingLoadBefore, OnSceneLoadedComplete);
        }
        
        private void ConfigureLogger()
        {
#if !UNITY_EDITOR
            //Debug.unityLogger.logEnabled = false;
#else
            Debug.unityLogger.logEnabled = true;
#endif
        }

        #endregion

        private async UniTask OnLoadingLoadBefore()
        {
            PlayerData.Initialized();
            await UniTask.WaitUntil(()=> PlayerData.IsInitialized);
            
            Setting.Initialized();
            await UniTask.WaitUntil(()=> Setting.IsInitialized);
            
            // LocalizeInitializer.Initialized();
            // await UniTask.WaitUntil(()=> LocalizeInitializer.IsInitialized);
            
            #region SDK & IAP

#if ENABLE_ADMOB_SDK
            SDKManager.Initialized();
            await UniTask.WaitUntil(()=> SDKManager.IsInitialized);
#endif
            
#if ENABLE_FIREBASE_SDK
            await FirebaseInitializer.Initialized();
            await UniTask.Yield();
#endif

#if ENABLE_IN_APP_PURCHASE
            //await IAP.Initialized();
#endif
    
            #endregion

            await GameManager.Instance.Initialized();
            
            if(SDKManager.IsInitialized) SDKManager.ShowBanner();
        }
        
        private void OnSceneLoadedComplete()
        {
            Debug.Log("GameScene 왔다능");
        }
    }
}
