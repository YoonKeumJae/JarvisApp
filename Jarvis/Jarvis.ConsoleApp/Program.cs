using DotNetEnv;
using Microsoft.SemanticKernel;

using Jarvis.Core;

string envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
Env.Load(envPath);
var token = Environment.GetEnvironmentVariable("AZURE_OPENAI_TOKEN");
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
if (string.IsNullOrEmpty(token))
    throw new InvalidOperationException("The AZURE_OPENAI_TOKEN environment variable is not set.");
if (string.IsNullOrEmpty(endpoint))
    throw new InvalidOperationException("The AZURE_OPENAI_ENDPOINT environment variable is not set.");

var builder = Kernel.CreateBuilder();

builder.AddAzureOpenAIChatCompletion(
            modelId: "gpt-4o",
            deploymentName: "gpt-4o",
            endpoint: endpoint,
            apiKey: token);

var kernel = builder.Build();
var mcpJsonPath = Path.Combine(AppContext.BaseDirectory, "Jarvis.Core", "mcp.json");
var mcpService = new MCPService(kernel, mcpJsonPath);
await mcpService.RegisterMcpToolsAsync();
await mcpService.InvokeMCPAsync();
