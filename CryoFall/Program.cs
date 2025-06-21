using CryoFall.Character;
using CryoFall.Commands;
using CryoFall.Dialogue;
using CryoFall.Items;
using CryoFall.Rooms;
using CryoFall.SaveSystem;
using Spectre.Console;

namespace CryoFall;

class Program
{
    private static void Main(string[] args)
    {
        //TODO Dare la scelta iniziale al giocatore se far partire una partita da zero o se caricare un salvataggio (solo se c'è già).
        
        #region INTRODUZIONE
        #region Salvataggio Items e Rooms
        //Ogni file "Repository" legge il json di una determinata cosa.
        var roomRepo = RoomRepository.Load();
        var itemRepo = ItemRepository.Load();
        
        //Manager di tutti gli oggetti presenti in gioco.
        var itemsManager = new ItemsManager();
        foreach (Item item in itemRepo.GetAllItemsFromJson())
        {
            itemsManager.AddItem(item);
        }
        
        //Manager di tutte le stanze presenti in gioco.
        var roomsManager = new RoomsManager();
        foreach (Room room in roomRepo.GetAllRoomsObjects())
        {
            var finalRoom = room;
            //Assegniamo tutti gli item del json dentro le stanze che li contengono.
            finalRoom = roomsManager.AddItemsInRoom(room,itemsManager);
            
            roomsManager.AddRoom(finalRoom);
        }
        roomsManager.SetRooms(roomRepo.GetAllNearRooms(roomsManager.GetRooms(), roomsManager)); // -> Salvo tutte le stanze qui dentro.
        
        //setto il percorso di salvataggio
        var savesDir = Path.Combine(AppContext.BaseDirectory, "saves");
        Directory.CreateDirectory(savesDir);
        var savePath = Path.Combine(savesDir, "save1.json");
        
        #endregion
        #region DEBUG
        var live = false;
        var ms = 10;
        #endregion
        #region DIALOGHI E INIZIALIZZAZIONE PLAYER
        MainCharacter player;
        bool loaded = false;
        if (File.Exists(savePath))
        {
            Console.Write("Trovato salvataggio! Vuoi caricare la partita? (s/n): ");
            var ans = Console.ReadLine()?.Trim().ToLower();
            if (ans == "s")
            {
                // Creo un player “vuoto” e poi lo popolo col SaveManager
                player = new MainCharacter("PlayerTemp", 30);
                SaveManager.Load(savePath, player, roomsManager, itemsManager);
                ConsoleStylingWrite.StartDialogue(player.LastDialogueId, player, 10, liveWriting:false);
                loaded = true;
            }
            else
            {
                // farà il ramo “nuova partita”
                player = null!;
            }
        }
        else
        {
            player = null!;
        }

        // se non ho caricato, avvio il nuovo gioco
        if (!loaded)
        {
            // dialogo e scelta del nome iniziale
            ConsoleStylingWrite.StartDialogue("benvenuto", null,  msToWaitForLine: 500, false);
            var name = ConsoleStylingWrite.GetPlaceHolders("playerName");
            player = new MainCharacter(name, 30);
            player.CurrentRoom = roomsManager.FindRoom("sala_ibernazione") ?? throw new InvalidOperationException();

            // Intro iniziale
            ConsoleStylingWrite.StartDialogue("introIbernazione", player, 10, liveWriting:false);
        }

        // 5) Avvio il CommandManager e il resto del flusso
        var cmdManager = new CommandManager();
        if (!player.HasCompletedTutorial)
        {
            Tutorial(cmdManager, player, roomsManager, itemsManager);
            player.HasCompletedTutorial = true;
        }

        GameplayAtto_01(cmdManager, player, roomsManager, itemsManager);
        GameplayAtto_02(cmdManager, player, roomsManager, itemsManager);
        
    }
    #endregion


    static bool ReadCmd(CommandManager cmdManager,MainCharacter player, RoomsManager rm,ItemsManager im, string cmdToWaitFor="")
    {
        var cmd = "";
        do
        {
            AnsiConsole.Markup("[bold #4287f5]>[/] ");
            cmd = Console.ReadLine();
        } while (!cmdManager.ReadCommand(cmd, player,rm,im));

        if (!string.IsNullOrEmpty(cmdToWaitFor))
        {
            return cmd.ToLower().Contains(cmdToWaitFor.ToLower());
        }
        
        return true;
    }

