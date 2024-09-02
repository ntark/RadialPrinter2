import cv2
import os
import sys
import random

# svg to gcode from https://github.com/sameer/svg2gcode
# canny_edge_detection from https://learnopencv.com/edge-detection-using-opencv/


def canny_edge_detection(img):
    # grayscale
    img_gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    # blur
    img_blur = cv2.GaussianBlur(img_gray, (3, 3), 0)
    # Canny Edge Detection
    edges = cv2.Canny(image=img_blur, threshold1=100, threshold2=200)

    cv2.destroyAllWindows()

    return edges


def image_to_svg(path, image_name, out_name):
    os.system(f"potrace {path}/{image_name} -b svg -o {path}/{out_name}")


def svg_to_gcode(path, cargo_path, image_name, out_name):
    script_path = os.getcwd()

    os.chdir(cargo_path)

    os.system(
        f"cargo run --release -- {path}/{image_name} --off 'M4' --on 'M5' -o {path}/{out_name}"
    )

    os.chdir(script_path)


def main():
    img_path = sys.argv[1]
    random_string = sys.argv[2]

    img = cv2.imread(img_path)

    # random_number = random.randint(10000000, 16777215)
    # random_string = str(hex(random_number))
    # random_string = f"{random_string[2:]}"

    image_bmp_path = f"image_{random_string}.bmp"
    image_svg_path = f"image_{random_string}.svg"
    image_gcode_path = f"image_{random_string}.gcode"
    edges_bmp_path = f"edges_{random_string}.bmp"
    edges_svg_path = f"edges_{random_string}.svg"
    edges_gcode_path = f"edges_{random_string}.gcode"

    temp_path = f"/home/opc/workspace/works/c#/RadialPrinter2/Converter/temp"
    cargo_path = f"/home/opc/workspace/works/c#/RadialPrinter2/Converter/svg2gcode"

    cv2.imwrite(os.path.join(temp_path, image_bmp_path), img)

    image_to_svg(temp_path, image_bmp_path, image_svg_path)
    svg_to_gcode(temp_path, cargo_path, image_svg_path, image_gcode_path)

    edges = canny_edge_detection(img)

    cv2.imwrite(os.path.join(temp_path, edges_bmp_path), edges)

    image_to_svg(temp_path, edges_bmp_path, edges_svg_path)
    svg_to_gcode(temp_path, cargo_path, edges_svg_path, edges_gcode_path)


if __name__ == "__main__":
    main()
