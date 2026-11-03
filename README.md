# Schedule I Mega Expansion

**Stav k 3. 11. 2026:** runtime baseline 0.3.1 + Harborline development/audit data.

Toto je herní mod pro Schedule I, nikoli webový Schedule-I Helper.

## Runtime baseline 0.3.1

Podle README v instalačním balíčku 0.3.1:
- 2 nativní dealeři: Mason Reed a Sierra Voss;
- 8 nativních zákazníků: Caleb Price, Nina Brooks, Rafael Cole, Olivia Hayes, Ethan Ward, Maya Bennett, Lucas Foster a Ava Collins;
- nativní supplier NPC Portside Supply;
- telefonní supplier katalog s 14 existujícími item ID a původním Delivery systémem;
- rozšíření katalogu vanilla Hardware Store o 14 existujících položek.

Jde o implementaci popsanou v balíčku; runtime chování nebylo v této relaci ověřeno v běžící hře. Katalog pracuje s existujícími item definicemi a změna BasePurchasePrice může působit globálně na stejné item ID.

## Daffy's Hills

Daffy's Hills 1.2 zůstane volitelným sousedním modem pro východní vyvýšenou oblast. Má vlastní 720m silnici, 32 domů a 43 zákazníků. Do našeho balíčku nepřebíráme jeho DLL, assety ani implementaci. Autor zakazuje úpravy a reuse assetů bez svolení.

Viz docs/DAFFYSHILLS-INTEGRATION.md.

## Zdrojový kód

Aktuální gameplay ZIP 0.3.1 obsahuje DLL, README, manifest a licenci, nikoli odpovídající C# zdrojový projekt. GitHub měl před tímto auditem pouze licenci a placeholder README. Starší Harborline v0.2 source je zachován jako historický greybox, nikoli jako zdroj 0.3.1.

Viz docs/SOURCE-RECOVERY.md.

## Struktura

- docs/IMPLEMENTATION-AUDIT.md – stav funkcí a testovací brány.
- docs/SOURCE-RECOVERY.md – co je a není obnovitelné ze současných souborů.
- docs/DAFFYSHILLS-INTEGRATION.md – hranice kompatibility s externím modem.
- world/harborline/ – ověřitelné kotvy a stav implementace.
- world/integrations/ – volitelné externí integrace.
- packs/Harborline/ – NPC roster a návrhy zákaznických profilů.

Data označená jako design/plán se ve hře neaktivují, dokud je runtime výslovně nenačte.

## Ověření dat

```bash
python -m unittest discover -s tests -v
python tools/validate_harborline_data.py
```

Tyto kontroly ověřují pouze datové kontrakty, nikoli S1API runtime, geometrii, navmesh, ukládání nebo multiplayer.
