var e2e = args.Contains("--e2e");
var backendPort = e2e ? 6654 : 6554;
var clientPort = e2e ? 5543 : 5443;

var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = args, DisableDashboard = e2e });

var dbUser = builder.AddParameter("db-user", "dev");
var dbPassword = builder.AddParameter("db-password", "dev", secret: true);
var jwtSecret = builder.AddParameter("jwt-secret", "your-super-secret-jwt-key-that-is-used-in-dev", secret: true);
var dataProtectionKey = builder.AddParameter("data-protection-key", "dev-only-data-protection-key-change-me-for-real-use", secret: true);

var smtpPassword = builder.AddParameter("smtp-password", string.Empty, secret: true);
var aiApiKey = builder.AddParameter("ai-api-key", string.Empty, secret: true);

var smtp = builder.Configuration.GetSection("Smtp");
var ai = builder.Configuration.GetSection("Ai");
var aiProvider = ai["Provider"] ?? "ollama";

const string databaseName = "boardgametracker-dev";

var postgres = builder.AddPostgres("postgres", dbUser, dbPassword, port: e2e ? null : 5432)
    .WithImage("pgvector/pgvector", "pg16");

if (!e2e)
{
    postgres.WithDataVolume("boardgametracker-pgdata")
        .WithLifetime(ContainerLifetime.Persistent);
}

var database = postgres.AddDatabase(databaseName);

var backend = builder.AddProject<Projects.BoardGameTracker_Host>("bgt-host")
    .WithHttpEndpoint(port: backendPort, isProxied: false)
    .WithHttpHealthCheck("/api/health")
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Swagger";
        url.Url = url.Url.TrimEnd('/') + "/swagger";
    })
    .WithEnvironment(context =>
    {
        var env = context.EnvironmentVariables;

        env["ASPNETCORE_ENVIRONMENT"] = "Development";

        // Database (provisioned by the Aspire-managed pgvector container above).
        env["DB_HOST"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        env["DB_PORT"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        env["DB_USER"] = dbUser.Resource;
        env["DB_PASSWORD"] = dbPassword.Resource;
        env["DB_NAME"] = databaseName;

        // Auth
        env["AUTH_ENABLED"] = "true";
        env["PUBLIC_URL"] = $"http://localhost:{clientPort}";
        env["JWT_SECRET"] = jwtSecret.Resource;
        env["DATA_PROTECTION_KEY"] = dataProtectionKey.Resource;

        // Runtime / logging
        env["STATISTICS_ENABLED"] = "false";
        env["LOGLEVEL"] = "info";
        env["TZ"] = "Europe/Brussels";

        // The frontend runs as its own Aspire resource (below), so disable the
        // SpaProxy hosting startup that would otherwise launch `pnpm dev` from the backend.
        env["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "";

        if (e2e)
        {
            env["RAG_ENABLED"] = "false";
            return;
        }

        // RAG / AI (see .env.example for the meaning of each value).
        env["RAG_ENABLED"] = "true";
        env["AI_PROVIDER"] = aiProvider;
        env["AI_BASE_URL"] = ai["BaseUrl"] ?? "http://localhost:11434";
        env["AI_CHAT_MODEL"] = ai["ChatModel"] ?? "qwen3:4b";
        env["AI_API_KEY"] = aiApiKey.Resource;
        env["AI_EMBEDDING_BASE_URL"] = "http://localhost:11434";
        env["AI_EMBEDDING_NUM_GPU"] = "-1";

        // SMTP is optional (used only for outgoing email).
        env["SMTP_HOST"] = smtp["Host"] ?? "smtp.example.com";
        env["SMTP_PORT"] = smtp["Port"] ?? "587";
        env["SMTP_USERNAME"] = smtp["Username"] ?? string.Empty;
        env["SMTP_PASSWORD"] = smtpPassword.Resource;
        env["SMTP_USE_SSL"] = smtp["UseSsl"] ?? "true";
        env["SMTP_FROM_ADDRESS"] = smtp["FromAddress"] ?? "boardgametracker@example.com";
        env["SMTP_FROM_NAME"] = smtp["FromName"] ?? "BoardGameTracker";
    })
    .WaitFor(database);

if (!e2e)
{
    var ollama = builder.AddOllama("ollama", port: 11434)
        .WithDataVolume("boardgametracker-ollama")
        .WithLifetime(ContainerLifetime.Persistent);

    if (!string.Equals(builder.Configuration["Ollama:UseGpu"], "false", StringComparison.OrdinalIgnoreCase))
    {
        ollama.WithGPUSupport();
    }

    if (string.Equals(aiProvider, "ollama", StringComparison.OrdinalIgnoreCase))
    {
        backend.WaitFor(ollama);
    }
}

var client = builder.AddViteApp("bgt-client", "../boardgametracker.client")
    .WithPnpm()
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = clientPort;
        endpoint.IsProxied = false;
    })
    .WithExternalHttpEndpoints()
    .WithEnvironment("API_PROXY_TARGET", backend.GetEndpoint("http"))
    .WithUrlForEndpoint("http", url => url.DisplayText = "website")
    .WaitFor(backend);

if (e2e)
{
    client.WithRunScript("preview");
}

await builder.Build().RunAsync();
