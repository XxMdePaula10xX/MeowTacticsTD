# Publicar na App Store (Capacitor + Xcode)

Guia para transformar o jogo web em um app iOS nativo e enviar para a App Store.
Requer um **Mac com Xcode** e uma **conta Apple Developer** (US$ 99/ano).

## 1. Empacotar com Capacitor (no Mac)

```bash
cd web
npm install                 # instala o Capacitor (ver package.json)
npx cap init "Meow Tactics" com.meowtactics.td --web-dir=.
npx cap add ios             # cria a pasta ios/ com o projeto Xcode
npx cap sync ios            # copia o jogo (web) para dentro do app
npx cap open ios            # abre no Xcode
```

> O `capacitor.config.json` já está pronto (appId `com.meowtactics.td`,
> fundo `#0f1020`, `contentInset: always` para respeitar a safe area).

## 2. No Xcode

- **Signing & Capabilities:** selecione seu Team (Apple Developer). O bundle id
  deve ser `com.meowtactics.td` (ou o seu próprio).
- **Deployment target:** iOS 14+ (o jogo usa recursos web amplamente suportados).
- **Orientação:** deixe apenas Landscape (o jogo é paisagem), em
  *General → Deployment Info → Device Orientation*.
- **App Icon:** arraste `assets/ui/app_icon.png` (1024×1024) para o
  `AppIcon` no asset catalog (`Assets.xcassets`). Gere os tamanhos com o Xcode
  ou uma ferramenta de app icon.
- **Launch Screen:** use um fundo `#0f1020` com o ícone ao centro (o
  `menu_bg.png` também serve como splash).

## 3. Haptics nativos (opcional, melhora o toque no iPhone)

O jogo já toca sons via WebAudio. Para vibração nativa, o pacote
`@capacitor/haptics` está no `package.json`. Depois do `npm install`, dá para
chamar no código (ex.: ao perder vida / derrota):

```js
import { Haptics, ImpactStyle } from '@capacitor/haptics';
Haptics.impact({ style: ImpactStyle.Medium });
```

## 4. Testar e enviar

1. **Rodar no device:** conecte o iPhone, escolha-o no Xcode e clique ▶︎.
2. **TestFlight:** *Product → Archive* → *Distribute App → App Store Connect*.
   Convide testadores no [App Store Connect](https://appstoreconnect.apple.com).
3. **Submissão:** preencha ficha da loja (nome, descrição, screenshots 6.7"/6.5"/5.5",
   categoria = Jogos/Estratégia, classificação etária, política de privacidade).
4. **Privacidade:** o jogo **não coleta dados** (tudo em `localStorage`).
   Marque "Não coleta dados" no questionário de privacidade.

## 5. Checklist rápido

- [ ] `npm install` + `npx cap add ios` OK
- [ ] Team de signing selecionado
- [ ] App icon 1024×1024 no asset catalog
- [ ] Launch screen com fundo `#0f1020`
- [ ] Orientação travada em Landscape
- [ ] Testado em device real
- [ ] Build enviado ao TestFlight
- [ ] Ficha da App Store preenchida + screenshots
- [ ] Enviado para revisão
