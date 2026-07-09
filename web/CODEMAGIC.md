# Build iOS pelo Codemagic (sem Mac)

O `codemagic.yaml` (na raiz do repositório) builda o app iOS na nuvem, assina com
sua **API key do App Store Connect** e envia para o **TestFlight**.

- **Team ID:** 249VTHN4GQ
- **Bundle ID:** `com.meowtactics.td`
- **API key (Codemagic):** integração `quantum_asc` (Key `4UQMCZZJ95`)

## Passo a passo (uma vez só)

### 1. App Store Connect — criar o app
[App Store Connect](https://appstoreconnect.apple.com) → **Apps** → **+** → **New App**:
- Plataforma: iOS
- Nome: **Meow Tactics**
- Idioma principal: Português (Brasil)
- Bundle ID: **com.meowtactics.td** (se não aparecer na lista, registre em
  [Identifiers](https://developer.apple.com/account/resources/identifiers/list) primeiro)
- SKU: `meowtactics` (qualquer texto único)

### 2. Conferir o papel da API key
Em App Store Connect → **Users and Access → Integrations → App Store Connect API**,
a chave `4UQMCZZJ95` deve ter papel **App Manager** (ou Admin) para subir builds.

### 3. Codemagic — conectar e buildar
- No [Codemagic](https://codemagic.io), conecte este repositório do GitHub.
- A integração App Store Connect já se chama **quantum_asc** (é o que o yaml usa).
- Selecione o workflow **"Meow Tactics — iOS (TestFlight)"** e a branch
  `claude/game-concept-prd-kgajvy` (ou faça merge para a main e use a main).
- Clique em **Start new build**.

O build vai: instalar o Capacitor → montar a pasta `www` → gerar o projeto iOS →
criar ícone/splash → travar em paisagem → assinar com a API key → compilar o `.ipa`
→ enviar ao TestFlight.

### 4. Testar
Quando o build terminar, o app aparece no **TestFlight** (App Store Connect →
seu app → TestFlight). Instale o **TestFlight** no iPhone, aceite o convite e jogue. 🎮

## O que o `codemagic.yaml` faz
| Etapa | Ação |
|-------|------|
| Instalar deps | `npm install` no `web/` |
| Montar `www` | copia `index.html/src/styles/assets` (sem `node_modules`) |
| Capacitor iOS | `cap add ios` + `cap sync ios` |
| Ícone/splash | `@capacitor/assets` a partir de `web/resources/` |
| Landscape | `plutil` fixa orientação paisagem no Info.plist |
| Assinatura | `xcode-project use-profiles` (perfis vindos da API key) |
| Build | `xcode-project build-ipa` |
| Publicar | envia ao TestFlight (`submit_to_testflight: true`) |

## Depois (para a loja de verdade)
- Preencher a ficha (descrição, screenshots 6.7"/6.5", categoria Jogos/Estratégia,
  classificação etária, política de privacidade — o jogo **não coleta dados**).
- Marcar "Não coleta dados" no questionário de privacidade.
- Enviar para revisão.

> Trocar o Bundle ID? Edite `web/capacitor.config.json` (`appId`) **e** o
> `bundle_identifier` no `codemagic.yaml`, e crie o app com esse id no App Store Connect.
