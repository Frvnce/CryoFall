using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CryoFall.Character;
using CryoFall.Commands;
using CryoFall.Items;
using CryoFall.Logging;
using CryoFall.Rooms;
using Spectre.Console;
namespace CryoFall.Dialogue;

/// <summary>
/// Classe statica che gestisce TUTTE le operazioni di stampa su console con <c>Spectre.Console</c>.
/// Responsabilità principali:
/// <list type="bullet">
///   <item>Riproduzione dei dialoghi con effetto «macchina da scrivere».</item>
///   <item>Evidenziazione dei nomi dei personaggi con colori predefiniti.</item>
///   <item>Messaggi d’aiuto e tutorial in stile differenziato.</item>
///   <item>Sostituzione a runtime dei <c>&lt;placeholder&gt;</c> (es. &lt;playerName&gt;).</item>
///   <item>Visualizzazione di menù di scelta interattivi.</item>
/// </list>
/// Tutti i membri pubblici sono documentati in italiano secondo le indicazioni del docente.
/// </summary>
public static class ConsoleStylingWrite
{
    /// <summary>Repository che contiene i dialoghi caricati da <c>dialogues.json</c>.</summary>
    private static readonly DialogueRepository RepoDialogue = DialogueRepository.Load();
    
    /// <summary>Dizionario (mutabile) dei placeholder attivi durante la sessione di gioco.</summary>
    private static Dictionary<string, string> PlaceHoldersNames = CharacterRepository.PlaceholdersNames;
    
    /// <summary>Restituisce una copia (read‑only) del dizionario dei placeholder.</summary>
    public static Dictionary<string,string> GetPlaceholdersDict() =>
        new(PlaceHoldersNames);
    
    /// <summary>Sostituisce in blocco il dizionario dei placeholder.</summary>
    public static void SetPlaceholdersDict(Dictionary<string,string> dict) =>
        PlaceHoldersNames = new(dict);

    
    /// <summary>
    /// Espressione regolare per identificare i segnaposto racchiusi tra < e >, 
    /// es. <playerName>. Cattura il contenuto all'interno del tag con il gruppo "key".
    /// </summary>
    private static readonly Regex PhRegex = 
        new(@"<(?<key>[^>]+)>", RegexOptions.Compiled);

    /// <summary>
    /// Espressione regolare per trovare blocchi di testo racchiusi tra tag personalizzati 
    /// con la sintassi [qualcosa]...[/]. Esempio: [rosso]Testo[/].
    /// Supporta multilinea.
    /// </summary>
    private static readonly Regex TagRegex = 
        new(@"\[[^\]]+\].*?\[\/\]", RegexOptions.Compiled | RegexOptions.Singleline);

    //Colori vari personaggi.
    private static readonly string ColorMainCharacter = "#12e6bb bold";
    private static readonly string ColorHelperCharacter = "#05a13b bold";
    private static readonly string ColorEnemyCharacter = "#9e2416 bold";
    private static readonly string ColorNarratorCharacter = "#dedddc bold italic";
    private static readonly string ColorIntroCharacter = "#dedddc bold italic";
    private static readonly string ColorFriendCharacter = "#ffea99 bold";
    private static readonly string ColorMalfunctioningCharacter = "#ff7e42 bold";
    
    //Vari testi preSalvati.
    private static readonly string ChooseAnOptionTitle = "[bold #f1f1f1]Scegli un'opzione:[/] ";

    /// <summary>
    /// Stampa le caratteristiche dell'item passato come parametro.
    /// </summary>
    /// <param name="item">Da analizzare</param>
    public static void AnalyzeItem(Item item)
    {
        var nameItem = item.Name;
        var color = item.Color;
        var description = item.Description;
        var weight = item.Weight;
        
        HelperCmd("Analizzo l'oggetto...",true);
        HelperCmd($"[{color}]{nameItem}[/], {description} Pesa {weight}kg!");
    }
    
