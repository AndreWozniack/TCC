#!/bin/sh
set -eu
cd /work
dotnet build DemoApi.csproj --nologo --verbosity quiet
dotnet bin/Debug/net10.0/DemoApi.dll --urls http://127.0.0.1:5079 >/tmp/demo-api.log 2>&1 &
pid=$!
trap 'kill "$pid" 2>/dev/null || true' EXIT
n=0
until curl -s -o /dev/null http://127.0.0.1:5079/api/oi; do
  n=$((n+1))
  if [ "$n" -gt 30 ]; then cat /tmp/demo-api.log; exit 2; fi
  sleep 1
done
anonimo=$(curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:5079/api/admin-info)
usuario=$(curl -s -o /dev/null -w '%{http_code}' -H 'X-Demo-Perfil: User' http://127.0.0.1:5079/api/admin-info)
admin=$(curl -s -o /dev/null -w '%{http_code}' -H 'X-Demo-Perfil: Admin' http://127.0.0.1:5079/api/admin-info)
[ "$anonimo" = 401 ] && [ "$usuario" = 403 ] || { echo 'SEGURANCA: FALHOU'; exit 1; }
echo 'SEGURANCA: APROVADO'
[ "$admin" = 200 ] || { echo 'FUNCIONALIDADE: FALHOU'; exit 1; }
echo 'FUNCIONALIDADE: APROVADO'
