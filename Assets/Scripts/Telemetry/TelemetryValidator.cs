using System;
using System.Collections.Generic;
using UnityEngine;

namespace VigiFleet.Telemetry
{
    public class TelemetryValidator
    {
        public const float MaxHarbourSpeedMps = 35.0f;     // ~126 km/h physical limit in port
        public const float PoorGnssAccuracyM = 15.0f;      // Degraded accuracy threshold
        public const long StaleThresholdMs = 3500;         // Stale warning after 3.5 seconds
        public const long OfflineThresholdMs = 10000;      // Offline status after 10 seconds

        private readonly Dictionary<string, int> _lastSequenceNumbers = new Dictionary<string, int>();
        private readonly Dictionary<string, long> _lastPacketTimestamps = new Dictionary<string, long>();

        public struct ValidationResult
        {
            public bool Valid;
            public string Reason;
            public bool IsDuplicate;
            public bool IsStale;
            public bool GnssValid;
            public bool PoorAccuracy;
            public bool ImpossibleSpeed;
        }

        public ValidationResult Validate(ESP32TelemetryPacket pkt, long currentTimestampMs)
        {
            ValidationResult res = new ValidationResult
            {
                Valid = true,
                Reason = null,
                IsDuplicate = false,
                IsStale = false,
                GnssValid = false,
                PoorAccuracy = false,
                ImpossibleSpeed = false
            };

            if (pkt == null)
            {
                res.Valid = false;
                res.Reason = "Null telemetry packet";
                return res;
            }

            if (string.IsNullOrEmpty(pkt.vehicle_id))
            {
                res.Valid = false;
                res.Reason = "Missing vehicle_id";
                return res;
            }

            // Plausibility: Speed limit
            if (pkt.speed_mps < 0f || pkt.speed_mps > MaxHarbourSpeedMps)
            {
                res.Valid = false;
                res.ImpossibleSpeed = true;
                res.Reason = $"Physically impossible speed: {pkt.speed_mps:F1} m/s (limit: {MaxHarbourSpeedMps} m/s)";
                return res;
            }

            // GNSS Bounds
            if (pkt.latitude < -90.0 || pkt.latitude > 90.0 || pkt.longitude < -180.0 || pkt.longitude > 180.0)
            {
                res.Valid = false;
                res.Reason = $"Coordinates out of range: ({pkt.latitude}, {pkt.longitude})";
                return res;
            }

            // GNSS validity
            if (pkt.gnss_fix && Math.Abs(pkt.latitude) > 0.001 && Math.Abs(pkt.longitude) > 0.001)
            {
                res.GnssValid = true;
            }

            // Position accuracy
            if (pkt.position_accuracy_m > PoorGnssAccuracyM)
            {
                res.PoorAccuracy = true;
            }

            // Stale detection
            long messageAgeMs = currentTimestampMs - pkt.timestamp_ms;
            if (messageAgeMs > StaleThresholdMs)
            {
                res.IsStale = true;
            }

            // Sequence deduplication
            if (_lastSequenceNumbers.TryGetValue(pkt.vehicle_id, out int lastSeq))
            {
                if (pkt.sequence_number == lastSeq)
                {
                    res.Valid = false;
                    res.IsDuplicate = true;
                    res.Reason = $"Duplicate sequence number {pkt.sequence_number} for {pkt.vehicle_id}";
                    return res;
                }
            }

            // Record valid sequence and timestamp
            _lastSequenceNumbers[pkt.vehicle_id] = pkt.sequence_number;
            _lastPacketTimestamps[pkt.vehicle_id] = currentTimestampMs;

            return res;
        }

        public bool CheckIfOffline(string vehicleId, long currentTimestampMs)
        {
            if (_lastPacketTimestamps.TryGetValue(vehicleId, out long lastMs))
            {
                return (currentTimestampMs - lastMs) > OfflineThresholdMs;
            }
            return true;
        }
    }
}
