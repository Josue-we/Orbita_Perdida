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
| `Shift` | Boost (liberado ao concluir o tutorial) |

O HUD mostra as barras de **SINAL** (cresce ao se aproximar de cada planeta e a
cada fragmento) e de **COMBUSTÍVEL** (drena enquanto voa, recarregada a cada
fragmento), além de uma **seta de navegação** apontando o próximo planeta da rota.

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

- Movimento com aceleração, desaceleração e rotação suave, com boost pós-tutorial.
- Barra de **SINAL** dinâmica (proximidade dos planetas + fragmentos).
- Barra de **COMBUSTÍVEL** (dreno por movimento; recarga por fragmento).
- HUD com seta de navegação, objetivo com distância e contador de fragmentos.
- Intro cinematográfica estilo "log de sistema" (avance com qualquer tecla, `Esc` pula).
- Narração da NOVA ao se aproximar de cada planeta.
- Sistema de **quiz** (errar deixa tentar de novo; acertar libera o fragmento).
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
  clássico (`Input.GetAxis`). O Unity pede para reiniciar o Editor após a troca.
- Pacote de assets "Planets of the Solar System 3D" (planetas + skybox).

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
│   ├── Core/          EventBus, GameManager (estados do jogo)
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
- [ ] Minigames de Saturno e Júpiter (hoje usam o mesmo quiz)
- [ ] Modelo 3D real do LUMEN (atualmente é uma cápsula placeholder)
- [ ] Tela de início / fim de jogo mais elaborada

Os scripts foram revisados, mas não compilados dentro deste ambiente de
desenvolvimento (Unity não está instalada aqui). Se algum erro aparecer no
Console, o feedback é bem-vindo para corrigir rápido.