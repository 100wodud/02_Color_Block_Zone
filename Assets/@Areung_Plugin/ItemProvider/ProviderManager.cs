using System;
using System.Collections;
using System.Collections.Generic;
using Areung_Plugin.ItemProvider;
using Areung_Plugin.Settings;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[System.Serializable]
public class ProviderTypeInfo
{
    public ProviderType type;
    public Sprite sprite;
    public RectTransform endPos;
    public AudioLibrarySounds clip;
}

public class ProviderManager : MonoBehaviour
{
    [SerializeField] private GameObject uiPrefab; 
    [SerializeField] private RectTransform canvasRoot; 
    [SerializeField] private List<ProviderTypeInfo> providerList;

    private Dictionary<ProviderType, ProviderTypeInfo> _provider;
    public bool IsInitialized { get; set; }
    
    public void Initialized()
    {
        _provider = new Dictionary<ProviderType, ProviderTypeInfo>();
        foreach (var item in providerList)
        {
            if (!_provider.ContainsKey(item.type))
                _provider.Add(item.type, item);
        }
        IsInitialized = true;
    }
    
    public void ProvideItem(ProviderType type, int value, Action callback = null, RectTransform targetUI = null, Vector2? startPos = null, Transform startTransform = null, float scale = 1f) 
    {
        if (!_provider.TryGetValue(type, out var info))
        {
            Debug.LogWarning($"ProviderType {type} not found.");
            return;
        }

        RectTransform endPos = targetUI ? targetUI : info.endPos;
        
        
        Vector2 spawnPos = startPos ?? new Vector2(Screen.width / 2f, Screen.height / 2f); 
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, spawnPos, null, out Vector2 localStart);
        
        Vector3 worldCenter = endPos.TransformPoint(endPos.rect.center);
        Vector3 screenCenter = RectTransformUtility.WorldToScreenPoint(null, worldCenter);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, screenCenter, null, out Vector2 localEnd);
        for (int i = 0; i < value; i++)
        {
            GameObject item = Instantiate(uiPrefab, canvasRoot);
            RectTransform rt = item.GetComponent<RectTransform>();
            rt.anchoredPosition = localStart;

            Image img = item.GetComponent<Image>();
            if (img != null)
                img.sprite = info.sprite;

            Vector2 target = localEnd;
            StartCoroutine(MoveUIItem(rt, target, i * 0.05f, callback, info, scale));
        }
    }

    #region Item Move

    private IEnumerator MoveUIItem(RectTransform item, Vector2 target, float delay, Action callback, ProviderTypeInfo info, float scale = 1f)
    {
        yield return new WaitForSecondsRealtime(delay);

        float popDuration = 0.1f;
        float duration = 0.2f;
        Vector2 start = item.anchoredPosition;
        Vector2 randomOffset = Random.insideUnitCircle * 30f;
        Vector2 bounceTarget = start + randomOffset;

        item.localScale = Vector3.zero* scale;

        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            item.anchoredPosition = Vector2.Lerp(start, bounceTarget, t);
            item.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 0.8f * scale, t);
            yield return null;
            yield return null;
        }

        item.anchoredPosition = bounceTarget;
        item.localScale = Vector3.one * 0.8f * scale;

        elapsed = 0f;
        yield return new WaitForSecondsRealtime(0.4f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            item.anchoredPosition = Vector2.Lerp(bounceTarget, target, t);
            item.localScale = Vector3.Lerp(Vector3.one * 0.8f * scale, Vector3.one * scale, t); 
            yield return null;
        }

        item.anchoredPosition = target;
        item.localScale = Vector3.one *  scale;
        Setting.HapticWeak();
        Setting.PlaySFX(info.clip, 0.25f);
        callback?.Invoke();
        Destroy(item.gameObject, 0.1f);
    }

    #endregion

    public Sprite GetProviderTypeImage(ProviderType type)
    {
        return _provider[type].sprite;
    }

    public void ClearAllProviderItems()
    {
        foreach (Transform child in canvasRoot)
        {
            Destroy(child.gameObject);
        }
    }
}