using System.Collections.Generic;
using UnityEngine;
using VigiFleet.Core;
using VigiFleet.Vehicles;

namespace VigiFleet.Simulation
{
    /// <summary>
    /// Standalone background harbour traffic generator for autonomous vehicle navigation.
    /// Drives vehicles along defined harbour road loops when no specific scenario is active.
    /// </summary>
    public class HarbourTrafficSimulator : MonoBehaviour
    {
        [Header("Traffic Controls")]
        [SerializeField] private bool autoStartTraffic = false;

        private void Start()
        {
            if (autoStartTraffic)
            {
                SetupDefaultHarbourRoutes();
            }
        }

        public void SetupDefaultHarbourRoutes()
        {
            VehicleController[] vehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);

            foreach (var v in vehicles)
            {
                var follower = v.GetComponent<VehicleWaypointFollower>();
                if (follower == null) follower = v.gameObject.AddComponent<VehicleWaypointFollower>();

                List<Vector3> route = GenerateRouteForVehicle(v.Profile.vehicleId);
                follower.SetWaypoints(route, loop: true);
                follower.TargetSpeedKmh = v.Profile.maxSpeedKmh * 0.75f;
                follower.IsNavigationActive = true;
            }
        }

        private List<Vector3> GenerateRouteForVehicle(string vehicleId)
        {
            List<Vector3> pts = new List<Vector3>();

            switch (vehicleId)
            {
                case "VIGI-TRUCK-001":
                    // Haulage loop: Gate -> Stack C3 -> Berth 4 -> Return
                    pts.Add(new Vector3(-140f, 0f, -60f));
                    pts.Add(new Vector3(-40f, 0f, -60f));
                    pts.Add(new Vector3(-40f, 0f, 40f));
                    pts.Add(new Vector3(60f, 0f, 40f));
                    pts.Add(new Vector3(60f, 0f, -60f));
                    break;

                case "VIGI-TRUCK-002":
                    // Modern truck loop: Hazmat yard bypass -> Berth 4 -> Main road
                    pts.Add(new Vector3(-60f, 0f, 120f));
                    pts.Add(new Vector3(40f, 0f, 120f));
                    pts.Add(new Vector3(40f, 0f, -20f));
                    pts.Add(new Vector3(-60f, 0f, -20f));
                    break;

                case "VIGI-FORK-001":
                    // Forklift aisle maneuver loop
                    pts.Add(new Vector3(80f, 0f, -30f));
                    pts.Add(new Vector3(80f, 0f, -10f));
                    pts.Add(new Vector3(100f, 0f, -10f));
                    pts.Add(new Vector3(100f, 0f, -30f));
                    break;

                case "VIGI-TRACTOR-001":
                    // Quayside tractor shuttle loop along Berth 4 cranes
                    pts.Add(new Vector3(120f, 0f, 100f));
                    pts.Add(new Vector3(120f, 0f, -50f));
                    pts.Add(new Vector3(90f, 0f, -50f));
                    pts.Add(new Vector3(90f, 0f, 100f));
                    break;

                default:
                    pts.Add(new Vector3(-50f, 0f, 0f));
                    pts.Add(new Vector3(50f, 0f, 0f));
                    break;
            }

            return pts;
        }
    }
}
