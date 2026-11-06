import { expect, type Locator, type Page } from "@playwright/test";
import { displayDate, duration } from "./format";
import {
	headToHead,
	literal,
	MIN_SESSIONS_FOR_AVERAGE,
	type Outcome,
	percent,
	playerId,
	sessionId,
	short,
	totals,
} from "./standings";

const VISIBLE_GAMES = 5;
const RECENT_ROWS = 5;

const plural = (amount: number, word: string) => `${amount} ${word}${amount === 1 ? "" : "s"}`;

export const leftPicker = (page: Page, name: string) =>
	page.getByRole("combobox", { name: `Change left player, now ${name}`, exact: true });

export const rightPicker = (page: Page, name: string) =>
	page.getByRole("combobox", { name: `Change right player, now ${name}`, exact: true });

export const choosePlayer = async (page: Page, picker: Locator, name: string) => {
	await picker.click();
	await page
		.getByRole("option")
		.filter({ hasText: new RegExp(`^${name}$`) })
		.click();
};

export const compareUrl = (one: string, two: string) => `/compare?left=${playerId(one)}&right=${playerId(two)}`;

export const expectPair = async (page: Page, one: string, two: string) => {
	await expect(leftPicker(page, one)).toBeVisible();
	await expect(rightPicker(page, two)).toBeVisible();
	await expect(page).toHaveURL(compareUrl(one, two));
	await expect(page.getByTestId("compare-content")).toHaveAttribute("aria-busy", "false");
};

export const openCompare = async (page: Page, one: string, two: string) => {
	await page.goto(compareUrl(one, two));
	await expectPair(page, one, two);
};

const expectOverall = async (page: Page, one: string, two: string) => {
	const left = totals(one);
	const right = totals(two);
	const row = (label: string, a: number, b: number, format: (value: number) => string) =>
		`${format(a)}${a > b ? " higher" : ""} ${label} ${format(b)}${b > a ? " higher" : ""}`;
	const region = page.getByRole("region", { name: "All sessions, not just together" });
	const rows = [
		row("Wins", left.wins, right.wins, String),
		row("Win rate", left.winRate, right.winRate, percent),
		row("Sessions", left.sessions, right.sessions, String),
		row("Play time", left.minutes, right.minutes, duration),
	];

	await expect(region.getByTestId("overall-row")).toHaveCount(rows.length);
	for (const [index, name] of rows.entries()) {
		await expect(region.getByTestId("overall-row").nth(index)).toHaveAccessibleName(name);
	}
	await expect(region.getByRole("columnheader")).toHaveText([one, "Stat", two]);
};

const winsLabel = (wins: number, name: string) => {
	if (wins === 0) return `no wins yet for ${name}`;
	return `${wins === 1 ? "win" : "wins"} for ${name}`;
};

const expectRecord = async (page: Page, one: string, two: string) => {
	const stats = headToHead(one, two);
	const record = page.getByRole("region", { name: "Record over all sessions together" });
	const team = `same co-op team ${stats.sameTeam} (won ${stats.sameTeamWon}, lost ${stats.sameTeam - stats.sameTeamWon})`;
	const summary = [
		`${plural(stats.together.length, "session")} together: ${one} won ${stats.oneWins}, ${two} won ${stats.twoWins}, ${stats.others} not won by either`,
		...(stats.sameTeam > 0 ? [team] : []),
	].join(", ");
	const legend = [
		...(stats.others > 0 ? [`not won by either${stats.others}`] : []),
		...(stats.sameTeam > 0 ? [`same co-op team${team.replace("same co-op team ", "")}`] : []),
	];

	await expect(record.getByTestId("record-playerOne")).toHaveText(String(stats.oneWins));
	await expect(record.getByTestId("record-playerTwo")).toHaveText(String(stats.twoWins));
	await expect(record.getByText(winsLabel(stats.oneWins, one), { exact: true })).toBeVisible();
	await expect(record.getByText(winsLabel(stats.twoWins, two), { exact: true })).toBeVisible();
	await expect(record.getByRole("img", { name: `${one} leads` })).toHaveCount(stats.oneWins > stats.twoWins ? 1 : 0);
	await expect(record.getByRole("img", { name: `${two} leads` })).toHaveCount(stats.twoWins > stats.oneWins ? 1 : 0);
	await expect(record.getByRole("img", { name: summary, exact: true })).toBeVisible();
	await expect(record.getByRole("listitem")).toHaveText(legend);
};

