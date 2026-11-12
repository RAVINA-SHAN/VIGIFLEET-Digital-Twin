using UnityEngine;
using VigiFleet.Communication;
using VigiFleet.Environment;
using VigiFleet.Geofence;
using VigiFleet.Safety;
using VigiFleet.Simulation;
using VigiFleet.UI;
using VigiFleet.Vehicles;

namespace VigiFleet.Core
{
    /// <summary>
    /// Runtime bootstrapper ensuring all environment elements, vehicles, subsystems,
    /// and control-room UI are present and linked even in clean or newly instantiated scenes.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class HarbourSceneBootstrapper : MonoBehaviour
    {
        [Header("Auto-Bootstrap Settings")]
        [SerializeField] private bool autoBuildEnvironmentIfMissing = true;
        [SerializeField] private bool autoSpawnFleetIfMissing = true;

        private void Awake()
        {
            EnsureCoordinateConverter();
            EnsureEnvironment();
            EnsureVehicles();
            EnsureSubsystems();
            EnsureUI();
        }

        private void EnsureCoordinateConverter()
        {
            if (HarbourCoordinateConverter.Instance == null)
            {
                GameObject go = new GameObject("HarbourCoordinateConverter");
                go.AddComponent<HarbourCoordinateConverter>();
            }
        }

        private void EnsureEnvironment()
        {
            if (!autoBuildEnvironmentIfMissing) return;

            HarbourProceduralBuilder builder = FindFirstObjectByType<HarbourProceduralBuilder>();
            if (builder == null)
            {
                GameObject envGo = new GameObject("HarbourEnvironment");
                builder = envGo.AddComponent<HarbourProceduralBuilder>();
                builder.BuildTerminalEnvironment();
            }

            DayNightController dayNight = FindFirstObjectByType<DayNightController>();
            if (dayNight == null)
            {
                GameObject dnGo = new GameObject("DayNightController");
                dnGo.AddComponent<DayNightController>();
            }

            // Ensure Main Camera has CameraController
            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<CameraController>() == null)
            {
                cam.gameObject.AddComponent<CameraController>();
            }
        }

        private void EnsureVehicles()
        {
            if (!autoSpawnFleetIfMissing) return;

            VehicleController[] found = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
            if (found.Length == 0)
            {
                VehiclePrefabBuilder.BuildLegacyTruck(new Vector3(-60f, 0f, 0f));
                VehiclePrefabBuilder.BuildModernTruck(new Vector3(-35f, 0f, -40f));
                VehiclePrefabBuilder.BuildForklift(new Vector3(80f, 0f, -20f));
                VehiclePrefabBuilder.BuildTerminalTractor(new Vector3(100f, 0f, 20f));
            }
        }

        private void EnsureSubsystems()
        {
            if (FindFirstObjectByType<V2VCommunicationVisualizer>() == null)
            {
                GameObject go = new GameObject("V2VCommunicationVisualizer");
                go.AddComponent<V2VCommunicationVisualizer>();
            }

            if (FindFirstObjectByType<CollisionPredictor>() == null)
            {
                GameObject go = new GameObject("SafetyEngine");
                go.AddComponent<CollisionPredictor>();
                go.AddComponent<ConflictZoneRenderer>();
                go.AddComponent<SafetyEventManager>();
            }

            if (FindFirstObjectByType<GeofenceMonitor>() == null)
            {
                GameObject go = new GameObject("GeofenceManager");
                go.AddComponent<GeofenceMonitor>();
            }

            if (FindFirstObjectByType<ScenarioController>() == null)
            {
                GameObject go = new GameObject("SimulationController");
                go.AddComponent<ScenarioController>();
                go.AddComponent<HarbourTrafficSimulator>();
            }

            if (FindFirstObjectByType<WebSocketClient>() == null)
            {
                GameObject go = new GameObject("NetworkManager");
                go.AddComponent<WebSocketClient>();
                go.AddComponent<RestApiClient>();
            }

            if (VigiFleetManager.Instance == null)
            {
                GameObject go = new GameObject("VigiFleetManager");
                go.AddComponent<VigiFleetManager>();
            }
        }

        private void EnsureUI()
        {
            if (FindFirstObjectByType<DigitalTwinUIManager>() == null)
            {
                GameObject go = new GameObject("DigitalTwinUI");
                go.AddComponent<EdgeMLPanelUI>();
                go.AddComponent<DigitalTwinUIManager>();
            }
        }
    }
}
