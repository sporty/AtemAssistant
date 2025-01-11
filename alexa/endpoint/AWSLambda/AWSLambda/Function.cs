using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Response;
using Amazon.Lambda.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

// シリアライズの自動化の設定
// 現在の主流は以下の通りだが、Alexa.NETが対応していないため、使わない
// [assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
// 古いJSONシリアライザを使用する
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AWSLambda;

public class CreateBroadcastResponse
{
    [JsonPropertyName("stream_id")]
    public string StreamId { get; set; }
}

public class WriteAtemMiniRequest
{
    public string StreamId { get; set; }
}



public class Function
{
    private static readonly HttpClient HttpClient = new HttpClient();

    /// <summary>
    /// takahashi-tribe.ngrok.devにリクエストを送信してライブ配信準備を行う。
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns>SkillResponse</returns>
    public async Task<SkillResponse> FunctionHandler(SkillRequest input, ILambdaContext context)
    {
        string baseUrl = "https://takahashi-tribe.ngrok.dev";
        var progressiveResponse = new ProgressiveResponse(input);

        await progressiveResponse.SendSpeech("ライブ配信準備を開始します");

        try
        {
            context.Logger.LogLine("CreateBroadcast ====================================");
            await progressiveResponse.SendSpeech("YouTube番組を作成しています");

            // POSTリクエストの送信
            HttpResponseMessage response = await HttpClient.PostAsync($"{baseUrl}/api/CreateBroadcast", null);

            if (!response.IsSuccessStatusCode)
            {
                context.Logger.LogLine($"{(int)response.StatusCode}");
                return ResponseBuilder.Tell(
                    $"エラーが発生しました。ステータスコード{(int)response.StatusCode}。 中断します。");
            }

            var prefecturesJsonString = await response.Content.ReadAsStringAsync();

            // JSON文字列をデシリアライズしてList<Prefecture>型のデータに変換
            CreateBroadcastResponse responseJson = JsonSerializer.Deserialize<CreateBroadcastResponse>(prefecturesJsonString);

            if (responseJson is null)
            {
                return ResponseBuilder.Tell("レスポンスのデシリアライズに失敗しました。中断します。");
            }

            // レスポンスの内容を取得
            context.Logger.LogLine($"Response: {responseJson}");

            context.Logger.LogLine("WriteAtemMini ====================================");
            await progressiveResponse.SendSpeech("エイテムミニに書き込みをしています");

            // 家庭内サーバーに送るデータのシリアライズ
            var payload = new WriteAtemMiniRequest()
            {
                StreamId = responseJson.StreamId,
            };
            string jsonPayload = JsonSerializer.Serialize(payload);

            // HTTPリクエストの準備
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // POSTリクエストの送信
            HttpResponseMessage response_2 = await HttpClient.PostAsync($"{baseUrl}/api/WriteAtemMini", content);
            if (!response_2.IsSuccessStatusCode)
            {
                context.Logger.LogLine($"{(int)response_2.StatusCode}");
                return ResponseBuilder.Tell(
                    $"エラーが発生しました。ステータスコード{(int)response_2.StatusCode}。 中断します。");
            }

            // レスポンスの内容を取得
            string responseBody_2 = await response_2.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            // エラーが発生した場合のログ
            context.Logger.LogLine($"Error: {ex.Message}");
            return ResponseBuilder.Tell("エラーが発生したため処理を中断します");
        }

        return ResponseBuilder.Tell("準備完了しました。頑張ってください");
    }


}
