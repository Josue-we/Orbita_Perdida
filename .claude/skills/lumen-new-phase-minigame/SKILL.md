---
name: lumen-new-phase-minigame
description: Como acrescentar fases/planetas e minigames ao jogo LUMEN / Órbita Perdida - novo PhaseData, novo desafio via IPhaseChallenge (anéis de Saturno, asteroides de Júpiter, ventos de Netuno, campo magnético de Urano, cânions de Marte), integração com PlanetApproachTrigger, HUD, EventBus e áudio. Use SEMPRE que o usuário pedir uma fase nova, um minigame, "criar a fase 2", "desafio de Júpiter/Saturno", mudar a ordem da rota ou trocar o quiz por outro tipo de desafio.
---

# Acrescentar fase ou minigame

Pré-requisito: ler `lumen-architecture` (padrões e fluxo do encontro) e, ao escrever o código,
seguir `lumen-unity-pipeline` (editar → compilar → corrigir).

## O que o GDD pede, planeta a planeta

O quiz atual (`QuizChallengeController`) é o desafio de **todos** os planetas hoje. O documento do
projeto prevê um minigame ligado à característica do planeta (o README lista "Minigames próprios
de Saturno e Júpiter" como pendente):

| Planeta | Curiosidade (NOVA) | Minigame previsto |
| --- | --- | --- |
| Netuno | ventos extremos e frio | resistir/desviar de rajadas de vento espacial |
| Urano | eixo inclinado ("rola") | reajustar a rota conforme o campo magnético inclinado |
| Saturno | os anéis | atravessar os anéis sem colidir |
| Júpiter | tamanho e tempestades | desviar de um campo de asteroides por um corredor estreito |
| Marte | exploração humana / água | navegação precisa entre cânions/crateras |

Regras de design do GDD que valem para qualquer minigame: **errar nunca encerra o jogo** (colisão =
leve efeito visual de dano, tenta de novo); o desafio é curto (alguns segundos); sucesso faz o
fragmento ser atraído à nave, partículas douradas, som de coleta e o contador do HUD subir; NOVA
comemora com fala curta; a nave volta ao movimento livre.

## Receita: fase nova (sem código)

1. **Dados:** `Assets > Create > LUMEN > Phase Data` (e `Quiz Question` para as perguntas). Preencha
   `PlanetName`, `NarrationLines` (falas curtas da NOVA), `QuizPool` e `FragmentEnergyReward`.
   Para várias perguntas de uma vez, acrescente `Q(...)` no `QuizBank.cs` (**a certa sempre primeiro**).
2. **Cena:** registre o planeta em `TutorialSceneBuilder.EnsureAllPlanetRoute` (posição constante,
   `EnsurePlanet(prefab, nome, posição, phase, índiceDaRota)`; o encadeamento `SetNextPlanet` vem do array `route`).
3. Peça ao usuário para rodar `LUMEN > Montar Cena de Tutorial` e testar com `DebugTeleport` (F1-F4 no Editor).
4. Se mudar a ordem/quantidade da rota, revise `GameTuning.SignalBase/SignalPerFragment` (hoje 5% + 18% × 5
   fragmentos = 95%; a Terra fecha em 100%) e o texto do HUD de fragmentos.

## Receita: minigame (código novo)

### 1. Escreva o desafio como `IPhaseChallenge`

```csharp
namespace Lumen.Gameplay.Challenges
{
    public class AsteroidCorridorChallenge : MonoBehaviour, IPhaseChallenge
    {
        private Action _onCompleted;

        public void StartChallenge(PhaseData phase, Action onCompleted)
        {
            _onCompleted = onCompleted;
            // 1. preparar o campo (reaproveitar objetos, nao Instantiate a cada vez)
            // 2. mostrar indicador no HUD ("desvie dos asteroides")
            // 3. jogador pilota normalmente; colisao = EventBus/efeito de dano, sem game over
        }

        private void Succeed()
        {
            // limpar/ocultar o campo, depois:
            _onCompleted?.Invoke();   // PlanetApproachTrigger.CompletePhase cuida de fragmento, energia e rota
        }
    }
}
```

Pontos que o minigame **não** deve reimplementar (já são do `PlanetApproachTrigger`): coletar fragmento,
recarregar energia, `AdvanceRoute`, ponto de resgate, apontar a seta ao próximo planeta, trocar `GameState`.

### 2. Estado e controles
- O encontro entra em `GameState.Challenge`, e o `LumenController` **trava** a nave nesse estado. Um minigame
  que exige pilotar precisa de um estado/flag que **libere o movimento** durante o desafio (por exemplo,
  um estado `Minigame` no `GameManager` que o `LumenController` trate como "pilotável", com velocidade reduzida
  como pede a cena de Júpiter). Altere `GameState` + `LumenController` juntos e teste o resgate (R) e a pausa (Esc).
- Combustível: decida se o minigame gasta tanque. Sugestão: **não** (evita o jogador falhar por combustível
  dentro do desafio); o tanque só drena com propulsor aceso, então basta não zerar no meio do desafio.

### 3. Ligue o desafio ao encontro (lacuna atual)
`PlanetApproachTrigger.StartChallenge` procura **só** `QuizChallengeController`. Para escolher o desafio por fase:
1. Adicione a `PhaseData` um campo (ex.: enum `ChallengeKind { Quiz, AsteroidCorridor, ... }`).
2. Em `PlanetApproachTrigger.StartChallenge`, resolva o `IPhaseChallenge` conforme o `ChallengeKind` (um
   `switch` simples é suficiente para um projeto deste tamanho; evite fábricas/injeção de dependência).
3. Mantenha o **quiz como padrão** (`Quiz`), assim as fases existentes continuam funcionando sem migração.
4. O minigame pode terminar pedindo uma pergunta rápida (`QuizChallengeController`) se o GDD exigir os dois; nesse caso
   encadeie: minigame → `onCompleted` do minigame chama o quiz → `onCompleted` do quiz chama o do trigger.

### 4. Feedback e áudio (via EventBus)
- Acerto/erro/seleção já têm eventos (`OnAnswerCorrect`, `OnAnswerIncorrect`, `OnOptionSelected`) e o `AudioManager`
  já reage. Para som novo (ex.: colisão com asteroide), crie um evento no `EventBus` + clipe no `AudioManager`.
- Efeito de dano: pisca leve de cor/tela (barato; ver `lumen-light-performance`). Sem tremor de câmera forte.
- Sobre a música: `OnQuizOpened/Closed` abafam a trilha; use-os também no minigame se quiser o mesmo efeito.

### 5. Montagem na cena
Crie um `Ensure...` no `TutorialSceneBuilder` que coloque o componente do desafio na cena e ligue referências
(use métodos `Configure(...)`, como o do quiz) e rode o menu de novo; não edite a cena na mão.

## Antes de entregar
- Fluxo completo: aproximação → narração → minigame → fragmento → próximo planeta (use o teleporte de teste).
- Perder/colidir repetidamente não trava o jogo nem rouba combustível; pausa e resgate funcionam durante o desafio.
- Sem alocações por quadro nem `Instantiate/Destroy` em loop (`lumen-light-performance`).
- Atualize `README.md` ("O que ainda não existe") e `TODO.md`.
