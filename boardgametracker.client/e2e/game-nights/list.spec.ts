import type { Page } from "@playwright/test";
import { format } from "date-fns";
import { expect, test } from "../support/fixtures";
import { displayDate, duration } from "../support/format";
import {
	dateBox,
	displayDateTime,
	displayTime,
	namesWith,
	pastNightId,
	patchResponse,
	relativeDay,
	rsvpSummary,
	rsvpsOf,
	seasonOpener,
} from "../support/gameNights";
import { gameNights, linkedPlayer, pastGameNight } from "../support/seedData";

const [firstNight, secondNight] = gameNights;

const cards = (page: Page) => page.getByTestId("game-night-card");

const openList = async (page: Page) => {
	await page.goto("/game-nights");
	await expect(cards(page)).toHaveCount(gameNights.length);
};

const mockNights = (page: Page, change: (nights: { startDate: string }[]) => unknown[]) =>
	patchResponse(page, "**/api/gamenight", change);

const isPast = (night: { startDate: string }) => new Date(night.startDate) < new Date();

test("shows the title, the add button and the nights view", async ({ page }) => {
	await openList(page);
	const main = page.getByRole("main");

	await expect(main.getByRole("heading", { name: "Game nights", level: 1 })).toBeVisible();
	await expect(main.getByRole("button", { name: "Add game night" })).toBeVisible();
	await expect(main.getByRole("button", { name: "Nights", exact: true })).toHaveAttribute("aria-pressed", "true");
	await expect(main.getByRole("button", { name: "Statistics" })).toHaveAttribute("aria-pressed", "false");
});

test("explains why invitations can't be mailed", async ({ page }) => {
	await openList(page);

	await expect(page.getByTestId("invites-hint")).toHaveText(
		"Email is not set up on the server, so invitations can't be mailed.",
	);
	await expect(page.getByRole("button", { name: "Email invites" })).toHaveCount(0);
});

test("lists the upcoming nights soonest first under their own heading", async ({ page }) => {
	await openList(page);
	const upcoming = page.getByRole("region", { name: "Upcoming" });

	await expect(upcoming.getByRole("heading", { level: 3 })).toHaveText(gameNights.map((night) => night.title));
	await expect(page.getByRole("region", { name: "Playing now" })).toHaveCount(0);
});

for (const [index, night] of gameNights.entries()) {
	test(`shows the card of ${night.title}`, async ({ page }) => {
		await openList(page);
		const card = cards(page).nth(index);
		const rsvps = rsvpsOf(night);

		await expect(card).toContainText(dateBox(night.startDate));
		await expect(card).toContainText(`${relativeDay(night.startDate)} · ${displayTime(night.startDate)}`);
		await expect(card.getByRole("link", { name: night.title })).toHaveAttribute("href", `/game-nights/${index + 1}`);
		await expect(card).toContainText(night.location);
		await expect(card).toContainText(`Host: ${linkedPlayer}`);
		await expect(card).toContainText(`${namesWith(night, "accepted").length} of ${rsvps.length} coming`);
		await expect(card.getByRole("img", { name: rsvpSummary(night), exact: true })).toBeVisible();
		await expect(card.getByTestId("rsvp-avatar-stack").getByRole("listitem")).toHaveText(
			rsvps.map(({ name, label }) => (name === linkedPlayer ? `${name} (host): ${label}` : `${name}: ${label}`)),
		);
		for (const game of night.suggestedGames) {
			await expect(card.getByRole("img", { name: game })).toBeVisible();
		}
		const otherGames = gameNights.flatMap((x) => x.suggestedGames).filter((x) => !night.suggestedGames.includes(x));
		for (const game of otherGames) {
			await expect(card.getByRole("img", { name: game })).toHaveCount(0);
		}
	});
}

test("names who has not answered, might come and is not coming", async ({ page }) => {
	await openList(page);
	const line = (names: string[], prefix: string) => (names.length > 0 ? [`${prefix}${names.join(", ")}`] : []);

	await expect(cards(page).nth(0).getByTestId("rsvp-names").getByRole("listitem")).toHaveText([
		...line(namesWith(firstNight, "pending"), "Waiting on "),
		...line(namesWith(firstNight, "maybe"), "Maybe: "),
		...line(namesWith(firstNight, "declined"), "Not coming: "),
	]);
	await expect(cards(page).nth(1).getByTestId("rsvp-names").getByRole("listitem")).toHaveText([
		...line(namesWith(secondNight, "pending"), "Waiting on "),
	]);
});

