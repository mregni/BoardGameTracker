import type { Page } from "@playwright/test";
import { addDays, addHours, subHours } from "date-fns";
import { writerCredentials } from "../support/fixtures";
import { displayDate } from "../support/format";
import {
	clockTime,
	createNight,
	fullDate,
	gameNightOf,
	inviteUrl,
	type NewNight,
	paceInviteRequests,
	playerIdOf,
	rsvpStateOf,
} from "../support/invites";
import { games, linkedPlayer, locations } from "../support/seedData";
import { type Api, apiFor, changeSettings, expect, signIn, test } from "../support/session";

test.describe.configure({ timeout: 90_000 });
test.use({ allowedApiStatuses: [400, 401, 404] });

const guests = ["Fozzie", "Gonzo"];
const [guest, otherGuest] = guests;
const invited = [linkedPlayer, ...guests];
const soon = () => addDays(new Date(), 2);

let api: Api;
let nightIds: number[] = [];

test.beforeAll(async ({ signedInContext }) => {
	api = await apiFor(signedInContext);
});

test.afterEach(async () => {
	for (const id of nightIds) await api.call("DELETE", `/api/gamenight/${id}`);
	nightIds = [];
});

const newNight = async (changes: Partial<NewNight> = {}) => {
	const night = await createNight(api, {
		title: "Invite test night",
		startDate: soon(),
		invited,
		...changes,
	});
	nightIds.push(night.id);
	return night;
};

const openInvite = async (page: Page, linkId: string) => {
	await paceInviteRequests(page);
	await page.goto(inviteUrl(linkId));
	await expect(page.getByRole("main")).toBeVisible();
};

const answer = async (page: Page, name: string, response: string) => {
	await page.getByRole("group", { name: "Who are you?" }).getByRole("button", { name }).click();
	await page
		.getByRole("group", { name: "Can you make it?" })
		.getByRole("button", { name: response, exact: true })
		.click();
	await page.getByRole("button", { name: "Send answer" }).click();
};

const confirmation = (page: Page) => page.getByTestId("rsvp-confirmation");

test("confirms a guest who is coming and shows it everywhere", async ({ guestPage, page }) => {
	const night = await newNight();
	await openInvite(guestPage, night.linkId);

	await answer(guestPage, guest, "Coming");

	const card = confirmation(guestPage);
	await expect(card.getByRole("heading", { name: `You're coming, ${guest}` })).toBeFocused();
	await expect(guestPage.getByTestId("rsvp-when-where")).toHaveText(
		`${fullDate(night.startDate as unknown as string)} · ${clockTime(night.startDate as unknown as string)} · ${locations[0]}`,
	);
	await expect(card).toContainText("See you there! Your answer is saved.");
	await expect(guestPage.getByRole("region", { name: "Coming (2)" })).toContainText(guest);
	await expect(guestPage.getByRole("region", { name: "No answer (1)" })).toContainText(otherGuest);
	expect(await rsvpStateOf(api, night.id, guest)).toBe("accepted");

	await page.goto(`/game-nights/${night.id}`);
	const rsvps = page.getByRole("region", { name: "RSVPs" });
	await expect(rsvps.getByRole("region", { name: "Coming (2)" }).getByRole("listitem")).toHaveText([
		new RegExp(`${linkedPlayer}\\s*· host`),
		new RegExp(guest),
	]);
	await expect(rsvps.getByRole("region", { name: "No answer (1)" }).getByRole("listitem")).toHaveText([otherGuest]);
});

