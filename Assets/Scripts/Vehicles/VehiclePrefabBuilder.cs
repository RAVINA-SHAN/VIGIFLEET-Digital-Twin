using UnityEngine;
using VigiFleet.Core;

namespace VigiFleet.Vehicles
{
    /// <summary>
    /// Constructs detailed modular 3D vehicle models for the VIGIFLEET Digital Twin:
    /// - Legacy Truck (VIGI-TRUCK-001)
    /// - Modern Truck (VIGI-TRUCK-002)
    /// - Yard Forklift (VIGI-FORK-001)
    /// - Terminal Tractor (VIGI-TRACTOR-001)
    /// </summary>
    public static class VehiclePrefabBuilder
    {
        public static GameObject BuildLegacyTruck(Vector3 spawnPos)
        {
            GameObject root = new GameObject("VIGI-TRUCK-001");
            root.transform.position = spawnPos;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.6f, 0f);
            collider.size = new Vector3(2.5f, 3.2f, 8.5f);

            VehicleController ctrl = root.AddComponent<VehicleController>();
            ctrl.Profile.vehicleId = "VIGI-TRUCK-001";
            ctrl.Profile.displayName = "Legacy Haulage Truck";
            ctrl.Profile.vehicleCategory = "Old Truck";
            ctrl.Profile.vehicleType = VehicleType.OLD_TRUCK;
            ctrl.Profile.vehicleMode = VehicleMode.LEGACY;
            ctrl.Profile.hasCanBus = false;
            ctrl.Profile.length = 8.5f;
            ctrl.Profile.width = 2.5f;
            ctrl.Profile.height = 3.2f;
            ctrl.Profile.wheelRadius = 0.55f;

            Material chassisMat = CreateMaterial(new Color(0.18f, 0.18f, 0.20f));
            Material cabMat = CreateMaterial(new Color(0.85f, 0.82f, 0.78f)); // Classic white/cream cab
            Material tipperMat = CreateMaterial(new Color(0.70f, 0.22f, 0.15f)); // Industrial rust/red tipper bed
            Material wheelMat = CreateMaterial(new Color(0.12f, 0.12f, 0.12f));

            // Chassis frame
            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.transform.SetParent(root.transform);
            chassis.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            chassis.transform.localScale = new Vector3(2.3f, 0.45f, 8.2f);
            chassis.GetComponent<Renderer>().material = chassisMat;

            // Truck Cab
            GameObject cab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cab.transform.SetParent(root.transform);
            cab.transform.localPosition = new Vector3(0f, 1.85f, 2.6f);
            cab.transform.localScale = new Vector3(2.4f, 2.2f, 2.6f);
            cab.GetComponent<Renderer>().material = cabMat;

            // Windshield
            GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
            windshield.transform.SetParent(cab.transform);
            windshield.transform.localPosition = new Vector3(0f, 0.25f, 0.51f);
            windshield.transform.localScale = new Vector3(0.9f, 0.45f, 0.05f);
            windshield.GetComponent<Renderer>().material = CreateMaterial(new Color(0.1f, 0.2f, 0.3f));

            // Rooftop External GNSS Antenna Pod (VIGIFLEET Retrofit)
            GameObject gnssPod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gnssPod.name = "Rooftop_GNSS_Pod";
            gnssPod.transform.SetParent(cab.transform);
            gnssPod.transform.localPosition = new Vector3(0f, 0.55f, 0.1f);
            gnssPod.transform.localScale = new Vector3(0.2f, 0.06f, 0.2f);
            gnssPod.GetComponent<Renderer>().material = CreateMaterial(Color.white);

            // Tipper Cargo Dump Bed
            GameObject tipper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tipper.transform.SetParent(root.transform);
            tipper.transform.localPosition = new Vector3(0f, 1.8f, -1.3f);
            tipper.transform.localScale = new Vector3(2.45f, 1.8f, 5.2f);
            tipper.GetComponent<Renderer>().material = tipperMat;

            // 6 Wheels
            CreateWheels(root.transform, wheelMat, 0.55f, 1.15f, new float[] { 2.6f }, new float[] { -1.8f, -3.2f });

            return root;
        }

        public static GameObject BuildModernTruck(Vector3 spawnPos)
        {
            GameObject root = new GameObject("VIGI-TRUCK-002");
            root.transform.position = spawnPos;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 2.0f, 0f);
            collider.size = new Vector3(2.6f, 4.0f, 14.5f);

