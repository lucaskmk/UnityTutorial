# Tanque Cheio — Projeto Introdução ao Unity

Jogo 2D top-down feito a partir do tutorial **"Introdução Rápida"** da disciplina de Jogos Digitais (Insper).
Você dirige um carro vermelho pela cidade e precisa coletar galões de gasolina antes que o tanque esvazie, enquanto a polícia corre atrás de você.

- **Unity:** 6000.6.4f1 (Universal 2D / URP)
- **Itch.io:** https://lkenji-016.itch.io/tanque-cheio
- **Autor:** Lucas Kamikawa

## Como jogar

- Colete **15 galões** para vencer. Cada galão enche 35% do tanque.
- A gasolina acaba com o tempo, e mais rápido quando você está andando. Se zerar, você perde.
- Viaturas **patrulham** a cidade e começam a **perseguir** você quando te veem. Se você sumir de vista, elas voltam a patrulhar.
- A cada alguns galões (2, 6 e 10), uma nova viatura entra direto na perseguição. As viaturas ficam mais rápidas conforme você coleta.
- Você tem **3 vidas**. Ao ser pego, fica alguns segundos invulnerável e a viatura para por um instante.
- **Manchas de óleo** deixam o carro lento e escorregadio.
- O melhor tempo de vitória fica salvo como recorde.

| Ação | Teclado | Controle (Xbox) |
| --- | --- | --- |
| Dirigir | WASD ou setas | Analógico esquerdo ou direcional |
| Pausar | ESC ou P | Start |
| Navegar nos menus | Setas + Enter / mouse | Direcional + A |

## Itens da rubrica

| Item | Onde |
| --- | --- |
| Tutorial | Movimento com Rigidbody2D e diagonal normalizada, coletáveis com trigger e tag `Coletavel`, prefabs, áudio, menu e `GameController` estático |
| Tempo | Cronômetro no HUD, tempo final na tela de fim, gasolina que acaba com o tempo e recorde de tempo |
| Inimigos | `PoliceCar.cs`: patrulha por pontos fixos, perseguição com pathfinding (BFS na grade da cidade) e viaturas que só perseguem |
| Visual | Sprites próprios coerentes com o tema: cidade, carros, galão, óleo e ícones |
| Áudio | Música do menu e do jogo; sons de motor, sirene, coleta, batida, alerta de gasolina, vitória, derrota e clique |
| UI | Vidas, barra de gasolina, galões e tempo; mensagens na tela; menu de pausa |
| Controles | Input System (`InputSystem_Actions`) com suporte a controle |
| Level Design | Cidade com quarteirões, posto e parque; manchas de óleo; dificuldade crescente |

## Estrutura

- `UnityTutorial/Assets/Scenes`: `Menu`, `Game` e `EndGame`
- `UnityTutorial/Assets/Scripts`
  - `PlayerMovement.cs`: movimento do carro, coleta, óleo e dano
  - `PoliceCar.cs`: IA da polícia (patrulha e perseguição)
  - `CityMap.cs`: grade da cidade e pathfinding (BFS)
  - `LevelManager.cs`: cronômetro, gasolina, spawns, pausa e fim de jogo
  - `GameController.cs`: estado estático da partida, compartilhado entre cenas
  - `HUD.cs`, `MainMenu.cs`, `EndGameMenu.cs`, `SelectionKeeper.cs`, `UIAudio.cs`, `ButtonSound.cs`: interface
- `UnityTutorial/Assets/Editor/TutorialSceneBuilder.cs`: utilitário (menu **Tools > Tutorial**) que gera as cenas, os prefabs e o build WebGL. **Rodar "Gerar Cenas" de novo sobrescreve as cenas e os prefabs.**
- `Tools/generate_assets.py`: gera todos os sprites e sons (`python Tools/generate_assets.py`, precisa de Pillow e numpy)

## Build WebGL

No Unity, use **Tools > Tutorial > Build WebGL** (gera em `UnityTutorial/Builds/WebGL`).
Para o itch.io, compacte o **conteúdo** da pasta `Builds/WebGL` (o `index.html` precisa ficar na raiz do .zip) e marque "This file will be played in the browser".

## Créditos

- Tutorial "Introdução Rápida": material da disciplina (Insper).
- Sprites e efeitos sonoros/músicas: gerados proceduralmente para este projeto pelo script `Tools/generate_assets.py`. Não usa assets de terceiros.
- Fonte: Arial (LegacyRuntime), embutida no Unity.
