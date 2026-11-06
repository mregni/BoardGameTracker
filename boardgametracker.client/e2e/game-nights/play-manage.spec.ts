import type { APIRequestContext, Page } from "@playwright/test";
import { expect, test } from "../support/fixtures";
import { duration, whole } from "../support/format";
import {
	api,
	createNight,
	displayDateTime,
	displayTime,
	gameId,
	openNight,
	playerId,
	removeCreatedData,
} from "../support/gameNights";
import {
	coopGame,
	expected,
	gameNights,
	games,
	linkedPlayer,
	locations,
	players,
	sessions,
	unscoredGame,
} from "../support/seedData";

const [firstNight] = gameNights;
const [, cafe] = locations;
const [, guest, third, absent, invitedLate] = players;
const title = "Night in progress";
const planned = games[3].title;
const other = "Redwood";
const mixedCoop = "City of the Great Machine";
const toolTip = "Finish or discard the running games first";

test.afterEach(async ({ request }) => {
	await removeCreatedData(request);
});

const minutesAgo = (minutes: number) => new Date(Date.now() - minutes * 60_000);

const liveNight = (request: APIRequestContext, startDate = minutesAgo(5)) =>
	createNight(request, {
		title,
		startDate,
		location: cafe,
		notes: "Bring snacks",
		suggestedGames: [planned, coopGame.title],
		rsvps: { [guest]: "accepted", [third]: "accepted", [absent]: "pending" },
	});

const openLive = async (page: Page, request: APIRequestContext) => {
	const id = await liveNight(request);
	await openNight(page, title, id);
	return id;
};

const main = (page: Page) => page.getByRole("main");
const sheet = (page: Page) => page.getByRole("dialog");
const running = (page: Page) => page.getByTestId("running-game");
const runningCard = (page: Page, game: string) => running(page).filter({ hasText: `Now playing: ${game}` });
const playedRows = (page: Page) => page.getByTestId("played-session");
const playerRow = (page: Page, name: string) => page.getByTestId("session-player-row").filter({ hasText: name });
const tableRows = (page: Page) => page.getByRole("table").getByRole("rowgroup").nth(1).getByRole("row");

const chooseGame = async (page: Page, game: string) => {
	await sheet(page).getByRole("combobox", { name: "Game", exact: true }).click();
	await page.getByRole("option", { name: new RegExp(`^${game} `) }).click();
};

const addPlayer = async (page: Page, combobox: string, name: string) => {
	await sheet(page).getByRole("combobox", { name: combobox, exact: true }).click();
	await page.getByRole("option", { name: new RegExp(`^${name} `) }).click();
};

const openStart = (page: Page) => main(page).getByRole("button", { name: "Start a game" }).click();

const startGame = async (page: Page, game: string) => {
	await openStart(page);
	await chooseGame(page, game);
	await sheet(page).getByRole("button", { name: "Start", exact: true }).click();
	await expect(runningCard(page, game)).toBeVisible();
};

const openFinish = async (page: Page, game: string) => {
	await runningCard(page, game).getByRole("button", { name: "Finish" }).click();
	await expect(sheet(page).getByRole("heading", { name: `Finish ${game}` })).toBeVisible();
};

const save = (page: Page) => sheet(page).getByRole("button", { name: "Save session" }).click();

const setScores = async (page: Page, scores: Record<string, number>) => {
	for (const [name, score] of Object.entries(scores)) {
		await sheet(page)
			.getByRole("spinbutton", { name: `Score of ${name}` })
			.fill(String(score));
	}
};

const logSession = async (page: Page, game: string, scores: Record<string, number>) => {
	await main(page).getByRole("button", { name: "Log session" }).click();
	await chooseGame(page, game);
	await setScores(page, scores);
	await save(page);
	await expect(sheet(page)).toBeHidden();
};

