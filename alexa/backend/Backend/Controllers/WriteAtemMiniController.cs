using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WriteAtemMiniController : ControllerBase
{
    [HttpPost]
    public IActionResult Post([FromBody] RequestBody request)
    {
        if (string.IsNullOrEmpty(request.StreamId))
        {
            return this.BadRequest("Stream ID is required.");
        }

        try
        {
            string executeFilePath = Process.GetCurrentProcess().MainModule.FileName;
            Console.WriteLine($"Execute '{executeFilePath}' to write stream ID..");

            // Backend.exeコマンドを実行
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = executeFilePath,
                    Arguments = $"writeAtemMini --stream-id {request.StreamId}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };

            process.Start();
            process.WaitForExit();

            var output = process.StandardOutput.ReadToEnd();
            Console.WriteLine(output);
            if (process.ExitCode != 0)
            {
                // 失敗
                return this.StatusCode(500, output);
            }

            // 成功レスポンス
            return this.Ok("Stream ID written to ATEM Mini successfully.");
        }
        catch (Exception ex)
        {
            return this.StatusCode(500, $"Error: {ex.Message}");
        }
    }

    public class RequestBody
    {
        public string StreamId { get; set; }
    }
}