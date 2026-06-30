# Meow Tactics TD — Checklist para publicar na App Store (iOS)

Status: 🟢 feito · 🟡 em andamento / parcial · 🔴 a fazer · ⚪ depende de você (conta/serviços)

---

## A) Código e Unity (o que dá pra resolver no projeto)

- 🟢 **Safe Area** — HUD (barras, loja, painéis, botões) agora fica dentro da área
  segura via `SafeArea.cs` + container `SafeArea` no `UIManager.BuildUI`. Notch /
  Dynamic Island / indicador de home não cobrem mais botões.
- 🟢 **Sem botão "Sair"** no iOS — a Apple não permite o app se encerrar sozinho
  (o "Sair" só aparece em PC/editor via `#if UNITY_STANDALONE`).
- 🟢 **Overlays de tela cheia** (menu/pausa/config/coleção/mapa/conquistas) —
  botões "Voltar" subidos para fora da zona do indicador de home (y=110). Em
  landscape o notch fica na lateral e o conteúdo é centralizado. **Confirmar no
  aparelho** (TestFlight) que tudo fica tocável.
- 🟢 **Orientação travada em Landscape** — feito no `ProjectSettings.asset`
  (autorrotação de portrait desativada; só LandscapeLeft/Right). Conferir em
  *Player Settings → Resolution and Presentation* se quiser.
- 🟢 **Aspecto da câmera/mapa** — `CameraFit.cs` mantém o MAPA INTEIRO visível em
  qualquer proporção (iPhone largo, iPad 4:3) sem cortar o caminho. As faixas que
  sobram usam a cor de fundo do tema (noturno). Melhoria futura opcional: arte de
  fundo maior para preencher as faixas.
- 🟢 **Toques (touch)** — tap/colocar gato/abrir detalhe funcionam. Tooltips de
  item e de sinergia agora aparecem por TOQUE e somem sozinhos; a seleção de item
  já mostra o efeito no aviso (sem depender de hover).
- 🟡 **IL2CPP + ARM64** — o iOS **já usa IL2CPP/ARM64 por padrão** na Unity 6
  (Mono não é suportado no device), então deve estar ok; confirmar em
  *Player Settings → Other → Scripting Backend = IL2CPP, Architecture = ARM64*.
- 🟢 **iOS mínimo** — já definido como **15.0** no projeto.
- 🔴 **Graphics API = Metal** (padrão no iOS; confirmar que OpenGLES não está forçado).
- 🟡 **Performance** — jogo leve; gerar texturas procedurais 1x no início (ok).
  Travar em 60 FPS (`Application.targetFrameRate = 60`) e testar em device antigo.
- 🟢 **Notificações locais + badge** — `NotificationManager.cs` agenda lembretes
  ("volte a jogar") em 1/3/7 dias quando o app vai pro background, e o badge do
  ícone SOME ao abrir o app. **Requer instalar o pacote** *Mobile Notifications*
  (Window > Package Manager > Unity Registry > **Mobile Notifications**). O código
  iOS só compila no build do device, então o pacote só é preciso na hora do build.
  Pedir permissão de notificação conta como boa prática (a Apple aceita).

## B) Assets obrigatórios

- 🟡 **Ícone do app** — salve a arte 1024×1024 (quadrada, SEM transparência, SEM
  cantos arredondados) em `Assets/Art/UI/app_icon.png`. O menu *MeowTactics >
  Definir Ícone do App* (e o "Fazer Tudo") aplica automaticamente como ícone
  padrão e do iOS. Falta só conferir no build.
- 🔴 **Launch Screen** (tela de abertura) — pode ser uma cor sólida + logo, ou a
  arte do menu. Configura em *Player Settings → Splash/Launch*.
- 🔴 **Screenshots** para a loja, nos tamanhos exigidos: 6.7" (iPhone 15/16 Pro Max)
  e 6.5" (iPhone 11 Pro Max/XS Max), em **landscape**. (iPad opcional se for
  universal.)
- 🔴 **Splash da Unity** — no plano gratuito aparece o logo da Unity (ok); com
  Unity Pro/Plus dá pra remover.

## C) App Store Connect / Legal ⚪ (depende da sua conta)

- ⚪ **Apple Developer Program** — US$ 99/ano (necessário para publicar).
- ⚪ **Bundle ID** único (ex.: `com.seunome.meowtacticstd`) registrado no portal.
- ⚪ **App Store Connect** — criar o app: nome, subtítulo, descrição, palavras-chave,
  categoria (Games → Strategy), classificação etária (questionário).