const expectForm = async (page: Page, one: string, two: string) => {
	const stats = headToHead(one, two);
	const record = page.getByRole("region", { name: "Record over all sessions together" });
	const tiles = record.getByTestId("form-tile");
	const oldestFirst = [...stats.recent].reverse();
	const who = (session: (typeof oldestFirst)[number]) => {
		const names: Record<Outcome, string> = {
			playerOne: one,
			playerTwo: two,
			other: "neither won",
			sameTeam: session.teamWon ? "same team, won" : "same team, lost",
		};
		return names[stats.outcome(session)];
	};

	await expect(
		record.getByText(
			oldestFirst.length === 1 ? "Last session together" : `Last ${oldestFirst.length} sessions together`,
		),
	).toBeVisible();
	await expect(tiles).toHaveCount(oldestFirst.length);
	for (const [index, session] of oldestFirst.entries()) {
		await expect(tiles.nth(index)).toHaveAttribute("data-outcome", stats.outcome(session));
		await expect(tiles.nth(index)).toHaveAttribute("title", `${session.game} · ${who(session)}`);
	}
	await expect(
		record.getByRole("img", {
			name: `Last sessions together, oldest first: ${oldestFirst.map(who).join(", ")}`,
			exact: true,
		}),
	).toBeVisible();

	const streak = record.getByText(/ in a row$/);
	if (stats.streak) await expect(streak).toHaveText(`${stats.streak.name} ${stats.streak.length} in a row`);
	else await expect(streak).toHaveCount(0);
};

const expectTiles = async (page: Page, one: string, two: string) => {
	const stats = headToHead(one, two);
	const count = stats.together.length;
	const [mostPlayed] = stats.perGame;
	const closest = stats.closest;
	const winnerOf = (name: string | null) => (name === one || name === two ? `${name} won` : "someone else won");
	const closestText = closest
		? `Closest${plural(closest.difference, "pt")}${closest.session.game} · ${winnerOf(closest.session.winner)}`
		: "";
	const firstDay = stats.first ? displayDate(stats.first.start) : "";

	await expect(page.getByRole("region", { name: "Together", exact: true })).toHaveText(
		[
			"Together",
			`Sessions${count}since ${firstDay}`,
			`Play time${short(stats.minutes)}`,
			count > 1 ? `avg ${short(stats.minutes / count)} a session` : "",
			`Most played${mostPlayed.title}${plural(mostPlayed.sessions, "session")}`,
			closestText,
		].join(""),
	);
	await expect(
		page.getByRole("region", { name: "Together", exact: true }).getByRole("link", { name: /^Most played/ }),
	).toHaveAttribute("href", `/games/${mostPlayed.id}`);
};

type PerGame = ReturnType<typeof headToHead>["perGame"][number];
type Wins = "oneWins" | "twoWins";

const detailOf = (game: PerGame) => {
	const sessions = plural(game.sessions, "session");
	if (game.sameTeam === game.sessions) return `${sessions} · co-op`;
	if (game.scored >= MIN_SESSIONS_FOR_AVERAGE) return `${sessions} · avg ${game.oneAverage} – ${game.twoAverage}`;
	if (game.scored === 1) return `${sessions} · ${game.oneAverage} – ${game.twoAverage}`;
	return game.scored === 0 ? `${sessions} · no scores` : sessions;
};

const winsOf = (game: PerGame) => {
	if (game.sameTeam === game.sessions) return `Same team · won ${game.sameTeamWins} of ${game.sameTeam}`;
	const extras = [
		...(game.others > 0 ? [`${game.others} not won by either`] : []),
		...(game.sameTeam > 0 ? [`+${game.sameTeam} same team`] : []),
	];
	return `${game.oneWins}${extras.join(" · ")}${game.twoWins}`;
};

const barOf = (game: PerGame, one: string, two: string) =>
	game.sameTeam === game.sessions
		? `${game.title}: Same team · won ${game.sameTeamWins} of ${game.sameTeam}`
		: `${game.title}: ${one} ${game.oneWins}, ${two} ${game.twoWins}, not won by either ${game.others}`;

