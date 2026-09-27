# LUMEN — A viagem de volta para casa

Jogo educativo em **Unity (URP)** onde o satélite **LUMEN** perdeu a rota de
volta para a Terra depois de uma tempestade solar. Para reestabelecer a conexão
com a base, você pilota o satélite planeta a planeta, coletando **fragmentos de
conhecimento** — cada um liberado ao responder um **quiz** sobre aquele mundo.

A jornada termina quando o LUMEN chega à Terra.

## Como jogar

| Tecla | Ação |
| --- | --- |
| `W` / `S` | Mover para frente / para trás |
| `A` / `D` | Mover para os lados |
| `Space` | Subir |
| `Left Ctrl` | Descer |
| `X` | Freio de manobra (não gasta combustível) |
| `Shift` | Boost (liberado ao concluir o tutorial) |
| `R` | Resgate (só com o combustível zerado) |

A câmera tem **orientação fixa no mundo** (ela não gira junto com o satélite), então
o horizonte nunca gira: `W` leva para o topo da tela, `A`/`D` para os lados, e o
satélite apenas aponta para onde está indo, como um avião.

O HUD mostra as barras de **SINAL** (cresce ao se aproximar de cada planeta e a
cada fragmento) e de **COMBUSTÍVEL** (drena com os propulsores acesos, recarrega
a cada fragmento), além de uma **seta de navegação** apontando o próximo planeta
da rota.

## Combustível e resgate

O combustível é um recurso real, não um enfeite:

- O tanque só drena com os propulsores acesos (um foguete parado não queima
  combustível).
- O consumo é de **0,2 por segundo** propulsionando. O tanque inteiro (100) dá
  ~500 s de voo, e o trecho mais longo da rota (Júpiter → Marte, ~2.725 unidades)
  custa ~61 a 9 u/s: dá para cruzar qualquer perna da rota voando direto, com
  ~39% de folga. Frear (`X`) não gasta combustível.
- **Se o combustível zerar, o LUMEN para no lugar** — não há mais propulsor.
- O HUD avisa antes (`COMBUSTÍVEL BAIXO`) e, quando zera, mostra
  `COMBUSTÍVEL ESGOTADO`.
- Aí entra o **resgate**: aperte `R` e o LUMEN volta ao último planeta onde você
  estava (o ponto seguro mais recente) com o tanque cheio, para você refazer a
  perna até o próximo. Nenhum fragmento é perdido. **Não há resgate automático**:
  a única forma de destravar a nave é apertar `R`.

## Rota das fases

A rota é propositalmente **zigue-zague**: a seta guia o jogador planeta a
planeta, impedindo o atalho em linha reta até a Terra.

| # | Planeta | Tema do quiz |
| --- | --- | --- |
| 1 | Netuno | Ventos mais fortes do Sistema Solar |
| 2 | Urano | Rotação "deitada" e estações de 21 anos |
| 3 | Saturno | Anéis de gelo e rocha |
| 4 | Júpiter | A Grande Mancha Vermelha |
| 5 | Marte | Superfície avermelhada (óxido de ferro) |
| 6 | Terra | Por que a Terra é única |

Ao acertar o quiz de um planeta, o fragmento é coletado, a energia é
restaurada e a seta passa a indicar o próximo destino. A missão termina ao
concluir o quiz da Terra.

## Funcionalidades

- **Direção arcade com intenção**: aceleração forte, freio ao soltar o acelerador
  e freio de manobra (`X`, que não gasta combustível). O satélite aponta para onde
  está indo, como um avião, com suavização — e a **câmera tem orientação fixa no
  mundo**, então o horizonte não gira junto com a nave. Boost pós-tutorial (`Shift`).
- Números de jogo com **fonte única** em `Scripts/Core/GameTuning.cs`
  (combustível, movimento, câmera), reaplicados a cada execução em `Awake`/`Start`
  e pelo construtor de cena — inclusive em cena já montada, onde os valores
  `[SerializeField]` ficam gravados no componente.
- **Câmera estável**: offset fixo no mundo, sem herdar a rotação do satélite, o
  que elimina a tela "voltar para o centro" ao soltar uma tecla de lado.
- Barra de **SINAL** dinâmica (proximidade dos planetas + fragmentos).
- Barra de **COMBUSTÍVEL** com consumo real, parada da nave ao zerar e resgate
  manual (tecla `R`) de volta ao último planeta.
- HUD com seta de navegação, objetivo com distância e contador de fragmentos.
- Intro cinematográfica estilo "log de sistema" (avance com qualquer tecla, `Esc` pula).
- Narração da NOVA ao se aproximar de cada planeta.
- Sistema de **quiz** (errar deixa tentar de novo; acertar libera o fragmento),
  com a trilha sonora abaizando enquanto o quiz está em tela e voltando ao volume
  anterior assim que ele fecha.
