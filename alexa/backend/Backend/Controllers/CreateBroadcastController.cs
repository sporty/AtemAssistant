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
            string executeFilePath = Process.GetCurrentProcess().MainModule.FileName;
            Console.WriteLine($"Execute '{executeFilePath}' to create broadcast.");

            // Backend.exeコマンドを実行
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = executeFilePath,
                    Arguments = "createBroadcast",
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
                return this.StatusCode(500, process.StandardOutput.ReadToEnd());
            }

            // ストリームIDを取得
            var lines = output.Replace("\r\n", "\n").Split(new[] { '\n', '\r' });
            var streamId = lines.LastOrDefault(x => !string.IsNullOrEmpty(x));

            if (string.IsNullOrEmpty(streamId))
            {
                // 失敗
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