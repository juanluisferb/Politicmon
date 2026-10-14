"""Exports the generated game content into the Unity 6 project (unity/).

- Copies sprites, tiles, UI art, font atlas and audio into Assets/Resources.
- Converts data/*.json into JsonUtility-friendly JSON (arrays instead of dictionaries).
- Writes .meta files with deterministic GUIDs for textures (pixel-art import settings,
  sliced character spritesheets) and AnimationClips (.anim) for every overworld character.

Run after tools/build_assets.sh:  python3 tools/export_unity.py
"""
import hashlib
import json
import os
import shutil

import pixlib

ROOT = pixlib.ROOT
UNITY = os.path.join(ROOT, "unity")
ASSETS = os.path.join(UNITY, "Assets")
RES = os.path.join(ASSETS, "Resources")
STATS = ["carisma", "oratoria", "aguante", "dialectica", "templanza", "agilidad"]
DIRECTIONS = ["down", "up", "left", "right"]


def guid_for(path):
    return hashlib.md5(("politicmon:" + path).encode()).hexdigest()


def internal_id(name):
    return int(hashlib.md5(name.encode()).hexdigest()[:15], 16)


def load(rel):
    with open(os.path.join(ROOT, "data", rel), encoding="utf-8") as f:
        return json.load(f)


