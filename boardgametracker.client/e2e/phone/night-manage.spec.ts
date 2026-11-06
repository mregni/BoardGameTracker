import type { Page } from "@playwright/test";
import { credentials, expect, test } from "../support/fixtures";
import {
	apiFor,
	expectContentClearsBars,
	expectDialogFits,
	expectFooterInView,
	expectNoSidewaysScroll,
	footerOf,
	gameId,
	locationId,
	phone,
	playerId,
	settled,
	signInOnTouchPhone,
	smallPhone,
} from "../support/phone";
import { games, linkedPlayer, locations, neverPlayedGame, players } from "../support/seedData";

const guests = players.slice(0, 3);
const suggested = neverPlayedGame.title;

type Api = Awaited<ReturnType<typeof apiFor>>;

type NightDetail = {
	gameNight: { invitedPlayers: { id: number; playerId: number }[] };
	runningGames: { id: number }[];
	sessions: { id: number; gameId: number }[];
};

const createActiveNight = async (api: Api, title: string) => {
	const { id } = await api.post("/api/gamenight", {
		title,
		notes: "",
		startDate: new Date(Date.now() - 3_600_000).toISOString(),
		hostId: playerId(linkedPlayer),
		locationId: locationId(locations[0]),
		suggestedGameIds: [gameId(suggested)],
		invitedPlayerIds: guests.map(playerId),
	});
	const detail = await api.get<NightDetail>(`/api/gamenight/${id}`);
	for (const rsvp of detail.gameNight.invitedPlayers) {
		await api.put("/api/gamenight/rsvp", {
			id: rsvp.id,
			gameNightId: id,
			playerId: rsvp.playerId,
			state: "accepted",
		});
	}
	return id;
};

const removeNight = async (api: Api, id: number) => {
	const detail = await api.get<NightDetail>(`/api/gamenight/${id}`);
	for (const running of detail.runningGames) await api.tidy(`/api/gamenight/running-games/${running.id}`);
	for (const session of detail.sessions) await api.tidy(`/api/session/${session.id}`);
	await api.tidy(`/api/gamenight/${id}`);
};

const startButton = (page: Page) => page.getByRole("main").getByRole("button", { name: "Start a game" });
const logButton = (page: Page) => page.getByRole("main").getByRole("button", { name: "Log session" });

const startGame = async (page: Page) => {
	await startButton(page).click();
	const sheet = page.getByRole("dialog", { name: "Start a game" });
	await sheet.getByRole("combobox").first().click();
	await page.getByRole("option", { name: new RegExp(`^${suggested}`) }).click();
	await footerOf(sheet).getByRole("button", { name: "Start" }).click();
	await expect(sheet).toBeHidden();
};

