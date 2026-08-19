#!/usr/bin/env bash
# ============================================================
# iniciar_emulador.sh - Arranca el emulador MiCachitoAVD
# Espera a que Android termine de bootear
# ============================================================
set -euo pipefail

# --- Verificar ANDROID_HOME ---
if [ -z "${ANDROID_HOME:-}" ]; then
    if [ -d "$HOME/Android/Sdk" ]; then
        export ANDROID_HOME="$HOME/Android/Sdk"
    elif [ -d "$HOME/.android-sdk" ]; then
        export ANDROID_HOME="$HOME/.android-sdk"
    else
        echo "ERROR: ANDROID_HOME no está configurado."
        exit 1
    fi
fi

export PATH="$ANDROID_HOME/platform-tools:$ANDROID_HOME/emulator:$PATH"

AVD_NAME="MiCachitoAVD"
ADB="$ANDROID_HOME/platform-tools/adb"
EMULATOR="$ANDROID_HOME/emulator/emulator"

# --- Verificar que el AVD existe ---
if ! "$EMULATOR" -list-avds 2>/dev/null | grep -q "$AVD_NAME"; then
    echo "ERROR: AVD '$AVD_NAME' no encontrado."
    echo "Ejecuta primero: ./crear_avd.sh"
    exit 1
fi

# --- Verificar que no haya ya un emulador corriendo ---
if "$ADB" devices 2>/dev/null | grep -q "emulator-"; then
    echo "Ya hay un emulador corriendo:"
    "$ADB" devices
    echo ""
    echo "Para detenerlo: adb -s emulator-5554 emu kill"
    echo "Para usarlo, continúa con: dotnet build -f net10.0-android -t:Run"
    exit 0
fi

# --- Iniciar emulador ---
echo "Iniciando emulador '$AVD_NAME'..."
echo "(Esto puede tardar 30-90 segundos la primera vez)"
echo ""

# Detectar DISPLAY para mostrar ventana gráfica
DISPLAY_OPT=""
if [ -n "${DISPLAY:-}" ]; then
    DISPLAY_OPT=""
else
    echo "AVISO: DISPLAY no configurado. Iniciando en modo headless (sin ventana)."
    echo "       Si necesitas ventana gráfica: export DISPLAY=:0"
    DISPLAY_OPT="-no-window"
fi

"$EMULATOR" -avd "$AVD_NAME" \
    -no-snapshot \
    -no-audio \
    -gpu swiftshader_indirect \
    $DISPLAY_OPT \
    -netdelay none -netspeed full &

EMULATOR_PID=$!
echo "Emulador iniciado (PID: $EMULATOR_PID)"
echo ""

# --- Esperar a que el dispositivo conecte ---
echo "Esperando que ADB detecte el emulador..."
"$ADB" wait-for-device
echo "  Dispositivo conectado."

# --- Esperar a que el boot termine ---
echo "Esperando boot de Android..."
MAX_WAIT=120
WAITED=0
while [ "$WAITED" -lt "$MAX_WAIT" ]; do
    BOOT=$("$ADB" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')
    if [ "$BOOT" = "1" ]; then
        echo "  Boot completado (${WAITED}s)"
        break
    fi
    sleep 2
    WAITED=$((WAITED + 2))
    echo "  Esperando... ${WAITED}s"
done

if [ "$WAITED" -ge "$MAX_WAIT" ]; then
    echo "AVISO: El boot tardó más de ${MAX_WAIT}s."
    echo "       El emulador puede seguir iniciando en segundo plano."
fi

echo ""
echo "============================================"
echo "  Emulador listo."
echo "============================================"
echo ""
echo "Dispositivos detectados:"
"$ADB" devices
echo ""
echo "Para desplegar MiCachito Mobile:"
echo "  cd .. && dotnet build -c Debug -f net10.0-android -t:Run"
echo ""
echo "Backend: debe estar corriendo en el host, puerto 8080."
echo "  Opcion A (Docker)     :  cd ../MiCachito-backend && docker-compose up -d"
echo "  Opcion B (PHP local)  :  cd ../MiCachito-backend && php -S 0.0.0.0:8080 -t web"
echo "  Helper opcional        :  ./levantar_backend.sh"
echo ""
echo "  Verificar:             curl http://localhost:8080/api/health"
echo ""
echo "Credenciales de prueba:"
echo "  Usuario:    billetero"
echo "  Contraseña: billetero123"