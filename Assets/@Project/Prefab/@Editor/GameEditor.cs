using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameEditor : MonoBehaviour
{
    #region Editor Field

    [Header("Editor")]
    [SerializeField] private Button editorBtn;
    [SerializeField] private GameObject editor; // 최종 개발자 UI
    [SerializeField] private TMP_InputField stageInputfield;
    
    [Header("Secret Settings")]
    [SerializeField] private GameObject passwordPopupUI; 
    [SerializeField] private TMP_InputField passwordInput;
    private string secretPassword = "0304125";
    [SerializeField] private int requiredTaps = 10;
    [SerializeField] private float timeWindowSeconds = 3f;

    private int _tapCount;
    private float _windowStartTime;
    private bool _isUnlocked; // 세션 동안 잠금 해제 여부

    private void Awake()
    {
        _isUnlocked = false;
        if (editorBtn != null)
        {
            editorBtn.onClick.AddListener(OnEditorBtnClicked);
        }
        
        // 초기 상태 설정
        if(editor != null) editor.SetActive(false);
        if(passwordPopupUI != null) passwordPopupUI.SetActive(false);
    }

    private void OnEditorBtnClicked()
    {
        // 이미 해제된 상태라면 바로 에디터 토글
        if (_isUnlocked)
        {
            editor.SetActive(!editor.activeSelf);
            return;
        }

        float currentTime = Time.unscaledTime;

        // 타임 윈도우 체크: 첫 클릭이거나 제한 시간을 초과했으면 리셋
        if (_tapCount == 0 || currentTime - _windowStartTime > timeWindowSeconds)
        {
            _tapCount = 1;
            _windowStartTime = currentTime;
            return;
        }

        _tapCount++;

        // 조건 충족 시 패스워드 팝업 오픈
        if (_tapCount >= requiredTaps)
        {
            OpenPasswordPopup();
            _tapCount = 0; // 카운트 리셋
        }
    }

    private void OpenPasswordPopup()
    {
        if (passwordPopupUI != null)
        {
            passwordPopupUI.SetActive(true);
            
            if (passwordInput != null)
            {
                passwordInput.text = string.Empty;
                passwordInput.ActivateInputField(); 
            }
        }
    }

    // UI의 '확인' 버튼이나 엔터키(OnEndEdit)에 연결
    public void CheckPassword()
    {
        if (passwordInput == null) return;

        if (passwordInput.text == secretPassword)
        {
            UnlockFinalDevMode();
        }
        else
        {
            Debug.Log("비밀번호 불일치!");
            passwordInput.text = string.Empty;
            if (passwordPopupUI != null) passwordPopupUI.SetActive(false);
        }
    }

    private void UnlockFinalDevMode()
    {
        _isUnlocked = true;
        if (passwordPopupUI != null) passwordPopupUI.SetActive(false);
        if (editor != null) editor.SetActive(true);
    }

    public void LoadIndexStage()
    {
        if (int.TryParse(stageInputfield.text, out var index))
        {
            PlayerData.ClearLevel = Mathf.Max(0, index-1);
            ReloadGame();
        }
    }

    public void AdsRemove()
    {
        PlayerData.AdsRemove = true;
    }
    
    #endregion
    
    #region Editor Actions

    public void PreviousStage()
    {
        PlayerData.ClearLevel = Mathf.Max(0, PlayerData.ClearLevel - 1);
        ReloadGame();
    }

    public void NextStage()
    {
        PlayerData.ClearLevel++;
        ReloadGame();
    }

    public void AddGold()
    {
        PlayerData.Gold += 10000;
    }

    public void StageClear()
    {
        if (SceneLoader.TargetScene == SceneName.GameScene && GameScene.Instance != null)
            GameScene.Instance.Stage.DebugClearBoard();
    }

    private void ReloadGame()
    {
        SceneLoader.LoadSceneWithLoading(SceneName.GameScene);
    }

    #endregion
}