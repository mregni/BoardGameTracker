import { subDays } from "date-fns";
import { expect, test } from "../support/fixtures";
import { displayDate } from "../support/format";
import { type Api, signedInApi } from "../support/scenarioApi";
import { coopGame, neverPlayedGame, unscoredGame } from "../support/seedData";
import { gameId } from "../support/standings";

const LEFT = "Statler";
const RIGHT = "Waldorf";
const THIRD = "Pepe";
const compareOf = (left: number, right: number) => `/compare?left=${left}&right=${right}`;
const days = (amount: number) => subDays(new Date(), amount);

let api: Api;
let left: number;
let right: number;
let third: number;
let sessionIds: number[] = [];

const addSession = async (session: Parameters<Api["addSession"]>[0]) => {
	const id = await api.addSession(session);
	sessionIds.push(id);
	return id;
};

const seatsOf = (winners: number[]) =>
	[left, right, third].map((playerId) => ({ playerId, won: winners.includes(playerId) }));

const scoredSeats = (leftScore: number, rightScore: number, winners: number[]) => [
	{ playerId: left, score: leftScore, won: winners.includes(left) },
	{ playerId: right, score: rightScore, won: winners.includes(right) },
	{ playerId: third, score: 10, won: winners.includes(third) },
];

test.beforeAll(async ({ baseURL }) => {
	api = await signedInApi(baseURL);
	left = await api.addPlayer(LEFT);
	right = await api.addPlayer(RIGHT);
	third = await api.addPlayer(THIRD);
});

test.afterEach(async () => {
	for (const id of sessionIds) await api.removeSession(id);
	sessionIds = [];
});

test.afterAll(async () => {
	for (const id of [left, right, third]) await api.removePlayer(id);
});

test("shows new players without sessions as equals", async ({ page }) => {
	await page.goto(compareOf(left, right));

	await expect(page.getByRole("heading", { name: "No sessions together yet" })).toBeVisible();
	await expect(page.getByTestId("overall-row")).toHaveText([
		"0Wins0",
		"0%Win rate0%",
		"0Sessions0",
		"0 minPlay time0 min",
	]);
	await expect(page.getByText("higher")).toHaveCount(0);
});

test("gets the wording of a single session right", async ({ page }) => {
	await addSession({
		gameId: gameId(neverPlayedGame.title),
		start: days(1),
		minutes: 45,
		seats: scoredSeats(70, 70, [left]),
	});

	await page.goto(compareOf(left, right));
	const record = page.getByRole("region", { name: "Record over all sessions together" });
	const tiles = page.getByRole("region", { name: "Together", exact: true });

	await expect(record.getByText(`win for ${LEFT}`, { exact: true })).toBeVisible();
	await expect(record.getByText(`no wins yet for ${RIGHT}`, { exact: true })).toBeVisible();
	await expect(record.getByRole("img", { name: `${LEFT} leads` })).toBeVisible();
	await expect(record.getByRole("img", { name: /^1 session together: / })).toBeVisible();
	await expect(record.getByText("Last session together")).toBeVisible();
	await expect(tiles).toHaveText(
		`TogetherSessions1since ${displayDate(days(1))}Play time45 minMost played${neverPlayedGame.title}1 sessionClosest0 pts${neverPlayedGame.title} · ${LEFT} won`,
	);
	await expect(page.getByTestId("per-game-row")).toContainText(`${neverPlayedGame.title}1 session · 70 – 7010`);
	await expect(page.getByRole("region", { name: "Strongest games" })).toHaveCount(0);
	await expect(page.getByTestId("overall-row").first()).toHaveAccessibleName("1 higher Wins 0");
});

