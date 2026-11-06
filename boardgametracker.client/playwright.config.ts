import { defineConfig, devices } from "@playwright/test";

process.env.E2E_SEED_TIME ??= new Date().toISOString();
process.env.TZ = "Europe/Brussels";

const baseURL = `http://localhost:${process.env.E2E_CLIENT_PORT ?? 5543}`;

export default defineConfig({
	testDir: "./e2e",
	workers: 1,
	reporter: "list",
	use: {
		...devices["Desktop Chrome"],
		baseURL,
		locale: "en-US",
		timezoneId: "Europe/Brussels",
		trace: process.env.E2E_TRACE ? "retain-on-failure" : "off",
		screenshot: "only-on-failure",
		launchOptions: { slowMo: Number(process.env.SLOW_MO) || 0 },
	},
	projects: [
		{ name: "empty", testMatch: "setup/empty.spec.ts" },
		{ name: "seed", testMatch: "setup/seed.setup.ts", dependencies: ["empty"] },
		{ name: "pages", testIgnore: ["setup/**", "**/*manage.spec.ts"], dependencies: ["seed"] },
		{ name: "mutations", testMatch: "**/*manage.spec.ts", dependencies: ["pages"] },
	],
	webServer: {
		command: "pnpm build && dotnet run --project ../BoardGameTracker.AppHost --launch-profile http -- --e2e",
		url: baseURL,
		reuseExistingServer: false,
		timeout: 300_000,
		stdout: "ignore",
		stderr: "pipe",
	},
});