test.describe("a night in progress before any game", () => {
	test("gets its own section on the list with a start button", async ({ page, request }) => {
		const startDate = minutesAgo(5);
		const id = await liveNight(request, startDate);
		await page.goto("/game-nights");
		const live = page.getByRole("region", { name: "Playing now" });
		const card = live.getByTestId("game-night-card");

		await expect(card).toHaveCount(1);
		await expect(card.getByRole("link", { name: title })).toHaveAttribute("href", `/game-nights/${id}`);
		await expect(card).toContainText(`Playing since ${displayTime(startDate)}`);
		await expect(card).toContainText("3 of 4 coming");
		await expect(card.getByRole("button", { name: "Manage RSVPs" })).toBeVisible();
		await expect(card.getByRole("button", { name: "Copy invite link" })).toHaveCount(0);
		await expect(card.getByTestId("rsvp-names")).toHaveCount(0);
		await expect(card).toContainText("Bring snacks");

		await card.getByRole("button", { name: `Actions for ${title}` }).click();
		await expect(page.getByRole("menuitem")).toHaveText(["Edit", "Delete"]);
		await page.keyboard.press("Escape");

		await expect(page.getByRole("region", { name: "Upcoming" }).getByTestId("game-night-card")).toHaveCount(2);
		await expect(page.getByTestId("game-night-card").nth(1)).not.toHaveClass(/bg-primary\/15/);
	});

	test("opens the start sheet from the card and clears the address", async ({ page, request }) => {
		const id = await liveNight(request);
		await page.goto("/game-nights");

		await page.getByRole("region", { name: "Playing now" }).getByRole("button", { name: "Start a game" }).click();

		await expect(page.getByRole("dialog", { name: "Start a game" })).toBeVisible();
		await expect(page).toHaveURL(`/game-nights/${id}`);
	});

	test("takes over the dashboard card", async ({ page, request }) => {
		const id = await liveNight(request);
		await page.goto("/");
		const card = page.getByRole("region", { name: "Game night in progress" });

		await expect(card).toContainText("Tonight · playing now");
		await expect(card.getByRole("heading", { name: title })).toBeVisible();
		await expect(card).toContainText(`${cafe} · host ${linkedPlayer}`);
		await expect(card).toContainText(whole("3 coming"));
		await expect(card).toContainText("1 no answer");
		await expect(card.getByRole("link", { name: planned })).toHaveAttribute("href", `/games/${gameId(planned)}`);
		await expect(card.getByRole("link", { name: "Open night" })).toHaveAttribute("href", `/game-nights/${id}`);
		await expect(card.getByRole("link", { name: "2 more planned" })).toHaveAttribute("href", "/game-nights");
	});

	test("shows the header, the play actions and who is expected", async ({ page, request }) => {
		await openLive(page, request);
		const header = page.getByRole("article");

		await expect(header).toContainText("Tonight · playing now");
		await expect(header).toContainText(cafe);
		await expect(header).toContainText("Bring snacks");
		await expect(page.getByTestId("night-actions").getByRole("button")).toHaveText([
			"End night",
			"Log session",
			"Start a game",
		]);
		await expect(page.getByRole("region", { name: "Played tonight" })).toContainText(
			"Nothing played yet. Tap “Start a game” when a game begins;",
		);
		const table = page.getByRole("region", { name: "Expected players" });
		await expect(table.getByRole("button").filter({ has: page.getByRole("img") })).toHaveText([
			linkedPlayer,
			guest,
			third,
		]);
		await expect(page.getByRole("region", { name: "Planned games" }).getByRole("button")).toHaveText([
			planned,
			coopGame.title,
		]);
		await expect(running(page)).toHaveCount(0);
		await expect(page.getByRole("button", { name: /^Play .* again$/ })).toHaveCount(0);
		await expect(main(page)).not.toContainText("Games played");
	});

	test("keeps the RSVPs open on the detail page", async ({ page, request }) => {
		const id = await openLive(page, request);

		const rsvps = page.getByRole("region", { name: "RSVPs" });
		await expect(rsvps.getByRole("region", { name: "No answer (1)" })).toContainText(absent);
		await expect(rsvps.getByRole("button", { name: "Copy invite link" })).toBeVisible();
		await rsvps.getByRole("button", { name: "Manage RSVPs" }).click();
		const dialog = page.getByRole("dialog", { name: "Manage RSVPs" });
		await dialog
			.getByTestId(`rsvp-row-${playerId(absent)}`)
			.getByRole("button", { name: "Coming", exact: true })
			.click();

		await dialog.getByRole("button", { name: "Close" }).first().click();
		await expect(rsvps.getByRole("region", { name: "Coming (4)" })).toContainText(absent);

		const { gameNight } = await api(request, "get", `/api/gamenight/${id}`);
		const answered = gameNight.invitedPlayers.find((rsvp: { playerId: number }) => rsvp.playerId === playerId(absent));
		expect(answered.state).toBe("accepted");
		await expect(
			page.getByRole("region", { name: "Expected players" }).getByRole("button", { name: absent }),
		).toBeVisible();
	});

	test("records who is here and offers them when starting", async ({ page, request }) => {
		await openLive(page, request);

		await main(page).getByRole("button", { name: "Edit who's here" }).click();
		const dialog = page.getByRole("dialog", { name: "Who's here?" });
		await dialog.getByRole("button", { name: new RegExp(`^${absent} `) }).click();
		await dialog.getByRole("button", { name: new RegExp(`^${guest} `) }).click();
		await expect(dialog.getByRole("button", { name: new RegExp(`^${absent} `) })).toContainText("Here");
		await expect(dialog.getByRole("button", { name: new RegExp(`^${guest} `) })).toContainText("Not here");
		await dialog.getByRole("button", { name: "Save" }).click();

		await expect(page.getByText("Attendance saved")).toBeVisible();
		const table = page.getByRole("region", { name: "At the table" });
		await expect(table.getByRole("button").filter({ has: page.getByRole("img") })).toContainText([
			linkedPlayer,
			absent,
		]);
		await expect(table.getByRole("button", { name: guest })).toHaveCount(0);

		await openStart(page);

		await expect(sheet(page).getByRole("button", { name: /^Leave .* out$/ })).toHaveCount(3);
		await expect(sheet(page).getByRole("button", { name: `Leave ${absent} out` })).toBeVisible();
		await expect(sheet(page).getByRole("button", { name: `Leave ${guest} out` })).toHaveCount(0);
	});

	test("discards the attendance changes on cancel", async ({ page, request }) => {
		await openLive(page, request);

		await main(page).getByRole("button", { name: "Edit who's here" }).click();
		const dialog = page.getByRole("dialog", { name: "Who's here?" });
		await dialog.getByRole("button", { name: new RegExp(`^${absent} `) }).click();
		await dialog.getByRole("button", { name: "Cancel" }).click();
		await expect(dialog).toBeHidden();
		await page.reload();

		await expect(
			page
				.getByRole("region", { name: "Expected players" })
				.getByRole("button")
				.filter({ has: page.getByRole("img") }),
		).toHaveText([linkedPlayer, guest, third]);
	});
});

test.describe("a player who said yes and is not there yet", () => {
	const startWithout = async (page: Page, name: string) => {
		await openStart(page);
		await sheet(page)
			.getByRole("button", { name: `Leave ${name} out` })
			.click();
		await chooseGame(page, planned);
		await sheet(page).getByRole("button", { name: "Start", exact: true }).click();
		await expect(runningCard(page, planned)).toBeVisible();
	};

	test("stays on the list and is proposed for the next game", async ({ page, request }) => {
		await openLive(page, request);
		await startWithout(page, third);
		await page.reload();

		const table = page.getByRole("region", { name: "Expected players" });
		await expect(table.getByRole("button").filter({ has: page.getByRole("img") })).toHaveText([
			linkedPlayer,
			guest,
			third,
		]);

		await openStart(page);

		await expect(sheet(page).getByRole("button", { name: /^Leave .* out$/ })).toHaveText([third]);
	});

	test("stays away once marked as not here", async ({ page, request }) => {
		await openLive(page, request);
		await startWithout(page, third);

		await main(page).getByRole("button", { name: "Edit who's here" }).click();
		const dialog = page.getByRole("dialog", { name: "Who's here?" });
		await dialog.getByRole("button", { name: new RegExp(`^${third} `) }).click();
		await dialog.getByRole("button", { name: "Save" }).click();
		await expect(page.getByText("Attendance saved")).toBeVisible();
		await page.reload();

		const table = page.getByRole("region", { name: "At the table" });
		await expect(table.getByRole("button").filter({ has: page.getByRole("img") })).toHaveText([linkedPlayer, guest]);

		await openStart(page);

		await expect(sheet(page).getByRole("button", { name: /^Leave .* out$/ })).toHaveCount(0);
	});
});

