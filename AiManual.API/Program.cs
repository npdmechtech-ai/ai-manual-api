using AiManual.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ── PORT CONFIGURATION (Render.com requires this) ──────────────────────────
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// ── CORS ───────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ── SERVICES ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── DEPENDENCY INJECTION ───────────────────────────────────────────────────
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<AIService>();
builder.Services.AddSingleton<ChatService>();

// ✅ NEW: Smart Inspect Service (IMPORTANT)
builder.Services.AddHttpClient<InspectService>();

var app = builder.Build();

// ── HEALTH CHECK ENDPOINTS ─────────────────────────────────────────────────
app.MapGet("/", () => Results.Ok(new
{
    status = "running",
    message = "Parking Brake Manual API is live",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    port = port,
    timestamp = DateTime.UtcNow
}));

// ── INITIALIZE DATA + EMBEDDINGS AT STARTUP ────────────────────────────────
try
{
    var dataService = app.Services.GetRequiredService<DataService>();
    var embeddingService = app.Services.GetRequiredService<EmbeddingService>();

    var fileName = "parking_brake_replacement_procedure.json";

    var pathOption1 = Path.Combine(
        Directory.GetCurrentDirectory(), "Data", fileName);

    var pathOption2 = Path.Combine(
        AppContext.BaseDirectory, "Data", fileName);

    Console.WriteLine($"[Debug] Checking path 1: {pathOption1}");
    Console.WriteLine($"[Debug] Path 1 exists  : {File.Exists(pathOption1)}");
    Console.WriteLine($"[Debug] Checking path 2: {pathOption2}");
    Console.WriteLine($"[Debug] Path 2 exists  : {File.Exists(pathOption2)}");

    var jsonPath = File.Exists(pathOption1) ? pathOption1
                : File.Exists(pathOption2) ? pathOption2
                : null;

    if (jsonPath == null)
    {
        Console.WriteLine("❌ JSON file not found.");
        Console.WriteLine("⚠️ API running but RAG disabled.");
    }
    else
    {
        Console.WriteLine($"✅ JSON found at: {jsonPath}");

        Console.WriteLine("⏳ Loading JSON...");
        dataService.LoadFromJson(jsonPath);

        var stepCount = dataService.GetAllSteps().Count;
        Console.WriteLine($"✅ Loaded {stepCount} steps.");

        if (stepCount > 0)
        {
            Console.WriteLine("⏳ Building vector store...");
            await embeddingService.BuildVectorStore(dataService);
            Console.WriteLine($"✅ Vector store ready.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("❌ Startup error:");
    Console.WriteLine(ex.Message);
    Console.WriteLine("⚠️ API running without RAG.");
}

// ── SWAGGER ────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Parking Brake API v1");
    c.RoutePrefix = "swagger";
});

// ── MIDDLEWARE ─────────────────────────────────────────────────────────────
app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// ── STARTUP LOG ────────────────────────────────────────────────────────────
Console.WriteLine($"🚀 API running on port : {port}");
Console.WriteLine($"📖 Swagger             : http://localhost:{port}/swagger");
Console.WriteLine($"🏥 Health              : http://localhost:{port}/health");

app.Run();