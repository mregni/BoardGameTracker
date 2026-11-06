import type { Page } from "@playwright/test";
import { expect, test } from "../support/fixtures";
import { displayDate, duration } from "../support/format";
import {
	dateBox,
	displayDateTime,
	displayTime,
	gameId,
	namesWith,
	openNight,
	pastNightId,
	patchResponse,
	playerId,
	relativeDay,
	rsvpSummary,
	rsvpsOf,
	scoreLine,
	seasonOpener,
} from "../support/gameNights";
import { gameNights, games, linkedPlayer, pastGameNight, players, sessions } from "../support/seedData";

const [night] = gameNights;
const rsvps = rsvpsOf(night);
const uninvited = players.filter((name) => !(name in night.rsvps));

test.describe("a planned night", () => {
	test.beforeEach(async ({ page }) => {
		await openNight(page, night.title, 1);
	});

	test("shows when, where and who hosts in the header", async ({ page }) => {
		const header = page.getByRole("article");

		await expect(header).toContainText(dateBox(night.startDate));
		await expect(header).toContainText(
			`Starts ${relativeDay(night.startDate)} · ${displayDate(night.startDate)} · ${displayTime(night.startDate)}`,
		);
		await expect(header).toContainText(night.location);
		await expect(header).toContainText(`Host: ${linkedPlayer}`);
		await expect(page).toHaveTitle(`${night.title} · BoardGameTracker`);
	});

	test("has no play actions yet", async ({ page }) => {
		const main = page.getByRole("main");

		await expect(main.getByRole("button", { name: "Edit", exact: true })).toBeVisible();
		await expect(main.getByRole("button", { name: "Delete", exact: true })).toBeVisible();
		for (const name of ["Start a game", "Log session", "End night", "Reopen night", "Edit who's here"]) {
			await expect(main.getByRole("button", { name })).toHaveCount(0);
		}
		await expect(main.getByRole("region", { name: "Played tonight" })).toHaveCount(0);
	});

	test("counts the answers and groups the players", async ({ page }) => {
		const panel = page.getByRole("region", { name: "RSVPs" });

		await expect(panel).toContainText(`${rsvps.length} invited`);
		await expect(panel.getByTestId("rsvp-summary")).toHaveText(rsvpSummary(night));
		for (const [state, label] of [
			["accepted", "Coming"],
			["maybe", "Maybe"],
			["pending", "No answer"],
			["declined", "Not coming"],
		] as const) {
			const names = namesWith(night, state);
			const group = panel.getByRole("region", { name: `${label} (${names.length})` });
			await expect(group.getByRole("listitem")).toHaveText(
				names.map((name) => new RegExp(name === linkedPlayer ? `${name}.*· host` : `${name}$`)),
			);
		}
	});

	test("tells how many players still have to answer", async ({ page }) => {
		await expect(page.getByRole("region", { name: "RSVPs" })).toContainText(
			`${namesWith(night, "pending").length} invitee hasn't answered yet. Send them the link.`,
		);
	});

	test("opens a player from the groups", async ({ page }) => {
		const [name] = namesWith(night, "maybe");

		await page.getByRole("region", { name: "RSVPs" }).getByRole("button", { name }).click();

		await expect(page).toHaveURL(`/players/${playerId(name)}`);
	});

	test("lists the planned games with players and duration", async ({ page }) => {
		const panel = page.getByRole("region", { name: "Planned games" });
		const planned = night.suggestedGames.map((title) => games.find((game) => game.title === title));
		const range = (min = 0, max = 0, unit = "") => (min === max ? `${min}${unit}` : `${min}–${max}${unit}`);

		await expect(panel.getByRole("listitem")).toHaveCount(planned.length);
		for (const [index, game] of planned.entries()) {
			const row = panel.getByRole("listitem").nth(index);
			await expect(row).toContainText(game?.title ?? "");
			await expect(row).toContainText(`Players:${range(game?.minPlayers, game?.maxPlayers)}`);
			await expect(row).toContainText(`Duration:${range(game?.minPlayTime, game?.maxPlayTime, " min")}`);
		}
	});

	test("opens a game from the plan", async ({ page }) => {
		const [title] = night.suggestedGames;

		await page.getByRole("region", { name: "Planned games" }).getByRole("listitem").first().getByRole("button").click();

		await expect(page).toHaveURL(`/games/${gameId(title)}`);
	});

	test("copies the invite link", async ({ page }) => {
		await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
		const panel = page.getByRole("region", { name: "RSVPs" });

		await panel.getByRole("button", { name: "Copy invite link" }).click();

		await expect(panel.getByRole("button", { name: "Copied to clipboard" })).toBeDisabled();
		await expect(panel.getByRole("button", { name: "Copy invite link" })).toBeEnabled({ timeout: 4_000 });
		const copied = await page.evaluate(() => navigator.clipboard.readText());
		await panel.getByRole("button", { name: "Manage RSVPs" }).click();
		await expect(page.getByRole("dialog").getByRole("code")).toHaveText(copied);
	});

	test("goes back to the list", async ({ page }) => {
		await page.getByRole("button", { name: "Game nights", exact: true }).first().click();

		await expect(page).toHaveURL("/game-nights");
	});

	test.describe("manage RSVPs", () => {
		test.beforeEach(async ({ page }) => {
			await page.getByRole("button", { name: "Manage RSVPs" }).click();
		});

		test("names the night and sums the answers", async ({ page }) => {
			const dialog = page.getByRole("dialog", { name: "Manage RSVPs" });

			await expect(dialog).toContainText(`${night.title} · ${displayDateTime(night.startDate)}`);
			await expect(dialog).toContainText(`${rsvps.length} invited`);
			await expect(dialog.getByTestId("rsvp-summary")).toHaveText(rsvpSummary(night));
		});

		test("puts the host first, who can't be changed, and the others by name", async ({ page }) => {
			const dialog = page.getByRole("dialog");
			const others = rsvps.filter((rsvp) => rsvp.name !== linkedPlayer);
			const order = others.map((rsvp) => rsvp.name).sort((a, b) => a.localeCompare(b));

			await expect(dialog.getByTestId(/^rsvp-row-/)).toHaveText(
				[linkedPlayer, ...order].map((name) => new RegExp(`^${name}`)),
			);
			await expect(dialog.getByTestId(`rsvp-row-${playerId(linkedPlayer)}`)).toContainText("Host · always coming");
			await expect(dialog.getByTestId(`rsvp-row-${playerId(linkedPlayer)}`).getByRole("button")).toHaveCount(0);
		});

		test("presses the current answer of every player", async ({ page }) => {
			for (const { name, label } of rsvps.filter((rsvp) => rsvp.name !== linkedPlayer)) {
				const group = page.getByRole("group", { name: `RSVP of ${name}` });
				await expect(group.locator('button[aria-pressed="true"]')).toHaveText(label);
			}
		});

		test("offers only the players who aren't invited and filters them", async ({ page }) => {
			const dialog = page.getByRole("dialog");
			const search = dialog.getByRole("textbox", { name: "Invite more players" });
			const invites = dialog.getByRole("button", { name: /^Invite / });

			await expect(invites).toHaveText(uninvited);

			await search.fill(uninvited[0].slice(0, 3).toUpperCase());
			await expect(invites).toHaveText([uninvited[0]]);

			await search.fill("nobody with this name");
			await expect(invites).toHaveCount(0);
			await expect(dialog).toContainText("No players found");
		});

		test("asks for a second click before removing a player", async ({ page }) => {
			const [name] = namesWith(night, "maybe");
			const row = page.getByTestId(`rsvp-row-${playerId(name)}`);

			await row.getByRole("button", { name: `Remove ${name} from this night` }).click();
			await expect(row.getByRole("button", { name: `Remove ${name} from this night` })).toHaveText("Remove?");

			await page.getByRole("dialog").getByRole("heading", { name: "Manage RSVPs" }).click();
			await expect(row.getByRole("button", { name: `Remove ${name} from this night` })).not.toHaveText("Remove?");
		});

		test("shows the link and copies it", async ({ page }) => {
			await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
			const dialog = page.getByRole("dialog");
			const link = await dialog.getByRole("code").textContent();
			expect(link).toContain("/rsvp?linkId=");

			await dialog.getByRole("button", { name: "Copy", exact: true }).click();

			await expect(dialog.getByRole("button", { name: "Copied to clipboard" })).toBeDisabled();
			expect(await page.evaluate(() => navigator.clipboard.readText())).toBe(link);
		});

		test("closes with both buttons and escape", async ({ page }) => {
			const dialog = page.getByRole("dialog");

			await dialog.getByRole("button", { name: "Close" }).last().click();
			await expect(dialog).toBeHidden();

			await page.getByRole("button", { name: "Manage RSVPs" }).click();
			await page.keyboard.press("Escape");
			await expect(dialog).toBeHidden();
		});
	});

	test("fills the edit form with the night", async ({ page }) => {
		await page.getByRole("main").getByRole("button", { name: "Edit", exact: true }).click();
		const dialog = page.getByRole("dialog", { name: "Edit game night" });
		const invited = rsvps.map((rsvp) => rsvp.name).filter((name) => name !== linkedPlayer);

		await expect(dialog.getByRole("textbox", { name: "Title" })).toHaveValue(night.title);
		await expect(dialog.getByRole("combobox", { name: "Host" })).toContainText(linkedPlayer);
		await expect(dialog.getByRole("combobox", { name: "Location" })).toContainText(night.location);
		const [day, month, year] = displayDate(night.startDate).split("-");
		await expect(dialog.getByRole("spinbutton", { name: "day, Date" })).toHaveText(day);
		await expect(dialog.getByRole("spinbutton", { name: "month, Date" })).toHaveText(month);
		await expect(dialog.getByRole("spinbutton", { name: "year, Date" })).toHaveText(year);
		for (const name of [...invited, ...night.suggestedGames]) {
			await expect(dialog.getByRole("button", { name: `Remove ${name}` })).toBeVisible();
		}
		await expect(dialog.getByRole("button", { name: /^Remove / })).toHaveCount(
			invited.length + night.suggestedGames.length,
		);

		await dialog.getByRole("button", { name: "Cancel" }).click();
		await expect(dialog).toBeHidden();
	});

	test("explains what deleting does and keeps the night on cancel", async ({ page }) => {
		await page.getByRole("main").getByRole("button", { name: "Delete", exact: true }).click();
		const dialog = page.getByRole("dialog");

		await expect(dialog).toContainText(`Delete “${night.title}”? This can't be undone. RSVPs are deleted.`);
		await dialog.getByRole("button", { name: "Cancel" }).click();

		await expect(dialog).toBeHidden();
		await expect(page).toHaveURL("/game-nights/1");
	});
});

