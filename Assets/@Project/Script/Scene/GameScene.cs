using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(StageController))]
public class GameScene : MonoBehaviour
{
    public static GameScene Instance { get; private set; }
    public StageController Stage { get; private set; }

    [SerializeField] private GameUI _gameUI;

    private async void Awake()
    {
        Instance = this;
        Stage = GetComponent<StageController>();
        await Initialized();
    }

    private async UniTask Initialized()
    {
        if (_gameUI != null) _gameUI.Initialize();
        Stage.StartGame();
        await UniTask.Yield();
        SceneLoader.OnSceneLoadReady();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
