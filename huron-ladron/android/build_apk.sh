#!/usr/bin/env bash
# Construye release/huron-ladron.apk a partir de dist/ sin Android SDK:
#   javac (contra android.jar API 16) -> dx -> apkpack.py (manifiesto/arsc/zip) -> apksig (firma v2).
# Las tres dependencias se descargan de Maven Central y se comprueba su SHA-1.
set -euo pipefail
cd "$(dirname "$0")/.."
ROOT=$PWD
CACHE=android/.cache
BUILD=build/android
OUT=release/huron-ladron.apk
mkdir -p "$CACHE" "$BUILD" release

M=https://repo1.maven.org/maven2
fetch() { # ruta-maven fichero sha1
  local dst="$CACHE/$2"
  if [ -f "$dst" ] && [ "$(sha1sum "$dst" | cut -d' ' -f1)" = "$3" ]; then return; fi
  for i in 1 2 3 4 5 6 7 8; do
    if curl -sSfL -o "$dst" "$M/$1" 2>/dev/null; then break; fi
    sleep $((i * 2))   # Maven Central limita peticiones (429): reintento con espera
  done
  [ "$(sha1sum "$dst" | cut -d' ' -f1)" = "$3" ] || { echo "SHA-1 incorrecto en $2" >&2; exit 1; }
}
fetch com/google/android/android/4.1.1.4/android-4.1.1.4.jar android.jar 3fb039385e71e9aa2ba547ea9ea8caa34a4ffac7
fetch com/jakewharton/android/repackaged/dalvik-dx/16.0.1/dalvik-dx-16.0.1.jar dx.jar 71ac16d1f34143d86440fca923422f9c2e2ac403
fetch com/android/tools/build/apksig/2.3.0/apksig-2.3.0.jar apksig.jar 6ef7a58375aa68fb492b58edd97607bee8ee5c5c

[ -f dist/index.html ] || { echo "Falta dist/: ejecuta npm run build" >&2; exit 1; }

rm -rf "$BUILD/classes" "$BUILD/signer"
mkdir -p "$BUILD/classes" "$BUILD/signer"
javac -nowarn --release 8 -cp "$CACHE/android.jar" \
  -d "$BUILD/classes" android/java/com/huronladron/game/*.java
java -cp "$CACHE/dx.jar" com.android.dx.command.Main --dex --output="$BUILD/classes.dex" "$BUILD/classes"

python3 android/gen_icon.py "$BUILD/icon.png"
python3 android/apkpack.py --dex "$BUILD/classes.dex" --assets dist --icon "$BUILD/icon.png" --out "$BUILD/unsigned.apk"

# Clave de depuración local (no se sube al repo). Si la borras, desinstala la app antes de reinstalar.
KS=android/debug.p12
if [ ! -f "$KS" ]; then
  keytool -genkeypair -keystore "$KS" -storetype PKCS12 -storepass android -keypass android \
    -alias huron -keyalg RSA -keysize 2048 -validity 10000 -dname "CN=Huron Ladron Debug,O=Huron Ladron,C=ES" 2>/dev/null
fi
javac -nowarn -cp "$CACHE/apksig.jar" -d "$BUILD/signer" android/signer/SignApk.java
java --add-exports java.base/sun.security.x509=ALL-UNNAMED --add-exports java.base/sun.security.pkcs=ALL-UNNAMED --add-exports java.base/sun.security.util=ALL-UNNAMED \
  -cp "$CACHE/apksig.jar:$BUILD/signer" SignApk "$BUILD/unsigned.apk" "$OUT" "$KS" huron android 24

python3 android/check_apk.py "$OUT"
ls -la "$OUT"
