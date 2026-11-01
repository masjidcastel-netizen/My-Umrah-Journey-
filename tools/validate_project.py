#!/usr/bin/env python3
"""Static project validation (no Unity needed). Mirrors what Assets/Editor/ProjectValidator.cs checks inside Unity."""
import json, re, sys, pathlib

root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else ".")
results = []
def ok(name, cond, detail=""): results.append((bool(cond), name, detail))

pv = root / "ProjectSettings/ProjectVersion.txt"
ok("ProjectVersion.txt present and Unity 6 (6000.x)", pv.exists() and "6000." in pv.read_text())
man = root / "Packages/manifest.json"
try:
    deps = json.loads(man.read_text())["dependencies"]
    ok("manifest.json valid JSON", True)
    ok("manifest has URP + Input System + ugui", all(k in deps for k in ("com.unity.render-pipelines.universal", "com.unity.inputsystem", "com.unity.ugui")))
except Exception as e:
    ok("manifest.json valid JSON", False, str(e))

ps = (root / "ProjectSettings/ProjectSettings.asset").read_text()
ok("Package id com.masjidcastel.myumrahjourney", "Android: com.masjidcastel.myumrahjourney" in ps)
ok("ARM64 (AndroidTargetArchitectures: 2)", "AndroidTargetArchitectures: 2" in ps)
ok("IL2CPP (scriptingBackend Android: 1)", re.search(r"scriptingBackend:\s*\n\s*Android: 1", ps))
ok("Landscape only", "allowedAutorotateToPortrait: 0" in ps and "allowedAutorotateToLandscapeLeft: 1" in ps)
ok("Input System active handler", "activeInputHandler: 1" in ps)

for d in ["Animations","Audio","Characters","Environment","Mauritius","Madinah","Makkah","Vehicles","IslamicAssets","Materials","Models","Prefabs","Scenes","UI","Resources","Settings","Editor"]:
    ok(f"Assets/{d}/ exists", (root / "Assets" / d).is_dir())
for d in ["Core","Player","Vehicles","AI","World","Missions","IslamicActivities","Umrah","Hajj","UI","Audio","Localization","SaveSystem"]:
    ok(f"Assets/Scripts/{d}/ exists", (root / "Assets/Scripts" / d).is_dir())

for a in ["Assets/Scripts/MyUmrah.Runtime.asmdef","Assets/Editor/MyUmrah.Editor.asmdef","Assets/Tests/EditMode/MyUmrah.Tests.EditMode.asmdef","Assets/Tests/PlayMode/MyUmrah.Tests.PlayMode.asmdef"]:
    try: json.loads((root / a).read_text()); ok(f"{a} valid JSON", True)
    except Exception as e: ok(f"{a} valid JSON", False, str(e))

# Localisation completeness
loc = (root / "Assets/Scripts/Localization/Loc.cs").read_text(encoding="utf-8")
keys = 0; badkeys = []
for line in loc.splitlines():
    line = line.strip()
    if line.startswith('A("'):
        strs = re.findall(r'"((?:[^"\\]|\\.)*)"', line)
        keys += 1
        if len(strs) != 6 or any(not s.strip() for s in strs): badkeys.append(strs[0] if strs else line[:30])
ok(f"Localisation: {keys} keys x 5 languages complete", keys > 0 and not badkeys, ",".join(badkeys))

# Every key used in code exists
used = set()
for f in (root / "Assets/Scripts").rglob("*.cs"):
    t = f.read_text(encoding="utf-8")
    used |= set(re.findall(r'(?:Loc\.T|ToastKey|Missions\.Report)\("([a-z_0-9]+)"', t))
    used |= set(re.findall(r'(?:promptKey|titleKey|descKey|nameKey|itemKey)\s*=\s*"([a-z_0-9]+)"', t))
    used |= set(re.findall(r'new MissionDef\("[a-z]+",\s*"([a-z_0-9]+)",\s*"([a-z_0-9]+)"', t) and [x for m in re.findall(r'new MissionDef\("[a-z]+",\s*"([a-z_0-9]+)",\s*"([a-z_0-9]+)"', t) for x in m])
known = set(re.findall(r'A\("([a-z_0-9]+)"', loc))
dyn = {"walk","camera","masjid","drive","greet","swim"}  # mission event ids, not loc keys
missing = sorted(k for k in used if k not in known and k not in dyn and not k.endswith("_"))  # "npc_" is a runtime prefix (npc_1..3)
ok("All referenced localisation keys exist", not missing, ",".join(missing))

# Prohibited web technology
bad = []
for f in root.rglob("*"):
    if f.is_file() and f.suffix.lower() in (".html", ".htm", ".css", ".js", ".jslib") and "Library" not in f.parts: bad.append(str(f.relative_to(root)))
for f in root.rglob("*.cs"):
    t = f.read_text(encoding="utf-8")
    if re.search(r"WebView|UnityWebRequest|WebGL|three\.js|babylon", t, re.I): bad.append(str(f.relative_to(root)))
ok("No HTML/CSS/JS/WebView/WebGL anywhere in the project", not bad, ",".join(bad))

fails = 0
for good, name, detail in results:
    print(("PASS  " if good else "FAIL  ") + name + (f"  [{detail}]" if detail else ""))
    fails += (not good)
print(f"\n{len(results)-fails}/{len(results)} checks passed")
sys.exit(1 if fails else 0)
