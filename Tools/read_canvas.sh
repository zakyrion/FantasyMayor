#!/bin/sh
# read_canvas.sh — project an Obsidian JSONCanvas (.canvas) to just the data an agent needs:
# node id/type/text/label/file + edges (from/to/label). Drops layout noise (x/y/width/height/color).
# This is the DEFAULT way to read a .canvas (cheap); use a full vault_read / Read only to edit layout.
#
#   Tools/read_canvas.sh ENTITIES.canvas
#
# Obsidian MCP cannot project inside a JSON canvas (it returns the raw file), so this is the projection.
set -e
if [ -z "$1" ]; then
  echo "usage: read_canvas.sh <file.canvas>" >&2
  exit 2
fi
jq '{
  nodes: [ .nodes[] | {id, type, text, label, file} | with_entries(select(.value != null)) ],
  edges: [ .edges[] | {from: .fromNode, to: .toNode, label} | with_entries(select(.value != null)) ]
}' "$1"