    static bool ReadCmdTutorial(CommandManager cmdManager, MainCharacter player, RoomsManager rm, ItemsManager im, string cmdToWaitFor) //funzione per verificare limitare input nel tutorial
    {
        AnsiConsole.Markup("[bold #4287f5]>[/] ");
        var cmd = Console.ReadLine();
        
        // Prima controlla se è il comando giusto
        if (!string.IsNullOrEmpty(cmdToWaitFor) && !cmd.ToLower().Contains(cmdToWaitFor.ToLower()))
        {
            return false; // Comando sbagliato, non eseguire nulla
        }
        
        // Se è il comando giusto, eseguilo
        return cmdManager.ReadCommand(cmd, player, rm, im);
    }

    static void Tutorial(CommandManager cmdManager,MainCharacter player,RoomsManager rm,ItemsManager im)
    {
        bool tutorial = false;
        while (!tutorial)
        {
            if (player.CurrentRoom != rm.FindRoom("sala_ibernazione"))
            {
                break;
            }
            
            //Comando help
            do
            {
                ConsoleStylingWrite.WriteTutorial("[yellow]Usa il comando 'help' per vedere tutti i comandi disponibili![/]");
            } while (!ReadCmdTutorial(cmdManager, player, rm, im, "help"));
            
            //Comando analizza
            ConsoleStylingWrite.StartDialogue("tutorial_000", player);
            do
            {
                ConsoleStylingWrite.WriteTutorial("[yellow]Devi usare il comando 'analizza' per continuare il tutorial![/]");
            } while(!ReadCmdTutorial(cmdManager, player, rm, im, "analizza"));
            
            //comando prendi oggetto
            ConsoleStylingWrite.StartDialogue("tutorial_002", player);
            do
            {
                ConsoleStylingWrite.WriteTutorial("[yellow]Devi usare il comando 'prendi codice di accesso' per continuare il tutorial![/]");
            } while(!ReadCmdTutorial(cmdManager, player, rm, im, "prendi") || !player.Inventory.Items.Contains(im.FindItem("codice_di_accesso"))); //fixare il fatto che se prendo un oggetto diverso dalla chiave mi vada avanti
            
            //comando apri inventario
            ConsoleStylingWrite.StartDialogue("tutorial_003", player);
            do
            {
                ConsoleStylingWrite.WriteTutorial("[yellow]Devi usare il comando 'inventario' per continuare il tutorial![/]");
            } while(!ReadCmdTutorial(cmdManager, player, rm, im, "inventario"));
            
            //comando usa
            ConsoleStylingWrite.StartDialogue("tutorial_004", player);
            do
            {
                ConsoleStylingWrite.WriteTutorial("[yellow]Devi usare il comando 'usa' per continuare il tutorial![/]");
            } while(!ReadCmdTutorial(cmdManager, player, rm, im, "usa"));
            
            //comando lascia
            ConsoleStylingWrite.StartDialogue("tutorial_005", player);
            do
            {
                ConsoleStylingWrite.WriteTutorial("[yellow]Devi usare il comando 'lascia' per continuare il tutorial![/]");
            } while(!ReadCmdTutorial(cmdManager, player, rm, im, "lascia"));
            
            //comando muovi
            ConsoleStylingWrite.StartDialogue("tutorial_006", player);
            do
            {
                ConsoleStylingWrite.WriteTutorial("[yellow]Devi usare il comando 'muoviti' per continuare il tutorial![/]");
            } while(!ReadCmdTutorial(cmdManager, player, rm, im, "muoviti"));
            
            //TODO Fare if per finire il gioco.
            if(player.CurrentRoom!=rm.FindRoom("sala_ibernazione")) tutorial = true;
        }
        ConsoleStylingWrite.StartDialogue("tutorial_007", player); 
    }
    
