using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace MyUmrah.Tests
{
    public class CoreTests
    {
        [Test]
        public void EveryLocalisationKeyHasAllFiveLanguages()
        {
            foreach (var key in Loc.Keys)
            {
                var a = Loc.Raw(key);
                Assert.AreEqual(5, a.Length, key);
                for (int i = 0; i < 5; i++) Assert.IsFalse(string.IsNullOrWhiteSpace(a[i]), key + " / language " + i);
            }
        }

        [Test]
        public void PlateauAroundCastelIsFlat()
        {
            for (float x = -150f; x <= 150f; x += 30f)
                for (float z = -150f; z <= 150f; z += 30f)
                {
                    if (Vector2.Distance(new Vector2(x, z), WorldLayout.LakeCenter) < WorldLayout.LakeVolumeRadius + 10f) continue;
                    Assert.AreEqual(WorldLayout.PlateauHeight, WorldLayout.Height(x, z), 0.001f, x + "," + z);
                }
        }

        [Test]
        public void LakeIsDeepEnoughToSwim()
        {
            float bottom = WorldLayout.Height(WorldLayout.LakeCenter.x, WorldLayout.LakeCenter.y);
            Assert.Less(bottom, WorldLayout.LakeLevel - 2f);
        }

        [Test]
        public void SeaIsBelowSeaLevelFarFromIsland()
        {
            Assert.Less(WorldLayout.Height(390f, 0f), WorldLayout.SeaLevel);
        }

        [Test]
        public void WaterQueryDetectsLakeButNotLand()
        {
            var go = new GameObject("lake");
            var v = go.AddComponent<WaterVolume>();
            v.surfaceY = WorldLayout.LakeLevel; v.center = WorldLayout.LakeCenter; v.radius = WorldLayout.LakeVolumeRadius;
            float s;
            Assert.IsTrue(WaterVolume.Query(new Vector3(WorldLayout.LakeCenter.x, 4f, WorldLayout.LakeCenter.y), out s));
            Assert.IsFalse(WaterVolume.Query(new Vector3(0f, 8f, 0f), out s));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void LoopSamplingWrapsAndStaysOnRoad()
        {
            Vector2 p, d;
            WorldLayout.SampleLoop(0f, out p, out d);
            Vector2 p2, d2;
            WorldLayout.SampleLoop(WorldLayout.LoopLength, out p2, out d2);
            Assert.AreEqual(0f, Vector2.Distance(p, p2), 0.01f);
            Assert.Less(WorldLayout.DistanceToRoads(p), 0.01f);
        }

        [Test]
        public void SaveRoundTrip()
        {
            SaveSystem.PathOverride = Path.Combine(Application.temporaryCachePath, "umrah_test_save.json");
            SaveSystem.Delete();
            var d = new SaveData { px = 1f, py = 2f, pz = 3f, language = 3, money = 777, hasPosition = true };
            d.items.Add("item_prayer_mat");
            Assert.IsTrue(SaveSystem.Save(d));
            var l = SaveSystem.Load();
            Assert.IsNotNull(l);
            Assert.AreEqual(3, l.language);
            Assert.AreEqual(777, l.money);
            Assert.AreEqual("item_prayer_mat", l.items[0]);
            SaveSystem.Delete();
            SaveSystem.PathOverride = null;
        }

        [Test]
        public void MissionCompletesAndPaysReward()
        {
            GameState.Data = new SaveData();
            Missions.ResetEvents();
            int before = Inventory.Money;
            Missions.Report("masjid", 1f);
            Assert.IsTrue(Missions.IsDone(System.Array.Find(Missions.All, m => m.id == "masjid")));
            Assert.AreEqual(before + 100, Inventory.Money);
        }

        [Test]
        public void SpendingFailsWithoutMoney()
        {
            GameState.Data = new SaveData { money = 10 };
            Assert.IsFalse(Inventory.TrySpend(50));
            Assert.AreEqual(10, Inventory.Money);
        }

        [Test]
        public void RtlFlagsOnlyForArabicAndUrdu()
        {
            Loc.Set(Language.Arabic); Assert.IsTrue(Loc.IsRtl);
            Loc.Set(Language.Urdu); Assert.IsTrue(Loc.IsRtl);
            Loc.Set(Language.French); Assert.IsFalse(Loc.IsRtl);
            Loc.Set(Language.English);
        }
    }
}
