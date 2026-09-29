using Areung_Plugin.Data.Scripts;

[System.Serializable]
public class SettingDataSave : ISaveObject
{
    public bool haptic = true;
    public bool bgm = true;
    public bool sfx = true;

    public void Flush() { }
}
