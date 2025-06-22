using System.Text.Json;

namespace CryoFall.Commands;

public sealed record CommandInfo(
    string Id,
    string Name,
    string Description,
    string Cmd,
    IReadOnlyList<string> Alias);

/// <summary>
/// Carica i comandi di gioco da JSON
/// e rende disponibili due dizionari pre-costruiti:
///   • ById       → ricerca rapida per id   (tp, move...)
///   • ByKeyword  → ricerca per keyword o alias (teletrasporta, tp, muoviti, move...)
/// </summary>
public static class CommandsRepository
{
    /// <summary>
    /// Dizionario pubblico (readonly) con chiave = id del comando.
    /// </summary>
    public static IReadOnlyDictionary<string, CommandInfo> ById { get; } = Load();

    /// <summary>
    /// Dizionario pubblico (readonly) con chiave = keyword principale
    /// oppure uno qualsiasi degli alias.
    /// Utile per il parser dell’input dell’utente.
    /// </summary>
    public static IReadOnlyDictionary<string, CommandInfo> ByKeyword { get; } = BuildKeywordIndex(ById);

    private static Dictionary<string, CommandInfo> Load()
    {
        string baseDir  = AppContext.BaseDirectory;             // bin/Debug/… o publish/
        string jsonPath = Path.Combine(baseDir, "Data", "Commands.json");

        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // leggiamo soltanto i campi utili
        var list = JsonSerializer.Deserialize<List<CommandStub>>(
                       File.ReadAllText(jsonPath), opts)
                   ?? throw new InvalidDataException("Impossibile deserializzare Commands.json");

        // costruiamo dizionario id → CommandInfo (case-insensitive sulle chiavi)
        return list.ToDictionary(
            c => c.Id,
            c => new CommandInfo(c.Id, c.Name, c.CmdDescription, c.Cmd, c.Alias ?? Array.Empty<string>()),
            StringComparer.OrdinalIgnoreCase);
    }
    /// <summary>
    /// Crea un indice <c>keyword → CommandInfo</c> usando sia la keyword principale (prima parola del pattern)
    /// sia tutti gli alias definiti nel JSON.
    /// </summary>
    private static IReadOnlyDictionary<string, CommandInfo> BuildKeywordIndex(
        IReadOnlyDictionary<string, CommandInfo> byId)
    {
        var dict = new Dictionary<string, CommandInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var cmd in byId.Values)
        {
            // keyword principale = prima parola del pattern (es. “teletrasporta”)
            var keyword = cmd.Cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            dict[keyword] = cmd;

            // eventuali alias
            foreach (var alias in cmd.Alias)
                dict[alias] = cmd;
        }

        return dict;
    }

    /// <summary>
    /// Record interno usato SOLO per deserializzare il JSON: contiene anche campi extra non esposti pubblicamente.
    /// </summary>
    private sealed record CommandStub(
        string Id,
        string Name,
        string CmdDescription,
        string Cmd,
        IReadOnlyList<string>? Alias);
}