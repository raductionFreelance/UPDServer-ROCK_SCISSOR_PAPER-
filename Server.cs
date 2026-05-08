using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

class UpdServer
{
    static Random random = new Random();

    static async Task Main(string[] args)
    {
        int port = 1200;
        using (UdpClient udpServer = new UdpClient(port))
        {
            Console.WriteLine("--- НАЛАШТУВАННЯ СЕРВЕРА ---");
            Console.WriteLine("Виберіть режим для СЕРВЕРА: 1 - Людина (ви вводите хід), 2 - Бот");
            int serverMode = int.Parse(Console.ReadLine() ?? "2");

            Console.WriteLine($"\n[UDP] Сервер слухає на порту {port}. Очікування першого ходу від клієнта...");

            int[] matchStats = new int[4]; 
            List<string> gameSummaries = new List<string>();
            IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);

            for (int g = 1; g <= 3; g++)
            {
                Stopwatch gameTimer = Stopwatch.StartNew();
                int playerWins = 0, computerWins = 0;

                for (int r = 1; r <= 5; r++)
                {
                    UdpReceiveResult result = await udpServer.ReceiveAsync();
                    clientEndPoint = result.RemoteEndPoint;
                    int playerMove = int.Parse(Encoding.UTF8.GetString(result.Buffer));

                    int serverMove = GenerateServerChoice(serverMode);

                    if (playerMove >= 1 && playerMove <= 3) matchStats[playerMove]++;
                    if (serverMove >= 1 && serverMove <= 3) matchStats[serverMove]++;

                    string roundResult = "";
                    if (playerMove == 4) { roundResult = "Гравець здався!"; computerWins = 99; break; }

                    if (playerMove == serverMove) roundResult = "Нічия!";
                    else if ((playerMove == 1 && serverMove == 2) || (playerMove == 2 && serverMove == 3) || (playerMove == 3 && serverMove == 1))
                    {
                        roundResult = "Клієнт виграв раунд!";
                        playerWins++;
                    }
                    else
                    {
                        roundResult = "Сервер виграв раунд!";
                        computerWins++;
                    }

                    string roundMessage = $"[Р{r}] Клієнт: {MoveName(playerMove)}, Сервер: {MoveName(serverMove)}. {roundResult}";
                    Console.WriteLine(roundMessage);
                    await Send(udpServer, roundMessage, clientEndPoint);
                }

                gameTimer.Stop();
                string summary = $"Гра №{g}: {(playerWins > computerWins ? "Клієнт" : "Сервер")} виграв ({playerWins}:{computerWins}). Час: {gameTimer.Elapsed.Seconds}с";
                gameSummaries.Add(summary);
                await Send(udpServer, summary, clientEndPoint);
            }

            string finalStats = "\n=== ПІДСУМОК МАТЧУ ===\n" + string.Join("\n", gameSummaries);
            int maxUse = matchStats.Skip(1).Max();
            finalStats += $"\nНайпопулярніша фігура: {MoveName(Array.IndexOf(matchStats, maxUse))}";
            
            await Send(udpServer, finalStats, clientEndPoint);
            Console.WriteLine("Матч завершено. Статистику відправлено.");
        }
    }

    static async Task Send(UdpClient s, string msg, IPEndPoint ep) =>
        await s.SendAsync(Encoding.UTF8.GetBytes(msg), Encoding.UTF8.GetByteCount(msg), ep);

    static string MoveName(int m) => m switch { 1 => "Камінь", 2 => "Ножиці", 3 => "Папір", _ => "Здача" };

    static int GenerateServerChoice(int mode) {
        if (mode == 1) {
            Console.Write("Ваш хід (1-К, 2-Н, 3-П): ");
            return int.Parse(Console.ReadLine() ?? "1");
        }
        return random.Next(1, 4);
    }
}
