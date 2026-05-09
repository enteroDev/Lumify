from pathlib import Path
import re, html, zipfile

root = Path("/mnt/data/app_unz")
if not root.exists():
    root.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile("/mnt/data/app.zip", "r") as zf:
        zf.extractall(root)

IMPORT_RE = re.compile(r'^\s*import\s+(.*?)\s+from\s+[\'"](.+?)[\'"];?', re.M)
PROP_TYPE_RE = re.compile(r'export\s+type\s+(\w+Props)\s*=\s*{([\s\S]*?)};')
FUNC_PARAM_TYPE_RE = re.compile(r'export\s+default\s+function\s+(\w+)\s*\(([\s\S]*?)\)\s*{', re.M)
NAME_RE = re.compile(r'export\s+default\s+function\s+(\w+)')
ATTR_RE = re.compile(r'([A-Za-z_]\w*)\s*=')

BOX_W = 360
NODE_H_BASE = 96
LEVEL_GAP = 130
LEAF_SLOT_W = 520
CANVAS_PAD = 50
CHIP_H = 28

def read_text(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except Exception:
        return path.read_text(encoding="latin-1")

def resolve_import(from_file: Path, import_path: str):
    if not import_path.startswith("."):
        return None
    base = (from_file.parent / import_path).resolve()
    candidates = [Path(str(base) + ".tsx"), Path(str(base) + ".ts"), base / "index.tsx", base / "index.ts"]
    for candidate in candidates:
        try:
            candidate.relative_to(root)
        except Exception:
            continue
        if candidate.exists():
            return candidate
    return None

def parse_imports(text: str):
    imports = {}
    for spec, import_path in IMPORT_RE.findall(text):
        spec = spec.strip()
        if import_path.endswith(".css") or import_path.endswith(".svg") or spec.startswith("type "):
            continue
        names = []
        if spec.startswith("{"):
            names.extend([x.strip().split(" as ")[-1] for x in spec.strip("{} ").split(",") if x.strip()])
        elif "," in spec:
            first, rest = spec.split(",", 1)
            if first.strip() and not first.strip().startswith("type "):
                names.append(first.strip())
            rest = rest.strip()
            if rest.startswith("{"):
                names.extend([x.strip().split(" as ")[-1] for x in rest.strip("{} ").split(",") if x.strip() and not x.strip().startswith("type ")])
        else:
            if spec:
                names.append(spec)
        for name in names:
            imports[name] = import_path
    return imports

def parse_inputs(text: str):
    match = PROP_TYPE_RE.search(text)
    if match:
        body = match.group(2)
        names = []
        for line in body.splitlines():
            stripped = line.strip()
            if not stripped or stripped.startswith("//"):
                continue
            prop_match = re.match(r'([A-Za-z_]\w*)\??\s*:', stripped)
            if prop_match:
                names.append(prop_match.group(1))
        return names

    match = FUNC_PARAM_TYPE_RE.search(text)
    if match:
        params = match.group(2).strip()
        if params.startswith("{"):
            depth = 0
            end = 0
            for i, ch in enumerate(params):
                if ch == "{":
                    depth += 1
                elif ch == "}":
                    depth -= 1
                    if depth == 0:
                        end = i
                        break
            inside = params[1:end]
            names = []
            for part in inside.split(","):
                name = part.strip()
                if not name:
                    continue
                name = name.split(":")[0].split("=")[0].strip()
                if re.match(r'^[A-Za-z_]\w*$', name):
                    names.append(name)
            return names
    return []

def find_tag_instances(text: str, tag: str):
    results = []
    search = f"<{tag}"
    i = 0
    while True:
        idx = text.find(search, i)
        if idx == -1:
            break
        j = idx + len(search)
        brace_depth = 0
        quote = None
        while j < len(text):
            ch = text[j]
            if quote:
                if ch == quote and text[j - 1] != "\\":
                    quote = None
            else:
                if ch in ('"', "'"):
                    quote = ch
                elif ch == "{":
                    brace_depth += 1
                elif ch == "}" and brace_depth > 0:
                    brace_depth -= 1
                elif ch == ">" and brace_depth == 0:
                    results.append(text[idx:j + 1])
                    i = j + 1
                    break
            j += 1
        else:
            break
    return results

def parse_outputs_for_file(file_path: Path):
    text = read_text(file_path)
    imports = parse_imports(text)
    outputs = {}
    for name, import_path in imports.items():
        tag_instances = find_tag_instances(text, name)
        if not tag_instances:
            continue
        props = []
        for instance in tag_instances:
            attrs = ATTR_RE.findall(instance)
            attrs = [a for a in attrs if a not in ("className", "key", "ref", "children", "dangerouslySetInnerHTML")]
            props.extend(attrs)
            spreads = re.findall(r'{\.\.\.([^}]+)}', instance)
            props.extend([f"...{s.strip()}" for s in spreads])

        deduped = []
        for prop in props:
            if prop not in deduped:
                deduped.append(prop)

        resolved = resolve_import(file_path, import_path)
        outputs[name] = {
            "props": deduped,
            "path": resolved.relative_to(root).as_posix() if resolved else import_path,
            "resolved": bool(resolved),
        }
    return outputs

def component_name(text: str, path: Path):
    match = NAME_RE.search(text)
    return match.group(1) if match else path.stem

meta_cache = {}
def get_meta(path: Path):
    if path not in meta_cache:
        text = read_text(path)
        meta_cache[path] = {
            "name": component_name(text, path),
            "inputs": parse_inputs(text),
            "outputs": parse_outputs_for_file(path),
            "path": path.relative_to(root).as_posix(),
        }
    return meta_cache[path]

def build_tree(path: Path, seen=None):
    if seen is None:
        seen = set()
    meta = get_meta(path)
    node = {
        "name": meta["name"],
        "path": meta["path"],
        "inputs": meta["inputs"],
        "children": [],
    }
    if meta["path"] in seen:
        node["cycle"] = True
        return node
    next_seen = set(seen)
    next_seen.add(meta["path"])
    for import_name, output in meta["outputs"].items():
        if output["resolved"] and output["path"].endswith(".tsx"):
            child_path = root / output["path"]
            child = build_tree(child_path, next_seen)
            child["edgeProps"] = output["props"]
            child["edgeFrom"] = meta["name"]
            child["importName"] = import_name
            node["children"].append(child)
    return node

def chip_width(label: str) -> int:
    return min(170, max(44, 22 + len(label) * 7))

def flow_rows(labels, width):
    if not labels:
        return 1
    rows = 1
    current = 0
    usable = max(80, int(width))
    for label in labels:
        w = chip_width(label) + 6
        if current and current + w > usable:
            rows += 1
            current = w
        else:
            current += w
    return rows

def compute_heights(node):
    top_rows = flow_rows(node.get("inputs", []), BOX_W - 24)
    top_h = max(58, 18 + top_rows * CHIP_H)

    if node.get("children"):
        child_count = len(node["children"])
        seg_w = (BOX_W - 4 * max(0, child_count - 1)) / child_count
        max_seg_rows = 1
        for child in node["children"]:
            max_seg_rows = max(max_seg_rows, flow_rows(child.get("edgeProps", []), seg_w - 18))
        bottom_h = max(72, 34 + max_seg_rows * CHIP_H)
    else:
        bottom_h = 58

    node["_top_h"] = top_h
    node["_mid_h"] = NODE_H_BASE
    node["_bottom_h"] = bottom_h
    node["_h"] = top_h + NODE_H_BASE + bottom_h

    for child in node.get("children", []):
        compute_heights(child)

def count_leaves(node):
    if not node.get("children"):
        node["_leaf_count"] = 1
        return 1
    total = 0
    for child in node["children"]:
        total += count_leaves(child)
    node["_leaf_count"] = total
    return total

def assign_depths(node, depth=0, by_depth=None):
    if by_depth is None:
        by_depth = {}
    node["_depth"] = depth
    by_depth.setdefault(depth, []).append(node)
    for child in node.get("children", []):
        assign_depths(child, depth + 1, by_depth)
    return by_depth

def compute_level_y(by_depth):
    levels = sorted(by_depth.keys())
    y_map = {}
    y = CANVAS_PAD
    for depth in levels:
        y_map[depth] = y
        max_h = max(node["_h"] for node in by_depth[depth])
        y += max_h + LEVEL_GAP
    return y_map

def assign_x_ranges(node, left):
    span = node["_leaf_count"] * LEAF_SLOT_W
    center = left + span / 2
    node["_x"] = center - BOX_W / 2

    current_left = left
    for child in node.get("children", []):
        assign_x_ranges(child, current_left)
        current_left += child["_leaf_count"] * LEAF_SLOT_W

def apply_y_positions(node, y_map):
    node["_y"] = y_map[node["_depth"]]
    for child in node.get("children", []):
        apply_y_positions(child, y_map)

def flatten(node, nodes=None, edges=None):
    if nodes is None:
        nodes = []
    if edges is None:
        edges = []
    nodes.append(node)
    child_count = len(node.get("children", []))
    for index, child in enumerate(node.get("children", [])):
        flatten(child, nodes, edges)
        seg_w = BOX_W / max(1, child_count)
        start_x = node["_x"] + seg_w * index + seg_w / 2
        start_y = node["_y"] + node["_h"] + 2
        end_x = child["_x"] + BOX_W / 2
        end_y = child["_y"] - 2
        bend_y = start_y + (end_y - start_y) / 2
        edges.append({
            "x1": start_x,
            "y1": start_y,
            "x2": end_x,
            "y2": end_y,
            "bend_y": bend_y,
        })
    return nodes, edges

def shift_all(nodes, dx=0):
    for node in nodes:
        node["_x"] += dx

def render_node(node):
    inputs_html = "".join(f'<span class="chip top-chip">{html.escape(item)}</span>' for item in node["inputs"]) if node.get("inputs") else '<span class="muted">none</span>'

    if node.get("children"):
        segments = []
        for child in node["children"]:
            props = child.get("edgeProps", [])
            props_html = "".join(f'<span class="chip bottom-chip">{html.escape(p)}</span>' for p in props) if props else '<span class="muted">no props</span>'
            segments.append(f'''
                <div class="output-segment">
                    <div class="segment-title">{html.escape(child["name"])}</div>
                    <div class="segment-props">{props_html}</div>
                </div>
            ''')
        outputs_html = "".join(segments)
    else:
        outputs_html = '<div class="output-empty">no child outputs</div>'

    return f'''
    <div class="node" style="left:{node["_x"]}px; top:{node["_y"]}px; width:{BOX_W}px; height:{node["_h"]}px;">
        <div class="top-zone" style="min-height:{node["_top_h"]}px;">{inputs_html}</div>
        <div class="info-zone" style="min-height:{node["_mid_h"]}px;">
            <div class="component-name">{html.escape(node["name"])}</div>
            <div class="component-path">{html.escape(node["path"])}</div>
        </div>
        <div class="bottom-zone" style="min-height:{node["_bottom_h"]}px; grid-template-columns: repeat({max(1, len(node.get("children", [])))}, minmax(0, 1fr));">
            {outputs_html}
        </div>
    </div>
    '''

def render_edge(edge):
    x1, y1, x2, y2, by = edge["x1"], edge["y1"], edge["x2"], edge["y2"], edge["bend_y"]
    d = f"M {x1} {y1} L {x1} {by} L {x2} {by} L {x2} {y2}"
    return f'<path d="{d}" class="edge-path"></path>'

entry_candidates = [
    ("Dashboard / Home", root / "page.tsx"),
    ("Events", root / "Events" / "page.tsx"),
    ("Notes", root / "Notes" / "page.tsx"),
    ("Todos", root / "Todos" / "page.tsx"),
    ("Root Layout", root / "layout.tsx"),
]

sections = []
for title, path in entry_candidates:
    if not path.exists():
        continue

    tree = build_tree(path)
    compute_heights(tree)
    count_leaves(tree)
    by_depth = assign_depths(tree)
    y_map = compute_level_y(by_depth)
    assign_x_ranges(tree, CANVAS_PAD)
    apply_y_positions(tree, y_map)
    nodes, edges = flatten(tree)

    min_x = min(node["_x"] for node in nodes)
    if min_x < CANVAS_PAD:
        shift_all(nodes, CANVAS_PAD - min_x)

    width = max(node["_x"] + BOX_W for node in nodes) + CANVAS_PAD
    height = max(node["_y"] + node["_h"] for node in nodes) + CANVAS_PAD
    sections.append({"title": title, "nodes": nodes, "edges": edges, "width": width, "height": height})

sections_html = []
for section in sections:
    nodes_html = "".join(render_node(node) for node in section["nodes"])
    edges_html = "".join(render_edge(edge) for edge in section["edges"])
    sections_html.append(f'''
        <section class="section">
            <div class="section-head">
                <h2>{html.escape(section["title"])}</h2>
                <div class="section-meta">{len(section["nodes"])} Komponenten · {len(section["edges"])} Verbindungen</div>
            </div>
            <div class="canvas-wrap">
                <div class="canvas" style="width:{section["width"]}px; height:{section["height"]}px;">
                    <svg class="edges" width="{section["width"]}" height="{section["height"]}" viewBox="0 0 {section["width"]} {section["height"]}">
                        {edges_html}
                    </svg>
                    {nodes_html}
                </div>
            </div>
        </section>
    ''')

html_doc = f"""<!doctype html>
<html lang="de">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Component Flow – App (layout pass)</title>
<style>
    :root {{
        --bg: #0f1117;
        --panel: #171a22;
        --line: #2e3447;
        --text: #eef2ff;
        --muted: #9da7c7;

        --imports: #1db954;
        --imports-bg: rgba(29, 185, 84, 0.09);

        --info: #ffffff;
        --info-bg: rgba(255, 255, 255, 0.05);

        --outputs: #c61d3a;
        --outputs-bg: rgba(198, 29, 58, 0.10);
    }}

    * {{ box-sizing: border-box; }}
    body {{
        margin: 0;
        font-family: Inter, Arial, sans-serif;
        color: var(--text);
        background: linear-gradient(180deg, #0d1016 0%, var(--bg) 100%);
    }}
    .page {{
        max-width: 100%;
        padding: 28px;
        display: grid;
        gap: 22px;
    }}
    .hero, .section {{
        background: rgba(255, 255, 255, 0.03);
        border: 1px solid var(--line);
        border-radius: 20px;
        padding: 18px;
        box-shadow: 0 22px 60px rgba(0, 0, 0, 0.25);
    }}
    h1 {{ margin: 0 0 10px; font-size: 30px; }}
    h2 {{ margin: 0; font-size: 20px; }}
    p {{ margin: 0; color: var(--muted); line-height: 1.55; }}
    .legend {{
        margin-top: 14px;
        display: flex;
        flex-wrap: wrap;
        gap: 10px;
    }}
    .legend-item {{
        display: inline-flex;
        gap: 8px;
        align-items: center;
        padding: 8px 10px;
        border-radius: 999px;
        background: var(--panel);
        border: 1px solid var(--line);
        color: var(--muted);
        font-size: 13px;
    }}
    .legend-swatch {{
        width: 14px;
        height: 14px;
        border-radius: 4px;
    }}
    .section-head {{
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        gap: 12px;
        margin-bottom: 16px;
        flex-wrap: wrap;
    }}
    .section-meta {{
        color: var(--muted);
        font-size: 13px;
    }}
    .canvas-wrap {{
        overflow: auto;
        border-radius: 16px;
        background: #10131b;
        border: 1px solid #23283a;
        padding: 22px;
    }}
    .canvas {{
        position: relative;
    }}
    .edges {{
        position: absolute;
        inset: 0;
        overflow: visible;
        pointer-events: none;
    }}
    .edge-path {{
        fill: none;
        stroke: rgba(255, 255, 255, 0.28);
        stroke-width: 2.5;
        stroke-linejoin: round;
        stroke-linecap: round;
    }}
    .node {{
        position: absolute;
    }}
    .top-zone {{
        border: 3px solid var(--imports);
        border-bottom: 0;
        background: var(--imports-bg);
        border-top-left-radius: 14px;
        border-top-right-radius: 14px;
        padding: 10px 12px;
        display: flex;
        flex-wrap: wrap;
        align-content: flex-start;
        gap: 8px;
    }}
    .info-zone {{
        background: var(--info-bg);
        border: 3px solid var(--info);
        display: grid;
        place-items: center;
        text-align: center;
        padding: 12px 14px;
    }}
    .bottom-zone {{
        border: 3px solid var(--outputs);
        border-top: 0;
        background: var(--outputs-bg);
        border-bottom-left-radius: 14px;
        border-bottom-right-radius: 14px;
        display: grid;
    }}
    .output-segment {{
        border-right: 1px solid rgba(198, 29, 58, 0.45);
        padding: 10px 10px 12px;
        min-width: 0;
    }}
    .output-segment:last-child {{
        border-right: 0;
    }}
    .component-name {{
        font-size: 17px;
        font-weight: 800;
        color: #ffffff;
        margin-bottom: 6px;
    }}
    .component-path {{
        font-size: 11px;
        color: var(--muted);
        word-break: break-word;
    }}
    .segment-title {{
        font-size: 12px;
        font-weight: 700;
        color: #ffb8c6;
        margin-bottom: 8px;
    }}
    .segment-props {{
        display: flex;
        flex-wrap: wrap;
        gap: 6px;
        align-content: flex-start;
        min-height: 24px;
    }}
    .output-empty {{
        padding: 10px 12px;
        color: var(--muted);
        font-size: 12px;
        display: flex;
        align-items: center;
    }}
    .chip {{
        display: inline-flex;
        align-items: center;
        padding: 5px 8px;
        border-radius: 999px;
        font-size: 12px;
        line-height: 1;
        white-space: nowrap;
    }}
    .top-chip {{
        background: rgba(29, 185, 84, 0.16);
        color: #d7ffe6;
        border: 1px solid rgba(126, 240, 164, 0.35);
    }}
    .bottom-chip {{
        background: rgba(198, 29, 58, 0.16);
        color: #ffd7de;
        border: 1px solid rgba(255, 133, 158, 0.35);
    }}
    .muted {{
        color: var(--muted);
        font-size: 12px;
    }}
</style>
</head>
<body>
<main class="page">
    <section class="hero">
        <h1>Component Flow für deine App</h1>
        <p>Neue Layout-Pass-Version: Komponenten werden jetzt über Leaf-Slots und Ebenen verteilt statt nur grob gestapelt. Dadurch ist der Platz deutlich planbarer. Die Info-Zone ist jetzt weiß statt gelb.</p>
        <div class="legend">
            <div class="legend-item"><span class="legend-swatch" style="background: var(--imports);"></span> Imports / Inputs</div>
            <div class="legend-item"><span class="legend-swatch" style="background: var(--info);"></span> Info</div>
            <div class="legend-item"><span class="legend-swatch" style="background: var(--outputs);"></span> Outputs pro Child</div>
        </div>
    </section>
    {''.join(sections_html)}
</main>
</body>
</html>
"""

out = Path("/mnt/data/component_flow_app_layoutpass.html")
out.write_text(html_doc, encoding="utf-8")
print(f"Created {out}")
