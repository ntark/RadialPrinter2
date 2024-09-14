import cv2
import os
import sys
import requests
import json
import uuid
from flask import Flask, request

# svg to gcode from https://github.com/sameer/svg2gcode
# canny_edge_detection from https://learnopencv.com/edge-detection-using-opencv/


def canny_edge_detection(in_path, out_path):
    # read image
    img = cv2.imread(in_path)
    # grayscale
    img_gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    # blur
    img_blur = cv2.GaussianBlur(img_gray, (3, 3), 0)
    # Canny Edge Detection
    edges = cv2.Canny(image=img_blur, threshold1=100, threshold2=200)

    cv2.destroyAllWindows()

    cv2.imwrite(out_path, edges)


def image_to_svg(in_path, out_path):
    res = os.system(f"potrace {in_path} -b svg -o {out_path}")

    if res != 0:
        raise Exception("svg to gcode server error")


def svg_to_gcode(in_path, out_path):
    script_path = os.getcwd()

    os.chdir(CARGO_FOLDER_PATH)

    res = os.system(
        f"RUSTFLAGS=\"-Awarnings\" cargo run --release -- {in_path} --off 'M4' --on 'M5' -o {out_path}"
    )

    if res != 0:
        raise Exception("svg to gcode server error")

    os.chdir(script_path)


def image_to_bmp(in_path, out_path):
    img = cv2.imread(in_path)
    cv2.imwrite(out_path, img)

def parse_gcode(gcode_file, result_path):
    plt.figure(figsize=(10, 10))
    with open(gcode_file, 'r') as file:
        px = None
        py = None
        for line in file:
            parts = line.split()
            x = y = None
            if line.startswith('G1') or line.startswith('G0'):
                for part in parts:
                    if part.startswith('X'):
                        x = float(part[1:])
                    elif part.startswith('Y'):
                        y = float(part[1:])
                if x is not None and y is not None and px is not None and py is not None:
                    if line.startswith('G1'):
                        plt.plot([px, x], [py, y], 'b-')
                    else:
                        plt.plot([px, x], [py, y], 'y-')
                px = x
                py = y
    plt.xlabel('X')
    plt.ylabel('Y')
    plt.title('G-code Preview')
    plt.legend()
    plt.grid(True)
    plt.savefig(result_path, format='png')

app = Flask(__name__)


TEMP_FLODER_PATH = "/home/opc/workspace/works/csharp/RadialPrinter2/Converter/temp/"
CARGO_FOLDER_PATH = (
    "/home/opc/workspace/works/csharp/RadialPrinter2/Converter/svg2gcode"
)


@app.route("/imageToSvg", methods=["GET"])
def imageToSvg():
    try:
        filePath = request.args.get("filePath")

        print(f"reading file from: {filePath}")

        randomString = str(uuid.uuid4())
        bmpFileName = f"{randomString}.bmp"

        resultFileName = f"{randomString}.svg"
        resultPath = os.path.join(TEMP_FLODER_PATH, resultFileName)

        bmpPath = os.path.join(TEMP_FLODER_PATH, bmpFileName)

        image_to_bmp(filePath, bmpPath)

        image_to_svg(bmpPath, resultPath)

        return resultPath, 200

    except Exception as e:
        return str(e), 500


@app.route("/svgToGCode", methods=["GET"])
def svgToGCode():
    try:
        filePath = request.args.get("filePath")

        resultFileName = f"{str(uuid.uuid4())}.gcode"

        resultPath = os.path.join(TEMP_FLODER_PATH, resultFileName)

        svg_to_gcode(filePath, resultPath)

        return resultPath, 200

    except Exception as e:
        return str(e), 500


@app.route("/imageToEdges", methods=["GET"])
def imageToEdges():
    try:
        filePath = request.args.get("filePath")

        resultFileName = f"{str(uuid.uuid4())}.bmp"

        resultPath = os.path.join(TEMP_FLODER_PATH, resultFileName)

        canny_edge_detection(filePath, resultPath)

        return resultPath, 200

    except Exception as e:
        return str(e), 500

@app.route("/gcodePreview", methods=["GET"])
def gcodePreview():
    try:
        filePath = request.args.get("filePath")

        resultFileName = f"{str(uuid.uuid4())}.png"

        resultPath = os.path.join(TEMP_FLODER_PATH, resultFileName)

        parse_gcode(filePath, resultPath)

        return resultPath, 200

    except Exception as e:
        return str(e), 500
        

if __name__ == "__main__":
    app.run(debug=True)
