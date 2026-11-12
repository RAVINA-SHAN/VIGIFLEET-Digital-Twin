using UnityEngine;
using VigiFleet.Core;

namespace VigiFleet.Safety
{
    /// <summary>
    /// Renders dynamic 3D ground conflict overlays, converging trajectory prediction lines,
    /// and floating Time-To-Collision (TTC) billboard indicators.
    /// </summary>
    public class ConflictZoneRenderer : MonoBehaviour
    {
        [Header("Visual Configuration")]
        [SerializeField] private float zoneRadius = 5.0f;
        [SerializeField] private float pulseFrequency = 4.0f;

        private LineRenderer _circleRenderer;
        private LineRenderer _trajLineA;
        private LineRenderer _trajLineB;
        private TextMesh _ttcText;
        private Transform _textPivot;
        private Material _lineMat;

        private void Awake()
        {
            _lineMat = new Material(Shader.Find("Sprites/Default"));
            CreateVisualComponents();
            Hide();
        }

        private void CreateVisualComponents()
        {
            // Ground conflict circle
            GameObject circleGo = new GameObject("ConflictCircle");
            circleGo.transform.SetParent(transform);
            _circleRenderer = circleGo.AddComponent<LineRenderer>();
            _circleRenderer.material = _lineMat;
            _circleRenderer.useWorldSpace = true;
            _circleRenderer.loop = true;
            _circleRenderer.positionCount = 36;
            _circleRenderer.startWidth = 0.25f;
            _circleRenderer.endWidth = 0.25f;

            // Trajectory lines
            GameObject lineAGo = new GameObject("TrajLine_A");
            lineAGo.transform.SetParent(transform);
            _trajLineA = lineAGo.AddComponent<LineRenderer>();
            _trajLineA.material = _lineMat;
            _trajLineA.useWorldSpace = true;
            _trajLineA.positionCount = 2;
            _trajLineA.startWidth = 0.15f;
            _trajLineA.endWidth = 0.15f;

            GameObject lineBGo = new GameObject("TrajLine_B");
            lineBGo.transform.SetParent(transform);
            _trajLineB = lineBGo.AddComponent<LineRenderer>();
            _trajLineB.material = _lineMat;
            _trajLineB.useWorldSpace = true;
            _trajLineB.positionCount = 2;
            _trajLineB.startWidth = 0.15f;
            _trajLineB.endWidth = 0.15f;

            // 3D Billboard text for TTC
            GameObject textPivotGo = new GameObject("TTCTextPivot");
            textPivotGo.transform.SetParent(transform);
            _textPivot = textPivotGo.transform;

            GameObject textGo = new GameObject("TTCLabel");
            textGo.transform.SetParent(_textPivot);
            textGo.transform.localPosition = Vector3.zero;
            _ttcText = textGo.AddComponent<TextMesh>();
            _ttcText.fontSize = 32;
            _ttcText.characterSize = 0.08f;
            _ttcText.alignment = TextAlignment.Center;
            _ttcText.anchor = TextAnchor.MiddleCenter;
            _ttcText.color = Color.white;
        }

        public void DisplayConflict(KinematicInteraction inter)
        {
            if (!inter.HasConflict || inter.AssessedRisk == RiskLevel.NORMAL)
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);

            Vector3 cp = inter.ConflictPoint;
            cp.y = 0.1f; // Slight offset above road surface

            Color riskColor = inter.AssessedRisk.GetRiskColor();
            float pulse = Mathf.PingPong(Time.time * pulseFrequency, 0.4f);
            Color dynamicColor = Color.Lerp(riskColor, Color.white, pulse);

            // Draw ground circle
            _circleRenderer.startColor = dynamicColor;
            _circleRenderer.endColor = dynamicColor;
            for (int i = 0; i < 36; i++)
            {
                float angle = (float)i / 36 * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * zoneRadius;
                float z = Mathf.Sin(angle) * zoneRadius;
                _circleRenderer.SetPosition(i, cp + new Vector3(x, 0f, z));
            }

            // Draw trajectory prediction lines
            if (inter.VehicleA != null)
            {
                _trajLineA.gameObject.SetActive(true);
                _trajLineA.startColor = dynamicColor;
                _trajLineA.endColor = dynamicColor;
                _trajLineA.SetPosition(0, inter.VehicleA.transform.position + Vector3.up * 0.3f);
                _trajLineA.SetPosition(1, cp);
            }
            else
            {
                _trajLineA.gameObject.SetActive(false);
            }

            if (inter.VehicleB != null)
            {
                _trajLineB.gameObject.SetActive(true);
                _trajLineB.startColor = dynamicColor;
                _trajLineB.endColor = dynamicColor;
                _trajLineB.SetPosition(0, inter.VehicleB.transform.position + Vector3.up * 0.3f);
                _trajLineB.SetPosition(1, cp);
            }
            else
            {
                _trajLineB.gameObject.SetActive(false);
            }

            // Update floating 3D text
            _textPivot.position = cp + Vector3.up * 3.5f;
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                _textPivot.rotation = Quaternion.LookRotation(_textPivot.position - mainCam.transform.position);
            }

            _ttcText.text = $"CONFLICT POINT\nTTC: {inter.TtcSeconds:F1}s | CPA: {inter.CpaDistanceM:F1}m\n[{inter.AssessedRisk.ToDisplayString()}]";
            _ttcText.color = dynamicColor;
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