            VehicleController ctrl = root.AddComponent<VehicleController>();
            ctrl.Profile.vehicleId = "VIGI-TRUCK-002";
            ctrl.Profile.displayName = "Modern Container Haulage Truck";
            ctrl.Profile.vehicleCategory = "Modern Truck";
            ctrl.Profile.vehicleType = VehicleType.MODERN_TRUCK;
            ctrl.Profile.vehicleMode = VehicleMode.MODERN;
            ctrl.Profile.hasCanBus = true;
            ctrl.Profile.length = 14.5f;
            ctrl.Profile.width = 2.6f;
            ctrl.Profile.height = 4.0f;
            ctrl.Profile.wheelRadius = 0.55f;

            Material cabMat = CreateMaterial(new Color(0.12f, 0.45f, 0.85f)); // Sleek Euro Blue
            Material trailerMat = CreateMaterial(new Color(0.25f, 0.25f, 0.28f));
            Material containerMat = CreateMaterial(new Color(0.24f, 0.62f, 0.82f)); // Maersk Light Blue
            Material wheelMat = CreateMaterial(new Color(0.12f, 0.12f, 0.12f));

            // Prime Mover Tractor Cab
            GameObject cab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cab.transform.SetParent(root.transform);
            cab.transform.localPosition = new Vector3(0f, 2.1f, 5.0f);
            cab.transform.localScale = new Vector3(2.55f, 2.8f, 3.2f);
            cab.GetComponent<Renderer>().material = cabMat;

            // Aerodynamic roof fairing
            GameObject fairing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fairing.transform.SetParent(cab.transform);
            fairing.transform.localPosition = new Vector3(0f, 0.55f, -0.1f);
            fairing.transform.localScale = new Vector3(0.95f, 0.25f, 0.75f);
            fairing.GetComponent<Renderer>().material = cabMat;

            // Semi-Trailer Chassis
            GameObject trailer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trailer.transform.SetParent(root.transform);
            trailer.transform.localPosition = new Vector3(0f, 1.1f, -1.5f);
            trailer.transform.localScale = new Vector3(2.5f, 0.45f, 12.8f);
            trailer.GetComponent<Renderer>().material = trailerMat;

            // 40ft ISO Container mounted on trailer
            GameObject container = GameObject.CreatePrimitive(PrimitiveType.Cube);
            container.transform.SetParent(trailer.transform);
            container.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            container.transform.localScale = new Vector3(0.98f, 5.6f, 0.95f);
            container.GetComponent<Renderer>().material = containerMat;

            // Wheels: Front steer, rear tractor tandem, trailer triple axle
            CreateWheels(root.transform, wheelMat, 0.55f, 1.15f, new float[] { 5.2f }, new float[] { 2.4f, 3.6f, -4.8f, -6.1f, -7.4f });

