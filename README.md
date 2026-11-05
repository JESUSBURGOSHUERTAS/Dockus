# ✦ Dockus — AI Quota & Token Monitor for Windows

<div align="center">

![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20(x64)-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![Framework](https://img.shields.io/badge/.NET-10.0%20WPF-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Style](https://img.shields.io/badge/Style-Modern%20Aero%20Glass-38BDF8?style=for-the-badge)
![License](https://img.shields.io/badge/License-GPLv3-10B981?style=for-the-badge)

**Un monitor de cuotas y consumo de tokens para IA de escritorio, diseñado con estética translúcida estilo Windows 7 Aero moderno, micropartículas ambientales y acople magnético a las esquinas de la barra de tareas.**

</div>

---

## 🌟 Características Principales

- **✦ Monitoreo Oficial de Cuotas de Antigravity en Tiempo Real**:
  - Conexión directa mediante **Connect-RPC** al Language Server local de Antigravity (`agy`).
  - Monitorea simultáneamente los límites de:
    - **Gemini Models** (Flash & Pro): Límite semanal y ventana móvil de 5 horas.
    - **Claude & GPT Models** (Opus, Sonnet, GPT-OSS): Límite semanal y ventana de 5 horas.
    - **Gemini 4 Argon**: Cuota dedicada de razonamiento rápido semanal.
- **⏱ Selector Rápido de Período (`[ ⏱ 5h ]` / `[ 📅 7d ]`)**:
  - Cambia al instante la vista de los medidores entre la ventana móvil de 5 horas y la cuota semanal.
- **⏳ Cuenta Regresiva Exacta de Reinicio**:
  - Muestra el tiempo restante real para el restablecimiento de cada modelo (ej. `4h 03m`, `4d 01h` o `100% Libre`), sin textos genéricos.
- **🔮 Estética Aero Glass Moderna con Micropartículas**:
  - Fondo translúcido con desenfoque de cristal físico **DWM Acrylic BlurBehind**.
  - Brillo especular biselado en el borde superior idéntico al vidrio de Windows 7 Aero.
  - Lienzo de **micropartículas de luz viva** (cian, púrpura, menta) que flotan suavemente en el interior del cristal con un consumo de CPU del 0.0%.
- **🧲 Acople Magnético a Esquinas y Barra de Tareas**:
  - Anclaje al ras de la esquina inferior derecha o izquierda de la pantalla, sin sombras cuadradas ni márgenes invisibles molestos.
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

## 🚀 Compilación e Instalación

### 1. Clonar el repositorio
```bash
git clone https://github.com/JESUSBURGOSHUERTAS/Dockus.git
cd Dockus
```

### 2. Compilar el proyecto
```powershell
dotnet build src/Dockus.csproj -c Release
```

### 3. Publicar la versión ejecutable independiente
```powershell
dotnet publish src/Dockus.csproj -c Release -o publish
```

### 4. Ejecución
Puedes iniciar la aplicación directamente desde la carpeta `publish`:
```powershell
.\publish\Dockus.exe
```
O ejecutando el lanzador por lotes:
```cmd
Iniciar-Dockus.bat
```

---

## ⚙️ Configuración y Uso

### Interfaz Compacta (Dock Bar)
- **Arrastre libre:** Haz clic con el botón izquierdo y arrastra la barra a cualquier parte de tu pantalla. Si la sueltas cerca de la barra de tareas, se acoplará magnéticamente a la esquina.
- **Cambio de período:** Haz clic en `⏱ 5h` o `📅 7d` para alternar entre cuotas por hora y semanales.
- **Expandir:** Haz clic en el botón `▼` en el extremo derecho para abrir el panel detallado.

### Panel Detallado (Flyout)
- **Pestaña Cuotas y Límites:** Visualiza las tarjetas completas de los modelos Gemini, Claude & GPT y Argon con sus tiempos de ciclo.
- **Pestaña Tokens y Contexto:** Analiza el consumo de la sesión activa de conversación, tokens de caché y uso acumulado del día.
- **Opciones de anclaje y opacidad:** Configura el nivel de transparencia del cristal, mantén la ventana siempre al frente (*Always on Top*) o cámbiala de esquina.

---

## 🛠️ Arquitectura Técnica

```
Dockus/
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

Este proyecto está bajo la licencia **GNU General Public License v3.0 (GPLv3)**.

> [!NOTE]
> **¿Por qué GPLv3?**  
> Esta licencia permite a cualquier persona usar, estudiar, modificar y compartir el software libremente. Sin embargo, impone una protección de **Copyleft estricto**: ninguna persona o empresa puede tomar este código o sus derivados para convertirlos en un producto propietario o de código cerrado. Cualquier modificación o distribución derivada debe permanecer 100% de código abierto bajo esta misma licencia.

---

<div align="center">
Desarrollado con pasión para la comunidad de desarrolladores e investigadores de IA.
</div>
