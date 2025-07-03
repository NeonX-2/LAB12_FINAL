using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Firebase.Database;
using Firebase.Database.Query;
using System.Text;

namespace LAB12_FINAL
{
    public class Player
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Level { get; set; }

        [JsonProperty("Gold")]
        public int CurrentGold { get; set; }

        public int Coins { get; set; }

        public bool IsActive { get; set; }

        public int VipLevel { get; set; }

        public string Region { get; set; }

        public DateTime LastLogin { get; set; }
    }

    internal class Program
    {
        
        private static readonly string FirebaseSecret = "VmWIJiMMsDvGXl6ZhSoSMVN6EFdZJpl61HyDG4Lk";
        private static readonly string FirebaseAppUrl = "https://lab12final-default-rtdb.firebaseio.com/";

        private static FirebaseClient firebaseClient;

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            firebaseClient = new FirebaseClient(FirebaseAppUrl, new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(FirebaseSecret)
            });

            
            List<Player> players = await GetPlayersFromJsonAsync("https://raw.githubusercontent.com/NTH-VTC/OnlineDemoC-/refs/heads/main/lab12_players.json");

            if (players == null || !players.Any())
            {
                Console.ReadKey(); 
                return;
            } 
 
            await FindInactivePlayers(players);
            Console.WriteLine("\n"); 

            
            await FindLowLevelPlayers(players);
            Console.WriteLine("\n--------\n");

            
            await AwardVipPlayers(players); 
            Console.WriteLine("\n--------\n");

            
            Console.ReadKey();  
        }

        private static async Task<List<Player>> GetPlayersFromJsonAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string json = await client.GetStringAsync(url);
                    return JsonConvert.DeserializeObject<List<Player>>(json);
                }
                catch (Exception ex) 
                {
                    return null;
                }
            }
        }

        private static async Task FindInactivePlayers(List<Player> players)
        {
            Console.WriteLine("1.1: Tìm người chơi không hoạt động gần đây ");

            DateTime fixedDateNow = new DateTime(2025, 06, 30, 0, 0, 0, DateTimeKind.Utc);
            Console.WriteLine($"So sánh với ngày cố định: {fixedDateNow:yyyy-MM-ddTHH:mm:ssZ} UTC");

            var inactivePlayers = players
                .Where(p => !p.IsActive || (fixedDateNow - p.LastLogin).TotalDays > 5)
                .ToList();

            Console.WriteLine($"Tìm thấy {inactivePlayers.Count} người chơi không hoạt động.");
            Console.WriteLine("Danh sách người chơi không hoạt động:");

            var firebaseDataList = new List<object>();
            if (inactivePlayers.Any())
            {
                foreach (var player in inactivePlayers)
                {
                    Console.WriteLine($"- Tên: {player.Name}, IsActive: {player.IsActive}, LastLogin: {player.LastLogin:yyyy-MM-ddTHH:mm:ssZ}");
                    firebaseDataList.Add(new
                    {
                        Name = player.Name,
                        IsActive = player.IsActive,
                        LastLogin = player.LastLogin.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });
                }
                try
                {
                    await firebaseClient
                        .Child("final_exam_bail1_inactive_players")
                        .PutAsync(firebaseDataList);
                    Console.WriteLine("Dữ liệu người chơi không hoạt động đã được đẩy lên Firebase thành công.");
                }
                catch (Exception)
                {
                    
                }
            }
        }

        private static async Task FindLowLevelPlayers(List<Player> players)
        {
            Console.WriteLine("1.2: Liệt kê người chơi cấp thấp ");
            var lowLevelPlayers = players
                .Where(p => p.Level < 10)
                .ToList();

            Console.WriteLine($"Tìm thấy {lowLevelPlayers.Count} người chơi co level duoi 10.");
            Console.WriteLine("Danh sách người chơi co level duoi 10:");

            var firebaseDataList = new List<object>();

            if (lowLevelPlayers.Any())
            {
                foreach (var player in lowLevelPlayers)
                {
                    Console.WriteLine($"- Name: {player.Name}, Level: {player.Level}, Gold: {player.CurrentGold}");
                    firebaseDataList.Add(new
                    {
                        Name = player.Name,
                        Level = player.Level,
                        CurrentGold = player.CurrentGold
                    });
                }

                try
                {
                    await firebaseClient
                        .Child("final_exam_bail1_low_level_players")
                        .PutAsync(firebaseDataList);
                    Console.WriteLine("Dữ liệu người chơi cấp thấp đã được đẩy lên Firebase thành công.");
                }
                catch (Exception)
                {
                    
                }
            }
        }

        private static async Task AwardVipPlayers(List<Player> players)
        {
            Console.WriteLine(" Bài 2: Tim top 3 nguoi choi VIP co level cao nhat va tinh Gold thuong du kien ");

            var topVipPlayers = players
                .Where(p => p.VipLevel > 0)
                .OrderByDescending(p => p.Level)
                .ThenByDescending(p => p.VipLevel)
                .Take(3)
                .ToList();

            Console.WriteLine($"Tìm thấy {topVipPlayers.Count} người chơi VIP đủ điều kiện nhận thưởng.");
            Console.WriteLine("Top 3 người chơi VIP và phần thưởng của họ:");

            var firebaseDataList = new List<object>();

            if (topVipPlayers.Any())
            {
                for (int i = 0; i < topVipPlayers.Count; i++)
                {
                    var player = topVipPlayers[i];
                    int awardedGold = 0;
                    string rankDescription = "";

                    if (i == 0)
                    {
                        awardedGold = 2000;
                        rankDescription = "Hạng 1";
                    }
                    else if (i == 1)
                    {
                        awardedGold = 1500;
                        rankDescription = "Hạng 2";
                    }
                    else if (i == 2)
                    {
                        awardedGold = 1000;
                        rankDescription = "Hạng 3";
                    }

                    Console.WriteLine($"- {rankDescription}: Tên: {player.Name}, VIP Level: {player.VipLevel}, Level: {player.Level}, Gold: {player.CurrentGold}, Số Gold được thưởng: {awardedGold}");

                    firebaseDataList.Add(new
                    {
                        Name = player.Name,
                        VipLevel = player.VipLevel,
                        Level = player.Level,
                        CurrentGold = player.CurrentGold,
                        AwardedGoldAmount = awardedGold
                    });
                }

                try
                {
                    await firebaseClient
                        .Child("final_exam_bai2_top3_vip_awards")
                        .PutAsync(firebaseDataList);
                    Console.WriteLine("Dữ liệu phần thưởng người chơi VIP đã được đẩy lên Firebase thành công.");
                }
                catch (Exception)
                {
                    
                }
            }
        }
    }
}