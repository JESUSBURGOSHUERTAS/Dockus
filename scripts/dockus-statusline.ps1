# Dockus - puente de statusline para Claude Code
#
# Claude Code ejecuta este script cada vez que actualiza su barra de estado y le
# pasa por stdin un JSON con el estado de la sesion. Si trae `rate_limits`
# (limites del plan: ventana de 5 horas y semanal), el script los guarda en
# %APPDATA%\Dockus\claude-usage.json para que Dockus los muestre.
#
# Solo se guardan porcentajes y horas de reinicio. No se guardan credenciales,
# rutas, ids de sesion ni contenido de conversaciones.
#
# Docs: https://code.claude.com/docs/en/statusline

$ErrorActionPreference = 'SilentlyContinue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Format-Window($label, $window) {
    if ($null -eq $window -or $null -eq $window.used_percentage) { return $null }
    $left = [math]::Max(0, 100 - [double]$window.used_percentage)
    return ('{0} {1:0}% libre' -f $label, $left)
}

$raw = [Console]::In.ReadToEnd()
$data = $null
try { $data = $raw | ConvertFrom-Json } catch { }

$parts = @()
if ($data -and $data.model -and $data.model.display_name) {
    $parts += [string]$data.model.display_name
} else {
    $parts += 'Claude'
}

$rl = $null
if ($data) { $rl = $data.rate_limits }

if ($rl -and ($rl.five_hour -or $rl.seven_day)) {
    $dir = Join-Path $env:APPDATA 'Dockus'
    $dst = Join-Path $dir 'claude-usage.json'
    $tmp = Join-Path $dir ('claude-usage.' + $PID + '.tmp')

    # Cada ventana puede faltar por separado: conservar la ultima conocida.
    $prev = $null
    if (Test-Path $dst) {
        try { $prev = Get-Content -Raw -Path $dst | ConvertFrom-Json } catch { }
    }

    $five = $rl.five_hour
    $week = $rl.seven_day
    if (-not $five -and $prev) { $five = $prev.five_hour }
    if (-not $week -and $prev) { $week = $prev.seven_day }

    $out = [ordered]@{
        version    = 1
        source     = 'claude-code-statusline'
        updated_at = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
        five_hour  = $null
        seven_day  = $null
    }
    if ($five) { $out.five_hour = [ordered]@{ used_percentage = $five.used_percentage; resets_at = $five.resets_at } }
    if ($week) { $out.seven_day = [ordered]@{ used_percentage = $week.used_percentage; resets_at = $week.resets_at } }

    try {
        if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
        ($out | ConvertTo-Json -Depth 4) | Set-Content -Path $tmp -Encoding UTF8
        Move-Item -Force -Path $tmp -Destination $dst
    } catch {
        Remove-Item -Force -Path $tmp -ErrorAction SilentlyContinue
    }

    $p5 = Format-Window '5h' $five
    $p7 = Format-Window 'Semana' $week
    if ($p5) { $parts += $p5 }
    if ($p7) { $parts += $p7 }
}

Write-Output ($parts -join ' | ')
