#!/bin/sh
# Compila Tailwind CSS v4 con el CLI standalone dentro de Docker (sin Node/npm).
# Uso: ./tailwind.sh [--watch]
set -e
TW_URL=https://github.com/tailwindlabs/tailwindcss/releases/download/v4.3.5/tailwindcss-linux-x64
if [ "$1" = "--watch" ]; then ARGS="--watch"; else ARGS="--minify"; fi
docker run --rm -e OWNER="$(id -u):$(id -g)" -v "$PWD":/src -w /src debian:stable-slim sh -c "
  trap 'chown \$OWNER Matricula.WebUI/wwwroot/css/app.css' EXIT
  apt-get update -qq >/dev/null && apt-get install -y -qq curl ca-certificates >/dev/null 2>&1 &&
  curl -sSL -o /tmp/tw $TW_URL && chmod +x /tmp/tw &&
  /tmp/tw -i Matricula.WebUI/Styles/app.css -o Matricula.WebUI/wwwroot/css/app.css $ARGS"
