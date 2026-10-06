# Coleta de Triângulos — Projeto Introdução ao Unity

Jogo 2D feito seguindo o tutorial **"Introdução Rápida"** da disciplina de Jogos Digitais (Insper).
O jogador controla uma bola vermelha dentro de uma arena e precisa coletar todos os triângulos amarelos.

- **Unity:** 6000.6.4f1 (Universal 2D / URP)
- **Itch.io:** _(adicionar link)_

## Como jogar

| Ação | Teclado |
| --- | --- |
| Mover | WASD ou setas |
| Voltar ao menu | ESC |

Colete todos os triângulos para terminar a partida. A tela final mostra quantos foram coletados.

## Estrutura

- `UnityTutorial/Assets/Scenes` — `Menu`, `Game` e `EndGame`
- `UnityTutorial/Assets/Scripts`
  - `PlayerMovement.cs` — movimento com Rigidbody2D (diagonal normalizada), coleta via `OnTriggerEnter2D` e som
  - `GameController.cs` — classe estática que guarda a pontuação entre cenas
  - `MainMenu.cs`, `EndGameMenu.cs`, `ScoreUI.cs` — menus e interface
- `UnityTutorial/Assets/Prefabs` — `Player` e `Coletavel`
- `UnityTutorial/Assets/Editor/TutorialSceneBuilder.cs` — utilitário (menu **Tools > Tutorial**) que gera as cenas e o build WebGL. **Rodar "Gerar Cenas" de novo sobrescreve as cenas e prefabs.**

## Build WebGL

No Unity: **File > Build Profiles > Web > Build**, ou **Tools > Tutorial > Build WebGL** (gera em `UnityTutorial/Builds/WebGL`).
Para o itch.io, compacte o *conteúdo* da pasta `Builds/WebGL` (o `index.html` precisa ficar na raiz do .zip).

## Créditos

- Tutorial "Introdução Rápida" — material da disciplina (Insper).
- Sprites (círculo, quadrado, triângulo): sprites padrão do pacote 2D Sprite da Unity.
- Efeito sonoro `pickup.wav`: gerado proceduralmente para este projeto.
