using UnityEngine;

namespace MyUmrah
{
    /// <summary>First-person / third-person camera with touch look, collision-safe boom and vehicle chase mode.</summary>
    public class CameraRig : MonoBehaviour
    {
        public Camera Cam;
        public bool FirstPerson;
        public float Sensitivity = 0.12f;
        public float Yaw { get { return yaw; } }
        public event System.Action ModeChanged;

        Transform target, fpPoint;
        Vector3 offset = new Vector3(0f, 1.55f, 0f);
        float yaw, pitch = 12f, dist = 4.2f;
        bool vehicleMode;
        Vector3 smoothedPos;
        bool hasSmoothed;

        public void Init(Camera cam)
        {
            Cam = cam;
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.15f;
            cam.allowHDR = false;
            cam.clearFlags = CameraClearFlags.Skybox;
            QualityManager.ApplyCulling(cam);
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            transform.SetParent(null);
        }

        public void SetYaw(float y) { yaw = y; }

        public void SetTarget(Transform t, bool vehicle, Transform firstPersonPoint, float chaseDistance)
        {
            target = t; vehicleMode = vehicle; fpPoint = firstPersonPoint;
            dist = vehicle ? chaseDistance : 4.2f;
            offset = vehicle ? Vector3.zero : new Vector3(0f, 1.55f, 0f);
            hasSmoothed = false;
        }

        public void ToggleMode()
        {
            SetFirstPerson(!FirstPerson);
            Missions.Report("camera", 1f);
        }

        public void SetFirstPerson(bool fp)
        {
            FirstPerson = fp;
            hasSmoothed = false;
            if (ModeChanged != null) ModeChanged();
        }

        void LateUpdate()
        {
            if (target == null || Cam == null) return;
            float dt = Time.unscaledDeltaTime;

            if (!GameState.Paused)
            {
                if (GameInput.ConsumeCamera()) ToggleMode();
                Vector2 look = GameInput.ConsumeLook();
                yaw += look.x * Sensitivity;
                pitch = Mathf.Clamp(pitch - look.y * Sensitivity, FirstPerson ? -80f : -25f, FirstPerson ? 80f : 70f);

                if (vehicleMode && look.sqrMagnitude < 0.01f)
                {
                    var rb = target.GetComponentInParent<Rigidbody>();
                    if (rb != null && rb.linearVelocity.magnitude > 2f)
                    {
                        float vyaw = target.eulerAngles.y;
                        yaw = Mathf.LerpAngle(yaw, vyaw, 1f - Mathf.Exp(-2f * dt));
                    }
                }
            }

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + offset;

            if (FirstPerson)
            {
                Vector3 eye = fpPoint != null ? fpPoint.position : pivot + new Vector3(0f, 0.07f, 0f);
                Cam.fieldOfView = 75f;
                transform.SetPositionAndRotation(eye, rot);
            }
            else
            {
                Cam.fieldOfView = 65f;
                Vector3 dir = rot * Vector3.back;
                float d = dist;
                RaycastHit hit;
                int mask = ~(1 << 2); // ignore the player layer (Ignore Raycast)
                if (Physics.SphereCast(pivot, 0.25f, dir, out hit, d, mask, QueryTriggerInteraction.Ignore))
                    d = Mathf.Max(0.6f, hit.distance);
                Vector3 desired = pivot + dir * d;
                if (!hasSmoothed) { smoothedPos = desired; hasSmoothed = true; }
                smoothedPos = Vector3.Lerp(smoothedPos, desired, 1f - Mathf.Exp(-18f * dt));
                transform.SetPositionAndRotation(smoothedPos, rot);
            }
        }
    }
}
