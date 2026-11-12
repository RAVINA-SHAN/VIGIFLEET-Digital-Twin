using System.Collections.Generic;
using UnityEngine;
using VigiFleet.Core;

namespace VigiFleet.Geofence
{
    /// <summary>
    /// Renders 3D procedural boundary perimeter lines, translucent ground fills,
    /// corner hazard warning beacons, and floating 3D labels for Harbour Geofences.
    /// </summary>
    public class GeofenceVisualizer : MonoBehaviour
    {
        [Header("Zone Reference")]
        [SerializeField] private GeofenceZone zone;

        private LineRenderer _perimeterLine;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material _fillMaterial;
        private Material _lineMaterial;
        private TextMesh _zoneLabelText;
        private Transform _labelPivot;
        private readonly List<GameObject> _cornerBeacons = new List<GameObject>();
        private bool _isBreached = false;
        private float _breachStrobeTimer = 0f;

        public GeofenceZone Zone => zone;

        public void Initialize(GeofenceZone geofenceZone)
        {
            zone = geofenceZone;
            name = $"Geofence_{zone.zoneId}";

            _fillMaterial = new Material(Shader.Find("Sprites/Default"));
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));

            BuildPerimeterLine();
            BuildGroundMesh();
            BuildCornerBeacons();
            BuildZoneLabel();
        }

        private void BuildPerimeterLine()
        {
            if (zone.worldPolygonPoints == null || zone.worldPolygonPoints.Count < 3) return;

            GameObject lineGo = new GameObject("PerimeterLine");
            lineGo.transform.SetParent(transform);
            _perimeterLine = lineGo.AddComponent<LineRenderer>();
            _perimeterLine.material = _lineMaterial;
            _perimeterLine.useWorldSpace = true;
            _perimeterLine.loop = true;
            _perimeterLine.startWidth = 0.4f;
            _perimeterLine.endWidth = 0.4f;

            Color baseColor = zone.GetZoneColor();
            baseColor.a = 0.85f;
            _perimeterLine.startColor = baseColor;
            _perimeterLine.endColor = baseColor;

            int count = zone.worldPolygonPoints.Count;
            _perimeterLine.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                Vector3 pt = zone.worldPolygonPoints[i];
                pt.y = 0.18f; // Above asphalt
                _perimeterLine.SetPosition(i, pt);
            }
        }

        private void BuildGroundMesh()
        {
            if (zone.worldPolygonPoints == null || zone.worldPolygonPoints.Count < 3) return;

            _meshFilter = gameObject.AddComponent<MeshFilter>();
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = _fillMaterial;

            Mesh mesh = new Mesh { name = $"GeofenceMesh_{zone.zoneId}" };

            int n = zone.worldPolygonPoints.Count;
            Vector3[] vertices = new Vector3[n + 1];
            Vector3 center = Vector3.zero;

            for (int i = 0; i < n; i++)
            {
                Vector3 p = zone.worldPolygonPoints[i];
                p.y = 0.08f;
                vertices[i] = p;
                center += p;
            }
            center /= n;
            vertices[n] = center;

            // Fan triangulation around center
            int[] triangles = new int[n * 3];
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                triangles[i * 3 + 0] = n;
                triangles[i * 3 + 1] = i;
                triangles[i * 3 + 2] = next;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            _meshFilter.mesh = mesh;
            _meshRenderer.material.color = zone.GetZoneColor();
        }

        private void BuildCornerBeacons()
        {
            if (zone.worldPolygonPoints == null) return;

            foreach (var pt in zone.worldPolygonPoints)
            {
                GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "WarningBeacon";
                pole.transform.SetParent(transform);
                pole.transform.position = new Vector3(pt.x, 1.2f, pt.z);
                pole.transform.localScale = new Vector3(0.2f, 1.2f, 0.2f);

                Renderer r = pole.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    r.material.color = new Color(0.9f, 0.8f, 0.1f); // Warning yellow
                }
                _cornerBeacons.Add(pole);
            }
        }

        private void BuildZoneLabel()
        {
            if (zone.worldPolygonPoints == null || zone.worldPolygonPoints.Count == 0) return;

            Vector3 center = Vector3.zero;
            foreach (var p in zone.worldPolygonPoints) center += p;
            center /= zone.worldPolygonPoints.Count;

            GameObject pivotGo = new GameObject("LabelPivot");
            pivotGo.transform.SetParent(transform);
            pivotGo.transform.position = center + Vector3.up * 4.5f;
            _labelPivot = pivotGo.transform;

            GameObject textGo = new GameObject("LabelText");
            textGo.transform.SetParent(_labelPivot);
            textGo.transform.localPosition = Vector3.zero;
            _zoneLabelText = textGo.AddComponent<TextMesh>();
            _zoneLabelText.fontSize = 28;
            _zoneLabelText.characterSize = 0.10f;
            _zoneLabelText.alignment = TextAlignment.Center;
            _zoneLabelText.anchor = TextAnchor.MiddleCenter;
            _zoneLabelText.color = Color.white;
            _zoneLabelText.text = $"{zone.zoneId}\n{zone.zoneName}\n[Max {zone.maxSpeedKmh:F0} km/h]";
        }

        private void Update()
        {
            // Make billboard face active camera
            if (_labelPivot != null && Camera.main != null)
            {
                _labelPivot.rotation = Quaternion.LookRotation(_labelPivot.position - Camera.main.transform.position);
            }

            if (_isBreached)
            {
                _breachStrobeTimer += Time.deltaTime * 6f;
                float strobe = Mathf.PingPong(_breachStrobeTimer, 1f);
                Color alertCol = Color.Lerp(Color.red, Color.yellow, strobe);
                if (_perimeterLine != null)
                {
                    _perimeterLine.startColor = alertCol;
                    _perimeterLine.endColor = alertCol;
                }
            }
        }

        public void SetBreachState(bool breached)
        {
            if (_isBreached != breached)
            {
                _isBreached = breached;
                if (!breached && _perimeterLine != null)
                {
                    Color baseCol = zone.GetZoneColor();
                    baseCol.a = 0.85f;
                    _perimeterLine.startColor = baseCol;
                    _perimeterLine.endColor = baseCol;
                }
            }
        }
    }
}
