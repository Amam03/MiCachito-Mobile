============================================================
  MiCachito Mobile - Configuración del Emulador Android
  Guía reproducible para cualquier desarrollador
============================================================

Esta carpeta contiene todo lo necesario para crear y configurar
un emulador Android donde ejecutar MiCachito Mobile.

El repositorio NO incluye la imagen del emulador ni el AVD
(combinan varios GB y son específicos de cada máquina).
En su lugar incluye scripts y esta guía para que cada
desarrollador cree su propio emulador en minutos.

------------------------------------------------------------
REQUISITOS PREVIOS
------------------------------------------------------------

Para ejecutar MiCachito Mobile necesitas:

1. .NET 10 SDK (rc.2 o superior)
   Verificar:  dotnet --version
   Descargar:   https://dotnet.microsoft.com/download

2. JDK 17
   Verificar:  java -version
   Descargar:  https://adoptium.net/

3. Android SDK (command-line tools)
   Incluye: sdkmanager, avdmanager, adb, emulator
   Descargar: https://developer.android.com/studio#command-line-tools-only

4. Backend disponible en el puerto 8080 de tu computadora.
   Puedes levantarlo de DOS formas (elige UNA):

   A) Docker
      - Necesitas Docker instalado y docker-compose
      - Necesitas el repositorio MiCachito-backend al lado del Mobile

   B) PHP local
      - Necesitas PHP 7.4+ instalado en tu computadora
      - Necesitas MySQL accesible desde tu computadora
      - Necesitas el repositorio MiCachito-backend al lado del Mobile

   Completa los requisitos de la opción que vayas a usar (ver PASO 1).

5. KVM / aceleración de hardware (Linux)
   Verificar:  ls -la /dev/kvm
   Si no existe, habilitar virtualización en BIOS y ejecutar:
     sudo apt install qemu-kvm
     sudo usermod -aG kvm $USER
   (Cerrar sesión y volver a entrar para que surta efecto)


============================================================
  PASO 1: Levantar el backend (elige A o B, NO ambas)
============================================================

La app móvil necesita que el backend responda en el puerto 8080
del host donde corre el emulador. Hay dos formas de hacerlo.

------------------------------------------------------------
OPCIÓN A — Backend con Docker
------------------------------------------------------------

Esta opción usa Docker y docker-compose para levantar el backend.
Mantiene todo aislado en contenedores (no necesitas instalar PHP
ni MySQL localmente, los proveen las imágenes Docker).

  1. Entra en la carpeta del backend:
       cd MiCachito-backend

  2. Verifica que existe docker-compose.yml en la carpeta.
     El repositorio incluye uno con la configuración base.

  3. Levanta los servicios:
       docker-compose up -d

  4. Verifica que el backend responde:
       curl http://localhost:8080/api/health

NOTA SOBRE EL PUERTO:
  El docker-compose.yml incluido en el repositorio expone el
  backend en el puerto 8000 (8000:80). Para que la app móvil
  funcione con la URL por defecto (10.0.2.2:8080) tienes dos
  alternativas:

   A1) Cambiar el puerto expuesto a 8080 en docker-compose.yml:
       ports:
         - "8080:80"
       (luego docker-compose up -d)

   A2) Dejar el puerto 8000 y usar la variable de entorno al
       compilar la app:
       export API_BASE_URL="http://10.0.2.2:8000/"
       dotnet build -c Debug -f net10.0-android -t:Run

  !  No elimines la configuración Docker existente.
     Solo ajústala si necesitas otro puerto.

Si tu entorno ya tiene una configuración Docker más completa
(por ejemplo un docker-compose.yml con MySQL incluido) en una
carpeta superior, puedes usar ese procedimiento en lugar del
anterior. Lo importante es que el backend quede disponible en
el puerto 8080 del host.

------------------------------------------------------------
OPCIÓN B — Backend local con PHP (sin Docker)
------------------------------------------------------------

