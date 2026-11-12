using System;
using System.Collections.Generic;
using UnityEngine;
using VigiFleet.Core;
using VigiFleet.Safety;
using VigiFleet.Vehicles;

namespace VigiFleet.Simulation
{
    /// <summary>
    /// Coordinates the 4 Interactive Technical Hackathon Demonstration Scenarios:
    /// 1. Blind Intersection Collision Warning
    /// 2. Forklift Reversing Near Truck
    /// 3. Sudden Stop Emergency Deceleration
    /// 4. Restricted-Zone Entry
    /// </summary>
    public class ScenarioController : MonoBehaviour
    {
        [Header("Active Scenario State")]
        [SerializeField] private ScenarioType currentScenario = ScenarioType.BLIND_INTERSECTION;
        [SerializeField] private bool isPlaying = true;
        [SerializeField] private float scenarioTime = 0f;
        [SerializeField] private float scenarioCycleDurationS = 18.0f;
        [SerializeField] private float simulationSpeed = 1.0f;

        [Header("Vehicle References")]
        [SerializeField] private VehicleController truck001; // Legacy Truck
        [SerializeField] private VehicleController truck002; // Modern Truck
        [SerializeField] private VehicleController fork001;  // Forklift
        [SerializeField] private VehicleController tractor001; // Terminal Tractor

        private CollisionPredictor _collisionPredictor;
        private ConflictZoneRenderer _conflictRenderer;
        private SafetyEventManager _eventManager;

        public ScenarioType CurrentScenario => currentScenario;
        public bool IsPlaying { get => isPlaying; set => isPlaying = value; }
        public float SimulationSpeed { get => simulationSpeed; set => simulationSpeed = Mathf.Clamp(value, 0.25f, 3f); }

        public event Action<ScenarioType> OnScenarioChanged;

        private void Awake()
        {
            _collisionPredictor = FindFirstObjectByType<CollisionPredictor>();
            _conflictRenderer = FindFirstObjectByType<ConflictZoneRenderer>();
            _eventManager = FindFirstObjectByType<SafetyEventManager>();
        }

        private void Start()
        {
            AutoAssignVehiclesIfNull();
            SetScenario(currentScenario);
        }

        private void AutoAssignVehiclesIfNull()
        {
            VehicleController[] allVehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
            foreach (var v in allVehicles)
            {
                if (v.Profile.vehicleId == "VIGI-TRUCK-001") truck001 = v;
                else if (v.Profile.vehicleId == "VIGI-TRUCK-002") truck002 = v;
                else if (v.Profile.vehicleId == "VIGI-FORK-001") fork001 = v;
                else if (v.Profile.vehicleId == "VIGI-TRACTOR-001") tractor001 = v;
            }
        }

        private void Update()
        {
            if (!isPlaying) return;

            scenarioTime += Time.deltaTime * simulationSpeed;

            // In Auto-Cycle mode, advance scenario every cycle duration
            if (currentScenario == ScenarioType.AUTO_CYCLE)
            {
                int phase = Mathf.FloorToInt(scenarioTime / scenarioCycleDurationS) % 4;
                switch (phase)
                {
                    case 0: ExecuteBlindIntersection(scenarioTime % scenarioCycleDurationS); break;
                    case 1: ExecuteForkliftReversing(scenarioTime % scenarioCycleDurationS); break;
                    case 2: ExecuteSuddenStop(scenarioTime % scenarioCycleDurationS); break;
                    case 3: ExecuteRestrictedZone(scenarioTime % scenarioCycleDurationS); break;
                }
            }
            else
            {
                float t = scenarioTime % scenarioCycleDurationS;
                switch (currentScenario)
                {
                    case ScenarioType.BLIND_INTERSECTION: ExecuteBlindIntersection(t); break;
                    case ScenarioType.FORKLIFT_REVERSING: ExecuteForkliftReversing(t); break;
                    case ScenarioType.SUDDEN_STOP: ExecuteSuddenStop(t); break;
                    case ScenarioType.RESTRICTED_ZONE_ENTRY: ExecuteRestrictedZone(t); break;
                    case ScenarioType.COMMUNICATION_FAILURE: ExecuteCommFailure(t); break;
                }
            }
        }

        public void SetScenario(ScenarioType scenario)
        {
            currentScenario = scenario;
            scenarioTime = 0f;
            ResetVehiclesToScenarioStarts();
            OnScenarioChanged?.Invoke(scenario);
        }

        public void ResetCurrentScenario()
        {
            scenarioTime = 0f;
            ResetVehiclesToScenarioStarts();
        }

