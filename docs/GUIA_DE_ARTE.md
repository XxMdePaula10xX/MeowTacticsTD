# 🎨 Guia de Arte — Meow Tactics TD

Objetivo: conseguir arte fofa e consistente (estilo Clash of Clans / mobile cartoon)
para os gatos, inimigos e UI — usando **IA** (mais fácil) ou **packs prontos**.

> A arte é a mesma para web ou Unity (são imagens PNG). Então isso vale para qualquer caminho.

---

## OPÇÃO A — Gerar com IA (recomendado para começar)

### Quais ferramentas usar (do mais fácil/grátis ao mais pro)
- **Bing Image Creator** (bing.com/create) — **grátis**, usa DALL·E 3, fácil. Ótimo pra começar.
- **Leonardo.ai** — tem plano grátis, focado em arte de jogos. Muito bom.
- **ChatGPT (Plus)** — se você já tiver, gera direto na conversa.
- **Midjourney** — melhor qualidade, mas é pago (~US$10/mês) e usa Discord.

### A REGRA DE OURO: consistência
Para tudo parecer do mesmo jogo, **use sempre a mesma "linha de estilo"** no fim de cada
prompt, e gere tudo na mesma sessão. Copie esta linha e cole no final de TODOS os prompts:

> **STYLE:** cute 2D mobile game character, Clash of Clans / Supercell style, soft cel
> shading, thick clean outlines, big expressive eyes, chunky friendly proportions, vibrant
> warm colors, front view, full body, centered, isolated on plain white background, game
> asset, high quality, no text, no watermark

### Especificações técnicas (importante para encaixar no jogo)
- Formato **PNG**. Se possível, **fundo transparente**; senão, **fundo branco liso** (eu removo depois).
- Personagem **centralizado**, corpo inteiro, mesmo ângulo e mesma luz (vinda de cima).
- Tamanho ~**512×512** ou **1024×1024**.
- Mesma escala relativa (gatos parecidos de tamanho; boss maior).

---

## 🐱 PROMPTS DOS GATOS (copie, cole, gere)

> Lembre de colar a linha **STYLE** no final de cada um.

1. **Gato Ninja**
   `a cute black cat ninja, wearing a red scarf and headband, holding a small shuriken, sneaky confident expression —`

2. **Gato Arqueiro**
   `a cute green forest cat archer, holding a small wooden bow, leaf-themed outfit, focused friendly expression —`

3. **Gato Sniper**
   `a cute blue-gray cat sniper, wearing cool glasses, calm patient pose, tiny scope, serious but adorable —`

4. **Gato Mago**
   `a cute purple cat wizard, pointy magic hat with a star, holding a glowing magic staff, sparkles around —`

5. **Gato Xamã**
   `a cute teal cat shaman, decorated with feathers and a glowing amulet, mystical aura, gentle wise expression —`

6. **Gato Samurai**
   `a cute red cat samurai, light shoulder armor, holding a small katana, honorable brave pose —`

---

## 👻 PROMPTS DOS INIMIGOS

> Os inimigos devem ser **sombrios mas fofos** (nada assustador demais). Cores roxas, azuis e cinzas.
> Cole a mesma linha **STYLE** no final.

1. **Fantasminha**
   `a cute tiny round ghost, soft lavender color, big innocent eyes, wavy bottom, friendly spooky —`

2. **Fantasma Blindado**
   `a cute round ghost wearing steel armor plates and a small helmet, sturdy tanky look, gray-blue —`

3. **Sombra Mística**
   `a cute purple shadow creature with glowing magic eyes, wispy smoky body, mysterious —`

4. **Pesadelo Veloz**
   `a cute small fast nightmare creature, cyan color, motion lines, big eyes, speedy energetic —`

5. **Rei dos Pesadelos (BOSS)**
   `a cute but imposing nightmare king ghost boss, dark purple, golden crown, glowing yellow eyes,
   big charismatic, larger than normal enemies —`

---

## 🪵 UI E CENÁRIO (gere depois dos personagens)

- **Painel de madeira:** `wooden game UI panel, rounded corners, golden trim, mobile game, plain background —`
- **Botão:** `cute green wooden game button, rounded, glossy, mobile game UI —`
- **Moeda:** `shiny gold coin icon with a cat paw print, mobile game, plain background —`
- **Coração de vida:** `cute red heart icon, glossy, mobile game UI, plain background —`
- **Fundo do mapa:** `cozy night grassy field with a stone path, top-down cartoon, purple night sky,
  stars and a moon, Clash of Clans style, no characters —`

---

## OPÇÃO B — Packs de arte prontos (sem gerar nada)

Procure por "tower defense", "cat", "cute monster", "ghost":
- **Kenney.nl** — assets grátis (CC0, pode usar de tudo). Ótimo ponto de partida.
- **itch.io/game-assets** — muitos packs fofos, grátis e baratos.
- **CraftPix.net** — packs de Tower Defense (alguns grátis).
- **Unity Asset Store** — busque "cute cat" / "tower defense" (mesmo no caminho web dá pra usar os PNGs).

Cuidado com a **licença**: prefira CC0 / "uso comercial permitido".

---

## ✅ O que fazer quando tiver as imagens

1. Salve cada uma com nome claro: `gato_ninja.png`, `inimigo_fantasminha.png`, etc.
2. Me mande aqui (ou coloque nas pastas `Assets/Art/Cats`, `Assets/Art/Enemies`...).
3. **Eu encaixo no jogo** (removo fundo se precisar, ajusto tamanho e ligo no código).

> Dica: comece gerando só **1 gato**. Me mande pra eu validar o estilo/tamanho antes de
> você gerar os outros 10 — assim não retrabalha.