test.describe("the start sheet", () => {
	test("starts with the expected players and needs a game", async ({ page, request }) => {
		await openLive(page, request);

		await openStart(page);

		await expect(sheet(page).getByRole("button", { name: /^Leave .* out$/ })).toHaveText([linkedPlayer, guest, third]);
		await expect(sheet(page).getByRole("button", { name: "Start", exact: true })).toBeDisabled();
	});

	test("groups the games of the night first", async ({ page, request }) => {
		await openLive(page, request);
		await openStart(page);

		await sheet(page).getByRole("combobox", { name: "Game", exact: true }).click();

		const tonight = page.getByRole("group", { name: "Tonight & planned" });
		await expect(tonight.getByRole("option")).toHaveCount(2);
		await expect(tonight.getByRole("option", { name: planned })).toBeVisible();
		await expect(tonight.getByRole("option", { name: coopGame.title })).toBeVisible();
		await expect(page.getByRole("group", { name: "All games" }).getByRole("option")).toHaveCount(games.length - 2);
	});

	test("explains co-op games when one is chosen", async ({ page, request }) => {
		await openLive(page, request);
		await openStart(page);

		await chooseGame(page, coopGame.title);
		await expect(sheet(page)).toContainText("Co-op game: you pick “We won” or “We lost” when you finish it.");

		await chooseGame(page, mixedCoop);
		await expect(sheet(page)).toContainText("This game can be played co-op: you can switch it on when you finish it.");

		await chooseGame(page, planned);
		await expect(sheet(page)).not.toContainText("co-op");
	});

	test("needs at least one player", async ({ page, request }) => {
		await openLive(page, request);
		await openStart(page);
		await chooseGame(page, planned);

		for (const name of [linkedPlayer, guest, third]) {
			await sheet(page)
				.getByRole("button", { name: `Leave ${name} out` })
				.click();
		}

		await expect(sheet(page).getByRole("button", { name: /^Leave .* out$/ })).toHaveCount(0);
		await expect(sheet(page)).not.toContainText("Tap a player to leave them out");
		await expect(sheet(page).getByRole("button", { name: "Start", exact: true })).toBeDisabled();
	});

	test("offers players who were left out and everyone else, attendees first", async ({ page, request }) => {
		await openLive(page, request);
		await openStart(page);

		await sheet(page)
			.getByRole("button", { name: `Leave ${guest} out` })
			.click();
		await sheet(page).getByRole("combobox", { name: "Who plays?", exact: true }).click();

		await expect(page.getByRole("option").first()).toContainText(guest);
		await expect(page.getByRole("option")).toHaveCount(players.length - 2);

		await page.getByRole("option", { name: new RegExp(`^${invitedLate} `) }).click();

		await expect(sheet(page).getByRole("button", { name: `Leave ${invitedLate} out` })).toBeVisible();
	});

	test("starts nothing on cancel", async ({ page, request }) => {
		await openLive(page, request);
		await openStart(page);
		await chooseGame(page, planned);

		await sheet(page).getByRole("button", { name: "Cancel" }).click();

		await expect(sheet(page)).toBeHidden();
		await expect(running(page)).toHaveCount(0);
	});

	test("warns about players who are in another game", async ({ page, request }) => {
		await openLive(page, request);
		await startGame(page, planned);

		await openStart(page);

		await expect(sheet(page).getByRole("button", { name: /^Leave .* out$/ })).toHaveCount(0);

		await addPlayer(page, "Who plays?", guest);

		await expect(sheet(page).getByRole("status")).toHaveText(`${guest} is playing ${planned}`);

		await chooseGame(page, other);
		await sheet(page).getByRole("button", { name: "Start", exact: true }).click();

		await expect(running(page)).toHaveCount(2);
		await expect(runningCard(page, other)).toContainText(guest);
		await expect(runningCard(page, planned)).toContainText(guest);
	});
});

