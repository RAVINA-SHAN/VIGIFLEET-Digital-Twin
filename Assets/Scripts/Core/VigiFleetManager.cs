using System;
using System.Collections.Generic;
using UnityEngine;
using VigiFleet.Communication;
using VigiFleet.Geofence;
using VigiFleet.Safety;
using VigiFleet.Simulation;
using VigiFleet.Telemetry;
using VigiFleet.Vehicles;

namespace VigiFleet.Core
{
    /// <summary>
    /// Master Coordinator for the VIGIFLEET 3D Harbour Digital Twin.
    /// Manages vehicles, dual operating modes, live WebSocket telemetry ingestion,
    /// Edge AI & safety evaluation, geofences, and UI data distribution.
    /// </summary>
    public class VigiFleetManager : MonoBehaviour
    {
        public static VigiFleetManager Instance { get; private set; }

        [Header("Operating Mode")]
        [SerializeField] private OperatingMode operatingMode = OperatingMode.SIMULATION;

        [Header("Fleet Registry")]
        [SerializeField] private List<VehicleController> activeVehicles = new List<VehicleController>();

        [Header("Subsystems")]
        [SerializeField] private WebSocketClient webSocketClient;
        [SerializeField] private RestApiClient restApiClient;
        [SerializeField] private ScenarioController scenarioController;
        [SerializeField] private CollisionPredictor collisionPredictor;
        [SerializeField] private ConflictZoneRenderer conflictRenderer;
        [SerializeField] private GeofenceMonitor geofenceMonitor;
        [SerializeField] private V2VCommunicationVisualizer commVisualizer;
        [SerializeField] private SafetyEventManager safetyEventManager;
        [SerializeField] private CameraController cameraController;

        [Header("Selection State")]
        [SerializeField] private VehicleController selectedVehicle;

        private readonly Dictionary<string, VehicleController> _vehicleDict = new Dictionary<string, VehicleController>();
        private readonly TelemetryValidator _validator = new TelemetryValidator();

        public OperatingMode CurrentOperatingMode => operatingMode;
        public IReadOnlyList<VehicleController> ActiveVehicles => activeVehicles;
        public VehicleController SelectedVehicle => selectedVehicle;

        public event Action<OperatingMode> OnOperatingModeChanged;
        public event Action<VehicleController> OnVehicleSelected;
        public event Action<ESP32TelemetryPacket> OnTelemetryProcessed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            FindSubsystemsIfNull();
        }

        private void Start()
        {
            EnsureVehiclesSpawned();
            RegisterVehiclesWithSubsystems();

            if (webSocketClient != null)
            {
                webSocketClient.OnTelemetryPacketReceived += HandleIncomingTelemetryPacket;
                webSocketClient.OnConnectionStatusChanged += HandleWsConnectionChanged;
            }

            SetOperatingMode(operatingMode);
        }

        private void FindSubsystemsIfNull()
        {
            if (webSocketClient == null) webSocketClient = FindFirstObjectByType<WebSocketClient>();
            if (restApiClient == null) restApiClient = FindFirstObjectByType<RestApiClient>();
            if (scenarioController == null) scenarioController = FindFirstObjectByType<ScenarioController>();
            if (collisionPredictor == null) collisionPredictor = FindFirstObjectByType<CollisionPredictor>();
            if (conflictRenderer == null) conflictRenderer = FindFirstObjectByType<ConflictZoneRenderer>();
            if (geofenceMonitor == null) geofenceMonitor = FindFirstObjectByType<GeofenceMonitor>();
            if (commVisualizer == null) commVisualizer = FindFirstObjectByType<V2VCommunicationVisualizer>();
            if (safetyEventManager == null) safetyEventManager = FindFirstObjectByType<SafetyEventManager>();
            if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
        }

