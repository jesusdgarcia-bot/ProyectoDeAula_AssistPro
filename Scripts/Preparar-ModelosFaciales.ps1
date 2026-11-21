[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
# MSBuild puede iniciar Windows PowerShell con PSModulePath heredado de PowerShell 7.
Import-Module (Join-Path $PSHOME 'Modules\Microsoft.PowerShell.Utility\Microsoft.PowerShell.Utility.psd1')
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$carpetaModelos = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\WindowsForms_Presentacion\Modelos'))
$revision = '48b10fa505aeb51fbd127fc60a5c660040e823b7'
$base = "https://media.githubusercontent.com/media/opencv/opencv_zoo/$revision/models"
$modelos = @(
    @{ Url = "$base/face_detection_yunet/face_detection_yunet_2023mar.onnx"; Hash = '8f2383e4dd3cfbb4553ea8718107fc0423210dc964f9f4280604804ed2552fa4' },
    @{ Url = "$base/face_recognition_sface/face_recognition_sface_2021dec.onnx"; Hash = '0ba9fbfa01b5270c96627c4ef784da859931e02f04419c829e83484087c34e79' }
)

New-Item -ItemType Directory -Path $carpetaModelos -Force | Out-Null
foreach ($modelo in $modelos) {
    $nombre = ([Uri]$modelo.Url).Segments[-1]
    $destino = Join-Path $carpetaModelos $nombre
    if ((Test-Path -LiteralPath $destino) -and
        (Get-FileHash -LiteralPath $destino -Algorithm SHA256).Hash -eq $modelo.Hash) {
        Write-Host "$nombre verificado."
        continue
    }
    $temporal = Join-Path $carpetaModelos ($nombre + ".${PID}.download")
    try {
        Write-Host "Descargando $nombre..."
        Invoke-WebRequest -Uri $modelo.Url -OutFile $temporal -UseBasicParsing
        if ((Get-FileHash -LiteralPath $temporal -Algorithm SHA256).Hash -ne $modelo.Hash) {
            throw "El hash SHA256 de $nombre no coincide con el modelo de referencia."
        }
        Move-Item -LiteralPath $temporal -Destination $destino -Force
    }
    finally {
        if (Test-Path -LiteralPath $temporal) { Remove-Item -LiteralPath $temporal -Force }
    }
}
Write-Host 'Modelos listos. Compila WindowsForms_Presentacion para copiarlos al directorio de salida.'
