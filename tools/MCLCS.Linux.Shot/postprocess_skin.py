import os
import sys
from PIL import Image


def main(out_dir: str):
    rect_file = os.path.join(out_dir, "skin_preview_rect.txt")
    skin3d_file = os.path.join(out_dir, "skin3d.png")
    if not os.path.exists(rect_file) or not os.path.exists(skin3d_file):
        print("skip: skin_preview_rect.txt / skin3d.png not found")
        return

    with open(rect_file, "r", encoding="utf-8") as f:
        parts = f.read().strip().split()
    if len(parts) != 4:
        print("skip: invalid rect file")
        return

    x, y, w, h = map(int, parts)
    fg = Image.open(skin3d_file).convert("RGBA")

    for name in ("tb-skin.png", "tb-skin-light.png"):
        src = os.path.join(out_dir, name)
        if not os.path.exists(src):
            continue
        bg = Image.open(src).convert("RGBA")
        # skin3d 已按 Preview3D.Bounds 精确尺寸渲染，1:1 贴入左上角；
        # 超出窗口的部分会由 PIL 自动裁剪，与真实 SkinPreview3D 在窗口中的裁剪一致。
        bg.paste(fg, (x, y), fg)
        bg.convert("RGB").save(src, "PNG")
        print(f"[postprocess] {name}: skin3d composited into ({x},{y},{w},{h})")


if __name__ == "__main__":
    if len(sys.argv) > 1:
        main(sys.argv[1])
    else:
        print("usage: python3 postprocess_skin.py <out_dir>")