export const expectGameRows = async (page: Page, one: string, two: string, expected: PerGame[]) => {
	const rows = page.getByTestId("per-game-row");

	await expect(rows).toHaveCount(expected.length);
	for (const [index, game] of expected.entries()) {
		const row = rows.nth(index);
		await expect(row.locator("strong")).toHaveText(game.title);
		await expect(row.locator("small")).toHaveText(detailOf(game));
		await expect(row).toHaveText(new RegExp(`^.?${literal(`${game.title}${detailOf(game)}${winsOf(game)}`)}$`));
		await expect(row.getByRole("img", { name: barOf(game, one, two), exact: true })).toBeVisible();
	}
};

const expectPerGame = async (page: Page, one: string, two: string) => {
	const { perGame } = headToHead(one, two);
	const showAll = page.getByRole("button", { name: `Show all ${plural(perGame.length, "game")}` });

	await expectGameRows(page, one, two, perGame.slice(0, VISIBLE_GAMES));
	if (perGame.length <= VISIBLE_GAMES) {
		await expect(showAll).toHaveCount(0);
		return;
	}
	await showAll.click();
	await expectGameRows(page, one, two, perGame);
};

const expectBest = async (page: Page, one: string, two: string) => {
	const stats = headToHead(one, two);
	const region = page.getByRole("region", { name: "Strongest games" });
	if (stats.bestOne.length === 0 && stats.bestTwo.length === 0) {
		await expect(region).toHaveCount(0);
		return;
	}
	const expectSide = async (side: string, name: string, best: PerGame[], own: Wins, other: Wins) => {
		const row = region.getByTestId(side);
		await expect(row.locator("strong")).toHaveText(name);
		await expect(row.getByRole("listitem")).toHaveText(
			best.map((game) => new RegExp(`${literal(game.title)}${game[own]} – ${game[other]}$`)),
		);
		if (best.length === 0) await expect(row).toContainText("Nothing yet");
	};

	await expectSide("best-worst-playerOne", one, stats.bestOne, "oneWins", "twoWins");
	await expectSide("best-worst-playerTwo", two, stats.bestTwo, "twoWins", "oneWins");
	await expect(region).toContainText("Only games played together at least 3 times.");
};

export const recentRows = (one: string, two: string) => {
	const stats = headToHead(one, two);
	return stats.recent.slice(0, RECENT_ROWS).map((session) => {
		const left = stats.scoreOf(session, one);
		const right = stats.scoreOf(session, two);
		const scores = session.isCoop ? " · same team" : left !== null && right !== null ? ` · ${left} – ${right}` : "";
		const pill = session.isCoop ? `Co-op · ${session.teamWon ? "won" : "lost"}` : session.winner;
		return {
			href: `/sessions/update/${sessionId(session)}`,
			text: new RegExp(
				`^${displayDate(session.start)} (${literal(session.game)}|.) ${literal(`${session.game}${scores} ${pill} ${duration(session.minutes)}`)}$`,
			),
		};
	});
};

const expectRecent = async (page: Page, one: string, two: string) => {
	const rows = recentRows(one, two);
	const links = page.getByRole("region", { name: "Recent sessions together" }).getByRole("link");

	await expect(links).toHaveCount(rows.length);
	for (const [index, row] of rows.entries()) {
		await expect(links.nth(index)).toHaveAccessibleName(row.text);
		await expect(links.nth(index)).toHaveAttribute("href", row.href);
	}
};

export const expectNothingTogether = async (page: Page, one: string, two: string) => {
	await expect(page.getByRole("heading", { name: "No sessions together yet" })).toBeVisible();
	await expect(page.getByText(`${one} and ${two} haven't played together yet.`)).toBeVisible();
	for (const name of ["Record over all sessions together", "Together", "Per game", "Recent sessions together"]) {
		await expect(page.getByRole("region", { name, exact: true })).toHaveCount(0);
	}
};

export const expectComparison = async (page: Page, one: string, two: string) => {
	await expectOverall(page, one, two);
	if (headToHead(one, two).together.length === 0) {
		await expectNothingTogether(page, one, two);
		return;
	}
	await expect(page.getByRole("heading", { name: "No sessions together yet" })).toHaveCount(0);
	await expect(page.getByRole("link", { name: /^Most played/ })).toBeVisible({ timeout: 15_000 });
	await expectRecord(page, one, two);
	await expectForm(page, one, two);
	await expectTiles(page, one, two);
	await expectBest(page, one, two);
	await expectRecent(page, one, two);
	await expectPerGame(page, one, two);
};