test("only the next night has the copy button on its card, the others in the menu", async ({ page }) => {
	await openList(page);
	const [next, later] = [cards(page).nth(0), cards(page).nth(1)];

	await expect(next.getByRole("button", { name: "Copy invite link" })).toBeVisible();
	await expect(later.getByRole("button", { name: "Copy invite link" })).toHaveCount(0);

	await next.getByRole("button", { name: `Actions for ${firstNight.title}` }).click();
	await expect(page.getByRole("menuitem")).toHaveText(["Edit", "Delete"]);
	await page.keyboard.press("Escape");

	await later.getByRole("button", { name: `Actions for ${secondNight.title}` }).click();
	await expect(page.getByRole("menuitem")).toHaveText(["Copy invite link", "Edit", "Delete"]);
});

test("copies the invite link of the right night", async ({ page }) => {
	await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
	await openList(page);
	const link = async (index: number) => {
		await cards(page).nth(index).getByRole("button", { name: "Manage RSVPs" }).click();
		const text = await page.getByRole("dialog").getByRole("code").textContent();
		await page.getByRole("dialog").getByRole("button", { name: "Close" }).last().click();
		return text;
	};
	const [first, second] = [await link(0), await link(1)];
	expect(first).toMatch(new RegExp(`^${new URL(page.url()).origin}/rsvp\\?linkId=[0-9a-f-]{36}$`));
	expect(second).not.toBe(first);

	await cards(page).nth(0).getByRole("button", { name: "Copy invite link" }).click();
	await expect(cards(page).nth(0).getByRole("button", { name: "Copied to clipboard" })).toBeDisabled();
	expect(await page.evaluate(() => navigator.clipboard.readText())).toBe(first);

	await cards(page)
		.nth(1)
		.getByRole("button", { name: `Actions for ${secondNight.title}` })
		.click();
	await page.getByRole("menuitem", { name: "Copy invite link" }).click();
	await expect(page.getByText("Copied to clipboard", { exact: true }).last()).toBeVisible();
	await expect.poll(() => page.evaluate(() => navigator.clipboard.readText())).toBe(second);
});

test("says so when the link can't be copied", async ({ page }) => {
	await page.addInitScript(() => {
		navigator.clipboard.writeText = () => Promise.reject(new Error("denied"));
	});
	await openList(page);

	await cards(page).nth(0).getByRole("button", { name: "Copy invite link" }).click();

	await expect(page.getByText("Couldn't copy the link")).toBeVisible();
});

test("opens a night from its card", async ({ page }) => {
	await openList(page);

	await cards(page).nth(1).getByRole("link", { name: secondNight.title }).click();

	await expect(page).toHaveURL("/game-nights/2");
	await expect(page.getByRole("heading", { name: secondNight.title, level: 1 })).toBeVisible();
});

test("opens Manage RSVPs for the night of the card", async ({ page }) => {
	await openList(page);

	await cards(page).nth(1).getByRole("button", { name: "Manage RSVPs" }).click();

	await expect(page.getByRole("dialog")).toContainText(
		`${secondNight.title} · ${displayDateTime(secondNight.startDate)}`,
	);
	await expect(page.getByRole("dialog").getByTestId("rsvp-summary")).toHaveText(rsvpSummary(secondNight));
});

test("opens the edit form of the night of the card", async ({ page }) => {
	await openList(page);

	await cards(page)
		.nth(1)
		.getByRole("button", { name: `Actions for ${secondNight.title}` })
		.click();
	await page.getByRole("menuitem", { name: "Edit" }).click();

	await expect(page.getByRole("dialog").getByRole("textbox", { name: "Title" })).toHaveValue(secondNight.title);
});

test("names the night in the delete dialog and keeps it on cancel", async ({ page }) => {
	await openList(page);

	await cards(page)
		.nth(0)
		.getByRole("button", { name: `Actions for ${firstNight.title}` })
		.click();
	await page.getByRole("menuitem", { name: "Delete" }).click();
	const dialog = page.getByRole("dialog");

	await expect(dialog).toContainText(`Delete “${firstNight.title}”? This can't be undone. RSVPs are deleted.`);
	await dialog.getByRole("button", { name: "Cancel" }).click();

	await expect(dialog).toBeHidden();
	await expect(cards(page)).toHaveCount(gameNights.length);
});

