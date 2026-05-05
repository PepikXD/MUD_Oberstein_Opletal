# MUD Oberstein & Opletal

Textová multiplayerová hra (Multi-User Dungeon) postavená na architektuře klient–server v jazyce C# (.NET).

## Požadavky

- .NET 8.0 SDK nebo novější
- Python 3.10+ (volitelné – pro přípravu testovacího prostředí)

## Struktura projektu

```
MUD_Oberstein_Opletal/
├── src/
│   ├── Program.cs                 # Vstupní bod serveru
│   ├── Server.cs                  # TCP Listener, správa klientů
│   ├── AccountManager.cs          # Registrace, přihlášení, persistence hráčů
│   ├── Player.cs                  # Herní entita hráče (inventář, questy, currency)
│   ├── World.cs                   # Načítání a správa herního světa z JSON
│   ├── Room.cs                    # Místnost (exits, items, NPCs, broadcast)
│   ├── NPC.cs                     # Definice NPC postav
│   ├── Item.cs                    # Definice herních předmětů
│   ├── Dialog.cs                  # Dialogový systém (stavový automat)
│   ├── Quest.cs                   # Enum QuestState
│   ├── Logger.cs                  # Asynchronní logování do souboru
│   ├── Resources.cs               # Systémové hlášky a texty
│   ├── Commands/                  # Implementace herních příkazů
│   │   ├── ICommand.cs
│   │   ├── CommandHandler.cs
│   │   ├── GoCommand.cs           # Pohyb mezi místnostmi
│   │   ├── LookCommand.cs         # Prozkoumání místnosti
│   │   ├── TakeCommand.cs         # Sebrání předmětu
│   │   ├── DropCommand.cs         # Odložení předmětu
│   │   ├── InventoryCommand.cs    # Zobrazení inventáře
│   │   ├── TalkCommand.cs         # Rozhovor s NPC
│   │   ├── UseCommand.cs          # Použití předmětu (M8)
│   │   ├── SayCommand.cs          # Lokální chat (M1)
│   │   ├── ShoutCommand.cs        # Globální chat (M1)
│   │   ├── BuyCommand.cs          # Nákup od obchodníka (M4)
│   │   ├── SellCommand.cs         # Prodej obchodníkovi (M4)
│   │   └── HelpCommand.cs         # Nápověda
│   ├── Data/
│   │   └── world.json             # Herní svět (místnosti, předměty, NPC, dialogy)
│   ├── Accounts/                  # Uložené profily hráčů (JSON)
│   ├── Logs/
│   │   └── server.log             # Serverové logy
│   ├── MUD_Client/                # Klientská aplikace
│   │   ├── Program.cs
│   │   └── appsettings.json       # Konfigurace klienta (IP, port)
│   └── appsettings.json           # Konfigurace serveru (port, cesty)
└── prepare_tests.py           # Skript pro přípravu testovacího prostředí
```

## Spuštění

### Server

```bash
cd src
dotnet run
```

Server se spustí na portu definovaném v `appsettings.json` (výchozí `8080`).

### Klient

```bash
cd src/MUD_Client
dotnet run
```

Klient se připojí na IP a port z `src/MUD_Client/appsettings.json` (výchozí `127.0.0.1:8080`).

## Konfigurace

### Server (`appsettings.json`)

```json
{
  "Server": {
    "Port": 8080,
    "MaxPlayers": 100
  },
  "Paths": {
    "Accounts": "Accounts",
    "WorldData": "Data/world.json",
    "Logs": "Logs/server.log"
  }
}
```

### Klient (`MUD_Client/appsettings.json`)

```json
{
  "ServerIp": "127.0.0.1",
  "ServerPort": 8080
}
```

## Herní příkazy

| Příkaz | Popis |
|---|---|
| `go <směr>` / `jdi` | Pohyb mezi místnostmi (north, south, east, west) |
| `look` / `prozkoumej` | Zobrazení popisu místnosti, východů, předmětů a hráčů |
| `take <předmět>` / `vezmi` | Sebrání předmětu ze země |
| `drop <předmět>` / `poloz` | Odložení předmětu z inventáře |
| `inventory` / `inventar` | Zobrazení obsahu batohu |
| `talk <npc>` / `mluv` | Zahájení rozhovoru s NPC |
| `use <předmět>` | Použití předmětu z inventáře |
| `say <zpráva>` / `rekni` | Odeslání zprávy hráčům ve stejné místnosti |
| `shout <zpráva>` / `krik` | Odeslání zprávy všem hráčům na serveru |
| `buy <předmět>` | Nákup od obchodníka v místnosti |
| `sell <předmět>` | Prodej obchodníkovi v místnosti |
| `help` / `pomoc` | Zobrazení nápovědy |

## Dokumentace datového formátu (I1)

Veškerá herní data se načítají z externích souborů. Server nesmí mít herní data napevno v kódu – změna herního světa nevyžaduje rekompilaci aplikace.

