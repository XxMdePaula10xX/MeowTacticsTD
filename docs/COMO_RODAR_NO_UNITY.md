# 🎮 Como rodar o jogo no Unity — guia clique a clique

Guia para quem **nunca usou Unity**. Siga na ordem.

---

## Parte 0 — Entendendo a tela do Unity

Quando o projeto abre, a janela do Unity tem várias áreas:

```
┌─────────────────────────────────────────────────────────────┐
│  MENU DO TOPO:  File  Edit  Assets  GameObject ... MeowTactics │  ← (1)
├─────────────────────────────────────────────────────────────┤
│            [ ▶ ]   ← botão PLAY fica aqui no meio do topo      │  ← (2)
├──────────────┬──────────────────────────────┬────────────────┤
│  Hierarchy   │      Scene / Game             │   Inspector    │
│  (objetos    │      (a área do jogo)         │  (detalhes do  │
│   da cena)   │                               │   selecionado) │
├──────────────┴──────────────────────────────┴────────────────┤
│  Project (suas pastas: Assets, Scripts...)   │   Console      │  ← (3)(4)
└─────────────────────────────────────────────────────────────┘
```

- **(1) Menu do topo:** no **Windows**, fica dentro da janela do Unity.
  No **Mac**, fica no topo da tela (a barra do sistema).
- **(2) Botão Play:** o triângulo ▶ no centro do topo. É ele que roda o jogo.
- **(3) Project:** embaixo, mostra as pastas (Assets/Scripts, etc.).
- **(4) Console:** onde aparecem erros (texto vermelho) e mensagens.

---

## Parte 1 — Conferir se está tudo OK (importante!)

1. **Espere o Unity terminar de carregar.** No canto inferior direito pode aparecer
   uma bolinha girando / barra de progresso. Espere ela sumir (pode levar minutos
   na primeira vez).

2. **Abra o Console** para ver se há erros:
   - Menu do topo: **Window → General → Console**.
   - Uma aba "Console" abre embaixo.
   - Clique no botão **Clear** (limpar) no canto da Console.

3. **Tem texto VERMELHO na Console?**
   - ❌ **Sim** → os scripts não compilaram. **Copie o texto vermelho e me mande.**
     (Não dá pra continuar enquanto houver erro vermelho.)
   - ✅ **Não** (só pode ter amarelo/branco, tudo bem) → siga para a Parte 2.

> 💡 Se o Unity abriu perguntando **"Enter Safe Mode?"**, clique em **Ignore** (ou
> "Quit/Ignore") e abra a Console: significa que havia erro. Me mande o texto.

---

## Parte 2 — Gerar o jogo (a parte mágica 🪄)

1. No **menu do topo**, procure um menu escrito **MeowTactics**
   (fica depois de "Component", ou perto de "Window").

   - **Não achou o menu "MeowTactics"?** Significa que os scripts ainda estão
     compilando (espere) ou há erro (veja a Parte 1). Ele só aparece quando tudo compila.

2. Clique em **MeowTactics → Fazer Tudo (gerar conteúdo + montar cena)**.

3. Espere alguns segundos. Vai aparecer uma janelinha dizendo **"Tudo pronto!"**.
   Clique em **Eba!**.

   - Isso criou os gatos/inimigos/ondas/itens e montou a cena do jogo sozinho.

---

## Parte 3 — Abrir a cena e jogar

1. Na janela **Project** (embaixo), navegue até a pasta **Assets → Scenes**.
2. Dê **dois cliques** no arquivo **Game** (ícone do Unity).
   - A área do meio deve mudar: você verá um "caminho" (linha) e quadradinhos (os slots).
3. Aperte o botão **▶ Play** (triângulo no topo do centro).
4. A aba muda para **Game** e o jogo começa! Você verá a interface (moedas, vidas,
   loja embaixo, etc.).
5. Para **parar**, aperte o ▶ de novo (ele fica azul enquanto roda).

---

## Parte 4 — Como jogar (controles)

- **Comprar gato:** clique num gato na **loja** (rodapé). Ele vai pro **BANCO**.
- **Posicionar:** clique num gato do **BANCO** (fica selecionado), depois clique num
  **quadradinho (slot)** no mapa.
- **Iniciar:** clique em **INICIAR ONDA** (botão verde). Os gatos atacam sozinhos.
- **Itens:** a cada 3 ondas aparece a escolha de itens. Escolha 1; depois clique no
  item (painel **ITENS**, à esquerda) e clique num **gato no mapa** pra equipar.
- **Detalhes/vender:** clique num gato já posicionado.

---

## Problemas comuns

| O que acontece | O que fazer |
|---|---|
| Não aparece o menu "MeowTactics" | Espere compilar; se persistir, veja a Console (Parte 1) e me mande o erro |
| "Enter Safe Mode?" ao abrir | Clique Ignore, abra a Console, me mande o texto vermelho |
| Apertei Play e a tela está preta/vazia | Confirme que abriu a cena **Game** (Parte 3) antes de dar Play |
| A interface aparece gigante ou minúscula | Normal no editor; clique na aba **Game** e ajuste a resolução no topo dela |
| Cliques nos gatos não funcionam | Me avise — pode ser ajuste de clique/câmera |

> Qualquer coisa que travar, tira um **print da tela inteira** e me manda aqui. 📸
