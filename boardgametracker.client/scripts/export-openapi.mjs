import { spawnSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const client = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const host = path.join(client, "..", "BoardGameTracker.Host");
const output = path.join(client, "openapi", "swagger.json");
const dotnetArgs = process.argv.slice(2);
const configuration = dotnetArgs.includes("-c") || dotnetArgs.includes("--configuration") ? [] : ["-c", "Release"];

const result = spawnSync(
	"dotnet",
	["run", "--project", host, ...configuration, ...dotnetArgs, "--", "--openapi", output],
	{
		stdio: "inherit",
		shell: process.platform === "win32",
		env: {
			...process.env,
			ASPNETCORE_ENVIRONMENT: "Production",
			JWT_SECRET: "openapi-export-placeholder-secret-with-32-chars",
			DB_HOST: "localhost",
			DB_USER: "openapi",
			DB_PASSWORD: "openapi",
			DB_NAME: "openapi",
			LOGLEVEL: "warn",
		},
	},
);

process.exit(result.status ?? 1);
