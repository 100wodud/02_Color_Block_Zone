using System;

public static class SettingDataEvent
{
    public static event Action VibeValueChange;
    public static event Action BGMValueChange;
    public static event Action SFXValueChange;

    public static void InvokeVibe() => VibeValueChange?.Invoke();
    public static void InvokeBGM() => BGMValueChange?.Invoke();
    public static void InvokeSFX() => SFXValueChange?.Invoke();

}