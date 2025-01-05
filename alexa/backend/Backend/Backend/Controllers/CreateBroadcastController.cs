using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CreateBroadcastController : ControllerBase
{
    [HttpPost]
    public IActionResult Post()
    {
        try
        {
            // Backend.exeコマンドを実行
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "Backend.exe",
                    Arguments = "createBroadcast",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };

            process.Start();

            // ストリームIDを取得
            process.WaitForExit();
            string streamId = process.StandardOutput.ReadToEnd().Trim();

            if (string.IsNullOrEmpty(streamId))
            {
                return this.StatusCode(500, "Failed to create broadcast. No stream ID returned.");
            }

            // 成功レスポンス
            return this.Ok(new { stream_id = streamId });
        }
        catch (Exception ex)
        {
            return this.StatusCode(500, $"Error: {ex.Message}");
        }
    }
}