test("says no games are planned", async ({ page }) => {
	await patchResponse<{ gameNight: object }>(page, "**/api/gamenight/1", (detail) => ({
		...detail,
		gameNight: { ...detail.gameNight, suggestedGames: [] },
	}));

	await openNight(page, night.title, 1);

	await expect(page.getByRole("region", { name: "Planned games" })).toContainText("No games planned yet");
});

test("hides the waiting line when everybody answered", async ({ page }) => {
	await patchResponse<{ gameNight: { invitedPlayers: object[] } }>(page, "**/api/gamenight/1", (detail) => ({
		...detail,
		gameNight: {
			...detail.gameNight,
			invitedPlayers: detail.gameNight.invitedPlayers.map((rsvp) => ({ ...rsvp, state: "accepted" })),
		},
	}));

	await openNight(page, night.title, 1);
	const panel = page.getByRole("region", { name: "RSVPs" });

	await expect(panel.getByTestId("rsvp-summary")).toHaveText(`${rsvps.length} coming`);
	await expect(panel).not.toContainText("hasn't answered yet");
	await expect(panel).not.toContainText("haven't answered yet");
});

test.describe("a planned night as a reader", () => {
	test("shows the plan without anything to change", async ({ readerPage: page }) => {
		await openNight(page, night.title, 1);
		const main = page.getByRole("main");

		await expect(main.getByTestId("rsvp-summary")).toHaveText(rsvpSummary(night));
		for (const name of ["Edit", "Delete", "Copy invite link", "Manage RSVPs"]) {
			await expect(main.getByRole("button", { name, exact: true })).toHaveCount(0);
		}
		await expect(main).not.toContainText("hasn't answered yet");
	});
});

