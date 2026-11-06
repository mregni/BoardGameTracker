import { expect, test } from "../support/fixtures";
import { expected } from "../support/seedData";
import { expectingFailures, openSection } from "../support/settings";

const resets = [
	{
		button: "Reset data",
		path: "**/api/maintenance/reset",
		keyword: "RESET",
		input: "Type RESET to confirm",
		confirm: "Reset data",
	},
	{
		button: "Factory reset",
		path: "**/api/maintenance/factory-reset",
		keyword: "FACTORY RESET",
		input: "Type FACTORY RESET to confirm",
		confirm: "Factory reset everything",
	},
];

for (const reset of resets) {
	test.describe(reset.button, () => {
		test("only arms the button with the exact keyword and keeps the data on cancel", async ({ page }) => {
			let calls = 0;
			await page.route(reset.path, (route) => {
				calls += 1;
				return route.fulfill({ status: 204 });
			});
			await openSection(page, "advanced");

			await page.getByRole("button", { name: reset.button, exact: true }).click();
			const dialog = page.getByRole("dialog");
			const input = dialog.getByRole("textbox", { name: reset.input });
			const confirm = dialog.getByRole("button", { name: reset.confirm, exact: true });

			await expect(dialog.getByRole("heading", { name: reset.button })).toBeVisible();
			await expect(input).toBeFocused();
			await expect(confirm).toBeDisabled();

			await input.fill(reset.keyword.toLowerCase());
			await expect(confirm).toBeDisabled();

			await input.fill(`${reset.keyword} `);
			await expect(confirm).toBeDisabled();

			await input.fill(reset.keyword);
			await expect(confirm).toBeEnabled();

			await dialog.getByRole("button", { name: "Cancel" }).click();

			await expect(dialog).toBeHidden();
			expect(calls).toBe(0);
		});
	});
}

test("explains what stays after a data reset and what comes back after a factory reset", async ({ page }) => {
	await openSection(page, "advanced");

	await page.getByRole("button", { name: "Reset data", exact: true }).click();
	await expect(page.getByRole("dialog")).toContainText(
		"Deletes all games, sessions, players, locations, loans, game nights, badges, images and uploaded manuals. Settings and user accounts stay.",
	);
	await page.getByRole("dialog").getByRole("button", { name: "Cancel" }).click();

	await page.getByRole("button", { name: "Factory reset", exact: true }).click();
	const dialog = page.getByRole("dialog");
	await expect(dialog).toContainText("Deletes all data, settings and user accounts, and signs you out.");
	await expect(dialog).toContainText("The admin account comes back with its first-login password.");
	await expect(dialog.getByRole("link", { name: "How is it set?" })).toHaveAttribute(
		"href",
		"https://mregni.github.io/BoardGameTracker/getting-started/authentication/#first-login",
	);
});

test("reports a finished data reset, with the call mocked so the seed stays", async ({ page }) => {
	let calls = 0;
	await page.route("**/api/maintenance/reset", (route) => {
		calls += 1;
		return route.fulfill({ status: 204 });
	});
	await openSection(page, "advanced");

	await page.getByRole("button", { name: "Reset data", exact: true }).click();
	const dialog = page.getByRole("dialog");
	await dialog.getByRole("textbox", { name: "Type RESET to confirm" }).fill("RESET");
	await dialog.getByRole("button", { name: "Reset data" }).click();

	await expect(page.getByText("Data reset", { exact: true })).toBeVisible();
	await expect(dialog).toBeHidden();
	expect(calls).toBe(1);
	await page.goto("/games");
	await expect(page.getByText(`${expected.gameCount} games`)).toBeVisible();
});

for (const [reset, message] of [
	[resets[0], "Failed to reset data"],
	[resets[1], "Failed to factory reset"],
] as const) {
	expectingFailures(`shows that ${reset.button} failed`, async ({ page }) => {
		await page.route(reset.path, (route) => route.fulfill({ status: 400, json: { title: "error.test.unknown" } }));
		await openSection(page, "advanced");

		await page.getByRole("button", { name: reset.button, exact: true }).click();
		const dialog = page.getByRole("dialog");
		await dialog.getByRole("textbox", { name: reset.input }).fill(reset.keyword);
		await dialog.getByRole("button", { name: reset.confirm, exact: true }).click();

		await expect(page.getByText(message)).toBeVisible();
	});
}

test("signs out after a factory reset, with the call mocked so the seed stays", async ({ page }) => {
	await page.route("**/api/maintenance/factory-reset", (route) => route.fulfill({ status: 204 }));
	await openSection(page, "advanced");

	await page.getByRole("button", { name: "Factory reset", exact: true }).click();
	const dialog = page.getByRole("dialog");
	await dialog.getByRole("textbox", { name: "Type FACTORY RESET to confirm" }).fill("FACTORY RESET");
	const navigated = page.waitForRequest((request) => request.isNavigationRequest() && request.url().endsWith("/login"));
	await dialog.getByRole("button", { name: "Factory reset everything" }).click();

	await navigated;
});
