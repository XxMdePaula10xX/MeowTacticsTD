# ⚖️ Balanceamento — Meow Tactics TD

Filosofia desta rodada: **força bruta não basta — o jogo premia COMPOSIÇÃO**
(sinergias + itens + counter de dano). O começo ficou mais difícil; gatos crus
são mais fracos e mais caros, mas quem monta uma build forte domina.

## O "triângulo" de dano (counterplay)
- **Físico** (Ninja, Arqueiro, Sniper): sofre contra **Fantasma Blindado** (armadura alta).
  → Counter: penetração de armadura (sinergia **Sniper**, item **Furador**).
- **Mágico** (Mago, Xamã): sofre contra **Sombra Mística** (resistência mágica alta).
  → Counter: penetração mágica (sinergia **Místico** nível 3, item **Runa Mística**).
- **Verdadeiro** (Samurai): ignora as duas defesas. É o mais caro (5), mas universal —
  ótimo contra blindados, sombras e o boss.

## Mudanças (antes → depois)

### Economia / custo
- Gatos ficaram mais caros: Ninja **2→3**, Arqueiro **2→3**, Sniper **3→4**,
  Mago **3→4**, Xamã **3** (mantido), Samurai **4→5**.
- Moedas iniciais: 10 (mantido). Resultado: ~3 gatos no turno 1 (antes ~4–5).

### Dano base dos gatos (−15% a −20%)
- Ninja 12→10 · Arqueiro 18→15 · Sniper 40→34 · Mago 25→20 · Xamã 10→8 · Samurai 20→17.

### Inimigos (mais resistentes)
- Fantasminha: vida 30→36.
- Fantasma Blindado: vida 60→68, **armadura 30→45**, RM 5→10.
- Sombra Mística: vida 50→56, armadura 5→10, **RM 30→45**.
- Pesadelo Veloz: vida 25→26 (continua frágil e rápido).
- Rei (boss): vida 700→850, armadura 20→32, RM 20→32.

### Sinergias (bem mais fortes)
- **Ninja** — 2: +20% vel. ataque · 3: +45% vel. ataque e +10% dano.
- **Sniper** — 2: +20% alcance e +30 pen. armadura · 3: +25% dano e +60 pen. armadura.
- **Místico** — 2: +20% dano · 3: +45% dano e +25 pen. mágica.

### Itens (bem mais fortes)
- Garra +20→**+35%** dano · Pata +25→**+40%** vel. ataque · Luneta +20→**+30%** alcance.
- Olho do Tigre +15→**+25%** crítico · Furador +20→**+45** pen. armadura · Runa +20→**+45** pen. mágica.
- Garra Fantasma +15→**+22** dano verdadeiro · Amuleto Gélido lentidão 20%→**30%** (2s) ·
  Bomba de Erva raio de área 1.5→**2.0**.

## Resultado esperado
- **Início (ondas 1–3):** mais punitivo. Comprar gatos aleatórios e largar deixa
  inimigos vazarem. Boa colocação + foco numa sinergia (ex.: Místico = Mago + Xamã)
  segura as ondas.
- **Meio/fim:** quem ativa sinergias e equipa itens nos gatos certos fica forte;
  o boss exige penetração ou dano verdadeiro (Samurai).

> Para ajustar tudo isso, mexa nos métodos `GenerateCats`, `GenerateEnemies`,
> `GenerateSynergies` e `GenerateItems` em `Assets/Scripts/Editor/MeowSetup.cs`,
> e em `Assets/Scripts/Core/GameBalance.cs` (economia global).