test.describe("past nights", () => {
	test("groups them by month and summarises the night", async ({ page }) => {
		await openList(page);
		const past = page.getByRole("region", { name: "Past" });
		const { played, attendees, winners } = seasonOpener;
		const link = past.getByRole("link");

		await expect(past.getByRole("heading", { level: 3 })).toHaveText(format(pastGameNight.startDate, "LLLL yyyy"));
		await expect(link).toHaveCount(1);
		await expect(link).toHaveAttribute("href", `/game-nights/${pastNightId}`);
		await expect(link).toContainText(pastGameNight.title);
		await expect(link).toContainText(`${pastGameNight.location} · ${attendees.length} attended`);
		await expect(link).toContainText(`${played.length} games played`);
		await expect(link).toContainText(
			`Played: ${[...played]
				.reverse()
				.map((x) => x.game)
				.join(", ")}`,
		);
		await expect(link).toContainText(`${winners.length > 1 ? "Shared most wins" : "Most wins"}: ${winners.join(", ")}`);
	});

	test.describe("beyond the first ten", () => {
		test.use({ allowedApiStatuses: [401, 404] });

		test("shows the first ten and folds out the older ones", async ({ page }) => {
			const count = 12;
			await mockNights(page, (nights) => {
				const past = nights.find(isPast) ?? {};
				return Array.from({ length: count }, (_, index) => ({
					...past,
					id: 100 + index,
					title: `Old night ${index}`,
					startDate: new Date(Date.now() - (index + 2) * 86_400_000).toISOString(),
				}));
			});
			await page.goto("/game-nights");
			const past = page.getByRole("region", { name: "Past" });

			await expect(past.getByRole("link")).toHaveCount(10);
			await expect(past.getByRole("link").first()).toContainText("Old night 0");

			await past.getByRole("button", { name: "Show 2 older nights" }).click();
			await expect(past.getByRole("link")).toHaveCount(count);

			await past.getByRole("button", { name: "Show fewer nights" }).click();
			await expect(past.getByRole("link")).toHaveCount(10);
		});
	});

	test("says when nothing was logged for a night", async ({ page }) => {
		await mockNights(page, (nights) =>
			nights.map((night) =>
				isPast(night)
					? {
							...night,
							summary: {
								...(night as unknown as { summary: object }).summary,
								gamesPlayed: 0,
								attendanceRecorded: false,
								winnerIds: [],
								playedGameIds: [],
							},
						}
					: night,
			),
		);
		await openList(page);

		await expect(page.getByRole("region", { name: "Past" }).getByRole("link")).toContainText(
			`${pastGameNight.location} · no games logged`,
		);
	});
});

test.describe("empty and error states", () => {
	test("invites to plan the first night when there is none", async ({ page }) => {
		await mockNights(page, () => []);
		await page.goto("/game-nights");
		const main = page.getByRole("main");

		await expect(main).toContainText("No game nights yet");
		await expect(main).toContainText("Plan an evening, invite your group and see who's coming.");

		await main.getByRole("button", { name: "Add game night" }).click();
		await expect(page.getByRole("dialog", { name: "Add game night" })).toBeVisible();
	});

	test("offers a new night when nothing is upcoming", async ({ page }) => {
		await mockNights(page, (nights) => nights.filter(isPast));
		await page.goto("/game-nights");
		const upcoming = page.getByRole("region", { name: "Upcoming" });

		await expect(upcoming).toContainText("No upcoming game nights");
		await expect(upcoming).toContainText("Pick a date and invite the group.");
		await expect(page.getByTestId("invites-hint")).toHaveCount(0);

		await upcoming.getByRole("button", { name: "Add game night" }).click();
		await expect(page.getByRole("dialog", { name: "Add game night" })).toBeVisible();
	});

	test("leaves out the past section when no night is over", async ({ page }) => {
		await mockNights(page, (nights) => nights.filter((night) => !isPast(night)));
		await openList(page);

		await expect(page.getByRole("region", { name: "Past" })).toHaveCount(0);
	});

	test("explains a failed load and retries", async ({ page }) => {
		let offline = true;
		await page.route("**/api/gamenight", async (route) => {
			if (offline) await route.abort("failed");
			else await route.fallback();
		});
		await page.goto("/game-nights");
		const main = page.getByRole("main");

		await expect(main).toContainText("Couldn't load game nights");
		await expect(main).toContainText("The server didn't answer. Check your connection and try again.");

		offline = false;
		await main.getByRole("button", { name: "Try again" }).click();

		await expect(cards(page)).toHaveCount(gameNights.length);
	});

	test("sends you home when game nights are switched off", async ({ page }) => {
		await patchResponse<object>(page, "**/api/settings", (settings) => ({ ...settings, gameNightsEnabled: false }));

		await page.goto("/game-nights");

		await expect(page).toHaveURL("/");
		await expect(
			page
				.getByRole("navigation")
				.getByRole("link", { name: /^Players/ })
				.first(),
		).toBeVisible();
		await expect(page.getByRole("link", { name: /^Game nights/ })).toHaveCount(0);
	});
});