test("shows a shared win, a win by someone else and no winner at all", async ({ page }) => {
	const shared = await addSession({
		gameId: gameId(neverPlayedGame.title),
		start: days(1),
		minutes: 30,
		seats: scoredSeats(50, 50, [left, right]),
	});
	const someoneElse = await addSession({
		gameId: gameId(neverPlayedGame.title),
		start: days(2),
		minutes: 30,
		seats: scoredSeats(5, 6, [third]),
	});
	const nobody = await addSession({
		gameId: gameId(neverPlayedGame.title),
		start: days(3),
		minutes: 30,
		seats: scoredSeats(1, 2, []),
	});

	await page.goto(compareOf(left, right));
	const record = page.getByRole("region", { name: "Record over all sessions together" });
	const recent = page.getByRole("region", { name: "Recent sessions together" });

	await expect(page.getByTestId("record-playerOne")).toHaveText("1");
	await expect(page.getByTestId("record-playerTwo")).toHaveText("1");
	await expect(record.getByRole("img", { name: / leads$/ })).toHaveCount(0);
	await expect(record.getByRole("listitem")).toHaveText(["not won by either2"]);
	await expect(
		record.getByRole("img", {
			name: "Last sessions together, oldest first: neither won, neither won, both won",
			exact: true,
		}),
	).toBeVisible();
	await expect(page.getByTestId("form-tile")).toHaveText(["–", "–", "+"]);
	await expect(page.getByTestId("form-tile").nth(2)).toHaveAttribute("data-outcome", "both");
	await expect(recent.getByRole("link", { name: /Both won/ })).toHaveAttribute("href", `/sessions/update/${shared}`);
	await expect(recent.getByRole("link", { name: new RegExp(` ${THIRD} `) })).toHaveAttribute(
		"href",
		`/sessions/update/${someoneElse}`,
	);
	await expect(recent.getByRole("link", { name: /No winner/ })).toHaveAttribute("href", `/sessions/update/${nobody}`);
	await expect(page.getByTestId("per-game-row")).toContainText("3 sessions");
	await expect(page.getByTestId("per-game-row")).toContainText("2 not won by either");
});

test("skips a shared win when counting a streak, stops at someone else", async ({ page }) => {
	const game = gameId(neverPlayedGame.title);
	await addSession({ gameId: game, start: days(1), minutes: 30, seats: scoredSeats(9, 1, [left]) });
	await addSession({ gameId: game, start: days(2), minutes: 30, seats: scoredSeats(5, 5, [left, right]) });
	await addSession({ gameId: game, start: days(3), minutes: 30, seats: scoredSeats(9, 1, [left]) });
	await addSession({ gameId: game, start: days(4), minutes: 30, seats: scoredSeats(1, 1, [third]) });
	await addSession({ gameId: game, start: days(5), minutes: 30, seats: scoredSeats(9, 1, [left]) });

	await page.goto(compareOf(left, right));

	await expect(page.getByRole("region", { name: "Record over all sessions together" })).toContainText(
		`${LEFT} 2 in a row`,
	);

	await page.getByRole("button", { name: "Swap players" }).click();

	await expect(page.getByRole("region", { name: "Record over all sessions together" })).toContainText(
		`${LEFT} 2 in a row`,
	);
});

test("leaves the closest tile out when no session has scores", async ({ page }) => {
	const game = gameId(unscoredGame.title);
	await addSession({ gameId: game, start: days(1), minutes: 15, seats: seatsOf([left]) });
	await addSession({ gameId: game, start: days(2), minutes: 15, seats: seatsOf([left]) });
	await addSession({ gameId: game, start: days(3), minutes: 15, seats: seatsOf([right]) });

	await page.goto(compareOf(left, right));
	const recent = page.getByRole("region", { name: "Recent sessions together" });

	await expect(page.getByTestId("per-game-row")).toContainText("3 sessions · no scores");
	await expect(page.getByRole("region", { name: "Together", exact: true })).not.toContainText("Closest");
	await expect(recent.getByRole("link").first()).toHaveAccessibleName(
		new RegExp(`^${displayDate(days(1))} .* ${unscoredGame.title} ${LEFT} 15 min$`),
	);
	await expect(page.getByTestId("best-worst-playerOne")).toContainText(`${unscoredGame.title}2 – 1`);
	await expect(page.getByTestId("best-worst-playerTwo")).toHaveText(`${RIGHT}Nothing yet`);
});

test("shows an average score from three scored sessions on", async ({ page }) => {
	const game = gameId(neverPlayedGame.title);
	await addSession({ gameId: game, start: days(1), minutes: 30, seats: scoredSeats(30, 20, [left]) });
	await addSession({ gameId: game, start: days(2), minutes: 30, seats: scoredSeats(20, 25, [right]) });

	await page.goto(compareOf(left, right));

	await expect(page.getByTestId("per-game-row").locator("small")).toHaveText("2 sessions");

	await addSession({ gameId: game, start: days(3), minutes: 30, seats: scoredSeats(25, 15.5, [left]) });
	await page.reload();

	await expect(page.getByTestId("per-game-row").locator("small")).toHaveText("3 sessions · avg 25 – 20.2");
	await expect(page.getByRole("region", { name: "Together", exact: true })).toContainText("Closest5 pts");
});

