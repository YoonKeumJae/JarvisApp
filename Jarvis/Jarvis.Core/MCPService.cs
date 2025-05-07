using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

using System.Text.Json.Nodes;

namespace Jarvis.Core;

public class MCPService(Kernel _kernel)
{
    public async Task RegisterMcpToolsAsync()
    {
        Kernel kernel = _kernel;
        var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "mcp.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        var root = JsonNode.Parse(json);
        var mcp = root?["mcp"]?.AsObject();

        if (mcp == null)
            throw new InvalidOperationException("mcp not found in mcp.json");

        var servers = mcp["servers"]?.AsObject();
        if (servers == null)
            throw new InvalidOperationException("servers not found in mcp.json");

        foreach (var (pluginName, server) in servers)
        {
            var command = server?["command"]?.ToString() ?? "docker";
            var args = server?["args"]?.AsArray()?.Select(x => x.ToString()).ToArray() ?? Array.Empty<string>();
            var env = server?["env"]?.AsObject();

            var transportOptions = new StdioClientTransportOptions
            {
                Name = pluginName,
                Command = command,
                Arguments = args,
                WorkingDirectory = Directory.GetCurrentDirectory()
            };

            if (env != null)
            {
                transportOptions.EnvironmentVariables = env.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? ""
                );
            }

            Console.WriteLine($"MCP 서버를 시작합니다: {command} {string.Join(" ", args)}");
            var mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(transportOptions));
            var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
            Console.WriteLine($"MCP 도구 {tools.Count}개를 불러왔습니다:");
            foreach (var tool in tools)
                Console.WriteLine($"- {tool.Name}: {tool.Description}");
            kernel.Plugins.AddFromFunctions(pluginName, tools.Select(tool => tool.AsKernelFunction()));
        }
    }

    public async Task InvokeMCPAsync()
    {
        Kernel kernel = _kernel;
     
        foreach (var plugin in kernel.Plugins)
        {
            Console.WriteLine($"Plugin Name: {plugin.Name}");

            foreach (var function in plugin)
            {
                Console.WriteLine($"  Function Name: {function}");
            }
        }

        var settings = new PromptExecutionSettings()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var history = new ChatHistory();
        history.AddSystemMessage("당신은 Jarvis라는 챗봇입니다. 영화 '아이언맨'의 J.A.R.V.I.S. 캐릭터를 모델로 사용합니다. 항상 한국어로 대답해주세요.");

        var service=  kernel.GetRequiredService<IChatCompletionService>();

        var input =  default(string);
        var message = default(string);
         while (true)
        {
            Console.Write("User: ");
            input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                break;
            }

            Console.Write("Assistant: ");

            history.AddUserMessage(input);
            var response = service.GetStreamingChatMessageContentsAsync(history, settings, kernel);
            await foreach (var content in response)
            {
                await Task.Delay(20);
                message += content;
                Console.Write(content);
            }
            history.AddAssistantMessage(message!);
            Console.WriteLine();

            Console.WriteLine();
        }

    }
} 