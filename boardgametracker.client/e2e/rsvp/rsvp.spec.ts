import type { Page } from "@playwright/test";
import { displayDate } from "../support/format";
import { clockTime, daysUntil, fullDate, gameNightByTitle, inviteUrl, paceInviteRequests } from "../support/invites";
import { gameNights, games, linkedPlayer, pastGameNight } from "../support/seedData";
import { apiFor, expect, test } from "../support/session";

test.describe.configure({ timeout: 60_000 });
test.use({ allowedApiStatuses: [401, 404] });

const HALF_A_DAY_MS = 12 * 60 * 60_000;
const [strategyNight, familyAfternoon] = gameNights;
const groups = [
	{ state: "accepted", label: "Coming", summary: "coming" },
	{ state: "maybe", label: "Maybe", summary: "maybe" },
	{ state: "pending", label: "No answer", summary: "no answer" },
	{ state: "declined", label: "Not coming", summary: "not coming" },
];
const invitedGuests = (rsvps: Record<string, string>) => Object.keys(rsvps).filter((name) => name !== linkedPlayer);
const whoAreYou = (page: Page) => page.getByRole("group", { name: "Who are you?" });
const answers = (page: Page) => page.getByRole("group", { name: "Can you make it?" });
const send = (page: Page) => page.getByRole("button", { name: "Send answer" });

const linkIds = new Map<string, string>();
const startDates = new Map<string, string>();

test.beforeAll(async ({ signedInContext }) => {
	const api = await apiFor(signedInContext);
	for (const title of [strategyNight.title, familyAfternoon.title, pastGameNight.title]) {
		const night = await gameNightByTitle(api, title);
		linkIds.set(title, night.linkId);
		startDates.set(title, night.startDate as unknown as string);
	}
});

const openInvite = async (page: Page, title: string) => {
	await paceInviteRequests(page);
	await page.goto(inviteUrl(linkIds.get(title) ?? ""));
	await expect(page.getByRole("main")).toBeVisible();
	await expect(page.getByRole("main")).not.toContainText("Loading data...", { timeout: 20_000 });
};

test("shows a guest what the night is about", async ({ guestPage: page }) => {
	const startDate = startDates.get(strategyNight.title) ?? "";
	expect(Math.abs(new Date(startDate).getTime() - strategyNight.startDate.getTime())).toBeLessThan(HALF_A_DAY_MS);
	await openInvite(page, strategyNight.title);

	const details = page.getByTestId("rsvp-event-details");
	await expect(details.getByRole("heading", { name: strategyNight.title, level: 1 })).toBeVisible();
	await expect(details).toContainText(`You're invited`);
	await expect(details).toContainText(`in ${daysUntil(startDate)} days · ${clockTime(startDate)}`);
	await expect(details.getByTestId("rsvp-date")).toHaveText(fullDate(startDate));
	await expect(details).toContainText(strategyNight.location);
	await expect(details).toContainText(`Hosted by`);
	await expect(details).toContainText(linkedPlayer);
	await expect(page).toHaveTitle(`${strategyNight.title} · BoardGameTracker`);
	await expect(page.getByRole("complementary")).toHaveCount(0);
	await expect(page.getByRole("button", { name: "Log out" })).toHaveCount(0);
});

test("lists the planned games with their player count and length", async ({ guestPage: page }) => {
	await openInvite(page, strategyNight.title);

	const expected = strategyNight.suggestedGames
		.map((title) => games.find((game) => game.title === title))
		.map((game) => {
			if (!game) throw new Error("planned game is not in the seed");
			const players =
				game.minPlayers === game.maxPlayers ? `${game.minPlayers}` : `${game.minPlayers}–${game.maxPlayers}`;
			const time =
				game.minPlayTime === game.maxPlayTime ? `${game.minPlayTime}` : `${game.minPlayTime}–${game.maxPlayTime}`;
			return `${game.title}Players:${players}Duration:${time} min`;
		});

	await expect(page.getByRole("heading", { name: "Planned games" })).toBeVisible();
	await expect(page.getByRole("listitem").filter({ hasText: "Players:" })).toHaveText(expected);
});