test.describe("a finished night", () => {
	const { played, attendees, winners, mostWins, minutes } = seasonOpener;

	test.beforeEach(async ({ page }) => {
		await openNight(page, pastGameNight.title, pastNightId);
	});

	test("shows the date and no play actions once the night is ended and too old", async ({ page }) => {
		const header = page.getByRole("article");

		await expect(header).toContainText(displayDate(pastGameNight.startDate));
		await expect(header).toContainText(pastGameNight.location);
		await expect(header).toContainText(`Host: ${linkedPlayer}`);
		await expect(page.getByTestId("night-actions")).toHaveCount(0);
		await expect(page.getByRole("button", { name: "Reopen night" })).toHaveCount(0);
		await expect(page.getByRole("button", { name: /^Play .* again$/ })).toHaveCount(0);
		await expect(page.getByTestId("night-plan")).toHaveCount(0);
	});

	test("sums the night", async ({ page }) => {
		const main = page.getByRole("main");

		await expect(main).toContainText(`Games played${played.length}`);
		await expect(main).toContainText(`Time in games${duration(minutes)}`);
		await expect(main).toContainText(`Night winner${winners.join(", ")}`);
		await expect(main).toContainText(`${mostWins} of ${played.length} games won`);
	});

	test("lists the sessions newest first with sorted scores and the winner", async ({ page }) => {
		const rows = page.getByRole("region", { name: "Played", exact: true }).getByTestId("played-session");

		await expect(rows).toHaveCount(played.length);
		for (const [index, session] of played.entries()) {
			const row = rows.nth(index);
			await expect(row).toContainText(`#${played.length - index}`);
			await expect(row).toContainText(session.game);
			await expect(row).toContainText(scoreLine(session));
			await expect(row).toContainText(new RegExp(`${duration(session.minutes)}$`));
			await expect(row.getByRole("img", { name: "won", exact: true })).toHaveCount(1);
			await expect(row.locator("span", { has: page.getByRole("img", { name: "won" }) }).last()).toContainText(
				session.winner ?? "",
			);
			await expect(row.getByRole("link")).toHaveAttribute("href", `/sessions/update/${sessions.indexOf(session) + 1}`);
		}
	});

	test("opens a session to edit it", async ({ page }) => {
		await page.getByTestId("played-session").first().getByRole("link").click();

		await expect(page).toHaveURL(`/sessions/update/${sessions.indexOf(played[0]) + 1}`);
	});

	test("shows who was at the table with the host first", async ({ page }) => {
		const table = page.getByRole("region", { name: "At the table" });

		await expect(table.getByRole("button").filter({ has: page.getByRole("img") })).toHaveText(attendees);
		await expect(table).toContainText(`${linkedPlayer}· host`);
	});

	test("opens a player from the table", async ({ page }) => {
		const name = attendees[1];

		await page.getByRole("region", { name: "At the table" }).getByRole("button", { name }).click();

		await expect(page).toHaveURL(`/players/${playerId(name)}`);
	});

	test("shows the planned game that was not played without a mark", async ({ page }) => {
		const panel = page.getByRole("region", { name: "Planned games" });
		const [title] = pastGameNight.suggestedGames;

		await expect(panel.getByRole("button")).toHaveText(title);
		await expect(panel).not.toContainText("played");

		await panel.getByRole("button", { name: title }).click();

		await expect(page).toHaveURL(`/games/${gameId(title)}`);
	});

	test("locks the players who played when editing who was there", async ({ page }) => {
		await page.getByRole("button", { name: "Edit who's here" }).click();
		const dialog = page.getByRole("dialog", { name: "Who's here?" });
		const buttons = dialog.getByRole("listitem").getByRole("button");
		const sat = attendees.filter((name) => name !== linkedPlayer);

		await expect(buttons).toHaveCount(players.length);
		for (const name of sat) {
			const button = dialog.getByRole("button", { name: new RegExp(`^${name} `) });
			await expect(button).toBeDisabled();
			await expect(button).toContainText("Played tonight");
			await expect(button).toHaveAttribute("aria-pressed", "true");
		}
		const host = dialog.getByRole("button", { name: new RegExp(`^${linkedPlayer} `) });
		await expect(host).toBeEnabled();
		await expect(host).toContainText("Here");
		for (const name of players.filter((x) => !attendees.includes(x))) {
			const button = dialog.getByRole("button", { name: new RegExp(`^${name} `) });
			await expect(button).toContainText("Not here");
			await expect(button).toHaveAttribute("aria-pressed", "false");
		}

		await dialog.getByRole("button", { name: "Cancel" }).click();
		await expect(dialog).toBeHidden();
	});
});