    /// <summary>
    /// Descrive dettagliatamente la stanza corrente (prima visita): descrizione, uscite e oggetti.
    /// </summary>
    public static void AnalyzeRoom(Room room)
    {
        var nameRoom = room.NameOfTheRoom;
        var descriptionRoom = room.DescriptionOfTheRoom;
        var listOfItems = room.GetItems();
        
        HelperCmd("Analizzo la stanza...",true);
        HelperCmd($"Ci troviamo in [bold]{nameRoom}[/], {descriptionRoom}",true);

        HelperCmd($"Vedo alcune porte...\n" +
                  $"{(room.NearRooms.NordRoom!=null? $"   [bold][[{room.NearRooms.NordRoom.NameOfTheRoom}]][/] a [bold]Nord![/]\n" :"")}" +
                  $"{(room.NearRooms.SudRoom!=null? $"   [bold][[{room.NearRooms.SudRoom.NameOfTheRoom}]][/] a [bold]Sud![/]\n" :"")}" +
                  $"{(room.NearRooms.EstRoom!=null? $"   [bold][[{room.NearRooms.EstRoom.NameOfTheRoom}]][/] a [bold]Est![/]\n" :"")}" +
                  $"{(room.NearRooms.OvestRoom!=null? $"   [bold][[{room.NearRooms.OvestRoom.NameOfTheRoom}]][/] a [bold]Ovest![/]" :"")}", true);

        if (room.GetItems().Count == 0) return;
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
    /// <summary>Versione semplificata per stanze già visitate: mostra solo le uscite.</summary>
    public static void AnalyzeRoomVisited(Room room)
    {
        var nameRoom = room.NameOfTheRoom;
        HelperCmd($"Ci troviamo in [bold]{nameRoom}[/]",true);
        HelperCmd($"Vedo alcune porte...\n" +
                  $"{(room.NearRooms.NordRoom!=null? $"   [bold][[{room.NearRooms.NordRoom.NameOfTheRoom}]][/] a [bold]Nord![/]\n" :"")}" +
                  $"{(room.NearRooms.SudRoom!=null? $"   [bold][[{room.NearRooms.SudRoom.NameOfTheRoom}]][/] a [bold]Sud![/]\n" :"")}" +
                  $"{(room.NearRooms.EstRoom!=null? $"   [bold][[{room.NearRooms.EstRoom.NameOfTheRoom}]][/] a [bold]Est![/]\n" :"")}" +
                  $"{(room.NearRooms.OvestRoom!=null? $"   [bold][[{room.NearRooms.OvestRoom.NameOfTheRoom}]][/] a [bold]Ovest![/]" :"")}", true);
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
    /// <summary>Messaggio dell'«assistente» (colore verde) facoltativamente con effetto macchina da scrivere.</summary>
    public static void HelperCmd(string dialogue, bool live=false)
    {
        WriteDialogue("helper","help","<assistant>",dialogue,live);
    }
    /// <summary>Messaggio giallo mostrato durante il tutorial.</summary>
    public static void WriteTutorial(string dialogue)
    {
        var name = "[bold][[[yellow]Tutorial[/]]]:[/]";
        AnsiConsole.MarkupLine($"{name} {dialogue}");
    }
    
    /// <summary>
    /// Scrive una linea di dialogo applicando colore, regole di stile ed eventualmente l'effetto macchina da scrivere.
    /// </summary>
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
            "friend"   => ColorFriendCharacter,
            "malfunctioning" => ColorMalfunctioningCharacter,
            _          => ColorNarratorCharacter
        };

        //Il tipo di dialogo, se è narrativo, allora il testo verrà impostato con lo stile italic, idem per Thought(Pensiero del giocatore)
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
        
        //Se liveWriting è attivo, stamperà le parole lettera per lettera.
        if (liveWriting)
        {
            LiveWriting(finalDialogue, thought, rules);
            return;
        }
        //altrimenti le stamperà in modo normale.
        AnsiConsole.Markup($"[{rules}]{thought}{finalDialogue}{thought}[/]\n");
    }
    /// <summary>
    /// Riproduce il testo con effetto macchina da scrivere, preservando correttamente i tag Spectre.Console.
    /// </summary>
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

