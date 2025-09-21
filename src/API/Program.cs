using API;
using API.Bots;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.SemanticKernel;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
#region logging
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig.ReadFrom.Configuration(context.Configuration);
});
#endregion
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
#region bot
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddSingleton<ConversationState>();
builder.Services.AddSingleton<UserState>();
builder.Services.AddTransient<IBot, EchoBot>();
#endregion

#region semantic kernel
var kernelBuilder = builder.Services.AddKernel();
kernelBuilder.AddOllamaChatCompletion(
    modelId: "llama3",
    endpoint: new Uri("http://localhost:11434"),
    serviceId: "llama3Service"
);
kernelBuilder.AddOllamaChatCompletion(
    modelId: "neural-chat:7b",
    endpoint: new Uri("http://localhost:11434"),
    serviceId: "neuralChatService"
);
kernelBuilder.AddOllamaChatCompletion(
    modelId: "deepseek-r1",
    endpoint: new Uri("http://localhost:11434"),
    serviceId: "deepseekService"
);
kernelBuilder.AddGoogleAIGeminiChatCompletion(
    modelId: "gemini-2.0-flash",
    apiKey: "",
    serviceId: "geminiService"
);
#endregion
var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
