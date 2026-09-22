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

## Atualização: efeitos sonoros (seleção, acerto, erro, coleta)

O `AudioManager` agora também tem 4 campos de efeitos sonoros, e reage sozinho
a eventos do jogo (mesmo que esses eventos ainda não sejam disparados por
nenhum sistema real — isso vem com a Fase 2/quiz). Quando esse sistema
existir, ele só vai chamar `EventBus.RaiseAnswerCorrect()` etc., e o som já
toca automaticamente, sem precisar mexer no AudioManager de novo.

### Fontes recomendadas (gratuitas, CC0)

- **Interface Sounds** (kenney.nl/assets/interface-sounds) — para seleção, acerto, erro.
- **Sci-fi Sounds** (kenney.nl/assets/sci-fi-sounds) — para o som de coleta do fragmento.

### Passos

1. Baixe os pacotes e escolha 4 arquivos: um de clique/seleção, um de acerto,
   um de erro, um de coleta.
2. Renomeie os arquivos para conter uma palavra-chave reconhecível antes de
   importar — por exemplo `select_beep.wav`, `correct_chime.wav`,
   `incorrect_buzz.wav`, `collect_pickup.wav`. Isso ajuda o script a
   identificar automaticamente qual é qual.
3. Arraste os 4 arquivos para `Assets/_Project/Audio` (pode ser direto na
   pasta raiz, ou dentro de uma subpasta `SFX` — tanto faz, a busca é recursiva).
4. Rode `LUMEN > Montar Cena de Tutorial` de novo.
5. Olhe o Console: cada som atribuído automaticamente aparece como log; cada
   som que não foi encontrado aparece como aviso (warning) dizendo qual
   palavra-chave ele procurou. Se algum não bater, arraste manualmente no
   Inspector do `AudioManager`.

Nenhum desses sons toca sozinho ainda na Fase 1 (não tem quiz nem coleta de
fragmento no tutorial) — isso é esperado. Eles vão tocar assim que a Fase 2
existir.

## Atualização: abertura da história

Antes do jogador ganhar controle do LUMEN, agora roda uma sequência de texto
estilo "log de sistema" contando a tempestade solar, a perda de contato com a
Terra e a falta de combustível — sem áudio, só texto na tela (decisão tomada
junto com você). O controle do LUMEN fica travado até a sequência terminar.

- Pressione qualquer tecla para avançar uma linha antes da hora.
- Pressione Esc para pular a sequência inteira de uma vez.
- O texto está fixo em `IntroSequenceController.cs` por enquanto — quando o
  sistema de dados (ScriptableObjects) existir na Fase 2, isso pode migrar
  para lá, mas não é obrigatório.

Rode `LUMEN > Montar Cena de Tutorial` de novo para gerar o painel de intro
automaticamente. Nada muda no que já existia — é só mais uma etapa antes do
tutorial começar.

## Atualização: Fase 2 (Netuno) — quiz, NOVA e fragmentos

Essa é a primeira fase de verdade além do tutorial. Sistemas novos:

- **Dados** (`Scripts/Data`): `PhaseData` e `QuizQuestionData`, ScriptableObjects.
  O construtor de cena já cria as instâncias de Netuno automaticamente em
  `Assets/_Project/Data/Phases` e `Assets/_Project/Data/Quizzes` — você pode
  abrir esses arquivos no Inspector e editar o texto da pergunta, das
  alternativas ou das falas da NOVA livremente, sem mexer em código.
- **NOVA** (`Scripts/Narrative/NovaNarrationController`): legenda na parte de
  baixo da tela (diferente da intro, que é tela cheia) — não trava a visão do
  jogo.
- **Quiz** (`Scripts/Gameplay/Challenges/QuizChallengeController`): painel
  central com a pergunta e até 3 alternativas. Errar mostra feedback e deixa
  tentar de novo; acertar libera o fragmento.
- **Gatilho do planeta** (`Scripts/Gameplay/PlanetApproachTrigger`): colocado
  no "Netuno_Encounter" na cena, a ~150 unidades à frente do ponto de partida.
  Trava o movimento, toca a narração, mostra o quiz, e ao acertar devolve
  energia e libera o jogador — tudo isso é reaproveitável: Urano e Marte vão
  usar exatamente essa mesma classe, só com um `PhaseData` diferente.

### Correção importante: Rigidbody no LUMEN

Adicionei um `Rigidbody` cinemático (sem gravidade) no LUMEN. Sem isso, a
Unity não garante disparar `OnTriggerEnter` quando os objetos se movem só por
`transform.position` (como o nosso LumenController faz) — é uma pegadinha
conhecida da engine. Isso pode ter afetado o gatilho de saída do tutorial
desde o início. Depois de atualizar os scripts, teste de novo o tutorial e
confirme que a mensagem "Tutorial concluído" aparece no Console.

### Passos

1. Substitua todos os scripts pela versão nova (são bastante arquivos dessa
   vez — o mais seguro é apagar a pasta `Assets/_Project/Scripts` inteira do
   seu projeto e colar a nova por cima).
2. Rode `LUMEN > Montar Cena de Tutorial` de novo.
3. Play. Atravesse o gatilho do tutorial (deve liberar o espaço de voo), e
   continue voando para frente (eixo Z positivo) até avistar Netuno.
4. Ao entrar na órbita, a NOVA fala, depois o quiz aparece. Responda —
   errar deixa tentar de novo, acertar dá o fragmento e libera o movimento.
5. Confira o HUD: o contador de fragmentos deve ir de 0/5 para 1/5.

## Próximo passo sugerido

Com Netuno funcionando, Urano e Marte são "quase de graça": basta duplicar o
padrão de `EnsureNetunoPhase()` no construtor de cena com um novo `PhaseData`
e reposicionar o gatilho mais à frente. Depois disso, partimos para os dois
minigames (Saturno e Júpiter) e a Fase 7 (Retorno à Terra).
