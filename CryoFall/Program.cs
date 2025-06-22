using CryoFall.Character;
using CryoFall.Commands;
using CryoFall.Dialogue;
using CryoFall.Items;
using CryoFall.Rooms;
using CryoFall.SaveSystem;
using Spectre.Console;
using System.Text.Json;
using System.Text.RegularExpressions;
using CryoFall.Logging;

namespace CryoFall;

class Program
{
    /// <summary>
    /// Metodo Main - gestisce l'avvio del gioco
    /// </summary>
    private static void Main(string[] args)
    {   
        Logger.InitNewLog(); // inizializza il logger
        #region BENVENUTO NEL GIOCO

        WelcomeToCryoFall(); // Stampa grafica ASCII iniziale
        Logger.Log("Gioco avviato");

        #endregion
        
        
        #region INTRODUZIONE
        #region Salvataggio Items e Rooms
        // Carica le informazioni delle stanze da file JSON 
        var roomRepo = RoomRepository.Load();
        // Carica le informazioni degli oggetti da file JSON
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
        var saveDirPath = Directory.GetFiles(savesDir);
        
        #endregion
        #region DIALOGHI E INIZIALIZZAZIONE PLAYER
        MainCharacter player = null!;
        var loaded = false;
        if (saveDirPath.Length>0)
        {
            Logger.Log("File di salvataggio trovati.");
            player = LoadSave(player, roomsManager,itemsManager,savesDir);
            if(player != null!) loaded = true;
        }

        // se non ho caricato, avvio il nuovo gioco
        if (!loaded)
        {
            Logger.Log("Parte il dialogo di benvenuto");
            // dialogo e scelta del nome iniziale
            ConsoleStylingWrite.StartDialogue("benvenuto", null, null,  msToWaitForLine: 500, false);
            var name = ConsoleStylingWrite.GetPlaceHolders("playerName");
            Logger.Log($"Il giocatore ha scelto il nome: {name}");
            player = new MainCharacter(name, 30);
            player.CurrentRoom = roomsManager.FindRoom("sala_ibernazione") ?? throw new InvalidOperationException();
            
            // Intro iniziale
            ConsoleStylingWrite.StartDialogue("introIbernazione", player, msToWaitForLine:10, liveWriting:false);
        }

        // Avvio il CommandManager e il resto del flusso
        var cmdManager = new CommandManager();
        if (!player.HasCompletedTutorial)
        {
            Logger.Log("Il giocatore inizia il tutorial");
            Tutorial(cmdManager, player, roomsManager, itemsManager);
            player.HasCompletedTutorial = true;
        }

        if (!player.ActivetedEvents.Contains("atto1"))
        {
            Logger.Log("Il giocatore si trova nell'atto1");
            GameplayAtto_01(cmdManager, player, roomsManager, itemsManager);
            player.ActivetedEvents.Add("atto1");
        }

        if (!player.ActivetedEvents.Contains("atto3"))
        {
            Logger.Log("Il giocatore si trova nell'atto 3");
            GameplayAtto_03(cmdManager, player, roomsManager, itemsManager);
            player.ActivetedEvents.Add("atto3");
        }

        if (!player.ActivetedEvents.Contains("atto4"))
        {
            Logger.Log("Il giocatore si trova nell'atto 4");
            GameplayAtto_04(cmdManager, player, roomsManager, itemsManager);
            player.ActivetedEvents.Add("atto4");
        }

        if (!player.ActivetedEvents.Contains("attoFinal"))
        {
            Logger.Log("Il giocatore si trova nell'atto 4");
            GameplayAtto_05(cmdManager, player, roomsManager, itemsManager);
            player.ActivetedEvents.Add("attoFinal");
        }

        #endregion 
    }
    /// <summary>
    /// Metodo WelcomeToCryoFall - Stampa la Grafica del gioco
    /// </summary>
    static void WelcomeToCryoFall()
    {
        AnsiConsole.Write(new Align(
            new Text("┌───────────────────────────────────────────────────────────────────────────────────────────┐\n│                                                                                           │\n│      ______ .______     ____    ____  ______    _______    ___       __       __          │\n│     /      ||   _  \\    \\   \\  /   / /  __  \\  |   ____|  /   \\     |  |     |  |         │\n│    |  ,----'|  |_)  |    \\   \\/   / |  |  |  | |  |__    /  ^  \\    |  |     |  |         │\n│    |  |     |      /      \\_    _/  |  |  |  | |   __|  /  /_\\  \\   |  |     |  |         │\n│    |  `----.|  |\\  \\----.   |  |    |  `--'  | |  |    /  _____  \\  |  `----.|  `----.    │\n│     \\______|| _| `._____|   |__|     \\______/  |__|   /__/     \\__\\ |_______||_______|    │\n│                                                                                           │\n└───────────────────────────────────────────────────────────────────────────────────────────┘"),
            HorizontalAlignment.Left,
            VerticalAlignment.Top
        ));
        Console.WriteLine();
    }
    /// <summary>
    /// Fa vedere al player una lista di salvataggi, carica il salvataggio selezionato e ripristina lo stato di gioco (player, stanze, inventario, ecc.). Se il giocatore decide di non caricare il salvataggio allora il metodo avrà return <see langword="null"/> e quindi verrà creato un nuovo gioco.
    /// </summary>
    /// <param name="gameSaveDir">Directory contenente i file <c>save0N.json</c>.</param>
    /// <param name="roomsManager">Runtime rooms manager da ripopolare.</param>
    /// <param name="itemsManager">Runtime items manager da ripopolare.</param>
    /// <returns>Un <see cref="MainCharacter"/> restaurato oppure <c>null</c> se l'utente annulla.</returns>
    static MainCharacter LoadSave(MainCharacter player, RoomsManager roomsManager, ItemsManager itemsManager, string saveDirPath)
    {
        var files = Directory.GetFiles(saveDirPath);
        string[] saves = new string[files.Length+1];
        
        for (int i = 0; i < files.Length; i++)
        {
            saves[i] = $"Si, carica il salvataggio: [bold green]{Path.GetFileNameWithoutExtension(files[i])} {File.GetLastAccessTime(files[i]):g}[/]";
        }
        saves[^1] = "No, voglio giocare una nuova partita";
        
        var ans = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Trovato [bold green]salvataggio[/]! Vuoi [bold green]caricare[/] la partita?")
                .AddChoices(saves));
        
