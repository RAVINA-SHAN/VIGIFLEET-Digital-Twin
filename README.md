# VIGIFLEET – Realistic 3D Harbour Digital Twin

**Intelligent V2V Cooperative Safety and Offline AI Voice Assistance System for Legacy and Modern Harbour Vehicles**

---

## 1. Executive Summary & Objective

The **VIGIFLEET Digital Twin** is an industrial-grade, interactive 3D virtual representation of an operational commercial container terminal. Purpose-built for technical evaluation and hackathon presentation, it simulates and mirrors heterogeneous harbour vehicle movements, Vehicle-to-Vehicle (V2V) cooperative communication, dynamic trajectory conflict predictions, geofence boundary monitoring, and Edge ML risk classification.

The system mirrors both legacy non-electronic vehicles and modern CAN-enabled vehicles, providing a real-time cyber-physical interface between physical ESP32 vehicle safety boxes, the local FastAPI edge backend, and the 3D monitoring control room.

---

## 2. System Architecture & Data Flow

```
PHYSICAL SENSORS (u-blox NEO-M8N GNSS + MPU6050 6-Axis IMU + Optional CAN)
    │
    ▼
ESP32-WROOM-32 VEHICLE SAFETY BOX (In-Cab Speaker, Status LEDs, Buzzer)
    │
    ▼ ESP-NOW 2.4 GHz Broadcast (Connectionless Beacons @ 2-10 Hz)
RECEIVING ESP32 SAFETY BOX / GATEWAY NODE
    │
    ▼ USB Serial UART / Local HTTP POST (/api/telemetry)
FASTAPI BACKEND (http://localhost:8000)
    ├── SQLite Telemetry & Event Storage (database/vigifleet.db)
    ├── Deterministic Kinematic Safety Engine (CPA / Trajectory Convergence)
    ├── Local Scikit-learn Edge ML Model (edge_ai/model/)
    └── Ray-Casting Polygon Geofence Engine (6 Harbour Zones)
    │
    ▼ WebSocket Stream (ws://localhost:8000/ws/telemetry)
UNITY 3D HARBOUR DIGITAL TWIN (D:\digital_twin)
    ├── Real-Time GNSS Local ENU Mapping (13.0825°N, 80.2900°E Origin)
    ├── V2V ESP-NOW Wireless Mesh Visualizer
    ├── Dynamic 3D Ground Conflict Overlays & TTC Countdown
    ├── 4 Interactive Demonstration Scenarios
    └── Industrial Control-Room Monitoring Dashboard
```

---

## 3. Fleet Composition & Vehicle Profiles

| Vehicle ID | Category | Mode | Electronics Profile | Telemetry & Signals |
| :--- | :--- | :--- | :--- | :--- |
| **VIGI-TRUCK-001** | Old Truck | `LEGACY` | 1998 Mechanical Diesel Drayage Truck. No factory ECU or CAN. Retrofitted with VIGIFLEET ESP32 Box, roof GNSS, and IMU. | Speed, Heading, Accel. Brake & Reverse status report `UNKNOWN`. |
| **VIGI-TRUCK-002** | Modern Truck | `MODERN` | Euro-VI Prime Mover with J1939 CAN bus interface + 40ft container semi-trailer. | Full CAN brake pedal and reverse gear telemetry. |
| **VIGI-FORK-001** | Forklift | `LEGACY` | 16-Ton Heavy Yard Forklift with roll cage, vertical mast, and IMU dead-reckoning. | Reverse gear status, low-speed turning maneuvers. |
| **VIGI-TRACTOR-001** | Terminal Tractor | `MODERN` | Ro-Ro Bomb-Cart Terminal Tractor shuttling between Berth 4 cranes and stacks. | CAN integration, high-rate ESP-NOW broadcast. |

---

## 4. Realistic 3D Harbour Environment Layout

