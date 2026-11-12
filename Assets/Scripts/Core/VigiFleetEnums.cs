using System;

namespace VigiFleet.Core
{
    public enum VehicleType
    {
        TRUCK,
        OLD_TRUCK,
        MODERN_TRUCK,
        FORKLIFT,
        TERMINAL_TRACTOR
    }

    public enum VehicleMode
    {
        LEGACY,
        MODERN
    }

    public enum TriStateSignal
    {
        ON,
        OFF,
        UNKNOWN
    }

    public enum RiskLevel
    {
        NORMAL,
        CAUTION,
        HIGH_RISK,
        CRITICAL,
        UNKNOWN
    }

    public enum VehicleStatus
    {
        MOVING,
        STOPPED,
        IDLE,
        OFFLINE
    }

    public enum OperatingMode
    {
        SIMULATION,
        LIVE_HARDWARE
    }

    public enum CommunicationStatus
    {
        CONNECTED,
        STALE,
        OFFLINE
    }

    public enum ScenarioType
    {
        AUTO_CYCLE,
        BLIND_INTERSECTION,
        FORKLIFT_REVERSING,
        SUDDEN_STOP,
        RESTRICTED_ZONE_ENTRY,
        COMMUNICATION_FAILURE
    }

    public static class VigiFleetEnumExtensions
    {
        public static string ToDisplayString(this RiskLevel risk)
        {
            switch (risk)
            {
                case RiskLevel.NORMAL: return "NORMAL";
                case RiskLevel.CAUTION: return "CAUTION";
                case RiskLevel.HIGH_RISK: return "HIGH RISK";
                case RiskLevel.CRITICAL: return "CRITICAL";
                default: return "UNKNOWN";
            }
        }

        public static UnityEngine.Color GetRiskColor(this RiskLevel risk)
        {
            switch (risk)
            {
                case RiskLevel.NORMAL: return new UnityEngine.Color(0.12f, 0.72f, 0.33f, 1f);       // Emerald Green #1F8
                case RiskLevel.CAUTION: return new UnityEngine.Color(0.95f, 0.77f, 0.05f, 1f);      // Safety Yellow #F5C
                case RiskLevel.HIGH_RISK: return new UnityEngine.Color(0.96f, 0.50f, 0.10f, 1f);    // Warning Orange #F81
                case RiskLevel.CRITICAL: return new UnityEngine.Color(0.88f, 0.15f, 0.15f, 1f);     // Emergency Red #E22
                default: return new UnityEngine.Color(0.55f, 0.60f, 0.65f, 1f);                     // Neutral Grey
            }
        }
    }
}
