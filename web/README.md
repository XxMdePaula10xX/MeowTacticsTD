# Meow Tactics TD — Web

Port do jogo de tower-defense **Meow Tactics** (originalmente em Unity 6/C#) para
**HTML + CSS + Canvas (JavaScript puro)**, com o objetivo de publicar na **Apple App Store**
empacotado via **Capacitor**.

> O projeto Unity original continua no repositório (raiz) e permanece funcional
> até a versão web atingir paridade. Nada é descartado — a migração é aditiva.

---

## Por que web

- **Distribuição instantânea:** joga direto no navegador; um link basta para testar.
- **Iteração rápida:** sem build da Unity, sem TestFlight para cada ajuste.
- **Mesma arte:** os PNGs dos gatos/inimigos/mapas entram sem retrabalho (Canvas desenha
  o mesmo arquivo que o Unity desenhava).
- **App Store:** o site estático é embrulhado com Capacitor num app iOS nativo
  (WKWebView), mantendo o caminho para a loja.

## Stack

| Camada        | Escolha                                   | Motivo |
|---------------|-------------------------------------------|--------|
| Renderização  | Canvas 2D                                 | leve, controle total, roda em qualquer WebView |
| Linguagem     | JavaScript (ES modules), sem framework    | zero toolchain, igual ao protótipo aprovado |
| Empacotamento | Capacitor                                 | vira app iOS/Android a partir de site estático |
| Dados         | JSON estático (portado do C#)             | mesma fonte de verdade balanceada na Unity |
| Persistência  | `localStorage`                            | equivalente ao PlayerPrefs |

## Estrutura

```
web/
├── index.html            # shell: canvas + HUD + docks
├── styles/
│   ├── tokens.css        # design system (cores, espaçamento, tipografia)
│   └── ui.css            # componentes (loja, painéis, botões, telas)
├── src/
│   ├── main.js           # bootstrap + game loop
│   ├── state.js          # estado da partida (vidas, moedas, onda, modo)
│   ├── engine/
│   │   ├── loop.js       # requestAnimationFrame + fixed timestep
│   │   ├── camera.js     # fit "contain" + screen shake
│   │   ├── input.js      # toque/clique, seleção, posicionamento
│   │   └── render.js     # desenho do campo, caminho, unidades, FX
│   ├── systems/
│   │   ├── waves.js      # spawn, progresso, fim de onda
│   │   ├── combat.js     # mira, projéteis, fórmula de dano (triângulo)
│   │   ├── economy.js    # moedas, custos, venda
│   │   ├── synergies.js  # cálculo de sinergias + buffs
│   │   ├── items.js      # inventário, equipar, drafts
│   │   ├── relics.js     # modificadores de run (roguelite)
│   │   └── save.js       # localStorage
│   ├── ui/
│   │   ├── hud.js        # barra superior (vidas/moedas/onda)
│   │   ├── shop.js       # loja + banco (bench)
│   │   ├── panels.js     # sinergias, ameaça, drafts, menus
│   │   └── screens.js    # menu, seleção de mapa, coleção, fim de jogo
│   └── data/             # PORTADO DO UNITY (gerado da extração)
│       ├── cats.js
│       ├── enemies.js
│       ├── waves.js
│       ├── synergies.js
│       ├── items.js
│       ├── relics.js
│       ├── maps.js
│       ├── achievements.js
│       ├── balance.js
│       └── meta.js       # desafio diário, modos
├── assets/
│   ├── cats/             # PNGs (copiados de Assets/Art/Cats)
│   ├── enemies/
│   └── maps/
└── dist/                 # bundle single-file para publicar como Artifact/playtest
```

## Fidelidade ao original (o que precisa ser portado 1:1)

- **10 gatos** com tipo de dano (Físico ▲ / Mágico ★ / Verdadeiro ◆), penetração, crítico.
- **8 inimigos** com HP, velocidade, armadura, resistência mágica, recompensa, chefes.
- **50 ondas** (1–10 na mão, 11–50 procedurais) + escalonamento suavizado.
- **13 sinergias** estilo TFT (breakpoints + efeitos).
- **Itens** (inventário, equipar até N por gato, drafts a cada N ondas).
- **Relíquias** (13, draft roguelite nas ondas 5/15/25…).
- **3 mapas** (Jardim 1 lane, Bosque 2, Ruínas 3) com caminhos.
- **Modos:** Normal, Infinito (+ recordes), Desafio Diário (modificador do dia).
- **Coleção** + **35 conquistas**.
- **Triângulo de dano:** físico vs armadura, mágico vs resistência, verdadeiro ignora ambos;
  penetração reduz a defesa relevante.
- **Acessibilidade/iOS:** safe-area, orientação paisagem, símbolos de tipo de dano.

## Roadmap de paridade

1. **Fundação** — estrutura, design system, motor (loop/câmera/input/render). ← em curso
2. **Dados** — portar cats/enemies/waves/synergies/items/relics/maps/balance (extração automática).
3. **Core loop** — colocar torres, ondas, combate com triângulo de dano, economia, vidas.
4. **Meta** — banco/board TFT, sinergias, itens, drafts.
5. **Modos** — Normal + Infinito + Diário + recordes.
6. **Telas** — menu, seleção de mapa, coleção, conquistas, fim de jogo.
7. **Polish** — FX, som (WebAudio), haptics (Capacitor), notificações locais.
8. **Empacotamento** — Capacitor + ícone + launch screen + build iOS + App Store.

## Rodar localmente

```bash
cd web
python3 -m http.server 8080   # ou qualquer servidor estático
# abrir http://localhost:8080
```

Para o app iOS (fase 8):

```bash
npm i -D @capacitor/cli @capacitor/core @capacitor/ios
npx cap init "Meow Tactics" com.meowtactics.td --web-dir=.
npx cap add ios
npx cap open ios   # abre no Xcode
```
