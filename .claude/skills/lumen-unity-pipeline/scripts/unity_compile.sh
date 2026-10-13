#!/usr/bin/env bash
# Compila o projeto Unity em modo batch e mostra so os erros/avisos de C#.
# Uso:  bash unity_compile.sh [caminho-do-projeto]
# Env:  UNITY_PATH=/caminho/para/Unity(.exe)  -> forca um Editor especifico
# Saida: 0 = compilou sem erros | 1 = erros de compilacao | 2 = nao foi possivel rodar o Unity

set -u

PROJECT="${1:-$(git rev-parse --show-toplevel 2>/dev/null || pwd)}"
VERSION_FILE="$PROJECT/ProjectSettings/ProjectVersion.txt"

if [ ! -f "$VERSION_FILE" ]; then
  echo "[unity_compile] Nao achei $VERSION_FILE - este nao parece um projeto Unity." >&2
  exit 2
fi

VERSION="$(grep -m1 'm_EditorVersion:' "$VERSION_FILE" | awk '{print $2}')"
echo "[unity_compile] Projeto: $PROJECT"
echo "[unity_compile] Editor esperado: $VERSION"

find_unity() {
  if [ -n "${UNITY_PATH:-}" ] && [ -e "$UNITY_PATH" ]; then echo "$UNITY_PATH"; return; fi
  local candidates=(
    "/c/Program Files/Unity/Hub/Editor/$VERSION/Editor/Unity.exe"
    "/mnt/c/Program Files/Unity/Hub/Editor/$VERSION/Editor/Unity.exe"
    "C:/Program Files/Unity/Hub/Editor/$VERSION/Editor/Unity.exe"
    "/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity"
    "$HOME/Unity/Hub/Editor/$VERSION/Editor/Unity"
    "/opt/unity/$VERSION/Editor/Unity"
  )
  for c in "${candidates[@]}"; do
    if [ -e "$c" ]; then echo "$c"; return; fi
  done
  command -v unity-editor 2>/dev/null || command -v Unity 2>/dev/null || true
}

UNITY="$(find_unity)"
if [ -z "$UNITY" ]; then
  echo "[unity_compile] Unity $VERSION nao encontrado neste computador." >&2
  echo "[unity_compile] Defina UNITY_PATH=/caminho/do/Unity ou rode static_check.py e avise o usuario" >&2
  echo "[unity_compile] que a compilacao real nao foi verificada." >&2
  exit 2
fi

if [ -e "$PROJECT/Temp/UnityLockfile" ] && pgrep -f "Unity" >/dev/null 2>&1; then
  echo "[unity_compile] O Unity parece estar aberto neste projeto (Temp/UnityLockfile)." >&2
  echo "[unity_compile] Feche o Editor ou use o Console dele; o modo batch nao abre uma 2a instancia." >&2
  exit 2
fi

LOG="$(mktemp 2>/dev/null || echo /tmp/unity_compile.log)"
echo "[unity_compile] Compilando com: $UNITY"
"$UNITY" -batchmode -nographics -quit -projectPath "$PROJECT" -logFile "$LOG" >/dev/null 2>&1
UNITY_EXIT=$?

# Erros de C# aparecem como: Assets/.../Arquivo.cs(12,34): error CS1002: ; expected
ERRORS="$(grep -E "error CS[0-9]+" "$LOG" | sort -u)"
WARNINGS="$(grep -E "warning CS[0-9]+" "$LOG" | sort -u | head -20)"

if [ -n "$WARNINGS" ]; then
  echo "---- avisos (ate 20) ----"
  echo "$WARNINGS"
fi

if [ -n "$ERRORS" ]; then
  echo "---- ERROS DE COMPILACAO ----"
  echo "$ERRORS"
  echo "[unity_compile] Log completo: $LOG"
  exit 1
fi

if [ "$UNITY_EXIT" -ne 0 ]; then
  echo "[unity_compile] Unity terminou com codigo $UNITY_EXIT sem 'error CS' no log." >&2
  echo "[unity_compile] Ultimas linhas do log ($LOG):" >&2
  tail -n 25 "$LOG" >&2
  exit 2
fi

echo "[unity_compile] OK - compilou sem erros de C#."
exit 0
