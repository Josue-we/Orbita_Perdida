---
name: lumen-architecture
description: Mapa da arquitetura do jogo LUMEN / Órbita Perdida (Unity 6, C#, namespaces Lumen.*) - EventBus, GameTuning, GameManager, ScriptableObjects de fase e quiz, TutorialSceneBuilder, HUD, áudio e narração. Use SEMPRE antes de adicionar ou mudar qualquer comportamento do jogo (novo sistema, nova mecânica, ajuste de números, HUD, áudio, câmera, fluxo menu/intro/jogo/quiz/fim), para seguir os padrões existentes em vez de inventar outros. Também use quando o usuário perguntar "onde fica X" ou "como funciona Y" no código.
---

# Arquitetura do LUMEN

Jogo educativo 3D (Sistema Solar) em Unity 6 (`6000.3.24f1`) com URP. Tudo do projeto fica em
`Assets/_Project/`. O código é pequeno (~4.700 linhas, 29 arquivos) e segue poucos padrões bem
claros. Respeitá-los mantém o jogo simples de entender e de apresentar na disciplina.

Mapa completo de arquivos e responsabilidades: `references/code-map.md` (leia quando precisar
localizar algo ou entender o fluxo de uma tela).

## Os 5 padrões que sustentam o projeto

1. **EventBus em vez de referências diretas** (`Core/EventBus.cs`). Sistemas publicam e assinam
   eventos estáticos (`OnFragmentCollected`, `OnAnswerCorrect`, `OnEnergyChanged`...). O LUMEN não
   conhece o HUD; o quiz não conhece o áudio. Para um recurso novo que precisa "avisar" outros,
   crie um evento + um método `Raise...` no EventBus. Sempre cancele a assinatura (`-=`) em
   `OnDisable`/`OnDestroy`, senão eventos estáticos seguram objetos de cenas antigas
   (e "Jogar novamente" recarrega a cena).

2. **Números de ajuste num lugar só: `GameTuning`** (`Core/GameTuning.cs`). Velocidade, consumo de
   combustível, câmera, tamanhos de fonte, sinal. O motivo está no próprio arquivo: um valor
   `[SerializeField]` fica gravado na cena e **mudar o default do script não altera uma cena já
   montada**. Por isso os componentes reaplicam o `GameTuning` no `Awake/Start`. Valor novo de
   balanceamento = constante nova em `GameTuning` + comentário dizendo como foi calibrado.

3. **Conteúdo em ScriptableObjects, não em código.** `PhaseData` (falas da NOVA, pool de quizzes,
   recompensa de energia) e `QuizQuestionData` (pergunta, alternativas, feedbacks) ficam em
   `Data/Phases` e `Data/Quizzes`. Um planeta novo é uma instância nova de `PhaseData`, sem código.
   Perguntas extras nascem em `Editor/QuizBank.cs` e viram assets quando o builder roda.

4. **Cena montada por script: `LUMEN > Montar Cena de Tutorial`** (`Editor/TutorialSceneBuilder.cs`,
   ~1.300 linhas). Cria HUD, nave, câmera, planetas, quizzes, menus e áudio, e é **idempotente**
   (métodos `Ensure...`: criam só o que falta). Consequência prática: para mudar a cena, altere o
   builder e peça ao usuário para rodar o menu de novo; não edite o `.unity` na mão. Ao criar algo
   novo no builder, escreva-o como `Ensure...` para continuar seguro de rodar várias vezes.

5. **Desafios plugáveis via `IPhaseChallenge`** (`Gameplay/Challenges`). `StartChallenge(phase,
   onCompleted)`; quem dispara (`PlanetApproachTrigger`) não precisa saber o tipo. Hoje só existe
   o quiz. A skill `lumen-new-phase-minigame` explica como acrescentar minigames.

## Máquina de estados

`GameManager` guarda `GameState`: `Boot → Menu → Intro → Playing ⇄ Paused / Challenge → Ended`.
Entrar num quiz/minigame = `SetState(Challenge)` (a nave trava); ao terminar volta a `Playing`.
Quem trava input ou o tempo deve respeitar o estado em vez de criar flags próprias.
`GameManager` é `DontDestroyOnLoad` e se reseta em `sceneLoaded` (é assim que "Jogar novamente" funciona).

## Convenções de código observadas

- Namespace `Lumen.<Área>` (`Core`, `Gameplay`, `Gameplay.Challenges`, `UI`, `Audio`, `Cameras`,
  `Narrative`, `Environment`, `Data`, `EditorTools`).
- Scripts só do Editor: pasta `Editor/` e/ou `#if UNITY_EDITOR`. `using UnityEditor` em runtime quebra o build.
- Campos de inspetor: `[SerializeField] private`. Métodos `Configure/Set...` públicos existem para o
  builder ligar referências sem reflection.
- Comentários e logs em português; logs com prefixo `[LUMEN]`. Comentários explicam o **porquê**
  (a história de um bug, o motivo de um número), não repetem o código.
- Input com a API antiga (`Input.GetKey`), `Active Input Handling = Both`.
- Busca de objetos (`FindFirstObjectByType`) só em `Awake/Start` ou com cache. Nunca `FindObjectOfType` (obsoleto no Unity 6).
- UI com `UnityEngine.UI` (`Text`, `Button`), Canvas em `Scale With Screen Size` 1920x1080
  (`UiTextTuning` reaplica isso em runtime em toda cena).

## Armadilhas conhecidas

- **Numeração inconsistente no GDD:** a cena de exemplo chama Júpiter de "Fase 4" e usa o HUD
  "3/5 → 4/5", mas a tabela de fases e o código têm Júpiter como 4º planeta da rota (índice 3) e
  a Terra como 6º destino. Não fixe esses números em código sem confirmar com o autor.
- `PlanetApproachTrigger` hoje procura `QuizChallengeController` diretamente; não é totalmente agnóstico
  de desafio ainda (detalhes em `lumen-new-phase-minigame`).
- A rota é **forçada em ordem** (`EventBus.RouteProgress`): planetas fora de ordem ficam bloqueados.
- O pacote de planetas é pago e não está no repo (ver `lumen-unity-pipeline`).
