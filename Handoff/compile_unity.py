"""Compiles the Unity game's Assembly-CSharp from its generated .csproj with the Roslyn
compiler that ships with the .NET SDK, without touching the running Unity Editor.

Usage: python compile_unity.py <label>
Writes the DLL and the compiler output under this folder and prints the error summary."""
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

PROJECT = r"D:\Unity\UnityProjects\AgriDabao2ndVer"
CSPROJ = os.path.join(PROJECT, "Assembly-CSharp.csproj")
CSC = r"C:\Program Files\dotnet\sdk\8.0.204\Roslyn\bincore\csc.dll"
HERE = os.path.dirname(os.path.abspath(__file__))
NS = {"m": "http://schemas.microsoft.com/developer/msbuild/2003"}

label = sys.argv[1] if len(sys.argv) > 1 else "build"
root = ET.parse(CSPROJ).getroot()

defines = ""
lang = "9.0"
for group in root.findall("m:PropertyGroup", NS):
    d = group.find("m:DefineConstants", NS)
    if d is not None and d.text and not defines:
        defines = d.text.strip()
    l = group.find("m:LangVersion", NS)
    if l is not None and l.text:
        lang = l.text.strip()

sources = [os.path.join(PROJECT, c.get("Include")) for c in root.iter("{%s}Compile" % NS["m"])]
# Scripts added since Unity last wrote the project file are not listed in it yet, so
# every non-Editor script under Assets is compiled (there are no assembly definitions).
listed = {os.path.normcase(os.path.abspath(s)) for s in sources}
for folder, _, files in os.walk(os.path.join(PROJECT, "Assets")):
    if os.sep + "Editor" in folder:
        continue
    for name in files:
        path = os.path.join(folder, name)
        if name.endswith(".cs") and os.path.normcase(os.path.abspath(path)) not in listed:
            print("not in project file yet:", os.path.relpath(path, PROJECT))
            sources.append(path)
refs = [h.text.strip() for h in root.iter("{%s}HintPath" % NS["m"])]
# Package and script assemblies are listed relative to the Unity project folder.
refs = [r if os.path.isabs(r) else os.path.join(PROJECT, r) for r in refs]
analyzers = [os.path.join(PROJECT, a.get("Include")) if not os.path.isabs(a.get("Include")) else a.get("Include")
             for a in root.iter("{%s}Analyzer" % NS["m"])]

# Scripts the project file still lists at an old path, because they have been moved
# since Unity last wrote it. The walk above already picked them up where they are now.
moved = [s for s in sources if not os.path.exists(s)]
if moved:
    print("listed at an old path, skipped:", len(moved))
sources = [s for s in sources if os.path.exists(s)]

out_dir = os.path.join(HERE, "out_" + label)
os.makedirs(out_dir, exist_ok=True)
rsp = os.path.join(out_dir, "csc.rsp")
with open(rsp, "w", encoding="utf-8") as f:
    f.write("-noconfig\n-nostdlib+\n-target:library\n-nologo\n-deterministic\n")
    f.write("-langversion:%s\n" % lang)
    f.write("-nowarn:0169,USG0001,CS0162,CS0414,CS0649,CS0618\n")
    f.write('-define:"%s"\n' % defines)
    f.write('-out:"%s"\n' % os.path.join(out_dir, "Assembly-CSharp.dll"))
    for r in refs:
        f.write('-reference:"%s"\n' % r)
    for a in analyzers:
        f.write('-analyzer:"%s"\n' % a)
    for s in sources:
        f.write('"%s"\n' % s)

proc = subprocess.run(["dotnet", CSC, "@" + rsp], capture_output=True, text=True, encoding="utf-8", errors="replace")
log = proc.stdout + proc.stderr
open(os.path.join(out_dir, "csc.log"), "w", encoding="utf-8").write(log)
errors = [line for line in log.splitlines() if re.search(r"\berror\b", line)]
warnings = [line for line in log.splitlines() if re.search(r"\bwarning\b", line)]
print("sources:", len(sources), "| references:", len(refs), "| analyzers:", len(analyzers))
print("exit:", proc.returncode, "| errors:", len(errors), "| warnings:", len(warnings))
for line in errors[:25]:
    print("  ", line[:300])
