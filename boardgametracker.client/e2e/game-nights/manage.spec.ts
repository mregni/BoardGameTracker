import type { Page } from "@playwright/test";
import { addDays, subMonths } from "date-fns";
import { expect, test } from "../support/fixtures";
import { displayDate } from "../support/format";
import { api, createNight, openNight, playerId, relativeDay, removeCreatedNights } from "../support/gameNights";
import { pick } from "../support/gamesPage";
import { gameNights, games, linkedPlayer, locations, players, wantedGame } from "../support/seedData";

const [firstNight, secondNight] = gameNights;
const [, guest, maybe, declined] = players;
const [, cafe, library] = locations;

test.afterEach(async ({ request }) => {
	await removeCreatedNights(request);
});

const cards = (page: Page) => page.getByTestId("game-night-card");

const card = (page: Page, title: string) => cards(page).filter({ hasText: title });

const pickFromAvatars = async (page: Page, combobox: string, option: string) => {
	await page.getByRole("combobox", { name: combobox, exact: true }).click();
	await page.getByRole("option", { name: new RegExp(`^${option} `) }).click();
};

const setStart = async (page: Page, date: Date, time: string) => {
	const dialog = page.getByRole("dialog");
	await dialog.getByRole("spinbutton", { name: "day, Date" }).click();
	await page.keyboard.type(displayDate(date).replaceAll("-", ""));
	await dialog.getByRole("spinbutton", { name: "hour, Time" }).click();
	await page.keyboard.type(time.replace(":", ""));
};

const openAddDialog = async (page: Page) => {
	await page.goto("/game-nights");
	await expect(cards(page)).toHaveCount(gameNights.length);
	await page.getByRole("button", { name: "Add game night" }).click();
	return page.getByRole("dialog", { name: "Add game night" });
};

test("refuses a night without title, host and location", async ({ page }) => {
	const dialog = await openAddDialog(page);

	await dialog.getByRole("button", { name: "Add", exact: true }).click();

	await expect(dialog).toContainText("Title is required");
	await expect(dialog).toContainText("Host is required");
	await expect(dialog).toContainText("Location is required");
	await expect(dialog).toBeVisible();
});

test("adds nothing when adding is cancelled", async ({ page }) => {
	const dialog = await openAddDialog(page);

	await dialog.getByRole("textbox", { name: "Title" }).fill("Never saved");
	await dialog.getByRole("button", { name: "Cancel" }).click();

	await expect(dialog).toBeHidden();
	await expect(cards(page)).toHaveCount(gameNights.length);

	await page.getByRole("button", { name: "Add game night" }).click();
	await expect(dialog.getByRole("textbox", { name: "Title" })).toHaveValue("");
});

test("offers only owned games to suggest", async ({ page }) => {
	const dialog = await openAddDialog(page);
	const owned = games.filter((game) => game.state === "owned");

	await dialog.getByRole("combobox", { name: "Suggested games" }).click();

	await expect(page.getByRole("option")).toHaveCount(owned.length);
	await expect(page.getByRole("option", { name: wantedGame.title })).toHaveCount(0);
});

