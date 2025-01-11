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

// �V���A���C�Y�̎������̐ݒ�
// ���݂̎嗬�͈ȉ��̒ʂ肾���AAlexa.NET���Ή����Ă��Ȃ����߁A�g��Ȃ�
// [assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
// �Â�JSON�V���A���C�U���g�p����
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AWSLambda;

public class Function
{
    private static readonly HttpClient HttpClient = new HttpClient();

    /// <summary>
    /// takahashi-tribe.ngrok.dev�Ƀ��N�G�X�g�𑗐M���ă��C�u�z�M�������s���B
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<SkillResponse> FunctionHandler(SkillRequest input, ILambdaContext context)
    {
        string baseUrl = "https://takahashi-tribe.ngrok.dev";
        var progressiveResponse = new ProgressiveResponse(input);

        await progressiveResponse.SendSpeech("���C�u�z�M�������J�n���܂�");

        try
        {
            context.Logger.LogLine("createBroadcast ====================================");
            await progressiveResponse.SendSpeech("YouTube�ԑg���쐬���Ă��܂�");
            //await Task.Delay(5000); // �͋[����

            // POST���N�G�X�g�̑��M
            HttpResponseMessage response = await HttpClient.PostAsync($"{baseUrl}/api/createBroadcast", null);

            if (response.IsSuccessStatusCode)
            {
                context.Logger.LogLine($"{(int)response.StatusCode}");
                return ResponseBuilder.Tell("�G���[���������܂����B���f���܂��B");
            }

            // ���X�|���X�̓��e���擾
            string responseBody = await response.Content.ReadAsStringAsync();
            context.Logger.LogLine($"Response: {responseBody}");

            context.Logger.LogLine("writeAtemMini ====================================");
            await progressiveResponse.SendSpeech("�G�C�e���~�j�ɏ������݂����Ă��܂�");
            // await Task.Delay(5000); // �͋[����

            // �ƒ���T�[�o�[�ɑ���f�[�^�̃V���A���C�Y
            var payload = new
            {
                liveStreamId = "",
            };
            string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

            // HTTP���N�G�X�g�̏���
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // POST���N�G�X�g�̑��M
            HttpResponseMessage response_2 = await HttpClient.PostAsync($"{baseUrl}/api/writeAtemMini", content);

            // ���X�|���X�̓��e���擾
            string responseBody_2 = await response.Content.ReadAsStringAsync();
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