test.describe("email invitations", () => {
	const enableEmail = (page: Page, publicUrl: string) =>
		patchResponse<object>(page, "**/api/settings", (settings) => ({ ...settings, emailEnabled: true, publicUrl }));

	const withEmail = (page: Page, names: string[]) =>
		patchResponse<{ name: string }[]>(page, "**/api/player", (list) =>
			list.map((player) => (names.includes(player.name) ? { ...player, email: "x@example.test" } : player)),
		);

	test("asks to set the public url first", async ({ page }) => {
		await enableEmail(page, "");
		await openList(page);

		await expect(page.getByTestId("invites-hint")).toHaveText(
			"Set the public URL under Settings > General before mailing invitations.",
		);
		await expect(page.getByRole("button", { name: "Email invites" })).toHaveCount(0);
	});

	test("sends a user to an admin", async ({ writerPage: page }) => {
		await openList(page);

		await expect(page.getByTestId("invites-hint")).toHaveText("Ask an admin to set up email invitations.");
	});

	test("lists who gets a mail and who has no address, and can be cancelled", async ({ page }) => {
		const [invitee] = namesWith(firstNight, "pending");
		await enableEmail(page, "https://games.example.test");
		await withEmail(page, [invitee]);
		await openList(page);
		await expect(page.getByTestId("invites-hint")).toHaveCount(0);

		await cards(page).nth(0).getByRole("button", { name: "Email invites" }).click();
		const dialog = page.getByRole("dialog", { name: "Email the invitations?" });

		await expect(dialog).toContainText(
			`1 invitee who hasn't answered gets an email with the RSVP link for ${firstNight.title}:`,
		);
		await expect(dialog.getByTestId("invite-recipients")).toHaveText(invitee);
		await expect(dialog.getByTestId("invite-without-email")).toHaveCount(0);

		await dialog.getByRole("button", { name: "Cancel" }).click();
		await expect(dialog).toBeHidden();
	});

	test("can't send when nobody has an address", async ({ page }) => {
		const [invitee] = namesWith(firstNight, "pending");
		await enableEmail(page, "https://games.example.test");
		await openList(page);

		await cards(page).nth(0).getByRole("button", { name: "Email invites" }).click();
		const dialog = page.getByRole("dialog", { name: "Email the invitations?" });

		await expect(dialog).toContainText(`Nobody who hasn't answered ${firstNight.title} has an email address`);
		await expect(dialog.getByTestId("invite-without-email")).toHaveText(`No email address, not mailed: ${invitee}`);
		await expect(dialog.getByRole("button", { name: /^Send/ })).toBeDisabled();
	});

	test("sends the invites and reports the result", async ({ page }) => {
		const [invitee] = namesWith(firstNight, "pending");
		await enableEmail(page, "https://games.example.test");
		await withEmail(page, [invitee]);
		await page.route("**/api/gamenight/1/send-invites", (route) => route.fulfill({ json: { sent: 1 } }));
		await openList(page);

		await cards(page).nth(0).getByRole("button", { name: "Email invites" }).click();
		await page.getByRole("dialog").getByRole("button", { name: "Send 1 email" }).click();

		await expect(page.getByText("Invites sent: 1")).toBeVisible();
		await expect(page.getByRole("dialog")).toBeHidden();
	});
});

