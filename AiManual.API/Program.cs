using AiManual.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔥 FIX: USE RENDER PORT (MANDATORY)
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// 🔧 SERVICES
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔧 DEPENDENCY INJECTION
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<AIService>();
builder.Services.AddSingleton<ChatService>();

var app = builder.Build();

// 🔥 ROOT ENDPOINT (BROWSER TEST)
app.MapGet("/", () => "✅ API is running");

// 🔥 INITIALIZE EMBEDDINGS (SAFE EXECUTION)
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dataService = scope.ServiceProvider.GetRequiredService<DataService>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<EmbeddingService>();

        Console.WriteLine("⏳ Initializing embeddings...");

        await dataService.InitializeEmbeddings(embeddingService);

        Console.WriteLine("✅ Embeddings initialized successfully.");
    }
}
catch (Exception ex)
{
    Console.WriteLine("❌ Embedding initialization failed:");
    Console.WriteLine(ex.Message);
}

// 🔥 SWAGGER (OPTIONAL BUT USEFUL)
app.UseSwagger();
app.UseSwaggerUI();

// 🔧 MIDDLEWARE
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

// 🔥 DEBUG LOG
Console.WriteLine($"🚀 API RUNNING ON PORT: {port}");

app.Run();
