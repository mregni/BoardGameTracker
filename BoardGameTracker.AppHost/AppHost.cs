var builder = DistributedApplication.CreateBuilder(args);

var dbUser = builder.AddParameter("db-user", "dev");
var dbPassword = builder.AddParameter("db-password", "dev", secret: true);
var jwtSecret = builder.AddParameter("jwt-secret", "your-super-secret-jwt-key-that-is-used-in-dev", secret: true);

var smtpPassword = builder.Configuration["Parameters:smtp-password"] ?? string.Empty;

const string databaseName = "boardgametracker-dev";

var postgres = builder.AddPostgres("postgres", dbUser, dbPassword, port: 5432)
    .WithImage("pgvector/pgvector", "pg16")
    .WithDataVolume("boardgametracker-pgdata")
    .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase(databaseName);

var ollama = builder.AddOllama("ollama", port: 11434)
    .WithDataVolume("boardgametracker-ollama")
    .WithLifetime(ContainerLifetime.Persistent);

if (!string.Equals(builder.Configuration["Ollama:UseGpu"], "false", StringComparison.OrdinalIgnoreCase))
{
    ollama.WithGPUSupport();
}

var backend = builder.AddProject<Projects.BoardGameTracker_Host>("bgt-host")
    .WithHttpEndpoint(port: 6554, isProxied: false)
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
        env["JWT_SECRET"] = jwtSecret.Resource;

        // Runtime / logging
        env["STATISTICS_ENABLED"] = "true";
        env["LOGLEVEL"] = "info";
        env["TZ"] = "Europe/Brussels";

        // RAG / AI (see .env.example for the meaning of each value).
        env["RAG_ENABLED"] = "true";
        env["AI_PROVIDER"] = "ollama";
        env["AI_BASE_URL"] = "http://localhost:11434";
        env["AI_CHAT_MODEL"] = "qwen3:4b";
        env["AI_API_KEY"] = "";
        env["AI_EMBEDDING_BASE_URL"] = "http://localhost:11434";
        env["AI_EMBEDDING_NUM_GPU"] = "-1";
        env["MANUALS_PATH"] = "./manuals";
        env["OLLAMA_PATH"] = "./ollama";

        // SMTP is optional (used only for outgoing email). Adjust these to your provider if you
        // want to test email; the password is read from user-secrets above and defaults to empty.
        env["SMTP_HOST"] = "mail.smtp2go.com";
        env["SMTP_PORT"] = "2525";
        env["SMTP_USERNAME"] = "nobelenoedelMailer";
        env["SMTP_PASSWORD"] = smtpPassword;
        env["SMTP_USE_SSL"] = "true";
        env["SMTP_FROM_ADDRESS"] = "noreply@nobelenoedel.be";
        env["SMTP_FROM_NAME"] = "BoardGameTracker";

        // The frontend runs as its own Aspire resource (below), so disable the
        // SpaProxy hosting startup that would otherwise launch `pnpm dev` from the backend.
        env["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "";
    })
    .WaitFor(database)
    .WaitFor(ollama);

builder.AddViteApp("bgt-client", "../boardgametracker.client")
    .WithPnpm()
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5443;
        endpoint.IsProxied = false;
    })
    .WithExternalHttpEndpoints()
    .WaitFor(backend);

await builder.Build().RunAsync();
