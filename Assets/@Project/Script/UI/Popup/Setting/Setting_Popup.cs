using Areung_Plugin.Core;
using Areung_Plugin.SDK;
using Areung_Plugin.SDK.AdMob;
using Areung_Plugin.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Setting_Popup : BasePopup
{
    [SerializeField] private Button exit;
    
    [SerializeField] private Button haptic;
    [SerializeField] private Button sound;
    [SerializeField] private Button bgm;

    [Header("Contact")]
    [SerializeField] private Button policy;
    [SerializeField] private Button privateBtn;

    [Header("Game")] 
    [SerializeField] private Button retry;
    //[SerializeField] private Button restoreBtn;
    [SerializeField] private Button contactBtn;

    private bool _busy;
    #region Button Init

    protected override void OnOpen()
    {
        _busy = false;
        SetBtn();
        Initialized();
    }

    protected override void OnClose() { }
    
    private void Initialized()
    {
        DeviceButtonState("sound");
        DeviceButtonState("bgm");
        DeviceButtonState("haptic");
        //VersionInitialize();
    }
    
    private void SetBtn()
    {
#if UNITY_IOS && ENABLE_IN_APP_PURCHASE
        restoreBtn.gameObject.SetActive(true);
        restoreBtn.AddEvent(EventTriggerType.PointerClick,(data) => IAPRestore());
#endif
        sound.AddEvent(EventTriggerType.PointerClick,(data) => OnClickSFXButton());
        bgm.AddEvent(EventTriggerType.PointerClick,(data) => OnClickBGMButton());
        haptic.AddEvent(EventTriggerType.PointerClick,(data) => OnClickHapticButton());
        
        exit.AddEvent(EventTriggerType.PointerClick,(data) => Close());
        retry.AddEvent(EventTriggerType.PointerClick,(data) => OnClickRefresh());
        policy.AddEvent(EventTriggerType.PointerClick,(data) => OpenPolicy());
        privateBtn.AddEvent(EventTriggerType.PointerClick,(data) => OpenPrivate());
        contactBtn.AddEvent(EventTriggerType.PointerClick,(data) => ContactUs());
    }
    
    private void OpenPolicy()
        => Application.OpenURL(DefaultKey.TERMS_SERVICE_URL);
    private void OpenPrivate()
        => Application.OpenURL(DefaultKey.PRIVATE_POLICY_URL);
    
    private void ContactUs()
    {
        ContactUs contactUs = new ContactUs();
        contactUs.Contact();
    }

    private void IAPRestore()
    {
#if ENABLE_IN_APP_PURCHASE
        Initializer.Instance.IAP?.IAPRestore();
#endif
    }

    
    private void OnClickRefresh()
    {
        
        if (_busy) return;
        _busy = true;

        void Load() => SceneLoader.LoadSceneWithLoading(SceneName.GameScene);

        if (SDKManager.IsInitialized && SDKManager.InterstitialCondition(AdsKey.CommonCooldown))
            SDKManager.ShowInterstitial(action: Load, failAction: Load, key: AdsKey.CommonCooldown);
        else Load();
    }

    #endregion

    #region Device Setting

    private void DeviceButtonState(string value)
    {
        switch (value)
        {
            case "sound":
                sound.transform.Find("On").gameObject.SetActive(Setting.SetSFX);
                sound.transform.Find("Off").gameObject.SetActive(!Setting.SetSFX);
                break;
            case "bgm":
                bgm.transform.Find("On").gameObject.SetActive(Setting.SetBGM);
                bgm.transform.Find("Off").gameObject.SetActive(!Setting.SetBGM);
                break;
            case "haptic":
                haptic.transform.Find("On").gameObject.SetActive(Setting.SetHaptic);
                haptic.transform.Find("Off").gameObject.SetActive(!Setting.SetHaptic);
                break;
        }
    }
    
    
    private void OnClickSFXButton()
    {
        Setting.SetSFX = !Setting.SetSFX;
        sound.transform.Find("On").gameObject.SetActive(Setting.SetSFX);
        sound.transform.Find("Off").gameObject.SetActive(!Setting.SetSFX);
    }

    private void OnClickBGMButton()
    {
        Setting.SetBGM = !Setting.SetBGM;
        bgm.transform.Find("On").gameObject.SetActive(Setting.SetBGM);
        bgm.transform.Find("Off").gameObject.SetActive(!Setting.SetBGM);
    }

    private void OnClickHapticButton()
    {
        Setting.SetHaptic = !Setting.SetHaptic;
        haptic.transform.Find("On").gameObject.SetActive(Setting.SetHaptic);
        haptic.transform.Find("Off").gameObject.SetActive(!Setting.SetHaptic);
    }

    #endregion

    #region Info Data

    // private void VersionInitialize()
    // {
    //     string versionText = $"{Application.platform} Ver {Application.version}";
    //     version.text = versionText;
    // }
    
    #endregion
   

}
