using UnityEngine;
using UnityEngine.InputSystem;

namespace HomeInventory3D.Scene
{
    /// <summary>
    /// Free orbit camera with click-to-focus on any object.
    /// LMB: rotate, RMB: pan, Scroll: zoom, Click object: fly to it, WASD: move.
    /// </summary>
    public class OrbitCamera : MonoBehaviour
    {
        [Header("Orbit")]
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 3f;
        [SerializeField] private float minDistance = 0.3f;
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private float rotationSpeed = 0.3f;
        [SerializeField] private float zoomSpeed = 1f;
        [SerializeField] private float panSpeed = 0.005f;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Smoothing")]
        [SerializeField] private float orbitSmooth = 8f;
        [SerializeField] private float flyToSpeed = 3f;

        private float _yaw;
        private float _pitch = 30f;
        private float _targetDistance;
        private Vector3 _focusPoint;
        private bool _isFlyingTo;
        private Transform _pivot;

        private Mouse _mouse;
        private Keyboard _keyboard;

        private void Start()
        {
            _mouse = Mouse.current;
            _keyboard = Keyboard.current;
            _targetDistance = distance;

            // Create invisible pivot — camera orbits around this, not the actual target object
            var pivotGo = new GameObject("[CameraPivot]");
            DontDestroyOnLoad(pivotGo);
            _pivot = pivotGo.transform;

            if (target != null)
                _pivot.position = target.position;

            _focusPoint = _pivot.position;

            // Initialize angles from current camera orientation
            var dir = (transform.position - _pivot.position).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                _yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                _pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            }
        }

        private void LateUpdate()
        {
            if (_mouse == null) return;

            HandleClickToFocus();
            HandleRotation();
            HandlePan();
            HandleZoom();
            HandleWASD();
            ApplyTransform();
        }

        private void HandleClickToFocus()
        {
            if (!_mouse.leftButton.wasPressedThisFrame) return;

            // Don't focus if dragging (check if mouse moved)
            var ray = Camera.main.ScreenPointToRay(_mouse.position.ReadValue());
            if (Physics.Raycast(ray, out var hit, 100f))
            {
                // Check if it's an interactable object (has ItemController or Renderer)
                var item = hit.collider.GetComponentInParent<ItemController>();
                if (item != null)
                {
                    FlyTo(hit.collider.bounds.center, hit.collider.bounds.extents.magnitude * 3f);
                    Debug.Log($"Camera focusing on: {item.ItemName}");
                    return;
                }

                // Any other object with a renderer
                var renderer = hit.collider.GetComponent<Renderer>();
                if (renderer != null)
                {
                    FlyTo(renderer.bounds.center, renderer.bounds.extents.magnitude * 3f);
                    Debug.Log($"Camera focusing on: {hit.collider.name}");
                }
            }
        }

        private void HandleRotation()
        {
            // Right mouse button — orbit rotation
            if (_mouse.rightButton.isPressed)
            {
                var delta = _mouse.delta.ReadValue();
                _yaw += delta.x * rotationSpeed;
                _pitch -= delta.y * rotationSpeed;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
                _isFlyingTo = false;
            }
        }

        private void HandlePan()
        {
            // Middle mouse button — pan
            if (_mouse.middleButton.isPressed)
            {
                var delta = _mouse.delta.ReadValue();
                var right = transform.right * (-delta.x * panSpeed * distance);
                var up = transform.up * (-delta.y * panSpeed * distance);
                _focusPoint += right + up;
                _isFlyingTo = false;
            }
        }

        private void HandleZoom()
        {
            var scroll = _mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _targetDistance -= scroll * zoomSpeed * 0.005f * _targetDistance;
                _targetDistance = Mathf.Clamp(_targetDistance, minDistance, maxDistance);
            }
        }

        private void HandleWASD()
        {
            if (_keyboard == null) return;

            var move = Vector3.zero;
            if (_keyboard.wKey.isPressed) move += transform.forward;
            if (_keyboard.sKey.isPressed) move -= transform.forward;
            if (_keyboard.aKey.isPressed) move -= transform.right;
            if (_keyboard.dKey.isPressed) move += transform.right;

            if (move.sqrMagnitude > 0.01f)
            {
                _focusPoint += move.normalized * (moveSpeed * Time.deltaTime);
                _isFlyingTo = false;
            }
        }

        private void ApplyTransform()
        {
            // Smooth fly-to
            if (_isFlyingTo)
            {
                _pivot.position = Vector3.Lerp(_pivot.position, _focusPoint, Time.deltaTime * flyToSpeed);
                if (Vector3.Distance(_pivot.position, _focusPoint) < 0.05f)
                    _isFlyingTo = false;
            }
            else
            {
                _pivot.position = Vector3.Lerp(_pivot.position, _focusPoint, Time.deltaTime * orbitSmooth);
            }

            // Smooth zoom
            distance = Mathf.Lerp(distance, _targetDistance, Time.deltaTime * orbitSmooth);

            // Apply orbit — camera moves, not the target objects
            var rotation = Quaternion.Euler(_pitch, _yaw, 0);
            var offset = rotation * new Vector3(0, 0, -distance);

            transform.position = _pivot.position + offset;
            transform.LookAt(_pivot.position);
        }

        /// <summary>
        /// Smoothly fly camera to focus on a world position.
        /// </summary>
        public void FlyTo(Vector3 worldPosition, float newDistance = -1f)
        {
            _focusPoint = worldPosition;
            _isFlyingTo = true;

            if (newDistance > 0)
                _targetDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);
        }

        /// <summary>
        /// Sets the initial orbit target (reads position, doesn't move the object).
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            _focusPoint = newTarget.position;
            if (_pivot != null)
                _pivot.position = newTarget.position;
        }

        /// <summary>
        /// Resets to initial target view.
        /// </summary>
        public void ResetView()
        {
            _yaw = 0f;
            _pitch = 30f;
            _targetDistance = 3f;
            if (target != null)
                _focusPoint = target.position;
        }
    }
}
