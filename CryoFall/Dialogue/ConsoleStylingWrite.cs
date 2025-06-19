using System.Text.RegularExpressions;
using CryoFall.Character;
using CryoFall.Commands;
using CryoFall.Rooms;
using Spectre.Console;
namespace CryoFall.Dialogue;

public static class ConsoleStylingWrite
{
    //Ottenere il file dei dialoghi.
    private static readonly DialogueRepository RepoDialogue = DialogueRepository.Load();
    
    private static Dictionary<string, string> PlaceHoldersNames = CharacterRepository.PlaceholdersNames;
    
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

    public static void AnalyzeRoom(Room room)
    {
        var nameRoom = room.NameOfTheRoom;
        var descriptionRoom = room.DescriptionOfTheRoom;
        var listOfItems = room.GetItems();
        
        HelperCmd("Analizzo la stanza...",true);
        HelperCmd($"Ci troviamo in [bold]{nameRoom}[/], {descriptionRoom}",true);

        HelperCmd($"Vedo alcune porte...\n   " +
                  $"{(room.NearRooms.UpperRoom!=null? $"[[{room.NearRooms.UpperRoom.NameOfTheRoom}]] a [bold]Nord![/]" :"")}" +
                  $"{(room.NearRooms.LowerRoom!=null? $"[[{room.NearRooms.LowerRoom.NameOfTheRoom}]] a [bold]Sud![/]" :"")}" +
                  $"{(room.NearRooms.LeftRoom!=null? $"[[{room.NearRooms.LeftRoom.NameOfTheRoom}]] a [bold]Est![/]" :"")}" +
                  $"{(room.NearRooms.RightRoom!=null? $"[[{room.NearRooms.RightRoom.NameOfTheRoom}]] a [bold]Ovest![/]" :"")}", true);
        
        HelperCmd("Vedo alcuni oggetti interessanti: ",true);
        Random rdm = new Random();
        var list = GetList();
        foreach (var item in listOfItems)
        {
            var number = rdm.Next(0, list.Count);
            var randomString = list[number];
            AnsiConsole.MarkupLine($"  [{item.Color}]{item.Name}[/]{randomString}");
        }
    }

    private static List<string> GetList()
    {
        var list = new List<string>();
        list.Add(", sembra utile, lo prendiamo?");
        list.Add(", non so, può tornarci utile?");
        list.Add(", carino.");
        list.Add(", sembra inutile, tu che dici?");
        list.Add(", wooooow, prendiamolo dai!");
        list.Add(", mmmh, che strano");
        list.Add(", fantastico, ne avevo uno simile a casa");
        list.Add(", il mio ultimo padrone ci è morto...");

        return list;
    }
    
    public static void HelperCmd(string dialogue, bool live=false)
    {
        WriteDialogue("helper","help","<assistant>",dialogue,live);
    }

    public static void WriteCmdHelp(CommandInfo cmd)
    {
        
        //WriteDialogue("helper","help",$"{cmd.Name}",$"{cmd.Description} \n \t[bold]Comando:[/] [bold]{cmd.Cmd}[/]\n",false);
        WriteDialogue("helper","help",$"{cmd.Cmd}",$"{cmd.Description}",false);
        
    }
    
    
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



    private static void AskPlayerPlaceHolders(string placeHolder, string dialogue)
    {
        var inputName = AnsiConsole.Ask<string>(dialogue);
        SetPlaceholder(placeHolder,inputName);
    }

    public static string GetPlaceHolders(string name)
    {
        var result = PlaceHoldersNames.GetValueOrDefault(name);
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
        if (!RepoDialogue.TryGet(id, out var current))
        {
            Console.WriteLine($"[ERRORE] ID '{id}' non trovato nei dialoghi");
            return;
        }

        while (current is not null) //se il current.next non è null, allora continua a ciclare stampando i messaggi.
        {
            switch (current.Action)
            {
                case "inputName": AskPlayerPlaceHolders("playerName",current.Text); break;
                case "inputNameAssistente": AskPlayerPlaceHolders("assistant",current.Text); break;
                default: WriteDialogue(current.Character, current.Kind, current.SpeakerName,current.Text, liveWriting: liveWriting); break;
            }
            
    
            //Se c'è una scelta, allora stampa il menu selezionabile.
            if (current.Choices is { Count: > 0 })
            {
                int pick = ShowMenu(current.Choices);
                current  = RepoDialogue.Get(current.Choices[pick].Next);
            }
            else if (!string.IsNullOrEmpty(current.Next)) //Altrimenti, se non c'è. Stampa semplicemente il prossimo dialogo.
            {
                current = RepoDialogue.Get(current.Next);
                
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

            if (PlaceHoldersNames.TryGetValue(key, out var val))
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
        PlaceHoldersNames[key] = value;
    }


    /// <summary>
    /// Mostrerà il menu, permettendo al giocatore di scegliere
    /// un'opzione, in base a essa, ritornerà un indice.
    /// Quest'ultimo permetterà di passare al prossimo dialogo.
    /// </summary>
    private static int ShowMenu(IReadOnlyList<Choice> choices)
    {
        // 1. Label “visibili” dopo ReplacePlaceholders
        var display = choices
            .Select(c => ReplacePlaceholders(c.Label))
            .ToList();                         // indice i ↔ choices[i]

        // 2. Prompt Spectre
        var prompt = new SelectionPrompt<string>()
            .Title(ChooseAnOptionTitle)
            .AddChoices(display);

        string selected = AnsiConsole.Prompt(prompt);

        // 3. Ricava l’indice dalla lista display
        int index = display.IndexOf(selected);   // sempre ≥0 perché arriva dal Prompt
        
        return index;
    }

}