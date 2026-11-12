using System.Collections.Generic;
using UnityEngine;

namespace VigiFleet.Environment
{
    /// <summary>
    /// Procedurally constructs an authentic, realistic, industrial container terminal harbour environment:
    /// Concrete terminal pavement, multi-lane asphalt road network with markings, container yards,
    /// stacked 20ft & 40ft shipping containers, STS gantry cranes, RTG cranes, warehouses,
    /// quay seawall, ocean water, security checkpoints, and high-mast lighting towers.
    /// </summary>
    public class HarbourProceduralBuilder : MonoBehaviour
    {
        [Header("Terminal Dimensions (Meters)")]
        [SerializeField] private float terminalWidth = 600.0f;
        [SerializeField] private float terminalLength = 500.0f;

        [Header("Material References / Palettes")]
        [SerializeField] private Material concreteMat;
        [SerializeField] private Material asphaltMat;
        [SerializeField] private Material roadMarkingWhiteMat;
        [SerializeField] private Material roadMarkingYellowMat;
        [SerializeField] private Material waterMat;
        [SerializeField] private Material steelCraneMat;
        [SerializeField] private Material warehouseMat;

        private static readonly Color[] ContainerColors = new Color[]
        {
            new Color(0.24f, 0.62f, 0.82f), // Maersk Light Blue
            new Color(0.95f, 0.44f, 0.06f), // Hapag-Lloyd Orange
            new Color(0.12f, 0.48f, 0.24f), // Evergreen Green
            new Color(0.08f, 0.18f, 0.42f), // CMA CGM Dark Navy
            new Color(0.78f, 0.14f, 0.14f), // Hamburg Sud Red
            new Color(0.85f, 0.85f, 0.88f), // Grey / Silver
            new Color(0.92f, 0.76f, 0.12f)  // Safety Yellow
        };

        private void Awake()
        {
            EnsureMaterials();
        }

        private void Start()
        {
            BuildTerminalEnvironment();
        }

        private void EnsureMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (concreteMat == null)
            {
                concreteMat = new Material(litShader);
                concreteMat.color = new Color(0.65f, 0.66f, 0.68f);
            }
            if (asphaltMat == null)
            {
                asphaltMat = new Material(litShader);
                asphaltMat.color = new Color(0.18f, 0.19f, 0.21f);
            }
            if (roadMarkingWhiteMat == null)
            {
                roadMarkingWhiteMat = new Material(litShader);
                roadMarkingWhiteMat.color = new Color(0.95f, 0.95f, 0.95f);
            }
            if (roadMarkingYellowMat == null)
            {
                roadMarkingYellowMat = new Material(litShader);
                roadMarkingYellowMat.color = new Color(0.95f, 0.78f, 0.10f);
            }
            if (waterMat == null)
            {
                waterMat = new Material(litShader);
                waterMat.color = new Color(0.06f, 0.22f, 0.38f, 0.85f);
            }
            if (steelCraneMat == null)
            {
                steelCraneMat = new Material(litShader);
                steelCraneMat.color = new Color(0.15f, 0.35f, 0.65f); // Port crane blue/red
            }
            if (warehouseMat == null)
            {
                warehouseMat = new Material(litShader);
                warehouseMat.color = new Color(0.75f, 0.77f, 0.80f);
            }
        }

        [ContextMenu("Rebuild Harbour Environment")]
        public void BuildTerminalEnvironment()
        {
            // Clear prior generated objects
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            BuildGroundAndQuay();
            BuildRoadNetwork();
            BuildContainerYards();
            BuildQuaysideCranes();
            BuildWarehouses();
            BuildSecurityCheckpoints();
            BuildLightingTowers();
        }

