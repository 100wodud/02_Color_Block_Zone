using DG.Tweening;
using UnityEngine;

public abstract class BasePopup : MonoBehaviour
{
    private bool isOpen = false;
    private bool isClose = false;
    
    #region Open
    public void Open(bool anime = true)
    {
        if(isOpen) return;
        isOpen = true;
        //GameManager.Instance.Popup.DimUISwitch();
        gameObject.SetActive(true);
        AddStackPopup();
        if (anime)
        {
            // 사운드
            //
            transform.localScale = Vector3.zero;
            PlayOpenAnimation();
        }
        //if (SceneLoader.TargetScene == SceneName.GameScene)
        OnOpen();
        isOpen = false;
    }
    
    private void AddStackPopup()
    {
        var uiPopup = GameManager.Instance.Popup;
        uiPopup.dim.SetActive(true);
        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = uiPopup.order++;
        }

        if (uiPopup.popupStack.Contains(this) == false)
        {
            if (uiPopup.popupStack.Count > 0)
            {
                var previous = uiPopup.popupStack.Peek();
                previous.gameObject.SetActive(false); 
            }
            uiPopup.popupStack.Push(this);
        }
    }
    
    private void PlayOpenAnimation()
    {
        transform.DOScale(1.2f, 0.15f).OnComplete(() =>
        {
            transform.DOScale(1.0f, 0.1f);
        });
    }
    
    protected abstract void OnOpen();

    #endregion

    #region Close

    public void Close()
    {
        if(isClose) return;
        isClose = true;
        GameManager.Instance.Popup.DimUISwitch();
        
        
        PlayCloseAnimation();
        var uiPopup = GameManager.Instance.Popup;
        if (uiPopup.popupStack.Count > 0 && uiPopup.popupStack.Peek() == this)
        {
            uiPopup.popupStack.Pop();
            uiPopup.order--;
        }

        if (uiPopup.popupStack.Count > 0)
        {
            var previous = uiPopup.popupStack.Peek();
            previous.Open(); 
        }
        isClose = false;
    }
    

    private void PlayCloseAnimation()
    {
        transform.DOScale(0f, 0.1f).OnComplete(() =>
        {
            gameObject.SetActive(false);
            transform.localScale = Vector3.one;
            OnClose();
            if (GameManager.Instance.Popup.popupStack.Count <= 0) GameManager.Instance.Popup.dim.SetActive(false);
        });
    }

    protected abstract void OnClose();

    #endregion
}