test.describe("as a reader", () => {
	test("shows the nights without anything to change", async ({ readerPage: page }) => {
		await openList(page);
		const main = page.getByRole("main");

		await expect(cards(page).nth(0)).toContainText(firstNight.title);
		await expect(cards(page).nth(0).getByTestId("rsvp-names")).toContainText("Waiting on");
		for (const name of ["Add game night", "Manage RSVPs", "Copy invite link"]) {
			await expect(main.getByRole("button", { name })).toHaveCount(0);
		}
		await expect(main.getByRole("button", { name: /^Actions for/ })).toHaveCount(0);
		await expect(page.getByTestId("invites-hint")).toHaveCount(0);
	});

	test("gets no call to action on the empty pages", async ({ readerPage: page }) => {
		await mockNights(page, (nights) => nights.filter(isPast));
		await page.goto("/game-nights");
		const upcoming = page.getByRole("region", { name: "Upcoming" });

		await expect(upcoming).toContainText("No upcoming game nights");
		await expect(upcoming).not.toContainText("Pick a date and invite the group.");
		await expect(upcoming.getByRole("button")).toHaveCount(0);

		await page.unroute("**/api/gamenight");
		await mockNights(page, () => []);
		await page.goto("/game-nights");

		await expect(page.getByRole("main")).toContainText("No game nights yet");
		await expect(page.getByRole("button", { name: "Add game night" })).toHaveCount(0);
	});
});

test.describe("as a user", () => {
	test("can plan and manage nights", async ({ writerPage: page }) => {
		await openList(page);

		await expect(page.getByRole("button", { name: "Add game night" })).toBeVisible();
		await expect(cards(page).nth(0).getByRole("button", { name: "Manage RSVPs" })).toBeVisible();
	});
});

test.describe("statistics", () => {
	const { played, attendees, winners, minutes, mostWins } = seasonOpener;
	const stats = (page: Page) => page.getByRole("main");

	test.beforeEach(async ({ page }) => {
		await page.goto("/game-nights");
		await page.getByRole("button", { name: "Statistics" }).click();
		await expect(page).toHaveURL(/view=stats/);
		await expect(page.getByRole("region", { name: /^Records/ })).toBeVisible();
	});

	test("sums the nights in the tiles", async ({ page }) => {
		await expect(stats(page)).toContainText(
			`Game nights1Games played${played.length}${played.length} per nightTime in games${duration(minutes)}${duration(minutes)} per nightAverage table${attendees.length}players per night`,
		);
	});

	test("ranks the night winners", async ({ page }) => {
		const card = page.getByRole("region", { name: /^Night winners/ });

		await expect(card.getByRole("progressbar")).toHaveCount(winners.length);
		for (const name of winners) {
			await expect(card.getByRole("progressbar", { name: `${name}: 1 of 1 nights won` })).toBeVisible();
			await expect(card).toContainText(`${name}1 night · ${mostWins} game won`);
		}
		await expect(card).toContainText("1 night ended in a tie and counts for every winner.");
	});

	test("links the history to the night", async ({ page }) => {
		const card = page.getByRole("region", { name: "Night winner history" });
		const link = card.getByRole("link");

		await expect(link).toHaveCount(1);
		await expect(link).toHaveAttribute("href", `/game-nights/${pastNightId}`);
		await expect(link).toContainText(dateBox(pastGameNight.startDate));
		await expect(link).toContainText(pastGameNight.title);
		await expect(link).toContainText(`${played.length} games · ${pastGameNight.location}`);
		for (const name of winners) {
			await expect(link).toContainText(`${name}· ${mostWins} of ${played.length}`);
		}
	});

	test("lists the games that were played and the ones only planned", async ({ page }) => {
		const card = page.getByRole("region", { name: "Most played at game nights" });

		await expect(card.getByRole("progressbar")).toHaveCount(played.length);
		for (const { game } of played) {
			await expect(card).toContainText(`${game}at 1 night1×`);
		}
		await expect(card).toContainText("Planned but never played:");
		await expect(card).toContainText(`${pastGameNight.suggestedGames[0]}· 1×`);
	});

	test("has no streaks after one night", async ({ page }) => {
		await expect(page.getByRole("region", { name: "Streaks" })).toContainText(
			"No streaks yet: a streak starts at two nights in a row.",
		);
	});

	test("counts who came and shows the last nights per player", async ({ page }) => {
		const card = page.getByRole("region", { name: /^Attendance/ });
		const [host, guest] = attendees;
		const dot = card.getByRole("img", {
			name: `${pastGameNight.title} · ${displayDate(pastGameNight.startDate)}: came`,
		});

		await expect(card.getByRole("progressbar")).toHaveCount(attendees.length);
		for (const name of attendees) {
			await expect(card.getByRole("progressbar", { name: `${name} came to 1 of 1 nights` })).toBeVisible();
		}
		await expect(card.getByRole("button", { name: `Show the last nights of ${host}` })).toContainText("host 1×");
		await expect(card.getByRole("button", { name: `Show the last nights of ${host}` })).toHaveAttribute(
			"aria-pressed",
			"true",
		);
		await expect(dot).toBeVisible();

		await card.getByRole("button", { name: `Show the last nights of ${guest}` }).click();

		await expect(card.getByRole("button", { name: `Show the last nights of ${guest}` })).toHaveAttribute(
			"aria-pressed",
			"true",
		);
		await expect(card.getByRole("button", { name: `Show the last nights of ${host}` })).toHaveAttribute(
			"aria-pressed",
			"false",
		);
		await expect(dot).toBeVisible();
	});

	test("shows the records of the season", async ({ page }) => {
		const card = page.getByRole("region", { name: "Records" });
		const ends = played.map((x) => x.start.getTime() + x.minutes * 60_000);
		const starts = played.map((x) => x.start.getTime());
		const span = Math.round((Math.max(...ends) - Math.min(...starts)) / 60_000);
		const last = played[ends.indexOf(Math.max(...ends))];
		const night = `${pastGameNight.title} · ${displayDate(pastGameNight.startDate)}`;

		await expect(card).toContainText(`Longest night${duration(span)}`);
		await expect(card).toContainText(`first game start to last game end · ${night}`);
		await expect(card).toContainText(`Most games in one night${played.length}${night}`);
		await expect(card).toContainText(`Biggest table${attendees.length}${night}`);
		await expect(card).toContainText(`Latest finish${displayTime(new Date(Math.max(...ends)))}`);
		await expect(card).toContainText(`${pastGameNight.title} · ${last.game}, ${duration(last.minutes)}`);
	});

	test("changes the period in the address and keeps the night within it", async ({ page }) => {
		const lastYear = page.waitForResponse((response) => response.url().includes("period=") && response.ok());
		await page.getByRole("button", { name: "Last 12 months" }).click();

		const response = await lastYear;
		await expect(page.getByRole("button", { name: "Last 12 months" })).toHaveAttribute("aria-pressed", "true");
		await expect(page).toHaveURL(/period=/);
		expect(new URL(page.url()).searchParams.get("period")).toBe(new URL(response.url()).searchParams.get("period"));
		await expect(stats(page)).toContainText(`Games played${played.length}${played.length} per night`);
	});

	test("opens straight on the statistics after a reload", async ({ page }) => {
		await page.reload();

		await expect(page.getByRole("button", { name: "Statistics" })).toHaveAttribute("aria-pressed", "true");

		await page.getByRole("button", { name: "Nights", exact: true }).click();

		await expect(cards(page)).toHaveCount(gameNights.length);
	});
});