        Logger.Log("Giocatore sceglie se caricare un salvataggio o no.");
        if (ans.Contains("No"))
        {
            Logger.Log("Il giocatore ha scelto di non caricare il salvataggio.");
            return null!;
        }
        // ottiene il file che ha selezionato.
        var m = Regex.Match(ans, @"\bsave\d{2,3}\b", RegexOptions.IgnoreCase);
        var saveFilePath = Path.Combine(AppContext.BaseDirectory, $"saves/{m.Value}.json");
        // Creo un player “vuoto” e poi lo popolo col SaveManager
        player = new MainCharacter("PlayerTemp", 30);
        
        SaveManager.Load($"{saveFilePath}", player, roomsManager, itemsManager);
        if (player.LastDialogueId != null)
        {
            ConsoleStylingWrite.StartDialogue(player.LastDialogueId, player, msToWaitForLine:300, liveWriting:false, loadLastDialogue:true);
        }
        Logger.Log($"Il giocatore ha scelto di caricare il salvataggio: {saveFilePath}");
        return player;
    }

    /// <summary>
    /// Legge <see cref="Console"/> input in loop finchè il comando è reputato valido da <paramref name="cmdManager"/>.  Opzionalmente aspetta per un input specifico (<paramref name="cmdToWaitFor"/>). Usato fuori dal tutorial.
    /// </summary>
    /// <returns><see langword="true"/> se è stato inserito un comando corretto; altrimenti <see langword="false"/>.</returns>
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
    /// <summary>
    /// Variante di <see cref="ReadCmd"/> impiegata durante il tutorial: prima verifica che l'input contenga il comando richiesto (<paramref name="cmdToWaitFor"/>) e <b>solo se</b> la verifica passa lo esegue.
    /// </summary>
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

    /// <summary>
    /// Sequenza guidata che introduce i comandi di base (help, analizza, prendi, inventario, usa, lascia, muoviti).  Termina quando il giocatore lascia la "sala_ibernazione".
    /// </summary>
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
    /// <summary>
    /// Gestisce la logica dell'Atto I: esplorazione iniziale e fuga dal robot.
    /// </summary>
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
                gameplay = true;
            }
            //IF sempre finale.
            if (player.VisitedRoomIds.Add(player.CurrentRoom.Id))
            {
                //Console.WriteLine($"{player.CurrentRoom.Id} stanza aggiunta nel while e nel file.");
            }
        }
    }
    /// <summary>
    /// Minigioco di fuga: il player deve raccogliere prima la mano del robot AX‑7 e poi il dispositivo di teletrasporto, quindi usare il teletrasporto.  Tutte le istruzioni vengono forzate con
    /// <see cref="ReadCmdTutorial"/>.
    /// </summary>
    static void EscapeFromRobotScene(CommandManager cmdManager, MainCharacter player, RoomsManager rm, ItemsManager im)
    {
        var robotHand = im.FindItem("mano_ax_7");
        var teleportDevice = im.FindItem("dispositivo_di_teletrasporto");
        
        // raccogli mano
        while (!ReadCmdTutorial(cmdManager, player, rm, im, "prendi") ||
               !player.Inventory.Items.Contains(robotHand))
        {
            ConsoleStylingWrite.HelperCmd($"Cosa fai? prendi [{robotHand.Color}]{robotHand.Name}[/] [bold]SUBITO![/]");
        }
        ConsoleStylingWrite.StartDialogue("atto1_012", player);
        
        //raccogli teletrasporto
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
        
        //usa teletrasporto
        while (!ReadCmdTutorial(cmdManager, player, rm, im, "teletrasporto"))
        {
            ConsoleStylingWrite.HelperCmd($"Cosa fai? Teletrasporti [bold]SUBITO![/], oh certo, non sai come fare.. magari prova ad usare il [bold italic]teletrasporto[/]");
        }
        
        ConsoleStylingWrite.StartDialogue("assistente_020", player);
        rm.FindRoom("zona_di_scarico").IsLocked = true;
    }
    /// <summary>
    /// Atto III – il giocatore scopre la verità nella Zona di Controllo.
    /// </summary>
    static void GameplayAtto_03(CommandManager cmdManager, MainCharacter player, RoomsManager rm, ItemsManager im)
    {
        var gameplay = false;
        while (!gameplay)
        {
            ReadCmd(cmdManager, player, rm, im);
            if (player.CurrentRoom.Id == "zona_di_scarico")
            {
                //fine gioco gameover perchè sei tornato li e muori
            }
            
            // CORRIDOIO OVEST 3 DOPO ZONA DI SCARICO
            if (player.CurrentRoom.Id == "corridoio_ovest_3"&&
                player.ActivetedEvents.Add("dialogo_corr_ovest_3"))
            {
                ConsoleStylingWrite.StartDialogue("atto3_001", player);
            }
                
            //CORRIDOIO EST
            if (player.CurrentRoom.Id == "corridoio_est" &&
                !player.ActivetedEvents.Contains("dialogo_corr_est"))
            {
                ConsoleStylingWrite.StartDialogue("atto3_002", player);
                player.ActivetedEvents.Add("dialogo_corr_est");
            }
            
            //ZONA DI CONTROLLO
            if (player.CurrentRoom.Id == "zona_di_controllo" && !player.VisitedRoomIds.Contains("zona_di_controllo"))
            {
                ConsoleStylingWrite.StartDialogue("atto3_003", player, rm);
                gameplay = true;
            }
            //IF sempre finale.
            if (player.VisitedRoomIds.Add(player.CurrentRoom.Id))
            {
                //Console.WriteLine($"{player.CurrentRoom.Id} stanza aggiunta nel while e nel file.");
            }
        }
    }
    /// <summary>
    /// Atto IV – Gestisce l'alleanza con Lena e la ricerca dell'emettitore.
    /// </summary>
    static void GameplayAtto_04(CommandManager cmdManager, MainCharacter player, RoomsManager rm, ItemsManager im)
    {
        var gameplay = false;
        var photoFound = false;
        while (!gameplay)
        {
            ReadCmd(cmdManager, player, rm, im);

            if (player.CurrentRoom.Id == "zona_di_scarico")
            {
                //fine gioco gameover perchè sei tornato li e muori
            }
            
            // ZONA CARBURANTE NORD - LENA - FOTO
            if (player.CurrentRoom.Id == "zona_carburante_nord" && player.VisitedRoomIds.Contains("zona_di_controllo") &&
                !player.ActivetedEvents.Contains("lena_foto"))
            {
                ConsoleStylingWrite.StartDialogue("atto4_001", player);
                if (!player.Inventory.Items.Contains(im.FindItem("foto")))
                {
                    ConsoleStylingWrite.StartDialogue("lena_diffidente_005", player);
                    while (!photoFound)
                    {
                        ReadCmd(cmdManager, player, rm, im);
                        if (player.Inventory.Items.Contains(im.FindItem("foto")) && player.CurrentRoom.Id == "zona_carburante_nord")
                        {
                            photoFound = true;
                        }
                    }
                    player.ActivetedEvents.Add("lena_foto");
                }

                ConsoleStylingWrite.StartDialogue("lena_diffidente_006", player);
                player.ActivetedEvents.Add("lena_foto");
            }
                
            //PRENDERE EMETTITORE - ACCESSO ARMERIA
            if (player.CurrentRoom.Id == "armeria" && !player.VisitedRoomIds.Contains("armeria")&&
                player.ActivetedEvents.Contains("lena_foto"))
            {
                ConsoleStylingWrite.StartDialogue("armeria_access", player);
            }
            if (player.Inventory.Items.Contains(im.FindItem("emettitore_di_energia")) && !player.ActivetedEvents.Contains("emettitore_prendi") &&
                player.ActivetedEvents.Contains("lena_foto"))
            {
                ConsoleStylingWrite.StartDialogue("main_035", player);
                player.ActivetedEvents.Add("emettitore_prendi");
            }
            
            //ACCESSO INFERMERIA
            if (player.CurrentRoom.Id == "infermeria" && !player.VisitedRoomIds.Contains("infermeria") && !player.ActivetedEvents.Contains("emettitore_no_prendi") &&
                player.ActivetedEvents.Contains("lena_foto"))
            {
                ConsoleStylingWrite.StartDialogue("infermeria_access", player);
                player.ActivetedEvents.Add("emettitore_no_prendi");
            }
            
            //RITORNO CON EMETTITORE
            if (player.CurrentRoom.Id == "zona_carburante_nord" 
                && player.Inventory.Items.Contains(im.FindItem("emettitore_di_energia")) 
                && !player.ActivetedEvents.Contains("ritorno_con_emettitore"))
            {
                ConsoleStylingWrite.StartDialogue("ritorno_lena_con_emettitore", player);
                player.ActivetedEvents.Add("ritorno_con_emettitore");
                gameplay = true;
            }
            
            //RITORNO SENZA EMETTITORE
            if (player.CurrentRoom.Id == "zona_carburante_nord" 
                && !player.Inventory.Items.Contains(im.FindItem("emettitore_di_energia")) 
                && !player.ActivetedEvents.Contains("ritorno_senza_emettitore")
                && player.ActivetedEvents.Contains("lena_foto")
                && player.ActivetedEvents.Contains("emettitore_no_prendi"))
            {
                ConsoleStylingWrite.StartDialogue("ritorno_lena_senza_emettitore", player);
                player.ActivetedEvents.Add("ritorno_senza_emettitore");
                gameplay = true;
            }
            //IF sempre finale.
            if (player.VisitedRoomIds.Add(player.CurrentRoom.Id))
            {
                //Console.WriteLine($"{player.CurrentRoom.Id} stanza aggiunta nel while e nel file.");
            }
        }
    }
    /// <summary>
    /// Atto V – Scontro finale con AX‑7.  Il finale dipende dalla presenza dell'emettitore.
    /// </summary>
    static void GameplayAtto_05(CommandManager cmdManager, MainCharacter player, RoomsManager rm, ItemsManager im)
    {
        //TRUE se emittor è stato preso, FALSE se non è stato preso.
        bool emittor = player.ActivetedEvents.Contains("ritorno_con_emettitore");
        var gameplay = false;
        while (!gameplay)
        {
            ReadCmd(cmdManager, player, rm, im);
            
            if (player.CurrentRoom.Id == "corridoio_ovest_3"
                && player.ActivetedEvents.Add("uscitiDaCarburante"))
            {
                ConsoleStylingWrite.StartDialogue("atto5_000", player);
                if (emittor && player.Inventory.Items.Contains(im.FindItem("emettitore_di_energia"))) 
                    ConsoleStylingWrite.StartDialogue("atto5_000.2", player);
                else 
                    ConsoleStylingWrite.StartDialogue("atto5_000.1", player);
            }

            if (player.CurrentRoom.Id == "zona_di_scarico" &&
                player.ActivetedEvents.Add("finaleScarico"))
            {
                ConsoleStylingWrite.StartDialogue("atto5_001", player);
                if (emittor && player.Inventory.Items.Contains(im.FindItem("emettitore_di_energia"))) 
                    ConsoleStylingWrite.StartDialogue("main_040_con_emettitore", player);
                else 
                    ConsoleStylingWrite.StartDialogue("finale_no_emettitore_001", player);
            }
            
            //IF sempre finale.
            if (player.VisitedRoomIds.Add(player.CurrentRoom.Id))
            {
                //Console.WriteLine($"{player.CurrentRoom.Id} stanza aggiunta nel while e nel file.");
            }
        }
    }

}
#endregion