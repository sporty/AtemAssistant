using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Response;
using Amazon.Lambda.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

// シリアライズの自動化の設定
// 現在の主流は以下の通りだが、Alexa.NETが対応していないため、使わない
// [assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
// 古いJSONシリアライザを使用する
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AWSLambda;

public class Function
{
    private static readonly HttpClient HttpClient = new HttpClient();

    /// <summary>
    /// takahashi-tribe.ngrok.devにリクエストを送信してライブ配信準備を行う。
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<SkillResponse> FunctionHandler(SkillRequest input, ILambdaContext context)
    {
        string baseUrl = "https://takahashi-tribe.ngrok.dev";
        var progressiveResponse = new ProgressiveResponse(input);

        await progressiveResponse.SendSpeech("ライブ配信準備を開始します");

        try
        {
            context.Logger.LogLine("createBroadcast ====================================");
            await progressiveResponse.SendSpeech("YouTube番組を作成しています");
            //await Task.Delay(5000); // 模擬処理

            // POSTリクエストの送信
            HttpResponseMessage response = await HttpClient.PostAsync($"{baseUrl}/api/createBroadcast", null);

            if (response.IsSuccessStatusCode)
            {
                context.Logger.LogLine($"{(int)response.StatusCode}");
                return ResponseBuilder.Tell("エラーが発生しました。中断します。");
            }

            // レスポンスの内容を取得
            string responseBody = await response.Content.ReadAsStringAsync();
            context.Logger.LogLine($"Response: {responseBody}");

            context.Logger.LogLine("writeAtemMini ====================================");
            await progressiveResponse.SendSpeech("エイテムミニに書き込みをしています");
            // await Task.Delay(5000); // 模擬処理

            // 家庭内サーバーに送るデータのシリアライズ
            var payload = new
            {
                liveStreamId = "",
            };
            string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

            // HTTPリクエストの準備
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // POSTリクエストの送信
            HttpResponseMessage response_2 = await HttpClient.PostAsync($"{baseUrl}/api/writeAtemMini", content);

            // レスポンスの内容を取得
            string responseBody_2 = await response.Content.ReadAsStringAsync();
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
