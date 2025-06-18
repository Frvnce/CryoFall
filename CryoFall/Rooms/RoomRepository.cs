using System.Text.Json;
using CryoFall.Rooms;

namespace CryoFall.Utils
{
    /// <summary>
    /// Rappresenta le stanze adiacenti per una RoomDefinition.
    /// </summary>
    public sealed record AdjacentDefinition(
        string? North,
        string? South,
        string? East,
        string? West
    );

    /// <summary>
    /// Rappresenta la definizione di una stanza come configurata in room.json.
    /// </summary>
    public sealed record RoomDefinition(
        string Id,
        string Name,
        string Description,
        bool IsLocked,
        string? UnlockKeyId,
        AdjacentDefinition AdjacentRooms,
        List<string>? Items,
        List<string>? Persons
    );
    
    /// <summary>
    /// Repository per l'accesso alle stanze definite in room.json.
    /// </summary>
    public sealed class RoomRepository
    {
        private readonly List<RoomDefinition> _allRooms;
        private readonly Dictionary<string, RoomDefinition> _byId;

        private RoomRepository(List<RoomDefinition> rooms)
        {
            _allRooms = rooms;
            _byId = rooms.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
        }

        public List<Room> GetAllRoomsObjects()
        {
            List<Room> rooms = new();
            foreach (var room in _allRooms)
            {
                rooms.Add(new Room(room.Id, room.Name, room.Description, room.IsLocked, room.UnlockKeyId, room.AdjacentRooms, room.Items, room.Persons));
            }
            return rooms;
        }

        public List<Room> GetAllNearRooms(List<Room> rooms,RoomsManager manager)
        {
            for (var i = 0; i < rooms.Count; i++)
            {
                var leftRoom = _allRooms[i].AdjacentRooms.West;
                var rightRoom = _allRooms[i].AdjacentRooms.East;
                var lowerRoom = _allRooms[i].AdjacentRooms.South;
                var upperRoom = _allRooms[i].AdjacentRooms.North;

                if (leftRoom != null)
                {
                    rooms[i].NearRooms.LeftRoom = manager.FindRoom(leftRoom);
                }

                if (rightRoom != null)
                {
                    rooms[i].NearRooms.RightRoom = manager.FindRoom(rightRoom);
                }

                if (upperRoom != null)
                {
                    rooms[i].NearRooms.UpperRoom = manager.FindRoom(upperRoom);
                }

                if (lowerRoom != null)
                {
                    rooms[i].NearRooms.LowerRoom = manager.FindRoom(lowerRoom);
                }
            }
            return rooms;
        }
        
        /// <summary>
        /// Carica il file "room.json" dalla cartella Data/ (output directory).
        /// </summary>
        public static RoomRepository Load()
        {
            var baseDir = AppContext.BaseDirectory;
            var jsonPath = Path.Combine(baseDir, "Data", "room.json");
            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"File {jsonPath} non trovato");
            }

            var json = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var rooms = JsonSerializer.Deserialize<List<RoomDefinition>>(json, options)
                        ?? throw new InvalidDataException("Impossibile deserializzare room.json");

            // Controllo duplicati di ID
            var dupes = rooms.GroupBy(r => r.Id)
                             .Where(g => g.Count() > 1)
                             .Select(g => g.Key)
                             .ToList();
            if (dupes.Any())
            {
                throw new InvalidDataException($"ID duplicati nelle stanze: {string.Join(", ", dupes)}");
            }

            return new RoomRepository(rooms);
        }

        /// <summary>
        /// Ritorna la stanza con l'ID esatto. Lancia eccezione se non esiste.
        /// </summary>
        public RoomDefinition Get(string id) => _byId[id];

        /// <summary>
        /// Tenta di ottenere la stanza con l'ID specificato.
        /// </summary>
        public bool TryGet(string id, out RoomDefinition? room) =>
            _byId.TryGetValue(id, out room);

        /// <summary>
        /// Restituisce tutte le stanze caricate (utile per debug, editor, ecc.).
        /// </summary>
        public IReadOnlyList<RoomDefinition> AllRooms => _allRooms;
    }
}
