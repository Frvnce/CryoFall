using System.Text.Json;
using CryoFall.Items;
using CryoFall.Rooms;
using CryoFall.Character;
using System.Text.Json.Serialization;
using CryoFall.SaveSystem;

namespace CryoFall.SaveSystem
{
    public static class SaveManager
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static void Save(string path,
                                MainCharacter player,
                                RoomsManager roomsManager,
                                ItemsManager itemsManager)
        {
            var data = new SaveGameData
            {
                PlayerCurrentRoomId = player.CurrentRoom.Id,
                PlayerInventoryIds  = player.Inventory.Items.Select(i => i.Id).ToList(),
                IsTutorialCompleted   = player.HasCompletedTutorial,
                VisitedRoomIds        = player.VisitedRoomIds.ToList()
            };

            foreach (var room in roomsManager.GetRooms())
            {
                var rsd = new RoomSaveData
                {
                    RoomId       = room.Id,
                    IsLocked     = room.IsLocked,
                    ItemIdsInRoom= room.GetItems().Select(i => i.Id).ToList()
                };
                data.Rooms.Add(rsd);
            }

            File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOptions));
        }

        public static void Load(string path,
                                MainCharacter player,
                                RoomsManager roomsManager,
                                ItemRepository itemRepo)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Save file non trovato: {path}");

            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<SaveGameData>(json, JsonOptions)
                       ?? throw new InvalidDataException("Salvataggio corrotto");
            player.HasCompletedTutorial = data.IsTutorialCompleted;
            player.VisitedRoomIds.Clear();
            foreach (var id in data.VisitedRoomIds)
                player.VisitedRoomIds.Add(id);

            // 1) Ripristino stanza corrente del giocatore
            var startRoom = roomsManager.FindRoom(data.PlayerCurrentRoomId)
                            ?? throw new InvalidOperationException("Room salvata non trovata");
            player.CurrentRoom = startRoom;

            // 2) Ripristino inventario
            player.Inventory.ClearAll();
            foreach (var itemId in data.PlayerInventoryIds)
            {
                if (itemRepo.TryGet(itemId, out var def))
                {
                    var inst = new Item(def.Id, def.Name, def.ItemDescription,
                                        def.Weight, def.IsPickable,
                                        def.IsUsable, def.IsAnalyzable, def.Color);
                    player.Inventory.ForceAdd(inst);
                }
            }

            // 3) Ripristino stato di tutte le stanze
            foreach (var roomSave in data.Rooms)
            {
                var room = roomsManager.FindRoom(roomSave.RoomId);
                if (room == null)
                    continue;

                room.IsLocked = roomSave.IsLocked;

                // svuota tutti gli item correnti
                foreach (var it in room.GetItems().ToList())
                    room.RemoveItem(it);

                // reinserisci quelli salvati
                foreach (var itemId in roomSave.ItemIdsInRoom)
                {
                    if (itemRepo.TryGet(itemId, out var def))
                    {
                        var inst = new Item(def.Id, def.Name, def.ItemDescription,
                                            def.Weight, def.IsPickable,
                                            def.IsUsable, def.IsAnalyzable, def.Color);
                        room.AddItem(inst);
                    }
                }
            } 
        }
    }
}
