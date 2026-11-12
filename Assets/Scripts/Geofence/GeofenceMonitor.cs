using System;
using System.Collections.Generic;
using UnityEngine;
using VigiFleet.Core;
using VigiFleet.Safety;
using VigiFleet.Vehicles;

namespace VigiFleet.Geofence
{
    /// <summary>
    /// Evaluates real-time 2D polygon containment, speed limits, and authorized vehicle types
    /// for all participating harbour vehicles against harbour geofences.
    /// </summary>
    public class GeofenceMonitor : MonoBehaviour
    {
        [Header("Geofence Registry")]
        [SerializeField] private List<GeofenceZone> zones = new List<GeofenceZone>();

        private readonly List<GeofenceVisualizer> _visualizers = new List<GeofenceVisualizer>();
        private SafetyEventManager _safetyEventManager;

        public IReadOnlyList<GeofenceZone> Zones => zones;

        private void Awake()
        {
            _safetyEventManager = FindFirstObjectByType<SafetyEventManager>();
        }

        private void Start()
        {
            if (zones.Count == 0)
            {
                InitializeDefaultHarbourGeofences();
            }
            BuildAllVisualizers();
        }

        public void InitializeDefaultHarbourGeofences()
        {
            zones.Clear();

            // 1. Berth 4 Gantry Crane Zone
            zones.Add(new GeofenceZone
            {
                zoneId = "GF-CRANE-01",
                zoneName = "Berth 4 Gantry Crane Operating Zone",
                zoneCategory = "Crane Operating Zone",
                coordinatesLatLon = new List<Vector2>
                {
                    new Vector2(13.0842f, 80.2902f),
                    new Vector2(13.0842f, 80.2924f),
                    new Vector2(13.0830f, 80.2924f),
                    new Vector2(13.0830f, 80.2902f)
                },
                maxSpeedKmh = 12.0f,
                allowedVehicleTypes = new List<string> { "TERMINAL_TRACTOR", "FORKLIFT" },
                alertSeverity = RiskLevel.HIGH_RISK,
                safetyRules = "Max speed 12 km/h. Legacy trucks require crane spotter clearance. Overhead load hazard."
            });

            // 2. North Terminal Hazmat Yard
            zones.Add(new GeofenceZone
            {
                zoneId = "GF-HAZMAT-02",
                zoneName = "North Terminal Restricted Hazmat Cargo Yard",
                zoneCategory = "Restricted Cargo Area",
                coordinatesLatLon = new List<Vector2>
                {
                    new Vector2(13.0852f, 80.2880f),
                    new Vector2(13.0852f, 80.2900f),
                    new Vector2(13.0838f, 80.2900f),
                    new Vector2(13.0838f, 80.2880f)
                },
                maxSpeedKmh = 10.0f,
                allowedVehicleTypes = new List<string> { "MODERN_TRUCK" },
                alertSeverity = RiskLevel.CRITICAL,
                safetyRules = "Unauthorized vehicle entry prohibited. Immediate voice stop warning."
            });

            // 3. Container Stack C3 Blind Intersection
            zones.Add(new GeofenceZone
            {
                zoneId = "GF-BLIND-03",
                zoneName = "Container Stack C3 Blind Intersection",
                zoneCategory = "Blind Intersection",
                coordinatesLatLon = new List<Vector2>
                {
                    new Vector2(13.0829f, 80.2890f),
                    new Vector2(13.0829f, 80.2904f),
                    new Vector2(13.0819f, 80.2904f),
                    new Vector2(13.0819f, 80.2890f)
                },
                maxSpeedKmh = 15.0f,
                allowedVehicleTypes = new List<string> { "TRUCK", "OLD_TRUCK", "MODERN_TRUCK", "FORKLIFT", "TERMINAL_TRACTOR" },
                alertSeverity = RiskLevel.HIGH_RISK,
                safetyRules = "Mandatory speed reduction <15 km/h. Cooperative V2V cross-traffic alert active."
            });

            // 4. Customs Gate Pedestrian Walkway
            zones.Add(new GeofenceZone
            {
                zoneId = "GF-PED-04",
                zoneName = "Customs Gate B Pedestrian Walkway Crossing",
                zoneCategory = "Pedestrian Crossing Zone",
                coordinatesLatLon = new List<Vector2>
                {
                    new Vector2(13.0817f, 80.2882f),
                    new Vector2(13.0817f, 80.2898f),
                    new Vector2(13.0810f, 80.2898f),
                    new Vector2(13.0810f, 80.2882f)
                },
                maxSpeedKmh = 8.0f,
                allowedVehicleTypes = new List<string> { "TRUCK", "OLD_TRUCK", "MODERN_TRUCK", "FORKLIFT", "TERMINAL_TRACTOR" },
                alertSeverity = RiskLevel.CAUTION,
                safetyRules = "Yield to stevedore personnel. Max speed 8 km/h. Audio horn alert recommended."
            });

            // 5. East Quay Water-Side Deep Berth Edge
            zones.Add(new GeofenceZone
            {
                zoneId = "GF-WATER-05",
                zoneName = "East Quay Water-Side Deep Berth Edge",
                zoneCategory = "Water-Side Boundary",
                coordinatesLatLon = new List<Vector2>
                {
                    new Vector2(13.0850f, 80.2926f),
                    new Vector2(13.0850f, 80.2938f),
                    new Vector2(13.0812f, 80.2938f),
                    new Vector2(13.0812f, 80.2926f)
                },
                maxSpeedKmh = 10.0f,
                allowedVehicleTypes = new List<string> { "TERMINAL_TRACTOR" },
                alertSeverity = RiskLevel.CRITICAL,
                safetyRules = "Unprotected quay wall drop-off. Reversing without spotter prohibited."
            });
        }

