using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
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

// �V���A���C�Y�̎������̐ݒ�
// ���݂̎嗬�͈ȉ��̒ʂ肾���AAlexa.NET���Ή����Ă��Ȃ����߁A�g��Ȃ�
// [assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
// �Â�JSON�V���A���C�U���g�p����
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AWSLambda;

public class CreateBroadcastRequest
{
    public string Template { get; set; }
}
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
    /// takahashi-tribe.ngrok.dev�Ƀ��N�G�X�g�𑗐M���ă��C�u�z�M�������s���B
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns>SkillResponse</returns>
    public async Task<SkillResponse> FunctionHandler(SkillRequest input, ILambdaContext context)
    {
        string baseUrl = "https://takahashi-tribe.ngrok.dev";
        var progressiveResponse = new ProgressiveResponse(input);
        var template = "piano";

        var intentRequest = input.Request as IntentRequest;
        if (intentRequest?.Intent.Name == "PrepareLiveStreamingIntent")
        {
            // �X���b�g�l���擾
            var musicalInstrumentSlot = intentRequest.Intent.Slots["musicalInstrument"]?.Value;
            if (musicalInstrumentSlot == "�M�^�[")
            {
                template = "guitar";
            }
        }

        await progressiveResponse.SendSpeech("���C�u�z�M�������J�n���܂�");
        await progressiveResponse.SendSpeech($"�e���v���[�g��{template}�ł�");

        try
        {
            context.Logger.LogLine("CreateBroadcast ====================================");
            await progressiveResponse.SendSpeech("YouTube�ԑg���쐬���Ă��܂�");

            // �ƒ���T�[�o�[�ɑ���f�[�^�̃V���A���C�Y
            var createBroadcastRequest = new CreateBroadcastRequest()
            {
                Template = template,
            };
            string jsonPayload = JsonSerializer.Serialize(createBroadcastRequest);

            // HTTP���N�G�X�g�̏���
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // POST���N�G�X�g�̑��M
            HttpResponseMessage response = await HttpClient.PostAsync($"{baseUrl}/api/CreateBroadcast", content);

            if (!response.IsSuccessStatusCode)
            {
                context.Logger.LogLine($"{(int)response.StatusCode}");
                return ResponseBuilder.Tell(
                    $"�G���[���������܂����B�X�e�[�^�X�R�[�h{(int)response.StatusCode}�B ���f���܂��B");
            }

            var prefecturesJsonString = await response.Content.ReadAsStringAsync();

            // JSON��������f�V���A���C�Y����List<Prefecture>�^�̃f�[�^�ɕϊ�
            CreateBroadcastResponse responseJson = JsonSerializer.Deserialize<CreateBroadcastResponse>(prefecturesJsonString);

            if (responseJson is null)
            {
                return ResponseBuilder.Tell("���X�|���X�̃f�V���A���C�Y�Ɏ��s���܂����B���f���܂��B");
            }

            // ���X�|���X�̓��e���擾
            context.Logger.LogLine($"Response: {responseJson}");

            context.Logger.LogLine("WriteAtemMini ====================================");
            await progressiveResponse.SendSpeech("�G�C�e���~�j�ɏ������݂����Ă��܂�");

            // �ƒ���T�[�o�[�ɑ���f�[�^�̃V���A���C�Y
            var writeAtemMiniRequest = new WriteAtemMiniRequest()
            {
                StreamId = responseJson.StreamId,
            };
            string jsonWriteAtemMiniRequest = JsonSerializer.Serialize(writeAtemMiniRequest);

            // HTTP���N�G�X�g�̏���
            var writeAtemMiniContent = new StringContent(jsonWriteAtemMiniRequest, Encoding.UTF8, "application/json");

            // POST���N�G�X�g�̑��M
            HttpResponseMessage writeAtemMiniResponse = await HttpClient.PostAsync($"{baseUrl}/api/WriteAtemMini", writeAtemMiniContent);
            if (!writeAtemMiniResponse.IsSuccessStatusCode)
            {
                context.Logger.LogLine($"{(int)writeAtemMiniResponse.StatusCode}");
                return ResponseBuilder.Tell(
                    $"�G���[���������܂����B�X�e�[�^�X�R�[�h{(int)writeAtemMiniResponse.StatusCode}�B ���f���܂��B");
            }

            // ���X�|���X�̓��e���擾
            string responseBody_2 = await writeAtemMiniResponse.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            // �G���[�����������ꍇ�̃��O
            context.Logger.LogLine($"Error: {ex.Message}");
            return ResponseBuilder.Tell("�G���[�������������ߏ����𒆒f���܂�");
        }

        return ResponseBuilder.Tell("�����������܂����B�撣���Ă�������");
    }


}