        #region 1. Ground and Quayside
        private void BuildGroundAndQuay()
        {
            GameObject groundRoot = new GameObject("GroundAndQuayside");
            groundRoot.transform.SetParent(transform);

            // Concrete Terminal Apron Ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "TerminalConcreteApron";
            ground.transform.SetParent(groundRoot.transform);
            ground.transform.position = new Vector3(-25f, -0.5f, 20f);
            ground.transform.localScale = new Vector3(terminalWidth, 1.0f, terminalLength);
            ground.GetComponent<Renderer>().material = concreteMat;

            // Concrete Seawall Quay Edge at X = 150
            GameObject seawall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seawall.name = "QuaysideSeawallEdge";
            seawall.transform.SetParent(groundRoot.transform);
            seawall.transform.position = new Vector3(155f, 0.5f, 20f);
            seawall.transform.localScale = new Vector3(8.0f, 3.0f, terminalLength);
            seawall.GetComponent<Renderer>().material = concreteMat;

            // Ocean Water Plane (East of quay wall)
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "HarbourOceanWater";
            water.transform.SetParent(groundRoot.transform);
            water.transform.position = new Vector3(290f, -0.8f, 20f);
            water.transform.localScale = new Vector3(260f, 0.5f, terminalLength * 1.3f);
            water.GetComponent<Renderer>().material = waterMat;

            // Steel Mooring Bollards along quay edge
            for (float z = -200f; z <= 220f; z += 35f)
            {
                GameObject bollard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bollard.name = "MooringBollard";
                bollard.transform.SetParent(groundRoot.transform);
                bollard.transform.position = new Vector3(153f, 2.3f, z);
                bollard.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                bollard.GetComponent<Renderer>().material = asphaltMat;
            }
        }
        #endregion

        #region 2. Asphalt Road Network
        private void BuildRoadNetwork()
        {
            GameObject roadsRoot = new GameObject("AsphaltRoadNetwork");
            roadsRoot.transform.SetParent(transform);

            // Main East-West Haulage Spine Road (Z = 0)
            CreateRoadSegment(roadsRoot.transform, "MainSpineRoad_EW", new Vector3(-35f, 0.02f, 0f), new Vector3(360f, 0.05f, 15f), isEastWest: true);

            // North Haulage Avenue (Z = 100)
            CreateRoadSegment(roadsRoot.transform, "NorthAvenue_EW", new Vector3(-35f, 0.02f, 100f), new Vector3(360f, 0.05f, 13f), isEastWest: true);

            // South Haulage Avenue (Z = -90)
            CreateRoadSegment(roadsRoot.transform, "SouthAvenue_EW", new Vector3(-35f, 0.02f, -90f), new Vector3(360f, 0.05f, 13f), isEastWest: true);

            // Cross Corridor West (X = -130)
            CreateRoadSegment(roadsRoot.transform, "CrossCorridor_West_NS", new Vector3(-130f, 0.02f, 5f), new Vector3(13f, 0.05f, 280f), isEastWest: false);

            // Container Stack C3 Intersection Corridor (X = -35) -> Blind Intersection Site
            CreateRoadSegment(roadsRoot.transform, "StackC3Corridor_NS", new Vector3(-35f, 0.02f, 5f), new Vector3(13f, 0.05f, 280f), isEastWest: false);

            // Berth 4 Crane Service Road (X = 120)
            CreateRoadSegment(roadsRoot.transform, "Berth4CraneRoad_NS", new Vector3(120f, 0.02f, 5f), new Vector3(14f, 0.05f, 320f), isEastWest: false);

            // Zebra Pedestrian Crossings
            CreateZebraCrossing(roadsRoot.transform, new Vector3(-130f, 0.06f, -15f), 13f, 5f, isEastWest: false);
            CreateZebraCrossing(roadsRoot.transform, new Vector3(-35f, 0.06f, 25f), 13f, 5f, isEastWest: false);
        }

        private void CreateRoadSegment(Transform parent, string segName, Vector3 pos, Vector3 size, bool isEastWest)
        {
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = segName;
            road.transform.SetParent(parent);
            road.transform.position = pos;
            road.transform.localScale = size;
            road.GetComponent<Renderer>().material = asphaltMat;

            // Center yellow line marking
            GameObject yellowLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            yellowLine.name = "YellowCenterLine";
            yellowLine.transform.SetParent(road.transform);
            yellowLine.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            yellowLine.transform.localScale = isEastWest ? new Vector3(1.0f, 0.1f, 0.02f) : new Vector3(0.02f, 0.1f, 1.0f);
            yellowLine.GetComponent<Renderer>().material = roadMarkingYellowMat;
        }

