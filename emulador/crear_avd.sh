#!/usr/bin/env bash
# ============================================================
# crear_avd.sh - Crea el AVD MiCachitoAVD para MiCachito Mobile
# Compatible con cualquier máquina Linux/macOS con Android SDK
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
        echo "Instala Android SDK command-line tools desde:"
        echo "  https://developer.android.com/studio#command-line-tools-only"
        echo "Luego ejecuta:"
        echo "  export ANDROID_HOME=\$HOME/Android/Sdk"
        exit 1
    fi
fi

export PATH="$ANDROID_HOME/cmdline-tools/latest/bin:$ANDROID_HOME/platform-tools:$ANDROID_HOME/emulator:$PATH"

AVD_NAME="MiCachitoAVD"
SYSTEM_IMAGE="system-images;android-35;google_apis;x86_64"
DEVICE_PROFILE="pixel_6"

echo "============================================"
echo "  Creación de AVD: $AVD_NAME"
echo "============================================"
echo ""
echo "ANDROID_HOME: $ANDROID_HOME"
echo "System image: $SYSTEM_IMAGE"
echo "Device:       $DEVICE_PROFILE"
echo ""

# --- Verificar sdkmanager ---
if ! command -v sdkmanager &> /dev/null; then
    echo "ERROR: sdkmanager no encontrado en PATH."
    echo "Instala cmdline-tools:"
    echo "  sdkmanager \"cmdline-tools;latest\""
    exit 1
fi

# --- Instalar componentes necesarios ---
echo "[1/4] Instalando platform-tools, emulator y system image..."
echo "y" | sdkmanager "platform-tools" "emulator" "$SYSTEM_IMAGE" "platforms;android-35" 2>/dev/null || {
    echo "  (Algunos componentes ya estaban instalados)"
}
echo "  OK"
echo ""

# --- Aceptar licencias ---
echo "[2/4] Aceptando licencias..."
yes | sdkmanager --licenses &> /dev/null || true
echo "  OK"
echo ""

# --- Verificar avdmanager ---
if ! command -v avdmanager &> /dev/null; then
    echo "ERROR: avdmanager no encontrado."
    echo "Instala cmdline-tools: sdkmanager \"cmdline-tools;latest\""
    exit 1
fi

# --- Crear AVD ---
echo "[3/4] Creando AVD '$AVD_NAME'..."
echo "no" | avdmanager create avd \
    -n "$AVD_NAME" \
    -k "$SYSTEM_IMAGE" \
    -d "$DEVICE_PROFILE" \
    --force
echo "  OK"
echo ""

# --- Verificar ---
echo "[4/4] Verificando..."
echo ""
emulator -list-avds | grep "$AVD_NAME" && {
    echo ""
    echo "============================================"
    echo "  AVD '$AVD_NAME' creado correctamente."
    echo "============================================"
    echo ""
    echo "Para iniciar el emulador:"
    echo "  ./iniciar_emulador.sh"
    echo ""
    echo "O manualmente:"
    echo "  emulator -avd $AVD_NAME -no-snapshot -no-audio -gpu swiftshader_indirect"
} || {
    echo "ERROR: El AVD no se creó correctamente."
    exit 1
}