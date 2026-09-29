# LUMEN — A viagem de volta para casa

Jogo educativo de viagem pelo Sistema Solar. Você pilota o satélite **LUMEN**,
que foi arrastado para longe da rota depois de uma tempestade solar, e precisa
levá-lo de volta para a Terra.

Combustível é o que dá forma à viagem inteira: voar custa. Por isso a missão é
**planeta a planeta** — cada parada é obrigatória, e é em cada uma que o LUMEN
recupera energia e você aprende alguma coisa sobre aquele mundo.

## A história

> **SINAL PERDIDO...**
> Uma tempestade solar atingiu o satélite LUMEN e interrompeu a conexão com a Terra.
> Arrastado para longe da rota original, LUMEN ficou sem combustível no meio do caminho de volta.
> **BASE TERRA:** sinal fraco reestabelecido. Orientando os primeiros passos...

A partida começa com o LUMEN parado no espaço. Você reativa os propulsores,
atravessa o farol que marca o fim do tutorial e parte para Netuno — o planeta
mais distante, e o primeiro de uma rota que só termina quando o LUMEN voltar
para casa.

## Como jogar

### Comandos

| Tecla | Ação |
| --- | --- |
| `W` / `S` | Avançar / recuar |
| `A` / `D` | Girar a nave (o rumo fica guardado) |
| `Space` | Subir |
| `Left Ctrl` | Descer |
| `X` | Frear (não gasta combustível) |
| `Shift` | Boost — liberado depois do tutorial |
| `R` | Resgate (só quando o combustível zera) |
| `Esc` | Confirmar saída do jogo |

### Pilotando

A câmera fica **atrás do satélite**, acompanhando só o giro horizontal dele. Na
prática: o horizonte nunca tomba, `W` leva a frente e `A`/`D` viram a nave como
um avião — o que você vê girando é o espaço, não a câmera.

Soltar a tecla de avanço **freia a nave** (não é gelo), então dá para ajustar a
aproximação de um planeta sem derrapar. Girar não gasta combustível: dá para ficar
orbitando enquanto se decide o próximo passo.

### Combustível

Voar tem custo, e é isso que organiza a partida:

- O tanque **só drena com os propulsores acesos**. Parado, girando ou freando, o
  LUMEN não queima nada.
- O tanque cheio dá cerca de **3 minutos e meio de propulsão contínua**.
- Cada **fragmento de conhecimento** coletado devolve boa parte do tanque, então
  a recolha é também o seu posto de abastecimento.
- O HUD avisa (`COMBUSTÍVEL BAIXO`) antes de você ficar sem nada.
- **Se o combustível acabar, o LUMEN para no lugar.** Não existe resgate
  automático: aperte `R` e o satélite volta ao último planeta onde você esteve,
  com o tanque cheio. Nenhum fragmento é perdido.

O combustível foi calibrado junto com a rota: dá para chegar ao próximo planeta
com folga, mas não para atravessar a rota inteira voando reto.

### O que aparece na tela

- **SINAL** — a conexão com a Terra. Começa fraco, cresce conforme você se
  aproxima do planeta e dá um salto a cada fragmento.
- **COMBUSTÍVEL** — o tanque, em porcentagem.
- **Seta de navegação** — sempre aponta o próximo destino, e a distância até ele
  fica escrita no objetivo. Se o alvo estiver atrás de você, a seta vira para
  baixo: é hora de dar meia-volta.
- **Fragmentos** — o contador do que já foi coletado.

## A rota

A rota é **zigue-zague** de propósito: a seta guia o LUMEN planeta a planeta, e
não existe atalho em linha reta até a Terra.

| # | Planeta | O que o quiz pergunta |
| --- | --- | --- |
| 1 | Netuno | Os ventos mais fortes do Sistema Solar, a 2.100 km/h |
| 2 | Urano | Por que ele gira "deitado" e tem estações de 21 anos |
| 3 | Saturno | O que forma os anéis de gelo e rocha |
| 4 | Júpiter | A Grande Mancha Vermelha, uma tempestade maior que a Terra |
| 5 | Marte | Por que a superfície é vermelha (óxido de ferro) |
| 6 | Terra | O que torna a Terra o único mundo com água líquida e vida |

A cada aproximação, a NOVA narra o que você está vendo e o desafio aparece. Errar
não penaliza: você tenta de novo até acertar. Acertando, o fragmento é
coletado, o tanque se reenche e a seta aponta o próximo mundo. A missão acaba no
quiz da Terra.

## Rodando o jogo

Precisa do **Unity 6 (URP)**. Antes de abrir a cena:

1. Em `Edit > Project Settings > Player > Active Input Handling`, deixe em
   **"Input Manager (Old)"** ou **"Both"** (o Unity pede para reiniciar o Editor).
2. Importe o pacote de assets **Planets of the Solar System 3D** para `Assets` —
   ele é pago e **não** está incluído aqui.
3. Crie uma cena `File > New Scene` (template **Basic (URP)**) e salve em
   `Assets/_Project/Scenes/Game.unity`.
4. Rode o menu **`LUMEN > Montar Cena de Tutorial`**. Ele monta tudo — menu
   inicial, nave, câmera, HUD, planetas, quizzes, narração e áudio — e é seguro
   rodar mais de uma vez.
5. Aperte **Play**.

O botão **Jogar** (ou `Enter`) abre a introdução; a partir daí o LUMEN está no
seu controle.

## Editando o conteúdo do jogo

Todo o texto — falas da NOVA, perguntas, alternativas e feedbacks — fica em
arquivos de dados dentro de `Assets/_Project/Data`. Basta abrir o arquivo no
Inspector e editar, sem mexer em código.

## O que ainda não existe

- [ ] Minigames próprios de Saturno e Júpiter (hoje usam o mesmo quiz)
- [ ] Modelo 3D do LUMEN (hoje é uma cápsula simples)
- [ ] Tela de final de jogo

Feedback de erros no Console do Unity é bem-vindo.