test("offers a calendar file with the night in it", async ({ guestPage: page }) => {
	const night = await newNight({ notes: "Bring snacks, please" });
	await openInvite(page, night.linkId);

	await answer(page, guest, "Coming");

	const link = page.getByRole("link", { name: "Add to calendar" });
	await expect(link).toHaveAttribute("download", `${night.title}.ics`);
	const href = (await link.getAttribute("href")) ?? "";
	const file = decodeURIComponent(href.replace("data:text/calendar;charset=utf-8,", ""));
	const stamp = new Date(night.startDate)
		.toISOString()
		.replace(/[-:]/g, "")
		.replace(/\.\d{3}/, "");
	const end = addHours(new Date(night.startDate), 3)
		.toISOString()
		.replace(/[-:]/g, "")
		.replace(/\.\d{3}/, "");

	expect(file.split("\r\n")).toEqual(
		expect.arrayContaining([
			`UID:${night.linkId}@boardgametracker`,
			`DTSTART:${stamp}`,
			`DTEND:${end}`,
			`SUMMARY:${night.title}`,
			`LOCATION:${locations[0]}`,
			"DESCRIPTION:Bring snacks\\, please",
			expect.stringMatching(new RegExp(`^URL:.*/rsvp\\?linkId=${night.linkId}$`)),
		]),
	);
});

test("lets a guest change the answer through every option", async ({ guestPage: page }) => {
	const night = await newNight();
	await openInvite(page, night.linkId);
	const card = confirmation(page);
	const change = page.getByRole("button", { name: "Change answer" });
	const options = page.getByRole("group", { name: "Can you make it?" });
	const send = page.getByRole("button", { name: "Send answer" });

	await answer(page, guest, "Not coming");
	await expect(card.getByRole("heading", { name: `You're not coming, ${guest}` })).toBeVisible();
	await expect(card).toContainText("Sorry you can't make it. Your answer is saved.");
	await expect(card.getByRole("link", { name: "Add to calendar" })).toHaveCount(0);
	await expect(page.getByRole("region", { name: "Not coming (1)" })).toContainText(guest);
	expect(await rsvpStateOf(api, night.id, guest)).toBe("declined");

	await change.click();
	await expect(page.getByRole("group", { name: "Who are you?" }).getByRole("button", { name: guest })).toHaveAttribute(
		"aria-pressed",
		"true",
	);
	await expect(options.getByRole("button", { name: "Not coming" })).toHaveAttribute("aria-pressed", "true");

	await options.getByRole("button", { name: "Maybe" }).click();
	await send.click();
	await expect(card.getByRole("heading", { name: `You might come, ${guest}` })).toBeVisible();
	await expect(card).toContainText("Let the host know when you're sure. Your answer is saved.");
	await expect(card.getByRole("link", { name: "Add to calendar" })).toBeVisible();
	await expect(page.getByRole("region", { name: "Maybe (1)" })).toContainText(guest);
	await expect(page.getByRole("region", { name: /^Not coming/ })).toHaveCount(0);
	expect(await rsvpStateOf(api, night.id, guest)).toBe("maybe");

	await change.click();
	await options.getByRole("button", { name: "Coming", exact: true }).click();
	await send.click();
	await expect(card.getByRole("heading", { name: `You're coming, ${guest}` })).toBeVisible();
	expect(await rsvpStateOf(api, night.id, guest)).toBe("accepted");
	expect(await rsvpStateOf(api, night.id, otherGuest)).toBe("pending");
});

test("keeps the form and explains when the night starts while answering", async ({ guestPage: page }) => {
	const night = await newNight();
	await openInvite(page, night.linkId);
	await expect(page.getByRole("heading", { name: "Can you make it?" })).toBeVisible();

	const moved = await api.call("PUT", "/api/gamenight", {
		id: night.id,
		title: night.title,
		notes: "",
		startDate: subHours(new Date(), 1).toISOString(),
		hostId: night.hostId,
		locationId: night.locationId,
		suggestedGameIds: [],
		invitedPlayerIds: night.invitedPlayers.map((rsvp) => rsvp.playerId),
	});
	expect(moved.ok(), await moved.text()).toBe(true);

	await answer(page, guest, "Coming");

	await expect(page.getByText("This game night has already taken place")).toBeVisible();
	await expect(confirmation(page)).toHaveCount(0);
	expect(await rsvpStateOf(api, night.id, guest)).toBe("pending");
});

