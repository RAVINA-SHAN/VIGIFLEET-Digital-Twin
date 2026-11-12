using System.Collections.Generic;
using UnityEngine;
using VigiFleet.Core;
using VigiFleet.Vehicles;

namespace VigiFleet.Communication
{
    /// <summary>
    /// Visualizes ESP-NOW 2.4 GHz Vehicle-to-Vehicle (V2V) cooperative communication links.
    /// Draws dynamic curved wireless link beams and animated packet pulses between participating vehicles.
    /// </summary>
    public class V2VCommunicationVisualizer : MonoBehaviour
    {
        [Header("Communication Parameters")]
        [Tooltip("Maximum physical range for ESP-NOW 2.4 GHz peer-to-peer broadcast (meters)")]
        [SerializeField] private float espNowMaxRangeM = 160.0f;

        [Header("Visual Styling")]
        [SerializeField] private Color linkConnectedColor = new Color(0.15f, 0.85f, 0.45f, 0.75f);
        [SerializeField] private Color linkDegradedColor = new Color(0.95f, 0.75f, 0.1f, 0.75f);
        [SerializeField] private Color linkStaleColor = new Color(0.85f, 0.25f, 0.25f, 0.65f);
        [SerializeField] private float lineWidth = 0.15f;
        [SerializeField] private float beamArcHeight = 2.5f;

        private readonly List<VehicleController> _registeredVehicles = new List<VehicleController>();
        private readonly List<LineRenderer> _linkRenderers = new List<LineRenderer>();
        private Material _lineMaterial;
        private float _packetPulseTime = 0f;

        public int ActiveLinkCount { get; private set; }

        private void Awake()
        {
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        public void RegisterVehicle(VehicleController vehicle)
        {
            if (vehicle != null && !_registeredVehicles.Contains(vehicle))
            {
                _registeredVehicles.Add(vehicle);
            }
        }

        public void UnregisterVehicle(VehicleController vehicle)
        {
            if (vehicle != null)
            {
                _registeredVehicles.Remove(vehicle);
            }
        }

        private void Update()
        {
            _packetPulseTime += Time.deltaTime * 3.0f;
            UpdateV2VLinks();
        }

        private void UpdateV2VLinks()
        {
            int linkIndex = 0;
            ActiveLinkCount = 0;

            for (int i = 0; i < _registeredVehicles.Count; i++)
            {
                var vehicleA = _registeredVehicles[i];
                if (vehicleA == null || !vehicleA.gameObject.activeInHierarchy) continue;

                for (int j = i + 1; j < _registeredVehicles.Count; j++)
                {
                    var vehicleB = _registeredVehicles[j];
                    if (vehicleB == null || !vehicleB.gameObject.activeInHierarchy) continue;

                    Vector3 posA = vehicleA.transform.position + Vector3.up * 2.2f;
                    Vector3 posB = vehicleB.transform.position + Vector3.up * 2.2f;

                    float dist = Vector3.Distance(posA, posB);
                    if (dist <= espNowMaxRangeM)
                    {
                        LineRenderer lr = GetOrCreateLineRenderer(linkIndex++);
                        DrawCurvedLink(lr, posA, posB, vehicleA, vehicleB, dist);
                        ActiveLinkCount++;
                    }
                }
            }

            // Hide unused renderers
            for (int k = linkIndex; k < _linkRenderers.Count; k++)
            {
                _linkRenderers[k].gameObject.SetActive(false);
            }
        }

        private LineRenderer GetOrCreateLineRenderer(int index)
        {
            if (index < _linkRenderers.Count)
            {
                var lr = _linkRenderers[index];
                lr.gameObject.SetActive(true);
                return lr;
            }

            GameObject linkGo = new GameObject($"V2V_Link_{index}");
            linkGo.transform.SetParent(transform);
            LineRenderer newLr = linkGo.AddComponent<LineRenderer>();
            newLr.material = _lineMaterial;
            newLr.useWorldSpace = true;
            newLr.startWidth = lineWidth;
            newLr.endWidth = lineWidth;
            newLr.positionCount = 16;
            _linkRenderers.Add(newLr);
            return newLr;
        }

        private void DrawCurvedLink(LineRenderer lr, Vector3 start, Vector3 end, VehicleController vA, VehicleController vB, float dist)
        {
            // Determine link health based on communication freshness
            Color linkColor = linkConnectedColor;
            if (vA.CurrentState.communication_status == "STALE" || vB.CurrentState.communication_status == "STALE")
            {
                linkColor = linkStaleColor;
            }
            else if (dist > (espNowMaxRangeM * 0.75f))
            {
                linkColor = linkDegradedColor;
            }

            // Pulse effect
            float pulse = Mathf.PingPong(_packetPulseTime, 0.4f);
            Color dynamicColor = Color.Lerp(linkColor, Color.white, pulse);

            lr.startColor = dynamicColor;
            lr.endColor = dynamicColor;

            int count = lr.positionCount;
            Vector3 midPoint = (start + end) * 0.5f + Vector3.up * (beamArcHeight + (dist * 0.03f));

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                // Quadratic bezier curve
                Vector3 p = (1 - t) * (1 - t) * start + 2 * (1 - t) * t * midPoint + t * t * end;
                lr.SetPosition(i, p);
            }
        }
    }
}
