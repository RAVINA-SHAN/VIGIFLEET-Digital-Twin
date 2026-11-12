using System;
using UnityEngine;

namespace VigiFleet.Telemetry
{
    [Serializable]
    public class ESP32TelemetryPacket
    {
        public string protocol = "VIGIFLEET";
        public int version = 1;
        public string vehicle_id;
        public string vehicle_type;
        public string vehicle_mode = "LEGACY";
        public double latitude;
        public double longitude;
        public float speed_mps;
        public float heading_deg;
        public float acceleration_mps2;
        public string vehicle_status = "MOVING";
        public string brake_status = "UNKNOWN";
        public string reverse_status = "UNKNOWN";
        public bool gnss_fix = true;
        public float position_accuracy_m = 2.5f;
        public long timestamp_ms;
        public int sequence_number;
        public string data_source = "HARDWARE";

        public float SpeedKmh => speed_mps * 3.6f;

        public static ESP32TelemetryPacket FromJson(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString)) return null;
            try
            {
                return JsonUtility.FromJson<ESP32TelemetryPacket>(jsonString);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VigiFleet] Failed to parse ESP32TelemetryPacket: {ex.Message}");
                return null;
            }
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class TelemetryUpdateWebSocketMessage
    {
        public string type;
        public ESP32TelemetryPacket packet;
        public string operating_mode;
        public long timestamp_ms;
    }
}
