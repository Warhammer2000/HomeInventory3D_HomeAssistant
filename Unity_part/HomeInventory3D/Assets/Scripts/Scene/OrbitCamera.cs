using UnityEngine;
using UnityEngine.InputSystem;

namespace HomeInventory3D.Scene
{
    /// <summary>
    /// Orbit camera controller for inspecting the container.
    /// Uses new Input System. Supports mouse drag to rotate, scroll to zoom, touch pinch.
    /// </summary>
    public class OrbitCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 2f;
        [SerializeField] private float minDistance = 0.5f;
        [SerializeField] private float maxDistance = 10f;
        [SerializeField] private float rotationSpeed = 0.3f;
        [SerializeField] private float zoomSpeed = 0.5f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private float smoothTime = 0.1f;

        private float _yaw;
        private float _pitch = 30f;
        private float _currentDistance;
        private float _velocityDistance;

        private Mouse _mouse;
        private Touchscreen _touch;

        private void Start()
        {
            _currentDistance = distance;
            _mouse = Mouse.current;
            _touch = Touchscreen.current;

            if (target == null)
            {
                var center = new GameObject("[OrbitTarget]");
                center.transform.position = Vector3.zero;
                target = center.transform;
            }

            var angles = transform.eulerAngles;
            _yaw = angles.y;
            _pitch = angles.x;
        }

        private void LateUpdate()
        {
            HandleInput();
            ApplyTransform();
        }

        private void HandleInput()
        {
            HandleMouseInput();
            HandleTouchInput();
        }

        private void HandleMouseInput()
        {
            if (_mouse == null) return;

            // Rotation — right mouse button drag
            if (_mouse.rightButton.isPressed)
            {
                var delta = _mouse.delta.ReadValue();
                _yaw += delta.x * rotationSpeed;
                _pitch -= delta.y * rotationSpeed;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            }

            // Zoom — scroll wheel
            var scroll = _mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance -= scroll * zoomSpeed * 0.01f;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }
        }

        private void HandleTouchInput()
        {
            if (_touch == null) return;

            var touches = _touch.touches;

            // Single finger drag — rotate
            if (touches.Count == 1 && touches[0].phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                var delta = touches[0].delta.ReadValue();
                _yaw += delta.x * rotationSpeed;
                _pitch -= delta.y * rotationSpeed;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            }

            // Two finger pinch — zoom
            if (touches.Count >= 2)
            {
                var t0 = touches[0];
                var t1 = touches[1];

                var t0Pos = t0.position.ReadValue();
                var t1Pos = t1.position.ReadValue();
                var t0Delta = t0.delta.ReadValue();
                var t1Delta = t1.delta.ReadValue();

                var prevDist = ((t0Pos - t0Delta) - (t1Pos - t1Delta)).magnitude;
                var currDist = (t0Pos - t1Pos).magnitude;
                var diff = currDist - prevDist;

                distance -= diff * 0.01f * zoomSpeed;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }
        }

        private void ApplyTransform()
        {
            _currentDistance = Mathf.SmoothDamp(_currentDistance, distance, ref _velocityDistance, smoothTime);

            var rotation = Quaternion.Euler(_pitch, _yaw, 0);
            var offset = rotation * new Vector3(0, 0, -_currentDistance);

            transform.position = target.position + offset;
            transform.LookAt(target.position);
        }

        /// <summary>
        /// Sets the orbit target to a new transform.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// Resets camera to default view.
        /// </summary>
        public void ResetView()
        {
            _yaw = 0f;
            _pitch = 30f;
            distance = 2f;
        }
    }
}
