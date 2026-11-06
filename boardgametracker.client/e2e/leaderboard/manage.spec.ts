import type { Page } from "@playwright/test";
import { subDays } from "date-fns";
import { expect, test } from "../support/fixtures";
import { type Api, signedInApi } from "../support/scenarioApi";
import { neverPlayedGame } from "../support/seedData";
import { gameId } from "../support/standings";

const NAMES = ["Alpha", "Bravo", "Charlie", "Delta", "Echo"];
const GAME = gameId(neverPlayedGame.title);
const boardOf = (minimum: number, rank?: string) =>
	`/leaderboard?game=${GAME}&min=${minimum}${rank ? `&rank=${rank}` : ""}`;

let api: Api;
let ids: Record<string, number>;
let sessionIds: number[] = [];

const play = async (
	winners: string[],
	losers: string[],
	scores?: Record<string, number>,
	start = subDays(new Date(), 1),
) => {
	const seats = [...winners, ...losers].map((name) => ({
		playerId: ids[name],
		won: winners.includes(name),
		score: scores?.[name] ?? null,
	}));
	sessionIds.push(await api.addSession({ gameId: GAME, start, minutes: 30, seats }));
};

const podium = (page: Page) => page.getByRole("region", { name: "Podium" }).getByRole("link");

test.beforeAll(async ({ baseURL }) => {
	api = await signedInApi(baseURL);
	ids = {};
	for (const name of NAMES) ids[name] = await api.addPlayer(name);
});

test.afterEach(async () => {
	for (const id of sessionIds) await api.removeSession(id);
	sessionIds = [];
});

test.afterAll(async () => {
	for (const id of Object.values(ids)) await api.removePlayer(id);
});

test("gives identical stats the same rank and skips the next one", async ({ page }) => {
	await play(["Bravo"], ["Charlie"]);
	await play(["Alpha"], ["Charlie"]);

	await page.goto(boardOf(1));

	await expect(podium(page)).toHaveCount(3);
	await expect(podium(page).nth(0)).toHaveAccessibleName("Place 1, Alpha, 1 Win, 100% · 1 session");
	await expect(podium(page).nth(1)).toHaveAccessibleName("Place 1, Bravo, 1 Win, 100% · 1 session");
	await expect(podium(page).nth(2)).toHaveAccessibleName("Place 3, Charlie, 0 Wins, 0% · 2 sessions");
});

test("breaks equal wins on the win rate", async ({ page }) => {
	await play(["Bravo"], ["Alpha"]);
	await play(["Alpha"], ["Bravo"]);
	await play(["Alpha"], ["Bravo"]);
	await play(["Bravo"], ["Alpha"]);
	await play(["Charlie"], []);
	await play(["Charlie"], []);

	await page.goto(boardOf(1));

	await expect(podium(page).nth(0)).toHaveAccessibleName("Place 1, Charlie, 2 Wins, 100% · 2 sessions");
	await expect(podium(page).nth(1)).toHaveAccessibleName("Place 2, Alpha, 2 Wins, 50% · 4 sessions");
	await expect(podium(page).nth(2)).toHaveAccessibleName("Place 2, Bravo, 2 Wins, 50% · 4 sessions");
});

test("breaks an equal win rate on the wins", async ({ page }) => {
	await play(["Alpha"], []);
	await play(["Alpha"], []);
	await play(["Bravo"], []);

	await page.goto(boardOf(1, "winRate"));

	await expect(podium(page).nth(0)).toHaveAccessibleName("Place 1, Alpha, 100% Win rate, 2 wins · 2 sessions");
	await expect(podium(page).nth(1)).toHaveAccessibleName("Place 2, Bravo, 100% Win rate, 1 win · 1 session");
});

test("breaks equal sessions on the wins", async ({ page }) => {
	await play(["Bravo"], ["Alpha"]);

	await page.goto(boardOf(1, "sessions"));

	await expect(podium(page).nth(0)).toHaveAccessibleName("Place 1, Bravo, 1 Session, 1 win · 100%");
	await expect(podium(page).nth(1)).toHaveAccessibleName("Place 2, Alpha, 1 Session, 0 wins · 0%");
});

test("uses two steps for two ranked players and no ranking list", async ({ page }) => {
	await play(["Alpha"], ["Bravo"]);

	await page.goto(boardOf(1));

	await expect(podium(page)).toHaveCount(2);
	await expect(page.getByTestId("leaderboard-row")).toHaveCount(0);
});

