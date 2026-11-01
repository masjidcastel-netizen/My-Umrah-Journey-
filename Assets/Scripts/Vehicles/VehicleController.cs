using System.Collections.Generic;
using UnityEngine;

namespace MyUmrah
{
    public enum VehicleKind { Car, Taxi, Bus, Motorcycle }

    /// <summary>Arcade vehicle physics on WheelColliders: steering, throttle, reverse, braking, fuel, engine sound.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : Interactable
    {
        public VehicleKind kind;
        public string nameKey = "veh_car";
        public Transform seat, exitPoint, cameraPivot, fpPoint;
        public float chaseDistance = 7f;
        public bool hideLegs = true;
        public float motorTorque = 1400f, brakeTorque = 3500f, maxSteer = 32f, topSpeedKmh = 120f;
        public float fuelCapacity = 40f, fuel = 30f, burnPerSecond = 0.05f;

        class Wheel { public WheelCollider wc; public Transform vis; public bool front, driven; }

        readonly List<Wheel> wheels = new List<Wheel>();
        Rigidbody rb;
        float throttle, steer;
        bool brake, outOfFuelWarned;
        float flipTimer;
        AudioSource engine;
        static AudioClip engineClip;

        public PlayerController Driver { get; private set; }
        public float SpeedMs { get { return rb != null ? rb.linearVelocity.magnitude : 0f; } }
        public float Kmh { get { return SpeedMs * 3.6f; } }
        public float FuelNormalized { get { return fuelCapacity > 0f ? fuel / fuelCapacity : 0f; } }

        public Vector3 ExitPosition
        {
            get
            {
                Vector3 p = exitPoint != null ? exitPoint.position : transform.position + transform.right * -2.5f;
                return new Vector3(p.x, p.y + 0.15f, p.z);
            }
        }

        public override string PromptKey { get { return "enter_vehicle"; } }
        public override bool CanInteract(PlayerController p) { return Driver == null; }
        public override void OnInteract(PlayerController p) { p.EnterVehicle(this); }

        public void AddWheel(WheelCollider wc, Transform vis, bool front, bool driven)
        {
            wheels.Add(new Wheel { wc = wc, vis = vis, front = front, driven = driven });
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            engine = gameObject.AddComponent<AudioSource>();
            engine.clip = GetEngineClip();
            engine.loop = true;
            engine.spatialBlend = 1f;
            engine.minDistance = 4f;
            engine.maxDistance = 45f;
            engine.volume = 0f;
            engine.playOnAwake = false;
        }

        static AudioClip GetEngineClip()
        {
            if (engineClip != null) return engineClip;
            // Procedural engine loop (placeholder audio): 80 Hz fundamental + harmonics, 0.5 s = whole cycles, so it loops seamlessly.
            const int rate = 22050;
            int n = rate / 2;
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                data[i] = 0.35f * Mathf.Sin(2f * Mathf.PI * 80f * t) + 0.2f * Mathf.Sin(2f * Mathf.PI * 160f * t) + 0.1f * Mathf.Sin(2f * Mathf.PI * 240f * t);
            }
            engineClip = AudioClip.Create("engine_placeholder", n, 1, rate, false);
            engineClip.SetData(data, 0);
            return engineClip;
        }

        public void SetDriver(PlayerController p)
        {
            Driver = p;
            if (engine != null && !engine.isPlaying) engine.Play();
        }

        public void ClearDriver()
        {
            Driver = null; throttle = 0f; steer = 0f; brake = false;
            if (engine != null) engine.volume = 0f;
        }

        public void SetInput(float t, float s, bool b) { throttle = t; steer = s; brake = b; }

        public void Refuel() { fuel = fuelCapacity; outOfFuelWarned = false; }

        void FixedUpdate()
        {
            Vector3 vel = rb.linearVelocity;
            float kmh = vel.magnitude * 3.6f;
            float fwdSpeed = Vector3.Dot(vel, transform.forward);
            float motor = 0f, brk;

            if (Driver != null)
            {
                brk = brakeTorque * 0.04f; // light engine braking
                if (fuel > 0f)
                {
                    if (throttle > 0.01f) { if (kmh < topSpeedKmh) motor = throttle * motorTorque; brk = 0f; }
                    else if (throttle < -0.01f)
                    {
                        if (fwdSpeed > 1.5f) brk = -throttle * brakeTorque;
                        else if (kmh < 25f) { motor = throttle * motorTorque * 0.6f; brk = 0f; }
                    }
                    fuel = Mathf.Max(0f, fuel - burnPerSecond * (0.25f + Mathf.Abs(throttle)) * Time.fixedDeltaTime);
                }
                if (brake) brk = brakeTorque;
            }
            else
            {
                brk = brakeTorque; // parking brake
            }

            float steerAngle = steer * maxSteer * Mathf.Lerp(1f, 0.3f, Mathf.Clamp01(kmh / 80f));
            for (int i = 0; i < wheels.Count; i++)
            {
                var w = wheels[i];
                if (w.front) w.wc.steerAngle = steerAngle;
                w.wc.motorTorque = w.driven ? motor : 0f;
                w.wc.brakeTorque = brk;
            }
        }

        void Update()
        {
            for (int i = 0; i < wheels.Count; i++)
            {
                Vector3 p; Quaternion q;
                wheels[i].wc.GetWorldPose(out p, out q);
                wheels[i].vis.SetPositionAndRotation(p, q * Quaternion.Euler(0f, 0f, 90f));
            }

            if (Driver != null)
            {
                engine.volume = 0.3f;
                engine.pitch = Mathf.Lerp(0.7f, 2f, Mathf.Clamp01(Kmh / topSpeedKmh)) + Mathf.Abs(throttle) * 0.15f;

                if (fuel <= 0f && !outOfFuelWarned) { outOfFuelWarned = true; GameUI.ToastKey("out_of_fuel"); }

                // Auto-right if flipped
                if (Vector3.Dot(transform.up, Vector3.up) < 0.25f && Kmh < 5f)
                {
                    flipTimer += Time.deltaTime;
                    if (flipTimer > 2f)
                    {
                        transform.position += Vector3.up * 1.2f;
                        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                        rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
                        flipTimer = 0f;
                    }
                }
                else flipTimer = 0f;
            }
        }
    }
}
