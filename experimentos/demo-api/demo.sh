#!/bin/sh
# Demonstração: Semgrep acha a falha -> AuthFix propõe e valida a correção -> aplica.
# Uso: sh demo.sh          (pausa entre etapas; Enter para avançar)
#      sh demo.sh reset    (desfaz a correção e volta ao estado vulnerável)
#
# A API fica dentro do repositório do TCC, mas o AuthFix valida o patch num
# Git worktree do repositório informado e monta a raiz dele em /work. Por isso
# o script copia a API para um repositório Git temporário (como no piloto) e
# roda o AuthFix contra essa cópia. A correção aprovada é aplicada depois aqui.
set -eu
cd "$(dirname "$0")"
DEMO=$(pwd)
TCC=$(git rev-parse --show-toplevel)
AUTHFIX=${AUTHFIX:-$TCC/src/AuthFix}
PAUSA=${PAUSA:-1}
ALVO=Controllers/InformacoesController.cs

titulo() { printf '\n\033[1;36m==== %s ====\033[0m\n' "$1"; }
pausa()  { [ "$PAUSA" = 0 ] || { printf '\033[2m[Enter para continuar]\033[0m'; read -r _; }; }
semgrep() {
    docker run --rm -v "$DEMO:/src:ro" -v "$DEMO/saida:/saida" -w /src \
        semgrep/semgrep semgrep scan --metrics=off --config /src/semgrep/regra-demo.yml \
        --sarif-output=/saida/semgrep.sarif
}

# Volta o controlador à versão vulnerável registrada no repositório do TCC.
if git ls-files --error-unmatch "$ALVO" >/dev/null 2>&1; then
    git checkout -q -- "$ALVO"
fi
rm -rf saida && mkdir saida
if [ "${1:-}" = reset ]; then echo "Demo resetada (versão vulnerável)."; exit 0; fi

# Repositório temporário só com a API, para o AuthFix criar o worktree de validação.
repo=$(mktemp -d)
trap 'rm -rf "$repo"' EXIT
cp -R DemoApi.csproj Program.cs Controllers Seguranca "$repo/"
git -C "$repo" init -q
git -C "$repo" add .
git -C "$repo" -c user.name=Demo -c user.email=demo@example.invalid commit -qm demo

titulo "1. Semgrep (contêiner) analisa o código"
semgrep || true
# Normaliza o SARIF para o AuthFix: caminho relativo (sem uriBaseId) e id da regra sem o prefixo da pasta.
jq 'del(.runs[].results[].locations[].physicalLocation.artifactLocation.uriBaseId)
    | .runs[].results[].ruleId |= sub("^.*\\."; "")' saida/semgrep.sarif > saida/alerta.sarif
echo; jq -r '.runs[].results[] | "Alerta: \(.ruleId)  \(.locations[0].physicalLocation.artifactLocation.uri):\(.locations[0].physicalLocation.region.startLine)"' saida/alerta.sarif
pausa

titulo "2. AuthFix: localiza o endpoint, recupera contexto, gera e valida o patch (contêiner sem rede)"
dotnet run --project "$AUTHFIX" -- "$repo" "$DEMO/saida/alerta.sarif" C "$DEMO/resposta-simulada.json" \
    "$DEMO/saida/relatorio.json" --rules "$DEMO/semgrep/regras.json" --validate HEAD "$DEMO/avaliacao"
echo; echo "Contexto recuperado (condição C):"
jq -r '.Input.Evidence[] | "  - \(.File):\(.Line) \(.Symbol) — \(.Reason)"' saida/relatorio.json
echo; cat saida/relatorio.md
pausa

titulo "3. Aplicando a correção"
jq -r '.Proposal.Diff' saida/relatorio.json | git apply --directory="$(git rev-parse --show-prefix)" -
git --no-pager diff --color -- "$ALVO"
echo
echo "Reinicie a API (Ctrl+C e dotnet run) e chame /api/admin-info de novo: 401 / 403 / 200 (Admin)."
pausa

titulo "4. Semgrep de novo"
semgrep || true
echo; echo "Fim. Para voltar ao estado vulnerável: sh demo.sh reset"
