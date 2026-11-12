using UnityEngine;
using VigiFleet.Vehicles;

namespace VigiFleet.Core
{
    public enum CameraMode
    {
        ORBIT_VEHICLE,
        CHASE_VEHICLE,
        IN_CAB_POV,
        FREE_FLY,
        PRESET_OVERVIEW,
        PRESET_QUAY_CRANES,
        PRESET_INTERSECTION
    }

    /// <summary>
    /// Multi-mode Camera Controller for the VIGIFLEET 3D Digital Twin.
    /// Supports Vehicle Orbit, Follow/Chase, In-Cab Driver POV, Free-Fly RTS, and Control-Room Presets.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target & Mode")]
        [SerializeField] private Transform targetVehicle;
        [SerializeField] private CameraMode mode = CameraMode.PRESET_OVERVIEW;

        [Header("Orbit Settings")]
        [SerializeField] private float orbitDistance = 22.0f;
        [SerializeField] private float minDistance = 5.0f;
        [SerializeField] private float maxDistance = 200.0f;
        [SerializeField] private float orbitSpeed = 4.0f;
        [SerializeField] private float zoomSensitivity = 8.0f;
        [SerializeField] private float smoothTime = 0.15f;

        [Header("Free Fly Settings")]
        [SerializeField] private float flySpeed = 50.0f;
        [SerializeField] private float fastFlyMultiplier = 2.5f;

        private float _yaw = 45.0f;
        private float _pitch = 30.0f;
        private Vector3 _currentVelocity;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;

        public CameraMode Mode => mode;

        public void SetTarget(VehicleController vehicle)
        {
            if (vehicle != null)
            {
                targetVehicle = vehicle.transform;
                SetCameraMode(CameraMode.ORBIT_VEHICLE);
            }
        }

        public void SetCameraMode(CameraMode newMode)
        {
            mode = newMode;
            switch (mode)
            {
                case CameraMode.PRESET_OVERVIEW:
                    _targetPosition = new Vector3(0f, 160f, -140f);
                    _targetRotation = Quaternion.Euler(50f, 0f, 0f);
                    break;
                case CameraMode.PRESET_QUAY_CRANES:
                    _targetPosition = new Vector3(160f, 65f, 20f);
                    _targetRotation = Quaternion.Euler(25f, -90f, 0f);
                    break;
                case CameraMode.PRESET_INTERSECTION:
                    _targetPosition = new Vector3(-35f, 40f, -25f);
                    _targetRotation = Quaternion.Euler(45f, 0f, 0f);
                    break;
            }
        }

        private void LateUpdate()
        {
            HandleKeyboardHotkeys();

            switch (mode)
            {
                case CameraMode.ORBIT_VEHICLE:
                    UpdateOrbit();
                    break;
                case CameraMode.CHASE_VEHICLE:
                    UpdateChase();
                    break;
                case CameraMode.IN_CAB_POV:
                    UpdateInCab();
                    break;
                case CameraMode.FREE_FLY:
                    UpdateFreeFly();
                    break;
                case CameraMode.PRESET_OVERVIEW:
                case CameraMode.PRESET_QUAY_CRANES:
                case CameraMode.PRESET_INTERSECTION:
                    transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _currentVelocity, smoothTime);
                    transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * 6f);
                    break;
            }
        }

        private void HandleKeyboardHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.F1)) SetCameraMode(CameraMode.PRESET_OVERVIEW);
            if (Input.GetKeyDown(KeyCode.F2)) SetCameraMode(CameraMode.PRESET_QUAY_CRANES);
            if (Input.GetKeyDown(KeyCode.F3)) SetCameraMode(CameraMode.PRESET_INTERSECTION);
            if (Input.GetKeyDown(KeyCode.F4)) SetCameraMode(CameraMode.IN_CAB_POV);
            if (Input.GetKeyDown(KeyCode.F5)) SetCameraMode(CameraMode.CHASE_VEHICLE);
            if (Input.GetKeyDown(KeyCode.F6)) SetCameraMode(CameraMode.FREE_FLY);
        }

        private void UpdateOrbit()
        {
            if (targetVehicle == null)
            {
                SetCameraMode(CameraMode.PRESET_OVERVIEW);
                return;
            }

            if (Input.GetMouseButton(1)) // Right drag to orbit
            {
                _yaw += Input.GetAxis("Mouse X") * orbitSpeed;
                _pitch -= Input.GetAxis("Mouse Y") * orbitSpeed;
                _pitch = Mathf.Clamp(_pitch, 5f, 85f);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                orbitDistance = Mathf.Clamp(orbitDistance - scroll * zoomSensitivity, minDistance, maxDistance);
            }

            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 focusPoint = targetVehicle.position + Vector3.up * 1.5f;
            Vector3 pos = focusPoint - (rot * Vector3.forward * orbitDistance);

            transform.position = Vector3.SmoothDamp(transform.position, pos, ref _currentVelocity, smoothTime);
            transform.LookAt(focusPoint);
        }

        private void UpdateChase()
        {
            if (targetVehicle == null) return;

            Vector3 focus = targetVehicle.position + Vector3.up * 2.0f;
            Vector3 desiredPos = targetVehicle.position - (targetVehicle.forward * 14.0f) + Vector3.up * 5.0f;

            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _currentVelocity, smoothTime);
            transform.LookAt(focus);
        }

        private void UpdateInCab()
        {
            if (targetVehicle == null) return;

            Vector3 cabEye = targetVehicle.position + (targetVehicle.forward * 1.8f) + Vector3.up * 2.4f;
            transform.position = cabEye;
            transform.rotation = targetVehicle.rotation;
        }

        private void UpdateFreeFly()
        {
            float speed = flySpeed * (Input.GetKey(KeyCode.LeftShift) ? fastFlyMultiplier : 1.0f);

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            float upDown = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);

            Vector3 move = (transform.forward * v + transform.right * h + Vector3.up * upDown) * (speed * Time.deltaTime);
            transform.position += move;

            if (Input.GetMouseButton(1))
            {
                _yaw += Input.GetAxis("Mouse X") * orbitSpeed;
                _pitch -= Input.GetAxis("Mouse Y") * orbitSpeed;
                _pitch = Mathf.Clamp(_pitch, -85f, 85f);
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }
        }
    }
}
