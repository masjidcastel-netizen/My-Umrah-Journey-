using System.Collections.Generic;
using UnityEngine;

namespace MyUmrah
{
    /// <summary>Water body (sea or lake). Queried by the player for swimming; pure maths, no physics needed.</summary>
    public class WaterVolume : MonoBehaviour
    {
        public static readonly List<WaterVolume> All = new List<WaterVolume>();
        public float surfaceY;
        public Vector2 center;
        public float radius; // 0 = unlimited (sea)

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static bool Query(Vector3 p, out float surface)
        {
            for (int i = 0; i < All.Count; i++)
            {
                var v = All[i];
                if (v.radius > 0f)
                {
                    float dx = p.x - v.center.x, dz = p.z - v.center.y;
                    if (dx * dx + dz * dz > v.radius * v.radius) continue;
                }
                if (p.y < v.surfaceY && WorldLayout.Height(p.x, p.z) < v.surfaceY)
                {
                    surface = v.surfaceY;
                    return true;
                }
            }
            surface = 0f;
            return false;
        }
    }

    /// <summary>The Masjid Castel landmark: shows information and completes the visit mission.</summary>
    public class LandmarkInteractable : Interactable
    {
        void Awake() { promptKey = "learn"; range = 12f; }

        public override void OnInteract(PlayerController p)
        {
            if (GameUI.I != null) GameUI.I.ShowInfo(Loc.T("masjid_name"), Loc.T("masjid_info"));
            Missions.Report("masjid", 1f);
        }
    }

    /// <summary>Petrol pump: refuels the nearest vehicle for a small fee.</summary>
    public class FuelPump : Interactable
    {
        void Awake() { promptKey = "refuel"; range = 6f; }

        public override void OnInteract(PlayerController p)
        {
            VehicleController best = null;
            float bd = 14f;
            foreach (var it in Interactable.All)
            {
                var v = it as VehicleController;
                if (v == null) continue;
                float d = Vector3.Distance(v.transform.position, transform.position);
                if (d < bd) { bd = d; best = v; }
            }
            if (best == null) { GameUI.ToastKey("no_vehicle_near"); return; }
            int cost = Mathf.CeilToInt((best.fuelCapacity - best.fuel) * 1.5f);
            if (cost <= 0) { GameUI.ToastKey("refueled"); return; }
            if (!Inventory.TrySpend(cost)) { GameUI.ToastKey("no_money"); return; }
            best.Refuel();
            GameUI.ToastKey("refueled");
        }
    }

    /// <summary>A collectible item (e.g. prayer mat).</summary>
    public class PickupItem : Interactable
    {
        public string itemKey = "item_prayer_mat";
        void Awake() { promptKey = "pickup"; range = 2.5f; }

        void Update() { transform.Rotate(0f, 45f * Time.deltaTime, 0f); }

        public override void OnInteract(PlayerController p)
        {
            Inventory.AddItem(itemKey);
            GameUI.ToastKey("picked", Loc.T(itemKey));
            Destroy(gameObject);
        }
    }
}
