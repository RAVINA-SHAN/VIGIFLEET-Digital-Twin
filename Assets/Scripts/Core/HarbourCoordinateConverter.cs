using System;
using UnityEngine;

namespace VigiFleet.Core
{
    /// <summary>
    /// Accurate WGS84 GNSS Latitude / Longitude to Unity Local World (ENU East-North-Up) Coordinate Converter.
    /// Uses spherical Earth geodesy calibrated to the specific harbour reference datum.
    /// </summary>
    public class HarbourCoordinateConverter : MonoBehaviour
    {
        public static HarbourCoordinateConverter Instance { get; private set; }

        [Header("Harbour Reference Datum (WGS84)")]
        [Tooltip("Harbour origin latitude (degrees)")]
        [SerializeField] private double referenceLatitude = 13.0825;

        [Tooltip("Harbour origin longitude (degrees)")]
        [SerializeField] private double referenceLongitude = 80.2900;

        [Tooltip("Ground elevation level in Unity world space")]
        [SerializeField] private float groundElevationY = 0f;

        [Header("Geodetic Constants")]
        private const double EarthRadiusM = 6371000.0;
        private const double DegToRad = Math.PI / 180.0;
        private const double RadToDeg = 180.0 / Math.PI;

        public double ReferenceLatitude => referenceLatitude;
        public double ReferenceLongitude => referenceLongitude;
        public float GroundElevationY => groundElevationY;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Converts WGS84 coordinates into Unity 3D local world coordinates.
        /// X = East (meters)
        /// Y = Elevation (meters)
        /// Z = North (meters)
        /// </summary>
        public Vector3 LatLonToWorldPosition(double lat, double lon, float altitudeM = 0f)
        {
            double dLat = (lat - referenceLatitude) * DegToRad;
            double dLon = (lon - referenceLongitude) * DegToRad;

            double northM = dLat * EarthRadiusM;
            double eastM = dLon * EarthRadiusM * Math.Cos(referenceLatitude * DegToRad);

            return new Vector3((float)eastM, groundElevationY + altitudeM, (float)northM);
        }

        /// <summary>
        /// Converts a Unity 3D world position back to WGS84 Latitude and Longitude.
        /// </summary>
        public (double lat, double lon) WorldPositionToLatLon(Vector3 worldPos)
        {
            double northM = worldPos.z;
            double eastM = worldPos.x;

            double dLat = northM / EarthRadiusM;
            double dLon = eastM / (EarthRadiusM * Math.Cos(referenceLatitude * DegToRad));

            double lat = referenceLatitude + (dLat * RadToDeg);
            double lon = referenceLongitude + (dLon * RadToDeg);

            return (lat, lon);
        }

        /// <summary>
        /// Calculates great-circle distance in meters between two lat/lon coordinates.
        /// </summary>
        public static double HaversineDistanceM(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = (lat2 - lat1) * DegToRad;
            double dLon = (lon2 - lon1) * DegToRad;

            double phi1 = lat1 * DegToRad;
            double phi2 = lat2 * DegToRad;

            double a = Math.Sin(dLat * 0.5) * Math.Sin(dLat * 0.5) +
                       Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(dLon * 0.5) * Math.Sin(dLon * 0.5);

            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(Math.Max(0.0, 1.0 - a)));
            return EarthRadiusM * c;
        }

        /// <summary>
        /// Maps standard compass heading (0 = North, 90 = East, 180 = South, 270 = West)
        /// to Unity Y-axis Euler rotation in degrees.
        /// </summary>
        public static float CompassHeadingToUnityYaw(float headingDeg)
        {
            float norm = (headingDeg % 360f);
            if (norm < 0f) norm += 360f;
            return norm;
        }

        /// <summary>
        /// Maps Unity Y-axis Euler rotation back to compass heading.
        /// </summary>
        public static float UnityYawToCompassHeading(float yawDeg)
        {
            float norm = (yawDeg % 360f);
            if (norm < 0f) norm += 360f;
            return norm;
        }
    }
}
