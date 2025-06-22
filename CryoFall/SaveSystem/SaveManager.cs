﻿using System.Text.Json;
using CryoFall.Items;
using CryoFall.Rooms;
using CryoFall.Character;
using CryoFall.Dialogue;

namespace CryoFall.SaveSystem
{
    /// <summary>
    /// Gestisce il salvataggio e il caricamento del gioco su file
    /// Include dati come: inventario, stanze, eventi attivati, e ID dialoghi
    /// </summary>
    public static class SaveManager
    {
        /// <summary>
        /// Opzioni del serializzatore JSON: scrittura formattata e camelCase
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true, // formattazione leggibile
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase // camelCase nei nomi JSON
        };

        /// <summary>
        /// Salva lo stato attuale del gioco in un file JSON
        /// </summary>
        /// <param name="path">Percorso del file</param>
        /// <param name="player">Oggetto MainCharacter da salvare</param>
        /// <param name="roomsManager">RoomsManager con tutte le stanze</param>
        /// <param name="itemsManager">ItemsManager con tutti gli oggetti</param>
        public static void Save(string path, MainCharacter player, RoomsManager roomsManager, ItemsManager itemsManager)
        {
            // prepara dati di salvataggio
            var data = new SaveGameData
            {
                PlayerCurrentRoomId = player.CurrentRoom.Id, // stanza attuale
                PlayerInventoryIds  = player.Inventory.Items.Select(i => i.Id).ToList(), // inventario (solo ID)
                IsTutorialCompleted = player.HasCompletedTutorial, // stato tutorial
                VisitedRoomIds = player.VisitedRoomIds.ToList(), // stanze visitate
                LastDialogueId = player.LastDialogueId, // ultimo dialogo
                Placeholders = ConsoleStylingWrite.GetPlaceholdersDict(), // placeholder per dialoghi
                EventiAttivati = player.ActivetedEvents.ToList() // eventi attivati
            };

            // salva stato di tutte le stanze
            foreach (var room in roomsManager.GetRooms())
            {
                var rsd = new RoomSaveData
                {
                    RoomId        = room.Id, // ID stanza
                    IsLocked      = room.IsLocked, // stato serratura
                    ItemIdsInRoom = room.GetItems().Select(i => i.Id).ToList() // oggetti presenti (solo ID)
                };
                data.Rooms.Add(rsd);
            }

            // serializza tutto il gioco in JSON
            File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOptions)); // Scrive file JSON
        }

        /// <summary>
        /// Carica lo stato del gioco da un file JSON
        /// </summary>
        /// <param name="path">Percorso del file</param>
        /// <param name="player">Oggetto MainCharacter da ricostruire</param>
        /// <param name="roomsManager">RoomsManager per accedere alle stanze</param>
        /// <param name="itemsManager">ItemsManager per accedere alle istanze oggetti</param>
        public static void Load(string path, MainCharacter player, RoomsManager roomsManager, ItemsManager itemsManager)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Save file non trovato: {path}");

            // carica e deserializza il file
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<SaveGameData>(json, JsonOptions)
                       ?? throw new InvalidDataException("Salvataggio corrotto");

            // aggiorna placeholder per dialoghi
            ConsoleStylingWrite.SetPlaceholdersDict(data.Placeholders);

            // ripristina tutorial, stanze visitate, eventi attivati e dialogo
            player.HasCompletedTutorial = data.IsTutorialCompleted;
            player.VisitedRoomIds.Clear();
            foreach (var id in data.VisitedRoomIds)
                player.VisitedRoomIds.Add(id);
            player.LastDialogueId = data.LastDialogueId;
            player.ActivetedEvents.Clear();
            foreach (var id in data.EventiAttivati)
                player.ActivetedEvents.Add(id);

            // imposta stanza corrente del personaggio
            var startRoom = roomsManager.FindRoom(data.PlayerCurrentRoomId)
                            ?? throw new InvalidOperationException("Room salvata non trovata");
            player.CurrentRoom = startRoom;

            // ripristina inventario con logica LIFO
            player.Inventory.ClearAll(); // svuota inventario

            // da ultimo a primo (LIFO): ForceAdd ricostruisce lo stack
            for (int idx = data.PlayerInventoryIds.Count - 1; idx >= 0; idx--)
            {
                var itemId = data.PlayerInventoryIds[idx];
                var inst = itemsManager.FindItem(itemId); // recupera istanza canonica
                if (inst != null)
                {
                    player.Inventory.ForceAdd(inst);
                }
            }

            // ripristina oggetti e stato delle stanze
            foreach (var roomSave in data.Rooms)
            {
                var room = roomsManager.FindRoom(roomSave.RoomId);
                if (room == null) continue;

                room.IsLocked = roomSave.IsLocked;

                // rimuove vecchi oggetti
                foreach (var it in room.GetItems().ToList())
                    room.RemoveItem(it);

                // aggiunge gli oggetti salvati (già esistenti in ItemsManager)
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