- ⚪ **Política de Privacidade (URL)** — obrigatória para TODO app, mesmo sem coletar
  dados. Como só usamos `PlayerPrefs` local (sem servidores/login/anúncios), a
  *nutrition label* pode ser **"Data Not Collected"**.
- ⚪ **Export Compliance** — sem criptografia própria → marcar
  `ITSAppUsesNonExemptEncryption = false` no Info.plist (evita perguntas a cada build).
- ⚪ **Sem IDFA / sem tracking** (não usamos anúncios/analytics) → sem App Tracking
  Transparency. Manter assim simplifica MUITO a aprovação.

## D) Build e assinatura ⚪

- ⚪ Build do Unity → projeto Xcode → assinar com seu Team (certificado + provisioning).
- ⚪ **TestFlight** para testar no seu iPhone antes de enviar para revisão.
- ⚪ Version (ex.: 1.0.0) e Build number incremental a cada envio.

## E) Conteúdo / diretrizes a conferir

- 🟢 Sem compras dentro do app, sem login, sem anúncios (menos burocracia).
- 🟡 Áudio é todo procedural (sem direitos de terceiros) — ok. Conferir se respeita
  o botão de silencioso (opcional).
- 🟢 **Múltiplos mapas** — 3 mapas jogáveis (Bosque, Jardim, Ruínas), cada um com
  traçado de caminho próprio. `RuntimeMapBuilder` troca fundo/caminhos/marcadores
  em runtime conforme o mapa escolhido. Jardim e Ruínas ficam num campo escuro até
  receberem arte própria (`Assets/Art/Maps/mapa_jardim.png` e `mapa_ruinas.png`,
  1672×941, 16:9) — opcional para o MVP, recomendado para o visual final.

---

## Revisão geral do MVP (3 frentes: corretude, iOS, UX) — correções aplicadas
- 🟢 **Orientação travada em landscape** (era o P0: o app girava pra portrait).
- 🟢 **Continuar carregava no mapa errado** → agora restaura no mapa salvo.
- 🟢 **Itens do inventário (não equipados) eram perdidos no save** → agora persistem.
- 🟢 **Mapa "assado" na cena desalinhado do default** (bosque vs jardim) → alinhado
  (cena vem montada com o Jardim; sem troca forçada no boot).
- 🟢 **Painel de detalhe mostrava dano-base ignorando buffs** → mostra o dano efetivo.
- 🟢 **Continuar contava como "nova partida"** (inflava conquista) → corrigido.
- 🟢 **Iniciar onda sem nenhum gato** dava perda de vidas sem aviso → agora avisa.
- 🟢 **Tutorial não ensinava o triângulo de dano** → novo passo (Físico/Mágico/Verdadeiro).
- 🟢 **Tooltip podia vazar da tela/notch** → preso à área segura.
- 🟢 **"Resetar Progresso" apagava tudo num toque** → confirmação em 2 toques.
- 🟢 **Botões "Voltar" perto do indicador de home** → subidos.
- 🟢 Defensivos: ranking de dano não infla com dano verdadeiro em inimigo morto;
  guarda de null na Coleção; HUD reflete o estado no load.

### Decisões de BALANCEAMENTO para você testar (não mexi sem seu ok)
- O **mini-boss (Rei) na onda 8** é um pico forte cedo. Se achar punitivo, dá pra
  suavizar (reduzir o `scaling` dele) — me avise.
- **Segunda metade (ondas 30–50)**: a vida cresce linear (+18%/onda) e itens só a
  cada 3 ondas (~16 itens em 50 ondas). Pode ficar "faminto de item". Posso ajustar
  a curva ou a frequência de itens quando você testar até lá.

---

## Próximos passos sugeridos (ordem)
1. Confirmar no Unity: **IL2CPP/ARM64**, **Metal** (Auto), e o **ícone** aplicado.
2. **Launch Screen** + **screenshots** (landscape, 6.7" e 6.5").
3. Instalar pacote **Mobile Notifications** antes do build iOS.
4. Conta **Apple Developer** → **Bundle ID** → **App Store Connect** → **TestFlight**.
5. **Política de Privacidade (URL)** + App Privacy = "Data Not Collected" + Export
   Compliance (`ITSAppUsesNonExemptEncryption=false`).
6. Teste em device (TestFlight): safe area, botões, e o balanceamento das ondas.
