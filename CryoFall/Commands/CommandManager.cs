using CryoFall.Character;
using CryoFall.Dialogue;
using CryoFall.Items;
using CryoFall.Logging;
using CryoFall.Rooms;
using CryoFall.SaveSystem;
using Spectre.Console;

namespace CryoFall.Commands;

/// <summary>
/// Gestisce il parsing e l'esecuzione dei comandi di testo inseriti dal giocatore.
/// Tutta la logica di alto livello («help», movimento, inventario, salvataggi...) passa da qui.
/// Ogni metodo pubblico/privato è stato documentato in italiano secondo le linee guida del professore.
/// </summary>
public class CommandManager
{
    /// <summary>
    /// Analizza il comando <paramref name="cmd"/>, verifica la sintassi e invoca il metodo corrispondente.
    /// </summary>
    /// <remarks>
    /// I comandi sono case-insensitive; eventuali argomenti multipli (es. «analizza Sala Ibernazione»)
    /// vengono normalizzati da <see cref="SplitCmd"/> così da eliminare gli spazi interni.
    /// </remarks>
    /// <param name="cmd">Stringa digitata dal player.</param>
    /// <param name="player">Istanza del <see cref="MainCharacter"/>.</param>
    /// <param name="roomsManager">Gestore stanze runtime.</param>
    /// <param name="itemsManager">Gestore item runtime.</param>
    /// <returns>
    /// <c>true</c> se il comando è valido ed è stato eseguito correttamente; <c>false</c> in caso di errore.
    /// </returns>
    public bool ReadCommand(string cmd,MainCharacter player,RoomsManager roomsManager,ItemsManager itemsManager)
    {
        var args = cmd.Split(' ');
        if (args.Length >= 2)
        {
            args = SplitCmd(args);
            args[1] = args[1].ToLower();
        }
        
        switch (args[0].ToLower()) //Switch per tutti i comandi presenti nel gioco, alcuni di essi hanno alias.
        {
            case "prendi":
                if (args.Length != 2) return ErrorCmd();
                return TryPickUpItem(args[1], player, roomsManager, itemsManager);
            case "help":
                return Help(player, itemsManager);
            case "teletrasporta":
            case "teletrasporto":
            case "tp":
                if (!player.Inventory.Items.Contains(itemsManager.FindItem("dispositivo_di_teletrasporto"))) return ErrorCmd();
                return Teleport(player,roomsManager); //Tp
            case "muoviti":
            case"move":
                if (args.Length != 2) return ErrorCmd();
                return Move(args[1],player,roomsManager,itemsManager); //Move
            case "analizza":
                if(args.Length==1)return AnalyzeRoom(player, roomsManager);
                if (args.Length != 2) return ErrorCmd();
                return AnalyzeItem(args[1],player,itemsManager);
            case "lascia":
                return RemoveTopItem(player);
            case "inventario":
            case "inv":
            case "inventory":
                return VisualizeInventory(player);
            case "usa":
                if (args.Length != 2) return ErrorCmd();
                return UseObject(args[1],player,roomsManager,itemsManager);
            case "salva":
                SaveGame(player,roomsManager,itemsManager);
                return true;
            case "carica":
                if(args.Length != 2) return ErrorCmd();
                return LoadGame(player, roomsManager, itemsManager, args[1]);
            case "mappa":
                return VisualizeMap(player,itemsManager);

            default: return ErrorCmd(); //Se il comando inviato non compare qui, da errore e dice al giocatore che ha sbagliato e che dovrebbe fare "help"
        }
    }

    bool VisualizeMap(MainCharacter player, ItemsManager im)
    {
        if (!player.Inventory.Items.Contains(im.FindItem("mappa_olografica"))) return ErrorCmd("mapNotFound");
        ConsoleStylingWrite.VisualizeMap(player);
        return true;
    }
    
    /// <summary>
    /// Carica un salvataggio dal percorso ‹saves/&lt;savePath&gt;.json›.
    /// </summary>
    bool LoadGame(MainCharacter player, RoomsManager roomsManager, ItemsManager itemsManager, string savePath)
    {
        //Otteniamo il path dove sono salvati i files.
        var path = Path.Combine(AppContext.BaseDirectory, "saves", $"{savePath}.json");
        //Se la cartella non esiste, allora errore
        if (!File.Exists(path)) return ErrorCmd("FileNotFound");
        //chiamiamo la funzione Load e carichiamo tutti i dati.
        SaveManager.Load(path, player, roomsManager, itemsManager);
        ConsoleStylingWrite.HelperCmd("Partita caricata da " + path);
        Thread.Sleep(1000);
        AnsiConsole.Clear(); //Liberiamo la console così da pulire la vecchia partita.
        Logger.Log($"Partita caricata da {path}");
        return true;
    }
    
