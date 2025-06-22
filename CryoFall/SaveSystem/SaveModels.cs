namespace CryoFall.SaveSystem
{
    /// <summary>
    /// Dati di salvataggio principali del gioco.
    /// </summary>
    public class SaveGameData
    {
        /// <summary>
        /// Identificativo della stanza in cui si trova attualmente il giocatore.
        /// </summary>
        public string PlayerCurrentRoomId { get; set; }

        /// <summary>
        /// Elenco degli ID degli oggetti attualmente in inventario.
        /// </summary>
        public List<string> PlayerInventoryIds { get; set; } = new();

        /// <summary>
        /// Dati di salvataggio di tutte le stanze del gioco.
        /// </summary>
        public List<RoomSaveData> Rooms { get; set; } = new();

        /// <summary>
        /// Indica se il tutorial è stato completato.
        /// </summary>
        public bool IsTutorialCompleted { get; set; }

        /// <summary>
        /// ID delle stanze già visitate dal giocatore.
        /// </summary>
        public List<string> VisitedRoomIds { get; set; } = new();

        /// <summary>
        /// ID dell’ultimo dialogo avviato, se presente.
        /// </summary>
        public string? LastDialogueId { get; set; } = null;

        /// <summary>
        /// Mappa di placeholder dinamici usati nelle strings di dialogo e interfaccia.
        /// </summary>
        public Dictionary<string, string> Placeholders { get; set; } = new();

        /// <summary>
        /// Elenco degli eventi di gioco già attivati.
        /// </summary>
        public List<string> EventiAttivati { get; set; } = new();
    }

    /// <summary>
    /// Dati di salvataggio relativi a una singola stanza.
    /// </summary>
    public class RoomSaveData
    {
        /// <summary>
        /// Identificativo univoco della stanza.
        /// </summary>
        public string RoomId { get; set; }

        /// <summary>
        /// Indica se la stanza è bloccata.
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// Elenco degli ID degli oggetti presenti nella stanza.
        /// </summary>
        public List<string> ItemIdsInRoom { get; set; } = new();
    }
}