test.describe("a running game", () => {
	test("shows who plays since when and locks ending the night", async ({ page, request }) => {
		const id = await openLive(page, request);

		await startGame(page, planned);

		await expect(page.getByText("Game started")).toBeVisible();
		const [game] = (await api(request, "get", `/api/gamenight/${id}`)).runningGames;
		await expect(runningCard(page, planned)).toContainText(
			`Started ${displayTime(new Date(game.start))} · 1 min · ${linkedPlayer}, ${guest}, ${third}`,
		);
		await expect(main(page).getByRole("button", { name: "End night" })).toBeDisabled();
		await expect(page.getByTestId("night-actions")).toContainText(toolTip);
		const panel = page.getByRole("region", { name: "Planned games" });
		await expect(panel.getByRole("button", { name: planned })).toBeVisible();
		await expect(panel).toContainText("playing now");
		await expect(page.getByRole("region", { name: "At the table" })).toBeVisible();

		await page.goto("/game-nights");
		await expect(page.getByRole("region", { name: "Playing now" })).toContainText("1 game on the table");
	});

	test("keeps running after a reload", async ({ page, request }) => {
		await openLive(page, request);
		await startGame(page, planned);

		await page.reload();

		await expect(runningCard(page, planned)).toBeVisible();
	});

	test("can be discarded without a session after confirming", async ({ page, request }) => {
		const id = await openLive(page, request);
		await startGame(page, planned);
		const discard = page.getByRole("button", { name: `Discard ${planned}` });

		await discard.click();
		await expect(sheet(page)).toContainText("The running game is removed and no session is saved.");
		await sheet(page).getByRole("button", { name: "Cancel" }).click();
		await expect(runningCard(page, planned)).toBeVisible();

		await discard.click();
		await sheet(page).getByRole("button", { name: "Discard" }).click();

		await expect(page.getByText("Running game discarded")).toBeVisible();
		await expect(running(page)).toHaveCount(0);
		await expect(page.getByRole("region", { name: "Played tonight" })).toContainText("Nothing played yet");
		await expect(main(page).getByRole("button", { name: "End night" })).toBeEnabled();
		expect((await api(request, "get", `/api/gamenight/${id}`)).sessions).toEqual([]);

		await page.goto("/game-nights");
		await expect(page.getByRole("region", { name: "Playing now" })).toContainText(title);
		await expect(page.getByRole("region", { name: "Playing now" })).not.toContainText("on the table");
	});

	test("does not save anything when the finish sheet is cancelled", async ({ page, request }) => {
		await openLive(page, request);
		await startGame(page, planned);

		await openFinish(page, planned);
		await sheet(page).getByRole("button", { name: "Cancel" }).click();

		await expect(sheet(page)).toBeHidden();
		await expect(runningCard(page, planned)).toBeVisible();
		await expect(playedRows(page)).toHaveCount(0);
	});

	test("prefills the finish sheet from the running game", async ({ page, request }) => {
		const id = await openLive(page, request);
		await startGame(page, planned);

		await openFinish(page, planned);
		const [game] = (await api(request, "get", `/api/gamenight/${id}`)).runningGames;

		await expect(sheet(page)).toContainText(`Started ${displayTime(new Date(game.start))} · 1 min · ${cafe}`);
		await expect(sheet(page).getByRole("combobox", { name: "Game" })).toHaveCount(0);
		await expect(sheet(page).getByRole("spinbutton", { name: "Duration" })).toHaveValue("1");
		await expect(sheet(page).getByRole("combobox", { name: "Location" })).toHaveText(cafe);
		await expect(page.getByTestId("session-player-row")).toHaveCount(3);
		await expect(sheet(page).getByRole("spinbutton", { name: `Score of ${guest}` })).toHaveValue("");
		await expect(sheet(page).getByRole("switch", { name: "Played co-op" })).toHaveCount(0);
	});

	test("needs a positive duration and a player to finish", async ({ page, request }) => {
		await openLive(page, request);
		await startGame(page, planned);
		await openFinish(page, planned);

		await sheet(page).getByRole("spinbutton", { name: "Duration" }).fill("0");
		await save(page);
		await expect(sheet(page)).toContainText("Duration must be greater than 0");

		await sheet(page).getByRole("spinbutton", { name: "Duration" }).fill("30");
		for (const name of [linkedPlayer, guest, third]) {
			await sheet(page)
				.getByRole("button", { name: `Remove ${name}`, exact: true })
				.click();
		}
		await save(page);

		await expect(sheet(page)).toContainText("Add at least one player to this session");
		await expect(runningCard(page, planned)).toBeVisible();
	});

	test("finishes into a session that shows up on every page", async ({ page, request }) => {
		const played = sessions.filter((session) => session.game === planned).length;
		const seats = (name: string) => sessions.filter((session) => session.scores.some((x) => x.player === name));
		const minutes = 40;
		const comment = "A close game";
		await openLive(page, request);
		await startGame(page, planned);

		await openFinish(page, planned);
		await sheet(page).getByRole("spinbutton", { name: "Duration" }).fill(String(minutes));
		await setScores(page, { [linkedPlayer]: 30, [guest]: 45, [third]: 20 });
		await expect(playerRow(page, guest).getByRole("button", { name: "Won" })).toHaveAttribute("aria-pressed", "true");
		await expect(playerRow(page, third).getByRole("button", { name: "Won" })).toHaveAttribute("aria-pressed", "false");
		await sheet(page).getByRole("textbox", { name: "Comment" }).fill(comment);
		await save(page);

		await expect(page.getByText("Session logged")).toBeVisible();
		await expect(sheet(page)).toBeHidden();
		await expect(running(page)).toHaveCount(0);
		await expect(main(page).getByRole("button", { name: "End night" })).toBeEnabled();

		await expect(playedRows(page)).toHaveCount(1);
		const row = playedRows(page).first();
		await expect(row).toContainText("#1");
		await expect(row).toContainText(planned);
		await expect(row).toContainText(`${guest} 45 · ${linkedPlayer} 30 · ${third} 20`);
		await expect(row).toContainText(duration(minutes));
		await expect(row.getByRole("img", { name: "won", exact: true })).toHaveCount(1);

		await expect(main(page)).toContainText(/Games played1\D/);
		await expect(main(page)).toContainText(`Time in games${duration(minutes)}`);
		await expect(main(page)).toContainText(`Leading tonight${guest}`);
		await expect(main(page)).toContainText("1 of 1 game won");
		await expect(page.getByRole("region", { name: "Planned games" })).toContainText("played");
		await expect(page.getByRole("region", { name: "Planned games" })).not.toContainText("playing now");
		await expect(page.getByRole("button", { name: `Play ${planned} again` })).toBeVisible();

		await page.goto(`/games/${gameId(planned)}`);
		await expect(page.getByRole("region", { name: new RegExp(`^Played ${played + 1} times`) })).toBeVisible();

		await page.goto(`/games/${gameId(planned)}/sessions`);
		await expect(tableRows(page)).toHaveCount(played + 1);
		await expect(tableRows(page).first()).toContainText(comment);
		await expect(tableRows(page).first().getByRole("cell").nth(2)).toHaveText(duration(minutes));

		await page.goto(`/players/${playerId(guest)}/sessions`);
		await expect(tableRows(page)).toHaveCount(seats(guest).length + 1);
		await expect(tableRows(page).first()).toContainText(planned);

		await page.goto("/");
		await expect(page.getByRole("main")).toContainText(`Sessions${expected.sessionCount + 1}`);
		await expect(page.getByRole("main")).toContainText(`${expected.sessionsThisMonth + 1} this month`);
		await expect(page.getByRole("main")).toContainText(
			`This month you played ${expected.linkedPlayedThisMonth + 1} of the group's ${expected.sessionsThisMonth + 1} sessions and won ${expected.linkedWonThisMonth}.`,
		);
		const last = page.getByRole("region", { name: "Last sessions" }).getByRole("link").first();
		await expect(last).toHaveAttribute("href", `/games/${gameId(planned)}/sessions`);
		await expect(last).toContainText("3 players");
		await expect(last).toContainText(guest);
	});

	test("opens the new session to edit and returns to the night", async ({ page, request }) => {
		const id = await openLive(page, request);
		await logSession(page, planned, { [linkedPlayer]: 10 });

		await playedRows(page).first().getByRole("link").click();
		await expect(page).toHaveURL(/\/sessions\/update\/\d+$/);
		await page.getByRole("spinbutton", { name: "Duration" }).fill("77");
		await page.getByRole("main").getByRole("button", { name: "Update session" }).click();

		await expect(page).toHaveURL(`/game-nights/${id}`);
		await expect(playedRows(page).first()).toContainText(duration(77));
	});

	test("finishes one of two games and leaves the other running", async ({ page, request }) => {
		await openLive(page, request);
		await startGame(page, planned);
		await openStart(page);
		await chooseGame(page, other);
		await addPlayer(page, "Who plays?", absent);
		await sheet(page).getByRole("button", { name: "Start", exact: true }).click();
		await expect(running(page)).toHaveCount(2);

		await openFinish(page, planned);
		await setScores(page, { [guest]: 5 });
		await save(page);

		await expect(running(page)).toHaveCount(1);
		await expect(runningCard(page, other)).toBeVisible();
		await expect(main(page).getByRole("button", { name: "End night" })).toBeDisabled();
	});
});

