#!/bin/sh
set -eu
if [ "$#" -ne 1 ]; then
    echo "Uso: sh experimentos/piloto/documentos/executar.sh <pasta-de-resultados>" >&2
    exit 2
fi
repo_root=$(git rev-parse --show-toplevel)
pilot="$repo_root/experimentos/piloto/documentos"
output=$(mkdir -p "$1" && cd "$1" && pwd)
temp=$(mktemp -d)
trap 'rm -rf "$temp"' EXIT
mkdir "$temp/repo"
cp "$pilot"/app/*.cs "$pilot"/app/*.csproj "$temp/repo/"
git -C "$temp/repo" init -q
git -C "$temp/repo" add .
git -C "$temp/repo" -c user.name=Piloto -c user.email=piloto@example.invalid commit -qm piloto
git -C "$temp/repo" rev-parse HEAD > "$output/commit.txt"
for condicao in A B C; do
    dotnet run --project "$repo_root/src/AuthFix" -- "$temp/repo" "$pilot/manifesto.json" "$condicao" "$pilot/resposta-simulada.json" "$output/$condicao.json" --validate HEAD "$pilot/avaliacao"
done
