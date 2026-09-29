using System;

public static class PlayerDataEvent
{
    public static event Action LevelValueChange;
    public static event Action GoldValueChange;

    public static void InvokeLevel() => LevelValueChange?.Invoke();
    public static void InvokeGold() => GoldValueChange?.Invoke();

}