using CryoFall.Character;
using CryoFall.Commands;
using CryoFall.Dialogue;
using CryoFall.Items;
using CryoFall.Rooms;
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
            Console.WriteLine($"{item.IsPickable}");
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
        #endregion
        #region DEBUG
        var live = false;
        var ms = 10;
        #endregion
        #region DIALOGHI E INIZIALIZZAZIONE PLAYER
        //Introduzione, dialogo a scelte multiple per spiegare la storia e far scegliere il nome al giocatore.
        ConsoleStylingWrite.StartDialogue("benvenuto",msToWaitForLine:500,live);
        
        //Salvataggio personaggio ed inventario.
        MainCharacter player = new MainCharacter(ConsoleStylingWrite.GetPlaceHolders("playerName"), 30);
        player.CurrentRoom = roomsManager.FindRoom("sala_ibernazione");
        
        //Introduzione, primi dialoghi con robot e amici.
        ConsoleStylingWrite.StartDialogue("introIbernazione",ms,liveWriting: live);
        #endregion
        #endregion
        #region CommandsManager
        // crea e avvia il CommandsManager
        var cmdManager = new CommandManager();
        #endregion
        //TODO Tutorial
        Tutorial(cmdManager,player,roomsManager,itemsManager);
        //TODO Partire con il gameplay vero e proprio.
    }

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

    static void Tutorial(CommandManager cmdManager,MainCharacter player,RoomsManager rm,ItemsManager im)
    {
        bool tutorial = false;
        
        while (!tutorial)
        {
            if (!ReadCmd(cmdManager, player,rm,im,"help")) continue;
            ConsoleStylingWrite.StartDialogue("tutorial_000");
            if(!ReadCmd(cmdManager, player,rm,im, "analizza")) continue;
            ConsoleStylingWrite.StartDialogue("tutorial_002");
            if(!ReadCmd(cmdManager, player,rm,im, "prendi")) continue;
            ConsoleStylingWrite.StartDialogue("tutorial_003");
            //TODO Fare if per finire il gioco.

        }
    }
}