test("shows half points in the closest tile", async ({ page }) => {
	await addSession({
		gameId: gameId(neverPlayedGame.title),
		start: days(1),
		minutes: 30,
		seats: scoredSeats(10.5, 11, [right]),
	});

	await page.goto(compareOf(left, right));

	await expect(page.getByRole("region", { name: "Together", exact: true })).toContainText(
		`Closest0.5 pts${neverPlayedGame.title} · ${RIGHT} won`,
	);
	await expect(page.getByRole("region", { name: "Recent sessions together" }).getByRole("link")).toHaveAccessibleName(
		new RegExp(` · 10\\.5 – 11 ${RIGHT} `),
	);
});

test("does not credit one player for a shared win on the closest tile", async ({ page }) => {
	test.fail(true, "BUG: the closest tile says only the left player won when both players won that session");
	await addSession({
		gameId: gameId(neverPlayedGame.title),
		start: days(1),
		minutes: 30,
		seats: scoredSeats(50, 50, [left, right]),
	});

	await page.goto(compareOf(left, right));

	const tiles = page.getByRole("region", { name: "Together", exact: true });
	await expect(tiles).toContainText("Closest0 pts");
	await expect(tiles).not.toContainText(`${LEFT} won`, { timeout: 3000 });
});

test("does not claim someone won the closest session when nobody did", async ({ page }) => {
	test.fail(true, "BUG: the closest tile says someone else won a session without a winner");
	await addSession({
		gameId: gameId(neverPlayedGame.title),
		start: days(1),
		minutes: 30,
		seats: scoredSeats(50, 51, []),
	});

	await page.goto(compareOf(left, right));

	const tiles = page.getByRole("region", { name: "Together", exact: true });
	await expect(tiles).toContainText("Closest1 pt");
	await expect(tiles).not.toContainText("someone else won", { timeout: 3000 });
});

test("counts a lost co-op session as a team loss, not as a win for anyone", async ({ page }) => {
	const game = gameId(coopGame.title);
	const seats = [{ playerId: left }, { playerId: right }];
	await addSession({ gameId: game, start: days(1), minutes: 60, seats, isCoop: true, teamWon: false });

	await page.goto(compareOf(left, right));
	const record = page.getByRole("region", { name: "Record over all sessions together" });

	await expect(page.getByTestId("record-playerOne")).toHaveText("0");
	await expect(record.getByRole("listitem")).toHaveText(["same co-op team1 (won 0, lost 1)"]);
	await expect(page.getByTestId("form-tile")).toHaveAttribute("title", `${coopGame.title} · same team, lost`);
	await expect(page.getByTestId("per-game-row")).toContainText("Same team · won 0 of 1");
	await expect(page.getByRole("region", { name: "Recent sessions together" }).getByRole("link")).toContainText(
		"Co-op · lost",
	);
	await expect(page.getByRole("region", { name: "Together", exact: true })).not.toContainText("Closest");
});

test("drops a deleted session from the open comparison", async ({ page }) => {
	const game = gameId(neverPlayedGame.title);
	await addSession({ gameId: game, start: days(1), minutes: 30, seats: scoredSeats(9, 1, [left]) });
	await page.goto(compareOf(left, right));
	await expect(page.getByTestId("record-playerOne")).toHaveText("1");

	await page.getByRole("link", { name: /^Most played/ }).click();
	await page.getByRole("link", { name: "View sessions" }).click();
	await page.getByRole("table").getByRole("button", { name: "Delete" }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();
	await expect(page.getByRole("dialog")).toBeHidden();
	sessionIds = [];
	await page.goBack();
	await page.goBack();

	await expect(page.getByRole("heading", { name: "No sessions together yet" })).toBeVisible();
	await expect(page.getByTestId("overall-row").first()).toHaveAccessibleName("0 Wins 0");
});

test("moves on to other players when a player in the link was deleted", async ({ page }) => {
	const temporary = await api.addPlayer("Crazy Harry");
	await page.goto(compareOf(temporary, left));
	await expect(page.getByRole("combobox", { name: "Change left player, now Crazy Harry" })).toBeVisible();

	await api.removePlayer(temporary);
	await page.goto(compareOf(temporary, left));

	await expect(page.getByRole("combobox", { name: `Change right player, now ${LEFT}` })).toBeVisible();
	await expect(page.getByRole("combobox", { name: /Crazy Harry/ })).toHaveCount(0);
});
