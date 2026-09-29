using System;
using System.Collections.Generic;
using UnityEngine;

public class UIPopupManager : MonoBehaviour
{
    public GameObject dim;
    private List<BasePopup> _popupList;
    //[SerializeField] private GameObject dimUI;
    private Dictionary<string, BasePopup> _popupMap = new();
    public int order = 5;
    public Stack<BasePopup> popupStack = new();
    public bool IsInitialized { get; set; } 

    public void Initialized()
    {
        Init();
        RegisterPopups();
    }

    private void Init()
    {
        order = 5;
        dim.SetActive(false);
        DimUISwitch();
        popupStack.Clear();
    }

    private void RegisterPopups()
    {
        _popupList = new List<BasePopup>(GetComponentsInChildren<BasePopup>(includeInactive: true));
        foreach (var popup in _popupList)
        {
            if (popup != null && !_popupMap.ContainsKey(popup.name))
            {
                _popupMap.Add(popup.name, popup);
                popup.gameObject.SetActive(false);
            }
        }

        IsInitialized = true;
        Debug.Log("[UIManager] Popup Register Done!");
    }

    public T GetPopup<T>(string name) where T : BasePopup
    {
        if (_popupMap.TryGetValue(name, out var popup))
        {
            return popup as T;
        }

        Debug.LogWarning($"[UIManager] {name} 팝업이 없습니다.");
        return null;
    }

    public void OpenPopup(string name, bool anime = true)
    {
        if (_popupMap.TryGetValue(name, out var popup))
        {
            popup.Open(anime);
        }
    }

    public void ClosePopup(string name)
    {
        if (_popupMap.TryGetValue(name, out var popup))
        {
            popup.Close();
        }
    }

    public void CloseAllPopups()
    {
        var temp = popupStack.ToArray();
        foreach (var popup in temp)
        {
            popup.gameObject.SetActive(false);
        }
        Init();
    }

    public void DimUISwitch(bool isTurnOn = false)
    {
        //dimUI.SetActive(isTurnOn);
    }
}