test.describe("co-op results", () => {
	test("needs a team result, then shows the win without scores", async ({ page, request }) => {
		await openLive(page, request);
		await startGame(page, coopGame.title);
		await openFinish(page, coopGame.title);

		await expect(sheet(page).getByRole("switch", { name: "Played co-op" })).toBeChecked();
		await expect(sheet(page).getByRole("spinbutton", { name: /^Score of/ })).toHaveCount(0);
		await expect(playerRow(page, guest).getByRole("button", { name: "Won" })).toHaveCount(0);

		await save(page);
		await expect(sheet(page)).toContainText("Pick whether the team won or lost");
		await expect(sheet(page)).toBeVisible();

		await sheet(page).getByRole("button", { name: "We won" }).click();
		await sheet(page).getByRole("spinbutton", { name: "Team score" }).fill("88");
		await save(page);

		await expect(sheet(page)).toBeHidden();
		const row = playedRows(page).first();
		await expect(row).toContainText("Co-op · won");
		await expect(row).toContainText([linkedPlayer, guest, third].join(" · "));
		await expect(row.getByRole("img", { name: "won", exact: true })).toHaveCount(0);
		await expect(main(page)).toContainText(`Leading tonight${[linkedPlayer, guest, third].join(", ")}`);
		await expect(main(page)).toContainText("1 of 1 game won");
	});

	test("shows a lost game with no winner", async ({ page, request }) => {
		await openLive(page, request);
		await startGame(page, coopGame.title);
		await openFinish(page, coopGame.title);

		await sheet(page).getByRole("button", { name: "We lost" }).click();
		await save(page);

		await expect(sheet(page)).toBeHidden();
		await expect(playedRows(page).first()).toContainText("Co-op · lost");
		await expect(main(page)).toContainText("Leading tonightNo winner yet");
		await expect(main(page)).not.toContainText("games won");
	});

	test("lets a game with an optional co-op mode switch it on when finishing", async ({ page, request }) => {
		await openLive(page, request);
		await startGame(page, mixedCoop);
		await openFinish(page, mixedCoop);
		const coop = sheet(page).getByRole("switch", { name: "Played co-op" });

		await expect(coop).not.toBeChecked();
		await expect(sheet(page).getByRole("spinbutton", { name: `Score of ${guest}` })).toBeVisible();

		await sheet(page).getByText("Played co-op").click();

		await expect(coop).toBeChecked();
		await expect(sheet(page).getByRole("spinbutton", { name: `Score of ${guest}` })).toHaveCount(0);

		await sheet(page).getByRole("button", { name: "We won" }).click();
		await save(page);

		await expect(playedRows(page).first()).toContainText("Co-op · won");
	});

	test("scores a game with an optional co-op mode normally when left off", async ({ page, request }) => {
		await openLive(page, request);
		await startGame(page, mixedCoop);
		await openFinish(page, mixedCoop);

		await setScores(page, { [guest]: 9 });
		await save(page);

		await expect(playedRows(page).first()).not.toContainText("Co-op");
		await expect(playedRows(page).first()).toContainText(`${guest} 9`);
		await expect(main(page)).toContainText(`Leading tonight${guest}`);
	});
});

test("finishes a game without scoring with only a winner", async ({ page, request }) => {
	await openLive(page, request);
	await startGame(page, unscoredGame.title);
	await openFinish(page, unscoredGame.title);

	await expect(sheet(page).getByRole("spinbutton", { name: /^Score of/ })).toHaveCount(0);
	await expect(sheet(page).getByRole("combobox", { name: "Players", exact: true })).toBeVisible();

	await playerRow(page, third).getByRole("button", { name: "Won" }).click();
	await save(page);

	await expect(sheet(page)).toBeHidden();
	await expect(playedRows(page).first()).toContainText(third);
	await expect(playedRows(page).first()).not.toContainText(/ \d+ · /);
	await expect(main(page)).toContainText(`Leading tonight${third}`);
});

