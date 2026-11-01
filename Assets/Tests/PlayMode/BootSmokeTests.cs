using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyUmrah.Tests
{
    /// <summary>End-to-end smoke test: load scene, start game, walk, switch camera, enter/drive/exit a vehicle.</summary>
    public class BootSmokeTests
    {
        [UnityTest]
        public IEnumerator SceneBootsAndCoreGameplayWorks()
        {
            SceneManager.LoadScene("MauritiusCastel");
            float t = 0f;
            GameUI ui = null;
            while (t < 120f)
            {
                yield return null;
                t += Time.unscaledDeltaTime;
                ui = Object.FindFirstObjectByType<GameUI>();
                if (GameObject.Find("Player") != null && ui != null && ui.TitleActive) break;
            }
            Assert.IsNotNull(GameObject.Find("Player"), "Player was not created within 120 s");

            ui.StartNewGameForTests();
            yield return null;
            var player = GameObject.Find("Player").GetComponent<PlayerController>();
            var rig = Object.FindFirstObjectByType<CameraRig>();
            Assert.IsNotNull(rig);

            // Walk forward for one second
            Vector3 start = player.transform.position;
            GameInput.TouchMove = new Vector2(0f, 1f);
            yield return new WaitForSeconds(1.5f);
            GameInput.TouchMove = Vector2.zero;
            Assert.Greater(Vector3.Distance(start, player.transform.position), 1f, "Player did not move");

            // Camera switch
            bool before = rig.FirstPerson;
            GameInput.PressCamera();
            yield return null; yield return null;
            Assert.AreNotEqual(before, rig.FirstPerson, "Camera did not switch");

            // Vehicle enter, drive, exit
            VehicleController car = null;
            foreach (var v in Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None))
                if (v.kind == VehicleKind.Car) car = v;
            Assert.IsNotNull(car);
            player.Teleport(car.transform.position + car.transform.right * -3f, 0f);
            yield return null;
            player.EnterVehicle(car);
            Assert.IsTrue(player.Driving);
            Vector3 carStart = car.transform.position;
            GameInput.TouchAccel = true;
            yield return new WaitForSeconds(3f);
            GameInput.TouchAccel = false;
            Assert.Greater(Vector3.Distance(carStart, car.transform.position), 3f, "Car did not drive");
            player.ExitVehicle();
            Assert.IsFalse(player.Driving);
        }
    }
}
