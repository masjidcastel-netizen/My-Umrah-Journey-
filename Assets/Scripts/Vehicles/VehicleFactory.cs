using UnityEngine;

namespace MyUmrah
{
    /// <summary>Builds drivable vehicles from primitives + WheelColliders. TEMPORARY PLACEHOLDER MODELS.</summary>
    public static class VehicleFactory
    {
        struct Spec
        {
            public Vector3 body; public float bodyY;
            public Vector3 cabin; public float cabinY, cabinZ;
            public float wheelR, wheelX, wheelZF, wheelZR, wheelW;
            public float mass, torque, topKmh, steer, spring, damper, chase, burn;
            public Vector3 seat; public bool hideLegs; public string nameKey;
            public float colX, colY, colZ, colH;
        }

        static Spec SpecFor(VehicleKind k)
        {
            switch (k)
            {
                case VehicleKind.Bus:
                    return new Spec
                    {
                        body = new Vector3(2.5f, 2.4f, 10.5f), bodyY = 1.85f,
                        cabin = new Vector3(2.52f, 0.9f, 9.2f), cabinY = 2.35f, cabinZ = -0.3f,
                        wheelR = 0.5f, wheelX = 1.12f, wheelZF = 3.4f, wheelZR = -3.2f, wheelW = 0.3f,
                        mass = 8000f, torque = 5200f, topKmh = 90f, steer = 28f, spring = 90000f, damper = 9000f,
                        chase = 13f, burn = 0.12f, seat = new Vector3(0.75f, 0.55f, 4.3f), hideLegs = true, nameKey = "veh_bus",
                        colX = 2.5f, colY = 2.4f, colZ = 10.5f, colH = 1.85f
                    };
                case VehicleKind.Motorcycle:
                    return new Spec
                    {
                        body = new Vector3(0.28f, 0.45f, 1.5f), bodyY = 0.75f,
                        cabin = new Vector3(0.25f, 0.15f, 0.5f), cabinY = 1.05f, cabinZ = -0.2f,
                        wheelR = 0.33f, wheelX = 0f, wheelZF = 0.75f, wheelZR = -0.75f, wheelW = 0.12f,
                        mass = 180f, torque = 260f, topKmh = 110f, steer = 30f, spring = 20000f, damper = 2000f,
                        chase = 5.5f, burn = 0.02f, seat = new Vector3(0f, -0.05f, -0.15f), hideLegs = false, nameKey = "veh_bike",
                        colX = 0.5f, colY = 1.0f, colZ = 1.8f, colH = 0.8f
                    };
                default:
                    return new Spec
                    {
                        body = new Vector3(1.8f, 0.55f, 4.1f), bodyY = 0.72f,
                        cabin = new Vector3(1.6f, 0.7f, 2.1f), cabinY = 1.4f, cabinZ = -0.15f,
                        wheelR = 0.34f, wheelX = 0.84f, wheelZF = 1.3f, wheelZR = -1.3f, wheelW = 0.22f,
                        mass = 1100f, torque = 1500f, topKmh = 130f, steer = 32f, spring = 35000f, damper = 4500f,
                        chase = 7f, burn = 0.05f, seat = new Vector3(0.4f, -0.1f, 0.15f), hideLegs = true,
                        nameKey = k == VehicleKind.Taxi ? "veh_taxi" : "veh_car",
                        colX = 1.8f, colY = 0.9f, colZ = 4.1f, colH = 0.85f
                    };
            }
        }

