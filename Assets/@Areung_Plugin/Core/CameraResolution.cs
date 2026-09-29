using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Areung_Plugin.Core
{
    public class CameraResolution : MonoBehaviour
    {
        
        private const float MinSize = 7.8f;
        private const float MaxSize = 11f;

        [Header("Camera Zoom Action Properties")]
        [SerializeField] private Ease actionEase = Ease.OutQuad;
        [SerializeField] private float duration = 0.5f;
        
        #region Fields
        public static CameraResolution Instance { get; private set; }
        public Camera Camera => _camera;
        public Camera UICamera { get; private set; }
        private Camera _camera;
        private float _currentScale;
        private float _screenAspect;
        private Tweener _shakeTween;
        private int _index;
        #endregion
        
        private void Awake()
        {
            Instance = this;
            TryGetComponent(out _camera);
            if (_camera.GetUniversalAdditionalCameraData().cameraStack.Count > 0)
            {
                UICamera = _camera.GetUniversalAdditionalCameraData().cameraStack[0];
            }
            Resolution();
        }
        
        private void Resolution()
        {
            _screenAspect = (float)Screen.width / Screen.height;
            _currentScale = _screenAspect switch
            {
                <= 0.38f => MaxSize,
                <= 0.41f => 10f,
                <= 0.42f => 9.8f,
                <= 0.44f => 9.7f,
                <= 0.46f => 9.3f,
                <= 0.466f => 9.2f,
                <= 0.6f => 9f,
                <= 0.76f => 8.5f,
                <= 0.81f => 8f,
                _ => MinSize
            };
            SetSizeTween(_currentScale, null);
        }

        private float GetSize(float min, float max) => (_screenAspect - min) / (max - min);
        
        private void OnEnable()
        {
            ResolutionValidate();
        }

        private async void ResolutionValidate()
        {
#if UNITY_EDITOR
            while (true)
            {
                if (!Mathf.Approximately(_screenAspect, (float)Screen.width / Screen.height)) Resolution();
                await UniTask.Delay(1000);
            }
#endif
        }
        
        private void SetSizeTween(float newSize, Action onComplete)
        {
            if (_camera == null) return;
            
            _camera.DOKill(true);
            if (_camera.orthographic)
            {
                float targetSize = Mathf.Clamp(newSize, MinSize, MaxSize);
                _camera.DOOrthoSize(targetSize, duration).SetEase(actionEase).OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                throw new NotImplementedException("Camera Projection Type is FOV");
                //_camera.DOFieldOfView(targetSize, duration).SetEase(actionEase).OnComplete(() => onComplete?.Invoke());
            }
        }
    }
}
