
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TwentySquares
{
    enum Status
    {
        Waiting,
        Connected,
        LostConnection
    }
    public class GameClient
    {
        private readonly Row row;
        private bool isX = false, myTurn = false, end = false;
        private Status status = Status.Waiting;
        private int port = 0;


        public GameClient()
        {
            row = new Row();
        }


        private void ClearLine()
        {
            int leftPos = Console.CursorLeft, topPos = Console.CursorTop;
            Console.SetCursorPosition(0, topPos);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(leftPos, topPos);
        }

        private void PrintRow()
        {
            int leftPos = Console.CursorLeft, topPos = Console.CursorTop;
            Console.SetCursorPosition(0, 0);
            ClearLine();

            row.PrintRow(isX ? Square.X : Square.O);

            Console.SetCursorPosition(leftPos, topPos);
        }

        private void PrintInput(uint type)
        {
            Console.SetCursorPosition(0, 2);
            ClearLine();

            string msg = "";

            switch(type)
            {
                case 0:
                    Console.ForegroundColor = isX ? Row.GetColorO(true) : Row.GetColorX(false);
                    msg = $"[ {(isX ? 'O' : 'X')} ] Soupeř ... ";
                    break;
                case 1:
                    Console.ForegroundColor = ConsoleColor.Green;
                    msg = $"[ {(isX ? 'X' : 'O')} ] Ty  >> ";
                    break;
                case 2:
                    Console.ForegroundColor = ConsoleColor.Green;
                    msg = $"[ {(isX ? 'X' : 'O')} ] Ty ... ";
                    break;
            }

            Console.Write(msg);
            Console.ResetColor();
        }

        private void PrintError(string msg)
        {
            int leftPos = Console.CursorLeft, topPos = Console.CursorTop;
            Console.SetCursorPosition(0, 3);
            ClearLine();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(msg);
            Console.ResetColor();
            Console.SetCursorPosition(leftPos, topPos);
        }

        private void PrintStatus()
        {
            switch (status)
            {
                case Status.Waiting:
                    PrintStatus("? Čekání na hráče...", ConsoleColor.DarkYellow);
                    break;
                case Status.Connected:
                    PrintStatus("~ Připojeno", ConsoleColor.Blue);
                    break;
                case Status.LostConnection:
                    PrintStatus("! Ztracené spojení", ConsoleColor.Red);
                    break;
            }
        }

        private void PrintStatus(string status, ConsoleColor bg)
        {
            int leftPos = Console.CursorLeft, topPos = Console.CursorTop;
            string msg = $" {status}", strPort = $"#{port} ";


            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = bg;
            Console.SetCursorPosition(0, Console.WindowHeight - 4);
            ClearLine();
            Console.SetCursorPosition(0, Console.WindowHeight - 2);
            ClearLine();
            Console.SetCursorPosition(0, Console.WindowHeight - 3);
            ClearLine();

            Console.Write(msg);
            Console.Write(new string(' ', Console.WindowWidth - msg.Length - strPort.Length));
            Console.Write(strPort);

            Console.ResetColor();
            Console.SetCursorPosition(leftPos, topPos);
        }

        public void Connect(string ip, int port)
        {
            Console.CursorVisible = false;
            Console.Clear();
            this.port = port;
            try
            {
                IPAddress addr = IPAddress.Parse(ip);
                myTurn = false;
                end = false;

                TcpClient cl = new(ip, port);
                NetworkStream stream = cl.GetStream();
                

                isX = Fetch(stream, "WHOAMI") == "X";
                end = IsEnd(stream);
                PrintStatus();
                while ((status = FetchStatus(stream)) == Status.Waiting) ;
                PrintStatus();

                end = IsEnd(stream);
                while(!IsEnd(stream))
                {
                    isX = Fetch(stream, "WHOAMI") == "X";
                    Console.Clear();

                    PrintStatus();

                    row.SetFromBytes(FetchRaw(stream, "SEND-ROW"));
                    PrintRow();

                    PrintInput(0);
                    while((myTurn = Fetch(stream, "CAN-PLAY") != "YES") && (!IsEnd(stream))) ;

                    if (end)
                        break;


                    while (true)
                    {
                        row.SetFromBytes(FetchRaw(stream, "SEND-ROW"));
                        PrintRow();

                        PrintStatus();

                        PrintInput(1);
                        Console.CursorVisible = true;

                        string input = Console.ReadLine() ?? string.Empty;
                        Console.CursorVisible = false;
                        PrintInput(2);
                        PrintError("");
                        int move;

                        try
                        {
                            move = int.Parse(input);
                            
                            if (move != 1 && move != 2)
                            {
                                PrintError("Můžeš přidat pouze 1 nebo 2");
                                continue;
                            }
                        }
                        catch
                        {
                            PrintError("Odpověď nelze přečíst jako celé číslo");
                            continue;
                        }
                        string res = Fetch(stream, $"ADD-{move}");
                        if ( res != "OK")
                        {
                            PrintError($"Server odpověď nepřijal (ADD-{move} -> {res})");
                            continue;
                        }
                        break;
                    }
                }

                PrintStatus();

                string winner = Fetch(stream, "WINNER");

                Console.Clear();
                PrintRow();

                if (winner != "NONE")
                {
                    bool won = ((isX && winner == "X") || (!isX && winner == "O"));

                    if (won)
                        PrintStatus("Vyhrál jsi!", ConsoleColor.Yellow);
                    else
                        PrintStatus("Prohrál jsi. Příště si povedeš lépe.", ConsoleColor.DarkMagenta);
                }
                else
                {
                    PrintStatus("! Něco se nepovedlo", ConsoleColor.Red);
                }

                cl.Close();
                stream.Close();

                Console.ReadKey();
                Console.Clear();
            }
            catch
            {
                Console.Clear();
                status = Status.LostConnection;
                PrintStatus();
                PrintError("Bylo ztraceno spojení se serverem.");
                Console.ReadKey();
                Console.Clear();
            }
            finally
            {
                Console.CursorVisible = true;
            }
        }

        public bool IsEnd(NetworkStream stream) => (
                    (end = (Fetch(stream, "END") == "YES")) ||
                    ((status = FetchStatus(stream)) == Status.LostConnection));

        private static string Fetch(NetworkStream stream, string msg)
        {
            byte[] data = Encoding.Default.GetBytes(msg);
            stream.Write(data, 0, data.Length);

            data = new byte[256];
            int len = stream.Read(data, 0, data.Length);
            return Encoding.Default.GetString(data, 0, len);
        }
        private static byte[] FetchRaw(NetworkStream stream, string msg)
        {
            byte[] data = Encoding.Default.GetBytes(msg);
            stream.Write(data, 0, data.Length);

            data = new byte[256];
            stream.Read(data, 0, data.Length);
            return data;
        }

        private static Status FetchStatus(NetworkStream stream)
        {
            string res = Fetch(stream, "STATUS");
            return res switch
            {
                "WAIT" => Status.Waiting,
                "READY" => Status.Connected,
                "LOST-CLIENT" => Status.LostConnection,
                _ => throw new Exception("Invalid server response")
            } ;
        }
    }
}