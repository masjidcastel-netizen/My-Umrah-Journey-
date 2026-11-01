using System.Collections.Generic;
using UnityEngine;

namespace MyUmrah
{
    /// <summary>Villager NPC: wanders around a home point, stops and greets the player when interacted with.</summary>
    public class WanderNPC : Interactable
    {
        public Vector3 home;
        public float wanderRadius = 12f;
        public int lineIndex = 1;
        public HumanoidAnimator Anim;

        const float Speed = 1.3f;
        Vector3 target;
        float wait, talk;

        void Awake() { promptKey = "talk"; range = 3.2f; }

        void Start() { PickTarget(); }

        void PickTarget()
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 r = Random.insideUnitCircle * wanderRadius;
                Vector3 p = new Vector3(home.x + r.x, 0f, home.z + r.y);
                if (WorldLayout.Height(p.x, p.z) > 7.5f && Vector2.Distance(new Vector2(p.x, p.z), WorldLayout.LakeCenter) > WorldLayout.LakeVolumeRadius)
                {
                    target = p; return;
                }
            }
            target = home;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (talk > 0f) { talk -= dt; if (Anim != null) Anim.Set(0f, false); return; }
            if (wait > 0f) { wait -= dt; if (Anim != null) Anim.Set(0f, false); return; }

            Vector3 to = target - transform.position; to.y = 0f;
            if (to.magnitude < 0.5f) { wait = Random.Range(2f, 6f); PickTarget(); return; }
            Vector3 dir = to.normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * dt);
            Vector3 p = transform.position + dir * Speed * dt;
            p.y = WorldLayout.Height(p.x, p.z);
            transform.position = p;
            if (Anim != null) Anim.Set(1f, false);
        }

        public override void OnInteract(PlayerController pc)
        {
            Vector3 d = pc.transform.position - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
            talk = 4f;
            GameUI.ToastKey("npc_" + (1 + (lineIndex % 3)));
            Missions.Report("greet", 1f);
        }
    }

    /// <summary>Ambient traffic car following the loop road on the LEFT (Mauritius drives on the left). Pooled.</summary>
    public class AmbientCar : MonoBehaviour
    {
        public float s;
        float cur;
        const float Cruise = 9f;

        public void Place(float arc)
        {
            s = arc; cur = 0f;
            Apply();
        }

        void Apply()
        {
            Vector2 p, d;
            WorldLayout.SampleLoop(s, out p, out d);
            Vector2 left = new Vector2(-d.y, d.x);
            Vector2 q = p + left * 1.9f;
            transform.SetPositionAndRotation(new Vector3(q.x, WorldLayout.PlateauHeight, q.y),
                Quaternion.LookRotation(new Vector3(d.x, 0f, d.y)));
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 origin = transform.position + Vector3.up * 0.7f + transform.forward * 2.6f;
            bool blocked = Physics.Raycast(origin, transform.forward, 8f, ~0, QueryTriggerInteraction.Ignore);
            cur = Mathf.MoveTowards(cur, blocked ? 0f : Cruise, (blocked ? 18f : 4f) * dt);
            s += cur * dt;
            Apply();
        }
    }

    /// <summary>Spawns, pools and recycles ambient traffic around the player.</summary>
    public class TrafficManager : MonoBehaviour
    {
        public Transform player;
        public int count = 4;
        SimplePool<AmbientCar> pool;
        readonly List<AmbientCar> active = new List<AmbientCar>();
        float timer;

        static readonly string[] Palette = { "CarWhite", "CarBlue", "CarSilver", "CarRed", "TaxiYellow" };

        void Awake()
        {
            pool = new SimplePool<AmbientCar>(() =>
            {
                var g = VehicleFactory.CreateAmbient(Mats.Get(Palette[Random.Range(0, Palette.Length)]));
                g.transform.SetParent(transform, false);
                return g.AddComponent<AmbientCar>();
            });
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f || player == null) return;
            timer = 1f;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (Vector3.Distance(active[i].transform.position, player.position) > 280f)
                {
                    pool.Release(active[i]);
                    active.RemoveAt(i);
                }
            }

            int guard = 0;
            while (active.Count < count && guard++ < 10)
            {
                float arc = Random.Range(0f, WorldLayout.LoopLength);
                Vector2 p, d;
                WorldLayout.SampleLoop(arc, out p, out d);
                float dist = Vector2.Distance(p, new Vector2(player.position.x, player.position.z));
                if (dist < 50f || dist > 250f) continue;
                var car = pool.Get();
                car.Place(arc);
                active.Add(car);
            }
        }
    }
}
