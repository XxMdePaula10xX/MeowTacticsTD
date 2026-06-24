# Meow Tactics TD — Checklist para publicar na App Store (iOS)

Status: 🟢 feito · 🟡 em andamento / parcial · 🔴 a fazer · ⚪ depende de você (conta/serviços)

---

## A) Código e Unity (o que dá pra resolver no projeto)

- 🟢 **Safe Area** — HUD (barras, loja, painéis, botões) agora fica dentro da área
  segura via `SafeArea.cs` + container `SafeArea` no `UIManager.BuildUI`. Notch /
  Dynamic Island / indicador de home não cobrem mais botões.
- 🟢 **Sem botão "Sair"** no iOS — a Apple não permite o app se encerrar sozinho
  (o "Sair" só aparece em PC/editor via `#if UNITY_STANDALONE`).
- 🟡 **Overlays de tela cheia** (menu, pausa, config, coleção, seleção de mapa) —
  ficam full-bleed (cobrem o notch, fica bonito) e o conteúdo é centralizado. Em
  landscape isso é seguro; **conferir no aparelho** se os botões de baixo (ex.:
  "Voltar") não encostam no indicador de home. Ajuste fácil depois do teste.
- 🔴 **Orientação travada em Landscape** — definir em *Player Settings → Resolution
  and Presentation → Default Orientation = Landscape Left/Right* (o jogo é
  horizontal). Desmarcar Portrait.
- 🟢 **Aspecto da câmera/mapa** — `CameraFit.cs` mantém o MAPA INTEIRO visível em
  qualquer proporção (iPhone largo, iPad 4:3) sem cortar o caminho. As faixas que
  sobram usam a cor de fundo do tema (noturno). Melhoria futura opcional: arte de
  fundo maior para preencher as faixas.
- 🟢 **Toques (touch)** — tap/colocar gato/abrir detalhe funcionam. Tooltips de
  item e de sinergia agora aparecem por TOQUE e somem sozinhos; a seleção de item
  já mostra o efeito no aviso (sem depender de hover).
- 🔴 **IL2CPP + ARM64** — *Player Settings → Other → Scripting Backend = IL2CPP*,
  *Architecture = ARM64* (obrigatório pela Apple).
- 🔴 **iOS mínimo** — definir Target minimum iOS (sugestão: iOS 13 ou 14+).
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
- 🟡 Garantir que nada de "placeholder/em construção" fique visível (ex.: mapas
  "Em breve" — tudo bem, mas deixar claro que é conteúdo futuro).

---

## Próximos passos sugeridos (ordem)
1. ✅ Safe Area (feito) → testar no **Device Simulator** do Unity.
2. Travar orientação Landscape + IL2CPP/ARM64 + Metal + targetFrameRate.
3. Resolver o aspecto câmera/mapa (faixas laterais).
4. Tooltips de item por toque.
5. Ícone + launch screen + screenshots.
6. Conta Apple Developer → Bundle ID → App Store Connect → TestFlight.
