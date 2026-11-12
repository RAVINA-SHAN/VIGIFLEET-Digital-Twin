using System;
using UnityEngine;
using VigiFleet.Core;
using VigiFleet.Telemetry;

namespace VigiFleet.Vehicles
{
    /// <summary>
    /// Realistic Vehicle Digital Twin Controller.
    /// Manages physical movement interpolation, wheel rotation, steering, lighting,
    /// in-world HUD billboard, and GNSS uncertainty radius visualization.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class VehicleController : MonoBehaviour
    {
        [Header("Vehicle Configuration & Profile")]
        [SerializeField] private VehicleProfile profile = new VehicleProfile();

        [Header("Runtime State")]
        [SerializeField] private VehicleStateData currentState = new VehicleStateData();

        [Header("Visual Parts & Lights")]
        [SerializeField] private Transform[] frontLeftWheels;
        [SerializeField] private Transform[] frontRightWheels;
        [SerializeField] private Transform[] rearWheels;
        [SerializeField] private Renderer[] brakeLightRenderers;
        [SerializeField] private Renderer[] reverseLightRenderers;
        [SerializeField] private GameObject beaconStrobe;

        [Header("Overhead Billboard HUD")]
        [SerializeField] private Transform billboardPivot;
        [SerializeField] private TextMesh hudText;
        [SerializeField] private SpriteRenderer riskPillRenderer;

        [Header("GNSS Position Uncertainty Visualizer")]
        [SerializeField] private LineRenderer uncertaintyRing;

        [Header("Interpolation Settings")]
        [SerializeField] private float positionSmoothTime = 0.25f;
        [SerializeField] private float rotationSmoothTime = 0.2f;

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _positionVelocity;
        private float _currentSteerAngle;
        private float _wheelRollAngle;
        private MaterialPropertyBlock _propBlock;

        public VehicleProfile Profile => profile;
        public VehicleStateData CurrentState => currentState;
        public Vector3 CurrentVelocity { get; private set; }

        public event Action<VehicleController> OnVehicleSelected;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;

            if (billboardPivot == null)
            {
                CreateDefaultBillboardHUD();
            }

            if (uncertaintyRing == null)
            {
                CreateDefaultUncertaintyRing();
            }
        }

        private void Start()
        {
            if (currentState == null || string.IsNullOrEmpty(currentState.vehicle_id))
            {
                currentState = new VehicleStateData
                {
                    vehicle_id = profile.vehicleId,
                    display_name = profile.displayName,
                    vehicle_category = profile.vehicleCategory,
                    vehicle_type = profile.vehicleType.ToString(),
                    vehicle_mode = profile.vehicleMode.ToString(),
                    speed_mps = 0f,
                    heading_deg = transform.eulerAngles.y,
                    brake_status = profile.hasCanBus ? "OFF" : "UNKNOWN",
                    reverse_status = profile.hasCanBus ? "OFF" : "UNKNOWN",
                    gnss_fix = true,
                    position_accuracy_m = 2.5f,
                    communication_status = "CONNECTED",
                    risk_level = "NORMAL"
                };
            }
            UpdateVisuals();
        }

        private void Update()
        {
            // Smoothly interpolate position and orientation towards target
            Vector3 previousPos = transform.position;
            transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _positionVelocity, positionSmoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime / Mathf.Max(0.01f, rotationSmoothTime));

            CurrentVelocity = (transform.position - previousPos) / Mathf.Max(0.001f, Time.deltaTime);

            // Update wheel roll based on linear movement
            float distanceMoved = Vector3.Dot(CurrentVelocity, transform.forward) * Time.deltaTime;
            float rollDelta = (distanceMoved / Mathf.Max(0.1f, profile.wheelRadius)) * Mathf.Rad2Deg;
            _wheelRollAngle = (_wheelRollAngle + rollDelta) % 360f;

            // Update steering angle based on angular rotation
            float targetSteer = Mathf.Clamp(Vector3.SignedAngle(transform.forward, _targetRotation * Vector3.forward, Vector3.up), -profile.maxSteeringAngleDeg, profile.maxSteeringAngleDeg);
            _currentSteerAngle = Mathf.Lerp(_currentSteerAngle, targetSteer, Time.deltaTime * 6f);