test("plans a night and shows it on the list, its page and the dashboard", async ({ page }) => {
	const title = "Hand made night";
	const notes = "Bring snacks";
	const date = addDays(new Date(), 5);
	const [game] = firstNight.suggestedGames;
	const dialog = await openAddDialog(page);

	await dialog.getByRole("textbox", { name: "Title" }).fill(title);
	await setStart(page, date, "20:15");
	await pickFromAvatars(page, "Host", linkedPlayer);
	await pick(page, "Location", library);
	await pickFromAvatars(page, "Invited players", guest);
	await pickFromAvatars(page, "Invited players", maybe);
	await pickFromAvatars(page, "Suggested games", game);
	await dialog.getByRole("textbox", { name: "Notes" }).fill(notes);
	await dialog.getByRole("button", { name: "Add", exact: true }).click();

	await expect(page.getByText("Game night created")).toBeVisible();
	await expect(dialog).toBeHidden();
	await expect(cards(page).getByRole("heading")).toHaveText([firstNight.title, title, secondNight.title]);
	const created = card(page, title);
	await expect(created).toContainText(`${relativeDay(date)} · 20:15`);
	await expect(created).toContainText(library);
	await expect(created).toContainText(`Host: ${linkedPlayer}`);
	await expect(created).toContainText("1 of 3 coming");
	await expect(created).toContainText(`Waiting on ${[guest, maybe].sort().join(", ")}`);
	await expect(created).toContainText(game);
	await expect(created).toContainText(notes);

	await created.getByRole("link", { name: title }).click();
	await expect(page.getByRole("heading", { name: title, level: 1 })).toBeVisible();
	await expect(page.getByRole("article")).toContainText(displayDate(date));
	await expect(page.getByRole("article")).toContainText("20:15");
	await expect(page.getByRole("article")).toContainText(notes);
	await expect(page.getByRole("region", { name: "RSVPs" })).toContainText(
		"2 invitees haven't answered yet. Send them the link.",
	);
	await expect(page.getByRole("region", { name: "Coming (1)" })).toContainText(linkedPlayer);
	await expect(page.getByRole("region", { name: "No answer (2)" })).toContainText(guest);
	await expect(page.getByRole("region", { name: "Planned games" })).toContainText(game);

	await page.goto("/");
	await expect(page.getByRole("region", { name: "Next game night" })).toContainText(firstNight.title);
	await expect(page.getByRole("link", { name: `${gameNights.length} more planned` })).toBeVisible();
});