test("closes answers once the night is under way", async ({ guestPage: page }) => {
	const startDate = subHours(new Date(), 1);
	const night = await newNight({ startDate });
	await openInvite(page, night.linkId);

	const card = page.getByTestId("rsvp-in-progress");
	await expect(card.getByRole("heading", { name: `${night.title} is in progress` })).toBeVisible();
	await expect(card).toContainText(displayDate(startDate));
	await expect(card).toContainText("Answers are closed.");
	await expect(page.getByRole("button", { name: "Send answer" })).toHaveCount(0);

	const rsvp = night.invitedPlayers.find((candidate) => candidate.player.name === guest);
	const refused = await page.request.put(`/api/gamenight/link/${night.linkId}/rsvp`, {
		data: { id: rsvp?.id, gameNightId: night.id, playerId: rsvp?.playerId, state: "accepted" },
	});
	expect(refused.status()).toBe(400);
	expect(await rsvpStateOf(api, night.id, guest)).toBe("pending");
});

test("says a night is over as soon as the host ends it", async ({ guestPage: page }) => {
	const night = await newNight({ startDate: subHours(new Date(), 1) });
	expect((await api.call("POST", `/api/gamenight/${night.id}/end`, {})).ok()).toBe(true);
	await openInvite(page, night.linkId);

	await expect(page.getByRole("heading", { name: `${night.title} has already taken place` })).toBeVisible();
	await expect(page.getByText("You can't answer any more.")).toBeVisible();
});

test("says a night is over when it started too long ago", async ({ guestPage: page }) => {
	const night = await newNight({ startDate: subHours(new Date(), 13) });
	await openInvite(page, night.linkId);

	await expect(page.getByRole("heading", { name: `${night.title} has already taken place` })).toBeVisible();
});

test("shows notes, leaves out planned games and keeps emails private", async ({ guestPage: page }) => {
	const email = "guest.secret@example.test";
	const created = await api.call("POST", "/api/player", { name: "Invite Mail Guest", email });
	expect(created.ok(), await created.text()).toBe(true);
	const { id: playerId } = await created.json();
	const notes = "Doors open at seven\nBring snacks";
	const night = await newNight({ notes, invited: [...invited, "Invite Mail Guest"] });

	try {
		await openInvite(page, night.linkId);
		await expect(page.getByTestId("rsvp-event-details")).toContainText("Doors open at seven");
		await expect(page.getByTestId("rsvp-event-details")).toContainText("Bring snacks");
		await expect(page.getByRole("heading", { name: "Planned games" })).toHaveCount(0);

		const body = await (await page.request.get(`/api/gamenight/link/${night.linkId}`)).text();
		expect(body).toContain("Invite Mail Guest");
		expect(body).not.toContain(email);
	} finally {
		await api.call("DELETE", `/api/gamenight/${night.id}`);
		nightIds = [];
		await api.call("DELETE", `/api/player/${playerId}`);
	}
});

test("refuses to change the answer of the host", async ({ guestPage: page }) => {
	const night = await newNight();
	const rsvp = night.invitedPlayers.find((candidate) => candidate.player.name === linkedPlayer);

	const response = await page.request.put(`/api/gamenight/link/${night.linkId}/rsvp`, {
		data: { id: rsvp?.id, gameNightId: night.id, playerId: rsvp?.playerId, state: "declined" },
	});

	expect(response.status()).toBe(400);
	expect(await response.text()).toContain("error.game-night.host-rsvp-locked");
	expect(await rsvpStateOf(api, night.id, linkedPlayer)).toBe("accepted");
});

