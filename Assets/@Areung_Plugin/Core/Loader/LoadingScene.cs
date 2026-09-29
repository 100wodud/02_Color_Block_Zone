using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Areung_Plugin.Core.Loader
{
    public class LoadingScene : MonoBehaviour
    {
        private async void Awake()
        {
            SceneName targetScene = SceneLoader.TargetScene;
            if (string.IsNullOrEmpty(targetScene.ToString())) return;
            
            if (!Initializer.Instance.AppFirstOpen) await GameStartLoading();
            else await SceneChangeLoading();
            
            SceneLoader.PrepareSceneInit();
            var loadScene = SceneManager.LoadSceneAsync(targetScene.ToString(), LoadSceneMode.Additive);
            if (loadScene == null) return;
            loadScene.allowSceneActivation = false;
            loadScene.allowSceneActivation = true;
            await UniTask.WaitUntil(() => loadScene.isDone);
            await SceneLoader.WaitUntilSceneReady();
            await SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
            SceneLoader.OnSceneLoadedComplete?.Invoke();
        }


        #region Sample Method

        [SerializeField] private GameObject isAppOpen;
        [SerializeField] private GameObject isLoading;
    
        // 씬 전환 애니메이션
        private async UniTask SceneChangeLoading()
        {
            GameManager.Instance.Popup.CloseAllPopups();
            isAppOpen.gameObject.SetActive(true);
            //isLoading.gameObject.SetActive(true);
            await UniTask.Delay(1000);
        }
    
        // 첫 로딩
        private async UniTask GameStartLoading()
        {
            isAppOpen.gameObject.SetActive(true);
            isAppOpen.transform.localScale = Vector3.zero; 
            isAppOpen.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            //isLoading.gameObject.SetActive(false);
            Initializer.Instance.AppFirstOpen = true;
            if (SceneLoader.OnGameStartLoading != null) await SceneLoader.OnGameStartLoading.Invoke();
            await UniTask.Delay(1000);
        }

        #endregion
    
    }
}
