using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WriteAtemMiniController : ControllerBase
{
    [HttpPost]
    public IActionResult Post([FromBody] StreamIdRequest request)
    {
        if (string.IsNullOrEmpty(request.StreamId))
        {
            return this.BadRequest("Stream ID is required.");
        }

        try
        {
            // Backend.exeコマンドを実行
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "Backend.exe",
                    Arguments = $"writeAtemMini --stream-id {request.StreamId}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };

            process.Start();
            process.WaitForExit();

            // 成功レスポンス
            return this.Ok("Stream ID written to ATEM Mini successfully.");
        }
        catch (Exception ex)
        {
            return this.StatusCode(500, $"Error: {ex.Message}");
        }
    }

    public class StreamIdRequest
    {
        public string StreamId { get; set; }
    }
}