            return root;
        }

        public static GameObject BuildForklift(Vector3 spawnPos)
        {
            GameObject root = new GameObject("VIGI-FORK-001");
            root.transform.position = spawnPos;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.4f, 0f);
            collider.size = new Vector3(1.8f, 2.8f, 3.6f);

            VehicleController ctrl = root.AddComponent<VehicleController>();
            ctrl.Profile.vehicleId = "VIGI-FORK-001";
            ctrl.Profile.displayName = "Heavy Yard Forklift";
            ctrl.Profile.vehicleCategory = "Forklift";
            ctrl.Profile.vehicleType = VehicleType.FORKLIFT;
            ctrl.Profile.vehicleMode = VehicleMode.LEGACY;
            ctrl.Profile.hasCanBus = false;
            ctrl.Profile.length = 3.6f;
            ctrl.Profile.width = 1.8f;
            ctrl.Profile.height = 2.8f;
            ctrl.Profile.wheelRadius = 0.40f;
            ctrl.Profile.maxSpeedKmh = 18.0f;

            Material bodyMat = CreateMaterial(new Color(0.95f, 0.72f, 0.08f)); // Industrial Caterpillar Yellow
            Material mastMat = CreateMaterial(new Color(0.20f, 0.20f, 0.22f));
            Material wheelMat = CreateMaterial(new Color(0.12f, 0.12f, 0.12f));

            // Forklift body & rear counterweight
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0f, 0.75f, -0.2f);
            body.transform.localScale = new Vector3(1.7f, 0.9f, 2.6f);
            body.GetComponent<Renderer>().material = bodyMat;

            // Driver overhead roll-cage safety guard
            GameObject cage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cage.transform.SetParent(root.transform);
            cage.transform.localPosition = new Vector3(0f, 1.85f, -0.2f);
            cage.transform.localScale = new Vector3(1.4f, 1.3f, 1.4f);
            cage.GetComponent<Renderer>().material = mastMat;

            // Orange rotating safety beacon on cage roof
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.transform.SetParent(cage.transform);
            beacon.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            beacon.transform.localScale = new Vector3(0.2f, 0.12f, 0.2f);
            beacon.GetComponent<Renderer>().material = CreateMaterial(new Color(1f, 0.5f, 0f));

            // Vertical Mast Tower
            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mast.transform.SetParent(root.transform);
            mast.transform.localPosition = new Vector3(0f, 1.5f, 1.3f);
            mast.transform.localScale = new Vector3(1.1f, 2.4f, 0.25f);
            mast.GetComponent<Renderer>().material = mastMat;

            // Fork Tines
            GameObject forkLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            forkLeft.transform.SetParent(mast.transform);
            forkLeft.transform.localPosition = new Vector3(-0.35f, -0.45f, 0.8f);
            forkLeft.transform.localScale = new Vector3(0.15f, 0.08f, 1.5f);
            forkLeft.GetComponent<Renderer>().material = mastMat;

            GameObject forkRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            forkRight.transform.SetParent(mast.transform);
            forkRight.transform.localPosition = new Vector3(0.35f, -0.45f, 0.8f);
            forkRight.transform.localScale = new Vector3(0.15f, 0.08f, 1.5f);
            forkRight.GetComponent<Renderer>().material = mastMat;

            // 4 Wheels
            CreateWheels(root.transform, wheelMat, 0.40f, 0.85f, new float[] { 0.9f }, new float[] { -1.1f });

            return root;
        }

        public static GameObject BuildTerminalTractor(Vector3 spawnPos)
        {
            GameObject root = new GameObject("VIGI-TRACTOR-001");
            root.transform.position = spawnPos;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.6f, 0f);
            collider.size = new Vector3(2.5f, 3.2f, 6.2f);

            VehicleController ctrl = root.AddComponent<VehicleController>();
            ctrl.Profile.vehicleId = "VIGI-TRACTOR-001";
            ctrl.Profile.displayName = "Ro-Ro Terminal Bomb-Cart Tractor";
            ctrl.Profile.vehicleCategory = "Terminal Tractor";
            ctrl.Profile.vehicleType = VehicleType.TERMINAL_TRACTOR;
            ctrl.Profile.vehicleMode = VehicleMode.MODERN;
            ctrl.Profile.hasCanBus = true;
            ctrl.Profile.length = 6.2f;
            ctrl.Profile.width = 2.5f;
            ctrl.Profile.height = 3.2f;
            ctrl.Profile.wheelRadius = 0.50f;
            ctrl.Profile.maxSpeedKmh = 35.0f;

            Material cabMat = CreateMaterial(new Color(0.92f, 0.40f, 0.12f)); // High-visibility Port Orange
            Material chassisMat = CreateMaterial(new Color(0.20f, 0.20f, 0.22f));
            Material wheelMat = CreateMaterial(new Color(0.12f, 0.12f, 0.12f));

            // Low-Profile Offset Port Tractor Cabin
            GameObject cab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cab.transform.SetParent(root.transform);
            cab.transform.localPosition = new Vector3(-0.45f, 1.9f, 1.2f);
            cab.transform.localScale = new Vector3(1.4f, 2.3f, 2.4f);
            cab.GetComponent<Renderer>().material = cabMat;

            // Chassis deck
            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.transform.SetParent(root.transform);
            deck.transform.localPosition = new Vector3(0f, 0.85f, -0.4f);
            deck.transform.localScale = new Vector3(2.45f, 0.65f, 5.8f);
            deck.GetComponent<Renderer>().material = chassisMat;

            // Fifth Wheel Hitch Coupling Plate (Hydraulic Lift)
            GameObject fifthWheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fifthWheel.transform.SetParent(deck.transform);
            fifthWheel.transform.localPosition = new Vector3(0f, 0.58f, -0.2f);
            fifthWheel.transform.localScale = new Vector3(0.5f, 0.15f, 0.5f);
            fifthWheel.GetComponent<Renderer>().material = CreateMaterial(new Color(0.35f, 0.35f, 0.38f));

            // Wheels
            CreateWheels(root.transform, wheelMat, 0.50f, 1.15f, new float[] { 1.8f }, new float[] { -1.8f });

            return root;
        }

        private static void CreateWheels(Transform parent, Material mat, float radius, float trackWidth, float[] frontZ, float[] rearZ)
        {
            foreach (float z in frontZ)
            {
                CreateWheel(parent, mat, radius, -trackWidth, z);
                CreateWheel(parent, mat, radius, trackWidth, z);
            }
            foreach (float z in rearZ)
            {
                CreateWheel(parent, mat, radius, -trackWidth, z);
                CreateWheel(parent, mat, radius, trackWidth, z);
            }
        }

        private static void CreateWheel(Transform parent, Material mat, float radius, float x, float z)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "Wheel";
            wheel.transform.SetParent(parent);
            wheel.transform.localPosition = new Vector3(x, radius, z);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = new Vector3(radius * 2f, 0.35f, radius * 2f);
            wheel.GetComponent<Renderer>().material = mat;
        }

        private static Material CreateMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            return mat;
        }
    }
}
