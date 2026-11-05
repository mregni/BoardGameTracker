import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
	testDir: "./e2e",
	workers: 1,
	reporter: "list",
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
		{ name: "empty", testMatch: "setup/empty.spec.ts" },
		{ name: "seed", testMatch: "setup/seed.setup.ts", dependencies: ["empty"] },
		{ name: "pages", testIgnore: ["setup/**", "**/manage.spec.ts"], dependencies: ["seed"] },
		{ name: "mutations", testMatch: "**/manage.spec.ts", dependencies: ["pages"] },
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
