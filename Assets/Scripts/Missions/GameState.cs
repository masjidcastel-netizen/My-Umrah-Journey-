using UnityEngine;

namespace MyUmrah
{
    /// <summary>Single source of truth for persistent player state (backed by SaveData).</summary>
    public static class GameState
    {
        public static SaveData Data = new SaveData();
        public static bool Paused;

        public static void Load()
        {
            Data = SaveSystem.Load() ?? new SaveData();
        }

        public static void Save() { SaveSystem.Save(Data); }

        public static MissionState Mission(string id)
        {
            foreach (var m in Data.missions) if (m.id == id) return m;
            var n = new MissionState { id = id };
            Data.missions.Add(n);
            return n;
        }
    }

    public static class Inventory
    {
        public static event System.Action Changed;
        public static void ResetEvents() { Changed = null; }

        public static int Money { get { return GameState.Data.money; } }

        public static void AddMoney(int amount)
        {
            GameState.Data.money += amount;
            if (Changed != null) Changed();
        }

        public static bool TrySpend(int amount)
        {
            if (GameState.Data.money < amount) return false;
            GameState.Data.money -= amount;
            if (Changed != null) Changed();
            return true;
        }

        public static void AddItem(string key)
        {
            if (!GameState.Data.items.Contains(key)) GameState.Data.items.Add(key);
            if (Changed != null) Changed();
        }

        public static bool Has(string key) { return GameState.Data.items.Contains(key); }
    }

    public class MissionDef
    {
        public string id, titleKey, descKey, eventId;
        public float target;
        public int reward;
        public MissionDef(string id, string titleKey, string descKey, string eventId, float target, int reward)
        {
            this.id = id; this.titleKey = titleKey; this.descKey = descKey;
            this.eventId = eventId; this.target = target; this.reward = reward;
        }
    }

    public static class Missions
    {
        public static readonly MissionDef[] All =
        {
            new MissionDef("walk", "m_walk_t", "m_walk_d", "walk", 50f, 50),
            new MissionDef("camera", "m_cam_t", "m_cam_d", "camera", 2f, 25),
            new MissionDef("masjid", "m_mosque_t", "m_mosque_d", "masjid", 1f, 100),
            new MissionDef("drive", "m_drive_t", "m_drive_d", "drive", 150f, 75),
            new MissionDef("greet", "m_greet_t", "m_greet_d", "greet", 1f, 50),
            new MissionDef("swim", "m_swim_t", "m_swim_d", "swim", 5f, 50),
        };

        public static event System.Action Changed;
        public static event System.Action<MissionDef> Completed;
        public static void ResetEvents() { Changed = null; Completed = null; }

        public static void Report(string eventId, float amount = 1f)
        {
            bool changed = false;
            foreach (var d in All)
            {
                if (d.eventId != eventId) continue;
                var st = GameState.Mission(d.id);
                if (st.done) continue;
                int before = (int)st.value;
                st.value += amount;
                if (st.value >= d.target)
                {
                    st.value = d.target;
                    st.done = true;
                    Inventory.AddMoney(d.reward);
                    changed = true;
                    if (Completed != null) Completed(d);
                }
                else if ((int)st.value != before) changed = true;
            }
            if (changed && Changed != null) Changed();
        }

        public static MissionDef FirstActive()
        {
            foreach (var d in All) if (!GameState.Mission(d.id).done) return d;
            return null;
        }

        public static float Progress(MissionDef d) { return GameState.Mission(d.id).value; }
        public static bool IsDone(MissionDef d) { return GameState.Mission(d.id).done; }
    }
}
