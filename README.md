# Projeto Transformador — 2º semestre de 2026

Esta pasta separa modelos reutilizáveis, entregas do semestre e referências gerais.

## Entregas

| Nº | Entrega | Prazo | Situação |
|---:|---|---:|---|
| 01 | Formulário Proposta | 18/08/2026 | Concluído |
| 02 | Proposta de Projeto | 31/08/2026 | Concluído |
| 03 | Plano de Projeto | 25/09/2026 | Próxima entrega |
| 04 | Defesa do Plano de Projeto | 28/09/2026 | Pendente |
| 07 | Ficha de encontros com o Orientador | 10/11/2026 | Pendente |
| 05 | Projeto Físico — artigo principal | 12/11/2026 | Pendente |
| 06 | Defesa do Projeto Físico | 19/11/2026 | Pendente |

## Organização

- `00_modelos/`: cópias limpas e reutilizáveis; não editar diretamente.
- `01_entregas/`: uma pasta numerada para cada atividade do semestre.
- `02_referencias/`: artigos, normas, manuais e materiais comuns a várias entregas.
- `fonte/`: arquivos editáveis (`.tex`, `.bib`, `.xlsx`, apresentações etc.).
- `material_recebido/`: enunciados, roteiros e modelos fornecidos pela disciplina.
- `entrega/`: versão final efetivamente enviada.

O template PUCPR limpo está em `00_modelos/latex_pucpr_2026`. A cópia de trabalho do artigo principal está em `01_entregas/05_projeto_fisico_artigo_principal/fonte`.

## Fluxo para novos documentos

1. Criar a pasta da nova entrega em `01_entregas/`.
2. Copiar o modelo apropriado de `00_modelos/` para a subpasta `fonte/`.
3. Guardar enunciados e roteiros em `material_recebido/`.
4. Trabalhar apenas na cópia localizada em `fonte/`.
5. Colocar em `entrega/` somente a versão final enviada.

## Compilação LaTeX

- Artigo principal/template PUCPR: usar `latexmk -xelatex main.tex` dentro da pasta `fonte/`.
- Proposta de projeto: usar `latexmk -pdf proposta.tex` dentro da pasta `fonte/`.

O template PUCPR requer XeLaTeX por utilizar o pacote `fontspec`.