        private void EnsureVehiclesSpawned()
        {
            VehicleController[] found = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
            if (found.Length == 0)
            {
                // Spawn the 4 default harbour vehicles
                GameObject v1 = VehiclePrefabBuilder.BuildLegacyTruck(new Vector3(-60f, 0f, 0f));
                GameObject v2 = VehiclePrefabBuilder.BuildModernTruck(new Vector3(-35f, 0f, -40f));
                GameObject v3 = VehiclePrefabBuilder.BuildForklift(new Vector3(80f, 0f, -20f));
                GameObject v4 = VehiclePrefabBuilder.BuildTerminalTractor(new Vector3(100f, 0f, 20f));

                activeVehicles.Clear();
                activeVehicles.Add(v1.GetComponent<VehicleController>());
                activeVehicles.Add(v2.GetComponent<VehicleController>());
                activeVehicles.Add(v3.GetComponent<VehicleController>());
                activeVehicles.Add(v4.GetComponent<VehicleController>());
            }
            else
            {
                activeVehicles.Clear();
                activeVehicles.AddRange(found);
            }

            _vehicleDict.Clear();
            foreach (var v in activeVehicles)
            {
                if (v != null)
                {
                    _vehicleDict[v.Profile.vehicleId] = v;
                    v.OnVehicleSelected += SelectVehicle;
                }
            }

            if (selectedVehicle == null && activeVehicles.Count > 0)
            {
                SelectVehicle(activeVehicles[0]);
            }
        }

        private void RegisterVehiclesWithSubsystems()
        {
            if (commVisualizer != null)
            {
                foreach (var v in activeVehicles)
                {
                    commVisualizer.RegisterVehicle(v);
                }
            }
        }

        private void Update()
        {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Run geofence and safety checks on all vehicles
            foreach (var v in activeVehicles)
            {
                if (v == null) continue;

                // Watchdog: check if vehicle went offline in hardware mode
                if (operatingMode == OperatingMode.LIVE_HARDWARE)
                {
                    if (_validator.CheckIfOffline(v.Profile.vehicleId, nowMs))
                    {
                        v.CurrentState.communication_status = "OFFLINE";
                    }
                }

                // Check geofence status
                if (geofenceMonitor != null)
                {
                    geofenceMonitor.CheckVehicleInGeofences(v);
                }
            }
        }

        public void SetOperatingMode(OperatingMode mode)
        {
            operatingMode = mode;

            if (scenarioController != null)
            {
                // Scenarios only drive kinematics when in SIMULATION mode
                scenarioController.enabled = (operatingMode == OperatingMode.SIMULATION);
            }

            // Sync with backend if REST client is available
            if (restApiClient != null)
            {
                string modeStr = (operatingMode == OperatingMode.SIMULATION) ? "SIMULATION" : "HARDWARE";
                _ = restApiClient.SetOperatingModeAsync(modeStr);
            }

            OnOperatingModeChanged?.Invoke(operatingMode);
        }

        public void SelectVehicle(VehicleController vehicle)
        {
            selectedVehicle = vehicle;
            if (cameraController != null && vehicle != null)
            {
                cameraController.SetTarget(vehicle);
            }
            OnVehicleSelected?.Invoke(vehicle);
        }

        public void SelectVehicleById(string vehicleId)
        {
            if (_vehicleDict.TryGetValue(vehicleId, out VehicleController v))
            {
                SelectVehicle(v);
            }
        }

        public VehicleController GetVehicleById(string vehicleId)
        {
            _vehicleDict.TryGetValue(vehicleId, out VehicleController v);
            return v;
        }

        private void HandleIncomingTelemetryPacket(ESP32TelemetryPacket pkt)
        {
            if (pkt == null) return;

            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var val = _validator.Validate(pkt, nowMs);

            if (!val.Valid)
            {
                return; // Rejected malformed or duplicate
            }

            // In LIVE_HARDWARE mode, apply real physical telemetry directly to vehicle
            if (operatingMode == OperatingMode.LIVE_HARDWARE || pkt.data_source == "HARDWARE")
            {
                if (_vehicleDict.TryGetValue(pkt.vehicle_id, out VehicleController v))
                {
                    v.ApplyTelemetryPacket(pkt, val.IsStale);
                }
            }

            OnTelemetryProcessed?.Invoke(pkt);
        }

        private void HandleWsConnectionChanged(bool connected)
        {
            Debug.Log($"[VigiFleetManager] WebSocket connection status: {connected}");
        }
    }
}
