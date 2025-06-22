using System.Text.Json;

namespace CryoFall.Character;

/// <summary>
/// Carica i personaggi (solo Nome + PlaceHolder)
/// e mette a disposizione il dizionario pronto:
///     {"playerName" → "Roberto",}
/// </summary>
public static class CharacterRepository
{
    /// <summary>
    /// Dizionario pubblico e readonly:
    ///     chiave  = PlaceHolder (playerName, robotName)
    ///     valore  = Nome visualizzato (Roberto, AX-47)
    /// </summary>
    public static Dictionary<string, string> PlaceholdersNames { get; } = Load();

    private static Dictionary<string, string> Load()
    {
        string baseDir  = AppContext.BaseDirectory;                  // bin/Debug/… o publish/
        string jsonPath = Path.Combine(baseDir, "Data", "Characters.json");

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Leggiamo solo i due campi che ci servono
        var list = JsonSerializer.Deserialize<List<CharacterStub>>(File.ReadAllText(jsonPath), opts)
                   ?? throw new InvalidDataException("Impossibile deserializzare Characters.json");

        // Costruiamo il dizionario case-insensitive
        return list.ToDictionary(c => c.PlaceHolder,
            c => c.Nome,
            StringComparer.OrdinalIgnoreCase);
    }

    // record “stub” con solo i campi utili
    private sealed record CharacterStub(string Nome, string PlaceHolder);
}