test("lets you pick another winner than the best score", async ({ page, request }) => {
	await openLive(page, request);
	await startGame(page, planned);
	await openFinish(page, planned);

	await setScores(page, { [linkedPlayer]: 30, [guest]: 45 });
	await playerRow(page, guest).getByRole("button", { name: "Won" }).click();
	await playerRow(page, linkedPlayer).getByRole("button", { name: "Won" }).click();
	await save(page);

	await expect(sheet(page)).toBeHidden();
	await expect(main(page)).toContainText(`Leading tonight${linkedPlayer}`);
	await expect(playedRows(page).first()).toContainText(`${guest} 45 · ${linkedPlayer} 30`);
});

test.describe("logging a session directly", () => {
	test("prefills the sheet from the night and needs a game", async ({ page, request }) => {
		await openLive(page, request);

		await main(page).getByRole("button", { name: "Log session" }).click();

		await expect(sheet(page).getByRole("heading", { name: "Log session" })).toBeVisible();
		await expect(sheet(page).getByRole("combobox", { name: "Location" })).toHaveText(cafe);
		await expect(sheet(page).getByRole("spinbutton", { name: "Duration" })).toHaveValue("30");
		await expect(page.getByTestId("session-player-row")).toHaveCount(3);

		await save(page);

		await expect(sheet(page)).toContainText("Game is required");
		await expect(sheet(page)).toBeVisible();
	});

	test("takes the longest play time of the chosen game", async ({ page, request }) => {
		const game = games[3];
		await openLive(page, request);

		await main(page).getByRole("button", { name: "Log session" }).click();
		await chooseGame(page, game.title);

		await expect(sheet(page).getByRole("spinbutton", { name: "Duration" })).toHaveValue(String(game.maxPlayTime));
	});

	test("refuses a start in the future", async ({ page, request }) => {
		await openLive(page, request);
		await main(page).getByRole("button", { name: "Log session" }).click();
		await chooseGame(page, planned);
		const year = sheet(page).getByRole("spinbutton", { name: "year, Date" });

		await year.click();
		await page.keyboard.type(String(new Date().getFullYear() + 1));
		await save(page);

		await expect(sheet(page)).toContainText("Start can't be in the future");
	});

	test("logs games in order and shares the lead on a tie", async ({ page, request }) => {
		const id = await openLive(page, request);
		const game = games[2];

		await logSession(page, planned, { [linkedPlayer]: 10, [guest]: 20 });
		await expect(page.getByText("Session logged")).toBeVisible();
		await expect(main(page)).toContainText(`Leading tonight${guest}`);

		await logSession(page, game.title, { [linkedPlayer]: 30, [guest]: 20 });

		await expect(playedRows(page)).toHaveCount(2);
		await expect(playedRows(page).nth(0)).toContainText(`#2`);
		await expect(playedRows(page).nth(0)).toContainText(game.title);
		await expect(playedRows(page).nth(0)).toContainText(duration(game.maxPlayTime));
		await expect(playedRows(page).nth(1)).toContainText(`#1`);
		await expect(playedRows(page).nth(1)).toContainText(planned);
		await expect(main(page)).toContainText(/Games played2\D/);
		await expect(main(page)).toContainText(`Leading tonight${linkedPlayer}, ${guest}`);
		await expect(main(page)).toContainText("1 of 2 games won");

		const detail = await api(request, "get", `/api/gamenight/${id}`);
		expect(detail.sessions.every((session: { locationId: number }) => session.locationId === 2)).toBe(true);
	});

	test("adds players from outside the table to the table", async ({ page, request }) => {
		await openLive(page, request);

		await main(page).getByRole("button", { name: "Log session" }).click();
		await chooseGame(page, planned);
		await addPlayer(page, "Players & scores", invitedLate);
		await save(page);

		await expect(sheet(page)).toBeHidden();
		await expect(
			page.getByRole("region", { name: "At the table" }).getByRole("button", { name: invitedLate }),
		).toBeVisible();
		await expect(playedRows(page).first()).toContainText(invitedLate);
	});
});

test.describe("play again", () => {
	test("offers each played game once and starts it with the same players", async ({ page, request }) => {
		await openLive(page, request);
		await logSession(page, planned, { [linkedPlayer]: 1 });
		await logSession(page, planned, { [linkedPlayer]: 2 });
		await logSession(page, other, { [linkedPlayer]: 3 });
		await expect(playedRows(page)).toHaveCount(3);

		await expect(page.getByRole("button", { name: /^Play .* again$/ })).toHaveText([other, planned]);

		await page.getByRole("button", { name: `Play ${planned} again` }).click();

		await expect(sheet(page).getByRole("combobox", { name: "Game", exact: true })).toContainText(planned);
		await expect(sheet(page).getByRole("button", { name: /^Leave .* out$/ })).toHaveText([linkedPlayer, guest, third]);

		await sheet(page)
			.getByRole("button", { name: `Leave ${third} out` })
			.click();
		await sheet(page).getByRole("button", { name: "Start", exact: true }).click();

		await expect(runningCard(page, planned)).toContainText(`${linkedPlayer}, ${guest}`);
		await expect(runningCard(page, planned)).not.toContainText(third);
	});

	test("leaves out players who are busy in another game", async ({ page, request }) => {
		await openLive(page, request);
		await logSession(page, planned, { [linkedPlayer]: 1 });
		await openStart(page);
		await chooseGame(page, other);
		await sheet(page)
			.getByRole("button", { name: `Leave ${linkedPlayer} out` })
			.click();
		await sheet(page)
			.getByRole("button", { name: `Leave ${third} out` })
			.click();
		await sheet(page).getByRole("button", { name: "Start", exact: true }).click();
		await expect(runningCard(page, other)).toBeVisible();

		await page.getByRole("button", { name: `Play ${planned} again` }).click();

		await expect(sheet(page).getByRole("button", { name: /^Leave .* out$/ })).toHaveText([linkedPlayer, third]);
	});
});

