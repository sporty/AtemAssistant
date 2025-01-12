using System;
using System.Threading.Tasks;
using LibAtem.Net;
using LibAtem.Commands.Streaming;

namespace LiveStreamAssistance;

public class AtemMini
{
    public string IpAddress = "10.0.0.9";
    public TimeSpan timeOut = TimeSpan.FromSeconds(5);

    public async Task<string> WriteStreamId(string streamId)
    {
        // ATEM 接続インスタンス
        var atem = new AtemClient(this.IpAddress);

        // 状態更新完了用のタスク完了ソース
        var tcs = new TaskCompletionSource<bool>();

        atem.OnReceive += (sender, commands) =>
        {
            var getCommand = commands.FirstOrDefault(x => x is StreamingServiceGetCommand);
            if (getCommand is StreamingServiceGetCommand getCommandObj)
            {
                Console.WriteLine($"Current Setting: url={getCommandObj.Url}, key={getCommandObj.Key}");

                if (getCommandObj.Key == streamId)
                {
                    // 状態更新完了を通知
                    Console.WriteLine($"StreamId is matched.");
                    tcs.SetResult(true);
                    return;
                }

                if (sender is AtemClient atemClient)
                {
                    var mask = StreamingServiceSetCommand.MaskFlags.Key;
                    var setCommand = new StreamingServiceSetCommand()
                    {
                        Mask = mask,
                        Key = streamId,
                    };
                    Console.WriteLine($"SendCommand key: {streamId}");
                    atemClient.SendCommand(setCommand);
                }
            }
        };

        atem.OnConnection += (sender) =>
        {
            try
            {
                // 現在のStreaming.Settings.Keyを表示
                //Console.WriteLine($"Current Streaming Key: {state.Streaming.Settings.Key}");

                // Streaming.Settings.Keyを変更

                // コマンド送信
                /*
                var command = new StreamingServiceSetCommand()
                {
                    Key = streamId,
                };
                atem.SendCommand(command);
                 */
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating streaming key: {ex.Message}");
                tcs.SetException(ex);
            }
        };

        // ATEM Miniに接続
        Console.WriteLine($"Connecting to ATEM Mini at {this.IpAddress} to write stream key {streamId}...");
        try
        {
            atem.Connect();

            // 状態更新が完了するのを待機
            bool result = await WaitWithTimeout(tcs.Task, this.timeOut);
            if (!result)
            {
                Console.WriteLine("タイムアウトしました。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to ATEM Mini: {ex.Message}");
        }
        finally
        {
            // 接続を終了
            atem.Dispose();
            Console.WriteLine("Connection closed.");
        }

        return "Finished.";
    }

    static async Task<bool> WaitWithTimeout(Task task, TimeSpan timeout)
    {
        // タイムアウトタスクを作成
        var timeoutTask = Task.Delay(timeout);

        // 完了タスクを待機
        var completedTask = await Task.WhenAny(task, timeoutTask);

        // 完了タスクが元のタスクかタイムアウトかを判定
        if (completedTask == task)
        {
            await task; // 必要に応じて元のタスクの結果を取得
            return true; // タスク完了
        }
        else
        {
            return false; // タイムアウト
        }
    }
}