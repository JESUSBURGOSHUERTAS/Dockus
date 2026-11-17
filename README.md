<div align="center">

<img src="assets/logo.png" width="180" alt="Dockus Logo" style="filter: drop-shadow(0 0 25px rgba(56, 189, 248, 0.45)); margin-bottom: 12px;" />

# ✦ Dockus — AI Quota & Token Monitor

[![Sitio Web Oficial](https://img.shields.io/badge/Sitio%20Web-GitHub%20Pages-38BDF8?style=for-the-badge&logo=googlechrome&logoColor=white)](https://jesusburgoshuertas.github.io/Dockus/)
[![Última Release](https://img.shields.io/github/v/release/JESUSBURGOSHUERTAS/Dockus?style=for-the-badge&color=0EA5E9)](https://github.com/JESUSBURGOSHUERTAS/Dockus/releases/latest)
![Platform](https://img.shields.io/badge/Windows-10%20%7C%2011%20(x64)-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![Linux Support](https://img.shields.io/badge/Linux-Próximamente-FCC624?style=for-the-badge&logo=linux&logoColor=black)
![Framework](https://img.shields.io/badge/.NET-10.0%20WPF-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Security](https://img.shields.io/badge/Security-Windows%20DPAPI%20Encrypted-10B981?style=for-the-badge)
![License](https://img.shields.io/badge/License-GPLv3-10B981?style=for-the-badge)

<p align="center">
  <b>Un monitor de escritorio flotante y translúcido que rastrea tus cuotas y consumo de tokens de IA en tiempo real, diseñado con estética inspirada en Windows 7 Aero Glass, micropartículas ambientales vivas, acople magnético a esquinas y selector multi-proveedor (Antigravity, DeepSeek, Claude, Gemini, OpenAI y Ollama).</b>
</p>

[🌐 Visitar Sitio Web](https://jesusburgoshuertas.github.io/Dockus/) · [⬇ Descargar v1.1.0](https://github.com/JESUSBURGOSHUERTAS/Dockus/releases/latest) · [🐞 Reportar un Error](https://github.com/JESUSBURGOSHUERTAS/Dockus/issues)

</div>

---

## 🌟 ¿Qué es Dockus?

**Dockus** es una barra de monitoreo inteligente y compacta concebida para **cualquier persona que use Inteligencia Artificial en su día a día**: desarrolladores, creadores de contenido, investigadores, redactores o entusiastas de la IA. Mientras trabajas en tu editor, interactúas con modelos de lenguaje o ejecutas tareas complejas, Dockus permanece discretamente acoplado sobre tu barra de tareas mostrándote con precisión matemática cuánto límite de uso te queda y exactamente cuántas horas y minutos faltan para que tu cuota se reinicie al 100%.

Todo esto integrado en un diseño de cristal físico **Aero Glass** de última generación, sin estorbar tu flujo de trabajo, con cifrado local seguro mediante **Windows DPAPI**, cero telemetría externa y 100% de ejecución local en tu máquina.

---

## 🤖 Plataformas de IA Compatibles

Dockus incluye un selector rápido interactivo en la barra (`✦ AGY ▾`, `🐋 DSK ▾`, `✻ CLD ▾`, etc.) que te permite alternar al instante entre diferentes proveedores y modelos:

| Plataforma / Motor | Tipo de Conexión | ¿Requiere API Key? | Qué Monitorea |
| :--- | :--- | :---: | :--- |
| **✦ Google Antigravity** | **Local Connect-RPC** | ❌ No | Cuotas oficiales de Gemini Pro/Flash (semanal y 5h), Claude Sonnet/Opus y Gemini 4 Argon directamente del proceso IDE activo. |
| **🐋 DeepSeek Platform** | **HTTPS REST API** |  Sí | Saldo de crédito restante en USD, disponibilidad de modelos DeepSeek-V3 / DeepSeek-R1 y límites de ventana. |
| **✻ Anthropic Claude** | **Sesión de claude.ai** (WebView2) o statusline de Claude Code | ❌ No (API key opcional) | Límites del plan Pro / Max / Team: % libre de 5 horas, semanal y por modelo, con cuenta regresiva y anillo junto al reloj. Con API key solo verifica el acceso. |
| **✦ Google AI Studio** | **HTTPS REST API** |  Sí | Modelos Gemini 1.5, 2.0 Flash/Pro y experimentales con ventana de hasta 1 millón de tokens. |
| **⚙ OpenAI API** | **HTTPS REST API** |  Sí | Modelos GPT-4o, o1, o3-mini y ventana de contexto de 128,000 tokens. |
| **🦙 Ollama Local** | **HTTP Local (11434)** | ❌ No | Modelos locales descargados en tu máquina (Llama 3, DeepSeek, Mistral) vía `localhost:11434`. |

---

## ✻ Límites del Plan de Claude sin API Key

Una API key de Anthropic **no** muestra los límites de un plan Pro, Max o Team: la API se factura aparte. Dockus los lee de dos formas, sin claves.

### Opción 1 (recomendada): cuenta de claude.ai

Funciona con la app de escritorio de Claude, la web o la terminal.

1. Abre Dockus → pestaña **`🔌 Claves`** → tarjeta **Anthropic Claude** → **Iniciar sesión en claude.ai**.
2. Inicia sesión en la ventana que se abre, como siempre. Se cierra sola cuando la sesión queda activa.
3. Elige **`✻ CLD`** en la barra. Dockus consulta cada 3 minutos el mismo uso de claude.ai → Configuración → Uso: 5 horas, semanal y por modelo.

**Anillo junto al reloj:** Dockus pone en la bandeja de la barra de tareas un anillo que se llena con el **% usado** de la ventana elegida en la barra de Dockus (**⏱ 5h** o **📅 7d**). Colores: azul por debajo de 50 %, naranja de 50 a 79 % y bermellón desde 80 %, que además muestra un punto en el centro para no depender solo del color. Al pasar el mouse muestra ambas ventanas y cuándo se reinician. Clic izquierdo abre Dockus; clic derecho, el menú. El botón **—** de Dockus lo oculta y deja solo el anillo. En Windows 11, los íconos nuevos quedan ocultos en la flecha **^**: para verlo siempre, ve a **Configuración → Personalización → Barra de tareas → Otros íconos de la bandeja** y activa **Dockus**. Se puede apagar con la casilla *Mostrar anillo de uso junto al reloj*.

**Qué tener en cuenta:**

- La sesión vive solo en tu PC, en `%LOCALAPPDATA%\Dockus\ClaudeWebSession` (perfil de WebView2). Para que no se pierda al reiniciar, Dockus guarda además una copia de las cookies de claude.ai en `%LOCALAPPDATA%\Dockus\claude-session.dat`, cifrada con DPAPI: solo tu cuenta de Windows en este PC puede leerla. Dockus nunca ve tu contraseña y las consultas van directo a claude.ai. **Cerrar sesión** borra el perfil y la copia.
- El registro `%APPDATA%\Dockus\claude-web.log` anota estados HTTP y conteos para diagnosticar, nunca valores de cookies.
- Usa los servicios internos de claude.ai, que Anthropic no documenta para apps externas. Si cambian, Dockus muestra *formato no reconocido* y guarda la respuesta (solo porcentajes y fechas) en `%APPDATA%\Dockus\claude-web-usage-debug.json` para ajustarlo.
- Que una app externa use tu sesión de claude.ai es una zona gris de los términos de uso. En cuentas de empresa (Team/Enterprise), consúltalo con el administrador.

### Opción 2: statusline de Claude Code (terminal)

La forma documentada es la [statusline de Claude Code](https://code.claude.com/docs/en/statusline), que recibe los campos `rate_limits.five_hour` y `rate_limits.seven_day` (porcentaje usado y hora de reinicio). Solo se ejecuta en la terminal, no en la app de escritorio.

**Cómo activarlo:**

1. Abre Dockus → pestaña **`🔌 Claves`** → tarjeta **Anthropic Claude** → **Conectar Claude Code**.
2. Dockus copia [`scripts/dockus-statusline.ps1`](scripts/dockus-statusline.ps1) a `%APPDATA%\Dockus\` y agrega `statusLine` a `%USERPROFILE%\.claude\settings.json` (deja una copia en `settings.json.dockus-backup`).
3. Usa Claude Code en la terminal (`claude`). Tras la primera respuesta, el script guarda los límites en `%APPDATA%\Dockus\claude-usage.json` y Dockus los muestra al elegir **`✻ CLD`**.

**Qué tener en cuenta:**

- Los datos solo se actualizan mientras Claude Code está en uso en la terminal. Dockus muestra hace cuánto se tomaron (medidor **ESTADO**, en ámbar si tienen más de 1 hora). Si una ventana ya se reinició, se muestra al 100 %.
- El archivo solo guarda porcentajes y horas de reinicio: nada de credenciales, rutas ni conversaciones.
- Si ya tienes otra `statusLine`, Dockus no la reemplaza. Puedes llamar a `dockus-statusline.ps1` desde tu propio script pasándole el mismo JSON por stdin.
- Para quitarlo: botón **Desconectar** (solo borra la `statusLine` si es la de Dockus). Al guardar, Dockus reescribe `settings.json` sin los comentarios que tuviera; el original queda en el respaldo.

---

## 🔐 ¿Cómo es la Seguridad al Guardar las Claves API?

Dockus aplica las mejores prácticas de seguridad de la industria para garantizar que tus credenciales nunca corran riesgo:

1. **Cifrado Físico en Reposo con Windows DPAPI (Data Protection API):**
   - Cuando guardas una clave API (DeepSeek, Claude, Gemini, OpenAI), Dockus **no la almacena en texto plano**.
   - Se cifra utilizando `ProtectedData.Protect` de Windows con el alcance `DataProtectionScope.CurrentUser`.
   - **¿Qué significa esto?** La clave queda cifrada con una clave criptográfica vinculada a tu propia cuenta de usuario de Windows. Ningún otro usuario en el mismo PC, ni nadie que copie el archivo a otra máquina, puede descifrarla.
2. **Almacenamiento Aislado Fuera del Repositorio:**
   - La configuración se almacena en `%APPDATA%\Dockus\settings.json`, completamente fuera del directorio del proyecto o de Git. Nunca se subirá ni compartirá accidentalmente en GitHub.
3. **Cero Telemetría y Cero Servidores Intermedios:**
   - Dockus **no tiene servidores externos propios**. Cada petición de verificación o saldo viaja de manera directa y cifrada por HTTPS desde tu equipo hacia el endpoint oficial de cada proveedor (Google, Anthropic, DeepSeek, OpenAI) o por localhost.
4. **Modo IDE 100% Sin Claves:**
   - Si utilizas Dockus con Google Antigravity, ni siquiera necesitas ingresar ninguna clave API: la comunicación ocurre por socket local entre procesos en tu propia máquina.

---

## ⚡ Características Principales

- **✦ Selector Rápido de IA en la Barra (`✦ AGY ▾`, `🐋 DSK ▾`, etc.)**:
  - Haz clic en el distintivo de la esquina para desplegar el menú de motores disponibles y cambiar en 1 clic qué IA rastrear.
- **⏱ Selector Rápido de Período (`[ ⏱ 5h ]` / `[ 📅 7d ]`)**:
  - Alterna al instante la vista de los medidores entre la ventana móvil corta de 5 horas y la cuota semanal completa de 7 días.
- **⏳ Cuenta Regresiva Exacta de Reinicio**:
  - Muestra el tiempo restante real para el restablecimiento de cada modelo (ej. `3h 28m`, `4h 55m`, `6d 18h` o `100% Libre`), calculando dinámicamente cada segundo.
- **🔮 Estética Aero Glass Moderna con Micropartículas**:
  - Fondo translúcido con desenfoque de cristal físico **DWM Acrylic BlurBehind** nativo de Windows.
  - Brillo especular biselado en el borde superior idéntico al vidrio de Windows 7 Aero.
  - Lienzo de **micropartículas de luz viva** (cian, púrpura, menta) que flotan suavemente en el interior del cristal con un consumo de CPU ultra bajo (<0.5%).
- **🧲 Acople Magnético a Esquinas y Barra de Tareas**:
  - Anclaje automático al ras de la esquina inferior derecha o izquierda de la pantalla, sin sombras cuadradas ni márgenes invisibles.
- **📊 Pestañas de Navegación Fluidas sin Solapamiento**:
  - Pestañas compactas luminosas: **`✦ Cuotas`**, **`✦ Tokens`** y **`🔌 Claves`**, con botón de sincronización `↻` dedicado.

---

## 🖥️ Compatibilidad de Sistemas Operativos

- **Windows 10 & 11 (x64):** ✅ **Disponible ahora** (Versión 1.1.0 estable con instalador portable).
- **🐧 Linux (Ubuntu, Fedora, Arch, Debian):** 🚀 **Próximamente** (en desarrollo activo con Avalonia UI y .NET nativo para soportar entornos GNOME, KDE y Wayland/X11).

---

## 🚀 Instalación Rápida (Sin Compilar)

1. Descarga el archivo comprimido desde la sección de [Releases de GitHub](https://github.com/JESUSBURGOSHUERTAS/Dockus/releases/latest):
   - **`Dockus-v1.1.0-win-x64.zip`**
2. Descomprime la carpeta en la ubicación que prefieras (ej. `C:\Dockus` o en tus Documentos).
3. Haz doble clic en **`Iniciar-Dockus.bat`** (o directamente en `Dockus.exe`).

---

## 🛠️ Compilación para Desarrolladores

Si deseas compilar el código fuente por ti mismo:

```powershell
# 1. Clonar el repositorio
git clone https://github.com/JESUSBURGOSHUERTAS/Dockus.git
cd Dockus

# 2. Compilar con dotnet
dotnet build src/Dockus.csproj -c Release

# 3. Publicar la versión ejecutable independiente
dotnet publish src/Dockus.csproj -c Release -o publish

# 4. Ejecutar
.\publish\Dockus.exe
```

---

## 📜 Licencia

Este proyecto está publicado bajo la licencia **GNU General Public License v3.0 (GPLv3)**.

> [!IMPORTANT]
> **¿Por qué GPLv3?**  
> Esta licencia garantiza que Dockus es y será siempre software libre y de uso público. Cualquier persona puede usarlo, estudiarlo, modificarlo y compartirlo. Sin embargo, impone **Copyleft estricto**: ninguna persona o empresa puede tomar este código o crear una obra derivada para convertirla en software privado o comercial de código cerrado. Todo software basado en Dockus debe permanecer 100% de código abierto bajo esta misma licencia.

---

<div align="center">
  <b>Hecho con dedicación por <a href="https://github.com/JESUSBURGOSHUERTAS">Jesus Burgos</a></b><br>
  <sub>Dockus es un proyecto independiente de código abierto y no está afiliado oficialmente a Google ni Anthropic.</sub>
</div>
