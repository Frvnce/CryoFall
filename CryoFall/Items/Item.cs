namespace CryoFall.Items
{
    /// <summary>
    /// Rappresenta un oggetto interattivo del gioco (per esempio
    /// una chiave, un’arma, un documento ecc.).
    /// </summary>
    /// <param name="name">Nome identificativo mostrato al giocatore.</param>
    /// <param name="description">Breve descrizione testuale dell’oggetto.</param>
    /// <param name="weight">
    /// Peso dell’oggetto in unità arbitrarie (Chilogrammi).
    /// Deve essere maggiore o uguale a zero.
    /// </param>
    public class Item(string id, string name, string description, double weight, bool isPickalbe, bool isUsable, bool isAnalyzable, string color)
    {
        /// <summary>ID identificativo dell’oggetto.</summary>
        /// <exception cref="ArgumentNullException">
        /// Il valore assegnato è <c>null</c>.
        /// </exception>
        public string Id
        {
            get => _id;
            set => _id = value ?? throw new ArgumentNullException(nameof(value));
        }
        private string _id = id;
        
        /// <summary>Nome identificativo dell’oggetto.</summary>
        /// <exception cref="ArgumentNullException">
        /// Il valore assegnato è <c>null</c>.
        /// </exception>
        public string Name
        {
            get => _name;
            set => _name = value ?? throw new ArgumentNullException(nameof(value));
        }
        private string _name = name;

        /// <summary>Descrizione testuale dell’oggetto.</summary>
        /// <exception cref="ArgumentNullException">
        /// Il valore assegnato è <c>null</c>.
        /// </exception>
        public string Description
        {
            get => _description;
            set => _description = value ?? throw new ArgumentNullException(nameof(value));
        }
        private string _description = description;

        /// <summary>
        /// Peso dell’oggetto in unità arbitrarie (Kg).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Il valore assegnato è negativo.
        /// </exception>
        public double Weight
        {
            get => _weight;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Il peso non può essere negativo.");
                _weight = value;
            }
        }
        private double _weight = weight >= 0
            ? weight
            : throw new ArgumentOutOfRangeException(nameof(weight), "Il peso non può essere negativo.");
        
        /// <summary>Capire se l'oggetto è raccoglibile o no.</summary>
        /// <exception cref="ArgumentNullException">
        /// Il valore assegnato è <c>null</c>.
        /// </exception>
        public bool IsPickable
        {
            get => _isPickalbe;
            set => _isPickalbe = value;
        }
        private bool _isPickalbe = isPickalbe;
        
        /// <summary>Capire se l'oggetto è usabile o no.</summary>
        /// <exception cref="ArgumentNullException">
        /// Il valore assegnato è <c>null</c>.
        /// </exception>
        public bool IsUsable
        {
            get => _isUsable;
            set => _isUsable = value;
        }
        private bool _isUsable = isUsable;
        
        /// <summary>Capire se l'oggetto è analizzabile o no.</summary>
        /// <exception cref="ArgumentNullException">
        /// Il valore assegnato è <c>null</c>.
        /// </exception>
        public bool IsAnalyzable
        {
            get => _isAnalyzable;
            set => _isAnalyzable = value;
        }
        private bool _isAnalyzable = isAnalyzable;
        
        /// <summary>Colore dell'oggetto.</summary>
        /// <exception cref="ArgumentNullException">
        /// Il valore assegnato è <c>null</c>.
        /// </exception>
        public string Color
        {
            get => _color;
            set => _color = value ?? throw new ArgumentNullException(nameof(value));
        }
        private string _color = color;
        
    }
}
