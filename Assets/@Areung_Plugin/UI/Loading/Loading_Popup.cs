using UnityEngine;

public class Loading_Popup : BasePopup
{
    protected override void OnOpen()
    {
        //SDKManager.IsDoingIAP = true;
    }
    
    protected override void OnClose() 
    { 
        //SDKManager.IsDoingIAP = false;
    }
}