        private void ResetVehiclesToScenarioStarts()
        {
            if (_conflictRenderer != null) _conflictRenderer.Hide();

            if (truck001 != null)
            {
                truck001.SetRiskLevel(RiskLevel.NORMAL);
                truck001.CurrentState.brake_status = "UNKNOWN";
                truck001.CurrentState.reverse_status = "UNKNOWN";
            }
            if (truck002 != null)
            {
                truck002.SetRiskLevel(RiskLevel.NORMAL);
                truck002.CurrentState.brake_status = "OFF";
                truck002.CurrentState.reverse_status = "OFF";
            }
            if (fork001 != null)
            {
                fork001.SetRiskLevel(RiskLevel.NORMAL);
                fork001.CurrentState.reverse_status = "OFF";
            }
            if (tractor001 != null)
            {
                tractor001.SetRiskLevel(RiskLevel.NORMAL);
            }
        }

        #region Scenario 1: Blind Intersection Collision Warning
        private void ExecuteBlindIntersection(float tNorm)
        {
            float progress = tNorm / scenarioCycleDurationS; // 0 to 1

            // Intersection center around (0, 0)
            Vector3 intersectionCenter = new Vector3(-35f, 0f, 15f);

            // Truck 001 moves Eastbound towards intersection
            float startDistA = 60f;
            float currentDistA = Mathf.Lerp(startDistA, -20f, progress);
            Vector3 posA = intersectionCenter + new Vector3(currentDistA - startDistA + 40f, 0f, 0f);

            if (truck001 != null)
            {
                truck001.SetTargetPosition(posA);
                truck001.SetTargetRotation(Quaternion.Euler(0f, 90f, 0f));
                truck001.CurrentState.speed_mps = 6.8f;
                truck001.CurrentState.speed_kmh = 24.5f;
            }

            // Truck 002 moves Northbound towards intersection
            float currentDistB = Mathf.Lerp(startDistA, -20f, progress);
            Vector3 posB = intersectionCenter + new Vector3(0f, 0f, currentDistB - startDistA + 40f);

            if (truck002 != null)
            {
                truck002.SetTargetPosition(posB);
                truck002.SetTargetRotation(Quaternion.Euler(0f, 0f, 0f));
                truck002.CurrentState.speed_mps = 6.5f;
                truck002.CurrentState.speed_kmh = 23.4f;
            }

            // Evaluate interaction
            if (_collisionPredictor != null && truck001 != null && truck002 != null)
            {
                var inter = _collisionPredictor.EvaluatePair(truck001, truck002);
                if (_conflictRenderer != null) _conflictRenderer.DisplayConflict(inter);

                truck001.SetRiskLevel(inter.AssessedRisk, inter.RiskReason);
                truck002.SetRiskLevel(inter.AssessedRisk, inter.RiskReason);

                if (inter.AssessedRisk >= RiskLevel.HIGH_RISK && _eventManager != null)
                {
                    _eventManager.TriggerSafetyEvent(
                        truck001.Profile.vehicleId,
                        truck002.Profile.vehicleId,
                        "Blind Intersection Conflict",
                        inter.AssessedRisk,
                        inter.DistanceM,
                        inter.TtcSeconds,
                        "Warning! Vehicle approaching from your right. Yield immediately."
                    );
                }
            }
        }
        #endregion

        #region Scenario 2: Forklift Reversing Near Truck
        private void ExecuteForkliftReversing(float tNorm)
        {
            float progress = tNorm / scenarioCycleDurationS;

            Vector3 bayCenter = new Vector3(80f, 0f, -40f);

            // Forklift reverses out of container aisle
            Vector3 forkPos = bayCenter + new Vector3(0f, 0f, Mathf.Lerp(15f, -12f, progress));
            if (fork001 != null)
            {
                fork001.SetTargetPosition(forkPos);
                fork001.SetTargetRotation(Quaternion.Euler(0f, 0f, 0f)); // Facing north while reversing south
                fork001.CurrentState.speed_mps = 2.8f;
                fork001.CurrentState.speed_kmh = 10.1f;
                fork001.CurrentState.reverse_status = "ON";
            }

            // Terminal tractor drives along cross corridor
            Vector3 tractorPos = bayCenter + new Vector3(Mathf.Lerp(-40f, 35f, progress), 0f, -10f);
            if (tractor001 != null)
            {
                tractor001.SetTargetPosition(tractorPos);
                tractor001.SetTargetRotation(Quaternion.Euler(0f, 90f, 0f));
                tractor001.CurrentState.speed_mps = 5.2f;
                tractor001.CurrentState.speed_kmh = 18.7f;
            }

            if (_collisionPredictor != null && fork001 != null && tractor001 != null)
            {
                var inter = _collisionPredictor.EvaluatePair(fork001, tractor001);
                if (_conflictRenderer != null) _conflictRenderer.DisplayConflict(inter);

                fork001.SetRiskLevel(inter.AssessedRisk, inter.RiskReason);
                tractor001.SetRiskLevel(inter.AssessedRisk, inter.RiskReason);

                if (inter.AssessedRisk >= RiskLevel.HIGH_RISK && _eventManager != null)
                {
                    _eventManager.TriggerSafetyEvent(
                        tractor001.Profile.vehicleId,
                        fork001.Profile.vehicleId,
                        "Forklift Reversing in Path",
                        inter.AssessedRisk,
                        inter.DistanceM,
                        inter.TtcSeconds,
                        "Caution! Industrial forklift reversing nearby. Reduce speed."
                    );
                }
            }
        }
        #endregion

