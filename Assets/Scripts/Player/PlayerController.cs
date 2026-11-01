using UnityEngine;

namespace MyUmrah
{
    /// <summary>Walk / run / jump / swim / interact / drive. Uses Unity's CharacterController.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 2.4f, runSpeed = 5.2f, swimSpeed = 2.2f, jumpHeight = 1.1f, gravity = -20f;

        public CameraRig Rig;
        public HumanoidAnimator Anim;
        public Vector3 SpawnPoint = WorldLayout.PlayerSpawn;

        public bool Driving { get; private set; }
        public VehicleController Vehicle { get; private set; }
        public bool Swimming { get; private set; }
        public Interactable Nearest { get; private set; }
        public event System.Action<bool> DrivingChanged;

        CharacterController cc;
        Vector3 horiz;
        float vy;
        Renderer[] renderers;
        float swimTimer;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.9f, 0f);
            cc.skinWidth = 0.03f; cc.stepOffset = 0.35f; cc.slopeLimit = 50f;
            gameObject.layer = 2; // Ignore Raycast: camera/vehicle rays skip the player
        }

        public void CacheRenderers() { renderers = GetComponentsInChildren<Renderer>(true); }

        public void SetModelVisible(bool v)
        {
            if (renderers == null) CacheRenderers();
            foreach (var r in renderers) if (r != null) r.enabled = v;
        }

        public void Teleport(Vector3 pos, float yawDeg)
        {
            if (Driving) ExitVehicle();
            cc.enabled = false;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yawDeg, 0f));
            cc.enabled = true;
            horiz = Vector3.zero; vy = 0f;
            if (Rig != null) Rig.SetYaw(yawDeg);
        }

        void Update()
        {
            if (GameState.Paused) return;
            bool interact = GameInput.ConsumeInteract();
            if (Driving) { DriveUpdate(interact); return; }
            GroundUpdate(interact);
        }

        void GroundUpdate(bool interact)
        {
            if (!cc.enabled) return;
            float dt = Time.deltaTime;
            Vector3 feet = transform.position;

            float surf;
            bool inWater = WaterVolume.Query(feet, out surf);
            Swimming = inWater && (surf - feet.y) > 1.0f;

            Vector2 mv = GameInput.Move;
            float camYaw = Rig != null ? Rig.Yaw : transform.eulerAngles.y;
            Quaternion yawRot = Quaternion.Euler(0f, camYaw, 0f);
            Vector3 wish = yawRot * new Vector3(mv.x, 0f, mv.y);

            float speed = Swimming ? swimSpeed : (GameInput.Sprint ? runSpeed : walkSpeed);
            if (inWater && !Swimming) speed *= 0.7f;
            horiz = Vector3.MoveTowards(horiz, wish * speed, 18f * dt);

            bool grounded = cc.isGrounded;
            bool jumpPressed = GameInput.ConsumeJump();
            if (Swimming)
            {
                float targetY = surf - 1.25f;
                vy = Mathf.Clamp((targetY - feet.y) * 3f, -3f, 3f);
            }
            else
            {
                if (grounded && vy < 0f) vy = -2f;
                if (grounded && jumpPressed)
                {
                    vy = Mathf.Sqrt(2f * -gravity * jumpHeight);
                    if (Anim != null) Anim.Jump();
                }
                vy += gravity * dt;
            }

            cc.Move((horiz + Vector3.up * vy) * dt);

            // Facing
            if (Rig != null && Rig.FirstPerson)
                transform.rotation = Quaternion.Euler(0f, camYaw, 0f);
            else if (wish.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(wish.normalized), 720f * dt);

            // Animation
            float hs = new Vector2(horiz.x, horiz.z).magnitude;
            float param = hs <= walkSpeed ? hs / walkSpeed : 1f + (hs - walkSpeed) / (runSpeed - walkSpeed);
            if (Anim != null) Anim.Set(Mathf.Clamp(param, 0f, 2f), Swimming);

            // Missions
            if (grounded && !Swimming) Missions.Report("walk", hs * dt);
            if (Swimming) Missions.Report("swim", dt);

            // Interaction
            Nearest = FindNearest();
            if (interact && Nearest != null) Nearest.OnInteract(this);

            // Safety nets
            Vector3 p = transform.position;
            if (p.y < -20f || new Vector2(p.x, p.z).magnitude > 440f) Teleport(SpawnPoint, 0f);
        }

        Interactable FindNearest()
        {
            Interactable best = null;
            float bestD = float.MaxValue;
            Vector3 pos = transform.position;
            var all = Interactable.All;
            for (int i = 0; i < all.Count; i++)
            {
                var it = all[i];
                if (it == null || !it.CanInteract(this)) continue;
                float d = Vector3.Distance(pos, it.InteractPoint);
                if (d <= it.range && d < bestD) { bestD = d; best = it; }
            }
            return best;
        }

        void DriveUpdate(bool interact)
        {
            var v = Vehicle;
            if (v == null) { Driving = false; return; }
            Vector2 m = GameInput.Move;
            float throttle = GameInput.Accel ? 1f : m.y;
            v.SetInput(throttle, m.x, GameInput.Brake);
            Missions.Report("drive", v.SpeedMs * Time.deltaTime);
            Nearest = null;
            if (interact) ExitVehicle();
        }

        public void EnterVehicle(VehicleController v)
        {
            if (Driving || v == null) return;
            Vehicle = v; Driving = true;
            v.SetDriver(this);
            cc.enabled = false;
            transform.SetParent(v.seat, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            if (Anim != null) Anim.SetSitting(true, v.hideLegs);
            if (Rig != null) Rig.SetTarget(v.cameraPivot, true, v.fpPoint, v.chaseDistance);
            GameInput.ClearOneShots();
            if (DrivingChanged != null) DrivingChanged(true);
        }

        public void ExitVehicle()
        {
            if (!Driving || Vehicle == null) { Driving = false; return; }
            var v = Vehicle;
            Vector3 p = v.ExitPosition;
            float yaw = v.transform.eulerAngles.y;
            v.ClearDriver();
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f));
            cc.enabled = true;
            horiz = Vector3.zero; vy = 0f;
            Driving = false; Vehicle = null;
            if (Anim != null) Anim.SetSitting(false, false);
            if (Rig != null) Rig.SetTarget(transform, false, null, 4.2f);
            GameInput.ClearOneShots();
            if (DrivingChanged != null) DrivingChanged(false);
        }
    }
}
