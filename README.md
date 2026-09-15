# LUMEN — Primeira versão jogável (Fase 1: Tutorial)

Este pacote contém os scripts da Fase 1 (Tutorial) do projeto LUMEN, prontos para
importar em um projeto Unity 6.3 LTS (URP).

**Aviso importante:** estes scripts foram escritos e revisados, mas não puderam ser
compilados/testados dentro do Editor da Unity (este ambiente não tem a Unity instalada).
Se algum erro de compilação aparecer no Console ao importar, me avise com o texto do erro
que eu corrijo rapidamente.

## O que está incluído

- Movimento do LUMEN (WASD + Space/Ctrl), com aceleração/desaceleração e rotação suave.
- Sistema de energia com dreno suave (sem pressão de tempo, como o Tutorial pede).
- Câmera de perseguição simples.
- HUD com barra de energia e texto de instrução.
- Gatilho invisível que marca a conclusão do tutorial (loga no Console).
- Um script de Editor (`LUMEN > Montar Cena de Tutorial`) que monta a cena inteira
  automaticamente — você não precisa criar GameObjects na mão.

## O que NÃO está incluído ainda (próxima iteração — Fase 2: Netuno)

- Modelo 3D real do LUMEN (por enquanto é uma cápsula placeholder da própria Unity).
- Skybox estrelado / ambiente espacial.
- PhaseManager, PhaseData e sistema de quiz (a Fase 1 não usa desafio).
- Áudio (narração da NOVA, propulsor, ambiente).

## Passo a passo

### 1. Configurar o Input (obrigatório antes de tudo)

Os scripts usam o sistema de Input clássico (`Input.GetAxis`). Projetos novos da
Unity 6 às vezes vêm configurados só para o "Input System" novo, o que quebraria
os scripts. Verifique:

`Edit > Project Settings > Player > Active Input Handling` → mude para
**"Input Manager (Old)"** ou **"Both"**. A Unity vai pedir para reiniciar o Editor.

### 2. Importar os scripts

Copie a pasta `Assets/_Project/Scripts` inteira (com as subpastas Core, Gameplay,
Camera, UI e Editor) para dentro do seu projeto Unity, no mesmo caminho
`Assets/_Project/Scripts`.

### 3. Criar a cena

`File > New Scene` → template "Basic (URP)" → salve como
`Assets/_Project/Scenes/Game.unity`.

### 4. Montar a cena automaticamente

Na barra de menu da Unity, clique em **LUMEN > Montar Cena de Tutorial**.
Isso cria: GameManager, o LUMEN (cápsula), a câmera com o script de perseguição
já configurado, o gatilho de saída do tutorial e o Canvas de HUD.

### 5. Testar

Pressione **Play**. Controles:

- `W` / `S` — mover para frente / para trás
- `A` / `D` — mover para os lados
- `Space` — subir
- `Left Ctrl` — descer

A barra de energia no canto superior esquerdo vai drenar bem devagar. Ao voar
cerca de 35 unidades para frente (eixo Z), o Console vai mostrar
"Tutorial concluído — próxima parada: Netuno." — esse é o sinal de que a Fase 1
está funcionalmente completa.

## Atualização: cenário e trilha sonora (para a entrega Demo 1)

O script de montagem de cena (`LUMEN > Montar Cena de Tutorial`) agora também:

- Aplica o skybox estrelado do pacote "Planets of the Solar System 3D" (fica em
  `Window > Rendering > Lighting > Environment`, mas o menu já faz isso por você).
- Instancia o Sol, Netuno e uma nebulosa ao fundo, bem longe da zona de voo do
  tutorial — só para dar profundidade visual. Netuno gira lentamente.
- Cria um `AudioManager` com um `AudioSource`, pronto para tocar a trilha em loop.

### Pré-requisito

Importe os dois pacotes primeiro (Asset Store > My Assets, ou arrastando o `.fbx`/
pacote baixado para dentro de `Assets`): **Planets of the Solar System 3D** e o
**LowpolySpaceshipPack** (se ainda não tiver feito isso pelo passo anterior).

### Passos

1. Exporte a trilha do Suno (mp3) e arraste o arquivo para dentro de
   `Assets/_Project/Audio/Music` no Project (crie a pasta se não existir).
2. Rode `LUMEN > Montar Cena de Tutorial` de novo — é seguro, ele não duplica nada
   que já existe, só adiciona o que estiver faltando (skybox, planetas, AudioManager).
3. Selecione o objeto `AudioManager` na Hierarchy e arraste o clipe de música
   importado para o campo **Background Music** no Inspector.
4. Se o Sol, Netuno ou a nebulosa aparecerem grandes ou pequenos demais, ou muito
   perto/longe, ajuste a posição e a escala deles direto no Inspector — isso eu
   não consigo calibrar sem ver a cena renderizada.
5. Dê Play. A trilha deve começar sozinha (em loop) e o fundo deve estar
   visivelmente mais "vivo".

### Observação de performance

Alguns pacotes de asset trazem um perfil de pós-processamento (bloom, etc.)
dentro da cena de exemplo deles. Não importe/ative esse perfil de pós-processamento
na nossa cena — para a Iris Xe, mantenha o pós-processamento off ou bem leve.
Skybox e alguns objetos decorativos parados custam muito pouco; bloom pesado custa bem mais.

## Próximo passo sugerido

Quando você validar que isso roda bem no seu hardware, seguimos para a Fase 2
(Netuno): PhaseManager, PhaseData, sistema de quiz e a primeira fala real da NOVA.
