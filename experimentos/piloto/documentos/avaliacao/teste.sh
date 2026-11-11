#!/bin/sh
set -eu
cd /work
dotnet build Piloto.csproj --nologo --verbosity quiet
dotnet bin/Debug/net10.0/Piloto.dll --urls http://127.0.0.1:5078 >/tmp/piloto.log 2>&1 &
pid=$!
trap 'kill "$pid" 2>/dev/null || true' EXIT
n=0
until curl -s -o /dev/null http://127.0.0.1:5078/documents; do
    n=$((n+1))
    if [ "$n" -gt 30 ]; then cat /tmp/piloto.log; exit 2; fi
    sleep 1
done
unauthorized=$(curl -s -o /dev/null -w '%{http_code}' -X PUT -H 'X-Pilot-User: bob' -H 'Content-Type: application/json' -d '"indevido"' http://127.0.0.1:5078/documents)
after=$(curl -s -H 'X-Pilot-User: alice' http://127.0.0.1:5078/documents)
authorized=$(curl -s -o /dev/null -w '%{http_code}' -X PUT -H 'X-Pilot-User: alice' -H 'Content-Type: application/json' -d '"atualizado"' http://127.0.0.1:5078/documents)
final=$(curl -s -H 'X-Pilot-User: alice' http://127.0.0.1:5078/documents)
[ "$unauthorized" = 403 ] && [ "$after" = inicial ] || { echo "SEGURANCA: FALHOU"; exit 1; }
echo "SEGURANCA: APROVADO"
[ "$authorized" = 204 ] && [ "$final" = atualizado ] || { echo "FUNCIONALIDADE: FALHOU"; exit 1; }
echo "FUNCIONALIDADE: APROVADO"
