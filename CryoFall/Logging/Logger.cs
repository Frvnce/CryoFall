namespace CryoFall.Logging;

/// <summary>
/// Gestisce la creazione e scrittura dei file di log per tenere traccia degli eventi nel gioco.
/// Ogni nuova partita genera automaticamente un nuovo file nella cartella "Logs".
/// </summary>
public static class Logger
{
    /// <summary>
    /// Percorso della cartella dove vengono salvati i file di log.
    /// </summary>
    private static readonly string logDir = Path.Combine(AppContext.BaseDirectory, "Logs");

    /// <summary>
    /// Percorso completo del file di log corrente.
    /// </summary>
    private static string logPath;

    /// <summary>
    /// Inizializza un nuovo file di log creando la cartella "Logs" se non esiste
    /// e trovando un nome progressivo libero (es: log_01.txt, log_02.txt, ...).
    /// Va chiamato all'avvio di una nuova partita.
    /// </summary>
    public static void InitNewLog()
    {
        // Crea la cartella Logs se non esiste
        Directory.CreateDirectory(logDir);

        // Trova il primo nome disponibile per il nuovo file di log
        int count = 1;
        do
        {
            logPath = Path.Combine(logDir, $"log_{count:D2}.txt");
            count++;
        } while (File.Exists(logPath));

        // Scrive intestazione iniziale nel file di log
        Log("====== Nuova Partita ======");
        Log($"Data: {DateTime.Now:dd/MM/yyyy} - Ora: {DateTime.Now:HH:mm:ss}");
    }

    /// <summary>
    /// Scrive una riga nel file di log corrente con timestamp.
    /// È necessario chiamare prima <see cref="InitNewLog"/> per inizializzare il file.
    /// </summary>
    /// <param name="message">Il messaggio da scrivere nel log.</param>
    public static void Log(string message)
    {
        if (string.IsNullOrEmpty(logPath))
            throw new InvalidOperationException("Devi chiamare InitNewLog() prima di usare Log().");

        string logMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
        File.AppendAllText(logPath, logMessage + Environment.NewLine);
    }
}