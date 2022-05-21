﻿using System;
using Discord;
using Discord.WebSocket;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Linq;
using System.Reflection;

class Program
{
    private readonly string prefix = "c!";

    private string token="SAMPLETOKEN";
    // token = Environment.GetEnvironmentVariable("token");
    private readonly DiscordSocketClient _client;
    static void Main(string[] args)
        => new Program()
            .MainAsync()
            .GetAwaiter()
            .GetResult();

    public Program()
    {
        _client = new DiscordSocketClient();

        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += MessageReceivedAsync;
        _client.InteractionCreated += InteractionCreatedAsync;
    }


    private static readonly string[] DefaultImports =
    {
    "System",
    "System.IO",
    "System.Linq",
    "System.Collections.Generic",
    "System.Text",
    "System.Net",
    "System.Net.Http",
    "System.Threading",
    "System.Threading.Tasks",
    "Discord.Net"
    };

    public async Task MainAsync()
    {
        
        Task task = Task.Run(() =>
        {
            SimpleWebServer.MMain();
        });
        await _client.LoginAsync(TokenType.Bot, token);

        await _client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        Console.WriteLine($"{_client.CurrentUser} is connected!");
        await _client.SetGameAsync(prefix + "help", null, ActivityType.Watching);
    }
    private async Task MessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.Id == _client.CurrentUser.Id)
            return;
        if (message.Content.IndexOf("c!") == -1) { return; }

        string[] args = message.Content.Substring(prefix.Length).Split(' ');
        if (args[0] == "ping")
        {
            DateTimeOffset timenow = DateTimeOffset.Now;
            var time = message.Timestamp;
            var sa = timenow - time;
            await message.Channel.SendMessageAsync("Pong with DateTimeOffset" + sa);

        }
        else if (args[0] == "help")
        {
            var embeds = new EmbedBuilder();
            embeds.WithTitle("help c#のevalbot");
            embeds.WithDescription(prefix + "eval [内容]で普通に実行");
            await message.Channel.SendMessageAsync("", embed: embeds.Build());
        }

        else if (args[0] == "eval" || args[0] == "run"||message.Content.Substring(prefix.Length,prefix.Length+1)=="run")
        {
            await EvalCommand(message, args[0]);
        }
        else if (args[0] == "run")
        {
            var replymessage = message.Reference;

            //   replymessage.
            var messageme = await message.Channel.SendMessageAsync(
                  "", embed: CommandEmbedBuilder("コードのコンパイル中..", "")
              );
            string a = eval(message.Content.ToString(), message).Result;
            Embed result = CommandEmbedBuilder("a", "a");
            if (a == "error:null")
            {
                result = CommandEmbedBuilder(
                    "エラー",
                    "Scriptを入力してください"
                );
            }
            else if (a.IndexOf("error CS") != -1)
            {
                result = CommandEmbedBuilder(
                    "コンパイルエラー",
                    a
                );
            }
            else
            {
                result = CommandEmbedBuilder(
                    "成功しました",
                    a
                );
            }
            await messageme.ModifyAsync((mes) =>
            {
                mes.Embed = result;
            });
        }

    }
    private async Task InteractionCreatedAsync(SocketInteraction interaction)
    {

    }
    private async Task EvalCommand(SocketMessage Message, string command)
    {
        var message = await Message.Channel.SendMessageAsync(
              "", embed: CommandEmbedBuilder("コードのコンパイル中..", "")
          );
        string a;
        if (command == "eval")
        {
            a = eval(Message.Content.Substring(prefix.Length + 4), Message).Result;
        }
        else if (command == "run"||Message.Content.Substring(prefix.Length,prefix.Length+1)=="run")
        {
            a = RunCSharpAsync(Message.Content.Substring(prefix.Length + 3), Message).Result;
        }
        else
        {
            a = "error";
        }

        Embed result = CommandEmbedBuilder("a", "a");
        if (a == "error:null")
        {
            result = CommandEmbedBuilder(
                "エラー",
                "Scriptを入力してください"
            );
        }
        else if (a.IndexOf("error CS") != -1)
        {
            result = CommandEmbedBuilder(
                "コンパイルエラー",
                a
            );
        }
        else
        {
            result = CommandEmbedBuilder(
                "成功しました",
                a
            );
        }
        await message.ModifyAsync((mes) =>
        {
            mes.Embed = result;
        });
    }
    private Embed CommandEmbedBuilder(string title, string content)
    {
        var e = new EmbedBuilder();
        e.Title = title;
        e.Description = content;
        return e.Build();
    }
    private async Task<string> eval(string script, SocketMessage message)
    {
        if (script == null) { return "error:null"; }
        uint _nowTimestamp = (uint)((DateTime.UtcNow.Ticks - DateTime.Parse("1970-01-01 00:00:00").Ticks) / 10000000);

        string output = await EvaluateCSharpAsync(script, message);
        return output;
    }

    private static readonly Assembly[] DefaultReferences =
{
    typeof(Enumerable).Assembly,
    typeof(List<string>).Assembly,
    typeof(System.Net.Http.HttpClient).Assembly,
};
    public async Task<string> RunCSharpAsync(string code, SocketMessage message)
    {
        try
        {
           object global;
            var options = ScriptOptions.Default
                 .WithImports(DefaultImports)
                 .WithReferences(DefaultReferences)
                 .WithReferences(Assembly.Load("Discord.Net.Commands"))
                 .WithReferences(Assembly.Load("Discord.Net.Core"))
                 .WithReferences(Assembly.Load("Discord.Net.Interactions"))
                 .WithReferences(Assembly.Load("Discord.Net.Rest"))
                 .WithReferences(Assembly.Load("Discord.Net.Webhook"))
                 .WithReferences(Assembly.Load("Discord.Net.WebSocket"));

            if (message.Content == $"{prefix}run")
            {
                IMessage replymes= message.Channel.GetMessageAsync(((ulong)message.Reference.MessageId)).Result;
                Console.WriteLine(message.Reference.MessageId); 
                Console.WriteLine(replymes.Content);
                global = new DiscordNetValuesImessage
                {
                    client = _client,
                    message = replymes,
                };
            var script = CSharpScript.Create(replymes.Content, options, typeof(DiscordNetValuesImessage));
            var a = await script.RunAsync(global);
             return a.ReturnValue.ToString() ?? "null";
            }
            else{
            global = new DiscordNetValues
            {
                client = _client,
                message = message,
            };
            var script = CSharpScript.Create(code, options, typeof(DiscordNetValues));
            var a = await script.RunAsync(global);
             return a.ReturnValue.ToString() ?? "null";
            }

            
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }
    public async Task<string> EvaluateCSharpAsync(string code, SocketMessage message)
    {
        object result = null;
        try
        {

            var global = new DiscordNetValues
            {
                client = _client,
                message = message,
            };

            result = await CSharpScript.EvaluateAsync(code ?? "コードが空です",
                ScriptOptions.Default
                    .WithImports(DefaultImports)
                    .WithReferences(DefaultReferences)
                    .WithReferences(Assembly.Load("Discord.Net.Commands"))
                    .WithReferences(Assembly.Load("Discord.Net.Core"))
                    .WithReferences(Assembly.Load("Discord.Net.Interactions"))
                    .WithReferences(Assembly.Load("Discord.Net.Rest"))
                    .WithReferences(Assembly.Load("Discord.Net.Webhook"))
                    .WithReferences(Assembly.Load("Discord.Net.WebSocket"))

                    , globals: global);

        }
        catch (Exception ex)
        {
            result = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
        }

        var resultText = result?.ToString() ?? "";

        return resultText;
    }
}

