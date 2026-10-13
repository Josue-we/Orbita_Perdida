# Mapa do código (Assets/_Project/Scripts)

Sumário: [Core](#core) · [Gameplay](#gameplay) · [Data](#data) · [UI](#ui) · [Narrative](#narrative) ·
[Audio](#audio) · [Camera](#camera) · [Environment](#environment) · [Editor](#editor) · [Fluxos](#fluxos-importantes)

## Core
| Arquivo | Papel |
| --- | --- |
| `EventBus.cs` | Eventos estáticos do jogo, `RouteProgress`, `TutorialCompleted`, `ResetSession()`. Enum `ShipThrustState` (Idle/Thrusting/Boosting). |
| `GameManager.cs` | `GameState`, ponto de resgate (`RescuePosition/RescueLabel`), reset em `sceneLoaded`. Singleton `DontDestroyOnLoad`. |
| `GameTuning.cs` | Constantes: combustível (`MaxEnergy 100`, `ThrustDrainPerSecond 0.45`), direção (`MaxSpeed 45`, `BoostSpeed 120`), câmera, mundo (`OpenWorldRadius 30000`), sinal (`SignalBase 5`, `SignalPerFragment 18`), fontes/layout do quiz e HUD. |
| `UiTextTuning.cs` | Aplica fonte/âncoras e configura todo `CanvasScaler` (1920x1080, match 0.5) ao carregar a cena. |

## Gameplay
| Arquivo | Papel |
| --- | --- |
| `Lumen/LumenController.cs` | Movimento em terceira pessoa sem Rigidbody: A/D giram, W/S avançam, Space/Ctrl sobem/descem, X freia, Shift boost (após o tutorial), R resgate sem combustível. Publica `ShipThrustState`. |
| `Lumen/LumenEnergySystem.cs` | Combustível: só drena com propulsor aceso; `Refill(x)`; publica `OnEnergyChanged`. |
| `PlanetApproachTrigger.cs` | No planeta. Dispara 1x (colisão **ou** checagem de distância a cada 0,2 s). Só o próximo da rota abre o encontro. Trava a nave → narração NOVA → desafio → `CompletePhase` (avança rota, recarrega energia, define ponto de resgate, aponta a seta para o próximo). |
| `TutorialExitTrigger.cs` | Farol que encerra o tutorial e libera o boost. |
| `Challenges/IPhaseChallenge.cs` | Contrato de desafio: `StartChallenge(PhaseData, Action onCompleted)`. |
| `Challenges/QuizChallengeController.cs` | Quiz: sorteia pergunta do pool, embaralha alternativas (Fisher-Yates), erro sem penalidade, abafa a música (`OnQuizOpened/Closed`). |

## Data
| Arquivo | Papel |
| --- | --- |
| `PhaseData.cs` | `PlanetName`, `NarrationLines[]`, `QuizPool[]`/`Quiz`, `FragmentEnergyReward (75)`. `PickQuiz()` sorteia sem repetir, memória em `PlayerPrefs` (`lumen.quizseen.<planeta>`). |
| `QuizQuestionData.cs` | `Question`, `Options[]`, `CorrectIndex`, `CorrectFeedback`, `IncorrectFeedback`. |

Assets: `Data/Phases/<Planeta>_Phase.asset` (6) e `Data/Quizzes/<Planeta>_Quiz[_NN].asset` (11 por planeta).

## UI
`HUDController` (sinal, combustível, fragmentos, objetivo, seta de navegação, alerta de combustível baixo,
prompt do tutorial) · `MainMenuController` (botão Jogar/Enter) · `PauseMenuController` (Esc, confirmar saída) ·
`EndScreenController` (resumo, curiosidades coletadas, Jogar novamente / Sair).

## Narrative
`IntroSequenceController` (texto "SINAL PERDIDO..." após o Jogar) · `NovaNarrationController.Play(linhas, onDone)`
(fala da NOVA por linhas).

## Audio
`AudioManager`: música do menu e da gameplay (troca com fade), abafa a música durante o quiz,
SFX (select/correct/incorrect/collect), loops do propulsor/boost. Assina o `EventBus`. Loops de nave
são carregados de `Resources/Audio/` se não atribuídos.

## Camera
`CameraFollow`: terceira pessoa atrás da nave, segue só o giro horizontal (horizonte não tomba).

## Environment
`PerformanceTuning` (60 FPS, sem sombras direcionais, remove partículas "Glow" pesadas), `SunLight` (luz
direcional do Sol apontando para a nave), `SunGlow`, `SlowRotator` (rotação lenta dos planetas),
`DebugTeleport` (F1-F5, só Editor).

## Editor
`TutorialSceneBuilder` (menu **LUMEN > Montar Cena de Tutorial**; posições da rota, escala por planeta,
HUD, menus, áudio), `QuizBank` (perguntas extras; **a alternativa certa é sempre a primeira**),
`AudioAutoSetup`, `AutoFocusGameView`.

## Fluxos importantes
- **Partida:** Menu → Jogar → Intro → Playing → (tutorial termina no farol ou ao chegar no 1º planeta) →
  Netuno → Urano → Saturno → Júpiter → Marte → Terra → `OnMissionComplete` → tela final.
- **Encontro:** `PlanetApproachTrigger` → `GameState.Challenge` → `NovaNarrationController.Play` →
  `QuizChallengeController.StartChallenge` → acertou → `CompletePhase`.
- **Sem combustível:** nave para; R devolve ao último planeta resolvido com tanque cheio (nenhum fragmento se perde).
- **Sinal com a Terra:** não depende de distância; sobe a cada fragmento (5% + 18% × fragmentos; fecha em 100% na Terra).