Esta opción levanta el backend directamente con el servidor
integrado de PHP. Útil cuando ya tienes PHP y MySQL instalados.

  1. Entra en la carpeta del backend:
       cd MiCachito-backend

  2. Asegúrate de tener las dependencias instaladas:
       composer install

  3. Configura config/db.php para que apunte a tu MySQL local.
     Toma como base config/db.php.example:
       cp config/db.php.example config/db.php
       # Edita db.php y ajusta host, usuario, password y base de datos

  4. Inicia el servidor PHP en el puerto 8080:
       php -S 0.0.0.0:8080 -t web

  5. MANTIENE la terminal abierta mientras uses la app.
     El servidor integrado de PHP corre en primer plano;
     si la cierras, el backend se detiene.

  6. Verifica que el backend responde (en otra terminal):
       curl http://localhost:8080/api/health

------------------------------------------------------------
(ambas opciones continúan igual a partir de aquí)
------------------------------------------------------------


============================================================
  PASO 2: Aplicar migraciones (primera vez)
============================================================

Las migraciones crean las tablas y datos semilla en la base
de datos del backend, incluyendo el usuario de prueba.

  - Si usaste la OPCIÓN A (Docker):
      docker exec micachito-php php /app/yii migrate/up --interactive=0
      docker exec micachito-php php /app/yii migrate/up \
        --migrationPath=@app/migrations --interactive=0

  - Si usaste la OPCIÓN B (PHP local):
      cd MiCachito-backend
      php yii migrate/up --interactive=0
      php yii migrate/up --migrationPath=@app/migrations --interactive=0

Verifica que el usuario de prueba exista:
  mysql -u <tu_usuario> -p MiCachito \
    -e "SELECT id_usuario, username FROM usuarios WHERE username='billetero';"

Si no existe, las migraciones no se aplicaron correctamente.
Revisa los mensajes de error del comando migrate/up.


============================================================
  PASO 3: Instalar Android SDK y componentes
============================================================

Si aún no tienes Android SDK instalado, descarga las
"command-line tools" desde el enlace en REQUISITOS PREVIOS y
extrae en:

  ~/Android/Sdk/

Luego ejecuta:

  export ANDROID_HOME=~/Android/Sdk
  export PATH="$ANDROID_HOME/cmdline-tools/latest/bin:$ANDROID_HOME/platform-tools:$ANDROID_HOME/emulator:$PATH"

Instala los componentes necesarios:

  sdkmanager "platform-tools" "emulator" "system-images;android-35;google_apis;x86_64" "platforms;android-35"

Acepta las licencias:

  yes | sdkmanager --licenses

Verifica la instalación:

  sdkmanager --list_installed
  emulator -list-avds
  adb version


============================================================
  PASO 4: Crear el AVD (emulador)
============================================================

Usa el script incluido:

  cd emulador
  chmod +x crear_avd.sh
  ./crear_avd.sh

Esto crea un AVD llamado "MiCachitoAVD" con:
  - System image: android-35 (Android 15) google_apis x86_64
  - Device profile: Pixel 6
  - GPU: swiftshader_indirect (compatible con cualquier máquina)

Para hacerlo manualmente:

  avdmanager create avd \
    -n MiCachitoAVD \
    -k "system-images;android-35;google_apis;x86_64" \
    -d pixel_6 \
    --force


============================================================
  PASO 5: Iniciar el emulador
============================================================

Usa el script incluido:

  cd emulador
  chmod +x iniciar_emulador.sh
  ./iniciar_emulador.sh

Esto arranca el emulador y espera a que Android termine de bootear
(puede tardar 30-90 segundos la primera vez).

Para iniciarlo manualmente:

  emulator -avd MiCachitoAVD -no-snapshot -no-audio -gpu swiftshader_indirect

Verificar que el emulador está corriendo:

  adb devices
  # Debe mostrar: emulator-5554  device

Verificar que el boot terminó:

  adb shell getprop sys.boot_completed
  # Debe mostrar: 1


============================================================
  PASO 6: Compilar y ejecutar MiCachito Mobile
============================================================

Desde la raíz del proyecto (un nivel arriba de emulador/):

  dotnet build -c Debug -f net10.0-android -t:Run

Esto compila la app y la despliega automáticamente al emulador
que esté corriendo. La app se abre en la pantalla de Login.

Para compilar sin desplegar:

  dotnet build -c Debug -f net10.0-android