test.describe("ending and reopening", () => {
	test("keeps the night open when ending is cancelled", async ({ page, request }) => {
		await openLive(page, request);

		await main(page).getByRole("button", { name: "End night" }).click();
		await expect(sheet(page)).toContainText("Nothing can be started or logged on this night until it is reopened.");
		await sheet(page).getByRole("button", { name: "Cancel" }).click();

		await expect(sheet(page)).toBeHidden();
		await expect(main(page).getByRole("button", { name: "Start a game" })).toBeVisible();
	});

	test("ends the night, locks it, moves it to past and can reopen it", async ({ page, request }) => {
		const id = await openLive(page, request);
		await logSession(page, planned, { [linkedPlayer]: 10, [guest]: 5 });

		await main(page).getByRole("button", { name: "End night" }).click();
		await sheet(page).getByRole("button", { name: "End night" }).click();

		await expect(page.getByText("Game night ended")).toBeVisible();
		await expect(page.getByTestId("night-actions").getByRole("button")).toHaveText(["Reopen night"]);
		await expect(page.getByRole("button", { name: /^Play .* again$/ })).toHaveCount(0);
		await expect(page.getByRole("region", { name: "Played", exact: true })).toBeVisible();
		await expect(main(page)).toContainText(`Night winner${linkedPlayer}`);

		await page.goto("/game-nights");
		await expect(page.getByRole("region", { name: "Playing now" })).toHaveCount(0);
		const past = page.getByRole("region", { name: "Past" }).getByRole("link", { name: new RegExp(title) });
		await expect(past).toContainText(`${cafe} · 3 attended · 1 game played`);
		await expect(past).toContainText(`Most wins: ${linkedPlayer}`);

		await page.goto("/");
		await expect(page.getByRole("region", { name: "Next game night" })).toContainText(firstNight.title);

		await page.goto("/game-nights?view=stats");
		await expect(page.getByRole("main")).toContainText(/Game nights2\D/);

		await openNight(page, title, id);
		await main(page).getByRole("button", { name: "Reopen night" }).click();

		await expect(page.getByText("Game night reopened")).toBeVisible();
		await expect(main(page).getByRole("button", { name: "Start a game" })).toBeVisible();
		await expect(page.getByRole("button", { name: `Play ${planned} again` })).toBeVisible();
		await expect(main(page)).toContainText(`Leading tonight${linkedPlayer}`);

		await page.goto("/game-nights");
		await expect(page.getByRole("region", { name: "Playing now" })).toContainText(title);
	});

	test("does not tell you to start a game on an ended night", async ({ page, request }) => {
		await openLive(page, request);

		await main(page).getByRole("button", { name: "End night" }).click();
		await sheet(page).getByRole("button", { name: "End night" }).click();

		await expect(page.getByText("Game night ended")).toBeVisible();
		await expect(main(page).getByRole("button", { name: "Start a game" })).toHaveCount(0);
		const played = page.getByRole("region", { name: "Played", exact: true });
		await expect(played).toContainText("Nothing was logged for this night.");
		await expect(played).not.toContainText(/Start a game|Log session/);
	});

	test("lists an empty ended night as nothing logged", async ({ page, request }) => {
		await openLive(page, request);

		await main(page).getByRole("button", { name: "End night" }).click();
		await sheet(page).getByRole("button", { name: "End night" }).click();

		await expect(page.getByText("Game night ended")).toBeVisible();

		await page.goto("/game-nights");
		await expect(
			page.getByRole("region", { name: "Past" }).getByRole("link", { name: new RegExp(title) }),
		).toContainText(`${cafe} · no games logged`);
	});
});

test.describe("a night that ran out of time", () => {
	test("is past, can't start games but can still log what was played", async ({ page, request }) => {
		const id = await liveNight(request, minutesAgo(13 * 60));

		await page.goto("/game-nights");
		await expect(
			page.getByRole("region", { name: "Past" }).getByRole("link", { name: new RegExp(title) }),
		).toBeVisible();

		await openNight(page, title, id);
		await expect(page.getByTestId("night-actions").getByRole("button")).toHaveText(["Log session"]);
		await expect(page.getByRole("region", { name: "Played", exact: true })).toContainText(
			"Nothing was logged for this night. Use “Log session” to add what was played.",
		);

		await logSession(page, planned, { [linkedPlayer]: 3 });

		await expect(playedRows(page)).toHaveCount(1);
		await expect(main(page)).toContainText(`Night winner${linkedPlayer}`);
		await expect(page.getByRole("button", { name: /^Play .* again$/ })).toHaveCount(0);
	});
});

test.describe("logging a session", () => {
	const startOfLoggedSession = async (page: Page, request: APIRequestContext, id: number) => {
		await logSession(page, planned, { [linkedPlayer]: 3 });
		const { sessions: logged } = await api(request, "get", `/api/gamenight/${id}`);
		return new Date(logged[0].start);
	};

	test("starts on the date of a night that is over", async ({ page, request }) => {
		const startDate = minutesAgo(3 * 24 * 60);
		const id = await liveNight(request, startDate);
		await openNight(page, title, id);

		expect(displayDateTime(await startOfLoggedSession(page, request, id))).toBe(displayDateTime(startDate));
	});

	test("ends now on a live night", async ({ page, request }) => {
		const id = await liveNight(request, minutesAgo(3 * 60));
		await openNight(page, title, id);

		const start = await startOfLoggedSession(page, request, id);

		const minutes = games[3].maxPlayTime;
		expect(Math.abs(start.getTime() - minutesAgo(minutes).getTime())).toBeLessThan(2 * 60_000);
	});
});