        static void BuildBody(Transform root, VehicleKind kind, Spec s, Material bodyMat)
        {
            Prim.Box("Body", root, new Vector3(0, s.bodyY, 0), s.body, bodyMat);
            if (kind == VehicleKind.Motorcycle)
            {
                Prim.Box("Seat", root, new Vector3(0, 0.95f, -0.25f), new Vector3(0.22f, 0.12f, 0.7f), Mats.Get("Dark"));
                Prim.Box("Handlebar", root, new Vector3(0, 1.1f, 0.6f), new Vector3(0.7f, 0.05f, 0.05f), Mats.Get("Chrome"));
                Prim.Box("Headlight", root, new Vector3(0, 0.95f, 0.78f), new Vector3(0.18f, 0.18f, 0.1f), Mats.Get("Chrome"), false, default(Vector3), false);
                return;
            }
            Prim.Box("Cabin", root, new Vector3(0, s.cabinY, s.cabinZ), s.cabin, Mats.Get("Glass"), false, default(Vector3), false);
            Prim.Box("Roof", root, new Vector3(0, s.cabinY + s.cabin.y * 0.5f + 0.04f, s.cabinZ), new Vector3(s.cabin.x + 0.04f, 0.08f, s.cabin.z + 0.04f), bodyMat);
            float fz = s.body.z * 0.5f;
            Prim.Box("HeadlightL", root, new Vector3(-s.body.x * 0.3f, s.bodyY, fz + 0.01f), new Vector3(0.3f, 0.15f, 0.05f), Mats.Get("Chrome"), false, default(Vector3), false);
            Prim.Box("HeadlightR", root, new Vector3(s.body.x * 0.3f, s.bodyY, fz + 0.01f), new Vector3(0.3f, 0.15f, 0.05f), Mats.Get("Chrome"), false, default(Vector3), false);
            Prim.Box("Tail", root, new Vector3(0, s.bodyY, -fz - 0.01f), new Vector3(s.body.x * 0.7f, 0.12f, 0.05f), Mats.Get("AwningRed"), false, default(Vector3), false);
            if (kind == VehicleKind.Taxi)
                Prim.Box("TaxiSign", root, new Vector3(0, s.cabinY + s.cabin.y * 0.5f + 0.2f, s.cabinZ), new Vector3(0.5f, 0.18f, 0.22f), Mats.Get("CarWhite"));
        }

        static Material BodyMaterial(VehicleKind k, Material tint)
        {
            if (tint != null) return tint;
            switch (k)
            {
                case VehicleKind.Taxi: return Mats.Get("TaxiYellow");
                case VehicleKind.Bus: return Mats.Get("BusBlue");
                case VehicleKind.Motorcycle: return Mats.Get("CarRed");
                default: return Mats.Get("CarRed");
            }
        }

        /// <summary>Fully drivable vehicle (Rigidbody + WheelColliders).</summary>
        public static VehicleController Create(VehicleKind kind, Vector3 pos, float yawDeg, Material tint = null)
        {
            Spec s = SpecFor(kind);
            var go = new GameObject(kind.ToString());
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yawDeg, 0f));

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = s.mass;
            rb.centerOfMass = new Vector3(0f, 0.45f, 0f);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (kind == VehicleKind.Motorcycle) rb.constraints = RigidbodyConstraints.FreezeRotationZ; // arcade bike stays upright

            var vc = go.AddComponent<VehicleController>();
            vc.kind = kind; vc.nameKey = s.nameKey;
            vc.motorTorque = s.torque; vc.topSpeedKmh = s.topKmh; vc.maxSteer = s.steer;
            vc.brakeTorque = s.mass * 3f; vc.chaseDistance = s.chase; vc.burnPerSecond = s.burn;
            vc.hideLegs = s.hideLegs; vc.range = kind == VehicleKind.Bus ? 7f : 4.5f;
            vc.fuelCapacity = kind == VehicleKind.Bus ? 120f : (kind == VehicleKind.Motorcycle ? 12f : 45f);
            vc.fuel = vc.fuelCapacity * 0.7f;

            BuildBody(go.transform, kind, s, BodyMaterial(kind, tint));

