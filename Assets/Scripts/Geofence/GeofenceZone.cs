using System;
using System.Collections.Generic;
using UnityEngine;
using VigiFleet.Core;

namespace VigiFleet.Geofence
{
    [Serializable]
    public class GeofenceZone
    {
        public string zoneId;
        public string zoneName;
        public string zoneCategory;
        public List<Vector2> coordinatesLatLon = new List<Vector2>(); // (x = lat, y = lon)
        public List<Vector3> worldPolygonPoints = new List<Vector3>();
        public bool active = true;
        public float maxSpeedKmh = 15.0f;
        public List<string> allowedVehicleTypes = new List<string>();
        public RiskLevel alertSeverity = RiskLevel.HIGH_RISK;
        public string safetyRules = "";

        public Color GetZoneColor()
        {
            switch (zoneCategory?.ToUpper())
            {
                case "CRANE OPERATING ZONE":
                    return new Color(0.95f, 0.65f, 0.1f, 0.35f); // Amber / Yellow
                case "RESTRICTED CARGO AREA":
                    return new Color(0.90f, 0.15f, 0.15f, 0.38f); // Red
                case "BLIND INTERSECTION":
                    return new Color(0.95f, 0.85f, 0.15f, 0.32f); // Caution Yellow
                case "PEDESTRIAN CROSSING ZONE":
                    return new Color(0.2f, 0.75f, 0.95f, 0.32f); // Cyan
                case "WATER-SIDE BOUNDARY":
                    return new Color(0.15f, 0.45f, 0.95f, 0.40f); // Deep Ocean Blue
                default:
                    return new Color(0.3f, 0.7f, 0.4f, 0.30f); // Green / Neutral
            }
        }
    }
}