test.describe("a night that ran out of time with a game still running", () => {
	test("keeps the game finishable and only then counts as over", async ({ page, request }) => {
		const id = await openLive(page, request);
		await startGame(page, planned);
		const { gameNight } = await api(request, "get", `/api/gamenight/${id}`);
		await api(request, "put", "/api/gamenight", {
			id,
			title,
			notes: "",
			startDate: minutesAgo(13 * 60).toISOString(),
			hostId: gameNight.hostId,
			locationId: gameNight.locationId,
			suggestedGameIds: [],
			invitedPlayerIds: gameNight.invitedPlayers.map((rsvp: { playerId: number }) => rsvp.playerId),
		});
		await page.reload();

		await expect(runningCard(page, planned)).toBeVisible();
		await expect(main(page).getByRole("button", { name: "End night" })).toBeDisabled();

		await openFinish(page, planned);
		await setScores(page, { [guest]: 7 });
		await save(page);

		await expect(sheet(page)).toBeHidden();
		await expect(page.getByTestId("night-actions").getByRole("button")).toHaveText(["Log session"]);
		await expect(main(page)).toContainText(`Night winner${guest}`);
	});
});

test.describe("deleting a night in use", () => {
	test("warns about running games and removes them", async ({ page, request }) => {
		await openLive(page, request);
		await startGame(page, planned);

		await main(page).getByRole("button", { name: "Delete", exact: true }).click();

		await expect(sheet(page)).toContainText(
			`Delete “${title}”? This can't be undone. 1 running game is discarded. RSVPs are deleted.`,
		);

		await sheet(page).getByRole("button", { name: "Delete" }).click();

		await expect(page).toHaveURL("/game-nights");
		await expect(page.getByRole("region", { name: "Playing now" })).toHaveCount(0);
	});

	test("warns about sessions and keeps them unlinked", async ({ page, request }) => {
		const id = await openLive(page, request);
		await logSession(page, planned, { [linkedPlayer]: 10 });
		const [session] = (await api(request, "get", `/api/gamenight/${id}`)).sessions;

		await main(page).getByRole("button", { name: "Delete", exact: true }).click();

		await expect(sheet(page)).toContainText(
			`Delete “${title}”? This can't be undone. 1 logged session is kept but no longer linked to this night. RSVPs are deleted.`,
		);

		await sheet(page).getByRole("button", { name: "Delete" }).click();
		await expect(page).toHaveURL("/game-nights");

		const kept = await api(request, "get", `/api/session/${session.id}`);
		await api(request, "delete", `/api/session/${session.id}`);
		expect(kept.gameNightId).toBeNull();
	});
});

test("takes a deleted player out of the night and the running game", async ({ page, request }) => {
	const name = "Sweetums";
	const { id: playerToDelete } = await api(request, "post", "/api/player", { name, image: null });
	const { id } = await api(request, "post", "/api/gamenight", {
		title,
		notes: "",
		startDate: minutesAgo(5).toISOString(),
		hostId: playerId(linkedPlayer),
		locationId: 2,
		suggestedGameIds: [],
		invitedPlayerIds: [playerToDelete, playerId(guest)],
	});
	const { gameNight } = await api(request, "get", `/api/gamenight/${id}`);
	for (const rsvp of gameNight.invitedPlayers as { id: number; playerId: number }[]) {
		await api(request, "put", "/api/gamenight/rsvp", {
			id: rsvp.id,
			gameNightId: id,
			playerId: rsvp.playerId,
			state: "accepted",
		});
	}
	await openNight(page, title, id);
	await startGame(page, planned);
	await expect(runningCard(page, planned)).toContainText(name);

	await api(request, "delete", `/api/player/${playerToDelete}`);
	await page.reload();

	await expect(runningCard(page, planned)).toContainText(`${linkedPlayer}, ${guest}`);
	await expect(runningCard(page, planned)).not.toContainText(name);
	await expect(page.getByRole("region", { name: "At the table" })).toContainText(guest);
	await expect(page.getByRole("region", { name: "At the table" })).not.toContainText(name);
});

test.describe("other roles", () => {
	test("a reader can follow the night but not touch it", async ({ page, readerPage, request }) => {
		const id = await openLive(page, request);
		await startGame(page, planned);
		await logSession(page, other, { [linkedPlayer]: 10 });

		await openNight(readerPage, title, id);

		await expect(runningCard(readerPage, planned)).toBeVisible();
		await expect(readerPage.getByTestId("played-session").getByRole("link")).toHaveCount(0);
		await expect(readerPage.getByTestId("night-actions")).toHaveCount(0);
		for (const name of [
			"Finish",
			`Discard ${planned}`,
			"Start a game",
			"Log session",
			"End night",
			"Edit who's here",
			`Play ${other} again`,
		]) {
			await expect(readerPage.getByRole("main").getByRole("button", { name, exact: true })).toHaveCount(0);
		}

		await readerPage.goto("/game-nights");
		await expect(readerPage.getByRole("region", { name: "Playing now" })).toContainText(title);
		await expect(
			readerPage.getByRole("region", { name: "Playing now" }).getByRole("button", { name: "Start a game" }),
		).toHaveCount(0);

		await readerPage.goto(`/game-nights/${id}?start=true`);
		await expect(runningCard(readerPage, planned)).toBeVisible();
		await expect(readerPage).toHaveURL(`/game-nights/${id}`);
		await expect(readerPage.getByRole("dialog")).toHaveCount(0);
	});

	test("a user can play a night", async ({ writerPage: page, request }) => {
		await openLive(page, request);

		await startGame(page, planned);
		await openFinish(page, planned);
		await save(page);

		await expect(sheet(page)).toBeHidden();
		await expect(page.getByTestId("played-session")).toHaveCount(1);
	});
});
