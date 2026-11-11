# Protótipo de remediação de autorização

A CLI lê um alerta de CWE-862 ou CWE-863, localiza um método C# com Roslyn, monta as condições A, B ou C e registra uma resposta simulada. O recuperador identifica declarações e instruções de autorização por correspondência sintática e textual, com arquivo, símbolo e linha. Os trechos são candidatos para revisão, pois ainda não há resolução semântica de dependências, ordenação por equivalência de operação nem cobertura completa de Minimal APIs.

O piloto usa um projeto fictício. A autenticação por cabeçalho existe apenas nesse projeto de teste. O endpoint de alteração contém uma falha deliberada e a avaliação é executada em contêiner sem rede. Mantenha esse projeto no ambiente de teste.

## Requisitos

- .NET 10 e Git.
- Docker em execução e imagem `mcr.microsoft.com/dotnet/sdk:10.0`.
- Pacote NuGet `Microsoft.CodeAnalysis.CSharp` 5.0.0 para compilar a CLI.

```sh
dotnet test tests/AuthFix.Tests/AuthFix.Tests.csproj
docker pull mcr.microsoft.com/dotnet/sdk:10.0
sh experimentos/piloto/documentos/executar.sh /tmp/resultados-authfix
```

O script cria um repositório temporário com o caso, fixa um commit local e executa A, B e C com a mesma resposta pré-escrita. Gera JSON e Markdown para cada condição e registra o commit criado. Os resultados demonstram a infraestrutura e a validação do patch de exemplo. Eles não medem o desempenho de um modelo de linguagem.

## Execução manual

```sh
dotnet run --project src/AuthFix -- REPOSITORIO ALERTA.json C RESPOSTA.json RELATORIO.json --validate COMMIT PASTA_AVALIACAO
```

O manifesto contém `id`, `cwe`, `file` e `startLine`. Um arquivo SARIF 2.1.0 com um resultado e uma localização também pode ser usado. Nesse caso adicione `--rules regras.json` com um objeto JSON que associe o identificador da regra à CWE, por exemplo `{"regra-1":"CWE-863"}`. A resposta simulada contém `diff`, `explanation` e `references`. Uma resposta com diff vazio é registrada como abstenção.

Para validação, o diretório de avaliação deve conter `teste.sh`. O script é montado em `/evaluation` como somente leitura e recebe o projeto em `/work`. O avaliador do piloto emite os marcadores `SEGURANCA: APROVADO` e `FUNCIONALIDADE: APROVADO`. A CLI aplica o diff em um Git worktree descartável, permite alteração apenas no arquivo indicado no alerta e executa o avaliador no contêiner. A revisão de aderência permanece manual.

A análise, a entrada da geração e os relatórios usam o conteúdo do checkout informado. Para validar, o checkout deve corresponder ao commit informado, como no script do piloto. O escopo atual do validador pressupõe que o projeto e o avaliador sejam preparados para o contêiner, inclusive com dependências disponíveis sem rede. Os resultados automatizados dependem da qualidade do avaliador e da integridade dos marcadores emitidos.

## Etapas pendentes

A integração com um provedor de LLM e os dados de modelo, tokens, custo e tempo de geração dependem de uma execução real. Ainda faltam a análise semântica completa por Roslyn, seleção contextual por relações entre símbolos, cobertura ampliada de endpoints Minimal API, orçamento de contexto, métricas agregadas e uma avaliação independente de aderência. O protótipo também precisa endurecer o isolamento e a verificação do resultado para executar projetos arbitrários não confiáveis.