test("deletes a night from its card", async ({ page, request }) => {
	const title = "Night to delete";
	await createNight(request, { title, startDate: addDays(new Date(), 20) });
	await page.goto("/game-nights");

	await page.getByRole("button", { name: `Actions for ${title}` }).click();
	await page.getByRole("menuitem", { name: "Delete" }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();

	await expect(page.getByText("Game night deleted")).toBeVisible();
	await expect(card(page, title)).toHaveCount(0);
	await expect(cards(page)).toHaveCount(gameNights.length);
});

test.describe("deleting from the page of the night", () => {
	test("returns to the list without asking for the deleted night", async ({ page, request }) => {
		const title = "Night to delete";
		const id = await createNight(request, { title, startDate: addDays(new Date(), 20) });
		await openNight(page, title, id);
		const nightReads: string[] = [];
		page.on("request", (call) => {
			if (call.method() === "GET" && new URL(call.url()).pathname === `/api/gamenight/${id}`)
				nightReads.push(call.url());
		});

		await page.getByRole("main").getByRole("button", { name: "Delete", exact: true }).click();
		await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();

		await expect(page).toHaveURL("/game-nights");
		await expect(page.getByText("Game night deleted")).toBeVisible();
		await expect(cards(page)).toHaveCount(gameNights.length);
		expect(nightReads).toEqual([]);
	});
});

test("edits a night from its card", async ({ page, request }) => {
	const title = "Night to rename";
	const renamed = "Renamed night";
	await createNight(request, { title, startDate: addDays(new Date(), 20) });
	await page.goto("/game-nights");

	await page.getByRole("button", { name: `Actions for ${title}` }).click();
	await page.getByRole("menuitem", { name: "Edit" }).click();
	await page.getByRole("dialog").getByRole("textbox", { name: "Title" }).fill(renamed);
	await page.getByRole("dialog").getByRole("button", { name: "Save" }).click();

	await expect(page.getByText("Game night saved")).toBeVisible();
	await expect(card(page, renamed)).toHaveCount(1);
	await expect(card(page, title)).toHaveCount(0);
});

test("edits every field of a night and keeps the answers", async ({ page, request }) => {
	const title = "Night to edit";
	const renamed = "Edited night";
	const notes = "Second floor";
	const date = addDays(new Date(), 21);
	const [kept, dropped] = firstNight.suggestedGames;
	const id = await createNight(request, {
		title,
		startDate: addDays(new Date(), 20),
		location: cafe,
		suggestedGames: [kept, dropped],
		rsvps: { [guest]: "accepted", [maybe]: "maybe", [declined]: "declined" },
	});
	await openNight(page, title, id);

	await page.getByRole("main").getByRole("button", { name: "Edit", exact: true }).click();
	const dialog = page.getByRole("dialog", { name: "Edit game night" });
	await dialog.getByRole("textbox", { name: "Title" }).fill(renamed);
	await setStart(page, date, "21:45");
	await pick(page, "Location", library);
	await dialog.getByRole("button", { name: `Remove ${dropped}` }).click();
	await dialog.getByRole("button", { name: `Remove ${declined}` }).click();
	await dialog.getByRole("textbox", { name: "Notes" }).fill(notes);
	await dialog.getByRole("button", { name: "Save" }).click();

	await expect(page.getByText("Game night saved")).toBeVisible();
	await expect(dialog).toBeHidden();
	const header = page.getByRole("article");
	await expect(header.getByRole("heading", { name: renamed, level: 1 })).toBeVisible();
	await expect(header).toContainText(`${displayDate(date)} · 21:45`);
	await expect(header).toContainText(library);
	await expect(header).toContainText(notes);
	await expect(page.getByTestId("rsvp-summary")).toHaveText("2 coming · 1 maybe");
	await expect(page.getByRole("region", { name: "Planned games" }).getByRole("listitem")).toHaveCount(1);
	await expect(page.getByRole("region", { name: "Planned games" })).toContainText(kept);

	await page.reload();
	await expect(page.getByRole("heading", { name: renamed, level: 1 })).toBeVisible();
});

test("refuses an edit without a title and keeps the night when cancelled", async ({ page, request }) => {
	const title = "Night to edit";
	const id = await createNight(request, { title, startDate: addDays(new Date(), 20) });
	await openNight(page, title, id);

	await page.getByRole("main").getByRole("button", { name: "Edit", exact: true }).click();
	const dialog = page.getByRole("dialog", { name: "Edit game night" });
	await dialog.getByRole("textbox", { name: "Title" }).fill("");
	await dialog.getByRole("button", { name: "Save" }).click();

	await expect(dialog).toContainText("Title is required");

	await dialog.getByRole("button", { name: "Cancel" }).click();
	await expect(dialog).toBeHidden();
	await expect(page.getByRole("heading", { name: title, level: 1 })).toBeVisible();
});

test("hands the night to another host", async ({ page, request }) => {
	const title = "Night to hand over";
	const id = await createNight(request, {
		title,
		startDate: addDays(new Date(), 20),
		rsvps: { [guest]: "pending" },
	});
	await openNight(page, title, id);

	await page.getByRole("main").getByRole("button", { name: "Edit", exact: true }).click();
	await pickFromAvatars(page, "Host", guest);
	await page.getByRole("dialog").getByRole("button", { name: "Save" }).click();

	await expect(page.getByRole("article")).toContainText(`Host: ${guest}`);
	await expect(page.getByTestId("rsvp-group-accepted").getByRole("listitem").first()).toContainText(`${guest}· host`);
});

test.describe("manage RSVPs", () => {
	const title = "Night with answers";

	const openManage = async (page: Page, request: Parameters<typeof createNight>[0]) => {
		const id = await createNight(request, {
			title,
			startDate: addDays(new Date(), 20),
			rsvps: { [guest]: "pending", [maybe]: "maybe" },
		});
		await openNight(page, title, id);
		await page.getByRole("region", { name: "RSVPs" }).getByRole("button", { name: "Manage RSVPs" }).click();
		return page.getByRole("dialog", { name: "Manage RSVPs" });
	};

	test("changes an answer and updates the summary, the groups and the list", async ({ page, request }) => {
		const dialog = await openManage(page, request);
		const group = dialog.getByRole("group", { name: `RSVP of ${guest}` });

		await group.getByRole("button", { name: "Coming", exact: true }).click();

		await expect(page.getByText("Answer saved")).toBeVisible();
		await expect(group.getByRole("button", { name: "Coming", exact: true })).toHaveAttribute("aria-pressed", "true");
		await expect(group.getByRole("button", { name: "No answer" })).toHaveAttribute("aria-pressed", "false");
		await expect(dialog.getByTestId("rsvp-summary")).toHaveText("2 coming · 1 maybe");

		await dialog.getByRole("button", { name: "Close" }).last().click();
		await expect(page.getByRole("region", { name: "Coming (2)" })).toContainText(guest);
		await expect(page.getByRole("region", { name: "RSVPs" })).not.toContainText("haven't answered");

		await page.goto("/game-nights");
		await expect(card(page, title)).toContainText("2 of 3 coming");
		await expect(card(page, title).getByRole("button", { name: "Manage RSVPs" })).toBeVisible();
	});

	test("removes a player after confirming and offers them again", async ({ page, request }) => {
		const dialog = await openManage(page, request);
		const row = dialog.getByTestId(`rsvp-row-${playerId(guest)}`);

		await row.getByRole("button", { name: `Remove ${guest} from this night` }).click();
		await row.getByRole("button", { name: `Remove ${guest} from this night` }).click();

		await expect(row).toHaveCount(0);
		await expect(dialog.getByTestId("rsvp-summary")).toHaveText("1 coming · 1 maybe");
		await expect(dialog.getByRole("button", { name: `Invite ${guest}` })).toBeVisible();
	});

	test("invites a player who then has no answer yet", async ({ page, request }) => {
		const dialog = await openManage(page, request);
		const search = dialog.getByRole("textbox", { name: "Invite more players" });
		await search.fill(declined);

		await dialog.getByRole("button", { name: `Invite ${declined}` }).click();

		await expect(dialog.getByTestId(`rsvp-row-${playerId(declined)}`)).toContainText(declined);
		await expect(
			dialog.getByRole("group", { name: `RSVP of ${declined}` }).locator('[aria-pressed="true"]'),
		).toHaveText("No answer");
		await expect(dialog.getByTestId("rsvp-summary")).toHaveText("1 coming · 1 maybe · 2 no answer");
		await expect(search).toHaveValue("");
		await expect(dialog.getByRole("button", { name: `Invite ${declined}` })).toHaveCount(0);
	});

	test("hides the invite search once everybody is invited", async ({ page, request }) => {
		const id = await createNight(request, {
			title,
			startDate: addDays(new Date(), 20),
			rsvps: Object.fromEntries(players.slice(1).map((name) => [name, "pending"])),
		});
		await openNight(page, title, id);

		await page.getByRole("region", { name: "RSVPs" }).getByRole("button", { name: "Manage RSVPs" }).click();

		await expect(page.getByRole("dialog").getByRole("textbox", { name: "Invite more players" })).toHaveCount(0);
		await expect(page.getByRole("dialog")).toContainText(`${players.length} invited`);
	});
});

test.describe("a night in the coming twelve hours", () => {
	const title = "Night later today";

	test("is still upcoming but can already be played, without the RSVP panel", async ({ page, request }) => {
		const startDate = new Date(Date.now() + 6 * 60 * 60_000);
		const id = await createNight(request, { title, startDate, rsvps: { [guest]: "accepted" } });

		await page.goto("/game-nights");
		await expect(page.getByRole("region", { name: "Upcoming" }).getByRole("heading", { level: 3 }).first()).toHaveText(
			title,
		);
		await expect(page.getByRole("region", { name: "Playing now" })).toHaveCount(0);

		await openNight(page, title, id);
		const main = page.getByRole("main");
		await expect(main.getByRole("button", { name: "Start a game" })).toBeVisible();
		await expect(main.getByRole("button", { name: "Log session" })).toBeVisible();
		await expect(main.getByRole("button", { name: "End night" })).toBeVisible();
		await expect(page.getByTestId("night-plan")).toHaveCount(0);
		await expect(
			main.getByRole("region", { name: "Expected players" }).getByRole("button", { name: guest }),
		).toBeVisible();
	});
});

test("leaves a night out of the statistics of a period it is not in", async ({ page, request }) => {
	const id = await createNight(request, { title: "Night long ago", startDate: subMonths(new Date(), 13) });
	await api(request, "post", `/api/gamenight/${id}/end`, {});
	await page.goto("/game-nights?view=stats");
	await expect(page.getByRole("main")).toContainText(/Game nights2\D/);

	await page.getByRole("button", { name: "Last 12 months" }).click();

	await expect(page.getByRole("main")).toContainText(/Game nights1\D/);
});
