using System;
using System.Collections.Generic;
using UnityEngine;
using VigiFleet.Communication;
using VigiFleet.Core;
using VigiFleet.Environment;
using VigiFleet.Safety;
using VigiFleet.Simulation;
using VigiFleet.Vehicles;

namespace VigiFleet.UI
{
    /// <summary>
    /// Master UI Controller for the VIGIFLEET 3D Harbour Digital Twin.
    /// Professional industrial control-room interface featuring real-time fleet roster,
    /// telemetry drawer, safety timeline, scenario controls, and Edge AI monitoring.
    /// </summary>
    public class DigitalTwinUIManager : MonoBehaviour
    {
        [Header("Manager References")]
        [SerializeField] private VigiFleetManager fleetManager;
        [SerializeField] private ScenarioController scenarioController;
        [SerializeField] private SafetyEventManager safetyEventManager;
        [SerializeField] private WebSocketClient webSocketClient;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private DayNightController dayNightController;
        [SerializeField] private EdgeMLPanelUI edgeMlPanel;

        [Header("UI Styling & Palettes")]
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _panelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _activeButtonStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _boldLabelStyle;
        private GUIStyle _badgeStyle;
        private bool _stylesInitialized = false;

        private Vector2 _eventScrollPos;
        private string _activeVoiceAlertText = null;
        private float _voiceAlertTimer = 0f;
        private RiskLevel _voiceAlertRisk = RiskLevel.NORMAL;

        private void Start()
        {
            FindReferencesIfNull();

            if (safetyEventManager != null)
            {
                safetyEventManager.OnVoiceAlertDispatched += HandleVoiceAlert;
            }
        }

        private void FindReferencesIfNull()
        {
            if (fleetManager == null) fleetManager = VigiFleetManager.Instance ?? FindFirstObjectByType<VigiFleetManager>();
            if (scenarioController == null) scenarioController = FindFirstObjectByType<ScenarioController>();
            if (safetyEventManager == null) safetyEventManager = FindFirstObjectByType<SafetyEventManager>();
            if (webSocketClient == null) webSocketClient = FindFirstObjectByType<WebSocketClient>();
            if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
            if (dayNightController == null) dayNightController = FindFirstObjectByType<DayNightController>();
            if (edgeMlPanel == null) edgeMlPanel = FindFirstObjectByType<EdgeMLPanelUI>();
        }

        private void Update()
        {
            if (_activeVoiceAlertText != null)
            {
                _voiceAlertTimer -= Time.deltaTime;
                if (_voiceAlertTimer <= 0f)
                {
                    _activeVoiceAlertText = null;
                }
            }
        }

