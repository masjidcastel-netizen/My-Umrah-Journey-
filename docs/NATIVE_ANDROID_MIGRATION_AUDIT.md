# Native Android Migration Audit and Plan

**Date:** 2026-11-02  
**Repository:** `masjidcastel-netizen/My-Umrah-Journey-`  
**Branch:** `native-android-development`  
**Backup:** `main` (left unchanged)

## Why this audit exists

The project specification requires a native Android application built with Kotlin, Jetpack Compose, and libGDX 1.12.1. The repository selected for this development branch is a Unity 6 / C# project. The requested Stage 2 archive is in the separate repository `masjidcastel-netizen/My-Umrah-Journey` (without a trailing hyphen). This document records the repository state and sets a safe migration gate; it does not claim that the Unity project is the requested Kotlin/libGDX implementation.

## Current repository baseline

Inspection of the default `main` tree and project documentation shows:

- Unity Editor version is pinned to **6000.0.23f1** in `ProjectSettings/ProjectVersion.txt`.
- Unity package manifest declares URP **17.0.3**, Input System **1.11.2**, uGUI **2.0.0**, and Unity Test Framework **1.4.5**.
- Source is organized in `Assets/Scripts` as Unity C# runtime/editor code. It includes player/camera, vehicle, world, UI, game-state/save, localization, and NPC/traffic areas.
- `Assets/Tests` contains Unity EditMode and PlayMode test sources; `tools/` contains offline validation scripts.
- The Unity build workflow requires a configured Unity licence and uses GameCI. The build guide says the project has not yet been opened, compiled, tested, or built in Unity.
- The Unity project documentation states that its scene/assets are generated on first open, its models are procedural placeholders, religious learning content is not yet written, and translation/religious review remain outstanding.
- `main` is retained as the untouched backup. The development branch was created from its current commit; no Unity source or settings have been deleted or replaced in this audit.

## Stage 2 archive status

The separate repository contains `MyUmrahJourney-stage2 (1).zip`. The repository API exposes the archive as a base64 blob, but this session has no local workspace or archive extraction/build tool. The archive contents cannot be safely copied or validated through the available GitHub file-edit tools alone. Earlier metadata indicates that the archive includes an Android Gradle project and app/core/game source trees, including driving, collision, camera, rendering, and tests; exact Gradle, Kotlin, and libGDX versions still need verification from extracted files.

**Do not delete the Unity project or overwrite it with an unverified archive.** Preserve `main` and the current development branch until the archive can be extracted, its files inventoried, and its original build tested. Then make a deliberate, reviewable migration commit from the verified Kotlin/libGDX project, retaining the original archive and recording any Unity files intentionally removed or replaced.

## Gated next steps

1. Download and extract the ZIP outside the active repository. Record its full file inventory and checksums.
2. Inspect Gradle wrapper, Gradle and Kotlin plugin versions, module inclusion, Android application ID/namespace, libGDX version, manifests, assets, tests, and existing CI configuration.
3. Run the archive's existing unit/build checks in a suitable Android/Gradle environment before changing it. Preserve its Stage 2 driving implementation and record known failures first.
4. Confirm with the project owner which repository is the canonical home for the ZIP-based Kotlin/libGDX project. Keep the Unity repository and `main` as backups unless the owner explicitly requests removal.
5. Migrate in small commits on `native-android-development`; do not claim a native app, successful APK/AAB, playable 3D world, or device acceptance tests until each is actually built and tested.
6. Configure Codemagic only after the native Gradle project is present and validated. Keep signing keys and credentials in Codemagic secrets, never in the repository.

## Verification record

| Check | Result |
|---|---|
| Repository tree and source/docs inspected through GitHub | PASS |
| Unity version and package manifest inspected | PASS |
| Gradle/Kotlin/libGDX versions of the ZIP | NOT VERIFIED — archive not extracted |
| Unity Editor compilation/tests | NOT RUN — no Unity Editor available |
| Android Gradle build / APK / AAB | NOT RUN |
| Install and runtime tests on Android device | NOT RUN |
