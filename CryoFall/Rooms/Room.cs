using CryoFall.Items;

namespace CryoFall.Rooms
{
    /// <summary>
    /// Rappresenta i riferimenti alle stanze adiacenti (nord, sud, ovest, est)
    /// rispetto a una determinata <see cref="Room"/>.
    /// </summary>
    public struct RoomsNear
    {
        public Room? UpperRoom { get; set; }
        public Room? LowerRoom { get; set; }
        public Room? LeftRoom  { get; set; }
        public Room? RightRoom { get; set; }
    }

    /// <summary>
    /// Modella una stanza dell’astronave: contiene dati descrittivi,
    /// lo stato di blocco/sblocco, gli oggetti presenti e i collegamenti
    /// con le stanze adiacenti.
    /// </summary>
    public class Room(string nameOfTheRoom,
                      string descriptionOfTheRoom,
                      bool   isLocked)
    {
        // ─── Dati di base ────────────────────────────────────────────────
        public string NameOfTheRoom
        {
            get => _nameOfTheRoom;
            set => _nameOfTheRoom = value ?? throw new ArgumentNullException(nameof(value));
        }
        private string _nameOfTheRoom = nameOfTheRoom;

        public string DescriptionOfTheRoom
        {
            get => _descriptionOfTheRoom;
            set => _descriptionOfTheRoom = value ?? throw new ArgumentNullException(nameof(value));
        }
        private string _descriptionOfTheRoom = descriptionOfTheRoom;

        public bool IsLocked
        {
            get => _isLocked;
            set => _isLocked = value;
        }
        private bool _isLocked = isLocked;

        // ─── Oggetti presenti nella stanza ──────────────────────────────
        private readonly List<Item> _items = new();

        /// <summary>Vista in sola lettura degli oggetti presenti.</summary>
        public IReadOnlyList<Item> Items => _items.AsReadOnly();

        /// <summary>Aggiunge un oggetto alla stanza.</summary>
        public void AddItem(Item item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));
            _items.Add(item);
        }

        /// <summary>Rimuove la prima occorrenza dell’item indicato.</summary>
        public bool RemoveItem(Item item) => _items.Remove(item);

        /// <summary>
        /// Estrae (e restituisce) il primo oggetto con il nome dato.  
        /// Torna <c>null</c> se l’oggetto non è presente.
        /// </summary>
        public Item? TakeItem(string itemName)
        {
            var found = _items.FirstOrDefault(i =>
                i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));

            if (found != null) _items.Remove(found);
            return found;
        }

        // ─── Stanze adiacenti ────────────────────────────────────────────
        public RoomsNear NearRooms
        {
            get => _nearRooms;
            set => _nearRooms = value;
        }
        private RoomsNear _nearRooms;
    }
}
