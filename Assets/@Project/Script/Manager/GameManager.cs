using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region Instance
    
    public static GameManager Instance { get; set; }
    public UIPopupManager Popup { get; set; }
    public ProviderManager Provider { get; set; }

    #endregion

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async UniTask Initialized()
    {
        Popup = GetComponent<UIPopupManager>();
        Provider = GetComponent<ProviderManager>();
        Popup.Initialized();
        await UniTask.WaitUntil(() => Popup.IsInitialized);
        Provider.Initialized();
    }
}