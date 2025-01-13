using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using CommandLine;
using Google.Apis.Auth.OAuth2.Flows;


namespace LiveStreamAssistance
{
    [Verb("createBroadcast")]
    class CreateBroadcastSubCommand
    {
        [Option("template", Required = false, Default = "piano", HelpText = "Template name")]
        public string Template { get; set; }
    }

    [Verb("writeAtemMini")]
    class WriteAtemMiniSubCommand
    {
        [Option("stream-id", Required = true, HelpText = "Stream ID")]
        public string StreamId { get; set; }


        [Option("ip-address", Required = false, Default = "10.0.0.3", HelpText = "Stream ID")]
        public string IpAddress { get; set; }
    }

    class BroadcastTemplate
    {
        public string BroadcastTitle { get; set; }

        public string BroadcastDescription { get; set; }

        public string StartTime { get; set; } = "{iso}";

        public string PrivacyStatus { get; set; } = "unlisted";

        public string StreamTitle { get; set; } = "New Stream";

        public string ThumbnailFilename { get; set; }

        public string PlaylistId { get; set; } = String.Empty;

        public string CategoryId { get; set; } = String.Empty;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CreateBroadcastSubCommand, WriteAtemMiniSubCommand>(args)
                .WithParsed<CreateBroadcastSubCommand>(opt =>
                {
                    Task.Run(async () =>
                    {
                        // ファイルに保存されたトークをもとにCredentialを取得
                        var credential = await GetGoogleCredential();
                        Console.WriteLine($"Google Credential: {credential}");

                        var youTube = new YouTubeBroadcast(credential);

                        var templates = new Dictionary<string, BroadcastTemplate>()
                        {
                            {
                                "piano",
                                new BroadcastTemplate
                                {
                                    BroadcastTitle = "ピアノの練習 ({date_ja})",
                                    BroadcastDescription = "毎日のピアノの練習風景\n作成:{datetime}",
                                    ThumbnailFilename = "images/piano_thumbnail.png",
                                    PlaylistId = "PLyhlpUpA8tqyvSstYYdNQImXrVC98YnCH",
                                }
                            },
                            {
                                "guitar",
                                new BroadcastTemplate
                                {
                                    BroadcastTitle = "ギターの練習 ({date_ja})",
                                    BroadcastDescription = "毎日のギターの練習風景\n作成:{datetime}",
                                    ThumbnailFilename = "images/guitar_thumbnail.png",
                                    PlaylistId = "PLyhlpUpA8tqxyv0eZ6yT7g6L8Tq3Yu4fs",
                                }
                            },
                        };

                        var tmp = templates[opt.Template];

                        var liveStream = await youTube.Create(
                            FormatWithStartTime(tmp.BroadcastTitle),
                            FormatWithStartTime(tmp.BroadcastDescription),
                            DateTime.Parse(FormatWithStartTime(tmp.StartTime)),
                            tmp.PrivacyStatus,
                            tmp.StreamTitle, tmp.ThumbnailFilename,
                            tmp.PlaylistId, tmp.CategoryId);

                        if (liveStream is null)
                        {
                            throw new InvalidOperationException("Can't create broadcast");
                        }

                        Console.WriteLine("Live Broadcastとストリーム作成に成功しました");
                        Console.WriteLine(liveStream.Cdn.IngestionInfo.StreamName);
                    }).GetAwaiter().GetResult();
                })
                .WithParsed<WriteAtemMiniSubCommand>(opt2 =>
                {
                    Task.Run(async () =>
                    {
                        var atemMini = new AtemMini()
                        {
                            IpAddress = opt2.IpAddress,
                        };
                        await atemMini.WriteStreamId(opt2.StreamId);
                    }).GetAwaiter().GetResult();
                })
                .WithNotParsed(er =>
                {
                    Console.WriteLine("Run Web Server...");
                    CreateHostBuilder(args).Build().Run();
                });
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });


        public static string FormatWithStartTime(string content)
        {
            // 現在のUTC時間を取得
            DateTime utcNow = DateTime.UtcNow;

            // 日本標準時 (JST) を取得
            TimeZoneInfo jstZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
            DateTime jstNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, jstZone);

            // １０分後を指定する
            var createDateTime = jstNow.AddMinutes(10);

            // 形式を変換して表示
            var keywords = new Dictionary<string, string>()
            {
                ["iso"] = createDateTime.ToString("yyyy-MM-ddTHH:mm:ssK"), // 例: 2025-01-02T15:30:45+09:00
                ["date"] = createDateTime.ToString("yyyy/MM/dd"), // 例: 2025/01/02
                ["datetime"] = createDateTime.ToString("yyyy/MM/dd HH:mm:ss"), // 例: 2025/01/02 15:30:45
                ["date_ja"] = createDateTime.ToString("yyyy年MM月dd日"), // 例: 2025年01月02日
                ["datetime_ja"] = createDateTime.ToString("yyyy年MM月dd日 HH:mm:ss"), // 例: 2025年01月02日 15時30分45秒
            };

            string formatted = content;
            foreach (var keyword in keywords)
            {
                formatted = formatted.Replace("{" + keyword.Key + "}", keyword.Value);
            }

            return formatted;
        }


        static async Task<TokenResponse?> GetAccessTokenAsync(ClientSecrets clientSecrets, string refreshToken)
        {
            try
            {
                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = clientSecrets,
                });

                Console.WriteLine("Getting access token...");
                // リフレッシュトークンを使って新しいアクセストークンを取得
                var tokenResponse = await flow.RefreshTokenAsync("rt.sporty@gmail.com", refreshToken, default)
                    .ConfigureAwait(false);
                Console.WriteLine($"TokenResponse is {tokenResponse}");

                return tokenResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return null;
            }
        }

        public static async Task<GoogleCredential> GetGoogleCredential()
        {
            // client_secret.jsonから読み込み
            var clientSecretFilename = Path.Combine(Environment.CurrentDirectory, "client_secret.json");
            var clientSecrets = GoogleClientSecrets.FromFile(clientSecretFilename).Secrets;

            // 保存されているリフレッシュトークンを読み込み
            var fileDataStore = new FileDataStore(Environment.CurrentDirectory, true);
            var token = await fileDataStore.GetAsync<string>("GoogleToken");

            // リフレッシュトークンがない場合は終了
            if (token == null)
            {
                throw new InvalidOperationException("Refresh token not found.");
            }

            var tokenResponse = await GetAccessTokenAsync(clientSecrets, token);
            if (tokenResponse == null)
            {
                throw new InvalidOperationException("Can't get access token.");
            }

            return GoogleCredential.FromAccessToken(tokenResponse.AccessToken);
        }
    }
}