Si tienes múltiples dispositivos/emuladores, especifica cuál:

  dotnet build -c Debug -f net10.0-android -t:Run -p:AndroidDevice=emulator-5554


============================================================
  PASO 7: Verificar conectividad y probar el Login
============================================================

Verifica que el emulador alcance el backend:

  adb shell ping -c 3 10.0.2.2
  # Debe responder con 0% packet loss

IMPORTANTE SOBRE 10.0.2.2:
  En el emulador de Android, "localhost" se refiere al propio
  emulador, NO a tu computadora. Para alcanzar el localhost de
  tu computadora (donde corre el backend) se usa la IP especial
  10.0.2.2, que equivale a 127.0.0.1 de la máquina host.
  Esto funciona en cualquier computadora sin cambiar el código.

Credenciales de prueba:
  Usuario:    billetero
  Contraseña: billetero123

El usuario "billetero" se crea con la migración:
  m260803_000003_seed_usuario_billetero.php

Si las credenciales no funcionan, verificar que las migraciones
se aplicaron correctamente (ver PASO 2).


============================================================
  CONFIGURACIÓN DE LA URL DEL BACKEND
============================================================

La app se comunica con el backend por HTTP. La URL base se
configura en UN solo lugar:

  Helpers/AppSettings.cs  ->  propiedad BaseUrl

Por defecto:
  http://10.0.2.2:8080/

Este valor apunta al backend local levantado con Docker (OPCIÓN A)
o con PHP local (OPCIÓN B), siempre que esté disponible en el
puerto 8080 del host.

Para cambiar la URL del backend sin tocar Views ni ViewModels:

  Opción A (variable de entorno, sin tocar código):
    export API_BASE_URL="http://servidor.remoto:8080/"
    dotnet build -c Debug -f net10.0-android -t:Run

  Opción B (editar AppSettings.cs):
    Helpers/AppSettings.cs, línea con BaseUrl:
    public static readonly string BaseUrl = "http://servidor.remoto:8080/";

Casos comunes de configuración:
  - Emulador + backend local .............. http://10.0.2.2:8080/   (default)
  - Backend en Docker puerto 8000 ........ http://10.0.2.2:8000/
  - Dispositivo físico + backend en LAN ... http://<IP-LAN>:8080/
  - Windows (depuración local) ............ http://localhost:8080/
  - Backend remoto (QA/Producción) ......... http://<servidor>:8080/

La arquitectura MVVM se mantiene intacta: la configuración vive
en Helpers/AppSettings.cs y NO requiere modificar Views ni
ViewModels al cambiar la URL del backend.


============================================================
  ARCHIVOS DE ESTA CARPETA
============================================================

  README.txt              Esta guía
  crear_avd.sh            Script para crear el AVD MiCachitoAVD
  iniciar_emulador.sh     Script para arrancar el emulador y esperar el boot
  levantar_backend.sh     Helper opcional: levanta el backend con Docker o PHP local


============================================================
  SOLUCIÓN DE PROBLEMAS COMUNES
============================================================

1. "No se pudo conectar con el servidor. Intente más tarde."
   Causa: La app no puede alcanzar el backend.
   Solución:
     a) Verificar que el backend esté corriendo:
        curl http://localhost:8080/api/health
        (O curl http://localhost:8000/api/health si usaste Docker
         con el puerto por defecto del docker-compose.yml)
     b) Verificar que el emulador alcance el host:
        adb shell ping -c 3 10.0.2.2
     c) Verificar que BaseUrl en AppSettings.cs apunte al puerto
        correcto (10.0.2.2:8080 por defecto, o 10.0.2.2:8000 si
        dejaste el puerto Docker por defecto).
     d) Verificar que el puerto 8080 (o el que uses) esté abierto
        y no bloqueado por firewall.

2. "adb: command not found"
   Causa: Android SDK no está en el PATH.
   Solución:
     export ANDROID_HOME=~/Android/Sdk
     export PATH="$ANDROID_HOME/platform-tools:$ANDROID_HOME/emulator:$PATH"
   Añadir estas líneas a ~/.bashrc o ~/.zshrc para que persistan.

