using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics; 
using System.Collections.Generic;
using System.Linq; 

class UpdClientServer
{
    static Random random = new Random();

    static async Task Main(string[] args)
    {
        int port = 1200;
        using (UdpClient udpServer = new UdpClient(port))
        {
            Console.WriteLine($"[UDP] Сервер запущено. Очікування вибору режиму...");

            UdpReceiveResult initResult = await udpServer.ReceiveAsync();
            int gameMode = int.Parse(Encoding.UTF8.GetString(initResult.Buffer));
            IPEndPoint clientEndPoint = initResult.RemoteEndPoint;

            int[] matchStats = new int[4]; 
            List<string> gameSummaries = new List<string>();

            for (int g = 1; g <= 3; g++)
            {
                Console.WriteLine($"\n--- ПОЧАТОК ГРИ №{g} ---");
                Stopwatch gameTimer = Stopwatch.StartNew();
                int playerWins = 0;
                int computerWins = 0;

                for (int r = 1; r <= 5; r++)
                {
                    UdpReceiveResult roundResult = await udpServer.ReceiveAsync();
                    int playerMove = int.Parse(Encoding.UTF8.GetString(roundResult.Buffer));
                    clientEndPoint = roundResult.RemoteEndPoint;

                    int serverMove = GenerataChoice(gameMode);

                    if (playerMove >= 1 && playerMove <= 3) matchStats[playerMove]++;
                    if (serverMove >= 1 && serverMove <= 3) matchStats[serverMove]++;

                    string resultText = "";
                    if (playerMove == 4) { resultText = "Гравець здався!"; computerWins = 99; break; }

                    if (playerMove == serverMove) resultText = "Нічия!";
                    else if ((playerMove == 1 && serverMove == 2) ||
                             (playerMove == 2 && serverMove == 3) ||
                             (playerMove == 3 && serverMove == 1))
                    {
                        resultText = "Ви виграли раунд!";
                        playerWins++;
                    }
                    else
                    {
                        resultText = "Сервер виграв раунд!";
                        computerWins++;
                    }

                    string roundStats = $"[Раунд {r}] Ви: {MoveName(playerMove)}, Сервер: {MoveName(serverMove)}. {resultText}";
                    Console.WriteLine(roundStats);
                    await Send(udpServer, roundStats, clientEndPoint);
                }

                gameTimer.Stop();
                string gameResult = playerWins > computerWins ? "ПЕРЕМОГА ГРАВЦЯ" : "ПЕРЕМОГА СЕРВЕРА";
                string gameStat = $"Результат гри №{g}: {gameResult} ({playerWins}:{computerWins}). Час: {gameTimer.Elapsed.Seconds} сек.";

                gameSummaries.Add(gameStat);
                Console.WriteLine(gameStat);
                await Send(udpServer, gameStat, clientEndPoint);
            }

            string finalMatchStats = "\n=== СТАТИСТИКА МАТЧУ ===\n" + string.Join("\n", gameSummaries);

            int maxUse = matchStats.Skip(1).Max();
            int minUse = matchStats.Skip(1).Min();
            string popular = MoveName(Array.IndexOf(matchStats, maxUse));
            string unpopular = MoveName(Array.LastIndexOf(matchStats, minUse));

            finalMatchStats += $"\nНайпопулярніша фігура: {popular}\nНайнепопулярніша: {unpopular}";

            Console.WriteLine(finalMatchStats);
            await Send(udpServer, finalMatchStats, clientEndPoint);
        }
    }

    static async Task Send(UdpClient s, string msg, IPEndPoint ep)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg);
        await s.SendAsync(data, data.Length, ep);
    }

    static string MoveName(int m) => m switch { 1 => "Камінь", 2 => "Ножиці", 3 => "Папір", _ => "Невідомо" };

    public static int GenerataChoice(int i)
    {
        if (i == 1)
        {
            Console.WriteLine("Ваш хід (1-К, 2-Н, 3-П): ");
            return int.Parse(Console.ReadLine() ?? "1");
        }
        return random.Next(1, 4);
    }
}