test.describe("an active game night", () => {
	test.beforeEach(async ({ page }) => {
		await page.setViewportSize(phone);
	});

	test("keeps start and log in a bar above the bottom navigation", async ({ page }) => {
		const api = await apiFor(page);
		const id = await createActiveNight(api, "Phone night bar");
		try {
			await page.goto(`/game-nights/${id}`);
			await settled(page);
			const nav = await page.getByRole("navigation").boundingBox();

			await expect(startButton(page)).toBeInViewport({ ratio: 1 });
			await expect(logButton(page)).toBeInViewport({ ratio: 1 });
			const start = await startButton(page).boundingBox();
			const log = await logButton(page).boundingBox();
			expect((start?.y ?? 0) + (start?.height ?? 0)).toBeLessThanOrEqual(nav?.y ?? 0);
			expect((start?.x ?? 0) + (start?.width ?? 0)).toBeLessThanOrEqual(log?.x ?? 0);
			await expectNoSidewaysScroll(page);
			await expectContentClearsBars(page);

			await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
			await expect(startButton(page)).toBeInViewport({ ratio: 1 });
		} finally {
			await removeNight(api, id);
		}
	});

	test("starts, finishes and ends a game with the sheets", async ({ page }) => {
		const api = await apiFor(page);
		const id = await createActiveNight(api, "Phone night play");
		try {
			await page.goto(`/game-nights/${id}`);
			await settled(page);

			await startButton(page).click();
			const startSheet = page.getByRole("dialog", { name: "Start a game" });
			await expectDialogFits(startSheet);
			await expectFooterInView(page, startSheet);
			await expect(footerOf(startSheet).getByRole("button", { name: "Start" })).toBeDisabled();
			await footerOf(startSheet).getByRole("button", { name: "Cancel" }).click();
			await expect(startSheet).toBeHidden();

			await startGame(page);
			const finish = page.getByRole("main").getByRole("button", { name: "Finish" });
			await expect(finish).toBeVisible();
			await expect(page.getByRole("main")).toContainText(suggested);
			await expectContentClearsBars(page);

			await finish.click();
			const finishSheet = page.getByRole("dialog", { name: `Finish ${suggested}` });
			await expectDialogFits(finishSheet);
			const save = footerOf(finishSheet).getByRole("button", { name: "Save session" });
			await save.scrollIntoViewIfNeeded();
			await expect(save).toBeInViewport({ ratio: 1 });
			await save.click();
			await expect(finishSheet).toBeHidden();
			await expect(finish).toHaveCount(0);

			const detail = await api.get<NightDetail>(`/api/gamenight/${id}`);
			expect(detail.runningGames).toHaveLength(0);
			expect(detail.sessions).toHaveLength(1);
			expect(detail.sessions[0].gameId).toBe(gameId(suggested));

			await page.getByRole("button", { name: "End night" }).click();
			const end = page.getByRole("dialog", { name: "End this game night?" });
			await expectFooterInView(page, end);
			await footerOf(end).getByRole("button", { name: "End night" }).click();
			await expect(end).toBeHidden();

			await expect(startButton(page)).toHaveCount(0);
			await expect(page.getByRole("button", { name: "Reopen night" })).toBeVisible();
			await page.getByRole("button", { name: "Reopen night" }).click();
			await expect(startButton(page)).toBeInViewport({ ratio: 1 });
		} finally {
			await removeNight(api, id);
		}
	});

	test("shows the save button of the finish sheet without scrolling", async ({ page }) => {
		const api = await apiFor(page);
		const id = await createActiveNight(api, "Phone night finish footer");
		try {
			await page.goto(`/game-nights/${id}`);
			await settled(page);
			await startGame(page);

			await page.getByRole("main").getByRole("button", { name: "Finish" }).click();

			test.fail(true, "BUG: with three players Save session of the finish sheet starts below the screen");
			await expectFooterInView(page, page.getByRole("dialog", { name: `Finish ${suggested}` }));
		} finally {
			await removeNight(api, id);
		}
	});

	test("discards a running game", async ({ page }) => {
		const api = await apiFor(page);
		const id = await createActiveNight(api, "Phone night discard");
		try {
			await page.goto(`/game-nights/${id}`);
			await settled(page);
			await startGame(page);

			await page.getByRole("button", { name: `Discard ${suggested}` }).click();
			const dialog = page.getByRole("dialog", { name: "Discard this game?" });
			await expectFooterInView(page, dialog);
			await footerOf(dialog).getByRole("button").last().click();

			await expect(page.getByRole("main").getByRole("button", { name: "Finish" })).toHaveCount(0);
			await expect.poll(async () => (await api.get<NightDetail>(`/api/gamenight/${id}`)).runningGames).toEqual([]);
		} finally {
			await removeNight(api, id);
		}
	});

	test("logs a played game with the sheet", async ({ page }) => {
		const api = await apiFor(page);
		const id = await createActiveNight(api, "Phone night log");
		try {
			await page.goto(`/game-nights/${id}`);
			await settled(page);

			await logButton(page).click();
			const sheet = page.getByRole("dialog", { name: "Log session" });
			await expectDialogFits(sheet);
			await sheet.getByRole("combobox").first().click();
			await page.getByRole("option", { name: new RegExp(`^${suggested}`) }).click();
			await footerOf(sheet).getByRole("button", { name: "Save session" }).scrollIntoViewIfNeeded();
			await footerOf(sheet).getByRole("button", { name: "Save session" }).click();
			await expect(sheet).toBeHidden();

			await expect
				.poll(async () => (await api.get<NightDetail>(`/api/gamenight/${id}`)).sessions.map((x) => x.gameId))
				.toEqual([gameId(suggested)]);
			await expect(page.getByRole("main")).toContainText(games[gameId(suggested) - 1].title);
		} finally {
			await removeNight(api, id);
		}
	});

	test("the start sheet can be finished on a small phone", async ({ page }) => {
		const api = await apiFor(page);
		const id = await createActiveNight(api, "Phone night small");
		try {
			await page.setViewportSize(smallPhone);
			await page.goto(`/game-nights/${id}`);
			await settled(page);

			await startButton(page).click();
			const sheet = page.getByRole("dialog", { name: "Start a game" });
			await expectDialogFits(sheet);
			await footerOf(sheet).getByRole("button", { name: "Cancel" }).scrollIntoViewIfNeeded();
			await expect(footerOf(sheet).getByRole("button", { name: "Cancel" })).toBeInViewport({ ratio: 1 });
		} finally {
			await removeNight(api, id);
		}
	});

	test("a touch screen can read why ending the night is disabled", async ({ browser, page }, info) => {
		const api = await apiFor(page);
		const id = await createActiveNight(api, "Phone night hint");
		const context = await signInOnTouchPhone(browser, info, credentials);
		try {
			const touchPage = await context.newPage();
			await touchPage.goto(`/game-nights/${id}`);
			await settled(touchPage);
			await startButton(touchPage).tap();
			const sheet = touchPage.getByRole("dialog", { name: "Start a game" });
			await sheet.getByRole("combobox").first().tap();
			await touchPage.getByRole("option", { name: new RegExp(`^${suggested}`) }).tap();
			await footerOf(sheet).getByRole("button", { name: "Start" }).tap();
			await expect(sheet).toBeHidden();

			await touchPage.getByText("End night", { exact: true }).tap({ force: true });

			test.fail(true, "BUG: the hint on the disabled End night button is a hover tooltip, a tap shows nothing");
			await expect(touchPage.getByRole("tooltip")).toContainText("Finish or discard the running games first", {
				timeout: 1500,
			});
		} finally {
			await context.close();
			await removeNight(api, id);
		}
	});
});