test("lets a guest download the manuals of the planned games", async ({ guestPage: page }) => {
	const title = games[2].title;
	const gamesList: { id: number; title: string }[] = await (await api.call("GET", "/api/game")).json();
	const gameId = gamesList.find((game) => game.title === title)?.id ?? 0;
	const uploaded = await page.context().request.post(`/api/manual/game/${gameId}`, {
		headers: api.headers,
		multipart: {
			Files: { name: "invite-rules.pdf", mimeType: "application/pdf", buffer: Buffer.from("%PDF-1.4\n%%EOF\n") },
		},
	});
	expect(uploaded.ok(), await uploaded.text()).toBe(true);
	const [manual] = await uploaded.json();
	const night = await newNight({ suggestedGameIds: [gameId] });

	try {
		await openInvite(page, night.linkId);
		await page.getByText("1 game manual").click();

		const link = page.getByRole("link", { name: /invite-rules/ });
		await expect(link).toHaveAttribute("href", `/api/manual/gamenight/${night.linkId}/manual/${manual.id}/download`);
		const download = await page.request.get((await link.getAttribute("href")) ?? "");
		expect(download.status()).toBe(200);
		expect(download.headers()["content-type"]).toBe("application/pdf");
		expect(await download.text()).toContain("%PDF-1.4");
	} finally {
		await api.call("DELETE", `/api/manual/${manual.id}`);
	}
});

test.describe("when invites need a sign in", () => {
	test.beforeEach(async () => {
		await changeSettings(api, { rsvpAuthenticationEnabled: true });
	});

	test.afterEach(async () => {
		await changeSettings(api, { rsvpAuthenticationEnabled: false });
	});

	test("sends a guest through the login page and back to the invite", async ({ guestPage: page }) => {
		const night = await newNight();
		await openInvite(page, night.linkId);

		await expect(page.getByRole("heading", { name: "Authentication Required" })).toBeVisible();
		await expect(page.getByText("You need to sign in to respond to this invitation.")).toBeVisible();
		await expect(page.getByRole("button", { name: "Send answer" })).toHaveCount(0);

		await page.getByRole("button", { name: "Sign in" }).click();
		await expect(page).toHaveURL(/\/login\?/);
		expect(new URL(page.url()).searchParams.get("redirect")).toBe(inviteUrl(night.linkId));

		await signIn(page, writerCredentials);

		await expect(page).toHaveURL(inviteUrl(night.linkId));
		await expect(page.getByRole("heading", { name: night.title, level: 1 })).toBeVisible({ timeout: 15_000 });
		await answer(page, guest, "Coming");
		await expect(confirmation(page).getByRole("heading", { name: `You're coming, ${guest}` })).toBeVisible({
			timeout: 15_000,
		});
		expect(await rsvpStateOf(api, night.id, guest)).toBe("accepted");
	});

	test("refuses a guest at the api", async ({ guestPage: page }) => {
		const night = await newNight();
		const rsvp = night.invitedPlayers.find((candidate) => candidate.player.name === guest);

		const read = await page.request.get(`/api/gamenight/link/${night.linkId}`);
		const manuals = await page.request.get(`/api/manual/gamenight/${night.linkId}`);
		const write = await page.request.put(`/api/gamenight/link/${night.linkId}/rsvp`, {
			data: { id: rsvp?.id, gameNightId: night.id, playerId: await playerIdOf(api, guest), state: "accepted" },
		});

		expect(read.status()).toBe(401);
		expect(manuals.status()).toBe(401);
		expect(write.status()).toBe(401);
		expect(await rsvpStateOf(api, night.id, guest)).toBe("pending");
		expect((await gameNightOf(api, night.id)).title).toBe(night.title);
	});

	test("shows a signed in user the invite directly", async ({ page }) => {
		const night = await newNight();
		await openInvite(page, night.linkId);

		await expect(page.getByRole("heading", { name: night.title, level: 1 })).toBeVisible();
		await expect(page.getByRole("heading", { name: "Authentication Required" })).toHaveCount(0);
	});
});

test.describe("when game nights are switched off", () => {
	test.use({ allowedApiStatuses: [401, 403, 404] });

	test.afterEach(async () => {
		await changeSettings(api, { gameNightsEnabled: true });
	});

	test("does not show the invite", async ({ guestPage: page }) => {
		const night = await newNight();
		await changeSettings(api, { gameNightsEnabled: false });
		await openInvite(page, night.linkId);

		await expect(page.getByRole("heading", { name: "This invite link doesn't work", level: 1 })).toBeVisible();
	});
});
