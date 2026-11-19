# Modelos faciales

La primera compilación prepara automáticamente los modelos que faltan. Para descargarlos o repararlos manualmente, ejecuta desde la raíz del repositorio:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\Preparar-ModelosFaciales.ps1
```

El script descarga los modelos oficiales de [OpenCV Zoo](https://github.com/opencv/opencv_zoo/tree/48b10fa505aeb51fbd127fc60a5c660040e823b7/models), fija esa revisión y verifica los SHA256 publicados en sus punteros Git LFS. No descarga modelos durante el acceso de un usuario. Se requieren aproximadamente 39 MB y conexión a Internet únicamente para esta preparación. Los archivos `.onnx` se copian en `Modelos` junto al ejecutable al compilar.

| Archivo | SHA256 | Licencia |
| --- | --- | --- |
| face_detection_yunet_2023mar.onnx | 8f2383e4dd3cfbb4553ea8718107fc0423210dc964f9f4280604804ed2552fa4 | MIT; ver LICENSE-YuNet.txt |
| face_recognition_sface_2021dec.onnx | 0ba9fbfa01b5270c96627c4ef784da859931e02f04419c829e83484087c34e79 | Apache 2.0; ver LICENSE-SFace.txt |

Requiere Windows de 64 bits y [Microsoft Visual C++ Redistributable 2015–2022 x64](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist). OpenCvSharp usa CPU; no requiere CUDA ni Python. El motor detecta un único rostro con YuNet, lo alinea a 112 × 112 usando cinco puntos y extrae con SFace una representación normalizada de 128 valores. El preprocesamiento RGB sigue [la implementación oficial de OpenCV](https://github.com/opencv/opencv/blob/4.10.0/modules/objdetect/src/face_recognize.cpp).