3. "emulator: ERROR: x86_64 emulation currently requires hardware acceleration"
   Causa: KVM no está disponible o no tienes permisos.
   Solución (Linux):
     sudo apt install qemu-kvm
     sudo usermod -aG kvm $USER
     # Cerrar sesión y volver a entrar
   Alternativa: usar -gpu swiftshader_indirect (renderizado por software)

4. El emulador arranca pero la app no se instala
   Causa: Puede haber un AVD corrupto o sin espacio.
   Solución:
     adb devices   # verificar que aparece "device" (no "offline")
     # Si aparece offline:
     adb kill-server
     adb start-server
     # Reintentar

5. Build error: "NETSDK1057: versión preliminar de .NET"
   Esto es solo un warning, no un error. El build continúa normalmente.

6. La app hace crash al abrirse
   Causa: Posiblemente se instaló con adb install en lugar de dotnet build -t:Run.
   Solución: Siempre usar "dotnet build -t:Run" para desplegar, no "adb install".
   El flag -t:Run hace el deploy completo con fast deployment de .NET MAUI.

7. "Permission denied" al ejecutar los scripts .sh
   Solución:
     chmod +x emulador/crear_avd.sh
     chmod +x emulador/iniciar_emulador.sh
     chmod +x emulador/levantar_backend.sh

8. El emulador está muy lento
   Soluciones:
     a) Verificar KVM: ls -la /dev/kvm (debe existir)
     b) Cerrar otros emuladores o máquinas virtuales
     c) Conceder más RAM al AVD:
        avdmanager create avd -n MiCachitoAVD -k "system-images;android-35;google_apis;x86_64" -d pixel_6 --force
        # Editar ~/.android/avd/MiCachitoAVD.avd/config.ini:
        # vm.heapSize=512
        # hw.ramSize=2048

9. "avdmanager: command not found"
   Causa: cmdline-tools no instalado.
   Solución:
     sdkmanager "cmdline-tools;latest"
     # O descargar desde:
     # https://developer.android.com/studio#command-line-tools-only

10. Migraciones del backend fallan o el usuario billetero no existe
    Causa: Faltan migraciones de la rama dev-mobile.
    Solución:
      cd MiCachito-backend
      git checkout dev-mobile

      - Docker:
        docker exec micachito-php php /app/yii migrate/up --migrationPath=@app/migrations --interactive=0
      - PHP local:
        php yii migrate/up --migrationPath=@app/migrations --interactive=0

      Verificar:
        mysql -u micachito_user -p MiCachito \
          -e "SELECT id_usuario, username FROM usuarios WHERE username='billetero';"

11. Backend con PHP local no responde
    Causa posible: el puerto 8080 ya está en uso.
    Solución:
      Cambia el puerto en el comando php -S y en AppSettings.cs (o API_BASE_URL):
        php -S 0.0.0.0:9000 -t web
        export API_BASE_URL="http://10.0.2.2:9000/"
    Causa posible: MySQL no responde a localhost.
    Solución: Revisa config/db.php (host=localhost, credenciales correctas)
    y que MySQL esté corriendo.

12. Backend con Docker en puerto 8000 pero la app espera 8080
    Solución:
      A1) Modifica el puerto en docker-compose.yml (ports: "8080:80") y reinicia.
      A2) O compila la app con la variable de entorno:
          export API_BASE_URL="http://10.0.2.2:8000/"
          dotnet build -c Debug -f net10.0-android -t:Run


============================================================
  NOTAS
============================================================

- El AVD creado ocupa ~10-12 GB de espacio en ~/.android/avd/
- La system image android-35 (Google APIs) no incluye Google Play Services
  por lo que no requiere cuenta de Google para iniciarse
- Si necesitas Google Play, cambia la system image a:
    system-images;android-35;google_apis_playstore;x86_64
- El emulador funciona en modo headless (sin ventana) si tu máquina
  no tiene servidor gráfico:
    emulator -avd MiCachitoAVD -no-window -no-audio -gpu swiftshader_indirect

- La configuración de la app NO depende de la computadora donde se
  creó el repositorio: 10.0.2.2 es el alias universal del emulador
  de Android y funciona en cualquier máquina sin cambios.