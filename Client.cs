using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class UdpClientApp
{
    static Random random = new Random();

    static async Task Main(string[] args)
    {
        string serverIp = "127.0.0.1";
        int port = 1200;

        using (UdpClient udpClient = new UdpClient())
        {
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(serverIp), port);
            
            Console.WriteLine("Виберіть режим: 1 - Людина, 2 - Бот");
            int clientMode = int.Parse(Console.ReadLine() ?? "1");

            for (int g = 1; g <= 3; g++)
            {
                for (int r = 1; r <= 5; r++)
                {
                    int myMove;
                    if (clientMode == 1) {
                        Console.WriteLine($"\n[Гра {g}, Раунд {r}] Ваш хід (1-К, 2-Н, 3-П, 4-Здача):");
                        myMove = int.Parse(Console.ReadLine() ?? "1");
                    } else {
                        myMove = random.Next(1, 4);
                        Console.WriteLine($"\n[Бот Клієнт] вибрав: {myMove}");
                    }

                    byte[] data = Encoding.UTF8.GetBytes(myMove.ToString());
                    await udpClient.SendAsync(data, data.Length, serverEP);

                    if (myMove == 4) break;

                    await Receive(udpClient);
                }
                await Receive(udpClient);
            }
            await Receive(udpClient);
        }
    }

    static async Task Receive(UdpClient client)
    {
        var result = await client.ReceiveAsync();
        Console.WriteLine($">>> {Encoding.UTF8.GetString(result.Buffer)}");
    }
}
