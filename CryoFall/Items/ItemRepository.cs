using System.Text.Json;
using CryoFall.Items;

namespace CryoFall.Utils
{
    /// <summary>
    /// Rappresenta un singolo item come definito in item.json.
    /// </summary>
    public sealed record ItemDefinition(
        string Id,
        string Name,
        string Description,
        double Weight,
        bool IsPickable,
        bool IsUsable,
        bool IsAnalyzable,
        string Color
    );

    /// <summary>
    /// Legge il file JSON e fornisce accesso rapido agli item.
    /// </summary>
    public sealed class ItemRepository
    {
        private readonly List<ItemDefinition> _allItems;
        private readonly Dictionary<string, ItemDefinition> _byId;

        private ItemRepository(List<ItemDefinition> items)
        {
            _allItems = items;
            _byId     = items.ToDictionary(i => i.Id);
        }

        /// <summary>
        /// Carica "item.json" dalla cartella Data/ (output directory).
        /// </summary>
        public static ItemRepository Load()
        {
            var baseDir = AppContext.BaseDirectory;
            var jsonPath = Path.Combine(baseDir, "Data", "item.json");

            var json = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var items = JsonSerializer.Deserialize<List<ItemDefinition>>(json, options)
                        ?? throw new InvalidDataException("Impossibile deserializzare item.json");

            // Controllo duplicati di id
            var dupes = items.GroupBy(i => i.Id)
                             .Where(g => g.Count() > 1)
                             .Select(g => g.Key)
                             .ToList();
            if (dupes.Any())
            {
                throw new InvalidDataException($"ID duplicati: {string.Join(", ", dupes)}");
            }

            return new ItemRepository(items);
        }

        /// <summary>
        /// Ottiene l'item con ID esatto. Lancia eccezione se non esiste.
        /// </summary>
        public ItemDefinition Get(string id) => _byId[id];

        /// <summary>
        /// Ritorna true e l'item se esiste, altrimenti false.
        /// </summary>
        public bool TryGet(string id, out ItemDefinition? item) =>
            _byId.TryGetValue(id, out item);

        /// <summary>
        /// Elenco completo di tutti gli item (utile per debug, editor, ecc.).
        /// </summary>
        public IReadOnlyList<ItemDefinition> AllItems => _allItems;
        
        
        public List<Item> GetAllItemsFromJson()
        {
            List<Item> itemsList = new();
            foreach (var item in _allItems)
            {
                itemsList.Add(new Item(item.Id,item.Name,item.Description,item.Weight,item.IsPickable,item.IsUsable,item.IsAnalyzable,item.Color));
            }
            return itemsList;
        }
    }
}