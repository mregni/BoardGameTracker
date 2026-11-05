import { defineConfig, devices } from "@playwright/test";

const isCi = Boolean(process.env.CI);

export default defineConfig({
	testDir: "./e2e",
	workers: 1,
	forbidOnly: isCi,
	retries: isCi ? 1 : 0,
	reporter: isCi ? "github" : "list",
	use: {
		...devices["Desktop Chrome"],
		baseURL: "http://localhost:5543",
		locale: "en-US",
		timezoneId: "Europe/Brussels",
		trace: "retain-on-failure",
		screenshot: "only-on-failure",
		launchOptions: { slowMo: Number(process.env.SLOW_MO) || 0 },
	},
	projects: [
		{ name: "empty", testMatch: "empty.spec.ts" },
		{ name: "seed", testMatch: "seed.setup.ts", dependencies: ["empty"] },
		{ name: "pages", testMatch: ["dashboard.spec.ts", "games.spec.ts"], dependencies: ["seed"] },
	],
	webServer: {
		command: "pnpm build && dotnet run --project ../BoardGameTracker.AppHost --launch-profile http -- --e2e",
		url: "http://localhost:5543",
		reuseExistingServer: false,
		timeout: 300_000,
		stdout: "ignore",
		stderr: "pipe",
	},
});