        private void HandleVoiceAlert(string alertText, RiskLevel risk)
        {
            _activeVoiceAlertText = alertText;
            _voiceAlertRisk = risk;
            _voiceAlertTimer = 4.5f;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            Texture2D panelTex = MakeTex(2, 2, new Color(0.96f, 0.97f, 0.98f, 0.95f)); // Clean light grey #f4f9f5
            Texture2D buttonTex = MakeTex(2, 2, new Color(0.90f, 0.92f, 0.95f, 1f));
            Texture2D activeBtnTex = MakeTex(2, 2, new Color(0.12f, 0.45f, 0.85f, 1f));

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = panelTex },
                padding = new RectOffset(10, 10, 10, 10)
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.06f, 0.16f, 0.28f) } // Deep navy #0B192C
            };

            _subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.20f, 0.45f, 0.35f) } // Emerald accent
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.10f, 0.15f, 0.20f) }
            };

            _boldLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.08f, 0.12f, 0.18f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { background = buttonTex, textColor = new Color(0.10f, 0.15f, 0.22f) }
            };

            _activeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { background = activeBtnTex, textColor = Color.white }
            };

            _badgeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            float screenW = Screen.width;
            float screenH = Screen.height;

            DrawTopBar(screenW);
            DrawLeftFleetPanel(screenH);
            DrawRightTelemetryPanel(screenW, screenH);
            DrawBottomControlsAndTimeline(screenW, screenH);
            DrawCameraBar(screenW, screenH);
            DrawVoiceAlertBanner(screenW);
        }

        #region 1. Top Bar
        private void DrawTopBar(float screenW)
        {
            Rect barRect = new Rect(0, 0, screenW, 52);
            GUI.Box(barRect, "", _panelStyle);

            GUILayout.BeginArea(new Rect(15, 6, screenW - 30, 42));
            GUILayout.BeginHorizontal();

            // Branding
            GUILayout.BeginVertical();
            GUILayout.Label("<b>VIGIFLEET</b> 3D DIGITAL TWIN", _headerStyle);
            GUILayout.Label("Intelligent V2V Cooperative Safety & Offline AI Voice Assistance", _subHeaderStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Active Warning & Fleet Stats
            int vehicleCount = fleetManager != null ? fleetManager.ActiveVehicles.Count : 4;
            int warningCount = CountActiveWarnings();

            GUILayout.Label($"<b>Fleet:</b> {vehicleCount} Vehicles", _boldLabelStyle);
            GUILayout.Space(15);

            Color origCol = GUI.color;
            if (warningCount > 0)
            {
                GUI.color = new Color(0.95f, 0.25f, 0.25f);
                GUILayout.Box($"<b>ALERTS: {warningCount} ACTIVE</b>", _badgeStyle, GUILayout.Height(28));
                GUI.color = origCol;
            }
            else
            {
                GUI.color = new Color(0.20f, 0.75f, 0.35f);
                GUILayout.Box("<b>ALL CLEAR (NORMAL)</b>", _badgeStyle, GUILayout.Height(28));
                GUI.color = origCol;
            }

            GUILayout.Space(15);

            // WebSocket Connection Badge
            bool wsConnected = webSocketClient != null && webSocketClient.IsConnected;
            GUI.color = wsConnected ? new Color(0.15f, 0.75f, 0.35f) : new Color(0.65f, 0.65f, 0.68f);
            GUILayout.Box(wsConnected ? "● WS CONNECTED" : "○ WS DISCONNECTED", _badgeStyle, GUILayout.Height(28));
            GUI.color = origCol;

            GUILayout.Space(15);

            // Mode Selector Toggle
            OperatingMode curMode = fleetManager != null ? fleetManager.CurrentOperatingMode : OperatingMode.SIMULATION;
            string modeBtnText = (curMode == OperatingMode.SIMULATION) ? "MODE: SIMULATION (CLICK FOR LIVE)" : "MODE: LIVE HARDWARE (CLICK FOR SIM)";
            GUI.color = (curMode == OperatingMode.SIMULATION) ? new Color(0.25f, 0.55f, 0.90f) : new Color(0.15f, 0.75f, 0.40f);
            if (GUILayout.Button($"<b>{modeBtnText}</b>", _activeButtonStyle, GUILayout.Height(30), GUILayout.Width(240)))
            {
                OperatingMode next = (curMode == OperatingMode.SIMULATION) ? OperatingMode.LIVE_HARDWARE : OperatingMode.SIMULATION;
                if (fleetManager != null) fleetManager.SetOperatingMode(next);
            }
            GUI.color = origCol;

            GUILayout.Space(15);

            // Day / Night Toggle
            if (GUILayout.Button("☀ Day/Night", _buttonStyle, GUILayout.Height(30), GUILayout.Width(85)))
            {
                if (dayNightController != null) dayNightController.ToggleDayNight();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        #endregion

        #region 2. Left Fleet Monitoring Panel
        private void DrawLeftFleetPanel(float screenH)
        {
            float panelW = 280;
            float panelH = screenH - 52 - 170;
            Rect panelRect = new Rect(10, 60, panelW, panelH);
            GUI.Box(panelRect, "", _panelStyle);

            GUILayout.BeginArea(new Rect(panelRect.x + 8, panelRect.y + 8, panelRect.width - 16, panelRect.height - 16));
            GUILayout.Label("<b>PARTICIPATING VEHICLES</b>", _headerStyle);
            GUILayout.Space(4);

            if (fleetManager != null)
            {
                foreach (var v in fleetManager.ActiveVehicles)
                {
                    if (v == null) continue;

                    bool isSelected = (fleetManager.SelectedVehicle == v);
                    Color riskCol = v.CurrentState.GetRiskLevelEnum().GetRiskColor();

                    GUILayout.BeginVertical("box");
                    GUILayout.BeginHorizontal();

                    // Vehicle ID & Selection
                    GUIStyle btnStyle = isSelected ? _activeButtonStyle : _buttonStyle;
                    if (GUILayout.Button(v.Profile.vehicleId, btnStyle, GUILayout.Height(26)))
                    {
                        fleetManager.SelectVehicle(v);
                    }

                    // Risk Badge
                    Color orig = GUI.color;
                    GUI.color = riskCol;
                    GUILayout.Box(v.CurrentState.risk_level ?? "NORMAL", _badgeStyle, GUILayout.Width(75), GUILayout.Height(24));
                    GUI.color = orig;

                    GUILayout.EndHorizontal();

                    // Category, Speed, Comm Status
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"<b>{v.Profile.vehicleCategory}</b>", _labelStyle);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"{v.CurrentState.speed_kmh:F1} km/h", _boldLabelStyle);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Mode: {v.Profile.vehicleMode}", _labelStyle);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"Comm: {v.CurrentState.communication_status}", _labelStyle);
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                    GUILayout.Space(2);
                }
            }

            GUILayout.FlexibleSpace();

            // Communication link count
            int activeLinks = FindFirstObjectByType<V2VCommunicationVisualizer>()?.ActiveLinkCount ?? 0;
            GUILayout.Label($"<b>Active V2V Links:</b> {activeLinks} ESP-NOW Peers", _subHeaderStyle);

            GUILayout.EndArea();
        }
        #endregion

        #region 3. Right Selected Vehicle Telemetry Drawer
        private void DrawRightTelemetryPanel(float screenW, float screenH)
        {
            float panelW = 320;
            float panelH = screenH - 52 - 170;
            Rect panelRect = new Rect(screenW - panelW - 10, 60, panelW, panelH);
            GUI.Box(panelRect, "", _panelStyle);

            GUILayout.BeginArea(new Rect(panelRect.x + 8, panelRect.y + 8, panelRect.width - 16, panelRect.height - 16));
            GUILayout.Label("<b>VEHICLE TELEMETRY DRAWER</b>", _headerStyle);
            GUILayout.Space(4);

            VehicleController sel = fleetManager != null ? fleetManager.SelectedVehicle : null;
            if (sel != null)
            {
                var s = sel.CurrentState;
                var p = sel.Profile;

                GUILayout.Label($"<b>Selected:</b> {s.vehicle_id} ({p.displayName})", _boldLabelStyle);
                GUILayout.Label($"Category: {p.vehicleCategory} | Mode: {p.vehicleMode}", _labelStyle);
                GUILayout.Space(4);

                // Kinematic Gauges
                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>KINEMATICS</b>", _subHeaderStyle);
                GUILayout.Label($"Speed: <b>{s.speed_kmh:F1} km/h</b> ({s.speed_mps:F1} m/s)", _labelStyle);
                GUILayout.Label($"Heading: <b>{s.heading_deg:F0}°</b> ({GetCardinalDirection(s.heading_deg)})", _labelStyle);
                GUILayout.Label($"Acceleration: <b>{s.acceleration_mps2:F2} m/s²</b>", _labelStyle);
                GUILayout.Label($"Status: <b>{s.vehicle_status}</b>", _labelStyle);
                GUILayout.EndVertical();

                GUILayout.Space(3);

                // GNSS & Sensor Telemetry
                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>GNSS & SENSOR INTERFACES</b>", _subHeaderStyle);
                GUILayout.Label($"GNSS Fix: <b>{(s.gnss_fix ? "VALID 3D FIX" : "NO FIX")}</b>", _labelStyle);
                GUILayout.Label($"Position Accuracy: <b>±{s.position_accuracy_m:F1} m</b>", _labelStyle);
                GUILayout.Label($"Coordinates: {s.latitude:F5}, {s.longitude:F5}", _labelStyle);
                GUILayout.Label($"Sequence Number: #{s.sequence_number}", _labelStyle);
                GUILayout.EndVertical();

                GUILayout.Space(3);

                // Tri-State Status (ON / OFF / UNKNOWN)
                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>CAN / MECHANICAL SIGNALS</b>", _subHeaderStyle);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Brake Status:", _labelStyle);
                GUILayout.FlexibleSpace();
                DrawTriStateBadge(s.brake_status);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Reverse Gear:", _labelStyle);
                GUILayout.FlexibleSpace();
                DrawTriStateBadge(s.reverse_status);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                GUILayout.Space(4);

                // Edge ML panel inside drawer
                if (edgeMlPanel != null)
                {
                    Rect mlRect = GUILayoutUtility.GetRect(panelW - 20, 140);
                    edgeMlPanel.DrawGUI(mlRect);
                }
            }
            else
            {
                GUILayout.Label("No vehicle selected. Click a vehicle in the left panel or in 3D.", _labelStyle);
            }

            GUILayout.EndArea();
        }

        private void DrawTriStateBadge(string status)
        {
            Color orig = GUI.color;
            string s = status?.ToUpper() ?? "UNKNOWN";
            if (s == "ON") GUI.color = new Color(0.92f, 0.25f, 0.25f);
            else if (s == "OFF") GUI.color = new Color(0.20f, 0.70f, 0.35f);
            else GUI.color = new Color(0.60f, 0.65f, 0.70f); // UNKNOWN

            GUILayout.Box($"<b>{s}</b>", _badgeStyle, GUILayout.Width(75), GUILayout.Height(20));
            GUI.color = orig;
        }

        private string GetCardinalDirection(float deg)
        {
            deg = (deg % 360f + 360f) % 360f;
            if (deg >= 337.5f || deg < 22.5f) return "N";
            if (deg < 67.5f) return "NE";
            if (deg < 112.5f) return "E";
            if (deg < 157.5f) return "SE";
            if (deg < 202.5f) return "S";
            if (deg < 247.5f) return "SW";
            if (deg < 292.5f) return "W";
            return "NW";
        }
        #endregion

        #region 4. Bottom Panel (Scenarios & Timeline)
        private void DrawBottomControlsAndTimeline(float screenW, float screenH)
        {
            float panelH = 155;
            Rect panelRect = new Rect(10, screenH - panelH - 10, screenW - 20, panelH);
            GUI.Box(panelRect, "", _panelStyle);

            GUILayout.BeginArea(new Rect(panelRect.x + 10, panelRect.y + 6, panelRect.width - 20, panelRect.height - 12));
            GUILayout.BeginHorizontal();

            // Left Section: Scenario Controls
            GUILayout.BeginVertical(GUILayout.Width(380));
            GUILayout.Label("<b>INTERACTIVE DEMONSTRATION SCENARIOS</b>", _headerStyle);

            ScenarioType curScen = scenarioController != null ? scenarioController.CurrentScenario : ScenarioType.BLIND_INTERSECTION;

            GUILayout.BeginHorizontal();
            DrawScenarioButton("1. Blind Intersection", ScenarioType.BLIND_INTERSECTION, curScen);
            DrawScenarioButton("2. Forklift Reversing", ScenarioType.FORKLIFT_REVERSING, curScen);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawScenarioButton("3. Sudden Stop", ScenarioType.SUDDEN_STOP, curScen);
            DrawScenarioButton("4. Restricted Zone", ScenarioType.RESTRICTED_ZONE_ENTRY, curScen);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawScenarioButton("Auto Multi-Scenario Cycle", ScenarioType.AUTO_CYCLE, curScen);
            DrawScenarioButton("5. Comm Failure", ScenarioType.COMMUNICATION_FAILURE, curScen);
            GUILayout.EndHorizontal();

            // Playback buttons
            GUILayout.BeginHorizontal();
            bool isPlaying = scenarioController != null && scenarioController.IsPlaying;
            if (GUILayout.Button(isPlaying ? "⏸ Pause" : "▶ Play", _buttonStyle, GUILayout.Height(24)))
            {
                if (scenarioController != null) scenarioController.IsPlaying = !isPlaying;
            }
            if (GUILayout.Button("↺ Reset", _buttonStyle, GUILayout.Height(24)))
            {
                if (scenarioController != null) scenarioController.ResetCurrentScenario();
            }
            if (GUILayout.Button("1x Speed", _buttonStyle, GUILayout.Height(24)))
            {
                if (scenarioController != null) scenarioController.SimulationSpeed = 1.0f;
            }
            if (GUILayout.Button("2x Speed", _buttonStyle, GUILayout.Height(24)))
            {
                if (scenarioController != null) scenarioController.SimulationSpeed = 2.0f;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.Space(20);

            // Right Section: Safety Event Log Timeline
            GUILayout.BeginVertical();
            GUILayout.Label("<b>SAFETY EVENT TIMELINE</b>", _headerStyle);

            _eventScrollPos = GUILayout.BeginScrollView(_eventScrollPos, GUILayout.Height(115));
            if (safetyEventManager != null && safetyEventManager.EventLog.Count > 0)
            {
                foreach (var evt in safetyEventManager.EventLog)
                {
                    GUILayout.BeginHorizontal("box");

                    Color orig = GUI.color;
                    GUI.color = evt.riskLevel.GetRiskColor();
                    GUILayout.Box(evt.riskLevel.ToDisplayString(), _badgeStyle, GUILayout.Width(75), GUILayout.Height(20));
                    GUI.color = orig;

                    GUILayout.Label($"<b>{evt.timestampIso.Substring(11, 8)}</b>", _boldLabelStyle, GUILayout.Width(65));
                    GUILayout.Label($"<b>[{evt.eventType}]</b> {evt.vehicleId}" + (evt.nearbyVehicleId != null ? $" ↔ {evt.nearbyVehicleId}" : ""), _boldLabelStyle, GUILayout.Width(220));
                    GUILayout.Label(evt.voiceAlertText ?? "", _labelStyle);

                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("No critical safety events recorded yet. Fleet operating within nominal clearance.", _labelStyle);
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawScenarioButton(string label, ScenarioType target, ScenarioType current)
        {
            bool isActive = (target == current);
            GUIStyle s = isActive ? _activeButtonStyle : _buttonStyle;
            if (GUILayout.Button(label, s, GUILayout.Height(24)))
            {
                if (scenarioController != null) scenarioController.SetScenario(target);
            }
        }
        #endregion

        #region 5. Camera View Quick Switcher Bar
        private void DrawCameraBar(float screenW, float screenH)
        {
            Rect barRect = new Rect(screenW * 0.5f - 240, 58, 480, 32);
            GUI.Box(barRect, "", _panelStyle);

            GUILayout.BeginArea(barRect);
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>Camera:</b>", _boldLabelStyle, GUILayout.Width(60));

            CameraMode curMode = cameraController != null ? cameraController.Mode : CameraMode.PRESET_OVERVIEW;

            DrawCamButton("Overview [F1]", CameraMode.PRESET_OVERVIEW, curMode);
            DrawCamButton("Quay [F2]", CameraMode.PRESET_QUAY_CRANES, curMode);
            DrawCamButton("Crossing [F3]", CameraMode.PRESET_INTERSECTION, curMode);
            DrawCamButton("In-Cab [F4]", CameraMode.IN_CAB_POV, curMode);
            DrawCamButton("Orbit [F5]", CameraMode.ORBIT_VEHICLE, curMode);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawCamButton(string label, CameraMode target, CameraMode current)
        {
            bool isActive = (target == current);
            GUIStyle s = isActive ? _activeButtonStyle : _buttonStyle;
            if (GUILayout.Button(label, s, GUILayout.Height(22)))
            {
                if (cameraController != null) cameraController.SetCameraMode(target);
            }
        }
        #endregion

        #region 6. Voice Alert Banner
        private void DrawVoiceAlertBanner(float screenW)
        {
            if (string.IsNullOrEmpty(_activeVoiceAlertText)) return;

            float bannerW = 680;
            float bannerH = 46;
            Rect bannerRect = new Rect(screenW * 0.5f - (bannerW * 0.5f), 96, bannerW, bannerH);

            Color orig = GUI.color;
            GUI.color = _voiceAlertRisk.GetRiskColor();
            GUI.Box(bannerRect, "", _panelStyle);

            GUIStyle alertStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            GUI.Label(bannerRect, $"📢 VOICE ALERT: {_activeVoiceAlertText}", alertStyle);
            GUI.color = orig;
        }
        #endregion

        private int CountActiveWarnings()
        {
            if (fleetManager == null) return 0;
            int count = 0;
            foreach (var v in fleetManager.ActiveVehicles)
            {
                if (v != null && v.CurrentState.GetRiskLevelEnum() >= RiskLevel.HIGH_RISK) count++;
            }
            return count;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
