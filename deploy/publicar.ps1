<#
    Corré esto en tu máquina (no en la PC del cliente) cada vez que quieras armar una versión
    nueva para llevar al local. Genera una carpeta autocontenida: no hace falta tener .NET
    instalado en la PC del cliente, ni entregar el código fuente.
#>
param(
    [string]$Salida = "$PSScriptRoot\publicado"
)

$ErrorActionPreference = "Stop"
$proyecto = Join-Path $PSScriptRoot "..\SinnersRelatos.Web\SinnersRelatos.Web.csproj"

if (Test-Path $Salida) {
    Remove-Item $Salida -Recurse -Force
}

dotnet publish $proyecto -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $Salida

Copy-Item "$PSScriptRoot\instalar-servicio.ps1" -Destination $Salida -Force
Copy-Item "$PSScriptRoot\instalar-servicio.bat" -Destination $Salida -Force

Write-Host ""
Write-Host "Listo. Copiá la carpeta completa '$Salida' a la PC del cliente (USB, red, etc.) y ahí corré 'instalar-servicio.bat' como Administrador (clic derecho > Ejecutar como administrador)." -ForegroundColor Green
