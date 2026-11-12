using System;
using System.Collections.Generic;
using UnityEngine;
using VigiFleet.Core;
using VigiFleet.Vehicles;

namespace VigiFleet.Safety
{
    public struct KinematicInteraction
    {
        public VehicleController VehicleA;
        public VehicleController VehicleB;
        public float DistanceM;
        public float RelativeSpeedMps;
        public float ClosingSpeedMps;
        public float TtcSeconds;
        public float CpaDistanceM;
        public bool HasConflict;
        public Vector3 ConflictPoint;
        public RiskLevel AssessedRisk;
        public string RiskReason;
    }

    /// <summary>
    /// Deterministic 2D Kinematic Interaction & Collision Predictor.
    /// Computes Relative Velocity, Line-of-Sight Projection, Closest Point of Approach (CPA),
    /// and Time-To-Collision (TTC). Strictly verifies trajectory convergence before alarming.
    /// </summary>
    public class CollisionPredictor : MonoBehaviour
    {
        [Header("Safety Thresholds (Meters / Seconds)")]
        [Tooltip("Minimum lateral safety corridor width for harbour haulage vehicles")]
        [SerializeField] private float safetyCorridorM = 4.5f;

        [Tooltip("Critical TTC threshold in seconds")]
        [SerializeField] private float criticalTtcS = 3.2f;

        [Tooltip("High Risk TTC threshold in seconds")]
        [SerializeField] private float highRiskTtcS = 6.5f;

        [Tooltip("Caution TTC threshold in seconds")]
        [SerializeField] private float cautionTtcS = 10.0f;

        public KinematicInteraction EvaluatePair(VehicleController vA, VehicleController vB)
        {
            KinematicInteraction inter = new KinematicInteraction
            {
                VehicleA = vA,
                VehicleB = vB,
                DistanceM = 0f,
                RelativeSpeedMps = 0f,
                ClosingSpeedMps = 0f,
                TtcSeconds = 99.0f,
                CpaDistanceM = 99.0f,
                HasConflict = false,
                ConflictPoint = Vector3.zero,
                AssessedRisk = RiskLevel.NORMAL,
                RiskReason = "Safe clearance"
            };

            if (vA == null || vB == null || vA == vB) return inter;

            Vector3 posA = vA.transform.position;
            Vector3 posB = vB.transform.position;
            posA.y = 0f;
            posB.y = 0f;

            Vector3 r = posB - posA;
            float dist = r.magnitude;
            inter.DistanceM = dist;

            if (dist < 0.05f)
            {
                inter.AssessedRisk = RiskLevel.CRITICAL;
                inter.HasConflict = true;
                inter.TtcSeconds = 0f;
                inter.ConflictPoint = posA;
                inter.RiskReason = "Zero clearance contact hazard";
                return inter;
            }

            // Velocities in 2D ground plane
            Vector3 vVelA = vA.transform.forward * vA.CurrentState.speed_mps;
            if (vA.CurrentState.reverse_status == "ON") vVelA = -vVelA;

            Vector3 vVelB = vB.transform.forward * vB.CurrentState.speed_mps;
            if (vB.CurrentState.reverse_status == "ON") vVelB = -vVelB;

            Vector3 vRel = vVelB - vVelA; // Relative velocity
            inter.RelativeSpeedMps = vRel.magnitude;

            // Closing speed: projection of -(vB - vA) onto unit vector r
            Vector3 uR = r.normalized;
            float closingSpeed = -Vector3.Dot(vRel, uR);
            inter.ClosingSpeedMps = Mathf.Max(0f, closingSpeed);

            // Combined GNSS uncertainty buffer
            float combinedUncertainty = Mathf.Max(1.0f, vA.CurrentState.position_accuracy_m + vB.CurrentState.position_accuracy_m);
            float conflictThresholdM = safetyCorridorM + combinedUncertainty;

            // If relative velocity is near zero, vehicles maintain relative distance
            float vRelSq = vRel.sqrMagnitude;
            if (vRelSq > 0.04f && closingSpeed > 0.25f)
            {
                // Time to Closest Point of Approach (t_CPA = -(r . vRel) / |vRel|^2)
                float tCpa = -Vector3.Dot(r, vRel) / vRelSq;

                if (tCpa > 0.01f) // Future convergence
                {
                    Vector3 rCpa = r + (vRel * tCpa);
                    float dCpa = rCpa.magnitude;
                    inter.CpaDistanceM = dCpa;

                    if (dCpa <= conflictThresholdM)
                    {
                        inter.HasConflict = true;
                        inter.TtcSeconds = tCpa;
                        inter.ConflictPoint = posA + (vVelA * tCpa);

                        // Risk level assignment
                        if (tCpa <= criticalTtcS)
                        {
                            inter.AssessedRisk = RiskLevel.CRITICAL;
                            inter.RiskReason = $"Imminent collision predicted (TTC: {tCpa:F1}s, CPA: {dCpa:F1}m)";
                        }
                        else if (tCpa <= highRiskTtcS)
                        {
                            inter.AssessedRisk = RiskLevel.HIGH_RISK;
                            inter.RiskReason = $"Trajectory conflict approaching (TTC: {tCpa:F1}s)";
                        }
                        else if (tCpa <= cautionTtcS)
                        {
                            inter.AssessedRisk = RiskLevel.CAUTION;
                            inter.RiskReason = $"Converging paths detected (TTC: {tCpa:F1}s)";
                        }
                    }
                }
            }

            // Specialized Harbour Hazard Rules:
            // Rule 1: Forklift Reversing in Proximity
            if ((vA.Profile.vehicleType == VehicleType.FORKLIFT && vA.CurrentState.reverse_status == "ON") ||
                (vB.Profile.vehicleType == VehicleType.FORKLIFT && vB.CurrentState.reverse_status == "ON"))
            {
                if (dist < 35.0f)
                {
                    RiskLevel fRisk = dist < 15.0f ? RiskLevel.HIGH_RISK : RiskLevel.CAUTION;
                    if (fRisk > inter.AssessedRisk)
                    {
                        inter.AssessedRisk = fRisk;
                        inter.HasConflict = true;
                        inter.ConflictPoint = (posA + posB) * 0.5f;
                        inter.RiskReason = $"Industrial forklift actively reversing within {dist:F1}m";
                    }
                }
            }

            // Rule 2: Sudden Stop / Emergency Braking Ahead
            bool vABraking = vA.CurrentState.brake_status == "ON" || vA.CurrentState.acceleration_mps2 < -2.5f;
            bool vBBraking = vB.CurrentState.brake_status == "ON" || vB.CurrentState.acceleration_mps2 < -2.5f;
            if (vABraking || vBBraking)
            {
                if (dist < 45.0f && closingSpeed > 1.0f)
                {
                    RiskLevel bRisk = dist < 20.0f ? RiskLevel.HIGH_RISK : RiskLevel.CAUTION;
                    if (bRisk > inter.AssessedRisk)
                    {
                        inter.AssessedRisk = bRisk;
                        inter.HasConflict = true;
                        inter.ConflictPoint = posA + (posB - posA) * 0.5f;
                        inter.RiskReason = "Lead vehicle emergency stop / high deceleration";
                    }
                }
            }

            return inter;
        }
    }
}
