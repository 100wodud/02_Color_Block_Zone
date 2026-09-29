using UnityEngine;

namespace Areung_Plugin.Data.Scripts
{
    [System.Serializable]
    public class SavedDataContainer
    {
        [SerializeField] int hash;
        public int Hash => hash;

        [SerializeField] string json;
        public bool Restored { get; set; }

        [System.NonSerialized] ISaveObject _saveObject;
        public ISaveObject SaveObject => _saveObject;

        public SavedDataContainer(int hash, ISaveObject saveObject)
        {
            this.hash = hash;
            _saveObject = saveObject;
            Restored = true;
        }

        public void Flush()
        {
            if (_saveObject != null) _saveObject.Flush();
            if (Restored) json = JsonUtility.ToJson(_saveObject);
        }

        public void Restore<T>() where T : ISaveObject
        {
            _saveObject = JsonUtility.FromJson<T>(json);
            Restored = true;
        }
    }
}