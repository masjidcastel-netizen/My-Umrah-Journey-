# My Umrah Journey - Native Unity 6 Android game (Milestone A)

Native Android 3D game: Unity 6 + C# + URP + Input System + Terrain + CharacterController + WheelColliders.
No HTML/CSS/JavaScript/WebView/WebGL anywhere. Package id: `com.masjidcastel.myumrahjourney`. Landscape. ARM64. IL2CPP.

**Status: source complete for Milestone A, but NOT yet opened, compiled, tested or built in Unity.** Read `docs/STATUS.md`.
**To build the APK/AAB you need a computer with Unity Editor 6000.0.x** - read `docs/BUILD_GUIDE.md`.

## What Milestone A contains
- Castel test area generated at load: terrain with beach/sea, lake (swimmable), loop road with rounded corners, roundabout, houses, shops, petrol station, palms/trees with LOD, fog, sun with soft shadows.
- Placeholder 3D Masjid Castel (domes, 2 minarets, colonnade, gate, wudhu area, name board) with an interactive info panel.
- Humanoid male/female placeholder characters with generated Animator clips (idle, walk, run, jump, swim, sit) + procedural fallback.
- First/third person camera, touch look, collision-safe boom, vehicle chase camera.
- Touch controls: floating joystick, look zone, jump, run toggle, context button (interact/enter/exit/talk/learn/refuel/pickup), view, menu, missions, inventory, accelerator, brake. Gamepad + keyboard/mouse for editor testing.
- Drivable car, taxi, bus, motorcycle (WheelColliders, fuel, engine sound, auto-right), enter/exit, petrol pump.
- Wandering villager NPCs (greetings), pooled ambient traffic driving on the left.
- 6 missions, money, inventory, JSON save/load (autosave, on pause, on quit).
- Localisation foundation: English, Mauritian Creole, Arabic, Urdu, French (62 keys; drafts need native review; Arabic/Urdu shaping not done).
- Adjustable graphics (3 levels), distance culling, LODs, static batching, object pool.
- Editor automation: `Umrah Journey` menu creates Player Settings, materials, animator, URP asset, scene, and builds APK/AAB.
- EditMode tests, a PlayMode smoke test, an in-Unity validator and offline validators in `tools/`.

## Quick start (on a computer)
1. Install Unity Hub + Unity `6000.0.x` with *Android Build Support* (OpenJDK, SDK & NDK).
2. Add project folder in Unity Hub and open it. Wait for import; the setup runs automatically (menu: Umrah Journey > Setup).
3. Open `Assets/Scenes/MauritiusCastel.unity`, press Play.
4. Menu: Umrah Journey > Build > Android Debug APK.
