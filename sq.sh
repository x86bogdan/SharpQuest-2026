#!/usr/bin/env sh
# SharpQuest helper for Linux and macOS. Usage: sh sq.sh test | sh sq.sh update | sh sq.sh upload | sh sq.sh help
# MANAGED FILE: `sq update` and CI replace it with the official copy.
#
# Finds dotnet even when it isn't on the PATH: on some lab PCs it only works inside Rider's terminal.

DOTNET=$(command -v dotnet 2>/dev/null)
if [ -z "$DOTNET" ]; then
  for dir in "$DOTNET_ROOT" "$HOME/.dotnet" /usr/share/dotnet /usr/lib/dotnet /usr/lib64/dotnet /opt/dotnet /usr/local/share/dotnet; do
    if [ -n "$dir" ] && [ -x "$dir/dotnet" ]; then DOTNET="$dir/dotnet"; break; fi
  done
fi
if [ -z "$DOTNET" ]; then
  echo "sq: can't find dotnet. On a lab PC, run this in Rider's terminal (View > Tool Windows > Terminal)." >&2
  exit 127
fi

# The programs dotnet starts (sq itself, then dotnet test) need to find it too.
REAL=$(readlink -f "$DOTNET" 2>/dev/null || echo "$DOTNET")
if [ -z "$DOTNET_ROOT" ] && [ -d "$(dirname "$REAL")/sdk" ]; then
  DOTNET_ROOT=$(dirname "$REAL"); export DOTNET_ROOT
fi
PATH="$(dirname "$REAL"):$PATH"; export PATH

exec "$DOTNET" run --project "$(dirname "$0")/tools/sq" -- "$@"
