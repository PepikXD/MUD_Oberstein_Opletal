#!/usr/bin/env python3
"""
prepare_tests.py  –  Přípravný skript pro testování MUD projektu.

Co dělá:
  1. Načte konfiguraci z appsettings.json (port, cesty).
  2. Zkontroluje, že Data/world.json existuje a je validní JSON.
  3. Ověří, že world.json obsahuje startovní místnost, NPC, itemy.
  4. Smaže stávající testovací účty a vytvoří nové přesně podle test casů.
  5. Ověří, že soubor server.log existuje (a pokud ne, vytvoří ho).
  6. Smaže Data/statistics.txt (aby P1 test začínal s čistým souborem).
  7. Vypíše přehledné shrnutí.

Spuštění:  python prepare_tests.py
"""

import json
import os
import sys
from datetime import datetime

# Fix Windows console encoding for Czech characters
sys.stdout.reconfigure(encoding='utf-8', errors='replace')

# ---------------------------------------------------------------------------
# Cesty
# ---------------------------------------------------------------------------
PROJECT_ROOT = os.path.dirname(os.path.abspath(__file__))

def resolve(path):
    """Resolve relative path against project root."""
    if os.path.isabs(path):
        return path
    return os.path.join(PROJECT_ROOT, path)

# ---------------------------------------------------------------------------
# 1.  Načtení appsettings.json
# ---------------------------------------------------------------------------
appsettings_path = os.path.join(PROJECT_ROOT, "appsettings.json")
if not os.path.exists(appsettings_path):
    print("[ERROR] appsettings.json nenalezen!")
    sys.exit(1)

with open(appsettings_path, "r", encoding="utf-8") as f:
    config = json.load(f)

port = config.get("Server", {}).get("Port", 5000)
accounts_dir = resolve(config.get("Paths", {}).get("Accounts", "Accounts"))
world_path = resolve(config.get("Paths", {}).get("WorldData", "Data/world.json"))
log_path = resolve(config.get("Paths", {}).get("Logs", "Logs/server.log"))

