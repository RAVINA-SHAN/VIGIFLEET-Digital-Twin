using System;
using System.Collections.Generic;
using UnityEngine;

namespace VigiFleet.Vehicles
{
    [RequireComponent(typeof(VehicleController))]
    public class VehicleWaypointFollower : MonoBehaviour
    {
        [Header("Path Settings")]
        [SerializeField] private List<Vector3> waypoints = new List<Vector3>();
        [SerializeField] private bool loopPath = true;
        [SerializeField] private float arrivalDistance = 3.0f;
        [SerializeField] private float targetSpeedKmh = 25.0f;
        [SerializeField] private bool isReversing = false;

        [Header("Controls")]
        [SerializeField] private bool isNavigationActive = true;

        private VehicleController _vehicleController;
        private int _currentWaypointIndex = 0;
        private float _currentSpeedMps = 0f;

        public bool IsNavigationActive
        {
            get => isNavigationActive;
            set => isNavigationActive = value;
        }

        public float TargetSpeedKmh
        {
            get => targetSpeedKmh;
            set => targetSpeedKmh = value;
        }

        public bool IsReversing
        {
            get => isReversing;
            set => isReversing = value;
        }

        private void Awake()
        {
            _vehicleController = GetComponent<VehicleController>();
        }

        public void SetWaypoints(IEnumerable<Vector3> newWaypoints, bool loop = true)
        {
            waypoints.Clear();
            if (newWaypoints != null) waypoints.AddRange(newWaypoints);
            loopPath = loop;
            _currentWaypointIndex = 0;
        }

        private void Update()
        {
            if (!isNavigationActive || waypoints.Count == 0) return;

            Vector3 currentPos = transform.position;
            Vector3 targetWp = waypoints[_currentWaypointIndex];

            // Ignore Y for ground pathfinding
            targetWp.y = currentPos.y;

            Vector3 toTarget = targetWp - currentPos;
            float dist = toTarget.magnitude;

            if (dist <= arrivalDistance)
            {
                _currentWaypointIndex++;
                if (_currentWaypointIndex >= waypoints.Count)
                {
                    if (loopPath)
                    {
                        _currentWaypointIndex = 0;
                    }
                    else
                    {
                        isNavigationActive = false;
                        _currentSpeedMps = 0f;
                        return;
                    }
                }
                targetWp = waypoints[_currentWaypointIndex];
                targetWp.y = currentPos.y;
                toTarget = targetWp - currentPos;
            }

            Vector3 moveDir = toTarget.normalized;
            float targetSpeedMps = targetSpeedKmh / 3.6f;

            // Turn slowdown
            float angleToTarget = Vector3.Angle(isReversing ? -transform.forward : transform.forward, moveDir);
            if (angleToTarget > 30f)
            {
                targetSpeedMps *= Mathf.Clamp01(1f - (angleToTarget / 90f));
            }

            _currentSpeedMps = Mathf.MoveTowards(_currentSpeedMps, targetSpeedMps, Time.deltaTime * _vehicleController.Profile.accelerationRate);

            // Calculate rotation
            Vector3 lookDirection = isReversing ? -moveDir : moveDir;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDirection, Vector3.up);
                _vehicleController.SetTargetRotation(targetRot);
            }

            // Move position
            Vector3 nextPos = currentPos + (isReversing ? -transform.forward : transform.forward) * (_currentSpeedMps * Time.deltaTime);
            _vehicleController.SetTargetPosition(nextPos);

            // Update telemetry state
            _vehicleController.CurrentState.speed_mps = _currentSpeedMps;
            _vehicleController.CurrentState.speed_kmh = _currentSpeedMps * 3.6f;
            _vehicleController.CurrentState.heading_deg = transform.eulerAngles.y;
            _vehicleController.CurrentState.vehicle_status = _currentSpeedMps > 0.1f ? "MOVING" : "STOPPED";
            _vehicleController.CurrentState.reverse_status = isReversing ? "ON" : "OFF";
        }
    }
}