test.describe("statistics in other states", () => {
	const mockStatistics = (page: Page, patch: Record<string, unknown>) =>
		patchResponse<object>(page, "**/api/gamenight/statistics**", (statistics) => ({ ...statistics, ...patch }));

	test("explains the page when no night counts yet", async ({ page }) => {
		await mockStatistics(page, { nights: 0 });

		await page.goto("/game-nights?view=stats");

		await expect(page.getByRole("main")).toContainText("No game nights to count yet");
		await expect(page.getByRole("main")).toContainText(
			"Statistics count the nights that have sessions logged or were ended.",
		);
		await expect(page.getByRole("region", { name: /^Records/ })).toHaveCount(0);
	});

	test("says how many nights have sessions logged", async ({ page }) => {
		await mockStatistics(page, { nightsWithSessions: 1, totalNights: 3 });

		await page.goto("/game-nights?view=stats");

		await expect(page.getByRole("main")).toContainText("1 of 3 nights has sessions logged");
	});

	test("explains the empty cards", async ({ page }) => {
		await mockStatistics(page, {
			winners: [],
			history: [],
			games: [],
			plannedNeverPlayed: [],
			attendance: [],
			tiedNights: 0,
		});

		await page.goto("/game-nights?view=stats");

		await expect(page.getByRole("region", { name: /^Night winners/ })).toContainText("Nobody has won a night yet.");
		await expect(page.getByRole("region", { name: "Most played at game nights" })).toContainText(
			"No games played at game nights yet.",
		);
		await expect(page.getByRole("region", { name: /^Attendance/ })).toContainText(
			"Nobody was marked at the table yet.",
		);
	});
});

test("ignores an unknown view in the address", async ({ page }) => {
	await page.goto("/game-nights?view=bogus");
	await expect(cards(page)).toHaveCount(gameNights.length, { timeout: 3_000 });
});
