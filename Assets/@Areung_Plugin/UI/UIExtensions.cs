using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UIExtensions
{
    private static bool _globalClickLock = false;
    public static void ClickButton(this Button button)
    {
        EventSystem.current.SetSelectedGameObject(button.gameObject);

        var eventData = new PointerEventData(EventSystem.current);

        eventData.button = PointerEventData.InputButton.Left;
        button.OnPointerClick(eventData);
    }

    public static void AddEvent(this Component behaviour, EventTriggerType triggerType, Action<PointerEventData> call)
    {
        AddEvent(behaviour.gameObject, triggerType, call);
    }

    public static void AddEvent(this GameObject behaviour, EventTriggerType triggerType, Action<PointerEventData> call)
    {
        EventTrigger trigger = behaviour.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = behaviour.gameObject.AddComponent<EventTrigger>();

        trigger.triggers.RemoveAll(entry => entry.eventID == triggerType);
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = triggerType;
        entry.callback.AddListener((data) =>
        {
            if (_globalClickLock) return; 
            _globalClickLock = true;  
            call((PointerEventData)data); 
            UnlockAfterDelay(0.2f).Forget();
        });

        trigger.triggers.Add(entry);
    }
    
    private static async UniTask UnlockAfterDelay(float delaySeconds)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds));
        _globalClickLock = false;
    }
}