test("skips the podium for a single ranked player", async ({ page }) => {
	await play(["Alpha"], []);
	await play(["Alpha"], ["Bravo"]);
	await play(["Alpha"], ["Bravo"]);

	await page.goto(boardOf(3));

	await expect(podium(page)).toHaveCount(0);
	await expect(page.getByTestId("leaderboard-row")).toHaveCount(2);
	await expect(page.getByTestId("leaderboard-row").nth(0)).toHaveAccessibleName(
		"Place 1, Alpha, 3 wins, 3 sessions, Win rate 100%, Time 1h 30m",
	);
	await expect(page.getByTestId("leaderboard-row").nth(1)).toHaveAccessibleName(
		"Not ranked, Bravo, 0 wins, 2 sessions, Win rate 0%, 1 more to be ranked, Time 1h",
	);
	await expect(page.getByTestId("leaderboard-row").nth(1)).toContainText("1 more to be ranked");
});

test("gives the three lowest scores the podium when the lowest score wins", async ({ page }) => {
	await play(["Alpha"], ["Bravo", "Charlie", "Delta", "Echo"], {
		Alpha: 10,
		Bravo: 50,
		Charlie: 40,
		Delta: 30,
		Echo: 20,
	});

	await page.goto(boardOf(1));

	await expect(page.getByTestId("leaderboard-row").nth(0)).toHaveAccessibleName(/^Place 2, Delta, .*, 1 podium,/);
	await expect(page.getByTestId("leaderboard-row").nth(1)).toHaveAccessibleName(/^Place 2, Echo, .*, 1 podium,/);
	await expect(podium(page).nth(1)).toHaveAccessibleName(/^Place 2, Bravo,/);
});

test("counts no podium with fewer than four scores", async ({ page }) => {
	await play(["Alpha"], ["Bravo", "Charlie", "Delta"], { Alpha: 40, Bravo: 30, Charlie: 20 });

	await page.goto(boardOf(1));

	await expect(page.getByTestId("leaderboard-row")).toHaveCount(1);
	await expect(page.getByTestId("leaderboard-row")).toHaveAccessibleName(
		/^Place 2, Delta, 0 wins, 1 session, Win rate 0%, Time/,
	);
});

test("shows nobody for the win records when no session has a winner", async ({ page }) => {
	await play([], ["Alpha", "Bravo"]);

	await page.goto(boardOf(1, "sessions"));
	const records = page.getByRole("region", { name: "Records" }).getByRole("listitem");

	await expect(records.filter({ hasText: "Most wins" })).toHaveText("Most wins-Nobody yet");
	await expect(records.filter({ hasText: "Best win rate" })).toHaveText("Best win rate · min. 1-Nobody yet");
});

test("drops a deleted session from the open leaderboard", async ({ page }) => {
	await play(["Alpha"], ["Bravo"]);
	await page.goto(boardOf(1));
	await expect(podium(page)).toHaveCount(2);

	await page.getByRole("link", { name: /^Place 1, Alpha/ }).click();
	await page
		.getByRole("main")
		.getByRole("button", { name: /sessions/i })
		.click();
	await page.getByRole("table").getByRole("button", { name: "Delete" }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();
	await expect(page.getByRole("dialog")).toBeHidden();
	sessionIds = [];
	await page.goBack();
	await page.goBack();

	await expect(page.getByRole("heading", { name: "No sessions match these filters" })).toBeVisible();
});

test("counts both the first and the last day of a custom period", async ({ page }) => {
	await play(["Alpha"], [], undefined, new Date("2020-03-10T11:00:00Z"));
	const rows = page.getByTestId("leaderboard-row");
	const nothing = page.getByRole("heading", { name: "No sessions match these filters" });

	await page.goto("/leaderboard?range=custom&from=2020-03-10&to=2020-03-10&min=1");
	await expect(rows).toHaveCount(1);

	await page.goto("/leaderboard?range=custom&from=2020-03-11&to=2020-03-12&min=1");
	await expect(nothing).toBeVisible();

	await page.goto("/leaderboard?range=custom&from=2020-03-08&to=2020-03-09&min=1");
	await expect(nothing).toBeVisible();
});

test("lists a new player at once below the minimum", async ({ page }) => {
	await play(["Alpha"], []);

	await page.goto("/leaderboard");

	await expect(page.getByRole("heading", { name: "Fewer than 5 sessions · not ranked" })).toBeVisible();
	await expect(page.getByTestId("leaderboard-row").last()).toHaveAccessibleName(
		/^Not ranked, Alpha, 1 win, 1 session, Win rate 100%, 4 more to be ranked,/,
	);
});
