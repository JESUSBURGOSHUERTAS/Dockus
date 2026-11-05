<div align="center">

<img src="assets/logo.png" width="180" alt="Dockus Logo" style="filter: drop-shadow(0 0 25px rgba(56, 189, 248, 0.45)); margin-bottom: 12px;" />

# ✦ Dockus — AI Quota & Token Monitor for Windows

[![Sitio Web Oficial](https://img.shields.io/badge/Sitio%20Web-GitHub%20Pages-38BDF8?style=for-the-badge&logo=googlechrome&logoColor=white)](https://jesusburgoshuertas.github.io/Dockus/)
[![Última Release](https://img.shields.io/github/v/release/JESUSBURGOSHUERTAS/Dockus?style=for-the-badge&color=0EA5E9)](https://github.com/JESUSBURGOSHUERTAS/Dockus/releases/latest)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20(x64)-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![Framework](https://img.shields.io/badge/.NET-10.0%20WPF-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Style](https://img.shields.io/badge/Style-Modern%20Aero%20Glass-38BDF8?style=for-the-badge)
![License](https://img.shields.io/badge/License-GPLv3-10B981?style=for-the-badge)

<p align="center">
  <b>Un monitor de escritorio flotante y transparente para Windows que rastrea tus cuotas y consumo de tokens de IA en tiempo real, diseñado con estética translúcida inspirada en Windows 7 Aero Glass, micropartículas ambientales vivas y acople magnético a las esquinas de tu pantalla.</b>
</p>

[🌐 Visitar Sitio Web](https://jesusburgoshuertas.github.io/Dockus/) · [⬇ Descargar v1.0.0](https://github.com/JESUSBURGOSHUERTAS/Dockus/releases/latest) · [🐞 Reportar un Error](https://github.com/JESUSBURGOSHUERTAS/Dockus/issues)

</div>

---

## 🌟 ¿Qué es Dockus?

**Dockus** es una barra de monitoreo inteligente y compacta concebida para **cualquier persona que use Inteligencia Artificial en su día a día**: programadores, creadores de contenido, investigadores, redactores o entusiastas de la IA. Mientras trabajas en tu editor, interactúas con modelos de lenguaje o ejecutas tareas complejas, Dockus permanece discretamente acoplado sobre tu barra de tareas mostrándote con precisión matemática cuánto límite de uso te queda y exactamente cuántas horas y minutos faltan para que tu cuota se reinicie al 100%.

Todo esto integrado en un diseño de cristal físico **Aero Glass** de última generación, sin estorbar tu flujo de trabajo, con cero telemetría externa y 100% de ejecución local en tu máquina.

---

## ⚡ Características Principales

- **✦ Monitoreo Oficial de Cuotas de Antigravity en Tiempo Real**:
  - Conexión directa mediante **Connect-RPC** al Language Server local de Antigravity (`agy`).
  - Monitorea simultáneamente los límites de:
    - **Gemini Models** (Flash & Pro): Límite semanal y ventana móvil de 5 horas.
    - **Claude & GPT Models** (Opus, Sonnet, GPT-OSS): Límite semanal y ventana de 5 horas.
    - **Gemini 4 Argon**: Cuota dedicada de razonamiento rápido semanal.
- **⏱ Selector Rápido de Período (`[ ⏱ 5h ]` / `[ 📅 7d ]`)**:
  - Alterna al instante la vista de los medidores entre la ventana móvil corta de 5 horas y la cuota semanal completa de 7 días con un solo clic.
- **⏳ Cuenta Regresiva Exacta de Reinicio**:
  - Muestra el tiempo restante real para el restablecimiento de cada modelo (ej. `3h 28m`, `4h 55m`, `6d 18h` o `100% Libre`), calculando dinámicamente cada segundo sin textos estáticos.
- **🔮 Estética Aero Glass Moderna con Micropartículas**:
  - Fondo translúcido con desenfoque de cristal físico **DWM Acrylic BlurBehind** nativo de Windows.
  - Brillo especular biselado en el borde superior idéntico al vidrio de Windows 7 Aero.
  - Lienzo de **micropartículas de luz viva** (cian, púrpura, menta) que flotan suavemente en el interior del cristal con un consumo de CPU ultra bajo (<0.5%).
- **🧲 Acople Magnético a Esquinas y Barra de Tareas**:
  - Anclaje automático al ras de la esquina inferior derecha o izquierda de la pantalla, sin sombras cuadradas, sin rebordes indeseados y sin márgenes invisibles.
- **📊 Panel Detallado Expandible (Flyout Dashboard)**:
  - Vista completa con desglose de cuotas oficiales, descripciones de capacidad global, tokens de entrada, salida, caché y tasa de acierto de conversaciones activas.
- **🧩 Arquitectura Multi-Proveedor Modular**:
  - Preparado para incorporar APIs externas (Gemini API directa, OpenAI, Claude, DeepSeek, Ollama).

---

## 🖥️ Requisitos del Sistema

- **Sistema Operativo:** Windows 10 (versión 1903 o superior) o Windows 11 (64 bits).
- **Runtime:** [.NET 10.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) para Windows `win-x64`.
- **Entorno:** Google Antigravity (AGY) en ejecución para el rastreo de cuotas locales.

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

## ⚙️ Controles y Uso

- **Arrastre libre:** Haz clic con el botón izquierdo sobre la barra y muévela a cualquier lugar. Al soltarla cerca de la barra de tareas, se acoplará magnéticamente a la esquina.
- **Alternar período:** Pulsa en `⏱ 5h` o `📅 7d` para alternar la escala de tiempo entre el ciclo corto móvil y el semanal.
- **Expandir detalles:** Pulsa en `▼` en la esquina derecha para abrir el panel flyout con la analítica de tokens, selector de opacidad y botones de anclaje rápido.
- **Cerrar:** Pulsa en `✕` en cualquier momento.

---

## 📂 Arquitectura del Proyecto

```
Dockus/
├── assets/                           # Identidad visual y logotipo oficial
│   ├── logo.png                      # Logotipo Cyber Lion en alta resolución
│   └── app.ico                       # Icono ejecutable de la aplicación
├── docs/                             # Sitio web estático para GitHub Pages
│   ├── index.html                    # Landing page moderna y responsive
│   └── assets/                       # Recursos visuales del portal
├── src/
│   ├── Controls/
│   │   ├── AeroParticlesCanvas.cs    # Motor liviano de partículas ambientales estilo cristal vivo
│   │   └── CircularGauge.xaml        # Medidor circular vectorial con geometría matemática sin recortes
│   ├── Helpers/
│   │   ├── TaskbarDockHelper.cs      # Algoritmo de acople magnético al área de trabajo de Windows
│   │   └── WindowBlurHelper.cs       # P/Invoke a DWM para Acrylic BlurBehind nativo
│   ├── Models/
│   │   ├── AppSettings.cs            # Persistencia de preferencias del usuario en JSON
│   │   ├── QuotaModels.cs            # Estructuras de datos de cuotas, límites y cuenta regresiva
│   │   └── TokenUsageSnapshot.cs     # Métricas de consumo de tokens y contexto
│   ├── Services/
│   │   ├── AntigravityQuotaService.cs# Cliente Connect-RPC para el Language Server local de Antigravity
│   │   ├── AntigravityGeminiProvider.cs # Lector de sesiones y base SQLite de Antigravity
│   │   └── ExternalAiProviders.cs    # Proveedores modulares extensibles
│   └── MainWindow.xaml               # Ventana principal acrílica con diseño Aero Glass
├── Iniciar-Dockus.bat                # Script de inicio rápido
├── LICENSE                           # Licencia GNU GPLv3
└── README.md
```

---

## 📜 Licencia

Este proyecto está publicado bajo la licencia **GNU General Public License v3.0 (GPLv3)**.

> [!IMPORTANT]
> **¿Por qué GPLv3?**  
> Esta licencia garantiza que Dockus es y será siempre software libre y de uso público. Cualquier persona puede usarlo, estudiarlo, modificarlo y compartirlo. Sin embargo, impone **Copyleft estricto**: ninguna persona o empresa puede tomar este código o crear una obra derivada para convertirla en software privado, cerrado o comercial de código cerrado. Todo software basado en Dockus debe permanecer 100% de código abierto bajo esta misma licencia.

---

<div align="center">
  <b>Hecho con dedicación por <a href="https://github.com/JESUSBURGOSHUERTAS">Jesus Burgos</a></b><br>
  <sub>Dockus es un proyecto independiente de código abierto y no está afiliado oficialmente a Google ni Anthropic.</sub>
</div>