test("shows who answered what", async ({ guestPage: page }) => {
	await openInvite(page, strategyNight.title);
	const names = Object.keys(strategyNight.rsvps);
	const inGroup = (state: string) => names.filter((name) => strategyNight.rsvps[name] === state);
	const summary = groups
		.filter((group) => inGroup(group.state).length > 0)
		.map((group) => `${inGroup(group.state).length} ${group.summary}`)
		.join(" · ");

	await expect(page.getByRole("heading", { name: "Who's coming" })).toBeVisible();
	await expect(page.getByText(`${names.length} invited`)).toBeVisible();
	await expect(page.getByRole("img", { name: summary })).toBeVisible();
	for (const group of groups) {
		const region = page.getByRole("region", { name: `${group.label} (${inGroup(group.state).length})` });
		await expect(region.getByRole("listitem")).toHaveText(
			inGroup(group.state).map((name) => new RegExp(name === linkedPlayer ? `${name}\\s*· host$` : `${name}$`)),
		);
	}
});

test("offers everyone who is invited, except the host", async ({ guestPage: page }) => {
	await openInvite(page, strategyNight.title);
	const buttons = whoAreYou(page).getByRole("button");

	await expect(page.getByRole("heading", { name: "Can you make it?" })).toBeVisible();
	await expect(buttons).toHaveCount(invitedGuests(strategyNight.rsvps).length);
	for (const name of invitedGuests(strategyNight.rsvps)) {
		await expect(whoAreYou(page).getByRole("button", { name })).toBeVisible();
	}
	await expect(whoAreYou(page).getByRole("button", { name: linkedPlayer })).toHaveCount(0);
	await expect(answers(page).getByRole("button")).toHaveText(["Coming", "Maybe", "Not coming"]);
});

test("needs both a name and an answer before sending", async ({ guestPage: page }) => {
	const [guest, other] = invitedGuests(strategyNight.rsvps);
	await openInvite(page, strategyNight.title);
	const coming = answers(page).getByRole("button", { name: "Coming", exact: true });
	const maybe = answers(page).getByRole("button", { name: "Maybe" });
	const name = whoAreYou(page).getByRole("button", { name: guest });

	await expect(send(page)).toBeDisabled();

	await name.click();
	await expect(name).toHaveAttribute("aria-pressed", "true");
	await expect(send(page)).toBeDisabled();

	await whoAreYou(page).getByRole("button", { name: other }).click();
	await expect(name).toHaveAttribute("aria-pressed", "false");

	await coming.click();
	await expect(coming).toHaveAttribute("aria-pressed", "true");
	await expect(send(page)).toBeEnabled();

	await maybe.click();
	await expect(coming).toHaveAttribute("aria-pressed", "false");
	await expect(maybe).toHaveAttribute("aria-pressed", "true");
});

test("offers one name when one guest is invited", async ({ guestPage: page }) => {
	await openInvite(page, familyAfternoon.title);

	await expect(page.getByRole("heading", { name: familyAfternoon.title, level: 1 })).toBeVisible();
	await expect(whoAreYou(page).getByRole("button")).toHaveText(
		invitedGuests(familyAfternoon.rsvps).map((name) => new RegExp(`${name}$`)),
	);
});

test("switches to a search box when many guests are invited", async ({ guestPage: page }) => {
	const guests = Array.from({ length: 9 }, (_, index) => `Guest ${index + 1}`);
	await page.route("**/api/gamenight/link/**", async (route) => {
		const response = await route.fetch();
		const night = await response.json();
		const [template] = night.invitedPlayers;
		const invitedPlayers = guests.map((name, index) => ({
			...template,
			id: 1000 + index,
			playerId: 1000 + index,
			player: { ...template.player, id: 1000 + index, name, image: null },
		}));
		await route.fulfill({ response, json: { ...night, invitedPlayers } });
	});
	await openInvite(page, strategyNight.title);

	await page.getByRole("combobox", { name: "Who are you?" }).click();
	await expect(page.getByRole("option")).toHaveCount(guests.length);
	await page.getByRole("option", { name: guests[8] }).click();

	await expect(page.getByRole("combobox", { name: "Who are you?" })).toContainText(guests[8]);
	await page.getByRole("button", { name: "Maybe" }).click();
	await expect(send(page)).toBeEnabled();
});