def write_json(rel, obj):
    path = os.path.join(RES, "Data", rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(obj, f, ensure_ascii=False, indent=1)


def strip(d):
    return {k: v for k, v in d.items() if not k.startswith("_")}


# ---------------------------------------------------------------- data
def export_data():
    t = load("types.json")
    chart = []
    for atk, row in t["chart"].items():
        for dfn, mult in row.items():
            chart.append({"attacker": atk, "defender": dfn, "multiplier": mult})
    write_json("types.json", {
        "regionalBonus": t["regional_bonus"], "regionalType": t["regional_type"],
        "types": [{"id": k, "name": v["name"], "color": v["color"]} for k, v in t["types"].items()],
        "chart": chart})

    moves = []
    for k, m in strip(load("moves.json")).items():
        e = m.get("effect", {})
        moves.append({"id": k, "name": m["name"], "type": m["type"], "category": m["category"],
                      "power": m["power"], "accuracy": m["accuracy"], "pp": m["pp"], "description": m["desc"],
                      "effect": {"kind": e.get("kind", ""), "status": e.get("status", ""), "chance": e.get("chance", 100),
                                 "target": e.get("target", ""), "stat": e.get("stat", ""), "stages": e.get("stages", 0),
                                 "fraction": e.get("fraction", 0.0), "turns": e.get("turns", 0)}})
    write_json("moves.json", {"moves": moves})

    pols = []
    for k, p in strip(load("politicians.json")).items():
        pols.append({"id": int(k), "key": p["key"], "name": p["name"], "types": p["types"], "zone": p["zone"],
                     "baseStats": {s: p["stats"][s] for s in STATS}, "catchRate": p["catch_rate"],
                     "baseExp": p["base_exp"], "learnset": [{"move": m, "level": l} for m, l in p["moves"]],
                     "description": p["desc"], "evolvesTo": p.get("evolves_to", 0),
                     "evolveLevel": p.get("evolve_level", 0)})
    pols.sort(key=lambda p: p["id"])
    write_json("politicians.json", {"politicians": pols})

    items = []
    for k, it in strip(load("items.json")).items():
        items.append({"id": k, "name": it["name"], "kind": it["kind"], "catchMultiplier": it.get("catch_mult", 0.0),
                      "amount": it.get("amount", 0), "curesStatus": it.get("status", ""), "price": it["price"],
                      "usage": it["use"], "icon": it["icon"], "description": it["desc"]})
    write_json("items.json", {"items": items})

    parties = [{"id": k, "name": p["name"], "shortName": p["short"], "motto": p.get("motto", "")}
               for k, p in strip(load("parties.json")).items()]
    write_json("parties.json", {"parties": parties})

    trainers = []
    for k, t in strip(load("trainers.json")).items():
        trainers.append({"id": k, "name": t["name"], "party": t["party"], "sprite": t["sprite"],
                         "team": [{"species": e[0], "level": e[1]} for e in t["team"]], "reward": t["reward"],
                         "intro": t["intro"], "defeatLine": t["lose"], "afterLine": t["after"],
                         "isLeader": t.get("leader", False), "badge": t.get("badge", ""), "badgeName": t.get("badge_name", "")})
    write_json("trainers.json", {"trainers": trainers})

    tiles = []
    td = load("tiles.json")
    for k, v in strip(td).items():
        tiles.append({"name": k, "index": v["index"], "solid": v.get("solid", False), "encounter": v.get("encounter", False),
                      "door": v.get("door", False), "water": v.get("water", False)})
    tiles.sort(key=lambda t: t["index"])
    write_json("tiles.json", {"columns": td["_columns"], "tiles": tiles})

    for fn in sorted(os.listdir(os.path.join(ROOT, "data", "maps"))):
        m = load("maps/" + fn)
        out = {"id": m["id"], "name": m["name"], "zone": m["zone"], "music": m["music"], "width": m["width"],
               "height": m["height"], "border": m["border"], "interior": m["interior"],
               "legend": [{"symbol": ch, "tile": t} for ch, t in m["legend"].items()],
               "rows": m["rows"],
               "warps": [{"x": w["x"], "y": w["y"], "toMap": w["to"], "toX": w["tx"], "toY": w["ty"], "facing": w["dir"],
                          "requiredBadges": w.get("require_badges", 0), "requiredFlag": w.get("require_flag", ""),
                          "deniedText": w.get("deny", "")} for w in m["warps"]],
               "npcs": [{"id": n["id"], "x": n["x"], "y": n["y"], "sprite": n["sprite"], "facing": n.get("dir", "down"),
                         "eventType": n.get("event", "talk") if "trainer" not in n else "trainer",
                         "lines": n.get("text", []), "trainer": n.get("trainer", ""), "sight": n.get("sight", 0),
                         "hideIf": n.get("hide_if", ""), "stock": n.get("stock", [])} for n in m["npcs"]],
               "signs": m["signs"],
               "items": [{"id": i["id"], "x": i["x"], "y": i["y"], "item": i["item"], "count": i["count"]} for i in m["items"]],
               "areas": [{"name": a["name"], "x": a["rect"][0], "y": a["rect"][1], "width": a["rect"][2], "height": a["rect"][3]} for a in m["areas"]],
               "encounterRate": (m["encounters"] or {}).get("rate", 0.0),
               "encounters": [{"species": e[0], "minLevel": e[1], "maxLevel": e[2], "weight": e[3]}
                              for e in (m["encounters"] or {}).get("table", [])]}
        write_json("Maps/" + fn, out)

    glyphs = []
    with open(os.path.join(ROOT, "assets", "fonts", "pixel8.fnt"), encoding="utf-8") as f:
        for line in f:
            if line.startswith("char "):
                kv = dict(p.split("=") for p in line.split()[1:])
                glyphs.append({"code": int(kv["id"]), "x": int(kv["x"]), "y": int(kv["y"]),
                               "width": int(kv["width"]), "advance": int(kv["xadvance"])})
    write_json("font.json", {"lineHeight": 10, "glyphHeight": 8, "glyphs": glyphs})


# ---------------------------------------------------------------- textures & audio
TEXTURE_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: {sprite_mode}
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 16
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
{sprites}    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: {sheet_id}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
{name_table}  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

SPRITE_ENTRY = """    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: {x}
        y: {y}
        width: 16
        height: 16
      alignment: 9
      pivot: {{x: 0.5, y: 0}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {sprite_id}
      internalID: {iid}
      vertices: []
      indices: 
      edges: []
      weights: []
"""


def write_texture_meta(dst, rel, sheet_frames=None):
    guid = guid_for(rel)
    sprites = ""
    names = ""
    if sheet_frames:
        for name, x, y in sheet_frames:
            iid = internal_id(rel + ":" + name)
            sprites += SPRITE_ENTRY.format(name=name, x=x, y=y, sprite_id=hashlib.md5(name.encode()).hexdigest(), iid=iid)
            names += "      %s: %d\n" % (name, iid)
    else:
        sprites = ""
        names = "      {}\n"
    text = TEXTURE_META.format(guid=guid, sprite_mode=2 if sheet_frames else 1,
                               sprites=sprites if sheet_frames else "    []\n".replace("    []\n", ""),
                               sheet_id=hashlib.md5(rel.encode()).hexdigest(), name_table=names if sheet_frames else "      {}\n")
    if not sheet_frames:
        text = text.replace("    sprites:\n    outline: []", "    sprites: []\n    outline: []")
        text = text.replace("    nameFileIdTable:\n      {}\n", "    nameFileIdTable: {}\n")
    with open(dst + ".meta", "w", encoding="utf-8") as f:
        f.write(text)
    return guid


def copy_tree(src_dir, dst_rel, rename=None):
    out = []
    src = os.path.join(ROOT, src_dir)
    dst_dir = os.path.join(RES, dst_rel)
    os.makedirs(dst_dir, exist_ok=True)
    for fn in sorted(os.listdir(src)):
        if fn.endswith(".import"):
            continue
        name = rename(fn) if rename else fn
        if name is None:
            continue
        dst = os.path.join(dst_dir, name)
        shutil.copyfile(os.path.join(src, fn), dst)
        out.append(dst)
    return out


def character_frames(name):
    """Overworld sheets are 32x64: columns = frame (0 stand, 1 step), rows = down, up, left, right (top to bottom).
    Unity rects start at the bottom-left corner of the texture."""
    frames = []
    for row, d in enumerate(DIRECTIONS):
        for col in range(2):
            frames.append(("%s_%s_%d" % (name, d, col), col * 16, 64 - 16 * (row + 1)))
    return frames


ANIM_TEMPLATE = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!74 &7400000
AnimationClip:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  serializedVersion: 7
  m_Legacy: 0
  m_Compressed: 0
  m_UseHighQualityCurve: 1
  m_RotationCurves: []
  m_CompressedRotationCurves: []
  m_EulerCurves: []
  m_PositionCurves: []
  m_ScaleCurves: []
  m_FloatCurves: []
  m_PPtrCurves:
  - serializedVersion: 2
    curve:
{keys}    attribute: m_Sprite
    path: 
    classID: 212
    script: {{fileID: 0}}
    flags: 2
  m_SampleRate: 12
  m_WrapMode: 0
  m_Bounds:
    m_Center: {{x: 0, y: 0, z: 0}}
    m_Extent: {{x: 0, y: 0, z: 0}}
  m_ClipBindingConstant:
    genericBindings:
    - serializedVersion: 2
      path: 0
      attribute: 0
      script: {{fileID: 0}}
      typeID: 212
      customType: 23
      isPPtrCurve: 1
      isIntCurve: 0
      isSerializeReferenceCurve: 0
    pptrCurveMapping:
{mapping}  m_AnimationClipSettings:
    serializedVersion: 2
    m_AdditiveReferencePoseClip: {{fileID: 0}}
    m_AdditiveReferencePoseTime: 0
    m_StartTime: 0
    m_StopTime: {stop}
    m_OrientationOffsetY: 0
    m_Level: 0
    m_CycleOffset: 0
    m_HasAdditiveReferencePose: 0
    m_LoopTime: 1
    m_LoopBlend: 0
    m_LoopBlendOrientation: 0
    m_LoopBlendPositionY: 0
    m_LoopBlendPositionXZ: 0
    m_KeepOriginalOrientation: 0
    m_KeepOriginalPositionY: 1
    m_KeepOriginalPositionXZ: 0
    m_HeightFromFeet: 0
    m_Mirror: 0
  m_EditorCurves: []
  m_EulerEditorCurves: []
  m_HasGenericRootTransform: 0
  m_HasMotionFloatCurves: 0
  m_Events: []
"""


def write_animations(char, tex_rel):
    """walk_<dir> clips (4 keys: stand, step, stand, step) and idle_<dir> (1 key)."""
    guid = guid_for(tex_rel)
    out_dir = os.path.join(RES, "Animations", "Characters", char)
    os.makedirs(out_dir, exist_ok=True)
    for d in DIRECTIONS:
        stand = internal_id(tex_rel + ":%s_%s_0" % (char, d))
        step = internal_id(tex_rel + ":%s_%s_1" % (char, d))
        for clip, seq, dur in (("walk_" + d, [step, stand, step, stand], 1.0 / 12.0), ("idle_" + d, [stand], 1.0 / 12.0)):
            keys = ""
            for i, fid in enumerate(seq):
                keys += "    - time: %s\n      value: {fileID: %d, guid: %s, type: 3}\n" % (round(i * dur * 1.0, 6), fid, guid)
            mapping = "".join("    - {fileID: %d, guid: %s, type: 3}\n" % (fid, guid) for fid in seq)
            stop = round(len(seq) * dur, 6)
            name = "%s_%s" % (char, clip)
            path = os.path.join(out_dir, name + ".anim")
            with open(path, "w", encoding="utf-8") as f:
                f.write(ANIM_TEMPLATE.format(name=name, keys=keys, mapping=mapping, stop=stop))
            with open(path + ".meta", "w", encoding="utf-8") as f:
                f.write("fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 7400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
                        % guid_for(os.path.relpath(path, UNITY)))


def export_assets():
    def rel(p):
        return os.path.relpath(p, UNITY)

    for p in copy_tree("assets/sprites/politicians", "Sprites/Politicians"):
        write_texture_meta(p, rel(p))
    for p in copy_tree("assets/sprites/trainers", "Sprites/Trainers"):
        write_texture_meta(p, rel(p))
    for p in copy_tree("assets/sprites/items", "Sprites/Items"):
        write_texture_meta(p, rel(p))
    for p in copy_tree("assets/sprites/overworld", "Sprites/Characters"):
        name = os.path.splitext(os.path.basename(p))[0]
        write_texture_meta(p, rel(p), character_frames(name))
        write_animations(name, rel(p))
    for p in copy_tree("assets/tiles", "Sprites/Tiles"):
        write_texture_meta(p, rel(p))
    for p in copy_tree("assets/ui", "Sprites/UI", rename=lambda f: f if f.endswith(".png") and f != "theme.tres" else None):
        write_texture_meta(p, rel(p))
    for p in copy_tree("assets/fonts", "Fonts", rename=lambda f: f if f.endswith(".png") else None):
        write_texture_meta(p, rel(p))
    copy_tree("assets/audio", "Audio/Music", rename=lambda f: f[len("music_"):] if f.startswith("music_") else None)
    copy_tree("assets/audio", "Audio/Sfx", rename=lambda f: f[len("sfx_"):] if f.startswith("sfx_") else None)
    contact = os.path.join(ASSETS, "Art", "ContactSheets")
    os.makedirs(contact, exist_ok=True)
    for fn in os.listdir(os.path.join(ROOT, "docs", "contact_sheets")):
        shutil.copyfile(os.path.join(ROOT, "docs", "contact_sheets", fn), os.path.join(contact, fn))
        write_texture_meta(os.path.join(contact, fn), rel(os.path.join(contact, fn)))


def write_script_metas():
    """Deterministic GUIDs for C# scripts so scenes can reference them."""
    for base, _, files in os.walk(ASSETS):
        for fn in files:
            if fn.endswith(".cs") or fn.endswith(".asmdef"):
                p = os.path.join(base, fn)
                meta = p + ".meta"
                if os.path.exists(meta):
                    continue
                importer = "MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" \
                    if fn.endswith(".cs") else "AssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
                with open(meta, "w", encoding="utf-8") as f:
                    f.write("fileFormatVersion: 2\nguid: %s\n%s" % (guid_for(os.path.relpath(p, UNITY)), importer))


def main():
    for sub in ("Sprites", "Audio", "Fonts", "Data", "Animations"):
        shutil.rmtree(os.path.join(RES, sub), ignore_errors=True)
    export_data()
    export_assets()
    write_script_metas()
    print("unity export done")


if __name__ == "__main__":
    main()
