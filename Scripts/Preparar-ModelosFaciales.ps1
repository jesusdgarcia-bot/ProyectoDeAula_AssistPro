[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$carpetaModelos = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\WindowsForms_Presentacion\Modelos'))
$revision = '48b10fa505aeb51fbd127fc60a5c660040e823b7'
$base = "https://media.githubusercontent.com/media/opencv/opencv_zoo/$revision/models"
$modelos = @(
    @{ Url = "$base/face_detection_yunet/face_detection_yunet_2023mar.onnx"; Algoritmo = 'SHA256'; Hash = '8f2383e4dd3cfbb4553ea8718107fc0423210dc964f9f4280604804ed2552fa4' },
    @{ Url = "$base/face_recognition_sface/face_recognition_sface_2021dec.onnx"; Algoritmo = 'SHA256'; Hash = '0ba9fbfa01b5270c96627c4ef784da859931e02f04419c829e83484087c34e79' },
    @{ Url = 'https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task'; Algoritmo = 'SHA256'; Hash = '64184e229b263107bc2b804c6625db1341ff2bb731874b0bcc2fe6544e0bc9ff' },
    @{ Url = 'https://storage.openvinotoolkit.org/repositories/open_model_zoo/public/2022.1/anti-spoof-mn3/anti-spoof-mn3.onnx'; Algoritmo = 'SHA384'; Hash = '6de4534964b723397b3e8c995cadcf43bc007cc2f9930b95ae25f76adccece5d1d4d058d0b15117b9e4a9f758424f92a' }
)

New-Item -ItemType Directory -Path $carpetaModelos -Force | Out-Null
foreach ($modelo in $modelos) {
    $nombre = ([Uri]$modelo.Url).Segments[-1]
    $destino = Join-Path $carpetaModelos $nombre
    if ((Test-Path -LiteralPath $destino) -and
        (Get-FileHash -LiteralPath $destino -Algorithm $modelo.Algoritmo).Hash -eq $modelo.Hash) {
        Write-Host "$nombre verificado."
        continue
    }
    $temporal = Join-Path $carpetaModelos ($nombre + ".${PID}.download")
    try {
        Write-Host "Descargando $nombre..."
        Invoke-WebRequest -Uri $modelo.Url -OutFile $temporal -UseBasicParsing
        if ((Get-FileHash -LiteralPath $temporal -Algorithm $modelo.Algoritmo).Hash -ne $modelo.Hash) {
            throw "El hash $($modelo.Algoritmo) de $nombre no coincide con el modelo de referencia."
        }
        Move-Item -LiteralPath $temporal -Destination $destino -Force
    }
    finally {
        if (Test-Path -LiteralPath $temporal) { Remove-Item -LiteralPath $temporal -Force }
    }
}
Write-Host 'Modelos listos. Compila WindowsForms_Presentacion para copiarlos junto a la aplicación.'
