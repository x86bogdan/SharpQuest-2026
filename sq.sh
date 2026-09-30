#!/usr/bin/env sh
# SharpQuest helper for Linux and macOS. Usage: ./sq.sh test | ./sq.sh update | ./sq.sh help
exec dotnet run --file "$(dirname "$0")/sq.cs" -- "$@"
