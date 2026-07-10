# App Store Connect — tudo pra preencher (copiar/colar)

App: **Meow Tactics TD** · Bundle ID: `com.meowtactics.td` · Team `249VTHN4GQ`

---

## 1. App Information (Informações do app)
| Campo | Valor |
|-------|-------|
| **Nome** (30 car.) | `Meow Tactics TD` |
| **Legenda/Subtitle** (30 car.) | `Defesa com gatos & sinergias` |
| **Idioma principal** | Português (Brasil) |
| **Bundle ID** | com.meowtactics.td |
| **SKU** | `meowtactics` |
| **Categoria primária** | Jogos → **Estratégia** |
| **Categoria secundária** | Jogos → **Casual** |
| **Direitos de conteúdo** | Não contém conteúdo de terceiros |

---

## 2. Classificação etária (questionário)
Responda o questionário assim (resultado ≈ **9+**):
- **Violência de desenho/fantasia:** *Infrequente/Leve* (gatos disparam projéteis em fantasminhas — combate fofo de fantasia)
- **Todo o resto** (violência realista, terror, linguagem, sexo, álcool, apostas, etc.): **Nenhum**
- Sem conteúdo gerado por usuários · Sem apostas · Não restrito à web

> Se preferir **4+**, marque a violência de fantasia como "Nenhum" (é discutível, pois são desenhos). Recomendo 9+ pra evitar rejeição.

---

## 3. Preços e disponibilidade
- **Preço:** Gratuito (Free) — *(ou escolha uma faixa se for pago)*
- **Disponibilidade:** Todos os países e regiões *(ou só Brasil no começo)*

---

## 4. Privacidade do app (App Privacy)
- **Coleta de dados:** responda **“Não, não coletamos dados deste app.”**
  (o jogo salva tudo localmente no aparelho, sem login/analytics/rede)
- **URL da Política de Privacidade** (obrigatório): use a página `privacy.html` (ver seção 7).

---

## 5. Página da versão 1.0
**Texto promocional** (170 car., editável a qualquer hora):
```
Defenda o reino dos pesadelos com torres-gato! 10 mapas, 10 gatos, 13 sinergias táticas, chefes épicos e 3 modos. Monte sua defesa e sobreviva a 50 ondas!
```

**Palavras-chave** (100 car., separadas por vírgula, SEM espaços):
```
tower defense,gatos,td,estratégia,sinergia,torre,defesa,tático,roguelite,gato,ondas,reino,chefe
```

**Descrição** (até 4000 car.):
```
🐱 MEOW TACTICS TD — Defesa de torre com gatos e sinergias táticas!

Os pesadelos invadiram o reino e só os gatos podem defendê-lo! Posicione torres-gato, combine sinergias no estilo auto-battler e sobreviva a 50 ondas de inimigos cada vez mais fortes.

⚔️ TRIÂNGULO DE DANO
Cada inimigo tem fraquezas: Físico (▲), Mágico (★) ou Verdadeiro (◆). Escolha os gatos certos e use penetração para furar armaduras e resistências mágicas.

🐾 10 GATOS ÚNICOS
Ninja, Arqueiro, Mago, Sniper, Samurai, Pirata, Feiticeira, Lobo, Xamã e Monge — cada um com dano, alcance e habilidades próprias (dano em área, lentidão, dano verdadeiro e mais).

✨ 13 SINERGIAS
Combine tipos e classes — Místico, Assassino, Guardião, Elemental, Caçador... — para ativar bônus poderosos. Monte a composição perfeita!

🗺️ 10 MAPAS COM ARTE ÚNICA
Jardim místico, vulcão de lava, caverna de cristal, geleira eterna, pântano sombrio, ilhas flutuantes e mais — cada um com caminhos e dificuldade diferentes.

🎁 ITENS & RELÍQUIAS (roguelite)
Ganhe itens para equipar nos gatos e relíquias que transformam a sua partida a cada escolha.

🎮 3 MODOS DE JOGO
• Normal — 50 ondas com chefes épicos
• Infinito — até onde você aguenta?
• Desafio Diário — um modificador novo todo dia

👑 E MAIS
Chefes épicos, arraste-e-solte para posicionar, prioridade de alvo por gato, recordes e conquistas.

Sem anúncios. Sem login. Jogue offline, quando e onde quiser. Defenda o reino! 🏰
```

**Copyright:** `© 2026 Matheus De Paula`
**URL de suporte** (obrigatório): a página `support.html` (ver seção 7)
**URL de marketing** (opcional): pode deixar vazio
**Versão:** `1.0`

**Screenshots:** você gera (paisagem: 2778×1284 ou 2688×1242). Sugestão de ordem:
1. Menu (key art) · 2. Jardim com sinergias · 3. Vulcão em combate · 4. Chefe · 5. Seleção de mapa · 6. Cristal.

---

## 6. App Review Information (para o revisor)
- **Precisa de login?** Não
- **Conta demo:** não se aplica
- **Contato:** seu nome, telefone e e-mail
- **Notas para o revisor:**
```
Jogo single-player, 100% offline. Não requer login nem conexão. Todos os recursos ficam disponíveis imediatamente ao abrir. Orientação: paisagem.
```
- **Liberação da versão:** "Liberar automaticamente após aprovação" (ou manual, se preferir).

---

## 7. URLs obrigatórias (privacidade e suporte)
Criei duas páginas prontas em `docs/`:
- `docs/privacy.html` — Política de Privacidade
- `docs/support.html` — Suporte

**Para virarem URLs públicas** (grátis), ative o GitHub Pages do repositório:
GitHub → Settings → **Pages** → Source: **Deploy from a branch** → Branch: a sua branch (ou `main`) e pasta `/docs` → Save.
Depois de alguns minutos as URLs ficam:
- Privacidade: `https://xxmdepaula10xx.github.io/MeowTacticsTD/privacy.html`
- Suporte: `https://xxmdepaula10xx.github.io/MeowTacticsTD/support.html`

> Se preferir, dá pra hospedar esses HTML em qualquer lugar (Netlify, Notion, etc.). Só precisa de um link público.

---

## Checklist final antes de "Enviar para revisão"
- [ ] Build apareceu no TestFlight (Codemagic) e foi selecionado na versão 1.0
- [ ] Nome, legenda, categorias
- [ ] Classificação etária respondida
- [ ] Preço/disponibilidade
- [ ] Privacidade: "não coleta dados" + URL de política
- [ ] Descrição, palavras-chave, texto promocional, copyright
- [ ] URL de suporte
- [ ] Screenshots (6.7" e/ou 6.5")
- [ ] Notas para o revisor
- [ ] **Enviar para revisão** 🚀
