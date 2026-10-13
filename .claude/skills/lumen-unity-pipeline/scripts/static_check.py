#!/usr/bin/env python3
"""Checagem estatica barata dos scripts do LUMEN, para quando o Unity nao esta disponivel.

NAO substitui a compilacao real. Pega os erros que mais quebram build/jogo neste projeto:
  - `using UnityEditor` em codigo de runtime (quebra o build do jogo);
  - APIs obsoletas no Unity 6 (FindObjectOfType & cia);
  - chaves/parenteses desbalanceados;
  - namespace fora do padrao Lumen.*;
  - Find*/GetComponent dentro de Update/LateUpdate/FixedUpdate (custo por quadro).

Uso: python3 static_check.py [pasta-de-scripts]   (padrao: Assets/_Project/Scripts)
Saida: 0 = sem erros (pode haver avisos) | 1 = ha erros
"""
import re
import sys
from pathlib import Path

ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else "Assets/_Project/Scripts")

DEPRECATED = {
    "FindObjectOfType": "FindFirstObjectByType",
    "FindObjectsOfType": "FindObjectsByType(..., FindObjectsSortMode.None)",
}
PER_FRAME_METHODS = ("Update", "LateUpdate", "FixedUpdate")
COSTLY_CALLS = re.compile(
    r"\b(GameObject\.Find\w*|FindFirstObjectByType|FindObjectsByType|FindAnyObjectByType|"
    r"Resources\.Load|GetComponentInChildren|GetComponentsInChildren)\b"
)


def strip_noise(text: str) -> str:
    """Remove comentarios e conteudo de strings, preservando quebras de linha."""
    out, i, n = [], 0, len(text)
    while i < n:
        c = text[i]
        two = text[i:i + 2]
        if two == "//":
            while i < n and text[i] != "\n":
                i += 1
        elif two == "/*":
            j = text.find("*/", i + 2)
            j = n if j == -1 else j + 2
            out.append("\n" * text.count("\n", i, j))
            i = j
        elif c == '"':
            verbatim = i > 0 and text[i - 1] == "@"
            i += 1
            while i < n:
                if verbatim:
                    if text[i] == '"' and text[i + 1:i + 2] == '"':
                        i += 2
                        continue
                    if text[i] == '"':
                        break
                else:
                    if text[i] == "\\":
                        i += 2
                        continue
                    if text[i] == '"' or text[i] == "\n":
                        break
                if text[i] == "\n":
                    out.append("\n")
                i += 1
            i += 1
            out.append('""')
        elif c == "'":
            j = i + 1
            while j < n and text[j] != "'" and text[j] != "\n":
                j += 2 if text[j] == "\\" else 1
            i = j + 1
            out.append("' '")
        else:
            out.append(c)
            i += 1
    return "".join(out)


def per_frame_bodies(code: str):
    """Gera (linha_inicial, trecho) de cada metodo Update/LateUpdate/FixedUpdate."""
    for m in re.finditer(r"\b(?:void|IEnumerator)\s+(%s)\s*\(\s*\)\s*\{" % "|".join(PER_FRAME_METHODS), code):
        depth, i = 1, m.end()
        while i < len(code) and depth:
            depth += {"{": 1, "}": -1}.get(code[i], 0)
            i += 1
        yield code.count("\n", 0, m.start()) + 1, code[m.end():i - 1], m.group(1)


def check_file(path: Path):
    errors, warnings = [], []
    raw = path.read_text(encoding="utf-8", errors="replace")
    code = strip_noise(raw)
    rel = path.as_posix()
    in_editor_folder = "/Editor/" in rel or rel.startswith("Editor/")

    for open_c, close_c, name in (("{", "}", "chaves"), ("(", ")", "parenteses"), ("[", "]", "colchetes")):
        if code.count(open_c) != code.count(close_c):
            errors.append(f"{rel}: {name} desbalanceados(as) ({code.count(open_c)} '{open_c}' x {code.count(close_c)} '{close_c}')")

    if re.search(r"^\s*using\s+UnityEditor\b", raw, re.M) and not in_editor_folder:
        if "#if UNITY_EDITOR" not in raw:
            errors.append(f"{rel}: 'using UnityEditor' fora de pasta Editor e sem #if UNITY_EDITOR - quebra o build do jogo")

    ns = re.search(r"^\s*namespace\s+([\w.]+)", raw, re.M)
    if ns is None:
        if "MonoBehaviour" in raw or "ScriptableObject" in raw:
            warnings.append(f"{rel}: classe Unity sem namespace (padrao do projeto: Lumen.<Area>)")
    elif not ns.group(1).startswith("Lumen"):
        warnings.append(f"{rel}: namespace '{ns.group(1)}' fora do padrao Lumen.*")

    for old, new in DEPRECATED.items():
        for m in re.finditer(r"\b%s\b" % old, code):
            line = code.count("\n", 0, m.start()) + 1
            errors.append(f"{rel}:{line}: {old} e obsoleto no Unity 6 - use {new}")

    for start, body, method in per_frame_bodies(code):
        for m in COSTLY_CALLS.finditer(body):
            line = start + body.count("\n", 0, m.start())
            warnings.append(f"{rel}:{line}: {m.group(0)} dentro de {method}() - cacheie no Awake/Start (custo por quadro)")

    return errors, warnings


def main():
    if not ROOT.exists():
        print(f"[static_check] Pasta nao encontrada: {ROOT}", file=sys.stderr)
        return 2
    all_err, all_warn = [], []
    files = sorted(ROOT.rglob("*.cs"))
    for f in files:
        e, w = check_file(f)
        all_err += e
        all_warn += w
    print(f"[static_check] {len(files)} arquivos .cs analisados em {ROOT}")
    for w in all_warn:
        print("  AVISO ", w)
    for e in all_err:
        print("  ERRO  ", e)
    if all_err:
        print(f"[static_check] {len(all_err)} erro(s). Lembrete: isto NAO e a compilacao real.")
        return 1
    print("[static_check] Sem erros encontrados. Lembrete: isto NAO e a compilacao real do Unity.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
