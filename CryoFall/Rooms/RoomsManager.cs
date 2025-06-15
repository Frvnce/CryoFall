namespace CryoFall.Rooms
{
    /// <summary>
    /// Gestisce l’insieme di tutte le <see cref="Room"/> conosciute dal gioco:
    /// permette di aggiungerle, rimuoverle, elencarle o cercarle in base al nome.
    /// </summary>
    public class RoomsManager
    {
        // ─── Lista interna delle stanze ─────────────────────────────────────
        private readonly List<Room> _rooms = new();

        /// <summary>
        /// Restituisce una vista in sola lettura sulle stanze attualmente registrate.
        /// </summary>
        public IReadOnlyList<Room> Rooms => _rooms;

        // ─── Operazioni principali ─────────────────────────────────────────
        /// <summary>
        /// Aggiunge una stanza al manager.
        /// </summary>
        /// <param name="room">Istanza di <see cref="Room"/> da registrare.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="room"/> è <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// Se esiste già una stanza con lo stesso <see cref="Room.NameOfTheRoom"/>.
        /// </exception>
        public void AddRoom(Room room)
        {
            if (room is null)
                throw new ArgumentNullException(nameof(room));

            if (_rooms.Any(r => r.NameOfTheRoom == room.NameOfTheRoom))
                throw new InvalidOperationException(
                    $"Esiste già una stanza chiamata «{room.NameOfTheRoom}».");

            _rooms.Add(room);
        }

        /// <summary>
        /// Rimuove la stanza indicata dal manager.
        /// </summary>
        /// <param name="room">Stanza da rimuovere.</param>
        /// <returns>
        /// <c>true</c> se la stanza era presente ed è stata rimossa;
        /// <c>false</c> se non era registrata.
        /// </returns>
        public bool RemoveRoom(Room room) => _rooms.Remove(room);

        /// <summary>
        /// Cerca una stanza per nome (case-insensitive).
        /// </summary>
        /// <param name="name">Nome della stanza da cercare.</param>
        /// <returns>
        /// L’istanza di <see cref="Room"/> trovata, oppure <c>null</c> se non esiste.
        /// </returns>
        public Room? FindRoom(string name) =>
            _rooms.FirstOrDefault(r =>
                r.NameOfTheRoom.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
