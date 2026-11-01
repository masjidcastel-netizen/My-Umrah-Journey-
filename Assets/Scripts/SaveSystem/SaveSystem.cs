using System.Collections.Generic;
using UnityEngine;

namespace MyUmrah
{
    [System.Serializable]
    public class MissionState
    {
        public string id;
        public float value;
        public bool done;
    }

    [System.Serializable]
    public class SaveData
    {
        public int version = 1;
        public bool hasPosition;
        public float px, py, pz, yaw;
        public int language;
        public int quality = 1;
        public int cameraMode;      // 0 = third person, 1 = first person
        public int money = 500;
        public int gender;          // 0 male, 1 female
        public float playSeconds;
        public List<string> items = new List<string>();
        public List<MissionState> missions = new List<MissionState>();
    }

    /// <summary>Local JSON save file in Application.persistentDataPath. No network.</summary>
    public static class SaveSystem
    {
        public static string PathOverride; // used by tests

        static string FilePath
        {
            get { return PathOverride ?? System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json"); }
        }

        public static bool Exists { get { return System.IO.File.Exists(FilePath); } }

        public static bool Save(SaveData d)
        {
            try
            {
                string json = JsonUtility.ToJson(d, true);
                string tmp = FilePath + ".tmp";
                System.IO.File.WriteAllText(tmp, json);
                if (System.IO.File.Exists(FilePath)) System.IO.File.Delete(FilePath);
                System.IO.File.Move(tmp, FilePath);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Save failed: " + e.Message);
                return false;
            }
        }

        public static SaveData Load()
        {
            try
            {
                if (!Exists) return null;
                return JsonUtility.FromJson<SaveData>(System.IO.File.ReadAllText(FilePath));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Load failed: " + e.Message);
                return null;
            }
        }

        public static void Delete()
        {
            try { if (Exists) System.IO.File.Delete(FilePath); } catch (System.Exception) { }
        }
    }
}
