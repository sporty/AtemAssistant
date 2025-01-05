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
        [Option('s', "secretFile", Required = false, HelpText = "secret file path")]
        public string SecretFile { get; set; }

        [Option("broadcast-title", Required = false, Default = "New Broadcast ({date_ja})",
            HelpText = "Broadcast Title")]
        public string BroadcastTitle { get; set; }

        [Option("broadcast-description", Required = false, Default = "This is New Broadcast ({datetime})",
            HelpText = "Broadcaast Description")]
        public string BroadcastDescription { get; set; }

        [Option("privacy-status", Required = false, Default = "unlisted",
            HelpText = "Broadcast privacy status (private|public|unlisted)")]
        public string PrivacyStatus { get; set; }

        [Option("start-time", Required = false, Default = "{iso}", HelpText = "Scheduled start time")]
        public string StartTime { get; set; }

        [Option("stream-title", Required = false, Default = "New Stream", HelpText = "Stream Title")]
        public string StreamTitle { get; set; }

        [Option("playlist-id", Required = false, HelpText = "Playlist ID")]
        public string PlaylistId { get; set; }

        [Option("category-id", Required = false, HelpText = "Category ID")]
        public string CategoryId { get; set; }

        [Option("thumbnail", Required = false, Default = "./images/thumbnail.png",
            HelpText = "Thumbnail image filename")]
        public string Thumbnail { get; set; }
    }

    [Verb("writeAtemMini")]
    class WriteAtemMiniSubCommand
    {
        [Option('i', "streamId", Required = true, HelpText = "Stream ID")]
        public string StreamId { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(args);
            Parser.Default.ParseArguments<CreateBroadcastSubCommand, WriteAtemMiniSubCommand>(args)
                .WithParsed<CreateBroadcastSubCommand>(opt =>
                {
                    Task.Run(async () =>
                    {
                        // ファイルに保存されたトークをもとにCredentialを取得
                        var credential = await GetGoogleCredential();
                        Console.WriteLine($"Google Credential: {credential}");

                        var youTubeStream = new YouTubeStream()
                        {
                            Credential = credential,
                        };

                        var liveStream = await youTubeStream.CreateStream(
                            FormatWithStartTime(opt.BroadcastTitle),
                            FormatWithStartTime(opt.BroadcastDescription),
                            DateTime.Parse(FormatWithStartTime(opt.StartTime)),
                            opt.PrivacyStatus,
                            opt.StreamTitle, opt.Thumbnail,
                            opt.PlaylistId, opt.CategoryId);

                        Console.WriteLine("Live Broadcastとストリーム作成に成功しました");
                        Console.WriteLine(liveStream.Id);
                    }).GetAwaiter().GetResult();
                })
                .WithParsed<WriteAtemMiniSubCommand>(async opt2 =>
                {
                    //args[2]
                    /*
                    var atemMini = new AtemMini();
                    await atemMini.Write(args[2]);
                     */
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
            Console.WriteLine($"現在の日本時間: {jstNow}");

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
                var tokenResponse = await flow.RefreshTokenAsync("rt.sporty@gmail.com", refreshToken, default).ConfigureAwait(false);
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
            const string clientSecretFilenameKey = "TEST_WEB_CLIENT_SECRET_FILENAME";
            var envValue = Environment.GetEnvironmentVariable(clientSecretFilenameKey);
            var clientSecrets = GoogleClientSecrets.FromFile(envValue).Secrets;

            // 保存されているリフレッシュトークンを読み込み
            var fileDataStore = new FileDataStore(@"C:\work\AtemAssistant\alexa\backend\Backend\Backend", true);
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