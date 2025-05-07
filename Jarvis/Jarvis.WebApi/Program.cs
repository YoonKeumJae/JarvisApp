using Jarvis.Core;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using DotNetEnv;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
    );
});

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok("Healthy")).WithName("HealthCheck");

MCPService? mcpService = null;
object mcpLock = new();

async Task<MCPService> GetMcpServiceAsync()
{
    if (mcpService != null) return mcpService;
    lock (mcpLock)
    {
        if (mcpService != null) return mcpService;
        string envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(envPath))
            Env.Load(envPath);
        var token = Environment.GetEnvironmentVariable("AZURE_OPENAI_TOKEN");
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("The AZURE_OPENAI_TOKEN environment variable is not set.");
        if (string.IsNullOrEmpty(endpoint))
            throw new InvalidOperationException("The AZURE_OPENAI_ENDPOINT environment variable is not set.");
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddAzureOpenAIChatCompletion(
            modelId: "gpt-4o",
            deploymentName: "gpt-4o",
            endpoint: endpoint,
            apiKey: token);
        var kernel = kernelBuilder.Build();
        mcpService = new MCPService(kernel);
        mcpService.RegisterMcpToolsAsync().GetAwaiter().GetResult();
        return mcpService;
    }
}

app.MapPost("/session", () =>
{
    var session = SessionManager.CreateSession();
    return Results.Ok(new { sessionId = session.SessionId });
});

app.MapPost("/chat", async (ChatRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.SessionId) || string.IsNullOrWhiteSpace(req.Message))
        return Results.BadRequest(new { error = "sessionId, message 필드는 필수입니다." });
    var session = SessionManager.GetSession(req.SessionId);
    if (session == null)
        return Results.NotFound(new { error = "세션을 찾을 수 없습니다." });
    var mcp = await GetMcpServiceAsync();
    var message = await mcp.GetChatResponseAsync(req.Message, session.History);
    return Results.Ok(new { response = message });
});

app.MapDelete("/session/{sessionId}", (string sessionId) =>
{
    var ok = SessionManager.DeleteSession(sessionId);
    if (!ok) return Results.NotFound(new { error = "세션을 찾을 수 없습니다." });
    return Results.Ok(new { result = "세션이 종료되었습니다." });
});

app.Run();

record ChatSession(string SessionId, Microsoft.SemanticKernel.ChatCompletion.ChatHistory History);

class SessionManager
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ChatSession> Sessions = new();
    public static ChatSession CreateSession()
    {
        var sessionId = System.Guid.NewGuid().ToString();
        var history = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
        history.AddSystemMessage("당신은 Jarvis라는 챗봇입니다. 영화 '아이언맨'의 J.A.R.V.I.S. 캐릭터를 모델로 사용합니다. 항상 한국어로 대답해주세요.");
        var session = new ChatSession(sessionId, history);
        Sessions[sessionId] = session;
        return session;
    }
    public static ChatSession? GetSession(string sessionId)
        => Sessions.TryGetValue(sessionId, out var session) ? session : null;
    public static bool DeleteSession(string sessionId)
        => Sessions.TryRemove(sessionId, out _);
}

record ChatRequest(string SessionId, string Message);
