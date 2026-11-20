# Modelos faciales

La compilación con `/p:PrepararBiometriaFacial=true` prepara los modelos que faltan para el piloto. Para descargarlos o repararlos manualmente, ejecuta desde la raíz del repositorio:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\Preparar-ModelosFaciales.ps1
```

La preparación descarga aproximadamente 55 MB desde revisiones fijas y comprueba cada archivo por hash. Durante una captura se usan los modelos que están en `Modelos` junto al ejecutable.

| Archivo | Hash | Fuente |
| --- | --- | --- |
| face_detection_yunet_2023mar.onnx | SHA256 `8f2383e4dd3cfbb4553ea8718107fc0423210dc964f9f4280604804ed2552fa4` | [OpenCV Zoo](https://github.com/opencv/opencv_zoo/tree/48b10fa505aeb51fbd127fc60a5c660040e823b7/models/face_detection_yunet), licencia MIT en LICENSE-YuNet.txt |
| face_recognition_sface_2021dec.onnx | SHA256 `0ba9fbfa01b5270c96627c4ef784da859931e02f04419c829e83484087c34e79` | [OpenCV Zoo](https://github.com/opencv/opencv_zoo/tree/48b10fa505aeb51fbd127fc60a5c660040e823b7/models/face_recognition_sface), licencia Apache 2.0 en LICENSE-SFace.txt |
| face_landmarker.task | SHA256 `64184e229b263107bc2b804c6625db1341ff2bb731874b0bcc2fe6544e0bc9ff` | [Google MediaPipe Face Landmarker v1](https://developers.google.com/edge/mediapipe/solutions/vision/face_landmarker#models) |
| anti-spoof-mn3.onnx | SHA384 `6de4534964b723397b3e8c995cadcf43bc007cc2f9930b95ae25f76adccece5d1d4d058d0b15117b9e4a9f758424f92a` | [Intel Open Model Zoo](https://github.com/openvinotoolkit/open_model_zoo/tree/master/models/public/anti-spoof-mn3) |

OpenCvSharp captura la imagen, detecta el rostro con YuNet y lo alinea a 112 × 112 usando cinco puntos. ONNX Runtime ejecuta SFace y devuelve un vector normalizado de 128 valores, con el mismo preprocesamiento que las plantillas ya existentes. También ejecuta MN3 para estimar la presencia a partir del recorte facial. MediaPipe se ejecuta en un proceso local de Python y devuelve las señales usadas por los retos de ojos y cabeza. [Implementación SFace de OpenCV](https://github.com/opencv/opencv/blob/4.10.0/modules/objdetect/src/face_recognize.cpp)

MN3 fue entrenado con CelebA-Spoof. El acuerdo del conjunto de datos limita su uso a la investigación y la educación no comerciales, incluyendo datos derivados. Esta integración se destina a la evaluación académica. [Acuerdo de CelebA-Spoof](https://github.com/ZhangYuanhan-AI/CelebA-Spoof#dataset-agreement)