### 1. Herní svět (`Data/world.json`)

Struktura hry je kompletně definována v jednom kořenovém JSON objektu:

- **`StartingRoomId`** (string): Identifikátor místnosti, ve které se objeví nově registrovaný hráč.
- **`Rooms`** (array): Pole místností na mapě.
  - `Id` (string): Identifikátor místnosti (např. `"entrance"`).
  - `Name` (string): Zobrazovaný název.
  - `Description` (string): Viditelný text popisující prostor.
  - `Exits` (slovník): Mapa, kde klíčem je směr (`"north"`) a hodnotou ID cílové místnosti.
  - `Items` (array of strings): Seznam ID předmětů ležících v místnosti.
  - `NPCs` (array of strings): Seznam ID postav v místnosti.
- **`Items`** (array): Definice herních předmětů.
  - `Id` (string): Unikátní identifikátor (propojeno na `Rooms`, `NPCs` nebo batoh hráče).
  - `Name` (string): Zobrazovaný a použitelný název (např. `"key"`, `"book"`).
  - `Price` (int): Hodnota (v mincích) používaná pro příkazy `buy` a `sell`.
  - `Action` (string, volitelné): Speciální akce při `use` (např. `"win_game"`).
- **`NPCs`** (array): Definice postav.
  - `Id` (string): Unikátní identifikátor.
  - `Name` (string): Viditelné jméno (používáno u `talk`).
  - `Dialog` (string): Krátký text pro jednoduché interakce (pokud NPC nemá `DialogTree`).
  - `IsMerchant` (bool): True, pokud je postava obchodník.
  - `ItemsForSale` (array of strings): Pole ID předmětů, které má na prodej.
  - `StartingDialogNodeId` (string): ID prvního uzlu ve stromovém dialogu.
  - `DialogTree` (dictionary): Stavový automat pro interaktivní dialogy. Obsahuje `DialogNode` s vlastnostmi `Text` a seznamem voleb (`Options`). Volby definují podmínky (`RequiredQuest`, `RequiredQuestState`, `RequiredItem`) a vyvolávají akce (`GiveItem`, `TakeItem`, `SetQuestState`).

### 2. Přihlášení a persistence (`Accounts/*.json`)

Když se hráč odpojí, jeho data se serializují do JSON souboru:

- `Name` (string): Identifikátor hráče.
- `Password` (string): Heslo hráče uložené v plain textu.
- `LocationId` (string): Poslední navštívená místnost.
- `InventoryItems` (array of strings): Uložené předměty z batohu.
- `Currency` (int): Zůstatek mincí.
- `Quests` (dictionary): Seznam názvů aktivních questů a jejich stav (z enumu `QuestState`: `NotStarted`, `Active`, `Completed`, `Failed`).

### 3. Záznam výher (`Data/statistics.txt`)

Pokud hráč dosáhne ukončení hry (akce předmětu `"win_game"`), server appenduje záznam ve formátu `[čas] Player Jméno has won the game!` do souboru `Data/statistics.txt`. Tím se naplňuje bod **P1**.

### 4. Logování (`Logs/server.log`)

Server zaznamenává důležité události (I2):
- Připojení a odpojení hráče
- Zadané příkazy
- Chyby a výjimečné stavy

Každý záznam obsahuje časovou značku. Formát: `[YYYY-MM-DD HH:MM:SS] [LEVEL] zpráva`.

## Implementované herní mechaniky

| Mechanika | Popis |
|---|---|
| **M1 – Komunikace** | `say` (lokální) a `shout` (globální) chat, oznámení při vstupu/odchodu |
| **M4 – Obchodování** | NPC obchodníci, herní měna, `buy`/`sell` s cenami z `world.json` |
| **M5 – Dialogy** | Stromové dialogy s podmínkami (quest stav, předměty) a akcemi |
| **M8 – Použití předmětů** | Předměty s definovaným účinkem, pravidla z externích souborů |
| **M10 – Questy** | Získávání a plnění úkolů, odměny, persistence stavu |

## Příprava testovacího prostředí

Pro přípravu prostředí před testováním spusťte:

```bash
python prepare_tests.py
```

Skript automaticky:
1. Zkontroluje validitu `world.json` (reference, exity, NPC, itemy, dialogy).
2. Vytvoří/resetuje testovací účty přesně podle testovacích scénářů.
3. Promaže starý log a `statistics.txt`.
4. Vypíše přehled s přihlašovacími údaji pro testery.

### Testovací účty

| Typ | Jméno | Heslo | Stav |
|---|---|---|---|
| Existující hráč | `hrac_veteran` | `mojeheslo` | Prázdný inventář, 100 currency, na startu |
| Bohatý hráč | `bohaty_hrac` | `heslo` | Má `book`, 500 currency, na startu |
| Nový hráč | `novacek` | – | Účet NEEXISTUJE (pro test registrace) |

## Autoři

- Miloš Opletal & Štěpán Oberstein
