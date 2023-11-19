using System;
using System.Net;
using System.Net.Sockets;

namespace TwentySquares;

public class Game()
{
    public static void Main()
    {
        while(true)
        {
            Console.Clear();

            int input = Choose([
                "2 player mode",
                "Host a game",
                "Connect to game"
            ]);

            switch (input)
            {
                case 0:
                    TwoPlayers();
                    break;
                case 1:
                    HostGame();
                    break;
                case 2:
                    ConnectToGame();
                    break;
            }
        }
    }

    public static int Choose(string[] options, string msg = "")
    {
        if (options.Length == 0)
            return -1;

        Console.CursorVisible = false;
        int optionsLen = options.Length;
        int result = 0;

        while (true)
        {
            Console.Clear();

            if (!string.IsNullOrWhiteSpace(msg))
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(msg);
                Console.ResetColor();
            }

            for (uint i = 0; i < optionsLen; ++i)
            {
                if (i != result)
                {
                    Console.WriteLine($"  {options[i]}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(options[i]))
                {
                    ++result;
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"> {options[i]}");
                Console.ResetColor();
            }

            ConsoleKeyInfo keyInfo = Console.ReadKey();

            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                    if (result > 0) --result;
                    break;
                case ConsoleKey.DownArrow:
                    if (result < optionsLen - 1) ++result;
                    break;
                case ConsoleKey.Enter:
                    Console.CursorVisible = true;
                    return result;
            }
        }
    }

    public static void TwoPlayers()
    {
        Row row = new();
        bool isX = true, err = false;
        uint input;

        while (!row.IsEnd())
        {
            while (true)
            {
                Console.Clear();
                row.PrintRow(Square.NONE);

                Console.WriteLine();

                if (err)
                {
                    int pos = Console.CursorTop;

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n\nMůžeš zadat pouze 1 nebo 2");
                    Console.ResetColor();

                    Console.SetCursorPosition(0, pos);
                }

                Console.ForegroundColor = isX ? ConsoleColor.Red : ConsoleColor.Blue;
                Console.Write($"[{(isX ? 'X' : 'O')}] >> ");
                Console.ResetColor();

                string strInput = Console.ReadLine() ?? string.Empty;
                err = true;

                try
                {
                    input = uint.Parse(strInput);

                    if (input != 1 && input != 2)
                        continue;
                }
                catch
                {
                    continue;
                }

                break;
            }

            row.Add((isX ? Square.X : Square.O), input);

            err = false;
            isX = !isX;
        }
        Console.Clear();
        row.PrintRow(Square.NONE);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\nKonec Hry.\n{(
            row.Winner() == Square.X ? "Vyhrál hráč, který hrál za X!!!" :
            row.Winner() == Square.O ? "Vyhrál hráč, který hrál za O!!!" : "Tie. Error might appeared."
            )}");
        Console.ResetColor();
        Console.ReadLine();
    }

    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
    public static void HostGame()
    {
        Console.Clear();

        GameServer server = new("127.0.0.1");
        GameClient client = new();

        var serverTask = new Task(server.Host);
        serverTask.Start();

        server.WaitForSetup();

        client.Connect("127.0.0.1", server.GetPort());
        serverTask.Wait();
    }

    public static void ConnectToGame()
    {
        GameClient client = new();

        Console.Write("PORT: ");
        int port = int.Parse(Console.ReadLine() ?? string.Empty);

        client.Connect("127.0.0.1", port);
    }
}