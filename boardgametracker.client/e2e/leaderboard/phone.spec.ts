import { expect, test } from "../support/fixtures";
import { pick } from "../support/gamesPage";
import { podiumOf } from "../support/leaderboardPage";
import { sessions } from "../support/seedData";
import { leaderboard, percent, type RankBy } from "../support/standings";

test.beforeEach(async ({ page }) => {
	await page.setViewportSize({ width: 375, height: 812 });
});

for (const path of ["/leaderboard", "/compare"]) {
	test(`${path} fits a phone without sideways scrolling`, async ({ page }) => {
		await page.goto(path);
		await expect(page.getByRole("main").getByRole("heading", { level: 1 })).toBeVisible();
		await page.waitForLoadState("networkidle");

		const { contentWidth, screenWidth } = await page.evaluate(() => ({
			contentWidth: document.documentElement.scrollWidth,
			screenWidth: document.documentElement.clientWidth,
		}));

		expect(contentWidth).toBeLessThanOrEqual(screenWidth);
	});
}

test("folds the extra filters and counts the ones in use", async ({ page }) => {
	await page.goto("/leaderboard?min=3&rank=sessions");
	const more = page.getByRole("button", { name: /^More filters/ });

	await expect(more).toHaveText("More filters(2)");
	await expect(more).toHaveAttribute("aria-expanded", "false");
	await expect(page.getByRole("combobox", { name: "Rank by" })).toBeHidden();

	await more.click();

	await expect(more).toHaveAttribute("aria-expanded", "true");
	await expect(page.getByRole("combobox", { name: "Rank by" })).toHaveText("Sessions");
});

test("uses the filters from the fold", async ({ page }) => {
	await page.goto("/leaderboard");

	await page.getByRole("button", { name: "More filters" }).click();
	await pick(page, "Rank by", "Win rate");

	await expect(page).toHaveURL("/leaderboard?rank=winRate");
	await expect(page.getByRole("button", { name: /^More filters/ })).toHaveText("More filters(1)");
});

const values: { rank: RankBy; query: string; value: (entry: ReturnType<typeof entries>[number]) => string }[] = [
	{ rank: "wins", query: "", value: (entry) => `${entry.wins} ${entry.wins === 1 ? "win" : "wins"}` },
	{ rank: "winRate", query: "?rank=winRate", value: (entry) => percent(entry.winRate) },
	{ rank: "sessions", query: "?rank=sessions", value: (entry) => `${entry.sessions} sessions` },
];

const entries = (rank: RankBy) => {
	const standing = leaderboard(sessions, 5, rank);
	return standing.ranked.filter((entry) => !podiumOf(standing).includes(entry));
};

for (const { rank, query, value } of values) {
	test(`shows the ${rank} on each phone row`, async ({ page }) => {
		await page.goto(`/leaderboard${query}`);

		await expect(page.getByTestId("leaderboard-mobile-value")).toHaveText(entries(rank).map(value));
	});
}