test("closes the invite of a night that has ended", async ({ guestPage: page }) => {
	await openInvite(page, pastGameNight.title);

	const card = page.getByTestId("rsvp-past");
	await expect(card.getByRole("heading", { name: `${pastGameNight.title} has already taken place` })).toBeVisible();
	await expect(card).toContainText(displayDate(pastGameNight.startDate));
	await expect(card).toContainText("You can't answer any more.");
	await expect(page.getByRole("button", { name: "Send answer" })).toHaveCount(0);
	await expect(page.getByRole("heading", { name: "Who's coming" })).toHaveCount(0);
});

test("refuses an answer to a night that has ended", async ({ guestPage: page, signedInContext }) => {
	const api = await apiFor(signedInContext);
	const night = await gameNightByTitle(api, pastGameNight.title);
	const [rsvp] = night.invitedPlayers;

	const response = await page.request.put(`/api/gamenight/link/${night.linkId}/rsvp`, {
		data: { id: rsvp.id, gameNightId: night.id, playerId: rsvp.playerId, state: "accepted" },
	});

	expect(response.status()).toBe(400);
	expect(await response.text()).toContain("error.game-night.rsvp-closed");
});

test("refuses an answer through the link of another night", async ({ guestPage: page, signedInContext }) => {
	const api = await apiFor(signedInContext);
	const night = await gameNightByTitle(api, strategyNight.title);
	const rsvp = night.invitedPlayers.find((candidate) => candidate.player.name !== linkedPlayer);

	const response = await page.request.put(`/api/gamenight/link/${linkIds.get(familyAfternoon.title)}/rsvp`, {
		data: { id: rsvp?.id, gameNightId: night.id, playerId: rsvp?.playerId, state: "accepted" },
	});

	expect(response.status()).toBe(404);
});

for (const [name, linkId] of [
	["an unknown", "00000000-0000-0000-0000-000000000000"],
	["a malformed", "not-a-link"],
]) {
	test(`explains that ${name} link does not work`, async ({ guestPage: page }) => {
		await paceInviteRequests(page);
		await page.goto(inviteUrl(linkId));

		await expect(page.getByRole("heading", { name: "This invite link doesn't work", level: 1 })).toBeVisible();
		await expect(page.getByText("It may have expired or been copied incorrectly.")).toBeVisible();
		await expect(page.getByRole("button", { name: "Send answer" })).toHaveCount(0);
	});
}

test("explains that a link without an id does not work", async ({ guestPage: page }) => {
	await page.goto("/rsvp");

	test.fail(true, "BUG: /rsvp without linkId shows the generic crash page instead of the invalid invite message");
	await expect(page.getByRole("heading", { name: "This invite link doesn't work", level: 1 })).toBeVisible({
		timeout: 3_000,
	});
});

test.describe("when the server fails", () => {
	test.use({ allowedApiStatuses: [500] });

	test("does not call a link broken when the server fails", async ({ guestPage: page }) => {
		await page.route("**/api/gamenight/link/**", (route) => route.fulfill({ status: 500, json: {} }));
		await page.goto(inviteUrl(linkIds.get(strategyNight.title) ?? ""));

		test.fail(true, "BUG: a 500 or 429 on the invite is reported as an invalid invite link");
		const calledBroken = await page
			.getByRole("heading", { name: "This invite link doesn't work" })
			.waitFor({ timeout: 3_000 })
			.then(
				() => true,
				() => false,
			);
		expect(calledBroken).toBe(false);
	});
});

test("shows a signed in user the same invite, without the menu", async ({ page }) => {
	await openInvite(page, strategyNight.title);

	await expect(page.getByRole("heading", { name: strategyNight.title, level: 1 })).toBeVisible();
	await expect(whoAreYou(page).getByRole("button")).toHaveCount(invitedGuests(strategyNight.rsvps).length);
	await expect(page.getByRole("complementary")).toHaveCount(0);
});

test("lets a reader open the invite", async ({ readerPage: page }) => {
	await openInvite(page, familyAfternoon.title);

	await expect(page.getByRole("heading", { name: familyAfternoon.title, level: 1 })).toBeVisible();
	await expect(page.getByRole("heading", { name: "Authentication Required" })).toHaveCount(0);
	await expect(send(page)).toBeDisabled();
});
