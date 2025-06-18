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
        var cmdManager = new CommandManager();
        
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
        
        // crea e avvia il CommandsManager
        
        
        var live = false;
        
        //Introduzione, dialogo a scelte multiple per spiegare la storia e far scegliere il nome al giocatore.
        ConsoleStylingWrite.StartDialogue("benvenuto",msToWaitForLine:500,live);
        
        MainCharacter player = new MainCharacter(ConsoleStylingWrite.GetPlaceHolders("playerName"), 30);
        player.CurrentRoom = roomsManager.FindRoom("sala_ibernazione");
        
        //Introduzione, primi dialoghi con robot e amici.
        ConsoleStylingWrite.StartDialogue("introIbernazione",10,liveWriting: live);
        bool isGameOver = false;
        
        //TODO Inizio gioco, comandi usabili ecc.
        
    }

    static void ReadCmd(CommandManager cmdManager)
    {
        ConsoleStylingWrite.StartDialogue("benvenuto",msToWaitForLine:500);
        var cmd = "";
        do
        {
            Console.Write("> ");
            cmd = Console.ReadLine();
        } while (!cmdManager.ReadCommand(cmd));
    }
}