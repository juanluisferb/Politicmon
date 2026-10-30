#!/usr/bin/env python3
"""Empaqueta el APK sin depender del Android SDK de Google.

Genera a mano:
  * AndroidManifest.xml en formato binario (AXML),
  * resources.arsc con un único recurso (@drawable/icon, nodpi),
y monta el zip con resources.arsc sin comprimir y alineado a 4 bytes (requisito de targetSdk >= 30).
La firma (v1 + v2) la hace después SignApk.java con la librería apksig.

Uso: apkpack.py --dex classes.dex --assets dist --icon icon.png --out unsigned.apk
"""
import argparse
import os
import struct
import zipfile

PACKAGE = "com.huronladron.game"
APP_LABEL = "Hurón Ladrón"
VERSION_CODE = 1
VERSION_NAME = "0.1.0"
MIN_SDK = 24  # Android 7.0+: basta la firma v2 (apksig 2.3.0 no puede firmar v1 con JDK 21)
TARGET_SDK = 34
ICON_PATH = "res/drawable-nodpi-v4/icon.png"
ICON_RES_ID = 0x7F010000  # paquete 0x7f, tipo 1 (drawable), entrada 0

ANDROID_NS = "http://schemas.android.com/apk/res/android"

# IDs de atributos del framework (android.R.attr, estables desde siempre).
ATTR = {
    "theme": 0x01010000,
    "label": 0x01010001,
    "icon": 0x01010002,
    "name": 0x01010003,
    "exported": 0x01010010,
    "launchMode": 0x0101001D,
    "screenOrientation": 0x0101001E,
    "configChanges": 0x0101001F,
    "minSdkVersion": 0x0101020C,
    "versionCode": 0x0101021B,
    "versionName": 0x0101021C,
    "targetSdkVersion": 0x01010270,
    "allowBackup": 0x01010280,
    "hardwareAccelerated": 0x010102D3,
}
THEME_NOTITLEBAR_FULLSCREEN = 0x01030007

T_REF, T_STRING, T_INT_DEC, T_INT_HEX, T_BOOL = 0x01, 0x03, 0x10, 0x11, 0x12


# ---------------------------------------------------------------- string pool
def _utf8_len(n):
    return bytes([n]) if n < 0x80 else bytes([0x80 | (n >> 8), n & 0xFF])


def string_pool(strings, utf8=True):
    data = bytearray()
    offsets = []
    for s in strings:
        offsets.append(len(data))
        if utf8:
            b = s.encode("utf-8")
            data += _utf8_len(len(s)) + _utf8_len(len(b)) + b + b"\x00"
        else:
            u = s.encode("utf-16-le")
            data += struct.pack("<H", len(s)) + u + b"\x00\x00"
    while len(data) % 4:
        data += b"\x00"
    header_size = 28
    strings_start = header_size + 4 * len(strings)
    size = strings_start + len(data)
    flags = 0x100 if utf8 else 0
    out = struct.pack("<HHIIIIII", 0x0001, header_size, size, len(strings), 0, flags, strings_start, 0)
    out += b"".join(struct.pack("<I", o) for o in offsets)
    return out + bytes(data)


# ---------------------------------------------------------------- binary XML
class Elem:
    def __init__(self, tag, attrs=None, children=None):
        self.tag = tag
        self.attrs = attrs or []  # (nombre, tipo, valor); nombre "android:x" o sin prefijo
        self.children = children or []


def build_axml(root):
    # Los nombres de atributo con ID de recurso van primero en el pool, en el orden del resource map.
    res_names = []
    others = []

    def collect(e):
        for name, typ, val in e.attrs:
            if name.startswith("android:"):
                n = name[8:]
                if n not in res_names:
                    res_names.append(n)
            elif name not in others:
                others.append(name)
            if typ == T_STRING and val not in others:
                others.append(val)
        if e.tag not in others:
            others.append(e.tag)
        for c in e.children:
            collect(c)

    collect(root)
    res_names.sort(key=lambda n: ATTR[n])
    for extra in ("android", ANDROID_NS):
        if extra not in others:
            others.append(extra)
    strings = res_names + [s for s in others if s not in res_names]
    idx = {s: i for i, s in enumerate(strings)}
    NONE = 0xFFFFFFFF

    chunks = bytearray()
    chunks += string_pool(strings, utf8=True)
    resmap = b"".join(struct.pack("<I", ATTR[n]) for n in res_names)
    chunks += struct.pack("<HHI", 0x0180, 8, 8 + len(resmap)) + resmap
    chunks += struct.pack("<HHIIIII", 0x0100, 16, 24, 1, NONE, idx["android"], idx[ANDROID_NS])

    def emit(e, line=[1]):
        line[0] += 1
        attrs = []
        for name, typ, val in e.attrs:
            if name.startswith("android:"):
                ns, nm, rid = idx[ANDROID_NS], idx[name[8:]], ATTR[name[8:]]
            else:
                ns, nm, rid = NONE, idx[name], 0xFFFFFFFF
            if typ == T_STRING:
                raw, data = idx[val], idx[val]
            elif typ == T_BOOL:
                raw, data = NONE, 0xFFFFFFFF if val else 0
            else:
                raw, data = NONE, val & 0xFFFFFFFF
            attrs.append((rid, ns, nm, raw, typ, data))
        attrs.sort(key=lambda a: a[0])  # primero los de ID de recurso, ordenados
        body = struct.pack("<IIHHHHHH", NONE, idx[e.tag], 20, 20, len(attrs), 0, 0, 0)
        for rid, ns, nm, raw, typ, data in attrs:
            body += struct.pack("<IIIHBBI", ns, nm, raw, 8, 0, typ, data)
        chunks.extend(struct.pack("<HHIII", 0x0102, 16, 16 + len(body), line[0], NONE) + body)
        for c in e.children:
            emit(c)
        line[0] += 1
        chunks.extend(struct.pack("<HHIIIII", 0x0103, 16, 24, line[0], NONE, NONE, idx[e.tag]))

    emit(root)
    chunks += struct.pack("<HHIIIII", 0x0101, 16, 24, 99, NONE, idx["android"], idx[ANDROID_NS])
    return struct.pack("<HHI", 0x0003, 8, 8 + len(chunks)) + bytes(chunks)