        public void BuildAllVisualizers()
        {
            // Clear existing
            foreach (var vis in _visualizers)
            {
                if (vis != null) Destroy(vis.gameObject);
            }
            _visualizers.Clear();

            foreach (var zone in zones)
            {
                // Convert lat/lons to world positions if coordinate converter is ready
                zone.worldPolygonPoints.Clear();
                foreach (var coord in zone.coordinatesLatLon)
                {
                    if (HarbourCoordinateConverter.Instance != null)
                    {
                        Vector3 worldPos = HarbourCoordinateConverter.Instance.LatLonToWorldPosition(coord.x, coord.y);
                        zone.worldPolygonPoints.Add(worldPos);
                    }
                    else
                    {
                        // Fallback local offsets centered around 0
                        zone.worldPolygonPoints.Add(new Vector3((coord.y - 80.2900f) * 10000f, 0f, (coord.x - 13.0825f) * 10000f));
                    }
                }

                GameObject visGo = new GameObject($"Visualizer_{zone.zoneId}");
                visGo.transform.SetParent(transform);
                GeofenceVisualizer visualizer = visGo.AddComponent<GeofenceVisualizer>();
                visualizer.Initialize(zone);
                _visualizers.Add(visualizer);
            }
        }

        public void CheckVehicleInGeofences(VehicleController vehicle)
        {
            if (vehicle == null) return;

            Vector3 vPos = vehicle.transform.position;
            string vType = vehicle.Profile.vehicleType.ToString();
            float vSpeed = vehicle.CurrentState.speed_kmh;

            vehicle.CurrentState.active_geofences.Clear();

            for (int i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                var vis = (i < _visualizers.Count) ? _visualizers[i] : null;

                bool isInside = IsPointInPolygon(new Vector2(vPos.x, vPos.z), zone.worldPolygonPoints);

                if (isInside)
                {
                    vehicle.CurrentState.active_geofences.Add(zone.zoneId);

                    // Check vehicle authorization
                    bool isAllowed = zone.allowedVehicleTypes.Contains(vType) ||
                                     (zone.allowedVehicleTypes.Contains("TRUCK") && (vType == "OLD_TRUCK" || vType == "MODERN_TRUCK"));

                    if (!isAllowed)
                    {
                        if (vis != null) vis.SetBreachState(true);
                        vehicle.SetRiskLevel(zone.alertSeverity, $"Unauthorized entry in {zone.zoneName}");

                        if (_safetyEventManager != null)
                        {
                            _safetyEventManager.TriggerSafetyEvent(
                                vehicle.Profile.vehicleId,
                                null,
                                "Restricted Zone Entry",
                                zone.alertSeverity,
                                0f,
                                0f,
                                $"Warning! Unauthorized entry into {zone.zoneName}. Halt immediately.",
                                "Geofence Ray-Casting Polygon Engine"
                            );
                        }
                    }
                    else if (vSpeed > zone.maxSpeedKmh)
                    {
                        vehicle.SetRiskLevel(RiskLevel.CAUTION, $"Overspeeding in {zone.zoneId} ({vSpeed:F1} km/h > {zone.maxSpeedKmh:F0} km/h)");
                    }
                }
                else
                {
                    if (vis != null) vis.SetBreachState(false);
                }
            }
        }

        public static bool IsPointInPolygon(Vector2 point, List<Vector3> polygon)
        {
            if (polygon == null || polygon.Count < 3) return false;

            bool inside = false;
            int j = polygon.Count - 1;

            for (int i = 0; i < polygon.Count; i++)
            {
                float xi = polygon[i].x;
                float yi = polygon[i].z;
                float xj = polygon[j].x;
                float yj = polygon[j].z;

                if (((yi > point.y) != (yj > point.y)) &&
                    (point.x < (xj - xi) * (point.y - yi) / (yj - yi) + xi))
                {
                    inside = !inside;
                }
                j = i;
            }
            return inside;
        }
    }
}
