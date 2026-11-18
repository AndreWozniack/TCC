# AuthFix como ferramenta independente (`authfix scan`)

## Intenção

O AuthFix é o artefato do TCC. Ele deve ser uma ferramenta independente, instalada globalmente, executada dentro do projeto analisado com um único comando. O desenvolvedor entra no projeto (a DemoApi, fora do repositório do TCC) e roda `authfix scan`. A ferramenta executa a análise estática, localiza o endpoint, recupera o contexto, obtém a proposta, valida a correção e mostra o diff e os resultados no terminal.

## Objetivos

- `dotnet tool` global com o comando `authfix`.
- `authfix scan` sem argumentos obrigatórios. Ele usa convenções de pasta do projeto.
- O Semgrep roda pelo próprio AuthFix (Docker), com regras genéricas de autorização ASP.NET embutidas na ferramenta e já mapeadas para CWE-862.
- O código continua genérico para ASP.NET Core (controllers e Minimal APIs), sem trechos específicos da DemoApi.
- As condições A/B/C continuam disponíveis para o experimento.

## Fora do escopo

- Adaptador de LLM. A geração continua usando `resposta-simulada.json` até a integração.
- Suporte a projeto em subpasta de outro repositório. O AuthFix usa a raiz do git da pasta atual.
- Aplicação automática sem pedido. O diff só é aplicado com `--aplicar` e se a validação aprovar segurança e funcionalidade.

## Abordagens consideradas

1. **DemoApi dentro do TCC e AuthFix com suporte a subpasta.** Mantém tudo num repositório, mas acopla o caso ao TCC.
2. **DemoApi em repositório próprio e AuthFix como ferramenta global.** *Escolhida pelo usuário.* É o uso real: a ferramenta roda sobre o projeto de quem a usa.

## Interface

```
authfix scan [--condicao A|B|C] [--alerta arquivo.sarif] [--regras mapa.json]
             [--resposta resposta-simulada.json] [--avaliacao pasta] [--saida pasta] [--aplicar]
```

| Opção | Padrão | Uso |
|---|---|---|
| `--condicao` | `C` | condição do experimento |
| `--alerta` | executa o Semgrep | usa um SARIF externo |
| `--regras` | mapa embutido | mapa regra → CWE para SARIF externo |
| `--resposta` | `resposta-simulada.json` | proposta simulada (lugar do LLM) |
| `--avaliacao` | `avaliacao` | pasta com `teste.sh`; sem ela a validação não é executada |
| `--saida` | `.authfix` | SARIF, relatórios JSON/Markdown e patch |
| `--aplicar` | desligado | aplica o patch aprovado no projeto |

## Decisões de implementação

- `AlertReader.ReadAll` lê todos os resultados do SARIF; cada alerta é processado em sequência.
- `Pipeline` concentra o fluxo (localização, recuperação, geração, validação), antes inline em `Program.cs`.
- `SemgrepRunner` grava as regras embutidas em pasta temporária, executa `semgrep/semgrep` em Docker com o projeto montado como somente leitura e normaliza o SARIF (remove `uriBaseId` e o prefixo da pasta no `ruleId`).
- Regras embutidas:
  - `aspnet-controller-sem-autorizacao`: action com `[Http*]` sem `[Authorize]`/`[AllowAnonymous]` no método e sem `[Authorize]` na classe;
  - `aspnet-minimal-api-sem-autorizacao`: `Map*` sem `RequireAuthorization`/`AllowAnonymous`.
- A validação continua exigindo um checkout igual a `HEAD`.

## Limpeza

- TCC: remover `experimentos/demo-api` e `experimentos/piloto/documentos`, e atualizar o README de experimentos.
- `~/Documents/AuthFix-Demo-API`: voltar o controlador à versão vulnerável, remover `semgrep/`, adicionar `.authfix/` ao `.gitignore`, simplificar `demo.sh` e `ROTEIRO.txt`.