Constructed to authentic container terminal scale (700m × 500m):
- **Concrete Apron Ground:** Heavy-duty reinforced industrial concrete pavement.
- **Asphalt Road Grid:** Multi-lane arterial spine roads, dual-lane haulage avenues, yellow dashed centerlines, white lane shoulders, and pedestrian zebra crossings.
- **Container Storage Yards:** Multi-tier container blocks (Stack C1, C2, C3, C4, and Hazmat Yard) stacked 1 to 4 tiers high with authentic shipping line liveries (Maersk Blue, Hapag-Lloyd Orange, Evergreen Green, CMA CGM Navy, Hamburg Süd Red).
- **Sightline Obstruction:** Container Stack C3 is intentionally positioned at the main crossroad intersection to physically occlude oncoming vehicles, creating a realistic blind-corner hazard.
- **Berth 4 Quayside:** Concrete seawall quay with mooring bollards and deep ocean water plane extending to the east.
- **Ship-to-Shore (STS) Gantry Cranes:** Three 45-meter industrial portal gantry cranes with outreach booms, trolleys, and container spreaders towering over Berth 4.
- **Warehouses & Facilities:** Freight sheds with corrugated siding, security checkpoint gates with inspection lanes, guard booths, and 25-meter high-mast lighting floodlight towers.

---

## 5. Real-Time GNSS Coordinate Conversion

GNSS WGS84 geographic coordinates (latitude and longitude) are mathematically transformed into Unity local Cartesian meters ($X, Y, Z$) using spherical Earth geodesy:

- **Harbour Reference Datum:**
  - Latitude Origin ($\phi_{ref}$): `13.0825° N`
  - Longitude Origin ($\lambda_{ref}$): `80.2900° E`
  - Earth Radius ($R$): `6,371,000 meters`
- **Transformation Equations:**
  $$X_{east} = (\lambda - \lambda_{ref}) \times \frac{\pi}{180} \times R \times \cos(\phi_{ref})$$
  $$Z_{north} = (\phi - \phi_{ref}) \times \frac{\pi}{180} \times R$$
  $$Y_{elevation} = 0 \text{ m (Ground Level)}$$
- **Compass Heading Mapping:**
  - $0^\circ$ = North ($+Z$), $90^\circ$ = East ($+X$), $180^\circ$ = South ($-Z$), $270^\circ$ = West ($-X$).
  - Unity Euler $Y = \text{heading\_deg}$.

---

## 6. Collision Prediction & Trajectory Conflict Engine

The deterministic safety engine never generates false collision warnings from distance alone:
1. **Relative Velocity Vector:** $\vec{v}_{rel} = \vec{v}_B - \vec{v}_A$
2. **Closing Speed:** Projected rate of closure along line-of-sight vector $\vec{r}$:
   $$v_{close} = -(\vec{v}_{rel} \cdot \hat{r})$$
3. **Time to Closest Point of Approach ($TTC$):**
   $$t_{CPA} = -\frac{\vec{r} \cdot \vec{v}_{rel}}{\|\vec{v}_{rel}\|^2}$$
4. **Closest Point of Approach Distance ($d_{CPA}$):**
   $$d_{CPA} = \|\vec{r} + \vec{v}_{rel} \cdot t_{CPA}\|$$
5. **Convergence Criteria:** Trajectory conflict is confirmed if $t_{CPA} > 0$, $v_{close} > 0.5 \text{ m/s}$, and $d_{CPA} \le (\text{corridor} + \text{acc}_A + \text{acc}_B)$.
6. **Risk Classification Levels:**
   - **NORMAL (Green):** Nominal clearance, non-converging trajectories.
   - **CAUTION (Yellow):** $TTC \le 10.0\text{s}$ or forklift reversing in proximity ($<35\text{m}$).
   - **HIGH RISK (Orange):** $TTC \le 6.5\text{s}$ or sudden deceleration ahead ($<-2.5\text{ m/s}^2$).
   - **CRITICAL (Red):** $TTC \le 3.2\text{s}$ or unauthorized restricted zone breach.
   - **UNKNOWN (Grey):** Telemetry stale ($>3.5\text{s}$) or GNSS invalid.

---

## 7. Interactive Hackathon Scenarios

Accessible via the interactive bottom control panel:

### Scenario 1 – Blind Intersection Collision Warning
- **Vehicles:** `VIGI-TRUCK-001` (Eastbound) & `VIGI-TRUCK-002` (Northbound).
- **Hazard:** Container Stack C3 occludes visual sightlines.
- **Visuals:** Dynamic conflict circle at intersection, converging trajectory lines, real-time TTC countdown, dual risk escalation, and in-cab audio voice alert: *"Warning! Vehicle approaching from your right. Yield immediately."*