    /// <summary>Stampa un segmento privo di tag, un carattere alla volta.</summary>
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
                // applica lo stile (italic, ecc.)
                AnsiConsole.Markup($"[{rules}]{Markup.Escape(c.ToString())}[/]");
            }
            Thread.Sleep(20); //Tempo tra un carattere e l'altro.
        }
    }


    /// <summary>
    /// Chiede al giocatore di rimpiazzare il placeholder con un nome.
    /// Utilizzato per scegliere il nome del giocatore e il nome dell'assistente.
    /// </summary>
    /// <param name="placeHolder"></param>
    /// <param name="dialogue"></param>
    private static void AskPlayerPlaceHolders(string placeHolder, string dialogue)
    {
        var inputName = AnsiConsole.Ask<string>(dialogue);
        SetPlaceholder(placeHolder,inputName);
    }

    /// <summary>
    /// Cerca di ottenere il risultato del PlaceHolder.
    /// </summary>
    /// <param name="placeHolder">Il PlaceHolder che contiene la parola che si vuole ottenere</param>
    /// <returns>Il corrispettivo del placeholder richiesto.</returns>
    public static string GetPlaceHolders(string placeHolder)
    {
        var result = PlaceHoldersNames.GetValueOrDefault(placeHolder);
        return String.IsNullOrEmpty(result)? "" : result;
    }
    
    /// <summary>
    /// La funzione serve a far partire un dialogo.
    /// All'interno del file json, c'è un campo chiamato "next", serve a far capire alla funzione se c'è o meno
    /// un dialogo successivo. Ciò permette di ottimizzare la logica del codice e di non dover scrivere una linea di codice per ogni dialogo.
    /// </summary>
    /// <param name="id">Id del dialogo</param>
    /// <param name="player"></param>
    /// <param name="msToWaitForLine">Tempo di attesa tra un dialogo e l'altro, di default 500ms</param>
    /// <param name="liveWriting">Scegliere se mostrare il dialogo lettera per lettera o no.</param>
    /// <param name="loadLastDialogue">Scegliere se caricare l'ultimo dialogo quando si carica una partita</param>
    public static void StartDialogue(string id, MainCharacter? player = null, RoomsManager? rm = null, int msToWaitForLine = 500, bool liveWriting = true, bool loadLastDialogue = false)
    {
        if (!RepoDialogue.TryGet(id, out var current))
        {
            Console.WriteLine($"[ERRORE] ID '{id}' non trovato nei dialoghi");
            return;
        }
        if (player != null && !id.Equals("waitForCmd", StringComparison.OrdinalIgnoreCase) && !id.Equals("end",StringComparison.OrdinalIgnoreCase))
        {
            player.LastDialogueId = id;
        }

        // Fa partire l'ultimo dialogo SOLO se il player è in quella stanza
        /*if (loadLastDialogue)
        {
            if (!current.Room.Equals(player.CurrentRoom.Id)) return;
        }*/
        
        while (current is not null) //se il current.next non è null, allora continua a ciclare stampando i messaggi.
        {
            Logger.Log($"Parte un dialogo: {current.Id}, {ReplacePlaceholders(current.SpeakerName)}.");
            switch (current.Action)
            {
                case "inputName": AskPlayerPlaceHolders("playerName",current.Text); break;
                case "inputNameAssistente": AskPlayerPlaceHolders("assistant",current.Text); break;
                case "pullLever": OpenDoor("zona_carburante_nord",rm); break;
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
    /// Apre la porta tramite la scelta dal dialogo.
    /// </summary>
    /// <param name="nameOfTheRoom"></param>
    /// <param name="rm"></param>
    private static void OpenDoor(string nameOfTheRoom, RoomsManager rm)
    {
        var room = rm.FindRoom(nameOfTheRoom);
        room.IsLocked = false;
    }


    /// <summary>Sostituisce tutti i placeholder presenti in <paramref name="raw"/>.</summary>
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
    private static void SetPlaceholder(string key, string value)
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
        int index = display.IndexOf(selected);   // sempre >=0 perché arriva dal Prompt
        
        return index;
    }

}