    static void GameplayAtto_01(CommandManager cmdManager, MainCharacter player, RoomsManager rm, ItemsManager im)
    {
        // Uso HashSet per poter sfruttare Add(...) che restituisce bool
        var itemsInInventory = new HashSet<string>();
        bool gameplay = false;

        while (!gameplay)
        {
            ReadCmd(cmdManager, player, rm, im);
            // STANZA TECNICA
            if (player.CurrentRoom.Id == "stanza_tecnica"
                && !player.VisitedRoomIds.Contains("stanza_tecnica"))
            {
                ConsoleStylingWrite.StartDialogue("atto1_001", player);
            }

            // CORRIDOIO OVEST 2 (dopo stanza_tecnica)
            if (player.CurrentRoom.Id == "corridoio_ovest_2"
                && player.VisitedRoomIds.Contains("stanza_tecnica")
                && !player.VisitedRoomIds.Contains("corridoio_ovest_4"))
            {
                ConsoleStylingWrite.StartDialogue("assistente_012", player);
            }

            // CORRIDOIO OVEST 4 (dopo stanza_tecnica)
            if (player.CurrentRoom.Id == "corridoio_ovest_4"
                && player.VisitedRoomIds.Contains("stanza_tecnica")
                && !player.VisitedRoomIds.Contains("corridoio_ovest_4"))
            {
                ConsoleStylingWrite.StartDialogue("main_017", player);
            }

            // CORRIDOIO SUD
            if (player.CurrentRoom.Id == "corridoio_sud"
                && !player.VisitedRoomIds.Contains("corridoio_sud"))
            {
                ConsoleStylingWrite.StartDialogue("assistente_013", player);
            }

            // ZONA DI SCARICO
            if (player.CurrentRoom.Id == "zona_di_scarico" &&
                !player.VisitedRoomIds.Contains("zona_di_scarico"))
            {
                ConsoleStylingWrite.StartDialogue("assistente_014", player);
                EscapeFromRobotScene(cmdManager, player, rm, im);
                gameplay = false;
            }
            //IF sempre finale.
            if (player.VisitedRoomIds.Add(player.CurrentRoom.Id))
            {
                //Console.WriteLine($"{player.CurrentRoom.Id} stanza aggiunta nel while e nel file.");
            }
        }
    }

    static void EscapeFromRobotScene(CommandManager cmdManager, MainCharacter player, RoomsManager rm, ItemsManager im)
    {
        var robotHand = im.FindItem("mano_ax_7");
        var teleportDevice = im.FindItem("dispositivo_teletrasporto");
        
        while (!ReadCmdTutorial(cmdManager, player, rm, im, "prendi") ||
               !player.Inventory.Items.Contains(robotHand))
        {
            ConsoleStylingWrite.HelperCmd($"Cosa fai? prendi [{robotHand.Color}]{robotHand.Name}[/] [bold]SUBITO![/]");
        }
        ConsoleStylingWrite.StartDialogue("atto1_012", player);
        
        while (!ReadCmdTutorial(cmdManager, player, rm, im, "prendi") ||
               !player.Inventory.Items.Contains(teleportDevice))
        {
            if (player.Inventory.Items.Contains(teleportDevice))
            {
                ConsoleStylingWrite.HelperCmd($"Oh beh, lo hai già preso, ottimo, ora USALO!");
                break;
            }
            ConsoleStylingWrite.HelperCmd($"Cosa fai? prendi [{teleportDevice.Color}]{teleportDevice.Name}[/] [bold]SUBITO![/]");
        }
        ConsoleStylingWrite.StartDialogue("atto2_001", player);
        while (!ReadCmdTutorial(cmdManager, player, rm, im, "teletrasporto"))
        {
            ConsoleStylingWrite.HelperCmd($"Cosa fai? Teletrasporti [bold]SUBITO![/], oh certo, non sai come fare.. magari prova ad usare il [bold italic]teletrasporto[/]");
        }
        
        ConsoleStylingWrite.StartDialogue("assistente_020", player);
        rm.FindRoom("zona_di_scarico").UnlockKeyId = "codice_zona_scarico";
        rm.FindRoom("zona_di_scarico").IsLocked = true;
    }
    
    static void GameplayAtto_02(CommandManager cmdManager, MainCharacter player, RoomsManager rm, ItemsManager im)
    {
        // Uso HashSet per poter sfruttare Add(...) che restituisce bool
        var itemsInInventory = new HashSet<string>();
        bool gameplay = false;
        
        while (!gameplay)
        {
            ReadCmd(cmdManager, player, rm, im);
        }
    }

}
#endregion