            var col = new GameObject("BodyCollider");
            col.transform.SetParent(go.transform, false);
            var bc = col.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, s.colH, 0f);
            bc.size = new Vector3(s.colX, s.colY, s.colZ);

            var seat = new GameObject("Seat").transform; seat.SetParent(go.transform, false); seat.localPosition = s.seat;
            var exit = new GameObject("ExitPoint").transform; exit.SetParent(go.transform, false);
            exit.localPosition = new Vector3(-(s.colX * 0.5f + 0.9f), 0.2f, s.seat.z);
            var pivot = new GameObject("CameraPivot").transform; pivot.SetParent(go.transform, false);
            pivot.localPosition = new Vector3(0f, s.bodyY + s.body.y + 0.9f, 0f);
            var fp = new GameObject("FirstPersonPoint").transform; fp.SetParent(go.transform, false);
            fp.localPosition = s.seat + new Vector3(0f, 1.62f, 0.12f);
            vc.seat = seat; vc.exitPoint = exit; vc.cameraPivot = pivot; vc.fpPoint = fp;

            Material tyre = Mats.Get("Tyre");
            float restY = s.wheelR + 0.14f + 0.05f;
            if (kind == VehicleKind.Motorcycle)
            {
                AddWheel(go.transform, vc, new Vector3(0f, restY, s.wheelZF), s, true, false, tyre);
                AddWheel(go.transform, vc, new Vector3(0f, restY, s.wheelZR), s, false, true, tyre);
            }
            else
            {
                AddWheel(go.transform, vc, new Vector3(-s.wheelX, restY, s.wheelZF), s, true, false, tyre);
                AddWheel(go.transform, vc, new Vector3(s.wheelX, restY, s.wheelZF), s, true, false, tyre);
                AddWheel(go.transform, vc, new Vector3(-s.wheelX, restY, s.wheelZR), s, false, true, tyre);
                AddWheel(go.transform, vc, new Vector3(s.wheelX, restY, s.wheelZR), s, false, true, tyre);
            }
            return go.GetComponent<VehicleController>();
        }

        static void AddWheel(Transform root, VehicleController vc, Vector3 lp, Spec s, bool front, bool driven, Material tyre)
        {
            var g = new GameObject("WheelCollider");
            g.transform.SetParent(root, false);
            g.transform.localPosition = lp;
            var wc = g.AddComponent<WheelCollider>();
            wc.radius = s.wheelR;
            wc.suspensionDistance = 0.28f;
            wc.mass = 20f;
            var sp = new JointSpring { spring = s.spring, damper = s.damper, targetPosition = 0.5f };
            wc.suspensionSpring = sp;
            var f = wc.forwardFriction; f.stiffness = 1.6f; wc.forwardFriction = f;
            var sw = wc.sidewaysFriction; sw.stiffness = 1.9f; wc.sidewaysFriction = sw;

            var vis = Prim.Make(PrimitiveType.Cylinder, "WheelVisual", root, lp,
                new Vector3(s.wheelR * 2f, s.wheelW * 0.5f, s.wheelR * 2f), tyre);
            vc.AddWheel(wc, vis.transform, front, driven);
        }

        /// <summary>Visual-only kinematic car for ambient traffic (no wheel physics).</summary>
        public static GameObject CreateAmbient(Material tint)
        {
            Spec s = SpecFor(VehicleKind.Car);
            var go = new GameObject("TrafficCar");
            BuildBody(go.transform, VehicleKind.Car, s, tint);
            Material tyre = Mats.Get("Tyre");
            float y = s.wheelR;
            foreach (float z in new[] { s.wheelZF, s.wheelZR })
                foreach (float x in new[] { -s.wheelX, s.wheelX })
                    Prim.Make(PrimitiveType.Cylinder, "Wheel", go.transform, new Vector3(x, y, z),
                        new Vector3(s.wheelR * 2f, s.wheelW * 0.5f, s.wheelR * 2f), tyre, false, new Vector3(0, 0, 90));
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, s.colH, 0f);
            bc.size = new Vector3(s.colX, s.colY, s.colZ);
            return go;
        }
    }
}
