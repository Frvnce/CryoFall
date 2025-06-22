using System.Text.Json;
using CryoFall.Items;
using CryoFall.Rooms;
using CryoFall.Character;
using CryoFall.Dialogue;

namespace CryoFall.SaveSystem
{
    /// <summary>
    /// Gestisce il salvataggio e il caricamento del gioco su file
    /// </summary>
    public static class SaveManager
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true, // formattazione leggibile
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase // camelCase nei nomi JSON
        };

        public static void Save(string path, MainCharacter player, RoomsManager roomsManager, ItemsManager itemsManager)
        {
            // prepara dati di salvataggio
            var data = new SaveGameData
            {
                PlayerCurrentRoomId = player.CurrentRoom.Id,
                PlayerInventoryIds  = player.Inventory.Items.Select(i => i.Id).ToList(),
                IsTutorialCompleted = player.HasCompletedTutorial,
                VisitedRoomIds = player.VisitedRoomIds.ToList(),
                LastDialogueId = player.LastDialogueId,
                Placeholders = ConsoleStylingWrite.GetPlaceholdersDict(),
                EventiAttivati = player.EventiAttivati.ToList()
            };

            // aggiunge lo stato di ogni stanza
            foreach (var room in roomsManager.GetRooms())
            {
                var rsd = new RoomSaveData
                {
                    RoomId        = room.Id,
                    IsLocked      = room.IsLocked,
                    ItemIdsInRoom = room.GetItems().Select(i => i.Id).ToList()
                };
                data.Rooms.Add(rsd);
            }

            File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOptions)); // Scrive file JSON
        }

        public static void Load(string path, MainCharacter player, RoomsManager roomsManager, ItemsManager itemsManager)
        {
            //Console.WriteLine(path);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Save file non trovato: {path}");

            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<SaveGameData>(json, JsonOptions)
                       ?? throw new InvalidDataException("Salvataggio corrotto");
            ConsoleStylingWrite.SetPlaceholdersDict(data.Placeholders);

            // ripristina tutorial e stanze visitate
            player.HasCompletedTutorial = data.IsTutorialCompleted;
            player.VisitedRoomIds.Clear();
            foreach (var id in data.VisitedRoomIds)
                player.VisitedRoomIds.Add(id);
            player.LastDialogueId = data.LastDialogueId;
            player.EventiAttivati.Clear();
            foreach (var id in data.EventiAttivati)
                player.EventiAttivati.Add(id);

            // imposta stanza corrente
            var startRoom = roomsManager.FindRoom(data.PlayerCurrentRoomId)
                            ?? throw new InvalidOperationException("Room salvata non trovata");
            player.CurrentRoom = startRoom;

            // ricostruisce inventario
            player.Inventory.ClearAll();

            // itero da fondo a cima così quando ForceAdd() aggiunge ogni istanza, ricostruisco la pila nell’ordine corretto
            for (int idx = data.PlayerInventoryIds.Count - 1; idx >= 0; idx--)
            {
                var itemId = data.PlayerInventoryIds[idx];
                // prendo l'istanza canonica da ItemsManager anziché ricrearne una nuova
                var inst = itemsManager.FindItem(itemId);
                if (inst != null)
                {
                    player.Inventory.ForceAdd(inst);
                }
            }



            // ripristina stato oggetti e blocchi in ogni stanza
            foreach (var roomSave in data.Rooms)
            {
                var room = roomsManager.FindRoom(roomSave.RoomId);
                if (room == null) continue;

                room.IsLocked = roomSave.IsLocked;
                // svuota
                foreach (var it in room.GetItems().ToList())
                    room.RemoveItem(it);

                // aggiungi le stesse istanze di ItemsManager
                foreach (var itemId in roomSave.ItemIdsInRoom)
                {
                    var inst = itemsManager.FindItem(itemId);
                    if (inst != null)
                        room.AddItem(inst);
                }
            }

        }
    }
}