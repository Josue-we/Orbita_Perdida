---
name: lumen-light-performance
description: Regras e checklist para manter o jogo LUMEN / Órbita Perdida leve em Unity 6 (URP) em computadores SEM placa de vídeo dedicada - sombras, transparência/overdraw, partículas, asteroides e minigames, texturas, áudio, alocações de memória, taxa de quadros. Use SEMPRE que adicionar ou alterar planetas, partículas, efeitos visuais, minigames, UI animada, áudio, iluminação ou qualquer coisa que rode por quadro, e quando o usuário falar em "lento", "travando", "FPS", "pesado", "otimizar" ou "rodar em PC fraco".
---

# Jogo 3D leve: regras de desempenho do LUMEN

Requisito do projeto: rodar em máquinas sem GPU dedicada (notebook de faculdade, laboratório).
Nessas máquinas o gargalo quase nunca é a quantidade de triângulos; é **preenchimento de pixels
(transparência/overdraw)**, sombras e trabalho repetido por quadro. Por isso as regras abaixo
priorizam isso, e o espaço aberto do jogo (poucos objetos, fundo estrelado) ajuda muito.

## O que o projeto já faz (não desfaça)

- `PerformanceTuning` (`Environment/`): `targetFrameRate = 60`, sombras de luz direcional desligadas,
  remove os sistemas de partículas "Glow" dos prefabs de planeta (um billboard de brilho com ~1000
  unidades de largura cobria a tela inteira com transparência - custo de preenchimento altíssimo).
- URP: `m_MainLightShadowsSupported: 0` e sombras suaves desligadas em `Mobile_RPAsset` e `PC_RPAsset`;
  `Mobile_RPAsset` usa `m_RenderScale: 0.8`; MSAA baixo (`m_MSAA: 1`).
- Planetas = prefabs do pacote (esferas com textura), escala ajustada por planeta no builder; rotação lenta por `SlowRotator` (barato).
- Rota espaçada (planetas a milhares de unidades): quase nunca há muitos objetos no frustum ao mesmo tempo.

## Checklist ao adicionar algo novo

**Visual**
- Evite materiais transparentes grandes na tela (halos, névoa, brilhos). Se precisar de brilho, use
  sprite pequeno, `Additive` com poucos quads, ou emissão no material em vez de partículas grandes.
- Sombras continuam desligadas. Sem luzes extras em tempo real; uma luz direcional (Sol) basta.
- Pós-processamento (Bloom, DoF, motion blur, SSAO) fica desligado por padrão. Se o usuário quiser um
  brilho "dourado" na coleta, prefira partículas pequenas a um Bloom de tela cheia.
- Texturas: potência de 2, só o tamanho necessário (1024-2048 para planetas), compressão ligada,
  mipmaps ligados, sem leitura/escrita (`Read/Write` desligado). Skybox estrelado fixo (material `Skybox.mat` do pacote de planetas).
- Mesmo material em muitos objetos (asteroides) para o SRP Batcher/instancing agruparem. Use poucas variações de malha.

**Partículas (explosão dourada da coleta, propulsor)**
- `Max Particles` baixo (30-60), vida curta, tamanho pequeno, sem colisão, sem sub-emissores, `Simulation Space` local quando possível.
- Uma instância reaproveitada (`Play()` ao coletar) em vez de `Instantiate` a cada vez.

**Minigames (asteroides, ventos, anéis)**
- Contagem pequena (dezenas, não centenas) e **objeto reaproveitado** (pool) em vez de `Instantiate/Destroy`.
- Sem `Rigidbody` por asteroide. O jogo já usa movimento sem física (`LumenController`); faça os asteroides
  se moverem por `Transform` num único manager (um `Update` que percorre o array) com colisores
  em modo trigger, ou teste de distância esfera-esfera.
- Cancele/oculte o campo quando o minigame acaba (`SetActive(false)`), em vez de deixá-lo rodando fora da vista.

**Código por quadro (`Update/LateUpdate/FixedUpdate`)**
- Nada de `Find*`, `GetComponent*`, `Resources.Load`, LINQ, `string +` ou `new List/array` por quadro: cacheie no `Awake/Start`.
- Atualize texto de UI **só quando o valor muda** (`Text.text = ...` realoca e reconstrói a malha do Canvas).
  Para barras, mexa em `fillAmount`/`Slider.value` em vez de redesenhar textos.
- Prefira eventos (`EventBus`) a "perguntar todo quadro". Coroutines com `WaitForSeconds` reaproveitável
  ou valores pequenos; sem `yield return new WaitForSeconds` em loop apertado.
- Distâncias: compare `sqrMagnitude` em vez de `Vector3.Distance` onde der.

**Áudio**
- Música longa: `Load Type = Streaming`, formato Vorbis. **Ponto real do projeto:** a trilha da gameplay
  (`Audio/Music/silence-among-the-stars [usesuno.com].wav`) tem ~62 MB em WAV. Reexporte como `.ogg`/`.mp3`
  (a música do menu já é `.mp3`) e use Streaming: reduz memória, tempo de carga e o tamanho do repositório
  (o GitHub avisa em arquivos acima de 50 MB). SFX curtos: `Decompress On Load` ou `Compressed In Memory`, `Preload Audio Data` ligado.
- Narrações da NOVA: `Compressed In Memory`, mono (voz não precisa de estéreo).

**Canvas / UI**
- Separe Canvas estáticos (HUD fixo) de dinâmicos (elementos que mudam todo quadro) para o Canvas não ser
  reconstruído inteiro. Desligue `Raycast Target` em textos/imagens decorativos.

## Como medir (antes de "otimizar")

1. Ver o `Stats` da Game View (FPS, SetPass calls, Batches, Tris) e o **Profiler** (CPU e GPU).
2. Testar com `Render Scale` 0.8 e janela pequena: se o FPS sobe muito, o gargalo é preenchimento de pixels.
3. Comparar antes/depois da mudança e registrar o número (ex.: "Júpiter: 58 → 60 FPS, 140 → 90 batches").
Otimização sem medição vira complexidade sem ganho; é um projeto acadêmico, mantenha o código simples.

## Falsos alarmes conhecidos

O `static_check.py` avisa sobre `Find*` em `SunLight.LateUpdate` e `DebugTeleport.Update`. No `SunLight` as
buscas são guardadas por `if (x == null)` (rodam até achar, depois não repetem) e no `DebugTeleport` só rodam
ao apertar tecla (Editor). Um custo pequeno real: `sun.GetComponent<SunGlow>()` roda todo quadro no
`SunLight.LateUpdate` e poderia ser cacheado.
