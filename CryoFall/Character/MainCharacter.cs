using CryoFall.Rooms;

namespace CryoFall.Character;

/// <summary>
/// Rappresenta il personaggio principale controllato dal giocatore.
/// Possiede un inventario personale.
/// </summary>
/// <param name="name">Nome del personaggio.</param>
/// <param name="maxCapacity">Capacità dell'inventario del personaggio</param>
public class MainCharacter(string name, int maxCapacity)
{
    /// <summary>Nome del personaggio.</summary>
    public string Name
    {
        get => _name;
        init => _name = value ?? throw new ArgumentNullException(nameof(value));
    }
    private readonly string _name = name;

    /// <summary>
    /// Stanza in cui si trova attualmente il giocatore
    /// </summary>
    public Room? CurrentRoom { get; set; }
    
    /// <summary>Inventario personale del giocatore.</summary>
    public Inventory Inventory { get; } = new(maxCapacity);
    
}