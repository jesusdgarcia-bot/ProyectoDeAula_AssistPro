"""Extrae señales faciales de fotogramas recibidos por una tubería local."""

import math
import struct
import sys
import time
from pathlib import Path


LADO_MAXIMO = 640
CABECERA = struct.Struct("<ii")


def responder(mensaje):
    print(mensaje, flush=True)


def senales(resultado):
    if not (
        len(resultado.face_landmarks)
        == len(resultado.face_blendshapes)
        == len(resultado.facial_transformation_matrixes)
        == 1
    ):
        return None

    puntos = resultado.face_landmarks[0]
    if not puntos or any(not (math.isfinite(p.x) and math.isfinite(p.y)) for p in puntos):
        return None

    puntuaciones = {
        categoria.category_name: categoria.score
        for categoria in resultado.face_blendshapes[0]
    }
    izquierdo = puntuaciones.get("eyeBlinkLeft")
    derecho = puntuaciones.get("eyeBlinkRight")
    if izquierdo is None or derecho is None:
        return None

    matriz = resultado.facial_transformation_matrixes[0]
    if matriz.shape != (4, 4):
        return None

    # Proyecta el vector frontal de la cara canónica sobre el plano XZ.
    # En vídeo sin reflejar, X positivo apunta hacia la izquierda de la persona.
    x_frontal, z_frontal = matriz[0, 2], matriz[2, 2]
    proyeccion = math.hypot(x_frontal, z_frontal)
    if not math.isfinite(proyeccion) or proyeccion <= 1e-6:
        return None
    giro = math.degrees(math.atan2(x_frontal, z_frontal))
    izquierda = max(0.0, min(p.x for p in puntos))
    arriba = max(0.0, min(p.y for p in puntos))
    derecha = min(1.0, max(p.x for p in puntos))
    abajo = min(1.0, max(p.y for p in puntos))
    if derecha <= izquierda or abajo <= arriba:
        return None

    return izquierdo, derecho, giro, izquierda, arriba, derecha, abajo


def analizar_fotogramas(mp, np, detector):
    entrada = sys.stdin.buffer
    ultima_marca = -1
    # La codificación de entrada de algunas consolas de Windows antepone un BOM.
    if entrada.peek(3).startswith(b"\xef\xbb\xbf"):
        entrada.read(3)

    while cabecera := entrada.read(CABECERA.size):
        ancho, alto = CABECERA.unpack(cabecera)
        if not (0 < ancho <= LADO_MAXIMO and 0 < alto <= LADO_MAXIMO):
            raise ValueError("Dimensiones de fotograma fuera del intervalo")

        tamano = ancho * alto * 3
        datos = entrada.read(tamano)
        if len(datos) != tamano:
            raise ValueError("Fotograma incompleto")

        rgb = np.frombuffer(datos, dtype=np.uint8).reshape((alto, ancho, 3))
        imagen = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
        marca = max(ultima_marca + 1, time.monotonic_ns() // 1_000_000)
        ultima_marca = marca
        resultado = detector.detect_for_video(imagen, marca)
        if len(resultado.face_landmarks) > 1:
            responder("varios_rostros")
            continue

        valores = senales(resultado)
        if valores is None:
            responder("sin_rostro")
        else:
            responder("ok\t" + "\t".join(format(valor, ".7g") for valor in valores))


def main():
    try:
        import mediapipe as mp
        import numpy as np
    except (ImportError, OSError):
        responder("dependencias")
        return 1

    try:
        opciones = mp.tasks.vision.FaceLandmarkerOptions(
            base_options=mp.tasks.BaseOptions(model_asset_buffer=Path(sys.argv[1]).read_bytes()),
            running_mode=mp.tasks.vision.RunningMode.VIDEO,
            num_faces=2,
            output_face_blendshapes=True,
            output_facial_transformation_matrixes=True,
        )
    except AttributeError:
        responder("dependencias")
        return 1
    except OSError:
        responder("modelo")
        return 1

    try:
        detector = mp.tasks.vision.FaceLandmarker.create_from_options(opciones)
    except OSError:
        responder("dependencias")
        return 1
    except (ValueError, RuntimeError):
        responder("modelo")
        return 1

    try:
        with detector:
            responder("listo")
            analizar_fotogramas(mp, np, detector)
    except (RuntimeError, ValueError, OSError, struct.error):
        responder("error")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
