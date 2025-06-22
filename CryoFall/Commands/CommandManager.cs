using CryoFall.Character;
using CryoFall.Dialogue;
using CryoFall.Items;
using CryoFall.Logging;
using CryoFall.Rooms;
using CryoFall.SaveSystem;
using Spectre.Console;

namespace CryoFall.Commands;

public class CommandManager
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cmd">Il comando scritto dal giocatore</param>
    /// <param name="player"> Passa <see cref="MainCharacter"/> per gestire il giocatore </param>
    /// <returns>
    /// <c>True</c> se il comando è valido ed è stato eseguito correttamente;
    /// <c>False</c> altrimenti.
    /// </returns>
    public bool ReadCommand(string cmd,MainCharacter player,RoomsManager roomsManager,ItemsManager itemsManager)
    {
        var args = cmd.Split(' ');
        if (args.Length >= 2)
        {
            args = SplitCmd(args);
            args[1] = args[1].ToLower();
        }
        
        switch (args[0].ToLower())
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
            {
                SaveGame(player,roomsManager,itemsManager);
                return true;
            }
            case "carica":
            {
                if(args.Length != 2) return ErrorCmd();
                return LoadGame(player, roomsManager, itemsManager, args[1]);
            }

            default: return ErrorCmd();
        }
    }

    bool LoadGame(MainCharacter player, RoomsManager roomsManager, ItemsManager itemsManager, string savePath)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "saves", $"{savePath}.json");
        if (!File.Exists(path)) return ErrorCmd("FileNotFound");
        SaveManager.Load(path, player, roomsManager, itemsManager);
        ConsoleStylingWrite.HelperCmd("Partita caricata da " + path);
        Thread.Sleep(1000);
        AnsiConsole.Clear();
        Logger.Log($"Partita caricata da {path}");
        return true;
    }
    
    void SaveGame(MainCharacter player, RoomsManager roomsManager, ItemsManager itemsManager) 
    {
        // 1. Cartella di destinazione
        string savesDir = Path.Combine(AppContext.BaseDirectory, "saves");
        Directory.CreateDirectory(savesDir);

        // 2. Trova il prossimo indice libero -----------------------------------
        //    Cerca tutti i file "save*.json", estrae il numero e calcola max+1
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
        string fileName = $"save{nextIndex:00}.json";   // usa :000 se vuoi 3 cifre
        string path     = Path.Combine(savesDir, fileName);

        // 4. Salva e avvisa l’utente -------------------------------------------
        SaveManager.Save(path, player, roomsManager, itemsManager);
        ConsoleStylingWrite.HelperCmd($"Partita salvata in {path}");
        Logger.Log($"Partita salvata in {path}");
    }
    
    /// <summary>
    /// Fa un lavoro dove imposta il secondo argomento unendo tutti gli argomenti dopo il primo.
    /// In modo tale, il player può scrivere "analizza Sala Ibernazione" e il risultato sarà
    /// "analizza salaibernazione", in modo tale da trovare la stanza.
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

    private bool RemoveTopItem(MainCharacter pl)
    {
        ConsoleStylingWrite.HelperCmd($"Beh, immagino che non ci servirà.");
        return pl.Inventory.DropTop(pl.CurrentRoom);;
    }

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

    private bool Move(string destination, MainCharacter p, RoomsManager rm, ItemsManager im)
    {
        Room finalDestination;
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
    ///  Verifica se il comando è valido e può essere eseguito.
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
            //ConsoleStylingWrite.WriteCmdHelp(cmd);
            if (cmd.Id != "tp")
            {
                AnsiConsole.MarkupLine($"   [bold #1fc459][[{cmd.Cmd}]][/]: {cmd.Description}");
                continue;
            }
            if(cmd.Id == "tp" && p.Inventory.Items.Contains(im.FindItem("dispositivo_teletrasporto")))
                AnsiConsole.MarkupLine($"   [bold #1fc459][[{cmd.Cmd}]][/]: {cmd.Description}");
        }
        Logger.Log("Il giocatore ha scritto il comando HELP e sta visualizzando i comandi");
        Console.WriteLine();
        return true;
    }

    private bool AnalyzeItem(string item, MainCharacter p, ItemsManager im)
    {
        return false;
    }

    private bool AnalyzeRoom(MainCharacter player,RoomsManager rm)
    {
        var roomToAnalyze = rm.FindRoom(player.CurrentRoom.Id);
        if (roomToAnalyze == null) return ErrorCmd("argsError");
        if(roomToAnalyze.Id.Replace("_","") != player.CurrentRoom.Id.Replace("_","")) return ErrorCmd("notInThisRoom");

        ConsoleStylingWrite.AnalyzeRoom(roomToAnalyze);
        Logger.Log($"Il giocatore sta analizzando la stanza: NAME: {roomToAnalyze.NameOfTheRoom} ID: {roomToAnalyze.Id}");
        return true;
    }
    
    private bool AnalyzeRoomVisited(string room,MainCharacter player,RoomsManager rm)
    {
        var roomToAnalyze = rm.FindRoom(room);
        if (roomToAnalyze == null) return ErrorCmd("argsError");
        if(roomToAnalyze.Id.Replace("_","") != player.CurrentRoom.Id.Replace("_","")) return ErrorCmd("notInThisRoom");

        ConsoleStylingWrite.AnalyzeRoomVisited(roomToAnalyze);
        Logger.Log($"Il giocatore sta analizzando la stanza: NAME: {roomToAnalyze.NameOfTheRoom} ID: {roomToAnalyze.Id}");
        return true;
    }

    private bool TryPickUpItem(string item, MainCharacter player, RoomsManager rm, ItemsManager im)
    {
        var itemObject = im.FindItem(item);
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
                AnsiConsole.MarkupLine("[bold italic #ff4400]La stanza che stai cercando non esiste[/]");
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
        }
    }
}