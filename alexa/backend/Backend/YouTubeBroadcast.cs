using System.Runtime.CompilerServices;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace LiveStreamAssistance;

public class YouTubeBroadcast
{
    public YouTubeService Service { get; set; }

    public GoogleCredential Credential { get; set; }

    public YouTubeBroadcast(GoogleCredential credentaial)
    {
        this.Credential = credentaial;

        this.Service = new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = this.Credential,
        });
    }

    public async Task<bool> EchoCategories()
    {
        return true;
    }

    public async Task<bool> EchoPlaylists()
    {
        // GoogleCredential cred = await auth.GetCredentialAsync();

        var listRequest = this.Service.Playlists.List(part: "id,snippet,status");

        // 引数で指定できないオプションをプロパティで設定する。
        // こういうことはよくあるみたい
        listRequest.Mine = true;
        listRequest.MaxResults = 50;

        var playlists = await listRequest.ExecuteAsync();
        foreach (var line in playlists.Items.Select(p => $"{p.Snippet.Title} : {p.Id}").ToList())
        {
            Console.WriteLine(line);
        }

        return true;
    }

    public async Task<bool> AddToPlaylist(string playlistId, LiveBroadcast liveBroadcast)
    {
        var listRequest = this.Service.Playlists.List(part: "id,snippet,status");
        listRequest.Mine = true;
        listRequest.MaxResults = 50;

        var playlists = await listRequest.ExecuteAsync();
        var title = playlists.Items.FirstOrDefault(x => x.Id == playlistId)?.Snippet.Title;
        Console.WriteLine($"プレイリスト'{title}'に追加します");

        /*
        var playlistItemsRequest = this.Service.PlaylistItems.List(part: "id,snippet,status");
        playlistItemsRequest.PlaylistId = playlistId;
        var playlistItems = await playlistItemsRequest.ExecuteAsync();
        Console.WriteLine(playlistItems);
         */

        var newPlaylistItem = new PlaylistItem()
        {
            Snippet = new PlaylistItemSnippet()
            {
                PlaylistId = playlistId,
                ResourceId = new ResourceId()
                {
                    Kind = "youtube#video",
                    VideoId = liveBroadcast.Id,
                },
            },
        };
        var insertPlaylistItemsRequest = this.Service.PlaylistItems.Insert(newPlaylistItem, part: "id,snippet,status");
        var result = await insertPlaylistItemsRequest.ExecuteAsync();

        return true;
    }

    private async Task<LiveBroadcast> InsertBroadcast(string broadcastTitle, string description, DateTime startTime,
        string privacyStatus)
    {
        var liveBroadcast = await this.Service.LiveBroadcasts.Insert(
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
            }).ExecuteAsync();

        Console.WriteLine($"'{liveBroadcast.Snippet.Title}'を作成しました");
        return liveBroadcast;
    }

    private async Task<LiveBroadcast> BindBroadcast(LiveBroadcast broadcast, LiveStream stream)
    {
        var bindRequest = this.Service.LiveBroadcasts.Bind(part: "id,contentDetails", id: broadcast.Id);
        bindRequest.StreamId = stream.Id;

        var response = await bindRequest.ExecuteAsync();

        return response;
    }

    private async Task<LiveStream> InsertStream(string streamTitle, LiveBroadcast broadcast)
    {
        var response = await this.Service.LiveStreams.Insert(
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

        Console.WriteLine($"'{response.Snippet.Title}'を作成しました");

        return response;
    }

    private async Task<bool> UploadThumbnail(LiveBroadcast broadcast, string thumbnailFilename)
    {
        using (var stream = new FileStream(thumbnailFilename, FileMode.Open, FileAccess.Read))
        {
            var request = this.Service.Thumbnails.Set(
                videoId: broadcast.Id,
                stream: stream,
                contentType: "application/octet-stream");

            var result = await request.UploadAsync();
            if (result.Status == UploadStatus.Failed)
            {
                Console.WriteLine($"サムネイル画像のアップロードに失敗しました。\n{result.Exception.Message}");
                return false;
            }

            Console.WriteLine($"サムネイル画像 '{thumbnailFilename}' をアップロードしました");
            return true;
        }
    }

    public async Task<LiveStream> Create(string broadcastTitle, string description, DateTime startTime,
        string privacyStatus, string streamTitle, string thumbnailFilename, string playlistId, string category)
    {
        if (this.Credential is null)
        {
            return null;
        }

        // ブロードキャスト作成
        var liveBroadcast = await this.InsertBroadcast(broadcastTitle, description, startTime, privacyStatus);

        // サムネイルアップロード
        await this.UploadThumbnail(liveBroadcast, thumbnailFilename);

        // プレイリストへの追加
        await this.AddToPlaylist(playlistId, liveBroadcast);

        // ストリーム作成とブロードキャストへのバインド
        var liveStream = await this.InsertStream(streamTitle, liveBroadcast);
        var _ = await this.BindBroadcast(liveBroadcast, liveStream);

        return liveStream;
    }
}