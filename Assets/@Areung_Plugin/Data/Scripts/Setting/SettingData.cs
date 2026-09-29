using System;
using Areung_Plugin.Data.Scripts;
using JetBrains.Annotations;

public static class SettingData
{
    public static bool IsInitialized { get; private set; }
    private static SettingDataSave _save;

    public static void Init()
    {
        SaveController.Init();
        _save = SaveController.GetSaveObject<SettingDataSave>("SettingDataSave");
        IsInitialized = true;
    }


    private static void SetAndSave<T>([NotNull] ref T field, T value, Action<T> saveSetter, Action eventCallback = null)
    {
        if (field == null) throw new ArgumentNullException(nameof(field));
        field = value;
        saveSetter(value);
        SaveController.MarkAsSaveIsRequired();
        eventCallback?.Invoke();
    }
    
    public static bool Haptic
    {
        get => _save.haptic;
        set => SetAndSave(ref _save.haptic, value, v => _save.haptic = v);
    }
    
    public static bool BGM
    {
        get => _save.bgm;
        set => SetAndSave(ref _save.bgm, value, v => _save.bgm = v);
    }
    
    public static bool SFX
    {
        get => _save.sfx;
        set => SetAndSave(ref _save.sfx, value, v => _save.sfx = v);
    }
}