    /// <summary>
    /// Genera un nuovo file «saveXX.json» incrementale e salva lo stato attuale.
    /// </summary>
    void SaveGame(MainCharacter player, RoomsManager roomsManager, ItemsManager itemsManager) 
    {
        // 1. Cartella di destinazione
        string savesDir = Path.Combine(AppContext.BaseDirectory, "saves");
        Directory.CreateDirectory(savesDir);

        // 2. Trova il prossimo indice libero -----------------------------------
        //    Cerca tutti i file "save**.json", estrae il numero e calcola max+1
        int nextIndex = 1;

        var files = Directory.GetFiles(savesDir, "save*.json");
        if (files.Length > 0)
        {
            // "save01.json" → "01" → 1
            nextIndex = files
                .Select(f => Path.GetFileNameWithoutExtension(f).Substring(4)) // rimuove "save"
                .Select(s => int.TryParse(s, out int n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;
        }

        // 3. Formatta l’indice con due cifre (01, 02, … 99) --------------------
        string fileName = $"save{nextIndex:00}.json"; //Se vogliamo si può impostare 000, se vogliamo più salvataggi (001-999)
        string path = Path.Combine(savesDir, fileName);

        // 4. Salva e avvisa l’utente -------------------------------------------
        SaveManager.Save(path, player, roomsManager, itemsManager);
        ConsoleStylingWrite.HelperCmd($"Partita salvata in {path}");
        Logger.Log($"Partita salvata in {path}");
    }
    
    /// <summary>
    /// Fa un lavoro dove imposta il secondo argomento unendo tutti gli argomenti dopo il primo.
    /// In modo tale, il player può scrivere "analizza ChiaveMagnetica Livello 5" e il risultato sarà
    /// "analizza chiavemagneticalivello5", in modo tale da trovare l'oggetto.
    /// </summary>
    /// <param name="args">Il cmd splittato.</param>
    /// <returns></returns>
    private string[] SplitCmd(string[] args)
    {
        string[] newArgs = new string[2];
        newArgs[0] = args[0];
        for (int i = 1; i < args.Length; i++)
        {
            newArgs[1] += args[i];
        }
        return newArgs;
    }
    
    /// <summary>
    /// Prova a utilizzare l'oggetto in cima all'inventario per aprire una porta in <paramref name="direction"/>.
    /// </summary>
    private bool UseObject(string direction, MainCharacter p, RoomsManager rm,ItemsManager im)
    {
        if (!p.Inventory.GetFirstItem().IsUsable) return ErrorCmd("itemNotUsable");
        if(p.Inventory.Items.Count==0) return ErrorCmd("noItemsInInventory");
        Room finalDestination;
        switch (direction.ToLower())
        {
            case "nord":
                if (p.CurrentRoom.NearRooms.NordRoom == null) return ErrorCmd("roomDoesNotExist");
                if (!p.CurrentRoom.NearRooms.NordRoom.IsLocked) return ErrorCmd("roomIsNotLocked");
                finalDestination = p.CurrentRoom.NearRooms.NordRoom;
                break;
            case "sud":
                if (p.CurrentRoom.NearRooms.SudRoom == null) return ErrorCmd("roomDoesNotExist");
                if (!p.CurrentRoom.NearRooms.SudRoom.IsLocked) return ErrorCmd("roomIsNotLocked");
                finalDestination = p.CurrentRoom.NearRooms.SudRoom;
                break;
            case "ovest":
                if (p.CurrentRoom.NearRooms.OvestRoom == null) return ErrorCmd("roomDoesNotExist");
                if (!p.CurrentRoom.NearRooms.OvestRoom.IsLocked) return ErrorCmd("roomIsNotLocked");
                finalDestination = p.CurrentRoom.NearRooms.OvestRoom;
                break;
            case "est":
                if (p.CurrentRoom.NearRooms.EstRoom == null) return ErrorCmd("roomDoesNotExist");
                if (!p.CurrentRoom.NearRooms.EstRoom.IsLocked) return ErrorCmd("roomIsNotLocked");
                finalDestination = p.CurrentRoom.NearRooms.EstRoom;
                break;
            default: return ErrorCmd("destinationError");
        }
        if(finalDestination.UnlockKeyId.ToLower().Replace("_","")!=p.Inventory.GetFirstItem().Id.ToLower().Replace("_","")) return ErrorCmd("incompatibleKey");
        rm.FindRoom(finalDestination.Id).IsLocked = false;
        ConsoleStylingWrite.HelperCmd($"[{p.Inventory.GetFirstItem().Color}]{p.Inventory.GetFirstItem().Name}[/] ha aperto la stanza [bold]{finalDestination.NameOfTheRoom}[/], ora puoi entrarci!");
        if (p.Inventory.GetFirstItem().Equals(im.FindItem("chiave_magnetica_livello_10")))
        {
            p.Inventory.GetFirstItem().IsUsable = false;
            ConsoleStylingWrite.HelperCmd($"Oh cavolo... credo che [{p.Inventory.GetFirstItem().Color}]{p.Inventory.GetFirstItem().Name}[/] si sia smagnetizzata... Penso che ora sia inutile.");
        }
        Logger.Log($"Il giocatore sta utilizzando {p.Inventory.GetFirstItem().Name} su {finalDestination.NameOfTheRoom}");
        return true;
    }
    
    /// <summary>Mostra l'inventario con capacità, items e descrizioni.</summary>
    private bool VisualizeInventory(MainCharacter p)
    {  
        ConsoleStylingWrite.HelperCmd($"Ecco il tuo inventario: Capacità: [#22ff00]{p.Inventory.CurrentLoad}[/]/[#22ff00]{p.Inventory.MaxCapacity}[/] kg");
        foreach (var item in p.Inventory.Items)
        {
            AnsiConsole.MarkupLine($"   [{item.Color}]{item.Name}[/]: {item.Description}");
        }
        ConsoleStylingWrite.HelperCmd($"Ricorda, puoi utilizzare solo l'oggetto che hai in cima alla lista!");
        Logger.Log("Il giocatore sta visualizzando il suo inventario");
        return true;
    }
    
    /// <summary>
    /// Lascia l'oggetto in cima all'inventario nella stanza corrente.
    /// </summary>
    private bool RemoveTopItem(MainCharacter pl)
    {
        ConsoleStylingWrite.HelperCmd($"Beh, immagino che non ci servirà.");
        return pl.Inventory.DropTop(pl.CurrentRoom);;
    }

    /// <summary>
    /// Restituisce una lista di stanze che sono disponibili per il teletrasporto casuale.
    /// </summary>
    /// <param name="rm"></param>
    /// <returns></returns>
    List<Room> GetListRooms(RoomsManager rm)
    {
        List<Room> rooms = new List<Room>();
        rooms.Add(rm.FindRoom("corridoio_ovest_1"));
        rooms.Add(rm.FindRoom("corridoio_ovest_2"));
        rooms.Add(rm.FindRoom("corridoio_ovest_3"));
        rooms.Add(rm.FindRoom("corridoio_ovest_4"));
        rooms.Add(rm.FindRoom("stanza_tecnica"));
        rooms.Add(rm.FindRoom("zona_carburante_sud"));
        rooms.Add(rm.FindRoom("corridoio_nord"));
        rooms.Add(rm.FindRoom("corridoio_est"));
        return rooms;
    }
    
    /// <summary>
    /// Teletrasporta il giocatore in una stanza in modo casuale.
    /// </summary>
    /// <returns>
    /// <c>True</c> se il comando è valido ed è stato eseguito correttamente;
    /// <c>False</c> altrimenti.
    /// </returns>
    private bool Teleport(MainCharacter p, RoomsManager rm)
    {
        var rooms = GetListRooms(rm);
        var rdm = new Random();
        var room = rooms[rdm.Next(0, rooms.Count)];

        if (room == null) return ErrorCmd("teleportNotWorking"); 
        p.CurrentRoom = room;
        
        Logger.Log($"Il giocatore si è teletrasportato nella stanza: Name: {room.NameOfTheRoom} ID: {room.Id}");
        if (!p.VisitedRoomIds.Contains(p.CurrentRoom.Id))
        {
            return AnalyzeRoom(p,rm);
        }
        return AnalyzeRoomVisited(p.CurrentRoom.Id, p, rm);
    }

    /// <summary>
    /// Muove il giocatore nella direzione richiesta se la porta non è bloccata.
    /// </summary>
    private bool Move(string destination, MainCharacter p, RoomsManager rm, ItemsManager im)
    {
        Room finalDestination;
        //In base alla direzione data dal giocatore, i 4 punti cardinali, allora analizziamo se la stanza esiste e se è bloccata o meno, fornendo
        // al giocatore una mano, dicendo l'oggetto che serve per sbloccare la porta.
        switch (destination.ToLower())
        {
            case "nord":
                if (p.CurrentRoom.NearRooms.NordRoom == null) return ErrorCmd("roomDoesNotExist");
                if (p.CurrentRoom.NearRooms.NordRoom.IsLocked) return ErrorCmd("roomIsLocked",im.FindItem(p.CurrentRoom.NearRooms.NordRoom.UnlockKeyId));
                finalDestination = p.CurrentRoom.NearRooms.NordRoom;
                break;
            case "sud":
                if (p.CurrentRoom.NearRooms.SudRoom == null) return ErrorCmd("roomDoesNotExist");
                if (p.CurrentRoom.NearRooms.SudRoom.IsLocked) return ErrorCmd("roomIsLocked",im.FindItem(p.CurrentRoom.NearRooms.SudRoom.UnlockKeyId));
                finalDestination = p.CurrentRoom.NearRooms.SudRoom;
                break;
            case "ovest":
                if (p.CurrentRoom.NearRooms.OvestRoom == null) return ErrorCmd("roomDoesNotExist");
                if (p.CurrentRoom.NearRooms.OvestRoom.IsLocked) return ErrorCmd("roomIsLocked",im.FindItem(p.CurrentRoom.NearRooms.OvestRoom.UnlockKeyId));
                finalDestination = p.CurrentRoom.NearRooms.OvestRoom;
                break;
            case "est":
                if (p.CurrentRoom.NearRooms.EstRoom == null) return ErrorCmd("roomDoesNotExist");
                if (p.CurrentRoom.NearRooms.EstRoom.IsLocked) return ErrorCmd("roomIsLocked",im.FindItem(p.CurrentRoom.NearRooms.EstRoom.UnlockKeyId));
                finalDestination = p.CurrentRoom.NearRooms.EstRoom;
                break;
            default: return ErrorCmd("destinationError");
        }
        //Spostiamo il giocatore nella finalDestination.
        p.CurrentRoom = finalDestination;
        ConsoleStylingWrite.HelperCmd("Ok andiamo!");
        Thread.Sleep(500);
        
        Logger.Log($"Il giocatore si è spostato nella stanza: NAME: {finalDestination.NameOfTheRoom} ID: {finalDestination.Id}");
        if (!p.VisitedRoomIds.Contains(p.CurrentRoom.Id))
        {
            return AnalyzeRoom(p,rm);
        }
        return AnalyzeRoomVisited(p.CurrentRoom.Id, p, rm);

    }

    /// <summary>
    ///  Verifica se il comando è valido, se lo è:
    /// Stampa tutti i comandi utilizzabili dal giocatore.
    /// </summary>
    /// <returns>
    /// <c>True</c> se il comando è valido ed è stato eseguito correttamente;
    /// <c>False</c> altrimenti.
    /// </returns>
    private bool Help(MainCharacter p,ItemsManager im)
    {
        ConsoleStylingWrite.HelperCmd($"Questa è la lista di tutti i comandi:");
        Console.WriteLine();
        foreach (var cmd in CommandsRepository.ById.Values)
        {
            
            if (cmd.Id != "tp")
            {
                //stampa tutti i comandi.
                AnsiConsole.MarkupLine($"   [bold #1fc459][[{cmd.Cmd}]][/]: {cmd.Description}");
                continue;
            }
            //se è il comando TP e il giocatore ha il dispositivo di teletrasporto, ALLORA stampa anche quel comando.
            if(cmd.Id == "tp" && p.Inventory.Items.Contains(im.FindItem("dispositivo_di_teletrasporto")))
                AnsiConsole.MarkupLine($"   [bold #1fc459][[{cmd.Cmd}]][/]: {cmd.Description}");
        }
        Logger.Log("Il giocatore ha scritto il comando HELP e sta visualizzando i comandi");
        Console.WriteLine();
        return true;
    }

    /// <summary>
    /// Analizza l'item, mostrando nome, descrizione e peso dell'oggetto.
    /// </summary>
    /// <param name="item">id dell'item</param>
    /// <param name="p"></param>
    /// <param name="im"></param>
    /// <returns></returns>
    private bool AnalyzeItem(string item, MainCharacter p, ItemsManager im)
    {
        var itemToAnalyze = im.FindItem(item); //cerca l'item nella lista di tutti gli items in gioco.
        //se l'item è null, allora riporta errore
        if (itemToAnalyze == null) return ErrorCmd("itemDoesNotExist");
        //se l'item non è nell'inventario, allora da errore.
        if (!p.Inventory.Items.Contains(itemToAnalyze)) return ErrorCmd("itemNotInInventory");
        //se l'item non è analizzabile, allora da errrore
        if (!itemToAnalyze.IsAnalyzable) return ErrorCmd("itemIsNotAnalyzable");
        
        //Stampa le varie info dell'item.
        ConsoleStylingWrite.AnalyzeItem(itemToAnalyze);
            
        return false;
    }

    /// <summary>
    /// Analizza la stanza per intero, mostrando descrizione e item al suo interno.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="rm"></param>
    /// <returns></returns>
    private bool AnalyzeRoom(MainCharacter player,RoomsManager rm)
    {
        var roomToAnalyze = rm.FindRoom(player.CurrentRoom.Id); //Cerca la stanza
        //se è null, allora errore
        if (roomToAnalyze == null) return ErrorCmd("argsError");
        //se la stanza non è la stessa del giocatore, allora riporta errore.
        if(roomToAnalyze.Id.Replace("_","") != player.CurrentRoom.Id.Replace("_","")) return ErrorCmd("notInThisRoom");
        
        //da la descrizione del giocatore.
        ConsoleStylingWrite.AnalyzeRoom(roomToAnalyze);
        Logger.Log($"Il giocatore sta analizzando la stanza: NAME: {roomToAnalyze.NameOfTheRoom} ID: {roomToAnalyze.Id}");
        return true;
    }
    
    /// <summary>
    /// Analizza la stanza mostrando solo il nome, senza la descrizione
    /// Utile al giocatore quando esplora una stanza già visitata in precedenza.
    /// </summary>
    /// <param name="room"></param>
    /// <param name="player"></param>
    /// <param name="rm"></param>
    /// <returns></returns>
    private bool AnalyzeRoomVisited(string room,MainCharacter player,RoomsManager rm)
    {
        var roomToAnalyze = rm.FindRoom(room); //Cerca la stanza
        //se è null, allora errore
        if (roomToAnalyze == null) return ErrorCmd("argsError");
        //se la stanza non è la stessa del giocatore, allora riporta errore.
        if(roomToAnalyze.Id.Replace("_","") != player.CurrentRoom.Id.Replace("_","")) return ErrorCmd("notInThisRoom");

        //da la descrizione del giocatore.
        ConsoleStylingWrite.AnalyzeRoomVisited(roomToAnalyze);
        Logger.Log($"Il giocatore sta analizzando la stanza: NAME: {roomToAnalyze.NameOfTheRoom} ID: {roomToAnalyze.Id}");
        return true;
    }

    /// <summary>
    /// Prova a prendere un item dalla stanza.
    /// </summary>
    /// <param name="item">ID dell'item</param>
    /// <param name="player">Classe player</param>
    /// <param name="rm"></param>
    /// <param name="im"></param>
    /// <returns></returns>
    private bool TryPickUpItem(string item, MainCharacter player, RoomsManager rm, ItemsManager im)
    {
        var itemObject = im.FindItem(item); //Cerca l'item grazie all'ItemsManager.
        // se esiste l'item
        if(itemObject==null)return ErrorCmd("itemNotFound"); 
        //se l'item è nella stanza
        if(!player.CurrentRoom.GetItems().Contains(itemObject))return ErrorCmd("itemNotInRoom");
        //se l'oggetto è raccoglibile
        if(!itemObject.IsPickable)return ErrorCmd("itemNotPickable");
        //se l'oggetto è troppo pesante
        if(player.Inventory.CurrentLoad + itemObject.Weight > player.Inventory.MaxCapacity)return ErrorCmd("inventoryFull"); 
        //prova ad aggiungere l'item nell'inventario del giocatore
        if (!player.Inventory.TryAdd(player.CurrentRoom.TakeItem(item))) return ErrorCmd("failItemAddToInventory");
        Logger.Log($"Il giocatore sta raccogliendo l'oggetto: NOME: {itemObject.Description} ID: {itemObject.Id}");
        AnsiConsole.MarkupLine($"   [italic][bold {itemObject.Color}]{itemObject.Name}[/] aggiunto correttamente al tuo inventario![/]");
        return true;
    }

    /// <summary>
    /// Mostra un errore e invita il giocatore a scrivere HELP per visualizzare i comandi
    /// Inoltre aiuta il giocatore in caso di comandi errati
    /// </summary>
    /// <param name="typeError">Il tipo di errore da mostrare</param>
    /// <param name="item">Item che serve a mostrare il tipo di oggetto per sbloccare la porta</param>
    /// <returns></returns>
    private bool ErrorCmd(string typeError = "cmdError", Item? item = null)
    {
        Logger.Log($"Errore: {typeError} nella funzione ErrorCmd");
        switch (typeError)
        {
            case "cmdError": 
            default:
                AnsiConsole.MarkupLine("[bold italic #ff4400]Comando errato! Scrivi [#11ff11]HELP[/] per vedere la lista dei comandi[/]");
                return false;
            case "argsError":
                AnsiConsole.MarkupLine("[bold italic #ff4400]Argomento errato! Scrivi [#11ff11]HELP[/] per vedere la lista dei comandi e degli argomenti disponibili![/]");
                return false;
            case "notInThisRoom":
                AnsiConsole.MarkupLine("[bold italic #ff4400]Non puoi analizzare una stanza a distanza![/]");
                //assistente ti ricorda dove ti trovi.
                return false;
            case "itemNotFound":
                AnsiConsole.MarkupLine("[bold italic #ff4400]L'oggetto non esiste![/]");
                return false;
            case "itemNotInRoom":
                AnsiConsole.MarkupLine("[bold italic #ff4400]L'oggetto non è in questa stanza![/]");
                return false;
            case "failItemAddToInventory":
                AnsiConsole.MarkupLine("[bold italic #ff4400]Non sei riuscito a mettere l'oggetto nel tuo inventario[/]");
                return false;
            case "itemNotPickable":
                AnsiConsole.MarkupLine("[bold italic]Non puoi raccogliere questo oggetto.[/]");
                return false;
            case "inventoryFull":
                AnsiConsole.MarkupLine("[bold italic]L'oggetto pesa troppo per il tuo inventario, prova a svuotarti per raccogliere l'oggetto![/]");
                return false;
            case "roomDoesNotExist":
                AnsiConsole.MarkupLine("[bold italic #ff4400]Non c'è nessuna stanza li![/]");
                return false;
            case "destinationError":
                AnsiConsole.MarkupLine("[bold italic #ff4400]Destinazione errata, ricorda, puoi scrivere solo: (Nord|Sud|Est|Ovest)[/]");
                return false;
            case "roomIsLocked":
                if(item!=null) 
                    AnsiConsole.MarkupLine($"[bold italic]La stanza è bloccata! Richiede: [{item.Color}]{item.Name}[/][/]");
                return false;
            case "roomIsNotLocked":
                AnsiConsole.MarkupLine($"[bold italic]La stanza non è chiusa a chiave![/]");
                return false;
            case "incompatibleKey":
                AnsiConsole.MarkupLine("[bold italic #ff4400]L'oggetto che stai cercando di usare, non è quello giusto![/]");
                return false;
            case "FileNotFound":
                AnsiConsole.MarkupLine("[bold italic #ff4400]Il salvataggio che stai cercando di caricare non esiste![/]");
                return false;
            case "noItemsInInventory":
                AnsiConsole.MarkupLine("[bold italic #ff4400]Non hai oggetti da utilizzare nell'inventario![/]");
                return false;
            case "itemNotUsable":
                AnsiConsole.MarkupLine("[bold italic #ff4400]L'oggetto non sembra avere uno scopo chiaro[/]");
                return false;
            case "itemDoesNotExist":
                AnsiConsole.MarkupLine($"[bold italic #ff4400]L'oggetto non esiste![/]");
                return false;
            case "itemNotInInventory":
                AnsiConsole.MarkupLine($"[bold italic]L'oggetto non è nel tuo inventario.[/]");
                return false;
            case "itemIsNotAnalyzable":
                AnsiConsole.MarkupLine($"[bold italic]L'oggetto non è analizzabile.[/]");
                return false;
            case "mapNotFound":
                AnsiConsole.MarkupLine($"[bold italic #ff4400]Non hai la mappa![/]");
                return false;
        }
    }
}