        private void CreateZebraCrossing(Transform parent, Vector3 center, float width, float length, bool isEastWest)
        {
            GameObject zebraRoot = new GameObject("ZebraCrossing");
            zebraRoot.transform.SetParent(parent);
            zebraRoot.transform.position = center;

            int stripes = 6;
            float step = width / (stripes * 2);

            for (int i = 0; i < stripes; i++)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.transform.SetParent(zebraRoot.transform);
                float offset = (i - stripes * 0.5f + 0.5f) * (step * 2f);
                stripe.transform.localPosition = isEastWest ? new Vector3(0f, 0.02f, offset) : new Vector3(offset, 0.02f, 0f);
                stripe.transform.localScale = isEastWest ? new Vector3(length, 0.04f, step * 0.85f) : new Vector3(step * 0.85f, 0.04f, length);
                stripe.GetComponent<Renderer>().material = roadMarkingWhiteMat;
            }
        }
        #endregion

        #region 3. Container Yards & Stacks
        private void BuildContainerYards()
        {
            GameObject yardsRoot = new GameObject("ContainerStorageYards");
            yardsRoot.transform.SetParent(transform);

            // Container Stack Block C1 (X: -110 to -50, Z: 20 to 80)
            CreateContainerBlock(yardsRoot.transform, "StackBlock_C1", new Vector3(-80f, 0f, 50f), rows: 3, columns: 4, maxTiers: 3);

            // Container Stack Block C2 (X: 10 to 80, Z: 20 to 80)
            CreateContainerBlock(yardsRoot.transform, "StackBlock_C2", new Vector3(40f, 0f, 50f), rows: 3, columns: 4, maxTiers: 4);

            // Container Stack Block C3 (X: -110 to -50, Z: -70 to -20) -> CRITICAL SIGHTLINE OBSTRUCTION
            CreateContainerBlock(yardsRoot.transform, "StackBlock_C3_BlindCorner", new Vector3(-80f, 0f, -45f), rows: 4, columns: 4, maxTiers: 4);

            // Container Stack Block C4 (X: 10 to 80, Z: -70 to -20)
            CreateContainerBlock(yardsRoot.transform, "StackBlock_C4", new Vector3(40f, 0f, -45f), rows: 3, columns: 4, maxTiers: 3);

            // North Hazmat Restricted Yard (X: -110 to -60, Z: 120 to 180)
            CreateContainerBlock(yardsRoot.transform, "HazmatYard_Restricted", new Vector3(-85f, 0f, 150f), rows: 2, columns: 3, maxTiers: 2, hazmatOnly: true);
        }

        private void CreateContainerBlock(Transform parent, string blockName, Vector3 origin, int rows, int columns, int maxTiers, bool hazmatOnly = false)
        {
            GameObject blockGo = new GameObject(blockName);
            blockGo.transform.SetParent(parent);
            blockGo.transform.position = origin;

            float containerLen = 12.2f; // 40ft container
            float containerWid = 2.44f;
            float containerHgt = 2.59f;

            int colorIdx = 0;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    int tiers = Random.Range(1, maxTiers + 1);
                    for (int t = 0; t < tiers; t++)
                    {
                        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        box.name = $"Container_{r}_{c}_T{t}";
                        box.transform.SetParent(blockGo.transform);

                        Vector3 localPos = new Vector3(
                            (c - columns * 0.5f) * (containerWid + 0.6f),
                            (t * containerHgt) + (containerHgt * 0.5f),
                            (r - rows * 0.5f) * (containerLen + 1.2f)
                        );
                        box.transform.localPosition = localPos;
                        box.transform.localScale = new Vector3(containerWid, containerHgt, containerLen);

                        Renderer rend = box.GetComponent<Renderer>();
                        Color chosenCol = hazmatOnly ? new Color(0.95f, 0.45f, 0.05f) : ContainerColors[colorIdx % ContainerColors.Length];
                        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                        mat.color = chosenCol;
                        rend.material = mat;
                        colorIdx++;
                    }
                }
            }
        }
        #endregion

        #region 4. Ship-to-Shore (STS) Gantry Cranes
        private void BuildQuaysideCranes()
        {
            GameObject cranesRoot = new GameObject("Berth4_STSGantryCranes");
            cranesRoot.transform.SetParent(transform);

            // Three massive STS Gantry Cranes along Berth 4 (Quay at X = 150)
            CreateSTSGantryCrane(cranesRoot.transform, "STS_Gantry_Crane_01", new Vector3(145f, 0f, -80f));
            CreateSTSGantryCrane(cranesRoot.transform, "STS_Gantry_Crane_02", new Vector3(145f, 0f, 20f));
            CreateSTSGantryCrane(cranesRoot.transform, "STS_Gantry_Crane_03", new Vector3(145f, 0f, 120f));
        }

        private void CreateSTSGantryCrane(Transform parent, string craneName, Vector3 basePos)
        {
            GameObject craneGo = new GameObject(craneName);
            craneGo.transform.SetParent(parent);
            craneGo.transform.position = basePos;

            // Leg Portal A (Landside)
            GameObject legA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legA.transform.SetParent(craneGo.transform);
            legA.transform.localPosition = new Vector3(-8f, 18f, 0f);
            legA.transform.localScale = new Vector3(2.5f, 36f, 16f);
            legA.GetComponent<Renderer>().material = steelCraneMat;

            // Leg Portal B (Waterside)
            GameObject legB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legB.transform.SetParent(craneGo.transform);
            legB.transform.localPosition = new Vector3(8f, 18f, 0f);
            legB.transform.localScale = new Vector3(2.5f, 36f, 16f);
            legB.GetComponent<Renderer>().material = steelCraneMat;

            // Upper Girder / Boom extending over ship
            GameObject boom = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boom.transform.SetParent(craneGo.transform);
            boom.transform.localPosition = new Vector3(18f, 38f, 0f);
            boom.transform.localScale = new Vector3(65f, 3.5f, 4.5f);
            boom.GetComponent<Renderer>().material = steelCraneMat;

            // A-Frame Tower Apex
            GameObject aframe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            aframe.transform.SetParent(craneGo.transform);
            aframe.transform.localPosition = new Vector3(0f, 48f, 0f);
            aframe.transform.localScale = new Vector3(14f, 18f, 3.5f);
            aframe.GetComponent<Renderer>().material = steelCraneMat;

            // Trolley and Container Spreader suspended under boom
            GameObject trolley = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trolley.transform.SetParent(craneGo.transform);
            trolley.transform.localPosition = new Vector3(15f, 34f, 0f);
            trolley.transform.localScale = new Vector3(4f, 2f, 3f);
            trolley.GetComponent<Renderer>().material = asphaltMat;

            GameObject spreader = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spreader.transform.SetParent(craneGo.transform);
            spreader.transform.localPosition = new Vector3(15f, 20f, 0f);
            spreader.transform.localScale = new Vector3(2.8f, 1.2f, 12.2f);
            spreader.GetComponent<Renderer>().material = roadMarkingYellowMat;
        }
        #endregion

        #region 5. Cargo Warehouses & Sheds
        private void BuildWarehouses()
        {
            GameObject shedsRoot = new GameObject("FreightWarehouses");
            shedsRoot.transform.SetParent(transform);

            // Customs CFS Warehouse 1 (X = -200, Z = 60)
            CreateWarehouseBuilding(shedsRoot.transform, "CFS_Warehouse_01", new Vector3(-200f, 0f, 60f), 45f, 12f, 90f);

            // CFS Warehouse 2 (X = -200, Z = -60)
            CreateWarehouseBuilding(shedsRoot.transform, "CFS_Warehouse_02", new Vector3(-200f, 0f, -60f), 45f, 12f, 90f);
        }

        private void CreateWarehouseBuilding(Transform parent, string whName, Vector3 pos, float width, float height, float length)
        {
            GameObject whGo = new GameObject(whName);
            whGo.transform.SetParent(parent);
            whGo.transform.position = pos;

            // Main body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(whGo.transform);
            body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            body.transform.localScale = new Vector3(width, height, length);
            body.GetComponent<Renderer>().material = warehouseMat;

            // Pitched Roof
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.transform.SetParent(whGo.transform);
            roof.transform.localPosition = new Vector3(0f, height + 1.5f, 0f);
            roof.transform.localScale = new Vector3(width + 2f, 3.0f, length + 2f);
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, 4f);
            roof.GetComponent<Renderer>().material = asphaltMat;

            // Loading Roll-Up Bay Doors along front face
            for (float z = -length * 0.35f; z <= length * 0.35f; z += 18f)
            {
                GameObject bay = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bay.transform.SetParent(whGo.transform);
                bay.transform.localPosition = new Vector3(width * 0.51f, 3f, z);
                bay.transform.localScale = new Vector3(0.3f, 5.5f, 8f);
                bay.GetComponent<Renderer>().material = roadMarkingYellowMat;
            }
        }
        #endregion

        #region 6. Security Checkpoints & Gates
        private void BuildSecurityCheckpoints()
        {
            GameObject gateRoot = new GameObject("CustomsSecurityCheckpoint");
            gateRoot.transform.SetParent(transform);

            Vector3 gatePos = new Vector3(-220f, 0f, 0f);
            gateRoot.transform.position = gatePos;

            // Overhead Inspection Canopy
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.transform.SetParent(gateRoot.transform);
            canopy.transform.localPosition = new Vector3(0f, 6.5f, 0f);
            canopy.transform.localScale = new Vector3(16f, 1.2f, 32f);
            canopy.GetComponent<Renderer>().material = steelCraneMat;

            // Canopy Support Columns
            float[] colZ = new float[] { -14f, 0f, 14f };
            foreach (float z in colZ)
            {
                GameObject colA = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                colA.transform.SetParent(gateRoot.transform);
                colA.transform.localPosition = new Vector3(-7f, 3.2f, z);
                colA.transform.localScale = new Vector3(0.6f, 3.2f, 0.6f);
                colA.GetComponent<Renderer>().material = concreteMat;

                GameObject colB = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                colB.transform.SetParent(gateRoot.transform);
                colB.transform.localPosition = new Vector3(7f, 3.2f, z);
                colB.transform.localScale = new Vector3(0.6f, 3.2f, 0.6f);
                colB.GetComponent<Renderer>().material = concreteMat;
            }

            // Security Guard Booths
            GameObject booth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            booth.transform.SetParent(gateRoot.transform);
            booth.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            booth.transform.localScale = new Vector3(3f, 3.2f, 2.5f);
            booth.GetComponent<Renderer>().material = warehouseMat;
        }
        #endregion

        #region 7. High-Mast Floodlight Towers
        private void BuildLightingTowers()
        {
            GameObject lightsRoot = new GameObject("HarbourFloodlightTowers");
            lightsRoot.transform.SetParent(transform);

            Vector3[] towerPositions = new Vector3[]
            {
                new Vector3(-130f, 0f, -80f),
                new Vector3(-130f, 0f, 80f),
                new Vector3(-35f, 0f, -90f),
                new Vector3(-35f, 0f, 90f),
                new Vector3(60f, 0f, -90f),
                new Vector3(60f, 0f, 90f),
                new Vector3(135f, 0f, -140f),
                new Vector3(135f, 0f, 140f)
            };

            foreach (var pos in towerPositions)
            {
                CreateLightTower(lightsRoot.transform, pos);
            }
        }

        private void CreateLightTower(Transform parent, Vector3 pos)
        {
            GameObject tower = new GameObject("FloodlightTower");
            tower.transform.SetParent(parent);
            tower.transform.position = pos;

            // Mast pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(tower.transform);
            pole.transform.localPosition = new Vector3(0f, 12.5f, 0f);
            pole.transform.localScale = new Vector3(0.8f, 12.5f, 0.8f);
            pole.GetComponent<Renderer>().material = concreteMat;

            // Head bracket
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.transform.SetParent(tower.transform);
            head.transform.localPosition = new Vector3(0f, 25.5f, 0f);
            head.transform.localScale = new Vector3(4.5f, 0.8f, 2.5f);
            head.GetComponent<Renderer>().material = asphaltMat;

            // Spot Light
            GameObject lightGo = new GameObject("SpotLight");
            lightGo.transform.SetParent(head.transform);
            lightGo.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            lightGo.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            Light spot = lightGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 85.0f;
            spot.spotAngle = 65.0f;
            spot.intensity = 2.2f;
            spot.color = new Color(1.0f, 0.95f, 0.85f);
        }
        #endregion
    }
}
