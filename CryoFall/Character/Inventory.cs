using CryoFall.Items;
using CryoFall.Rooms;

namespace CryoFall.Character
{
    /// <summary>
    /// Inventario a pila (LIFO) con un limite di peso totale.
    /// Solo l’oggetto in cima può essere prelevato direttamente;
    /// per usarne uno in fondo il giocatore dovrà rimuovere manualmente quelli sopra.
    /// </summary>
    public class Inventory(double maxCapacity)
    {
        // ─── Campi ────────────────────────────────────────────────────────
        private readonly double _maxCapacity = maxCapacity >= 0
            ? maxCapacity
            : throw new ArgumentOutOfRangeException(
                  nameof(maxCapacity), "La capacità massima non può essere negativa.");

        // lista usata come pila (indice più alto = cima)
        private readonly List<Item> _stack = new();

        // ─── Proprietà ───────────────────────────────────────────────────
        /// <summary>Peso complessivo degli oggetti trasportati.</summary>
        public double CurrentLoad => _stack.Sum(i => i.Weight);

        /// <summary>Vista in sola lettura (cima → fondo).</summary>
        public IReadOnlyList<Item> Items => _stack.AsReadOnly();

        // ─── Operazioni ──────────────────────────────────────────────────
        /// <summary>
        /// Aggiunge l’item in cima se non supera <see cref="_maxCapacity"/>.
        /// </summary>
        public bool TryAdd(Item item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));
            if (CurrentLoad + item.Weight > _maxCapacity) return false;

            _stack.Add(item);
            return true;
        }

        /// <summary>
        /// Rimuove l’item in cima e lo deposita nella <paramref name="room"/> corrente.
        /// </summary>
        public bool DropTop(Room? room)
        {
            if (room is null || _stack.Count == 0) return false;

            var top = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);
            room.AddItem(top);
            return true;
        }

        /// <summary>
        /// Preleva dalla <paramref name="room"/> il primo oggetto con nome
        /// <paramref name="itemName"/> e lo mette in cima,
        /// se il peso restante lo consente.
        /// </summary>
        public bool TryPickUp(Room? room, string itemName)
        {
            if (room is null) return false;

            var candidate = room.Items
                .FirstOrDefault(i => i.Name.Equals(itemName,
                                                   StringComparison.OrdinalIgnoreCase));
            if (candidate is null) return false;
            if (CurrentLoad + candidate.Weight > _maxCapacity) return false;

            room.RemoveItem(candidate);
            _stack.Add(candidate);
            return true;
        }
    }
}
