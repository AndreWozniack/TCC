# Plano de implementação da remediação de autorização em ASP.NET Core

## Repasse para o agente de implementação

Implemente o MVP neste repositório a partir do [Capítulo 2 do Projeto Físico](../../../Artigos/01_entregas/05_projeto_fisico_artigo_principal/fonte/capitulos/cap_02.tex) e do [protocolo de avaliação](../../../Artigos/01_entregas/05_projeto_fisico_artigo_principal/fonte/capitulos/cap_04.tex). Comece por um único caso executável de autorização por recurso. O marco do MVP é produzir relatórios comparáveis de A, B e C para esse caso. Os pilotos adicionais e a expansão são etapas posteriores.

Use o mesmo modelo, versão, parâmetros, instruções e procedimento de validação nas três condições. A recebe alerta e método, B recebe alerta e arquivo, e C recebe a entrada de B com as evidências recuperadas. A CLI deve aceitar ambos os formatos de entrada e produzir diff, justificativa contextual e resultados separados de compilação, segurança, funcionalidade e aderência.

## Diagnóstico técnico

O repositório reúne entregas acadêmicas, um template PUCPR em LaTeX e referências bibliográficas. A inspeção dos arquivos rastreados, da árvore de trabalho e dos caminhos no histórico local não encontrou projetos C#, soluções .NET, código do agente, integração com LLM, índice de recuperação, aplicador de patches ou suíte executável de testes. As referências a esses módulos nos planos descrevem o desenho anterior. A única cadeia de compilação configurada para a entrega atual é XeLaTeX com latexmk e BibTeX.

Do Projeto Físico anterior, podem ser reaproveitados o template, a identificação acadêmica, os marcos de entrega e os conceitos de CLI, isolamento em Git worktree, verificação e revisão humana. A triagem genérica, Java, a base de especialistas, a memória de correções e o ciclo autônomo serão retirados do desenho do MVP. O escopo anterior permanece registrado nas entregas históricas.

| Arquivo do Projeto Físico | Alteração |
| --- | --- |
| `fonte/dados_academicos.tex` e `fonte/pretextual.tex` | Título, resumo e palavras-chave sobre autorização em ASP.NET Core. |
| `fonte/capitulos/cap_01.tex` | Problema, hipótese, objetivos, escopo e trabalhos relacionados. |
| `fonte/capitulos/cap_02.tex` | Cinco blocos, interfaces, fluxos, tecnologias e estrutura dos casos. |
| `fonte/capitulos/cap_03.tex` | Dependências e fases do piloto, mantendo os marcos acadêmicos do documento anterior. |
| `fonte/capitulos/cap_04.tex` | Testes, critérios de aceite, condições A/B/C e métricas. |
| `fonte/capitulos/cap_05.tex` e `cap_06.tex` | Riscos e viabilidade do recorte. |
| `fonte/referencias.bib` | Referências citadas na versão revisada. |
| `fonte/main.tex` | Metadados do PDF e configuração dos destinos internos. |

## Arquitetura e dependências

O protótipo terá um projeto de CLI em `src/AuthFix/`, uma suíte em `tests/AuthFix.Tests/` e casos de avaliação em `experimentos/`. A CLI usará C# e .NET 10. O Roslyn analisará projetos com dependências preparadas no ambiente de teste. A CLI receberá SARIF 2.1.0 ou um manifesto de localização e produzirá um diff e relatórios em JSON e Markdown. Um adaptador de LLM selecionado por configuração receberá apenas o material autorizado para envio.

O fluxo será `Analysis → Retrieval (condição C) → Generation → Validation → Reporting`. A geração recebe um `Alert`, o código permitido pela condição e uma lista de `Evidence`. A validação recebe um `Proposal`, o commit e a configuração de teste mantida pelo avaliador. O relatório consome esses registros e o `ValidationResult`. As etapas abaixo incluem testes para localização ambígua, contexto insuficiente, resposta inválida, alterações em arquivos protegidos e falhas de infraestrutura.

