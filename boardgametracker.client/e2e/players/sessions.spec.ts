import { expect, test } from "../support/fixtures";
import { duration } from "../support/format";
import { linkedPlayer, sessions } from "../support/seedData";

test("sorts the sessions of a player on duration", async ({ page }) => {
	const minutes = sessions
		.filter((session) => session.scores.some((score) => score.player === linkedPlayer))
		.map((session) => session.minutes);
	await page.goto("/players/1/sessions");
	const firstRow = page.getByRole("table").getByRole("rowgroup").nth(1).getByRole("row").first();
	const header = page.getByRole("columnheader", { name: "Duration" }).getByRole("button");

	await header.click();
	await expect(firstRow.getByRole("cell").nth(3)).toHaveText(duration(Math.max(...minutes)));

	await header.click();
	await expect(firstRow.getByRole("cell").nth(3)).toHaveText(duration(Math.min(...minutes)));
});

test.describe("a player that does not exist", () => {
	test.use({ allowedApiStatuses: [401, 404] });

	test("explains it instead of loading forever", async ({ page }) => {
		await page.goto("/players/9999/sessions");

		await expect(page.getByText("Could not find player")).toBeVisible();
	});
});
