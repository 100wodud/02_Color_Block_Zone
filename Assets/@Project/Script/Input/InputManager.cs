using System;
using System.Collections.Generic;
using Areung_Plugin.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Areung_Plugin.Input
{
    public enum PickMode
    {
        Physics2D,
        Physics3D
    }

    [AddComponentMenu("Areung/Input/Input Manager")]
    public sealed class InputManager : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Camera _camera;

        [Header("Pick")]
        [SerializeField] private PickMode pickMode = PickMode.Physics2D; //대상 선택 방식. Physics2D = OverlapPoint, Physics3D = 카메라 레이캐스트.
        [SerializeField] private LayerMask pickMask = ~0; //선택 대상으로 검사할 레이어
        [SerializeField] private string requiredTag = ""; //태그만 대상으로 인식

        [Header("Gesture")]
        [SerializeField] private float dragThreshold = 8f; //픽셀 이상 움직여야 드래그로 판정

        [Header("UI")]
        [SerializeField] private bool blockWhenOverUI = true; //UI 위에서 시작한 입력을 무시

        public bool allowInput = true;
        public Func<bool> CanInteract;

        public event Action<Component, Vector3> Selected;
        public event Action<Vector3> Dragged;
        public event Action<Component, Vector3> Released;
        public event Action<Component, Vector3> Tapped;

        private IPointerTargetResolver _resolver;
        private InputAction _press;
        private InputAction _point;

        private Component _current;
        private Vector2 _pressScreen;
        private bool _pressActive;
        private bool _moved;

        private PointerEventData _uiPointer;
        private readonly List<RaycastResult> _uiResults = new();

        public Camera Camera
        {
            get => _camera;
            set => _camera = value;
        }

        // Shared main camera from the boot Initializer (Camera.main fallback for edit mode / pre-boot).
        private static Camera MainCamera => Initializer.Instance != null ? Initializer.Instance.mainCamera : Camera.main;

        public IPointerTargetResolver Resolver
        {
            get => _resolver;
            set => _resolver = value;
        }

        public Component Current => _current;

        private void Reset() => _camera = MainCamera;

        private void Awake()
        {
            if (_camera == null) _camera = MainCamera;
            _resolver ??= pickMode == PickMode.Physics2D
                ? new Physics2DTargetResolver(pickMask, requiredTag)
                : (IPointerTargetResolver)new Physics3DTargetResolver(pickMask, requiredTag);
        }

        private void OnEnable()
        {
            _press = new InputAction("press", InputActionType.Button, "<Pointer>/press");
            _point = new InputAction("point", InputActionType.Value, "<Pointer>/position");

            _press.started += OnPressStarted;
            _press.canceled += OnPressCanceled;
            _point.performed += OnPointMoved;

            _press.Enable();
            _point.Enable();
        }

        private void OnDisable()
        {
            if (_pressActive) ForceCancel();

            if (_press != null)
            {
                _press.started -= OnPressStarted;
                _press.canceled -= OnPressCanceled;
                _press.Disable();
                _press.Dispose();
                _press = null;
            }

            if (_point != null)
            {
                _point.performed -= OnPointMoved;
                _point.Disable();
                _point.Dispose();
                _point = null;
            }
        }

        public void ForceCancel()
        {
            if (_current != null)
                (_current as IDragTarget)?.OnDeselect(ReadPointer().World);

            _current = null;
            _pressActive = false;
            _moved = false;
        }

        private bool Gate()
        {
            if (!allowInput) return false;
            if (CanInteract != null && !CanInteract()) return false;
            return true;
        }

        private PointerInput ReadPointer()
        {
            Vector2 screen = Pointer.current != null
                ? Pointer.current.position.ReadValue()
                : _point.ReadValue<Vector2>();
            Vector3 world = _camera.ScreenToWorldPoint(
                new Vector3(screen.x, screen.y, Mathf.Abs(_camera.transform.position.z)));
            Ray ray = _camera.ScreenPointToRay(screen);
            return new PointerInput(screen, world, ray);
        }

        private void OnPressStarted(InputAction.CallbackContext _)
        {
            if (!Gate()) return;

            var pointer = ReadPointer();
            if (blockWhenOverUI && IsOverUI(pointer.Screen)) return;

            if (!_resolver.TryResolve(pointer, out _current) || _current == null)
            {
                _current = null;
                return;
            }

            _pressActive = true;
            _pressScreen = pointer.Screen;
            _moved = false;

            (_current as IDragTarget)?.OnSelect(pointer.World);
            Selected?.Invoke(_current, pointer.World);
        }

        private void OnPointMoved(InputAction.CallbackContext _)
        {
            if (!_pressActive || _current == null) return;

            var pointer = ReadPointer();

            if (!_moved)
            {
                float sq = (pointer.Screen - _pressScreen).sqrMagnitude;
                if (sq < dragThreshold * dragThreshold) return;
                _moved = true;
            }

            (_current as IDragTarget)?.OnMove(pointer.World);
            Dragged?.Invoke(pointer.World);
        }

        private void OnPressCanceled(InputAction.CallbackContext _)
        {
            if (!_pressActive)
            {
                _current = null;
                return;
            }

            var pointer = ReadPointer();

            if (_current != null)
            {
                (_current as IDragTarget)?.OnDeselect(pointer.World);

                if (_moved)
                {
                    Released?.Invoke(_current, pointer.World);
                }
                else
                {
                    (_current as ITapTarget)?.OnTap(pointer.World);
                    Tapped?.Invoke(_current, pointer.World);
                }
            }

            _current = null;
            _pressActive = false;
            _moved = false;
        }

        private bool IsOverUI(Vector2 screen)
        {
            if (EventSystem.current == null) return false;

            _uiPointer ??= new PointerEventData(EventSystem.current);
            _uiPointer.position = screen;
            _uiResults.Clear();
            EventSystem.current.RaycastAll(_uiPointer, _uiResults);
            return _uiResults.Count > 0;
        }
    }
}
