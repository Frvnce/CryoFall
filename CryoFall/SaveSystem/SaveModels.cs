namespace CryoFall.SaveSystem
{
    /// <summary>
    /// Dati di salvataggio principali del gioco
    /// </summary>
    public class SaveGameData
    {
        public string PlayerCurrentRoomId { get; set; } // ID stanza corrente del giocatore
        public List<string> PlayerInventoryIds { get; set; } = new(); // ID oggetti in inventario
        public List<RoomSaveData> Rooms { get; set; } = new(); // Dati salvataggio stanze
        public bool IsTutorialCompleted { get; set; } // Tutorial completato?
        public List<string> VisitedRoomIds { get; set; } = new(); // Stanze visitate
        public string? LastDialogueId { get; set; } = null; // ultimo dialogo
        public Dictionary<string,string> Placeholders { get; set; } = new();
    }

    /// <summary>
    /// Dati di salvataggio per una singola stanza
    /// </summary>
    public class RoomSaveData
    {
        public string RoomId { get; set; } // ID della stanza
        public bool IsLocked { get; set; } // Stato di blocco della stanza
        public List<string> ItemIdsInRoom { get; set; } = new(); // ID oggetti presenti
    }
}