print("=" * 60)
print("  MUD Test Preparation Script")
print("=" * 60)
print(f"  Čas:          {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
print(f"  Projekt:      {PROJECT_ROOT}")
print(f"  Port:         {port}")
print(f"  Accounts:     {accounts_dir}")
print(f"  World:        {world_path}")
print(f"  Log:          {log_path}")
print("=" * 60)

errors = []

# ---------------------------------------------------------------------------
# 2.  Kontrola world.json
# ---------------------------------------------------------------------------
print("\n[1/5] Kontrola world.json ...")
if not os.path.exists(world_path):
    errors.append(f"world.json neexistuje: {world_path}")
    print(f"  [FAIL] Soubor neexistuje: {world_path}")
else:
    try:
        with open(world_path, "r", encoding="utf-8") as f:
            world = json.load(f)
        print("  [OK] JSON je validní.")

        # Kontrola základní struktury
        rooms = world.get("Rooms", [])
        items = world.get("Items", [])
        npcs = world.get("NPCs", [])
        starting_room = world.get("StartingRoomId", "")

        room_ids = [r["Id"] for r in rooms]
        item_ids = [i["Id"] for i in items]
        npc_ids = [n["Id"] for n in npcs]

        if starting_room not in room_ids:
            errors.append(f"StartingRoomId '{starting_room}' neexistuje v Rooms!")
            print(f"  [FAIL] StartingRoomId '{starting_room}' nenalezeno.")
        else:
            print(f"  [OK] StartingRoomId='{starting_room}' existuje.")

        print(f"  [INFO] Rooms: {len(rooms)} ({', '.join(room_ids)})")
        print(f"  [INFO] Items: {len(items)} ({', '.join(item_ids)})")
        print(f"  [INFO] NPCs:  {len(npcs)}  ({', '.join(npc_ids)})")

        # Kontrola exitů
        for room in rooms:
            for direction, target in room.get("Exits", {}).items():
                if target not in room_ids:
                    msg = f"Room '{room['Id']}' exit '{direction}' -> '{target}' neexistuje!"
                    errors.append(msg)
                    print(f"  [FAIL] {msg}")

        # Kontrola NPC odkazů v rooms
        for room in rooms:
            for npc_ref in room.get("NPCs", []):
                if npc_ref not in npc_ids:
                    msg = f"Room '{room['Id']}' odkazuje NPC '{npc_ref}' - neexistuje!"
                    errors.append(msg)
                    print(f"  [FAIL] {msg}")

        # Kontrola Item odkazů v rooms
        for room in rooms:
            for item_ref in room.get("Items", []):
                if item_ref not in item_ids:
                    msg = f"Room '{room['Id']}' odkazuje Item '{item_ref}' - neexistuje!"
                    errors.append(msg)
                    print(f"  [FAIL] {msg}")

        # Kontrola NPC merchant items
        for npc in npcs:
            for sale_item in npc.get("ItemsForSale", []):
                if sale_item not in item_ids:
                    msg = f"NPC '{npc['Id']}' prodává '{sale_item}' - neexistuje v Items!"
                    errors.append(msg)
                    print(f"  [FAIL] {msg}")

        # Kontrola Dialog Trees - GiveItem/TakeItem
        for npc in npcs:
            tree = npc.get("DialogTree", {})
            for node_id, node in tree.items():
                for opt in node.get("Options", []):
                    if opt.get("GiveItem") and opt["GiveItem"] not in item_ids:
                        msg = f"NPC '{npc['Id']}' dialog dává '{opt['GiveItem']}' - neexistuje!"
                        errors.append(msg)
                        print(f"  [FAIL] {msg}")
                    if opt.get("NextNodeId") and opt["NextNodeId"] not in tree:
                        msg = f"NPC '{npc['Id']}' dialog odkazuje node '{opt['NextNodeId']}' - neexistuje!"
                        errors.append(msg)
                        print(f"  [FAIL] {msg}")

        if not any(e for e in errors if "world" in e.lower() or "room" in e.lower()):
            print("  [OK] Všechny reference jsou konzistentní.")

    except json.JSONDecodeError as e:
        errors.append(f"world.json není validní JSON: {e}")
        print(f"  [FAIL] JSON parse error: {e}")

# ---------------------------------------------------------------------------
# 3.  Příprava testovacích účtů
# ---------------------------------------------------------------------------
print(f"\n[2/5] Příprava testovacích účtů v '{accounts_dir}' ...")

# Definice testovacích účtů (podle Test_Cases_MUD.md)
test_accounts = {
    # novacek - NESMÍ existovat (registrace v testu)
    # hrac_veteran - existující, prázdný inventář, na startu
    "hrac_veteran": {
        "Name": "hrac_veteran",
        "Password": "mojeheslo",
        "LocationId": starting_room if 'starting_room' in dir() else "entrance",
        "InventoryItems": [],
        "Currency": 100,
        "Quests": {}
    },
    # bohaty_hrac - existující, s knihou a 500 penězi
    "bohaty_hrac": {
        "Name": "bohaty_hrac",
        "Password": "heslo",
        "LocationId": starting_room if 'starting_room' in dir() else "entrance",
        "InventoryItems": ["book"],
        "Currency": 500,
        "Quests": {}
    },
}

# Účty, které MUSÍ být smazány (nesmí existovat pro registrační test)
accounts_to_delete = ["novacek"]

# Vyčistíme VŠECHNY staré testovací soubory + nesmějící existovat
os.makedirs(accounts_dir, exist_ok=True)

for name in accounts_to_delete:
    path = os.path.join(accounts_dir, f"{name}.json")
    if os.path.exists(path):
        os.remove(path)
        print(f"  [DEL] Smazán účet: {name}")
    else:
        print(f"  [OK]  Účet '{name}' už neexistoval.")

# Vytvoříme/přepíšeme požadované účty
for name, data in test_accounts.items():
    path = os.path.join(accounts_dir, f"{name}.json")
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    print(f"  [SET] Vytvořen účet: {name} (heslo ve hře, lokace={data['LocationId']}, currency={data['Currency']}, items={data['InventoryItems']})")

# ---------------------------------------------------------------------------
# 4.  Příprava log souboru
# ---------------------------------------------------------------------------
print(f"\n[3/5] Příprava log souboru ...")
log_dir = os.path.dirname(log_path)
if log_dir:
    os.makedirs(log_dir, exist_ok=True)

if os.path.exists(log_path):
    # Promazat starý log
    os.remove(log_path)
    print(f"  [DEL] Starý log smazán: {log_path}")

# Vytvoříme prázdný soubor
with open(log_path, "w", encoding="utf-8") as f:
    f.write("")
print(f"  [OK]  Prázdný log vytvořen: {log_path}")

# ---------------------------------------------------------------------------
# 5.  Příprava statistics.txt
# ---------------------------------------------------------------------------
print(f"\n[4/5] Reset souboru Data/statistics.txt ...")
stats_path = os.path.join(PROJECT_ROOT, "Data", "statistics.txt")
if os.path.exists(stats_path):
    os.remove(stats_path)
    print(f"  [DEL] Starý statistics.txt smazán.")
else:
    print(f"  [OK]  statistics.txt neexistoval.")

# ---------------------------------------------------------------------------
# 6.  Shrnutí
# ---------------------------------------------------------------------------
print(f"\n[5/5] Shrnutí kontroly ...")
print("=" * 60)

if errors:
    print(f"  NALEZENO {len(errors)} CHYB:")
    for i, err in enumerate(errors, 1):
        print(f"    {i}. {err}")
    print("=" * 60)
    print("  ⚠️  Opravte chyby výše, než začnete testovat!")
else:
    print("  ✅  Vše v pořádku! Prostředí je připraveno k testování.")
    print()
    print("  Testovací účty:")
    print(f"    hrac_veteran / mojeheslo  (na startu, prázdný inventář, 100 currency)")
    print(f"    bohaty_hrac  / heslo      (na startu, má 'book', 500 currency)")
    print(f"    novacek      / -          (NEEXISTUJE - pro registrační test)")
    print()
    print(f"  Spuštění serveru:")
    print(f"    cd {PROJECT_ROOT}")
    print(f"    dotnet run")
    print(f"    (Server naslouchá na portu {port})")
    print()

    # Načtení konfigurace klienta
    client_dir = os.path.join(PROJECT_ROOT, "MUD_Client")
    client_config_path = os.path.join(client_dir, "appsettings.json")
    client_ip = "127.0.0.1"
    client_port = port
    if os.path.exists(client_config_path):
        with open(client_config_path, "r", encoding="utf-8") as f:
            client_cfg = json.load(f)
        client_ip = client_cfg.get("ServerIp", client_ip)
        client_port = client_cfg.get("ServerPort", client_port)

    print(f"  Spuštění klienta:")
    print(f"    cd {client_dir}")
    print(f"    dotnet run")
    print(f"    (Klient se připojí na {client_ip}:{client_port})")

print("=" * 60)
