# 🐱 Meow Tactics TD

Tower Defense tático de gatos contra fantasmas e pesadelos, feito em **Unity (C#)**.
Você posiciona gatos defensores, ativa **sinergias** combinando tipos de gato e usa
**itens** (ganhos a cada 3 ondas) para montar builds e segurar 10 ondas até o boss final.

> Este README é o seu guia. Foi escrito assumindo que **você não programa**. Siga os
> passos na ordem e vai dar certo. 🐾

---

## ✅ O que já está pronto (MVP)

- 1 mapa com caminho fixo e **12 slots** de posicionamento
- **10 ondas** + **boss** (Rei dos Pesadelos)
- **6 gatos** (Ninja, Arqueiro, Sniper, Mago, Xamã, Samurai)
- **5 inimigos** (Fantasminha, Fantasma Blindado, Sombra Mística, Pesadelo Veloz, Boss)
- **3 tipos de dano**: Físico, Mágico e Verdadeiro (com armadura, resistência mágica e penetração)
- **3 sinergias**: Ninja (vel. ataque), Sniper (alcance / pen. armadura), Místico (dano)
- **Loja aleatória** (5 gatos), compra, venda e atualizar (reroll)
- **Banco** com 8 espaços
- **Sistema de itens**: a cada 3 ondas você escolhe 1 de 3 itens. Equipe nos gatos.
  - Itens de status, itens especiais (dano verdadeiro, lentidão, área) e **distintivos**
    que dão uma **sinergia extra** ao gato (ex: fazer um Ninja contar também como Sniper)
- Vida do jogador, vitória e derrota
- Interface completa montada **automaticamente por código**
- Visuais **placeholder** (círculos/quadrados coloridos) já prontos para trocar por arte

### Duas decisões de design que tomamos juntos
1. **Sem sistema de estrelas** (juntar 3 gatos iguais). Tiramos do MVP para simplificar.
   No lugar, comprar gatos iguais serve para **encher slots e ativar sinergias**.
2. **Sistema de itens** entrou como a fonte de "força" da partida (no lugar das estrelas).

---

## 🚀 Como rodar (passo a passo)

### 1. Instalar o Unity
1. Baixe o **Unity Hub**: https://unity.com/download
2. No Hub, instale uma versão **Unity 2022.3 LTS** (qualquer 2022.3.x serve).
   - Para exportar para iPhone depois, marque o módulo **iOS Build Support** na instalação.

### 2. Abrir o projeto
1. No Unity Hub, clique em **Add → Add project from disk**.
2. Selecione a pasta deste projeto (a que contém as pastas `Assets`, `Packages`, `ProjectSettings`).
3. Abra o projeto (o Unity pode levar alguns minutos na primeira vez).

### 3. Gerar tudo com 1 clique (a parte mágica 🪄)
No topo do Unity vai aparecer um menu chamado **MeowTactics**. Clique em:

> **MeowTactics → Fazer Tudo (gerar conteúdo + montar cena)**

Isso cria sozinho todos os gatos/inimigos/ondas/itens **e** monta a cena de jogo.
(Se preferir fazer em etapas, use os itens "1. Gerar Conteúdo" e "2. Montar Cena".)

### 4. Jogar
1. Abra a cena `Assets/Scenes/Game.unity` (dê dois cliques nela na aba *Project*).
2. Aperte o botão **▶ Play** no topo do editor.

---

## 🎮 Como jogar

1. Você começa com **10 moedas** e **20 vidas**, na fase de preparação.
2. Na **loja** (rodapé), clique num gato para comprá-lo → ele vai para o **banco**.
3. Clique num gato do **banco** para selecioná-lo, depois clique num **slot** no mapa para posicioná-lo.
4. O painel de **sinergias** (direita) mostra quais estão ativas conforme você posiciona gatos.
5. Clique em **INICIAR ONDA**. Os gatos atacam sozinhos os inimigos no alcance.
6. Ganhe moedas matando inimigos e ao fim de cada onda. Entre ondas, compre/venda/reposicione.
7. **A cada 3 ondas** aparece a **escolha de itens**. Escolha 1. Depois, clique no item
   (painel **ITENS**, à esquerda) e clique num **gato posicionado** para equipar.
8. Clique num gato posicionado para ver detalhes, vender ou devolver ao banco.
9. Sobreviva às 10 ondas e derrote o boss = **vitória**. Vidas chegando a 0 = **derrota**.

> Dica: você só equipa itens em gatos que já estão **no mapa** (não no banco).

---

## ⚖️ Como mexer no balanceamento (sem programar)

Quase tudo é editável clicando nos arquivos dentro de `Assets/ScriptableObjects/`:

- **Gatos** → `ScriptableObjects/Cats/` (dano, alcance, velocidade, custo, sinergias...)
- **Inimigos** → `ScriptableObjects/Enemies/` (vida, armadura, velocidade, recompensa...)
- **Ondas** → `ScriptableObjects/Waves/` (quais inimigos, quantos, intervalo...)
- **Sinergias** → `ScriptableObjects/Synergies/` (quantos gatos para ativar e qual bônus)
- **Itens** → `ScriptableObjects/Items/` (efeitos e distintivos)

Clique no arquivo, edite os campos na aba **Inspector** (lado direito) e pronto.

Regras gerais do jogo (moedas iniciais, tamanho da loja, itens a cada N ondas, etc.)
ficam em **um único arquivo de código**: `Assets/Scripts/Core/GameBalance.cs`.

---

## ➕ Como adicionar conteúdo novo

- **Novo gato:** menu `Assets → Create → MeowTactics → Cat Data`. Preencha os campos.
  Depois arraste-o para o campo *Available Cats* do **ShopManager** (no objeto `GameSystems` da cena).
- **Novo inimigo:** `Assets → Create → MeowTactics → Enemy Data`, e use-o em alguma onda.
- **Nova onda:** `Assets → Create → MeowTactics → Wave Data`, e adicione na lista *Waves* do **WaveManager**.
- **Nova sinergia:** `Assets → Create → MeowTactics → Synergy Data`, e adicione na lista do **SynergyManager**.
- **Novo item:** `Assets → Create → MeowTactics → Item Data`, e adicione na lista do **ItemManager**.

> Mais simples ainda: edite o gerador em `Assets/Scripts/Editor/MeowSetup.cs` e rode de novo.

---

## 🗂️ Estrutura do código (resumo)

```
Assets/Scripts/
  Core/        Enums, GameBalance (regras gerais)
  Data/        ScriptableObjects: CatData, EnemyData, WaveData, SynergyData, ItemData
  Combat/      DamageCalculator, DamageContext (fórmulas de dano)
  Cats/        CatUnit (gato no jogo: mira, ataque, itens)
  Enemies/     EnemyUnit, HealthBar (inimigo andando + barra de vida)
  Map/         MapManager (caminho/slots), MapSlot (slot clicável)
  Managers/    GameManager, WaveManager, EconomyManager, ShopManager,
               BenchManager, ItemManager, SynergyManager, PlacementManager
  UI/          UIManager (monta toda a interface), UIFactory (helpers de UI)
  Utilities/   SpriteFactory (placeholders), UnitFactory (cria gatos/inimigos)
  Editor/      MeowSetup (os botões mágicos de gerar conteúdo e montar cena)
```

---

## ⚠️ Limitações conhecidas / o que ainda falta

- **Visual é placeholder** (formas coloridas). A arte final entra depois, trocando os sprites.
- **Áudio** ainda não implementado (estava marcado como opcional no MVP).
- **iOS**: o código já está pensado para landscape, mas exportar para iPhone exige um Mac,
  o Xcode e uma conta de desenvolvedor Apple (US$ 99/ano). Fazemos isso numa etapa futura.
- Itens só podem ser equipados em gatos **posicionados** (não no banco).
- Não testado dentro do Unity ainda — foi escrito com cuidado, mas se aparecer algum
  erro vermelho no Console, me mande o texto do erro que eu corrijo na hora.

---

## 🛣️ Próximos passos sugeridos

1. Abrir no Unity, rodar os geradores e **testar a primeira partida**.
2. Ajustar balanceamento (dificuldade das ondas, força dos itens).
3. Trocar placeholders por arte dos gatos e inimigos.
4. Adicionar áudio (cliques, ataques, vitória/derrota).
5. Mais mapas, mais gatos e mais sinergias.
6. Preparar o build de iOS.

Bom jogo! 🐈
