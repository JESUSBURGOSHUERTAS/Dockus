# ✦ Dockus - Monitor Transparente de Tokens para Gemini y Otras IA

**Dockus** es un monitor de escritorio con diseño translúcido moderno (*glassmorphism / acrílico*) que se acopla directamente sobre la **Barra de Tareas de Windows**. Muestra en tiempo real el consumo de tokens, el tamaño de la ventana de contexto, el ahorro generado por caché y cuánto te queda antes de alcanzar los límites de la IA.

Inicia conectado de forma nativa a **Google Gemini en Antigravity**, con una arquitectura modular extensible para **Google Gemini API (Studio)**, **OpenAI (GPT-4o)**, **Anthropic Claude (3.5/3.7)**, **DeepSeek (V3/R1)** y **Ollama**.

---

## 📸 Características Principales

### 1. Barra Compacta Tipo Dock (Taskbar Companion)
- **Ultra-ligera y elegante:** Se adapta automáticamente justo sobre la barra de tareas de Windows (Abajo a la derecha, Centro, Abajo a la izquierda o Flotante libre).
- **Métricas en vivo:**
  - `Contexto Activo / Máximo`: Cantidad exacta de tokens cargados en la sesión (ej: `42.5k / 1.0M`).
  - `Cuánto me queda`: Tokens libres y porcentaje restante con color dinámico (Verde > 50%, Celeste > 25%, Amarillo < 25%, Rojo < 10%).
  - `Barra Neón con Gradiente`: Indicador visual de llenado de contexto.
  - `⚡ Prompt Cache Hit`: Muestra el ahorro porcentual de tokens reutilizados mediante la caché de Google.
  - `Indicador de Actividad`: Pulso visual cuando Antigravity o la IA está procesando.

### 2. Panel Expandido (Dashboard Glass)
- Al hacer clic en el botón de expansión `▲`:
  - **Ventana de Contexto:** Total de tokens en el modelo y porcentaje ocupado.
  - **Cuánto me Queda:** Tokens disponibles antes de compactación o truncado.
  - **Ahorro Prompt Cache:** Tokens recuperados del caché en el último turno y en la sesión.
  - **Consumo Total de Hoy:** Desglose acumulado de todas las sesiones de hoy (Input, Output, Cache).
  - **Historial de Conversaciones Recientes:** Listado de sesiones con sus nombres reales, cantidad de pasos y tokens estimados.
  - **Selector de Proveedor (Tabs):** Alterna entre *Gemini (Antigravity)*, *Gemini API*, *OpenAI*, *Claude* y *DeepSeek*.
  - **Ajustes Rápidos:** Control de opacidad del vidrio (50% a 100%), botón "Siempre Visible" (Always on Top), alineación en barra y selector de límite de contexto (1M Flash, 2M Pro, 200k Claude, 128k GPT).

---

## 🚀 Cómo Iniciar Dockus

### Opción 1: Con el Acceso Directo (Recomendado)
Haz doble clic sobre el archivo:
```bat
Iniciar-Dockus.bat
```

### Opción 2: Ejecutable Directo
Puedes ejecutar directamente:
```text
publish\Dockus.exe
```

### Opción 3: Crear Acceso Directo en el Escritorio
Ejecuta con PowerShell:
```powershell
powershell -ExecutionPolicy Bypass -File .\Crear-Acceso-Directo.ps1
```

---

## 🛠️ Arquitectura y Tecnologías
- **Framework:** .NET 10.0 WPF (Windows Presentation Foundation) nativo x64.
- **Efectos:** Desenfoque acrílico DWM (`SetWindowCompositionAttribute` / `DwmSetWindowAttribute`).
- **Persistencia y Lectura:**
  - Lectura en tiempo real de logs de Antigravity (`FileShare.ReadWrite`).
  - Consulta de metadatos en SQLite con `Microsoft.Data.Sqlite` en modo solo lectura (`Mode=ReadOnly`).
- **Arquitectura Modular (`ITokenProvider`):**
  - `AntigravityGeminiProvider`: Monitor nativo en tiempo real sin requerir credenciales adicionales.
  - `GoogleGeminiApiProvider`: Conexión directa a Google AI Studio.
  - `OpenAiProvider`: GPT-4o / GPT-4o-mini.
  - `ClaudeProvider`: Anthropic Claude 3.5/3.7.
  - `DeepSeekProvider`: DeepSeek V3 / R1.

---

## ⚙️ Personalización
La configuración se guarda automáticamente en:
`%APPDATA%\Dockus\settings.json`

Permite configurar:
- `DockPosition`: `"BottomRight"`, `"BottomCenter"`, `"BottomLeft"`, `"Floating"`.
- `AlwaysOnTop`: `true` / `false`.
- `GlassOpacity`: `0.50` a `1.0`.
- `MaxContextTokens`: `1000000` (1M), `2000000` (2M), etc.
- Claves API para proveedores externos (opcional).

