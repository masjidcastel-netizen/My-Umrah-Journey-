# Build guide (Android APK / AAB)

Unity cannot compile or build on a phone. You need ONE computer with Unity Editor. Options without owning a PC:
a rented cloud Windows/Linux/Mac desktop (any provider), a friend's/shop computer, or a self-hosted/hosted CI runner with a Unity licence.
Your phone only installs and plays the resulting APK.

## 1. Install
- Unity Hub, then Unity **6000.0.x LTS** (pinned in `ProjectSettings/ProjectVersion.txt`: 6000.0.23f1, the Unity 6 launch build).
  A newer 6000.0.x patch is fine - let Unity upgrade the project. Unity 6.3 (6000.3) may work but is untested here.
- Modules: **Android Build Support** including **OpenJDK** and **Android SDK & NDK Tools**.
- A free Unity Personal licence is enough for testing.

## 2. Open and set up
1. Unity Hub > Add > select this folder > open.
2. First import takes several minutes (packages: URP 17.0.3, Input System 1.11.2, uGUI 2.0.0).
   If Unity asks to enable the new Input System backend / restart, accept.
3. Setup runs automatically; if not: menu **Umrah Journey > Setup > Build Everything**.
4. Fix any red console errors (see "If something fails").
5. Menu **Umrah Journey > Validate Project** (writes validation-report.txt) and **Window > General > Test Runner** > run EditMode and PlayMode.
6. Open `Assets/Scenes/MauritiusCastel.unity` and press Play. Editor controls: WASD move, mouse-right-drag look, Shift run, Space jump/brake, E interact, C view, Esc menu.

## 3. Debug APK (installable test build)
Menu **Umrah Journey > Build > Android Debug APK** -> `Builds/Android/MyUmrahJourney-debug.apk`.
Command line (any OS):
```
Unity -batchmode -nographics -quit -projectPath <folder> -buildTarget Android -executeMethod MyUmrah.EditorTools.BuildScript.BuildDebugApk -logFile build.log
```
Install: copy the APK to the phone and open it (allow "install unknown apps"), or `adb install -r MyUmrahJourney-debug.apk`.
Check logs: `adb logcat -s Unity`.

## 4. Release APK / AAB for Google Play
Create a keystore ONCE and back it up (losing it means you cannot update the app):
```
keytool -genkeypair -v -keystore umrah-release.keystore -alias umrah -keyalg RSA -keysize 2048 -validity 10000
```
Provide secrets as environment variables (never commit them):
`UMRAH_KEYSTORE_PATH`, `UMRAH_KEYSTORE_PASS`, `UMRAH_KEY_ALIAS`, `UMRAH_KEY_PASS`, then menu or:
`-executeMethod MyUmrah.EditorTools.BuildScript.BuildReleaseAab` -> `MyUmrahJourney-release.aab`
(`BuildReleaseApk` for a signed APK). If a variable is missing the build stops with an error and builds nothing.
Google Play requires a recent target API level: Unity uses the highest SDK installed (Unity Hub's default) - check Play Console requirements before upload, and consider Play App Signing.

## If something fails
- **Pink materials / no render**: URP asset not assigned -> run Setup again; Project Settings > Graphics should show UmrahMobile_URP.
- **Compile errors**: this source was never compiled. Send me the exact first error lines and I will fix them.
- **Terrain pink**: Assets/Materials/TerrainLit.mat must use "Universal Render Pipeline/Terrain/Lit".
- **UI not clickable**: an EventSystem with Input System UI Input Module must exist (created automatically); Active Input Handler must be "Input System Package".
- **Vehicle wobbles**: tune spring/damper in `VehicleFactory.cs` and `rb.centerOfMass`.

## 5. No PC? Cloud build with GitHub Actions (UNVERIFIED path)
`.github/workflows/build-unity-android.yml` builds the debug APK in GitHub's cloud with GameCI. You need a (free) GitHub account,
this project in a repository, and a Unity licence secret `UNITY_LICENSE`. Unity Personal activation for Unity 6 in CI has changed
over time - check https://game.ci/docs/github/activation before relying on it. Run it from the repo's Actions tab ("Run workflow");
the APK appears as a downloadable artifact you can open on your phone. Release/AAB signing in CI is not wired yet.
