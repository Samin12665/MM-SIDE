"""
Build the ARV-1 Onshape document through the Onshape REST API (Path C in docs/06_onshape_build_guide.md).

Requires:
  * network access to https://cad.onshape.com from this environment
  * env secrets ONSHAPE_ACCESS_KEY / ONSHAPE_SECRET_KEY (Onshape → My account → Developer → API keys)
  * python3 cad/build.py has been run (out/step/*.step exist)

Usage:
  python3 cad/onshape/onshape_api_build.py --dry-run      # print the plan only
  python3 cad/onshape/onshape_api_build.py                # create a NEW document (never touches existing ones)

Status: written offline — the session that produced it could not reach cad.onshape.com, so it has
not yet been exercised against the live API. Each step logs the request so failures are easy to fix.
"""
import base64, json, os, sys, time, urllib.request, urllib.error, uuid

BASE = os.environ.get("ONSHAPE_BASE", "https://cad.onshape.com") + "/api/v10"
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, "..", "..")
DOC_NAME = "ARV-1 Autonomous Research Vehicle"
IMPORTS = [  # (step file, tab name)
    ("ASM_CAR_COMPLETE.step", "ASM_CAR_COMPLETE"),
    ("ASM_FRONT_SUSPENSION.step", "ASM_FRONT_SUSPENSION"),
    ("ASM_REAR_SUSPENSION.step", "ASM_REAR_SUSPENSION"),
    ("ASM_STEERING.step", "ASM_STEERING"),
    ("ASM_DRIVETRAIN.step", "ASM_DRIVETRAIN"),
    ("ASM_CHASSIS.step", "ASM_CHASSIS"),
    ("ASM_SENSOR_SYSTEM.step", "ASM_SENSOR_SYSTEM"),
    ("ASM_ELECTRONICS.step", "ASM_ELECTRONICS"),
]


def auth():
    a, s = os.environ.get("ONSHAPE_ACCESS_KEY"), os.environ.get("ONSHAPE_SECRET_KEY")
    if not a or not s:
        sys.exit("Set ONSHAPE_ACCESS_KEY and ONSHAPE_SECRET_KEY as environment secrets.")
    return "Basic " + base64.b64encode(f"{a}:{s}".encode()).decode()


def req(method, path, body=None, raw=None, ctype="application/json"):
    url = BASE + path
    data = raw if raw is not None else (json.dumps(body).encode() if body is not None else None)
    r = urllib.request.Request(url, data=data, method=method)
    r.add_header("Authorization", auth())
    r.add_header("Accept", "application/json;charset=UTF-8; qs=0.09")
    if data is not None:
        r.add_header("Content-Type", ctype)
    try:
        with urllib.request.urlopen(r, timeout=120) as resp:
            txt = resp.read().decode()
            return json.loads(txt) if txt else {}
    except urllib.error.HTTPError as e:
        print(f"!! {method} {path} -> {e.code}: {e.read().decode()[:500]}")
        raise


def multipart(fields, file_field, file_path):
    b = uuid.uuid4().hex
    out = []
    for k, v in fields.items():
        out.append(f"--{b}\r\nContent-Disposition: form-data; name=\"{k}\"\r\n\r\n{v}\r\n".encode())
    fn = os.path.basename(file_path)
    out.append(f"--{b}\r\nContent-Disposition: form-data; name=\"{file_field}\"; filename=\"{fn}\"\r\n"
               f"Content-Type: application/octet-stream\r\n\r\n".encode())
    out.append(open(file_path, "rb").read())
    out.append(f"\r\n--{b}--\r\n".encode())
    return b"".join(out), f"multipart/form-data; boundary={b}"


def wait_translation(tid):
    for _ in range(120):
        t = req("GET", f"/translations/{tid}")
        st = t.get("requestState")
        if st in ("DONE", "FAILED"):
            return t
        time.sleep(3)
    raise TimeoutError(tid)


def main():
    dry = "--dry-run" in sys.argv
    plan = [f"create document '{DOC_NAME}'", "create Feature Studio 00_CAR_MASTER_FS with CAR_MASTER.fs",
            "create Part Studio 00_CAR_MASTER"] + [f"import {f} as {t}" for f, t in IMPORTS]
    print("PLAN:\n  " + "\n  ".join(plan))
    if dry:
        return
    doc = req("POST", "/documents", {"name": DOC_NAME, "isPublic": False})
    did, wid = doc["id"], doc["defaultWorkspace"]["id"]
    print("document", did, "workspace", wid)

    # Feature Studio with the generated master
    fs = req("POST", f"/featurestudios/d/{did}/w/{wid}", {"name": "00_CAR_MASTER_FS"})
    eid = fs["id"]
    cur = req("GET", f"/featurestudios/d/{did}/w/{wid}/e/{eid}")
    header = "\n".join(cur["contents"].splitlines()[:2])          # keep Onshape's version + std import lines
    body = open(os.path.join(HERE, "CAR_MASTER.fs")).read().split("// ---- paste below ----", 1)[1]
    req("POST", f"/featurestudios/d/{did}/w/{wid}/e/{eid}",
        {"contents": header + "\n" + body, "serializationVersion": cur.get("serializationVersion"),
         "sourceMicroversion": cur.get("sourceMicroversion"), "rejectMicroversionSkew": False})
    print("feature studio committed")

    ps = req("POST", f"/partstudios/d/{did}/w/{wid}", {"name": "00_CAR_MASTER"})
    print("part studio 00_CAR_MASTER", ps["id"], "(insert the two ARV-1 custom features from the toolbar;"
          " API feature insertion of custom features needs the studio microversion namespace — see guide §3.2)")

    for fname, tab in IMPORTS:
        path = os.path.join(ROOT, "out", "step", fname)
        raw, ctype = multipart({"translate": "true", "formatName": "STEP", "flattenAssemblies": "false",
                                "yAxisIsUp": "false", "splitAssembliesIntoMultipleDocuments": "false",
                                "storeInDocument": "true", "encodedFilename": fname}, "file", path)
        r = req("POST", f"/blobelements/d/{did}/w/{wid}", raw=raw, ctype=ctype)
        tid = r.get("translationId")
        if tid:
            t = wait_translation(tid)
            print(f"imported {fname}: {t.get('requestState')} -> elements {t.get('resultElementIds')}")
    print(f"\nOpen: https://cad.onshape.com/documents/{did}/w/{wid}")


if __name__ == "__main__":
    main()
