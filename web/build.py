#!/usr/bin/env python3
"""Empacota o projeto web em UM arquivo self-contained (CSS/JS inline + sprites base64).
Uso: python3 build.py   ->  web/dist/meow-tactics.html (pronto para publicar como Artifact)."""
import base64, io, os, re, json
from PIL import Image

ROOT = os.path.dirname(os.path.abspath(__file__))
def p(*a): return os.path.join(ROOT, *a)

def data_uri_png(path, size, is_map=False):
    im = Image.open(path).convert('RGBA')
    if is_map:
        w = size; h = round(im.height * size / im.width)
        im = im.resize((w, h), Image.LANCZOS)
    else:
        im = im.resize((size, size), Image.LANCZOS)
    buf = io.BytesIO(); im.save(buf, format='PNG', optimize=True)
    return 'data:image/png;base64,' + base64.b64encode(buf.getvalue()).decode()

# ---- assets ----
assets = {}
for f in os.listdir(p('assets', 'cats')):
    if f.endswith('.png'): assets['assets/cats/' + f] = data_uri_png(p('assets', 'cats', f), 140)
for f in os.listdir(p('assets', 'enemies')):
    if f.endswith('.png'): assets['assets/enemies/' + f] = data_uri_png(p('assets', 'enemies', f), 140)
for f in os.listdir(p('assets', 'maps')):
    if f.endswith('.png'): assets['assets/maps/' + f] = data_uri_png(p('assets', 'maps', f), 640, is_map=True)
# UI: fundo do menu (grande) + ícones (pequenos)
UI_SIZE = {'menu_bg.png': (720, True), 'ui_moeda.png': (72, False), 'ui_vida.png': (72, False),
           'ui_painel.png': (300, True), 'ui_botao.png': (300, True), 'app_icon.png': (256, False)}
for f, (sz, ismap) in UI_SIZE.items():
    fp = p('assets', 'ui', f)
    if os.path.exists(fp): assets['assets/ui/' + f] = data_uri_png(fp, sz, is_map=ismap)

# ---- css ----
css = (open(p('styles', 'tokens.css')).read() + '\n' + open(p('styles', 'ui.css')).read()
       + '\n' + open(p('styles', 'polish.css')).read())

# ---- js (ordem) ----
JS_ORDER = ['src/data/gamedata.js', 'src/util.js', 'src/rules.js', 'src/game.js',
            'src/meta.js', 'src/sound.js', 'src/render.js', 'src/input.js', 'src/ui.js', 'src/main.js']
js_parts = []
for f in JS_ORDER:
    if f == 'src/main.js':
        # injeta o resolvedor de assets ANTES do main
        js_parts.append('window.MT=window.MT||{};MT.ASSETS=' + json.dumps(assets) +
                        ';MT.assetURL=function(x){return MT.ASSETS[x]||x;};')
    js_parts.append('/* ' + f + ' */\n' + open(p(f)).read())
js = '\n'.join(js_parts)

# ---- markup do body (sem doctype/html/head/body e sem <script src>) ----
html = open(p('index.html')).read()
body = re.search(r'<body>(.*)</body>', html, re.S).group(1)
body = re.sub(r'\s*<script src=[^>]+></script>', '', body)

# Preserva as <meta> da <head> original (charset, viewport, apple-web-app) para
# o bundle NÃO divergir do index.html — sem isso, mobile renderiza em largura
# desktop e a safe-area (env(...)) vira 0.
head = re.search(r'<head>(.*)</head>', html, re.S).group(1)
metas = re.findall(r'<meta[^>]+>', head)
if not any('charset' in m for m in metas):
    metas.insert(0, '<meta charset="utf-8">')
head_out = '\n'.join(metas) + '\n<title>Meow Tactics TD</title>'

out = head_out + '\n<style>\n' + css + '\n</style>\n' + body + '\n<script>\n' + js + '\n</script>\n'
os.makedirs(p('dist'), exist_ok=True)
open(p('dist', 'meow-tactics.html'), 'w').write(out)
print('dist/meow-tactics.html:', len(out) // 1024, 'KB  |  assets:', len(assets))
