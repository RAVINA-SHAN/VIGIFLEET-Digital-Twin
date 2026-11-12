using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VigiFleet.Communication;
using VigiFleet.Core;
using VigiFleet.Environment;
using VigiFleet.Geofence;
using VigiFleet.Safety;
using VigiFleet.Simulation;
using VigiFleet.UI;
using VigiFleet.Vehicles;

namespace VigiFleet.Editor
{
    public static class HarbourSceneBuilder
    {
        [MenuItem("VIGIFLEET/Build All Harbour Scenes")]
        public static void BuildAllScenes()
        {
            EnsureDirectories();

            string mainScenePath = "Assets/Scenes/Harbour_Main.unity";
            string blindScenePath = "Assets/Scenes/Scenario_BlindIntersection.unity";
            string forkScenePath = "Assets/Scenes/Scenario_ForkliftReversing.unity";

            BuildScene(mainScenePath, ScenarioType.BLIND_INTERSECTION);
            BuildScene(blindScenePath, ScenarioType.BLIND_INTERSECTION);
            BuildScene(forkScenePath, ScenarioType.FORKLIFT_REVERSING);

            // Register in EditorBuildSettings
            EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(mainScenePath, true),
                new EditorBuildSettingsScene(blindScenePath, true),
                new EditorBuildSettingsScene(forkScenePath, true)
            };
            EditorBuildSettings.scenes = buildScenes;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VIGIFLEET] Successfully generated all harbour scenes and registered in build settings!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        }

        private static void BuildScene(string scenePath, ScenarioType defaultScenario)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Main Camera & Camera Controller
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1200f;
            camGo.AddComponent<AudioListener>();
            CameraController camCtrl = camGo.AddComponent<CameraController>();
            camCtrl.SetCameraMode(CameraMode.PRESET_OVERVIEW);

            // 2. Directional Sun Light & Day/Night Controller
            GameObject lightGo = new GameObject("Directional Light");
            Light sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.35f;
            sun.color = new Color(1.0f, 0.96f, 0.90f);
            sun.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.sun = sun;

            GameObject dayNightGo = new GameObject("DayNightController");
            DayNightController dayNight = dayNightGo.AddComponent<DayNightController>();

            // 3. Harbour Coordinate Converter
            GameObject coordGo = new GameObject("HarbourCoordinateConverter");
            coordGo.AddComponent<HarbourCoordinateConverter>();

            // 4. Harbour Procedural Environment Builder
            GameObject envGo = new GameObject("HarbourEnvironment");
            HarbourProceduralBuilder envBuilder = envGo.AddComponent<HarbourProceduralBuilder>();
            envBuilder.BuildTerminalEnvironment();

            // 5. Vehicles
            GameObject v1 = VehiclePrefabBuilder.BuildLegacyTruck(new Vector3(-60f, 0f, 0f));
            GameObject v2 = VehiclePrefabBuilder.BuildModernTruck(new Vector3(-35f, 0f, -40f));
            GameObject v3 = VehiclePrefabBuilder.BuildForklift(new Vector3(80f, 0f, -20f));
            GameObject v4 = VehiclePrefabBuilder.BuildTerminalTractor(new Vector3(100f, 0f, 20f));

            // 6. Subsystems
            GameObject commVisGo = new GameObject("V2VCommunicationVisualizer");
            commVisGo.AddComponent<V2VCommunicationVisualizer>();

            GameObject safetyGo = new GameObject("SafetyEngine");
            safetyGo.AddComponent<CollisionPredictor>();
            safetyGo.AddComponent<ConflictZoneRenderer>();
            safetyGo.AddComponent<SafetyEventManager>();

            GameObject geofenceGo = new GameObject("GeofenceManager");
            geofenceGo.AddComponent<GeofenceMonitor>();

            GameObject simGo = new GameObject("SimulationController");
            ScenarioController scenCtrl = simGo.AddComponent<ScenarioController>();
            scenCtrl.SetScenario(defaultScenario);
            simGo.AddComponent<HarbourTrafficSimulator>();

            GameObject netGo = new GameObject("NetworkManager");
            netGo.AddComponent<WebSocketClient>();
            netGo.AddComponent<RestApiClient>();

            // 7. Core Fleet Manager
            GameObject mgrGo = new GameObject("VigiFleetManager");
            mgrGo.AddComponent<VigiFleetManager>();

            // 8. User Interface
            GameObject uiGo = new GameObject("DigitalTwinUI");
            uiGo.AddComponent<EdgeMLPanelUI>();
            uiGo.AddComponent<DigitalTwinUIManager>();

            EditorSceneManager.SaveScene(scene, scenePath);
        }
    }
}
