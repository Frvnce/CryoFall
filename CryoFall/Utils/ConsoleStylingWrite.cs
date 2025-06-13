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
    
    //Colori vari personaggi.
    private static readonly string ColorMainCharacter = "#12e6bb";
    private static readonly string ColorHelperCharacter = "#05a13b";
    private static readonly string ColorEnemyCharacter = "#9e2416";
    private static readonly string ColorNarratorCharacter = "#dedddc";
    
    //Vari testi preSalvati.
    private static readonly string ChooseAnOptionTitle = "[bold #f1f1f1]Scegli un'opzione:[/] ";
    
    private static void WriteDialogue(string character, string characterName, string dialogue, bool liveWriting = true)
    {
        //sceglie il colore in base a quale personaggio parla.
        string color = character switch
        {
            "main"     => ColorMainCharacter,
            "helper"   => ColorHelperCharacter,
            "enemy"    => ColorEnemyCharacter,
            _          => ColorNarratorCharacter
        };
        
        //rimpiazza i placeholder con i veri nomi.
        var finalCharName = ReplacePlaceholders(characterName);
        var finalDialogue = ReplacePlaceholders(dialogue);
        
        //Stampa il nome indipendentemente se c'è il liveWriting o no.
        AnsiConsole.Markup($"[{color}][[{finalCharName}]][/]: ");
        if (liveWriting)
        {
            LiveWriting(finalDialogue);
            return;
        }
        AnsiConsole.Markup($"{finalDialogue}\n");
    }

    private static void LiveWriting(string dialogue) //Stampa aspettando x millisecondi tra una lettera e l'altra.
    {
        foreach (var k in dialogue)
        {
            AnsiConsole.Markup($"{k}");
            Thread.Sleep(20);
        }
        AnsiConsole.Markup("\n");
    }

    /// <summary>
    /// La funzione serve a far partire un dialogo.
    /// All'interno del file json, c'è un campo chiamato "next", serve a far capire alla funzione se c'è o meno
    /// un dialogo successivo. Ciò permette di ottimizzare la logica del codice e di non dover scrivere una linea di codice per ogni dialogo.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="msToWaitForLine"></param>
    public static void StartDialogue(string id, int msToWaitForLine = 1000)
    {
        if (!Repo.TryGet(id, out var current))
        {
            Console.WriteLine($"[ERRORE] ID '{id}' non trovato nei dialoghi");
            return;
        }

        while (current is not null) //se il current.next non è null, allora continua a ciclare stampando i messaggi.
        {
            WriteDialogue(current.Character,current.SpeakerName,current.Text);
    
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

        string replaced = PhRegex.Replace(raw, m =>
        {
            string key = m.Groups["key"].Value;
            return Vars.TryGetValue(key, out var val) ? val : m.Value; // se manca, lascia <key>
        });

        // Protegge da caratteri di markup Spectre ([ ])
        return Markup.Escape(replaced);
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