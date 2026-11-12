using System;
using System.Collections.Generic;
using UnityEngine;
using VigiFleet.Core;

namespace VigiFleet.Telemetry
{
    [Serializable]
    public class VehicleStateData
    {
        public string vehicle_id;
        public string display_name;
        public string vehicle_category;
        public string vehicle_type;
        public string vehicle_mode;
        public double latitude;
        public double longitude;
        public float speed_mps;
        public float speed_kmh;
        public float heading_deg;
        public float acceleration_mps2;
        public string vehicle_status = "STOPPED";
        public string brake_status = "UNKNOWN";
        public string reverse_status = "UNKNOWN";
        public bool gnss_fix = false;
        public string gnss_status = "INVALID";
        public float position_accuracy_m = 2.5f;
        public string communication_status = "OFFLINE";
        public string risk_level = "NORMAL";
        public long last_update_ms;
        public int sequence_number;
        public string data_source = "HARDWARE";
        public string active_hazard = null;
        public List<string> active_geofences = new List<string>();

        public RiskLevel GetRiskLevelEnum()
        {
            switch (risk_level?.ToUpper())
            {
                case "CRITICAL": return RiskLevel.CRITICAL;
                case "HIGH RISK":
                case "HIGH_RISK": return RiskLevel.HIGH_RISK;
                case "CAUTION": return RiskLevel.CAUTION;
                case "NORMAL": return RiskLevel.NORMAL;
                default: return RiskLevel.UNKNOWN;
            }
        }

        public CommunicationStatus GetCommStatusEnum()
        {
            switch (communication_status?.ToUpper())
            {
                case "CONNECTED": return CommunicationStatus.CONNECTED;
                case "STALE": return CommunicationStatus.STALE;
                default: return CommunicationStatus.OFFLINE;
            }
        }

        public TriStateSignal GetBrakeSignal()
        {
            switch (brake_status?.ToUpper())
            {
                case "ON": return TriStateSignal.ON;
                case "OFF": return TriStateSignal.OFF;
                default: return TriStateSignal.UNKNOWN;
            }
        }

        public TriStateSignal GetReverseSignal()
        {
            switch (reverse_status?.ToUpper())
            {
                case "ON": return TriStateSignal.ON;
                case "OFF": return TriStateSignal.OFF;
                default: return TriStateSignal.UNKNOWN;
            }
        }

        public void UpdateFromPacket(ESP32TelemetryPacket pkt, bool isStale = false)
        {
            if (pkt == null) return;
            vehicle_id = pkt.vehicle_id;
            vehicle_type = pkt.vehicle_type;
            vehicle_mode = pkt.vehicle_mode;
            latitude = pkt.latitude;
            longitude = pkt.longitude;
            speed_mps = pkt.speed_mps;
            speed_kmh = pkt.speed_mps * 3.6f;
            heading_deg = pkt.heading_deg;
            acceleration_mps2 = pkt.acceleration_mps2;
            vehicle_status = pkt.vehicle_status;
            brake_status = string.IsNullOrEmpty(pkt.brake_status) ? "UNKNOWN" : pkt.brake_status;
            reverse_status = string.IsNullOrEmpty(pkt.reverse_status) ? "UNKNOWN" : pkt.reverse_status;
            gnss_fix = pkt.gnss_fix;
            gnss_status = pkt.gnss_fix ? "VALID" : "INVALID";
            position_accuracy_m = pkt.position_accuracy_m;
            sequence_number = pkt.sequence_number;
            data_source = pkt.data_source;
            last_update_ms = pkt.timestamp_ms > 0 ? pkt.timestamp_ms : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (isStale)
            {
                communication_status = "STALE";
            }
            else
            {
                communication_status = "CONNECTED";
            }
        }
    }
}