## Sequência de implementação

1. **Núcleo.** Criar `src/AuthFix/AuthFix.csproj`, `Program.cs` e `Contracts.cs`, além de `tests/AuthFix.Tests/AuthFix.Tests.csproj`. Definir `Alert`, `Evidence`, `Proposal`, `ValidationResult` e `RunReport`. Em `Analysis/AlertReader.cs`, normalizar SARIF e manifesto e rejeitar entradas sem localização válida, caminhos externos e classes fora do recorte.
2. **Análise.** Criar `Analysis/AuthorizationAnalyzer.cs`. Localizar a action ou o endpoint mínimo pelo intervalo do alerta e construir um inventário com símbolos, locais, policies, roles, claims, requisitos, handlers e relações resolvidas pelo Roslyn. Cobrir metadados de controller, action e grupo, defaults, fallback, `AllowAnonymous` e chamadas a `AuthorizeAsync`. Testar localizações inequívocas e a indicação de vínculos que a análise estática não resolve.
3. **Piloto.** Preparar um caso de autorização por recurso em `experimentos/piloto/documentos/`. Fixar projeto, commit, localização, regra esperada e mecanismo existente de autorização. Manter a matriz de permissões, a correção de referência e os testes do avaliador fora das entradas do LLM. Preparar a infraestrutura isolada antes da primeira execução. A validação deve verificar o acesso permitido, a recusa de acesso indevido, a preservação da operação e a ausência de efeitos colaterais quando houver recusa.
4. **Geração sem RAG.** Criar `Generation/PatchGenerator.cs` e `Generation/LlmClient.cs`. Executar A e B sobre o mesmo caso e registrar um resultado por condição, inclusive abstenções. Testar a composição da entrada com um provedor substituto, incluindo o descarte de segredos e a rejeição de respostas inválidas. Usar o mesmo modelo e parâmetros na execução experimental real.
5. **Recuperação.** Criar `Retrieval/ContextRetriever.cs`. Priorizar definições ligadas por símbolos, requisitos e handlers associados e endpoints com operação equivalente. Usar busca textual para candidatos complementares. Entregar arquivo, intervalo, símbolo e motivo por trecho. Testar a presença das evidências anotadas e a sinalização de ambiguidade, sem usar as anotações para ranquear os resultados.
6. **Validação e comparação.** Criar `Validation/PatchValidator.cs`, `Reporting/RunReportWriter.cs` e `experimentos/README.md`. Validar o diff em worktree descartável, limitar os arquivos alteráveis, executar build e testes em ambiente isolado e registrar os resultados de segurança, funcionalidade e aderência separadamente. Testar a proteção dos arquivos de avaliação e a preservação do checkout original. Executar A/B/C no primeiro caso e gerar os relatórios pareados.

## Verificação e entrega do MVP

- Rode `dotnet build src/AuthFix/AuthFix.csproj` e `dotnet test tests/AuthFix.Tests/AuthFix.Tests.csproj`. A suíte deve verificar equivalência de localização entre SARIF e manifesto, recuperação com proveniência, separação das entradas A/B/C e preservação do checkout original.
- Documente em `experimentos/README.md` o comando da CLI para cada condição, o commit do caso, as versões do SDK e do modelo, os parâmetros e a preparação do ambiente. O carregamento de projetos com MSBuild e as execuções dos projetos analisados devem ocorrer no ambiente isolado.
- Guarde uma execução real de cada condição para o primeiro caso, com o diff ou a abstenção, o relatório em JSON e Markdown e os resultados dos testes. Um patch reprovado é um resultado válido do experimento. Se faltar acesso ao provedor, execute as verificações locais com o adaptador de testes e registre que a comparação experimental está pendente.
- Registre as limitações observadas na cobertura do SAST e na resolução de símbolos. A avaliação posterior começa com cinco a dez casos em duas a três aplicações e pode chegar a 30 a 50 casos conforme a viabilidade.
