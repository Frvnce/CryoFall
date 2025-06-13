using System.Text.Json;

namespace CryoFall.Utils;

/// <summary>
/// Legge il file JSON e fornisce accesso rapido alle battute.
/// </summary>
public sealed class DialogueRepository
{
    private readonly List<DialogueLine> _allLines;
    private readonly Dictionary<string, DialogueLine> _byId;

    private DialogueRepository(List<DialogueLine> lines)
    {
        _allLines = lines;
        _byId     = lines.ToDictionary(l => l.Id);
    }

    /// <summary>
    /// Carica "dialogue.json" dalla cartella Data/ (output directory).
    /// </summary>
    public static DialogueRepository Load()
    {
        var baseDir  = AppContext.BaseDirectory;
        var jsonPath = Path.Combine(baseDir, "Data", "Dialogue.json");

        var json   = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var lines = JsonSerializer.Deserialize<List<DialogueLine>>(json, options)
                    ?? throw new InvalidDataException("Impossibile deserializzare Dialogue.json");

        // Controllo duplicati di id
        var dupes = lines.GroupBy(l => l.Id).Where(g => g.Count() > 1).ToList();
        if (dupes.Any())
            throw new InvalidDataException($"ID duplicati: {string.Join(", ", dupes.Select(g => g.Key))}");

        return new DialogueRepository(lines);
    }

    /// <summary>
    /// Ottiene la battuta con ID esatto. Lancia eccezione se non esiste.
    /// </summary>
    public DialogueLine Get(string id) => _byId[id];

    /// <summary>
    /// Elenco completo (utile per debug, editor, ecc.).
    /// </summary>
    public IReadOnlyList<DialogueLine> AllLines => _allLines;
    
    public bool TryGet(string id, out DialogueLine? line) =>
        _byId.TryGetValue(id, out line);
}

/// <summary>
/// Rappresenta una singola riga di dialogo o narrazione.
/// </summary>
public sealed record DialogueLine(
    string Id,
    string Room,
    string SpeakerName,
    string Character,
    string Kind,
    string Text,
    string? Next      = null,
    List<Choice>? Choices = null
);

/// <summary>
/// Rappresenta una scelta interattiva che punta a un altro ID.
/// </summary>
public sealed record Choice(string Label, string Next);

