#!/usr/bin/env python3
"""Comprobaciones del APK: entradas obligatorias, alineación y, si androguard está instalado, manifiesto."""
import struct
import sys
import zipfile

path = sys.argv[1]
ok = True
with zipfile.ZipFile(path) as z, open(path, "rb") as f:
    names = z.namelist()
    for need in ("AndroidManifest.xml", "classes.dex", "resources.arsc", "assets/index.html",
                 "res/drawable-nodpi-v4/icon.png"):
        if need not in names:
            print("FALTA", need); ok = False
    for info in z.infolist():
        if info.compress_type == zipfile.ZIP_STORED:
            f.seek(info.header_offset)
            hdr = f.read(30)
            n, e = struct.unpack("<HH", hdr[26:30])
            start = info.header_offset + 30 + n + e
            if start % 4:
                print("SIN ALINEAR", info.filename, start); ok = False
    if z.getinfo("resources.arsc").compress_type != zipfile.ZIP_STORED:
        print("resources.arsc comprimido"); ok = False
try:
    from loguru import logger
    logger.remove()
    from androguard.core.apk import APK
    from androguard.core.dex import DEX
    a = APK(path)
    print("Paquete:", a.get_package(), "| versión:", a.get_androidversion_name(),
          "| SDK min/target:", a.get_min_sdk_version(), a.get_target_sdk_version())
    print("Actividad principal:", a.get_main_activity(), "| etiqueta:", a.get_app_name())
    icon = a.get_app_icon()
    print("Icono:", icon)
    dex = DEX(a.get_dex())
    print("Clases en classes.dex:", sorted(c.get_name() for c in dex.get_classes()))
    if a.get_main_activity() != "com.huronladron.game.MainActivity" or icon != "res/drawable-nodpi-v4/icon.png":
        ok = False
except ImportError:
    print("(androguard no instalado: se omite la lectura del manifiesto)")
print("APK OK" if ok else "APK CON ERRORES")
sys.exit(0 if ok else 1)
