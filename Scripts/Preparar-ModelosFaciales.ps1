[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$carpetaModelos = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\WindowsForms_Presentacion\Modelos'))
$revision = '48b10fa505aeb51fbd127fc60a5c660040e823b7'
$base = "https://media.githubusercontent.com/media/opencv/opencv_zoo/$revision/models"
$modelos = @(
    @{ Ruta = 'face_detection_yunet/face_detection_yunet_2023mar.onnx'; Sha256 = '8f2383e4dd3cfbb4553ea8718107fc0423210dc964f9f4280604804ed2552fa4' },
    @{ Ruta = 'face_recognition_sface/face_recognition_sface_2021dec.onnx'; Sha256 = '0ba9fbfa01b5270c96627c4ef784da859931e02f04419c829e83484087c34e79' }
)

New-Item -ItemType Directory -Path $carpetaModelos -Force | Out-Null
foreach ($modelo in $modelos) {
    $nombre = Split-Path $modelo.Ruta -Leaf
    $destino = Join-Path $carpetaModelos $nombre
    if ((Test-Path -LiteralPath $destino) -and
        (Get-FileHash -LiteralPath $destino -Algorithm SHA256).Hash -eq $modelo.Sha256) {
        Write-Host "$nombre verificado."
        continue
    }
    $temporal = Join-Path $carpetaModelos ($nombre + '.download')
    try {
        Write-Host "Descargando $nombre desde OpenCV Zoo..."
        Invoke-WebRequest -Uri "$base/$($modelo.Ruta)" -OutFile $temporal -UseBasicParsing
        if ((Get-FileHash -LiteralPath $temporal -Algorithm SHA256).Hash -ne $modelo.Sha256) {
            throw "El SHA256 de $nombre no coincide con el modelo oficial. No se instaló el archivo."
        }
        Move-Item -LiteralPath $temporal -Destination $destino -Force
    }
    finally {
        if (Test-Path -LiteralPath $temporal) { Remove-Item -LiteralPath $temporal -Force }
    }
}
Write-Host 'Modelos listos. Compila WindowsForms_Presentacion para copiarlos junto a la aplicación.'
