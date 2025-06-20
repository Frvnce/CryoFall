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
        public readonly double MaxCapacity = maxCapacity >= 0
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
        /// Aggiunge l’item in cima se non supera <see cref="MaxCapacity"/>.
        /// </summary>
        public bool TryAdd(Item item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));
            if (CurrentLoad + item.Weight > MaxCapacity) return false;

            _stack.Insert(0,item);
                
            return true;
        }

        /// <summary>
        /// Rimuove l’item in cima e lo deposita nella <paramref name="room"/> corrente.
        /// </summary>
        public bool DropTop(Room? room)
        {
            if (room is null || _stack.Count == 0) return false;

            var top = _stack[0];
            _stack.RemoveAt(0);
            room.AddItem(top);
            return true;
        }

        public Item GetFirstItem()
        {
            return _stack[0];
        }
        /// <summary>Rimuove tutti gli oggetti senza restituirli ad alcuna stanza.</summary>
        public void ClearAll()
        {
            // svuota internamente la pila
            _stack.Clear();
        }

        /// <summary>Aggiunge direttamente un item in cima, ignorando il limite di capacità.</summary>
        public void ForceAdd(Item item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));
            _stack.Insert(0, item);
        }
    }
}
