using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace LiveStreamAssistance;

public class YouTubeStream
{
    public GoogleCredential Credential { get; set; }

    public void print_categories()
    {
    }

    public async Task<List<string>> print_playlists()
    {
        // GoogleCredential cred = await auth.GetCredentialAsync();

        var service = new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = this.Credential,
        });
        var listRequest = service.Playlists.List(part: "id,snippet,status");

        // 引数で指定できないオプションをプロパティで設定する。
        // こういうことはよくあるみたい
        listRequest.Mine = true;
        listRequest.MaxResults = 50;

        var playlists = await listRequest.ExecuteAsync();
        var fileNames = playlists.Items.Select(p => p.Snippet.Title).ToList();

        return fileNames;
    }

    private async Task<LiveBroadcast> InsertBroadcast(
        string broadcastTitle, string description,
        DateTime? startTime,
        string privacyStatus)
    {
        var service = new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = this.Credential,
            ApplicationName = "Live Streaming Assistance",
        });

        var liveBroadcast = await service.LiveBroadcasts.Insert(
            part: "snippet,status,contentDetails",
            body: new LiveBroadcast()
            {
                Snippet = new LiveBroadcastSnippet()
                {
                    Title = broadcastTitle,
                    Description = description,
                    ScheduledStartTimeDateTimeOffset = startTime,
                },
                Status = new LiveBroadcastStatus()
                {
                    PrivacyStatus = privacyStatus,
                    SelfDeclaredMadeForKids = false,
                },
                ContentDetails = new LiveBroadcastContentDetails()
                {
                    EnableAutoStart = true,
                    EnableAutoStop = true,
                },
            }
        ).ExecuteAsync();

        return liveBroadcast;
    }

    private async Task<LiveBroadcast> BindBroadcast(LiveBroadcast broadcast, LiveStream stream)
    {
        var service = new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = this.Credential,
            ApplicationName = "Live Streaming Assistance",
        });

        var bindRequest = service.LiveBroadcasts.Bind(part: "id,contentDetails", id: broadcast.Id);
        bindRequest.StreamId = stream.Id;

        var response = await bindRequest.ExecuteAsync();

        return response;
    }

    private async Task<LiveStream> InsertStream(string streamTitle, LiveBroadcast broadcast)
    {
        var service = new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = this.Credential,
            ApplicationName = "Live Streaming Assistance",
        });

        var response = await service.LiveStreams.Insert(
            part: "snippet,cdn",
            body: new LiveStream()
            {
                Snippet = new LiveStreamSnippet()
                {
                    Title = streamTitle,
                },
                Cdn = new CdnSettings()
                {
                    IngestionType = "rtmp",
                    Resolution = "1080p",
                    FrameRate = "60fps",
                },
            }).ExecuteAsync();

        return response;
    }

    private ThumbnailsResource.SetMediaUpload UploadThumbnail(LiveBroadcast broadcast,
        string thumbnail)
    {
        var service = new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = this.Credential,
            ApplicationName = "Live Streaming Assistance",
        });

        using (var stream = new FileStream(thumbnail, FileMode.Open, FileAccess.Read))
        {
            var response = service.Thumbnails.Set(
                videoId: broadcast.Id,
                stream: stream,
                contentType: "application/octet-stream");
            return response;
        }
    }

    public async Task<LiveStream> CreateStream(
        string broadcastTitle, string description, DateTime startTime, string privacyStatus,
        string streamTitle,
        string thumbnail,
        string playlistId, string category
    )
    {
        // var credential = await this.Auth();

        if (this.Credential is null)
        {
            return null;
        }

        //print_categories(credential);
        var playlists = await this.print_playlists();
        foreach (var playlist in playlists)
        {
            Console.WriteLine(playlist);
        }

        var liveBroadcast = await this.InsertBroadcast(broadcastTitle, description, startTime, privacyStatus);
        var liveStream = await this.InsertStream(streamTitle, liveBroadcast);
        var _ = await this.BindBroadcast(liveBroadcast, liveStream);

        // this.UploadThumbnail(liveBroadcast, thumbnail);

        return liveStream;
    }
}