using System;
using UnityEngine;
using VigiFleet.Core;

namespace VigiFleet.Vehicles
{
    [Serializable]
    public class VehicleProfile
    {
        public string vehicleId = "VIGI-TRUCK-001";
        public string displayName = "Legacy Haulage Truck";
        public VehicleType vehicleType = VehicleType.TRUCK;
        public VehicleMode vehicleMode = VehicleMode.LEGACY;
        public string vehicleCategory = "Old Truck";

        [Header("Physical Dimensions (Meters)")]
        public float length = 8.5f;
        public float width = 2.5f;
        public float height = 3.2f;
        public float wheelBase = 4.8f;
        public float wheelRadius = 0.55f;

        [Header("Kinematic Capabilities")]
        public float maxSpeedKmh = 40.0f;
        public float accelerationRate = 1.8f;
        public float brakingRate = 3.5f;
        public float maxSteeringAngleDeg = 35.0f;

        [Header("Hardware Retrofit Capabilities")]
        public bool hasCanBus = false;
        public bool hasGnss = true;
        public bool hasImu = true;
        public string description = "Retrofitted with VIGIFLEET ESP32-WROOM-32 Box, u-blox GNSS, and MPU6050 IMU";

        [Header("Visual Identity")]
        public Color primaryColor = new Color(0.2f, 0.4f, 0.7f);
        public Color cabinColor = new Color(0.85f, 0.85f, 0.88f);
    }
}
