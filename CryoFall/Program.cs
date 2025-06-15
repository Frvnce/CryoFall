using CryoFall.Character;
using CryoFall.Rooms;
using CryoFall.Utils;
namespace CryoFall;

class Program
{
    private static void Main(string[] args)
    {
        //TODO Dare la scelta iniziale al giocatore se far partire una partita da zero o se caricare un salvataggio (solo se c'è già).
        
        
        //Istanziare RoomsManager e aggiungere le stanze da un file json.
        RoomsManager roomsManager = new RoomsManager();
        //TODO foreach () // -> Per ogni stanza presente nel file json, la aggiunge nel roomsmanager.
        
        //Introduzione, dialogo a scelte multiple per spiegare la storia e far scegliere il nome al giocatore.
        ConsoleStylingWrite.StartDialogue("benvenuto",msToWaitForLine:500);
        
        MainCharacter player = new MainCharacter(ConsoleStylingWrite.GetPlaceHolders("playerName"), 30);
        //TODO player.CurrentRoom = roomsManager.FindRoom("Ibernazione"); // -> Aggiunge il player nella stanza ibernazione di default, dato che il gioco inizia li.
        
        //Introduzione, primi dialoghi con robot e amici.
        ConsoleStylingWrite.StartDialogue("introIbernazione");
        
        //TODO Inizio gioco, scelta di dove andare, raccogliere oggetti ecc.
        
        
        
    }
}