            UpdateWheels();
            UpdateLights();
            UpdateBillboardHUD();
            UpdateUncertaintyRing();
        }

        public void ApplyTelemetryPacket(ESP32TelemetryPacket pkt, bool isStale = false)
        {
            if (pkt == null) return;
            currentState.UpdateFromPacket(pkt, isStale);

            // Convert GNSS Lat/Lon to Unity World Space if coordinate converter is ready
            if (HarbourCoordinateConverter.Instance != null && pkt.gnss_fix)
            {
                Vector3 worldPos = HarbourCoordinateConverter.Instance.LatLonToWorldPosition(pkt.latitude, pkt.longitude);
                SetTargetPosition(worldPos);

                float yaw = HarbourCoordinateConverter.CompassHeadingToUnityYaw(pkt.heading_deg);
                SetTargetRotation(Quaternion.Euler(0f, yaw, 0f));
            }

            UpdateVisuals();
        }

        public void SetTargetPosition(Vector3 newPos)
        {
            _targetPosition = new Vector3(newPos.x, HarbourCoordinateConverter.Instance != null ? HarbourCoordinateConverter.Instance.GroundElevationY : 0f, newPos.z);
        }

        public void SetTargetRotation(Quaternion newRot)
        {
            _targetRotation = newRot;
        }

        public void SetRiskLevel(RiskLevel risk, string hazard = null)
        {
            currentState.risk_level = risk.ToDisplayString();
            currentState.active_hazard = hazard;
            UpdateVisuals();
        }

        private void UpdateWheels()
        {
            // Spin and steer front wheels
            if (frontLeftWheels != null)
            {
                foreach (var w in frontLeftWheels)
                {
                    if (w != null) w.localRotation = Quaternion.Euler(_wheelRollAngle, _currentSteerAngle, 0f);
                }
            }
            if (frontRightWheels != null)
            {
                foreach (var w in frontRightWheels)
                {
                    if (w != null) w.localRotation = Quaternion.Euler(_wheelRollAngle, _currentSteerAngle, 0f);
                }
            }

            // Spin rear wheels
            if (rearWheels != null)
            {
                foreach (var w in rearWheels)
                {
                    if (w != null) w.localRotation = Quaternion.Euler(_wheelRollAngle, 0f, 0f);
                }
            }
        }

        private void UpdateLights()
        {
            bool isBraking = currentState.brake_status == "ON" || currentState.acceleration_mps2 < -1.5f;
            bool isReversing = currentState.reverse_status == "ON";

            if (brakeLightRenderers != null)
            {
                foreach (var r in brakeLightRenderers)
                {
                    if (r != null)
                    {
                        Color c = isBraking ? new Color(1f, 0.05f, 0.05f, 1f) : new Color(0.25f, 0.02f, 0.02f, 1f);
                        _propBlock.SetColor("_BaseColor", c);
                        _propBlock.SetColor("_EmissionColor", isBraking ? c * 2.5f : Color.black);
                        r.SetPropertyBlock(_propBlock);
                    }
                }
            }

            if (reverseLightRenderers != null)
            {
                foreach (var r in reverseLightRenderers)
                {
                    if (r != null)
                    {
                        Color c = isReversing ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1f);
                        _propBlock.SetColor("_BaseColor", c);
                        _propBlock.SetColor("_EmissionColor", isReversing ? Color.white * 2f : Color.black);
                        r.SetPropertyBlock(_propBlock);
                    }
                }
            }

            if (beaconStrobe != null)
            {
                beaconStrobe.SetActive(isReversing || currentState.GetRiskLevelEnum() >= RiskLevel.HIGH_RISK);
            }
        }

        private void UpdateBillboardHUD()
        {
            if (billboardPivot == null) return;

            // Make billboard face the active camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                billboardPivot.rotation = Quaternion.LookRotation(billboardPivot.position - mainCam.transform.position);
            }

            if (hudText != null)
            {
                string statusText = $"{currentState.vehicle_id}\n{currentState.speed_kmh:F1} km/h";
                if (currentState.communication_status == "STALE") statusText += " [STALE]";
                else if (currentState.communication_status == "OFFLINE") statusText += " [OFFLINE]";
                hudText.text = statusText;
            }

            if (riskPillRenderer != null)
            {
                riskPillRenderer.color = currentState.GetRiskLevelEnum().GetRiskColor();
            }
        }

        private void UpdateUncertaintyRing()
        {
            if (uncertaintyRing == null) return;

            int segments = 32;
            float radius = Mathf.Clamp(currentState.position_accuracy_m, 1.0f, 25.0f);
            Color ringColor = currentState.gnss_fix ? new Color(0.2f, 0.6f, 1.0f, 0.45f) : new Color(1.0f, 0.2f, 0.2f, 0.65f);

            uncertaintyRing.positionCount = segments + 1;
            uncertaintyRing.startColor = ringColor;
            uncertaintyRing.endColor = ringColor;
            uncertaintyRing.startWidth = 0.12f;
            uncertaintyRing.endWidth = 0.12f;

            for (int i = 0; i <= segments; i++)
            {
                float rad = (float)i / segments * Mathf.PI * 2f;
                float x = Mathf.Cos(rad) * radius;
                float z = Mathf.Sin(rad) * radius;
                uncertaintyRing.SetPosition(i, transform.position + new Vector3(x, 0.05f, z));
            }
        }

        private void CreateDefaultBillboardHUD()
        {
            GameObject hudGo = new GameObject("BillboardHUD");
            hudGo.transform.SetParent(transform);
            hudGo.transform.localPosition = new Vector3(0f, profile.height + 1.2f, 0f);
            billboardPivot = hudGo.transform;

            GameObject textGo = new GameObject("HUDText");
            textGo.transform.SetParent(billboardPivot);
            textGo.transform.localPosition = Vector3.zero;
            hudText = textGo.AddComponent<TextMesh>();
            hudText.fontSize = 28;
            hudText.characterSize = 0.08f;
            hudText.alignment = TextAlignment.Center;
            hudText.anchor = TextAnchor.MiddleCenter;
            hudText.color = Color.white;
            hudText.text = profile.vehicleId;
        }

        private void CreateDefaultUncertaintyRing()
        {
            GameObject ringGo = new GameObject("UncertaintyRing");
            ringGo.transform.SetParent(transform);
            ringGo.transform.localPosition = Vector3.zero;
            uncertaintyRing = ringGo.AddComponent<LineRenderer>();
            uncertaintyRing.useWorldSpace = true;
            uncertaintyRing.loop = true;
            uncertaintyRing.material = new Material(Shader.Find("Sprites/Default"));
        }

        public void UpdateVisuals()
        {
            UpdateLights();
            UpdateBillboardHUD();
            UpdateUncertaintyRing();
        }

        private void OnMouseDown()
        {
            OnVehicleSelected?.Invoke(this);
        }
    }
}
