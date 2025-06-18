using CryoFall.Dialogue;
using Spectre.Console;

namespace CryoFall.Commands;

public class CommandManager
{
    public bool ReadCommand(string cmd)
    {
        bool result = false;
        var args = cmd.Split(' ');
        // La prima parola letta (Comando digitato dall'utente)
        
        switch (args[0])
        {
            case "help":
                return Help();
            case "teletrasporta":
            case "tp":
                if (args.Length > 1) return false;
                return Teleport(args[1], true);
            case "muoviti":
            case"move":
                return Teleport(args[1]);
            
            default: return false;
        }
        
        return result;
    }

    private bool Teleport(string destination,bool isTeleport = false)
    {
        return false;
    }

    private bool Help()
    {
        ConsoleStylingWrite.HelperCmd($"Questa è la lista di tutti i comandi:");
        Console.WriteLine();
        foreach (var cmd in CommandsRepository.ById.Values)
        {
            ConsoleStylingWrite.WriteCmdHelp(cmd);
        }
        Console.WriteLine();
        return true;
    }
}