using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_IN_APP_PURCHASE
using UnityEngine.Purchasing;
#endif
using UnityEngine.UI;

public class Notice_Popup : BasePopup
{
    private string message;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button exit;
    protected override void OnOpen()
    {
        SetUI();
    }

    protected override void OnClose() { }
    
    private void SetUI()
    {
        exit.AddEvent(EventTriggerType.PointerClick, (data) => Close());
        messageText.text = message;
    }
    
    public void EndMessage(string msg = null)
    {
        message = $"{msg}";
    }

#if ENABLE_IN_APP_PURCHASE
    public void SetMessage(Product product = null, string msg = null)
    {
        message = $"{product?.metadata.localizedTitle}\n{msg}";
    }
#endif
}
