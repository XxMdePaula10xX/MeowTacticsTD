# Decisões de Design — Meow Tactics TD

Registro das mudanças feitas em cima do PRD original, com o porquê de cada uma.

## 1. Regra de sinergia: cada gato posicionado conta (mesmo repetido)
**Problema no PRD:** a regra "gatos repetidos contam só uma vez" + elenco de 6 gatos
fazia com que **Ninja e Sniper nunca ativassem** (só existe 1 gato de cada tag).
**Decisão:** cada gato **posicionado** conta para a sinergia, incluindo cópias.
Assim, colocar 3 Ninjas ativa a sinergia Ninja. (Implementado no `SynergyManager`.)

## 2. Sistema de estrelas REMOVIDO do MVP
**Decisão:** tiramos o "junta 3 iguais e evolui". Motivo: é o sistema mais difícil de
balancear e adicionava complexidade desnecessária ao MVP. No lugar, comprar gatos iguais
serve para **encher slots e ativar sinergias**. Pode ser readicionado no futuro.

## 3. Sistema de Itens ADICIONADO (fonte de "força" da partida)
Substitui o papel que as estrelas teriam. **A cada 3 ondas** (3, 6, 9) aparece uma
escolha de **3 itens**; o jogador pega 1, guarda no inventário e equipa em um gato
posicionado (até **3 itens por gato**).

Três famílias de itens:
- **Status:** +dano, +vel. ataque, +alcance, +crítico, +pen. armadura, +pen. mágica.
- **Especiais:** dano verdadeiro por ataque, lentidão nos ataques, dano em área.
- **Distintivos:** dão uma **sinergia extra** ao gato (ex: um Ninja passa a contar também
  como Sniper). Mecânica inspirada nos "emblemas" do TFT.

Regras de balanceamento:
- Itens voltam ao inventário se o gato for vendido (não se perde nada).
- Só ~3 itens por partida, então cada escolha pesa.

Parâmetros em `GameBalance.cs`: `ItemDropEveryNWaves`, `ItemDraftChoices`, `MaxItemsPerCat`.

## Itens fora do MVP (mantidos para o futuro)
Multiplayer, login, ranking, monetização, skins, campanha, modo infinito, roguelike de
relíquias, mais mapas/gatos/sinergias, áudio completo e build final de iOS.
