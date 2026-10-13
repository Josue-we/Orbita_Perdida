# LUMEN / Órbita Perdida - contexto para o Claude

Jogo educativo 3D de Ciências (Sistema Solar) em **Unity 6 (`6000.3.24f1`, URP)**, Projeto Final da disciplina
9424207 - Tópicos Especiais em Tecnologia (UNIVATES, 2026/B). Autor: Josué Weschenfelder.
Você pilota o satélite **LUMEN**, guiado pela IA **NOVA**, de Netuno de volta à Terra; em cada planeta há
uma narração e um desafio (hoje, sempre quiz) que devolve combustível.

## Como trabalhar aqui

- **Responda em português do Brasil.** Mantenha o escopo simples e leve: é um projeto acadêmico, com cenário
  leve e mecânicas diretas.
- **Pipeline fixo:** edite os arquivos direto na pasta do projeto (nada de zip), compile para checar erros,
  corrija o que aparecer e refaça até compilar limpo. Detalhes e scripts: skill `lumen-unity-pipeline`.
  Se não der para compilar, diga isso explicitamente; nunca afirme que compila sem ter compilado.
- **Leve em máquina sem GPU dedicada:** sem sombras, cuidado com transparência/partículas, 60 FPS.
  Skill `lumen-light-performance`.
- Não edite cenas/prefabs/`.meta` à mão; a cena é montada pelo menu `LUMEN > Montar Cena de Tutorial`.

## Skills do projeto (`.claude/skills/`)

| Skill | Use quando |
| --- | --- |
| `lumen-unity-pipeline` | qualquer edição/compilação de código ou assets |
| `lumen-architecture` | antes de mudar comportamento; "onde fica / como funciona" |
| `lumen-light-performance` | efeitos, partículas, minigames, áudio, qualquer coisa por quadro |
| `lumen-new-phase-minigame` | fase nova, minigame, mudar a rota |
| `lumen-educational-content` | perguntas, feedbacks, falas da NOVA, fatos dos planetas |

Skills gerais do Claude que também servem a este projeto: `skill-creator` (evoluir/testar as skills acima),
`deep-research` (pesquisar curiosidades dos planetas com fontes), `docs`/`docx`/`pdf` (relatório e GDD da
disciplina), `pptx` (apresentação final).

## Estado do projeto

Rota completa e jogável (Netuno → Urano → Saturno → Júpiter → Marte → Terra) com menu, intro, HUD, quiz com pool de
11 perguntas por planeta, narração da NOVA, combustível/resgate, tela final (protótipo) e áudio.
Pendências de fonte de verdade em `README.md` ("O que ainda não existe") e `TODO.md`.

## Pontos a confirmar com o autor antes de fixar em código

- Numeração do GDD: a cena de exemplo chama Júpiter de "Fase 4" e o HUD de "3/5 → 4/5", mas na tabela Júpiter é a fase 5
  e o código tem 6 destinos (a Terra fecha a missão sem fragmento).
- Minigames próprios por planeta (hoje todos usam quiz) e série/idade-alvo das perguntas.

## Pacotes e arquivos fora do repositório

- "Planets of the Solar System 3D" (pago) precisa ser importado em `Assets/` para o builder achar os planetas.
- Áudio e imagens usam Git LFS (`.gitattributes`). A trilha de gameplay em `.wav` tem ~62 MB; ver `lumen-light-performance`.
