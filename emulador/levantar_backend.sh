#!/usr/bin/env bash
# ============================================================
# levantar_backend.sh - Helper para levantar el backend de MiCachito
#
# Ofrece DOS opciones para que el backend quede disponible
# en el puerto 8080 del host (necesario para el emulador
# Android que apunta a http://10.0.2.2:8080/).
#
#   A) Docker      -> docker-compose up -d
#   B) PHP local   -> php -S 0.0.0.0:8080 -t web
#
# No obliga a usar una u otra. Solo facilita el arranque.
# Eliges la que tengas configurada en tu máquina.
# ============================================================
set -euo pipefail

# --- Localizar la carpeta del backend (sibling del Mobile) ---
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MOBILE_DIR="$(dirname "$SCRIPT_DIR")"

BACKEND_DIR=""
for candidate in "$MOBILE_DIR/../MiCachito-backend" \
                 "$MOBILE_DIR/MiCachito-backend" \
                 "$MOBILE_DIR/../backend"; do
    if [ -f "$candidate/web/index.php" ]; then
        BACKEND_DIR="$(cd "$candidate" && pwd)"
        break
    fi
done

# Directorios padres con docker-compose (pueden existir setups completos)
PARENT_COMPOSE=""
for parent in "$MOBILE_DIR/.." "$MOBILE_DIR"; do
    if [ -f "$parent/docker-compose.yml" ]; then
        PARENT_COMPOSE="$(cd "$parent" && pwd)/docker-compose.yml"
        break
    fi
done

echo "============================================"
echo "  Levantar el backend de MiCachito"
echo "============================================"
echo ""
echo "El backend debe responder en el puerto 8080 del host"
echo "(el emulador Android lo alcanza como http://10.0.2.2:8080/)."
echo ""
if [ -n "$BACKEND_DIR" ]; then
    echo "Carpeta del backend: $BACKEND_DIR"
else
    echo "AVISO: no se encontró MiCachito-backend junto a MiCachito-Mobile."
    echo "       Coloca el repositorio del backend al lado del Mobile."
fi
echo ""
echo "Selecciona una opción:"
echo "  1) Docker    (docker-compose up -d)"
echo "  2) PHP local  (php -S 0.0.0.0:8080 -t web)"
echo "  3) Salir"
echo ""
read -p "Opción [1-3]: " OPCION

case "$OPCION" in
    1)
        echo ""
        echo ">> Opción A: Docker"
        echo ""

        if ! command -v docker &> /dev/null; then
            echo "ERROR: docker no está instalado o no está en el PATH."
            echo "       Instálalo desde: https://docs.docker.com/get-docker/"
            exit 1
        fi
        if ! command -v docker-compose &> /dev/null \
           && ! docker compose version &> /dev/null 2>&1; then
            echo "ERROR: docker-compose no está disponible."
            echo "       Instálalo como plugin: 'docker compose' o por separado."
            exit 1
        fi

        # Preferir el compose del directorio padre (suele incluir MySQL),
        # si no, usar el que viene en el backend repo.
        COMPOSE_DIR=""
        if [ -n "$PARENT_COMPOSE" ] && [ -f "$PARENT_COMPOSE" ]; then
            COMPOSE_DIR="$(dirname "$PARENT_COMPOSE")"
        elif [ -n "$BACKEND_DIR" ] && [ -f "$BACKEND_DIR/docker-compose.yml" ]; then
            COMPOSE_DIR="$BACKEND_DIR"
        fi

        if [ -z "$COMPOSE_DIR" ]; then
            echo "ERROR: no se encontró docker-compose.yml."
            echo "       Verifica que el repositorio del backend esté presente."
            exit 1
        fi

        echo "Usando: $COMPOSE_DIR/docker-compose.yml"
        echo ""

        cd "$COMPOSE_DIR"
        if docker compose version &> /dev/null 2>&1; then
            docker compose up -d
        else
            docker-compose up -d
        fi

        echo ""
        echo "Contenedores activos:"
        docker ps --format 'table {{.Names}}\t{{.Ports}}' 2>/dev/null | grep -i 'micachito\|NAMES' || docker ps --format 'table {{.Names}}\t{{.Ports}}' 2>/dev/null

        # Avisar si el puerto expuesto no es 8080
        PORT_LINE=$(docker ps --format '{{.Ports}}' 2>/dev/null \
            | grep -oE '0\.0\.0\.0:[0-9]+->80' | head -1 || true)
        if [ -n "$PORT_LINE" ]; then
            HOST_PORT="${PORT_LINE%%:*}"   # 0.0.0.0
            PORT_NUMBER="$(echo "$PORT_LINE" | grep -oE ':[0-9]+->80' | tr -d ':' | sed 's/->80//')"
            if [ "$PORT_NUMBER" = "8080" ]; then
                echo ""
                echo "Backend disponible en http://localhost:8080/  ✓"
            else
                echo ""
                echo "AVISO: el contenedor está en el puerto $PORT_NUMBER, no en 8080."
                echo "       Opciones para que la app lo encuentre:"
                echo "         A1) Cambiar el port mapping a '8080:80' en docker-compose.yml"
                echo"             y reiniciar el contenedor."
                echo "         A2) Compilar la app con:"
                echo "             export API_BASE_URL=\"http://10.0.2.2:$PORT_NUMBER\""
                echo "             dotnet build -c Debug -f net10.0-android -t:Run"
            fi
        fi
        echo ""
        echo "Verificar: curl http://localhost:8080/api/health"
        ;;

    2)
        echo ""
        echo ">> Opción B: PHP local"
        echo ""

        if ! command -v php &> /dev/null; then
            echo "ERROR: php no está instalado o no está en el PATH."
            echo "       Instala PHP 7.4+ con las extensiones de MySQL."
            exit 1
        fi

        if [ -z "$BACKEND_DIR" ]; then
            echo "ERROR: no se encontró la carpeta del backend."
            echo "       Coloca MiCachito-backend al lado de MiCachito-Mobile."
            exit 1
        fi

        cd "$BACKEND_DIR"

        # Verificar dependencias de composer
        if [ ! -d "vendor" ]; then
            echo "Instalando dependencias (composer install)..."
            if command -v composer &> /dev/null; then
                composer install --no-interaction
            else
                echo "ERROR: composer no está instalado."
                echo "       Instálalo desde: https://getcomposer.org/"
                exit 1
            fi
        fi

        # Verificar config/db.php
        if [ ! -f "config/db.php" ]; then
            if [ -f "config/db.php.example" ]; then
                echo "AVISO: config/db.php no existe."
                echo "       Copia config/db.php.example a config/db.php y ajusta"
                echo "       host, usuario, password y nombre de la base de datos."
                echo ""
                echo "       cp config/db.php.example config/db.php"
                echo "       # Luego edita con tus credenciales locales."
                exit 1
            else
                echo "ERROR: no se encontró config/db.php ni config/db.php.example."
                exit 1
            fi
        fi

        echo "Backend serving on http://0.0.0.0:8080 ..."
        echo "(Mantén esta terminal abierta. Ctrl+C para detener.)"
        echo ""
        php -S 0.0.0.0:8080 -t web
        ;;

    3)
        echo "Saliendo."
        exit 0
        ;;

    *)
        echo "ERROR: opción inválida. Debe ser 1, 2 o 3."
        exit 1
        ;;
esac