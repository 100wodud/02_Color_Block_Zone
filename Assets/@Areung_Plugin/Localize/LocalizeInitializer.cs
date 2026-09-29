using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Areung_Plugin.Localize
{
    public static class LocalizeInitializer
    {
        public static bool IsInitialized;
        private static LocalizeProvider _provider;

        private static bool _isInitializing;

        public static async void Initialized()
        {
            if (IsInitialized || _isInitializing) return;
            _isInitializing = true;

            ResourceRequest request = Resources.LoadAsync<LocalizeProvider>("SettingSO/LocalizeProvider");
            await request.ToUniTask();

            _provider = request.asset as LocalizeProvider;

            if (_provider == null)
            {
                Debug.LogError("[Localized] LocalizeProvider Resources Load Fail!");
                IsInitialized = true;
                return;
            }

            await _provider.InitProvider();
            IsInitialized = true;
            Debug.Log("[Localized] Initialized");
        }
    }
}