namespace CryoFall.Items
{
    /// <summary>
    /// Gestisce l’insieme di tutte le <see cref="Room"/> conosciute dal gioco:
    /// permette di aggiungerle, rimuoverle, elencarle o cercarle in base al nome.
    /// </summary>
    public class ItemsManager
    {
        // ─── Lista interna delle stanze ─────────────────────────────────────
        private List<Item> _items = new();

        public List<Item> GetItems()
        {
            return _items;
        }

        public void SetItemList(List<Item> itemsList)
        {
            _items = itemsList;
        }

        // ─── Operazioni principali ─────────────────────────────────────────
        /// <summary>
        /// Aggiunge un oggetto al manager.
        /// </summary>
        /// <param name="item">Istanza di <see cref="Item"/> da registrare.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="item"/> è <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// Se esiste già un oggetto con lo stesso <see cref="Item.Id"/>.
        /// </exception>
        public void AddItem(Item item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            if (_items.Any(r => r.Id == item.Id))
                throw new InvalidOperationException(
                    $"Esiste già un oggetto chiamato «{item.Id}».");

            _items.Add(item);
        }

        /// <summary>
        /// Rimuove l'oggetto indicata dal manager.
        /// </summary>
        /// <param name="item">Oggetto da rimuovere.</param>
        /// <returns>
        /// <c>true</c> se L'oggetto era presente ed è stata rimosso;
        /// <c>false</c> se non era registrato.
        /// </returns>
        public bool RemoveItem(Item item) => _items.Remove(item);

        /// <summary>
        /// Cerca un oggetto per nome (case-insensitive).
        /// </summary>
        /// <param name="id">Id dell'oggetto da cercare.</param>
        /// <returns>
        /// L’istanza di <see cref="Item"/> trovata, oppure <c>null</c> se non esiste.
        /// </returns>
        public Item? FindItem(string id)
        {
            Item item = null;
            foreach (var itemInList in _items)
            {
                if (itemInList.Id == id) item = itemInList;
            }
            return item;
        }
            
    }
}