        #region Scenario 3: Sudden Stop Detection
        private void ExecuteSuddenStop(float tNorm)
        {
            float progress = tNorm / scenarioCycleDurationS;
            bool isBrakingPhase = (progress > 0.35f && progress < 0.85f);

            Vector3 haulageLaneStart = new Vector3(-120f, 0f, 60f);

            // Lead Modern Truck 002
            float leadDist = isBrakingPhase ? 45f : Mathf.Lerp(0f, 90f, progress);
            Vector3 posLead = haulageLaneStart + new Vector3(leadDist, 0f, 0f);

            if (truck002 != null)
            {
                truck002.SetTargetPosition(posLead);
                truck002.SetTargetRotation(Quaternion.Euler(0f, 90f, 0f));
                truck002.CurrentState.speed_mps = isBrakingPhase ? 0.2f : 7.2f;
                truck002.CurrentState.speed_kmh = isBrakingPhase ? 0.7f : 26.0f;
                truck002.CurrentState.brake_status = isBrakingPhase ? "ON" : "OFF";
                truck002.CurrentState.acceleration_mps2 = isBrakingPhase ? -4.8f : 0.2f;
                truck002.CurrentState.vehicle_status = isBrakingPhase ? "STOPPED" : "MOVING";
            }

            // Trailing Legacy Truck 001 approaching from behind
            float trailDist = Mathf.Lerp(-35f, isBrakingPhase ? 30f : 65f, progress);
            Vector3 posTrail = haulageLaneStart + new Vector3(trailDist, 0f, 0f);

            if (truck001 != null)
            {
                truck001.SetTargetPosition(posTrail);
                truck001.SetTargetRotation(Quaternion.Euler(0f, 90f, 0f));
                truck001.CurrentState.speed_mps = 6.4f;
                truck001.CurrentState.speed_kmh = 23.0f;
            }

            if (_collisionPredictor != null && truck001 != null && truck002 != null)
            {
                var inter = _collisionPredictor.EvaluatePair(truck001, truck002);
                if (_conflictRenderer != null) _conflictRenderer.DisplayConflict(inter);

                truck001.SetRiskLevel(inter.AssessedRisk, inter.RiskReason);
                truck002.SetRiskLevel(inter.AssessedRisk, inter.RiskReason);

                if (isBrakingPhase && inter.AssessedRisk >= RiskLevel.HIGH_RISK && _eventManager != null)
                {
                    _eventManager.TriggerSafetyEvent(
                        truck001.Profile.vehicleId,
                        truck002.Profile.vehicleId,
                        "Sudden Deceleration Ahead",
                        inter.AssessedRisk,
                        inter.DistanceM,
                        inter.TtcSeconds,
                        "Warning! Vehicle ahead has stopped. Apply brakes."
                    );
                }
            }
        }
        #endregion

        #region Scenario 4: Restricted-Zone Entry
        private void ExecuteRestrictedZone(float tNorm)
        {
            float progress = tNorm / scenarioCycleDurationS;

            // Hazmat zone coordinates around (x: -90, z: 120)
            Vector3 hazmatApproach = new Vector3(-90f, 0f, Mathf.Lerp(70f, 140f, progress));

            if (truck001 != null)
            {
                truck001.SetTargetPosition(hazmatApproach);
                truck001.SetTargetRotation(Quaternion.Euler(0f, 0f, 0f));
                truck001.CurrentState.speed_mps = 4.8f;
                truck001.CurrentState.speed_kmh = 17.3f;
            }
        }
        #endregion

        #region Scenario 5: Communication Failure
        private void ExecuteCommFailure(float tNorm)
        {
            float progress = tNorm / scenarioCycleDurationS;

            if (fork001 != null)
            {
                if (progress > 0.35f)
                {
                    fork001.CurrentState.communication_status = "STALE";
                    fork001.CurrentState.gnss_fix = false;
                    fork001.CurrentState.position_accuracy_m = 18.5f;
                    fork001.SetRiskLevel(RiskLevel.CAUTION, "Telemetry stale (>3.5s). Lost GNSS fix.");
                }
                else
                {
                    fork001.CurrentState.communication_status = "CONNECTED";
                    fork001.CurrentState.gnss_fix = true;
                    fork001.CurrentState.position_accuracy_m = 3.2f;
                }
            }
        }
        #endregion
    }
}
