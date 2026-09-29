using System.Collections;
using System.Threading;
using Areung_Plugin.Data.Scripts.Wrappers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Areung_Plugin.Data.Scripts
{
    public static class SaveController
    {
        #region Save Fields
    
        private const string SAVE_FILE_NAME = "save";
        public static GlobalSave GlobalSave;
        // Debug.Log(SaveController.GlobalSave.LastExitTime);
        private static bool _isSaveLoaded;
        private static bool _isSaveRequired;

        #endregion

        #region Save & Load
    
#if UNITY_EDITOR
        public static void EditorInit()
        {
            if (_isSaveLoaded) return;

            Serializer.Init();
            Load(Time.realtimeSinceStartup);
        }
#endif
    
        public static void Init(float autoSaveDelay = 0f, float overrideTime = -1f)
        {
            if (_isSaveLoaded) return;

            Serializer.Init();

            GameObject saveCallbackReceiver = new GameObject("[SAVE CALLBACK RECEIVER]")
            {
                hideFlags = HideFlags.HideInHierarchy
            };

            Object.DontDestroyOnLoad(saveCallbackReceiver);

            UnityCallbackReceiver unityCallbackReceiver = saveCallbackReceiver.AddComponent<UnityCallbackReceiver>();

            Load(Time.time);

            if (autoSaveDelay > 0)
            {
                // Enable auto-save coroutine
                unityCallbackReceiver.StartCoroutine(AutoSaveCoroutine(autoSaveDelay));
            }
        }

        private static T GetSaveObject<T>(int hash) where T : ISaveObject, new()
        {
            if (!_isSaveLoaded)
            {
                Debug.LogError("Save controller has not been initialized");
                return default;
            }

            return GlobalSave.GetSaveObject<T>(hash);
        }

        public static T GetSaveObject<T>(string uniqueName) where T : ISaveObject, new()
        {
            return GetSaveObject<T>(uniqueName.GetHashCode());
        }

        private static void InitClear(float time)
        {
            GlobalSave = new GlobalSave();
            GlobalSave.Init(time);

            Debug.Log("[Save Controller]: Created clear save!");

            _isSaveLoaded = true;
        }

        private static void Load(float time)
        {
            if (_isSaveLoaded)
                return;

            // Try to read and deserialize file or create new one
            GlobalSave = BaseSaveWrapper.ActiveWrapper.Load(SAVE_FILE_NAME);

            GlobalSave.Init(time);

            Debug.Log("[Save Controller]: Save is loaded!");

            _isSaveLoaded = true;
        }

        public static void Save(bool forceSave = false, bool useThreads = true)
        {
            if (!forceSave && !_isSaveRequired) return;
            if (GlobalSave == null) return;

            GlobalSave.Flush(true);

            BaseSaveWrapper saveWrapper = BaseSaveWrapper.ActiveWrapper;
            if(useThreads && saveWrapper.UseThreads())
            {
                Thread saveThread = new Thread(() => BaseSaveWrapper.ActiveWrapper.Save(GlobalSave, SAVE_FILE_NAME));
                saveThread.Start();
            }
            else
            {
                BaseSaveWrapper.ActiveWrapper.Save(GlobalSave, SAVE_FILE_NAME);
            }

            Debug.Log("[Save Controller]: Game is saved!");

            _isSaveRequired = false;
        }
    
        public static void MarkAsSaveIsRequired()
        {
            _isSaveRequired = true;
        }

        private static IEnumerator AutoSaveCoroutine(float saveDelay)
        {
            WaitForSeconds waitForSeconds = new WaitForSeconds(saveDelay);

            while (true)
            {
                yield return waitForSeconds;

                Save();
            }
            // ReSharper disable once IteratorNeverReturns
        }

        public static void DeleteSaveFile()
        {
            BaseSaveWrapper.ActiveWrapper.Delete(SAVE_FILE_NAME);
        }

        private class UnityCallbackReceiver : MonoBehaviour
        {
            private void OnDestroy()
            {
#if UNITY_EDITOR
                Save(true);
#endif
            }

            private void OnApplicationFocus(bool focus)
            {
#if !UNITY_EDITOR
            if(!focus) Save();
#endif
            }
        }
    
        #endregion

    }
}