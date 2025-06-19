using CryoFall.Character;
using CryoFall.Dialogue;
using CryoFall.Rooms;
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
    public bool ReadCommand(string cmd,MainCharacter player,RoomsManager roomsManager)
    {
        var args = cmd.Split(' ');
        // La prima parola letta (Comando digitato dall'utente)
        
        switch (args[0])
        {
            case "help":
                return Help();
            case "teletrasporta":
            case "tp":
                if (args.Length < 2) return ErrorCmd();
                return Teleport(args[1], true);
            case "muoviti":
            case"move":
                if (args.Length < 2) return ErrorCmd();
                return Teleport(args[1]);
            case "analizza":
                if (args.Length < 2) return ErrorCmd();
                return AnalyzeRoom(args[1], player, roomsManager);
            
            default: return ErrorCmd();
        }
    }

    /// <summary>
    /// Teletrasporta il giocatore in una stanza.
    /// Utilizzabile anche per muoversi nelle stanze normalmente.
    /// </summary>
    /// <param name="destination"> La destinazione da raggiungere</param>
    /// <param name="isTeleport"> Capire se il giocatore si è teletrasportato o spostato normalmente</param>
    /// <returns>
    /// <c>True</c> se il comando è valido ed è stato eseguito correttamente;
    /// <c>False</c> altrimenti.
    /// </returns>
    private bool Teleport(string destination,bool isTeleport = false)
    {
        return ErrorCmd("argsError");
    }

    /// <summary>
    ///  Verifica se il comando è valido e può essere eseguito.
    /// </summary>
    /// <returns>
    /// <c>True</c> se il comando è valido ed è stato eseguito correttamente;
    /// <c>False</c> altrimenti.
    /// </returns>
    private bool Help()
    {
        ConsoleStylingWrite.HelperCmd($"Questa è la lista di tutti i comandi:");
        Console.WriteLine();
        foreach (var cmd in CommandsRepository.ById.Values)
        {
            //ConsoleStylingWrite.WriteCmdHelp(cmd);
            AnsiConsole.MarkupLine($"   [bold #1fc459][[{cmd.Cmd}]][/]: {cmd.Description}");
        }
        Console.WriteLine();
        return true;
    }

    private bool AnalyzeRoom(string room,MainCharacter player,RoomsManager rm)
    {
        var roomToAnalyze = rm.FindRoom(room);
        if (roomToAnalyze == null) return ErrorCmd("argsError");
        if(roomToAnalyze.Id != player.CurrentRoom.Id) return ErrorCmd("roomDoesNotExist");

        ConsoleStylingWrite.AnalyzeRoom(roomToAnalyze);
        
        return true;
    }

    /// <summary>
    /// Mostra un errore e invita il giocatore a scrivere HELP per visualizzare i comandi
    /// </summary>
    /// <param name="typeError">Il tipo di errore da mostrare</param>
    /// <returns></returns>
    private bool ErrorCmd(string typeError = "cmdError")
    {
        switch (typeError)
        {
            case "cmdError": 
            default:
                AnsiConsole.MarkupLine("[bold italic #ff4400]Comando errato! Scrivi [#11ff11]HELP[/] per vedere la lista dei comandi[/]");
                return false;
            case "argsError":
                AnsiConsole.MarkupLine("[bold italic #ff4400]Argomento errato! Scrivi [#11ff11]HELP[/] per vedere la lista dei comandi e degli argomenti disponibili![/]");
                return false;
            case "roomDoesNotExist":
                AnsiConsole.MarkupLine("[bold italic #ff4400]La stanza in cui ti trovi non esiste![/]");
                //assistente ti ricorda dove ti trovi.
                return false;
        }
    }
}