public class DiscordNetValues
{
    public DiscordSocketClient client;
    public SocketMessage message;
}
public class DiscordNetValuesImessage
{
    public DiscordSocketClient client;
    public IMessage message;
}

public class SimpleWebServer
{
    static Encoding enc = Encoding.UTF8;
    public async static void MMain()
    {
        Console.WriteLine("start up http server...");


        TcpListener listener = new TcpListener(IPAddress.Any, 8080);
        listener.Start();

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("request incoming...");

            NetworkStream stream = client.GetStream();
            string request = ToString(stream);

            Console.WriteLine("");
            Console.WriteLine(request);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(@"HTTP/1.1 200 OK");
            builder.AppendLine(@"Content-Type: text/html");
            builder.AppendLine(@"");
            builder.AppendLine(@"<html><head><title>Hello world!</title></head><body><h1>Hello world!</h1>Hi!</body></html>");

            Console.WriteLine("");
            Console.WriteLine("responce...");
            Console.WriteLine(builder.ToString());

            byte[] sendBytes = enc.GetBytes(builder.ToString());
            stream.Write(sendBytes, 0, sendBytes.Length);

            stream.Close();
            client.Close();
        }
    }
    public static string ToString(NetworkStream stream)
    {
        MemoryStream memoryStream = new MemoryStream();
        byte[] data = new byte[256];
        int size;
        do
        {
            size = stream.Read(data, 0, data.Length);
            if (size == 0)
            {
                Console.WriteLine("client disconnected...");
                Console.ReadLine();
                return null;
            }
            memoryStream.Write(data, 0, size);
        } while (stream.DataAvailable);
        return enc.GetString(memoryStream.ToArray());
    }
}