def manifest():
    activity = Elem("activity", [
        ("android:name", T_STRING, PACKAGE + ".MainActivity"),
        ("android:exported", T_BOOL, True),
        ("android:launchMode", T_INT_DEC, 2),            # singleTask
        ("android:screenOrientation", T_INT_DEC, 6),     # sensorLandscape
        ("android:configChanges", T_INT_HEX, 0x1FF0),    # rotación, teclado, tamaño, densidad...
    ], [Elem("intent-filter", [], [
        Elem("action", [("android:name", T_STRING, "android.intent.action.MAIN")]),
        Elem("category", [("android:name", T_STRING, "android.intent.category.LAUNCHER")]),
    ])])
    app = Elem("application", [
        ("android:label", T_STRING, APP_LABEL),
        ("android:icon", T_REF, ICON_RES_ID),
        ("android:theme", T_REF, THEME_NOTITLEBAR_FULLSCREEN),
        ("android:allowBackup", T_BOOL, True),
        ("android:hardwareAccelerated", T_BOOL, True),
    ], [activity])
    root = Elem("manifest", [
        ("android:versionCode", T_INT_DEC, VERSION_CODE),
        ("android:versionName", T_STRING, VERSION_NAME),
        ("package", T_STRING, PACKAGE),
    ], [Elem("uses-sdk", [
        ("android:minSdkVersion", T_INT_DEC, MIN_SDK),
        ("android:targetSdkVersion", T_INT_DEC, TARGET_SDK),
    ]), app])
    return build_axml(root)


# ---------------------------------------------------------------- resources.arsc
def build_arsc():
    values = string_pool([ICON_PATH], utf8=True)
    types = string_pool(["drawable"], utf8=False)
    keys = string_pool(["icon"], utf8=False)

    type_spec = struct.pack("<HHIBBHI", 0x0202, 16, 20, 1, 0, 0, 1) + struct.pack("<I", 0)

    config = bytearray(64)
    struct.pack_into("<I", config, 0, 64)
    struct.pack_into("<H", config, 14, 0xFFFF)  # density = nodpi
    struct.pack_into("<H", config, 24, 4)       # sdkVersion = 4 (implícito al usar densidad)
    header_size = 20 + len(config)
    entries_start = header_size + 4
    entry = struct.pack("<HHI", 8, 0, 0) + struct.pack("<HBBI", 8, 0, T_STRING, 0)
    type_chunk = (struct.pack("<HHIBBHII", 0x0201, header_size, entries_start + len(entry), 1, 0, 0, 1, entries_start)
                  + bytes(config) + struct.pack("<I", 0) + entry)

    name = PACKAGE.encode("utf-16-le").ljust(256, b"\x00")
    pkg_header = 288
    type_off = pkg_header
    key_off = type_off + len(types)
    body = types + keys + type_spec + type_chunk
    package = struct.pack("<HHII", 0x0200, pkg_header, pkg_header + len(body), 0x7F) + name
    package += struct.pack("<IIIII", type_off, 1, key_off, 1, 0) + body

    content = values + package
    return struct.pack("<HHII", 0x0002, 12, 12 + len(content), 1) + content


# ---------------------------------------------------------------- zip alineado
def add_stored_aligned(zf, arcname, data, align=4):
    info = zipfile.ZipInfo(arcname, date_time=(2026, 1, 1, 0, 0, 0))
    info.compress_type = zipfile.ZIP_STORED
    header_offset = zf.fp.tell()
    name_len = len(arcname.encode("utf-8"))
    data_start = header_offset + 30 + name_len
    pad = (-data_start) % align
    if pad:
        # Campo extra 0xD935 (el que usa zipalign): id + tamaño + relleno.
        pad = pad if pad >= 4 else pad + align
        while (data_start + pad) % align:
            pad += 1
        info.extra = struct.pack("<HH", 0xD935, pad - 4) + b"\x00" * (pad - 4)
    zf.writestr(info, data)


def add_deflated(zf, arcname, data):
    info = zipfile.ZipInfo(arcname, date_time=(2026, 1, 1, 0, 0, 0))
    info.compress_type = zipfile.ZIP_DEFLATED
    zf.writestr(info, data, compresslevel=9)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dex", required=True)
    ap.add_argument("--assets", required=True)
    ap.add_argument("--icon", required=True)
    ap.add_argument("--out", required=True)
    a = ap.parse_args()

    with zipfile.ZipFile(a.out, "w") as zf:
        add_deflated(zf, "AndroidManifest.xml", manifest())
        add_stored_aligned(zf, "resources.arsc", build_arsc())
        add_deflated(zf, "classes.dex", open(a.dex, "rb").read())
        add_stored_aligned(zf, ICON_PATH, open(a.icon, "rb").read())
        for root, _, files in sorted(os.walk(a.assets)):
            for f in sorted(files):
                p = os.path.join(root, f)
                rel = os.path.relpath(p, a.assets).replace(os.sep, "/")
                add_deflated(zf, "assets/" + rel, open(p, "rb").read())
    print("APK sin firmar:", a.out)


if __name__ == "__main__":
    main()
