using System;
using Areung_Plugin.Data.Scripts;
using JetBrains.Annotations;
using UnityEngine;

public static class PlayerData
{
    public static bool IsInitialized { get; private set; }
    private static PlayerDataSave _save;

    public static void Initialized()
    {
        SaveController.Init();
        _save = SaveController.GetSaveObject<PlayerDataSave>("PlayerDataSave");
        IsInitialized = true;
        Debug.Log("[PlayerData] Initialized");
    }

    private static void SetAndSave<T>([NotNull] ref T field, T value, Action<T> saveSetter, Action eventCallback = null)
    {
        if (field == null) throw new ArgumentNullException(nameof(field));
        field = value;
        saveSetter(value);
        SaveController.MarkAsSaveIsRequired();
        eventCallback?.Invoke();
    }
    public static bool AdsRemove
    {
        get => _save.adsRemove;
        set => SetAndSave(ref _save.adsRemove, value, v => _save.adsRemove = v);
    }

    public static int ClearLevel
    {
        get => _save.clearLevel;
        set => SetAndSave(ref _save.clearLevel, value, v => _save.clearLevel = v, PlayerDataEvent.InvokeLevel);
    }

    public static int BestScore
    {
        get => _save.bestScore;
        set => SetAndSave(ref _save.bestScore, value, v => _save.bestScore = v);
    }

    public static int Gold
    {
        get => _save.gold;
        set => SetAndSave(ref _save.gold, value, v => _save.gold = v, PlayerDataEvent.InvokeGold);
    }
}