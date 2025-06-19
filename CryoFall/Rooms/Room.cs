using CryoFall.Items;

namespace CryoFall.Rooms
{
    /// <summary>
    /// Rappresenta i riferimenti alle stanze adiacenti (nord, sud, ovest, est)
    /// rispetto a una determinata <see cref="Room"/>.
    /// </summary>
    public struct RoomsNear
    {
        public Room? NordRoom;
        public Room? SudRoom;
        public Room? OvestRoom;
        public Room? EstRoom;
    }

    /// <summary>
    /// Modella una stanza dell’astronave: contiene dati descrittivi,
    /// lo stato di blocco/sblocco, gli oggetti presenti e i collegamenti
    /// con le stanze adiacenti.
    /// </summary>
    public class Room(string id,
                      string nameOfTheRoom,
                      string descriptionOfTheRoom,
                      bool   isLocked,
                      string   unlockKeyId,
                      AdjacentDefinition adjacentRooms,
                      List<string>? items,
                      List<string>? persons)
    {
        // ─── Dati di base ────────────────────────────────────────────────

        public string Id
        {
            get => _id;
            set => _id = value ?? throw new ArgumentNullException(nameof(value));
        }
        private string _id = id;
        
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

        public string UnlockKeyId
        {
            get => _unlockKeyId;
            set => _unlockKeyId = value ?? throw new ArgumentNullException(nameof(value));
        }
        private string _unlockKeyId = unlockKeyId;
        
        public List<string> ItemsString
        {
            get => _itemsString;
            set => _itemsString = value ?? throw new ArgumentNullException(nameof(value));
        }
        private List<string> _itemsString = items;

        public List<string> PersonsInTheRoom
        {
            get => _personsInTheRoom;
            set => _personsInTheRoom = value ?? throw new ArgumentNullException(nameof(value));
        }
        private List<string> _personsInTheRoom = persons;

        // ─── Stanze adiacenti ────────────────────────────────────────────
        public RoomsNear NearRooms;

        public AdjacentDefinition AdjacentRooms
        {
            get => _adjacentRooms;
            set => _adjacentRooms = value ?? throw new ArgumentNullException(nameof(value));
        }
        private AdjacentDefinition _adjacentRooms = adjacentRooms;

        // ─── Oggetti presenti nella stanza ──────────────────────────────
        private readonly List<Item> _items = new();

        public List<Item> GetItems()
        {
            return _items;
        }
        
        /// <summary>Aggiunge un oggetto alla stanza.</summary>
        public void AddItem(Item item)
        {
            if (item is null) return;
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
            Item found = null;
            foreach (var itemInList in _items)
            {
                if (itemInList.Id.ToLower().Replace("_","") == itemName.ToLower().Replace("_","")) found = itemInList;
            }
            
            if (found != null) _items.Remove(found);
            return found;
        }

        
    }
}
