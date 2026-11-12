using System;
using System.Collections.Generic;
using UnityEngine;
using VigiFleet.Core;

namespace VigiFleet.Safety
{
    [Serializable]
    public class SafetyEventRecord
    {
        public string eventId;
        public string timestampIso;
        public long timestampMs;
        public string vehicleId;
        public string nearbyVehicleId;
        public string eventType;
        public RiskLevel riskLevel;
        public float distanceM;
        public float ttcSeconds;
        public string voiceAlertText;
        public string detectionMethod;
        public bool isResolved;
    }

    /// <summary>
    /// Manages the safety event lifecycle, audio voice alert dispatches,
    /// and event logging timeline in the VIGIFLEET Digital Twin.
    /// </summary>
    public class SafetyEventManager : MonoBehaviour
    {
        [Header("Audio Alert Settings")]
        [SerializeField] private AudioSource alertAudioSource;
        [SerializeField] private AudioClip cautionChimeClip;
        [SerializeField] private AudioClip criticalAlarmClip;

        [Header("Cooldown Settings")]
        [Tooltip("Minimum seconds between repeated voice alerts for the same vehicle pair/hazard")]
        [SerializeField] private float alertCooldownSeconds = 4.0f;

        private readonly List<SafetyEventRecord> _eventLog = new List<SafetyEventRecord>();
        private readonly Dictionary<string, float> _lastAlertTimes = new Dictionary<string, float>();

        public IReadOnlyList<SafetyEventRecord> EventLog => _eventLog;

        public event Action<SafetyEventRecord> OnEventRecorded;
        public event Action<string, RiskLevel> OnVoiceAlertDispatched;

        private void Awake()
        {
            if (alertAudioSource == null)
            {
                alertAudioSource = gameObject.AddComponent<AudioSource>();
                alertAudioSource.playOnAwake = false;
                alertAudioSource.spatialBlend = 0f; // 2D audible in control room
            }
        }

        public SafetyEventRecord TriggerSafetyEvent(
            string vehicleId,
            string nearbyVehicleId,
            string eventType,
            RiskLevel risk,
            float distanceM,
            float ttcS,
            string voiceText,
            string detectionMethod = "Deterministic Kinematic Engine")
        {
            string key = $"{vehicleId}_{nearbyVehicleId}_{eventType}";
            if (_lastAlertTimes.TryGetValue(key, out float lastTime))
            {
                if ((Time.time - lastTime) < alertCooldownSeconds)
                {
                    return null; // Suppress spam during ongoing encounter
                }
            }
            _lastAlertTimes[key] = Time.time;

            SafetyEventRecord record = new SafetyEventRecord
            {
                eventId = $"EVT-{DateTime.UtcNow:HHmmss}-{UnityEngine.Random.Range(100, 999)}",
                timestampIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                vehicleId = vehicleId,
                nearbyVehicleId = nearbyVehicleId,
                eventType = eventType,
                riskLevel = risk,
                distanceM = distanceM,
                ttcSeconds = ttcS,
                voiceAlertText = voiceText,
                detectionMethod = detectionMethod,
                isResolved = false
            };

            _eventLog.Insert(0, record);
            if (_eventLog.Count > 100)
            {
                _eventLog.RemoveAt(_eventLog.Count - 1);
            }

            PlayAlertAudio(risk);
            OnEventRecorded?.Invoke(record);
            OnVoiceAlertDispatched?.Invoke(voiceText, risk);

            return record;
        }

        private void PlayAlertAudio(RiskLevel risk)
        {
            if (alertAudioSource == null) return;

            if (risk == RiskLevel.CRITICAL && criticalAlarmClip != null)
            {
                alertAudioSource.PlayOneShot(criticalAlarmClip);
            }
            else if ((risk == RiskLevel.HIGH_RISK || risk == RiskLevel.CAUTION) && cautionChimeClip != null)
            {
                alertAudioSource.PlayOneShot(cautionChimeClip);
            }
        }
    }
}
