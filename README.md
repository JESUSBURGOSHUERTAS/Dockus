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

[🌐 Visitar Sitio Web](https://jesusburgoshuertas.github.io/Dockus/) · [⬇ Descargar v1.0.0](https://github.com/JESUSBURGOSHUERTAS/Dockus/releases/latest) · [🐞 Reportar un Error](https://github.com/JESUSBURGOSHUERTAS/Dockus/issues)

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
| **✻ Anthropic Claude** | **HTTPS REST API** |  Sí | Disponibilidad de Claude 3.5 y 3.7 Sonnet / Opus y ventana de contexto de hasta 200,000 tokens. |
| **✦ Google AI Studio** | **HTTPS REST API** |  Sí | Modelos Gemini 1.5, 2.0 Flash/Pro y experimentales con ventana de hasta 1 millón de tokens. |
| **⚙ OpenAI API** | **HTTPS REST API** |  Sí | Modelos GPT-4o, o1, o3-mini y ventana de contexto de 128,000 tokens. |
| **🦙 Ollama Local** | **HTTP Local (11434)** | ❌ No | Modelos locales descargados en tu máquina (Llama 3, DeepSeek, Mistral) vía `localhost:11434`. |

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

- **Windows 10 & 11 (x64):** ✅ **Disponible ahora** (Versión 1.0.0 estable con instalador portable).
- **🐧 Linux (Ubuntu, Fedora, Arch, Debian):** 🚀 **Próximamente** (en desarrollo activo con Avalonia UI y .NET nativo para soportar entornos GNOME, KDE y Wayland/X11).

---

## 🚀 Instalación Rápida (Sin Compilar)

1. Descarga el archivo comprimido desde la sección de [Releases de GitHub](https://github.com/JESUSBURGOSHUERTAS/Dockus/releases/latest):
   - **`Dockus-v1.0.0-win-x64.zip`**
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