### Scenario 2 – Forklift Reversing Near Truck
- **Vehicles:** `VIGI-FORK-001` (Heavy Forklift) & `VIGI-TRACTOR-001` (Terminal Tractor).
- **Hazard:** Forklift engages reverse gear (`reverse_status = ON`) and backs out of aisle at 2.8 m/s into approaching tractor's lane.
- **Visuals:** Flashing amber hazard beacon, reverse light illumination, trajectory conflict bar, and voice alert: *"Caution! Industrial forklift reversing nearby. Reduce speed."*

### Scenario 3 – Sudden Stop Emergency Braking
- **Vehicles:** Lead `VIGI-TRUCK-002` & Trailing `VIGI-TRUCK-001`.
- **Hazard:** Lead truck emergency brakes ($-4.8\text{ m/s}^2$), status shifts to `STOPPED`.
- **Visuals:** Emissive red brake lights illuminate, trailing truck closes rapidly, TTC drops below 2.5s, voice alert: *"Warning! Vehicle ahead has stopped. Apply brakes."*

### Scenario 4 – Restricted-Zone Entry
- **Vehicles:** `VIGI-TRUCK-001` approaching `GF-HAZMAT-02` (Restricted Hazmat Yard).
- **Hazard:** Unauthorized mechanical truck approaches and enters restricted boundary.
- **Visuals:** 3D perimeter fence flashes red strobe, unauthorized breach logged, voice alert: *"Warning! Unauthorized entry into Restricted Hazmat Yard. Halt immediately."*

---

## 8. Digital Twin User Interface

Designed with clean off-white / light-grey panels, navy typography (`#0B192C`), and restrained emerald/cyan accents:

- **Top Bar:** VIGIFLEET branding, Operating Mode Toggle (`[SIMULATION]` / `[LIVE HARDWARE]`), Fleet Count, Active Warning Counter, WebSocket Status badge, and Day/Night Lighting Toggle.
- **Left Fleet Panel:** Complete vehicle roster showing vehicle IDs, category badges, speed in km/h, communication state, and risk pills. Click any vehicle to track.
- **Right Telemetry Drawer:** Real-time speed gauge, heading compass, acceleration meter, GNSS status & accuracy ($\pm X.X\text{m}$), tri-state brake/reverse signals, and active geofences.
- **Edge ML Panel:** Model name, version, connection mode (`TRAINED_LOCAL_MODEL` / `DEMO_SIMULATED_FALLBACK`), predicted risk class, confidence score, and inference latency.
- **Bottom Timeline Panel:** Interactive scenario buttons, simulation playback controls (Play, Pause, Reset, 1x/2x Speed), and chronological safety event stream.
- **Camera Bar:** Instant view shortcuts:
  - `F1`: Harbour Overview (Top-down master view)
  - `F2`: Quay Cranes Berth 4 view
  - `F3`: Blind Intersection Focus
  - `F4`: In-Cab Cockpit POV
  - `F5`: Follow / Orbit Vehicle

---

## 9. FastAPI Backend & Live Hardware Integration

### Running the Backend
From the existing project folder (`D:\L&T`):
```powershell
cd "D:\L&T"
.\start_vigifleet.bat
```
Backend starts on `http://localhost:8000` (FastAPI + SQLite + WebSockets).

### Live WebSocket Feed
- The Digital Twin automatically connects to `ws://localhost:8000/ws/telemetry`.
- Real ESP32 frames sent via Serial (USB COM3) or HTTP `POST http://localhost:8000/api/telemetry` are parsed, validated, and streamed live to the 3D scene.
- Switching to **LIVE HARDWARE MODE** displays real telemetry exclusively and disables simulated kinematic overrides.

---

## 10. How to Open the Unity Project

1. Open **Unity Hub**.
2. Click **Add project from disk**.
3. Select folder: `D:\digital_twin`.
4. Open the project with **Unity 6 (6000.6.8f1)**.
5. In the Project window, navigate to `Assets/Scenes/Harbour_Main.unity` and double-click to open.
6. Press the **Play** button in Unity Editor to launch the Digital Twin!
