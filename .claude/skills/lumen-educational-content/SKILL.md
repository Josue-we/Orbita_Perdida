---
name: lumen-educational-content
description: Como escrever e revisar o conteúdo educativo do jogo LUMEN / Órbita Perdida - perguntas de quiz, alternativas, feedbacks, falas da NOVA e curiosidades sobre os planetas do Sistema Solar, em português do Brasil, com precisão científica. Use SEMPRE que o usuário pedir novas perguntas, mais curiosidades, revisão de textos, "falas da NOVA", dados de um planeta, ou ajustar dificuldade/linguagem do quiz - e ao mexer em QuizBank.cs, PhaseData ou QuizQuestionData.
---

# Conteúdo educativo (ciências, Sistema Solar)

O jogo é um projeto da disciplina de Ciências (UNIVATES): o valor dele está em o jogador **aprender algo
correto e interessante** em cada planeta. Uma pergunta errada ou ambígua estraga isso mais do que
qualquer bug visual, então a prioridade é: correto > claro > curto > divertido.

## Fatos: nunca de memória

Números e afirmações científicas (distâncias, temperaturas, velocidade do vento, número de luas, datas de
descoberta) mudam com novas missões e são fáceis de lembrar errado. Antes de escrever ou aprovar um fato:
1. Confirme em fonte primária: NASA Science (science.nasa.gov), ESA, JPL. Use a busca na web.
2. Escreva no feedback o dado **com contexto** (ex.: "ventos de até ~2.100 km/h"), nunca um número solto.
3. Se as fontes divergirem ou o dado for estimativa, use "cerca de" e arredonde.
4. Informe ao usuário quais fontes usou, para ele citar no trabalho acadêmico.

Fatos já usados no jogo (README): Netuno - ventos mais fortes do Sistema Solar (~2.100 km/h); Urano - gira
"deitado", estações de ~21 anos; Saturno - anéis de gelo e rocha; Júpiter - Grande Mancha Vermelha, tempestade
maior que a Terra; Marte - superfície vermelha por óxido de ferro; Terra - único mundo conhecido com água líquida e vida.

## Formato de uma pergunta (`QuizBank.cs`)

```csharp
Q("Pergunta clara, com uma única resposta correta?",
  "ALTERNATIVA CERTA (sempre a primeira)",
  "Errada plausível A",
  "Errada plausível B",
  "Feedback de acerto: confirma e ENSINA um dado extra (1-2 frases).",
  "Feedback de erro: dá uma pista sem entregar a resposta."),
```

- **A certa é sempre a primeira** no código; o jogo embaralha em tempo de execução (`QuizChallengeController`).
- **3 alternativas** (o GDD aceita 2 ou 3). As erradas devem ser plausíveis e de tamanho parecido com a certa,
  senão a resposta "aparece" pelo tamanho. Evite "todas as anteriores" e "nenhuma das anteriores".
- **Pergunta:** uma ideia só, sem negação dupla, sem pegadinha. Prefira "Por quê?/O que?/Como?" com resposta
  que o jogador acabou de ouvir na narração ou pode deduzir.
- **Feedback de acerto:** reforça e acrescenta uma curiosidade. É o que o jogador mais lê (a tela fica ~2,5 s).
- **Feedback de erro:** o erro **não tem penalidade** no jogo; o texto deve incentivar ("Quase..."), dar pista e
  nunca ridicularizar. Não revele qual alternativa é a certa.
- Tamanho (para caber no painel do quiz, fontes grandes em 1920x1080): pergunta até ~140 caracteres,
  alternativas até ~70, feedbacks até ~160. Teste visualmente no Unity se passar disso.
- Cada planeta tem um pool de 11 perguntas (1 original + 10 do banco); o jogo sorteia sem repetir entre
  partidas. Novas perguntas devem **cobrir temas diferentes** das existentes - leia o pool do planeta antes.
- Depois de editar o `QuizBank`, o usuário roda `LUMEN > Montar Cena de Tutorial` para gerar os assets.

## Voz da NOVA (narradora IA) - `PhaseData.NarrationLines`

- Falas **curtas** (1-2 frases por linha; cada linha aparece na tela e deve ser lida sem pressa).
- Estrutura que o GDD descreve: (1) fala de entrada curta ("Estamos entrando na órbita de Júpiter."), (2) narração
  principal com a curiosidade, (3) ao acertar, comemoração curta.
- Tom: calmo, curioso e acolhedor; NOVA é uma IA de bordo que guia o LUMEN. Sem gírias datadas, sem sarcasmo.
- A curiosidade da narração deve ser a **mesma** que a pergunta cobra (o jogador precisa poder acertar ouvindo a NOVA).
- LUMEN é o satélite (avatar do jogador); a Base Terra fala pelo rádio, com sinal fraco no início.

## Linguagem e acessibilidade

- Português do Brasil, com acentuação correta nos textos do jogo (nos comentários do código o projeto às vezes omite acentos; o texto do jogador não).
- Público: estudantes de ensino fundamental/médio (confirmar com o usuário a série-alvo antes de aumentar a dificuldade).
  Explique termos técnicos na primeira vez ("metano, um gás que absorve a luz vermelha").
- Unidades: km/h, °C, anos terrestres; escreva números com ponto de milhar (2.100) e vírgula decimal (1,5).
- Não afirme como fato o que é teoria ou hipótese ("muitos cientistas acham que...").

## Checklist de revisão

- [ ] Cada fato foi conferido em fonte confiável (e anotado).
- [ ] Só uma alternativa pode ser considerada correta por um aluno atento.
- [ ] Alternativas erradas são plausíveis e de tamanho parecido; certa na primeira posição no `QuizBank`.
- [ ] Feedback de acerto ensina algo; feedback de erro dá pista sem entregar.
- [ ] Texto cabe no painel; sem erros de português; tom da NOVA consistente.
- [ ] Não repete tema de outra pergunta do mesmo planeta.