test.describe("a finished night as a reader", () => {
	test("shows the sessions without links or edit buttons", async ({ readerPage: page }) => {
		await openNight(page, pastGameNight.title, pastNightId);
		const main = page.getByRole("main");

		await expect(page.getByTestId("played-session")).toHaveCount(seasonOpener.played.length);
		await expect(page.getByTestId("played-session").getByRole("link")).toHaveCount(0);
		for (const name of ["Edit", "Delete", "Edit who's here"]) {
			await expect(main.getByRole("button", { name, exact: true })).toHaveCount(0);
		}
	});
});

test("shows an error instead of a crash for an address that is no number", async ({ readerPage: page }) => {
	await page.goto("/game-nights/abc");

	await expect(page.getByRole("main").getByRole("button", { name: "Game nights" })).toBeVisible({ timeout: 3_000 });
});

test("ignores an unknown start value in the address", async ({ page }) => {
	await page.goto("/game-nights/1?start=1");

	test.fail(true, "BUG: /game-nights/1?start=1 fails the search schema and lands on the generic error page");
	await expect(page.getByRole("heading", { name: night.title, level: 1 })).toBeVisible({ timeout: 3_000 });
});

test.describe("a night that does not exist", () => {
	test.use({ allowedApiStatuses: [401, 404] });

	const openMissing = async (page: Page) => {
		await page.goto("/game-nights/9999");
		await expect(page.getByRole("main").getByRole("button", { name: "Game nights" })).toBeVisible();
	};

	test("explains it instead of loading forever", async ({ page }) => {
		await openMissing(page);

		await expect(page.getByRole("main").getByText("Loading data...")).toHaveCount(0, { timeout: 5_000 });
	});

	test("still leads back to the list", async ({ page }) => {
		await openMissing(page);

		await page.getByRole("button", { name: "Game nights", exact: true }).first().click();

		await expect(page).toHaveURL("/game-nights");
	});
});
