using System.Text.RegularExpressions;
using Spectre.Console;
namespace CryoFall.Utils;

public static class ConsoleStylingWrite
{
    //Ottenere il file dei dialoghi.
    private static readonly DialogueRepository Repo = DialogueRepository.Load();
    
    
    private static readonly Dictionary<string, string> Vars = 
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["robotName"]  = "AX-7"
            // aggiungi altri segnaposto qui o in runtime
        };
    private static readonly Regex PhRegex =
        new(@"<(?<key>[^>]+)>", RegexOptions.Compiled);
    private static readonly Regex TagRegex =
        new(@"\[[^\]]+\].*?\[\/\]", RegexOptions.Compiled | RegexOptions.Singleline);
    
    //Colori vari personaggi.
    private static readonly string ColorMainCharacter = "#12e6bb bold";
    private static readonly string ColorHelperCharacter = "#05a13b bold";
    private static readonly string ColorEnemyCharacter = "#9e2416 bold";
    private static readonly string ColorNarratorCharacter = "#dedddc bold italic";
    private static readonly string ColorIntroCharacter = "#dedddc bold";
    
    //Vari testi preSalvati.
    private static readonly string ChooseAnOptionTitle = "[bold #f1f1f1]Scegli un'opzione:[/] ";
    
    private static void WriteDialogue(string character, string kind, string characterName, string dialogue, bool liveWriting = true)
    {
        if (String.IsNullOrEmpty(dialogue)) return;
        //sceglie il colore in base a quale personaggio parla.
        var color = character switch
        {
            "main"     => ColorMainCharacter,
            "helper"   => ColorHelperCharacter,
            "enemy"    => ColorEnemyCharacter,
            "intro"    => ColorIntroCharacter,
            _          => ColorNarratorCharacter
        };

        var rules = kind switch
        {
            "dialogue" => "",
            "narration" => "italic",
            "thought" => "italic",
            _ => ""
        };

        //Se le regole di formattazione contengono la parola "italic", allora stamperà un * all'inizio e alla fine
        var thought = rules.Contains("italic")? "*" : "";
        
        //rimpiazza i placeholder con i veri nomi.
        var finalCharName = ReplacePlaceholders(characterName);
        var finalDialogue = ReplacePlaceholders(dialogue);

        //Stampa il nome indipendentemente se c'è il liveWriting o no.
        AnsiConsole.Markup($"[{color}][[{finalCharName}]][/]: ");
        
        if (liveWriting)
        {
            LiveWriting(finalDialogue, thought, rules);
            return;
        }
        AnsiConsole.Markup($"[{rules}]{thought}{finalDialogue}{thought}[/]\n");
    }

    private static void LiveWriting(string dialogue, string thought, string rules = "")
    {
        // eventuale prefisso (ad es. * se pensiero)
        if (!string.IsNullOrEmpty(thought))
            AnsiConsole.Markup($"[{rules}]{thought}[/]");

        int cursor = 0;
        foreach (Match block in TagRegex.Matches(dialogue))
        {
            // 1) testo normale PRIMA del tag → typewriter
            if (block.Index > cursor)
            {
                string plain = dialogue.Substring(cursor, block.Index - cursor);
                WritePlainSegment(plain, rules);
            }

            // 2) il tag completo (es. [bold]CRYOFALL![/]) → in un colpo solo
            AnsiConsole.Markup(block.Value);

            cursor = block.Index + block.Length;
        }

        // 3) testo NORMALE dopo l’ultimo tag
        if (cursor < dialogue.Length)
        {
            string tail = dialogue.Substring(cursor);
            WritePlainSegment(tail, rules);
        }

        // eventuale suffisso
        if (!string.IsNullOrEmpty(thought))
            AnsiConsole.Markup($"[{rules}]{thought}[/]");

        AnsiConsole.WriteLine();          // newline finale
    }

    private static void WritePlainSegment(string segment, string rules)
    {
        foreach (char c in segment)
        {
            // se è testo "normale" e NON c'è alcuna regola -> Write più veloce
            if (string.IsNullOrEmpty(rules))
            {
                AnsiConsole.Write(c);
            }
            else
            {
                // applica lo stile (italic, ecc.) al singolo carattere
                AnsiConsole.Markup($"[{rules}]{Markup.Escape(c.ToString())}[/]");
            }
            Thread.Sleep(20);
        }
    }



    private static void AskPlayerHisName(string dialogue)
    {
        var inputName = AnsiConsole.Ask<string>(dialogue);
        SetPlaceholder("playerName",inputName);
    }

    public static string GetPlaceHolders(string name)
    {
        var result = Vars.GetValueOrDefault(name);
        return String.IsNullOrEmpty(result)? "" : result;
    }
    
    /// <summary>
    /// La funzione serve a far partire un dialogo.
    /// All'interno del file json, c'è un campo chiamato "next", serve a far capire alla funzione se c'è o meno
    /// un dialogo successivo. Ciò permette di ottimizzare la logica del codice e di non dover scrivere una linea di codice per ogni dialogo.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="msToWaitForLine"></param>
    /// <param name="liveWriting"></param>
    public static void StartDialogue(string id, int msToWaitForLine = 1000, bool liveWriting = true)
    {
        if (!Repo.TryGet(id, out var current))
        {
            Console.WriteLine($"[ERRORE] ID '{id}' non trovato nei dialoghi");
            return;
        }

        while (current is not null) //se il current.next non è null, allora continua a ciclare stampando i messaggi.
        {
            switch (current.Action)
            {
                case "inputName": AskPlayerHisName(current.Text); break;
                default: WriteDialogue(current.Character, current.Kind, current.SpeakerName,current.Text, liveWriting: liveWriting); break;
            }
            
    
            //Se c'è una scelta, allora stampa il menu selezionabile.
            if (current.Choices is { Count: > 0 })
            {
                int pick = ShowMenu(current.Choices);
                current  = Repo.Get(current.Choices[pick].Next);
            }
            else if (!string.IsNullOrEmpty(current.Next)) //Altrimenti, se non c'è. Stampa semplicemente il prossimo dialogo.
            {
                current = Repo.Get(current.Next);
                
            }
            else //se non esiste un prossimo dialogo, finisce e prosegue con il gioco.
            {
                current = null; // --> fine sequenza per questo dialogo.
            }
            Thread.Sleep(msToWaitForLine); //aspetta x tempo tra un dialogo e l'altro.
        }
    }

    /// <summary>
    /// Rimpiazza tutti i placeholder con i nomi e con gli oggetti.
    /// </summary>
    private static string ReplacePlaceholders(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return raw;

        // sostituiamo ogni <chiave> con il valore escapato
        return PhRegex.Replace(raw, m =>
        {
            string key = m.Groups["key"].Value;

            if (Vars.TryGetValue(key, out var val))
                return Markup.Escape(val);   // protegge solo il valore

            return m.Value;                  // se chiave mancante, lascia <chiave>
        });
    }
    
    /// <summary>
    /// Salva il nuovo placeholder, temporaneamente.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public static void SetPlaceholder(string key, string value)
    {
        Vars[key] = value;
    }


    /// <summary>
    /// Mostrerà il menu, permettendo al giocatore di scegliere
    /// un'opzione, in base a essa, ritornerà un indice.
    /// Quest'ultimo permetterà di passare al prossimo dialogo.
    /// </summary>
    private static int ShowMenu(IReadOnlyList<Choice> choices)
    {
        //Creo la selezione
        var prompt = new SelectionPrompt<string>()
            .Title(ChooseAnOptionTitle)
            .AddChoices(choices.Select(c => ReplacePlaceholders(c.Label)));

        //stampo la selezione
        string selected = AnsiConsole.Prompt(prompt);

        // Ottengo il numero della scelta in base a ciò che è stato scelto, così da riportarlo e far proseguire il testo.
        int index = choices
            .Select((c, i) => new {c.Label, Index = i})
            .First(t => t.Label == selected)
            .Index;

        return index;
    }
}