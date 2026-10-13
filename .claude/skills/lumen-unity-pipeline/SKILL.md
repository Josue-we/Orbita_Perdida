---
name: lumen-unity-pipeline
description: Pipeline de trabalho do projeto LUMEN / Órbita Perdida (Unity 6, URP) - editar os arquivos direto na pasta do projeto, compilar para checar erros, corrigir e refazer até compilar limpo. Use SEMPRE que for criar, alterar, corrigir ou refatorar qualquer script C#, asset ou configuração do jogo, e quando o usuário falar em "compilar", "erro no Console", "CS0xxx", "não compila", "build" ou "rodar o jogo" - mesmo que ele não peça o pipeline explicitamente.
---

# Pipeline de trabalho: editar → compilar → corrigir → repetir

O usuário definiu este fluxo como regra fixa ("siga sempre neste mesmo pipeline").
Ele existe porque o Unity só revela erros de C# quando compila; entregar código sem
compilar empurra o conserto para o usuário, que está num projeto acadêmico com prazo.

## O fluxo

1. **Editar direto na pasta do projeto.** Nada de mandar zip ou trechos soltos de código
   para o usuário colar: altere os arquivos reais em `Assets/_Project/...`.
2. **Compilar** com `scripts/unity_compile.sh` (nesta skill). Ele abre o Unity em
   modo batch, compila e imprime só as linhas `error CS...` e `warning CS...` relevantes.
3. **Corrigir** cada erro na ordem em que aparece (o primeiro costuma causar os seguintes).
4. **Recompilar** até zerar os erros. Só então dizer que terminou.
5. Resumir em 1-3 frases o que mudou e o que o usuário precisa testar dentro do Unity.

```bash
bash .claude/skills/lumen-unity-pipeline/scripts/unity_compile.sh
```

O script lê a versão do Editor em `ProjectSettings/ProjectVersion.txt`
(hoje `6000.3.24f1`) e procura o Unity nos caminhos padrão do Windows, macOS e Linux.
Se o Unity estiver instalado em outro lugar, passe `UNITY_PATH=/caminho/Unity`.

## Quando não dá para compilar

Se o Unity não existir no ambiente (acontece em sessões na nuvem) ou o Editor já estiver
aberto no mesmo projeto (o Unity bloqueia uma segunda instância):

1. Rode `python3 .claude/skills/lumen-unity-pipeline/scripts/static_check.py`. É uma checagem
   estática barata que pega o que mais quebra build neste projeto: `using UnityEditor` fora de
   pasta `Editor`, API obsoleta do Unity 6 (`FindObjectOfType`), chaves desbalanceadas,
   namespace fora do padrão `Lumen.*` e `Find*` dentro de `Update`.
2. **Diga com todas as letras que não compilou de verdade** e peça para o usuário abrir o
   Unity e colar os erros do Console, se houver. Nunca declare "compila sem erros" sem
   ter compilado - essa frase é exatamente o que o usuário não pode ficar esperando.

## Regras que evitam dor de cabeça com arquivos Unity

- **Todo arquivo do Unity tem um `.meta`.** Ao criar um script novo fora do Editor, o Unity gera
  o `.meta` na próxima abertura; avise o usuário para commitar os `.meta` novos. Ao mover/renomear
  um arquivo, mova o `.meta` junto (senão referências em cenas e assets quebram).
- **Não edite à mão** `Assets/Scenes/*.unity`, `*.prefab` ou `*.asset` (YAML do Unity). A cena é
  montada pelo menu `LUMEN > Montar Cena de Tutorial` (`TutorialSceneBuilder`); para mudar a cena,
  mude o builder e peça para o usuário rodar o menu de novo. Exceção: `.asset` simples de dados
  (`PhaseData`, `QuizQuestionData`) podem ser ajustados se a mudança for pequena e o YAML estiver claro.
- **Nunca mexa** em `Library/`, `Temp/`, `Logs/`, `UserSettings/` (ignorados pelo git).
- **Os planetas vêm de um pacote pago** ("Planets of the Solar System 3D") que não está no
  repositório. Sem ele, o builder não acha os prefabs; isso não é erro de código.
- **Áudio e imagens usam Git LFS** (`.gitattributes`). Se um clone avisar "files that should have
  been pointers", os binários vieram como arquivo comum; não "conserte" regravando-os.
- O Input do projeto usa a API antiga (`Input.GetKey`), com `Active Input Handling = Both`.
  Não migre para o novo Input System sem o usuário pedir.

## Antes de dar o trabalho por terminado

- Compilou sem erros (ou avisou claramente que não foi possível).
- Nenhum valor numérico novo ficou "solto" no código: tuning vai para `GameTuning`
  (ver a skill `lumen-architecture`).
- Mudou algo que afeta desempenho? Confira a skill `lumen-light-performance`.
- Mudou comportamento visível? Atualize `README.md` / `TODO.md` se o item estiver listado lá.
