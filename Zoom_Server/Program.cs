using Zoom_Server.Logging;
using Zoom_Server.Net;

namespace Zoom_Server
{
    internal class Program
    {
        //internal static Serverrrrr server;
        internal static UdpServer server;

        static string serverIP = "127.0.0.1";
        static int serverPort = 9999;

        static void Main(string[] args)
        {
            server = new UdpServer(serverIP, serverPort, new LoggerWithConsoleAndTime());

            server.Run();
            Console.WriteLine("Server started");

            while (Console.ReadLine() != "exit") { }

            server.Stop();

            Console.ReadLine();
        }
    }
}
