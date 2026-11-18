# AuthFix

Ferramenta de linha de comando que propõe e valida correções de falhas de autorização (CWE-862 e CWE-863) em APIs ASP.NET Core. Ela roda dentro do projeto analisado, que fica em seu próprio repositório Git. O caso usado no TCC é a DemoApi (`~/Documents/AuthFix-Demo-API`).

## Instalação

Requisitos: .NET 10, Git, Docker em execução e as imagens `semgrep/semgrep` e `mcr.microsoft.com/dotnet/sdk:10.0`.

```sh
dotnet pack src/AuthFix/AuthFix.csproj -c Release
dotnet tool install -g AuthFix --add-source ./nupkg
```

Para atualizar depois de mudar o código, gere o pacote de novo com outra `Version` no `AuthFix.csproj` e use `dotnet tool update -g AuthFix --add-source ./nupkg`. Se o nuget.org estiver inacessível, use um `nuget.config` que tenha só a pasta `nupkg` como origem (`--configfile`).

A pasta `~/.dotnet/tools` precisa estar no `PATH`. Com o .NET do Homebrew, defina também o `DOTNET_ROOT`:

```sh
export PATH="$PATH:$HOME/.dotnet/tools"
export DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec
```

## Uso

Na pasta do projeto (raiz do repositório Git):

```sh
authfix scan
```

1. **Análise estática:** executa o Semgrep em contêiner, com o projeto montado como somente leitura e as regras embutidas em `Regras/`:
   - `aspnet-controller-sem-autorizacao`: action com `[Http*]` sem `[Authorize]`/`[AllowAnonymous]`, num controlador sem `[Authorize]`;
   - `aspnet-minimal-api-sem-autorizacao`: `Map*` sem `RequireAuthorization`/`AllowAnonymous`.

   As duas regras são mapeadas para CWE-862.
2. **Localização:** o Roslyn identifica o método ou a instrução de Minimal API do alerta.
3. **Contexto:** na condição C, recupera declarações de autorização dos outros arquivos versionados, ignorando testes e `avaliacao/`.
4. **Proposta:** por enquanto vem de `resposta-simulada.json`; o adaptador de LLM entra neste ponto.
5. **Validação:** aplica o diff em um Git worktree descartável, permite alterar apenas o arquivo do alerta e executa `avaliacao/teste.sh` em contêiner sem rede, sem credenciais e com recursos limitados. O avaliador imprime `SEGURANCA: APROVADO` e `FUNCIONALIDADE: APROVADO`.
6. **Saída:** mostra o alerta, as evidências, o diff e os resultados, e grava `alerta.sarif`, `relatorio.json`, `relatorio.md` e `correcao.patch` em `.authfix/`.

O projeto não é alterado sem pedido. Para aplicar a correção aprovada:

```sh
authfix scan --aplicar
```

| Opção | Padrão | Uso |
|---|---|---|
| `--condicao A\|B\|C` | `C` | contexto enviado ao gerador: método, arquivo, ou arquivo e evidências |
| `--alerta ARQ` | executa o Semgrep | usa um SARIF 2.1.0 existente |
| `--regras ARQ` | mapa embutido | mapa JSON regra → CWE para o SARIF informado |
| `--resposta ARQ` | `resposta-simulada.json` | proposta simulada |
| `--avaliacao DIR` | `avaliacao` | pasta com `teste.sh`; sem ela, a validação não é executada |
| `--saida DIR` | `.authfix` | relatórios e patch |
| `--aplicar` | desligado | aplica a correção se segurança e funcionalidade forem aprovadas |

A validação exige que o checkout corresponda ao `HEAD`. Faça commit antes de rodar. Código de saída: `0` quando não há alertas ou todas as correções foram aprovadas, `3` quando alguma correção não foi aprovada e `1` em erro.

## Testes

```sh
dotnet test tests/AuthFix.Tests/AuthFix.Tests.csproj
```

## Limitações

A geração ainda usa uma resposta simulada, a mesma para todos os alertas e condições. A recuperação usa marcadores sintáticos e textuais, sem resolução semântica de símbolos. A cobertura de Minimal API se limita a instruções de topo, sem `MapGroup`. A aderência depende de revisão humana. Os resultados da validação dependem da qualidade de `teste.sh` e da integridade dos marcadores que ele imprime.
