using System;
using System.Collections.Generic;
using UnityEngine;

namespace Areung_Plugin.Data.Scripts
{
    [Serializable]
    public class GlobalSave
    {
        [SerializeField] SavedDataContainer[] saveObjects;
        private List<SavedDataContainer> saveObjectsList;
        private float _lastFlushTime = 0;
        public float Time { get; set; }

        
        #region Data Fields
        // 저장할 데이터 이곳에
        private DateTime _lastExitTime;
        public DateTime LastExitTime => _lastExitTime;
        
        private float _gameTime;
        public float GameTime => _gameTime + (Time - _lastFlushTime);
        
        #endregion

        public void Init(float time)
        {
            if (saveObjects == null)
            {
                saveObjectsList = new List<SavedDataContainer>();
            }
            else
            {
                saveObjectsList = new List<SavedDataContainer>(saveObjects);
            }

            for (int i = 0; i < saveObjectsList.Count; i++)
            {
                saveObjectsList[i].Restored = false;
            }

            Time = time;
            _lastFlushTime = Time;
        }

        public void Flush(bool updateLastExitTime)
        {
            saveObjects = saveObjectsList.ToArray();

            for (int i = 0; i < saveObjectsList.Count; i++)
            {
                SavedDataContainer saveObject = saveObjectsList[i];

                saveObject.Flush();
            }

            _gameTime += Time - _lastFlushTime;

            _lastFlushTime = Time;

            if(updateLastExitTime) _lastExitTime = DateTime.Now;
        }

        public T GetSaveObject<T>(int hash) where T : ISaveObject, new()
        {
            SavedDataContainer container = saveObjectsList.Find((container) => container.Hash == hash);

            if (container == null)
            {
                container = new SavedDataContainer(hash, new T());

                saveObjectsList.Add(container);

            }
            else
            {
                if (!container.Restored) container.Restore<T>();
            }
            return (T)container.SaveObject;
        }

        public T GetSaveObject<T>(string uniqueName) where T : ISaveObject, new()
        {
            return GetSaveObject<T>(uniqueName.GetHashCode());
        }

        public void Info()
        {
            foreach (var container in saveObjectsList)
            {
                Debug.Log("Hash: " + container.Hash);
                Debug.Log("Save Object: " + container.SaveObject);
            }
        }
    }
}