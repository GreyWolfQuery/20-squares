
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TwentySquares
{
    public class GameServer
    {
        private int port;
        private bool serverOnline = false, clientsConnected = false, nowIsX = false, start = false;
        private readonly IPAddress addr;
        private readonly string strAddr;
        private readonly IPEndPoint remoteEndPoint;
        private readonly Row row;

        public int GetPort() => port;

        public GameServer(string ip)
        {
            strAddr = ip;
            addr = IPAddress.Parse(strAddr);
            remoteEndPoint = new(addr, port);
            row = new Row();
        }

        public bool IsClientsConnected() => clientsConnected;

        public void WaitForSetup()
        {
            while (!serverOnline) ;
        }

        private bool IsPortOpen()
        {
            try
            {
                TcpClient cl = new(strAddr, port);
                cl.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public void Host()
        {
            serverOnline = false;
            Random random = new();

            while(true)
            {
                port = random.Next(10000, 30001);

                if (!IsPortOpen())
                    break;
            }

            TcpListener server = new(addr, port);
            bool firstClientIsX = random.Next(1, 3) == 1;
            nowIsX = random.Next(1, 3) == 1;

            try
            {
                start = false;
                server.Start();
                serverOnline = true;

                using TcpClient cl1 = server.AcceptTcpClient();
                var cl1Handler = new Task(() => HandleClient(cl1, firstClientIsX));
                cl1Handler.Start();

                using TcpClient cl2 = server.AcceptTcpClient();
                var cl2Handler = new Task(() => HandleClient(cl2, !firstClientIsX));
                cl2Handler.Start();


                clientsConnected = true;
                start = true;
                Task.WaitAll([cl1Handler, cl2Handler]);
            }
            catch
            {
                Console.WriteLine("Nastala chyba na straně serveru! (Vyjímka)");
            }
            finally
            {
                server.Stop();
            }
        }

        private void HandleClient(TcpClient cl, bool isX)
        {
            byte[] bytes = new byte[cl.SendBufferSize];
            byte[] res;
            string data;
            NetworkStream stream = cl.GetStream();
            int i;

            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                data = Encoding.Default.GetString(bytes, 0, i);

                if (start)
                {
                    res = data switch
                    {
                        "STATUS" => Encoding.Default.GetBytes(clientsConnected ? "READY" : "LOST-CLIENT"),
                        "CAN-PLAY" => Encoding.Default.GetBytes(((isX && nowIsX) || (!isX && !nowIsX)) ? "YES" : "NO"),
                        "WHOAMI" => Encoding.Default.GetBytes(isX ? "X" : "O"),
                        "SEND-ROW" => row.GetRowInBytes(),
                        "ADD-1" => Encoding.Default.GetBytes(AddOne(isX)),
                        "ADD-2" => Encoding.Default.GetBytes(AddTwo(isX)),
                        "END" => Encoding.Default.GetBytes(row.IsEnd() ? "YES" : "NO"),
                        "WINNER" => Encoding.Default.GetBytes(!row.IsEnd() ? "NONE" : row.Winner() == Square.X ? "X" : "O"),
                        _ => Encoding.Default.GetBytes("BAD"),
                    };
                }
                else
                {
                    res = Encoding.Default.GetBytes("WAIT");
                }
                stream.Write(res, 0, res.Length);
            }
            stream.Close();
            clientsConnected = false;
        }

        private string AddOne(bool isX)
        {
            if ((isX && !nowIsX) || (!isX && nowIsX))
                return "CANNOT";

            row.Add(isX ? Square.X : Square.O, 1);
            nowIsX = !nowIsX;

            return "OK";
        }
        private string AddTwo(bool isX)
        {
            if ((isX && !nowIsX) || (!isX && nowIsX))
                return "CANNOT";

            row.Add(isX ? Square.X : Square.O, 2);
            nowIsX = !nowIsX;

            return "OK";
        }
    }
}