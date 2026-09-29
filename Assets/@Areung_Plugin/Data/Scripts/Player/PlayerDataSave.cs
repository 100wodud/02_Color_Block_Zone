using Areung_Plugin.Data.Scripts;

[System.Serializable]
public class PlayerDataSave : ISaveObject
{
    public bool adsRemove = false;
    
    public int clearLevel = 0;
    public int bestScore = 0;
    public int gold = 0;

    
    public void Flush() { }
}