- Gatilhos de encontro com fallback por **distância** (não dependem de colisão física).
- **Construtor de cena automático**: o menu `LUMEN > Montar Cena de Tutorial`
  monta a cena inteira (GameManager, LUMEN, câmera, HUD, planetas, quiz, áudio,
  skybox) e é seguro rodar mais de uma vez — ele reposiciona e conserta o que já
  existe em vez de duplicar.
- Planetas com tamanho **proporcional ao raio real** de cada um (ancorados em Netuno).

## Requisitos

- **Unity 6 LTS (URP)**.
- **Active Input Handling**: `Edit > Project Settings > Player > Active Input Handling`
  deve estar em **"Input Manager (Old)"** ou **"Both"** — os scripts usam o input
  clássico (`Input.GetAxisRaw` para o movimento e `Input.GetKey` para as teclas
  extras). O Unity pede para reiniciar o Editor após a troca.
- Pacote de assets "Planets of the Solar System 3D" (planetas + skybox) — compre e
  importe por conta própria; ele **não** está incluído neste repositório.

## Como rodar

1. Configure o **Active Input Handling** conforme acima e reinicie o Editor.
2. Importe o pacote **Planets of the Solar System 3D** para `Assets`.
3. Crie uma cena nova: `File > New Scene` → template **Basic (URP)** →
   salve como `Assets/_Project/Scenes/Game.unity`.
4. Rode o menu **`LUMEN > Montar Cena de Tutorial`**.
5. Pressione **Play**.

A intro conta a história da tempestade solar; depois do tutorial ("atravesse o
FAROL"), a navegação aponta para Netuno e a jornada começa.

## Estrutura do projeto

```
Assets/_Project/
├── Scripts/
│   ├── Core/          EventBus, GameManager (estados do jogo), GameTuning (números oficiais)
│   ├── Gameplay/
│   │   ├── Lumen/     LumenController (movimento), LumenEnergySystem
│   │   ├── Challenges/ QuizChallengeController, IPhaseChallenge
│   │   ├── PlanetApproachTrigger.cs   gatilho de encontro por planeta
│   │   └── TutorialExitTrigger.cs     fim do tutorial (farol)
│   ├── UI/            HUDController
│   ├── Narrative/     IntroSequenceController (abertura), NovaNarrationController
│   ├── Camera/        CameraFollow
│   ├── Audio/         AudioManager (trilha + SFX reativos a eventos)
│   ├── Data/          PhaseData, QuizQuestionData (ScriptableObjects)
│   └── Editor/        TutorialSceneBuilder (menu de montagem da cena)
├── Data/
│   ├── Phases/        dados de cada fase (falas da NOVA, quiz, recompensa)
│   └── Quizzes/       perguntas e alternativas de cada planeta
└── Audio/             trilha e efeitos sonoros (identificados por palavra-chave)
```

## Editar conteúdos do jogo

Todo o texto do jogo (falas da NOVA, perguntas, alternativas, feedbacks) vive em
**ScriptableObjects** dentro de `Assets/_Project/Data` — basta abrir um arquivo
no Inspector e editar sem mexer em código. Se algum planeta novo precisar entrar,
é só seguir o padrão de `EnsureAllPlanetRoute()` no builder.

## Status atual

- [x] Tutorial com fim claro e boost liberado
- [x] Rota completa com quiz em cada planeta e seta de navegação
- [x] HUD com barras vazadas, porcentagens e distância até o objetivo
- [x] Combustível com consequência real (parada + resgate) e trilha abaixando no quiz
- [x] Combustível equilibrado para a rota: 100 de tanque, 0,2/s aceso e fragmento de 75
- [x] Resgate só pela tecla `R` (sem temporizador automático)
- [x] Câmera de orientação fixa e satélite alinhado ao rumo, sem giro de tela
- [x] Freio ao soltar o acelerador e freio de manobra (`X`)
- [x] Textos do jogo revisados em português (HUD, intro, falas da NOVA e quizzes)
- [ ] Minigames de Saturno e Júpiter (hoje usam o mesmo quiz)
- [ ] Modelo 3D real do LUMEN (atualmente é uma cápsula placeholder)
- [ ] Tela de início / fim de jogo mais elaborada

O código compila no Unity 6 LTS e os assets do jogo (HUD, intro, falas da NOVA e
quizzes) abrem sem avisos no Editor. Feedback de qualquer erro no Console é
bem-vindo para corrigir rápido.