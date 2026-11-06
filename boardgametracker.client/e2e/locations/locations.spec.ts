import type { Locator, Page } from "@playwright/test";
import { subDays } from "date-fns";
import { expect, test } from "../support/fixtures";
import { displayDate, duration } from "../support/format";
import { inTitleOrder } from "../support/gamesPage";
import { patchJson } from "../support/loansPage";
import { games, locations, players, sessions } from "../support/seedData";

const LISTED = 5;
const [clubhouse, cafe, library] = locations;
const locationId = (name: string) => locations.indexOf(name) + 1;
const playedAt = (name: string) =>
	sessions.filter((session) => session.location === name).sort((a, b) => b.start.getTime() - a.start.getTime());
const lastPlayed = (name: string) => Math.max(...playedAt(name).map((session) => session.start.getTime()));
const unique = (values: string[]) => [...new Set(values)];
const gameId = (title: string) => games.findIndex((game) => game.title === title) + 1;
const playerId = (name: string) => players.indexOf(name) + 1;
const tally = (names: string[]) => {
	const counts = new Map<string, number>();
	for (const name of names) counts.set(name, (counts.get(name) ?? 0) + 1);
	return [...counts].sort((a, b) => b[1] - a[1]);
};
const plural = (count: number, word: string) => `${count} ${word}${count === 1 ? "" : "s"}`;

const tiles = (page: Page) => page.getByRole("main").getByRole("link");
const tileName = (name: string, sessionCount = playedAt(name).length) =>
	`${name} ${plural(sessionCount, "session")} · last played today`;
const expectTiles = async (page: Page, names: string[]) => {
	await expect(tiles(page)).toHaveCount(names.length);
	for (const [index, name] of names.entries()) {
		await expect(tiles(page).nth(index)).toHaveAccessibleName(tileName(name));
	}
};
const expectRows = async (rows: Locator, names: string[]) => {
	await expect(rows).toHaveCount(names.length);
	for (const [index, name] of names.entries()) {
		await expect(rows.nth(index)).toHaveAccessibleName(name);
	}
};

const openList = async (page: Page) => {
	await page.goto("/locations");
	await expect(page.getByRole("heading", { name: "Locations", level: 1 })).toBeVisible();
};

const openDetail = async (page: Page, name: string) => {
	await page.goto(`/locations/${locationId(name)}`);
	await expect(page.getByRole("heading", { name, level: 1, exact: true })).toBeVisible();
};

const sessionRow = (session: (typeof sessions)[number]) => {
	const result = session.isCoop ? `Co-op · ${session.teamWon ? "won" : "lost"}` : `Winner: ${session.winner}`;
	return `${session.game} ${result} ${displayDate(session.start)} ${duration(session.minutes)}`;
};

test.describe("list", () => {
	test.beforeEach(async ({ page }) => {
		await openList(page);
	});

	test("shows every location with its sessions, recently played first", async ({ page }) => {
		const recent = [...locations].sort((a, b) => lastPlayed(b) - lastPlayed(a));
		const sort = page.getByRole("group", { name: "Sort locations" });

		await expect(sort.getByRole("button", { name: "Recently played" })).toHaveAttribute("aria-pressed", "true");
		await expectTiles(page, recent);
	});

	test("links a tile to the location", async ({ page }) => {
		await expect(tiles(page).filter({ hasText: cafe })).toHaveAttribute("href", `/locations/${locationId(cafe)}`);

		await tiles(page).filter({ hasText: cafe }).click();

		await expect(page).toHaveURL(`/locations/${locationId(cafe)}`);
		await expect(page.getByRole("heading", { name: cafe, level: 1 })).toBeVisible();
	});

	test("counts the locations in the sidebar", async ({ page }) => {
		await expect(
			page.getByRole("navigation").getByRole("link", { name: `Locations ${locations.length}` }),
		).toBeVisible();
	});

	test("sorts by most played and by name", async ({ page }) => {
		const sort = page.getByRole("group", { name: "Sort locations" });
		const mostPlayed = [...locations].sort((a, b) => playedAt(b).length - playedAt(a).length);

		await sort.getByRole("button", { name: "Name" }).click();
		await expectTiles(page, inTitleOrder(locations));

		await sort.getByRole("button", { name: "Most played" }).click();
		await expectTiles(page, mostPlayed);
	});

	test("filters by name and clears the filter", async ({ page }) => {
		const filter = page.getByRole("textbox", { name: "Filter by name" });

		await filter.fill(library.slice(0, 3).toLowerCase());
		await expectTiles(page, [library]);

		await filter.fill("nowhere");

		await expect(tiles(page)).toHaveCount(0);
		await expect(page.getByText("No locations match “nowhere”")).toBeVisible();

		await page.getByRole("main").getByRole("button", { name: "Clear filter" }).click();

		await expect(filter).toHaveValue("");
		await expect(tiles(page)).toHaveCount(locations.length);
	});

	test("forgets a new location when cancelled", async ({ page }) => {
		await page.getByRole("button", { name: "Add location" }).click();
		const dialog = page.getByRole("dialog", { name: "Add location" });
		await expect(dialog.getByRole("textbox", { name: "Name" })).toBeFocused();
		await dialog.getByRole("textbox", { name: "Name" }).fill("Never saved");

		await dialog.getByRole("button", { name: "Cancel" }).click();

		await expect(dialog).toBeHidden();
		await expect(tiles(page)).toHaveCount(locations.length);
	});

	test("refuses a location without a name", async ({ page }) => {
		await page.getByRole("button", { name: "Add location" }).click();
		const dialog = page.getByRole("dialog", { name: "Add location" });

		await dialog.getByRole("button", { name: "Add" }).click();
		await expect(dialog.getByText("Location name is required")).toBeVisible();

		await dialog.getByRole("textbox", { name: "Name" }).fill("   ");
		await dialog.getByRole("button", { name: "Add" }).click();
		await expect(dialog.getByText("Location name is required")).toBeVisible();
		await expect(dialog).toBeVisible();
	});
});

test.describe("other data", () => {
	const patchList = async (page: Page, patch: (all: object[]) => object[]) => {
		await page.route("**/api/location", (route) => patchJson(route, patch));
	};

	test("explains the page without locations", async ({ page, readerPage }) => {
		await patchList(page, () => []);
		await patchList(readerPage, () => []);
		await openList(page);
		await openList(readerPage);

		await expect(page.getByRole("heading", { name: "No locations yet", level: 2 })).toBeVisible();
		await expect(readerPage.getByRole("heading", { name: "No locations yet", level: 2 })).toBeVisible();
		await expect(readerPage.getByRole("button", { name: "Add location" })).toHaveCount(0);
		await expect(page.getByRole("textbox", { name: "Filter by name" })).toHaveCount(0);

		await page.getByRole("button", { name: "Add location" }).click();

		await expect(page.getByRole("dialog", { name: "Add location" })).toBeVisible();
	});

	test("leaves out the last played date of an unused location and says when it was long ago", async ({ page }) => {
		await patchList(page, () => [
			{ id: 1, name: clubhouse, playCount: 0, lastPlayed: null },
			{ id: 2, name: cafe, playCount: 1, lastPlayed: subDays(new Date(), 21).toISOString() },
		]);
		await openList(page);

		await expect(tiles(page).nth(0)).toHaveAccessibleName(`${cafe} 1 session · last played 3 weeks ago`);
		await expect(tiles(page).nth(1)).toHaveAccessibleName(`${clubhouse} No sessions yet`);
	});

	test.describe("when loading fails", () => {
		test.use({ allowedApiStatuses: [401, 400] });

		test("offers to retry the list", async ({ page }) => {
			let failing = true;
			await page.route("**/api/location", async (route) => {
				if (failing) await route.fulfill({ status: 400, json: {} });
				else await route.fallback();
			});
			await page.goto("/locations");
			const main = page.getByRole("main");

			await expect(main.getByText("Couldn't load locations")).toBeVisible();
			await expect(main.getByText("Check your connection and try again.")).toBeVisible();

			failing = false;
			await main.getByRole("button", { name: "Try again" }).click();

			await expect(tiles(page)).toHaveCount(locations.length);
		});

		test("offers to retry a location", async ({ page }) => {
			let failing = true;
			await page.route(`**/api/location/${locationId(cafe)}/statistics`, async (route) => {
				if (failing) await route.fulfill({ status: 400, json: {} });
				else await route.fallback();
			});
			await page.goto(`/locations/${locationId(cafe)}`);
			const main = page.getByRole("main");

			await expect(main.getByRole("heading", { name: "Couldn't load this location", level: 1 })).toBeVisible();

			failing = false;
			await main.getByRole("button", { name: "Try again" }).click();

			await expect(main.getByRole("heading", { name: cafe, level: 1 })).toBeVisible();
		});
	});

	test("says when nobody won at a location", async ({ page }) => {
		await page.route(`**/api/location/${locationId(cafe)}/sessions**`, (route) =>
			patchJson<{ playerSessions: { won: boolean }[] }[]>(route, (all) =>
				all.map((session) => ({
					...session,
					playerSessions: session.playerSessions.map((seat) => ({ ...seat, won: false })),
				})),
			),
		);
		await openDetail(page, cafe);
		const main = page.getByRole("main");

		await expect(main.getByText("No winner")).toHaveCount(LISTED);
		await expect(main.getByText("No data available")).toBeVisible();
		await expect(main.getByRole("link", { name: / wins?$/ })).toHaveCount(0);
	});
});

test.describe("not found", () => {
	test.use({ allowedApiStatuses: [401, 404] });

	test("explains that the location does not exist", async ({ page }) => {
		await page.goto("/locations/9999");
		const main = page.getByRole("main");

		await expect(main.getByRole("heading", { name: "Location not found", level: 1 })).toBeVisible();
		await expect(main.getByText("It may have been deleted, or the link is out of date.")).toBeVisible();

		await main.getByRole("button", { name: "Back to locations" }).click();

		await expect(page).toHaveURL("/locations");
		await expect(tiles(page)).toHaveCount(locations.length);
	});

	for (const id of ["abc", "0", "-1"]) {
		test(`treats the id ${id} as not found`, async ({ page }) => {
			await page.goto(`/locations/${id}`);

			test.fail(true, "BUG: an invalid location id shows the generic error page instead of 'Location not found'");
			await expect(page.getByRole("heading", { name: "Location not found", level: 1 })).toBeVisible({
				timeout: 3_000,
			});
		});
	}
});

test.describe("roles", () => {
	test("lets a reader look but not change anything", async ({ readerPage }) => {
		await openList(readerPage);
		await expect(readerPage.getByRole("button", { name: "Add location" })).toHaveCount(0);
		await expect(tiles(readerPage)).toHaveCount(locations.length);

		await openDetail(readerPage, clubhouse);
		const main = readerPage.getByRole("main");

		await expect(main.getByRole("button", { name: `Edit ${clubhouse}` })).toHaveCount(0);
		await expect(main.getByRole("button", { name: `Delete ${clubhouse}` })).toHaveCount(0);
		await expect(main.getByRole("heading", { name: "Recent sessions" })).toBeVisible();
		await expect(main.locator('a[href^="/sessions/update"]')).toHaveCount(0);
	});

	test("lets a user manage a location", async ({ writerPage }) => {
		await openList(writerPage);
		await expect(writerPage.getByRole("button", { name: "Add location" })).toBeVisible();

		await openDetail(writerPage, clubhouse);

		await expect(writerPage.getByRole("button", { name: `Edit ${clubhouse}` })).toBeVisible();
		await expect(writerPage.getByRole("button", { name: `Delete ${clubhouse}` })).toBeVisible();
		await expect(writerPage.locator('a[href^="/sessions/update"]')).toHaveCount(LISTED);
	});
});

for (const name of locations) {
	test.describe(`detail of ${name}`, () => {
		const played = playedAt(name);
		const gameTally = tally(played.map((session) => session.game));
		const winTally = tally(
			played.flatMap((session) =>
				session.scores
					.map((score) => score.player)
					.filter((player) => player === session.winner || session.teamWon === true),
			),
		);

		test.beforeEach(async ({ page }) => {
			await openDetail(page, name);
		});

		test("shows when it was last played and the statistics", async ({ page }) => {
			const main = page.getByRole("main");
			const minutes = played.reduce((total, session) => total + session.minutes, 0);
			const playerCount = unique(played.flatMap((session) => session.scores.map((score) => score.player))).length;
			const gameCount = unique(played.map((session) => session.game)).length;

			await expect(main.getByText(`Last played today · ${played[0].game}`)).toBeVisible();
			await expect(main).toContainText(
				`Sessions${played.length}Games played${gameCount}Players${playerCount}Play time${duration(minutes)}`,
			);
		});

		test("ranks the most played games", async ({ page }) => {
			const list = page.getByRole("main").getByRole("list").nth(0);
			const rows = list.getByRole("link");
			const top = gameTally.slice(0, LISTED);

			await expectRows(
				rows,
				top.map(([title, count]) => `${title} ${plural(count, "session")}`),
			);
			await expect(rows.first()).toHaveAttribute("href", `/games/${gameId(top[0][0])}`);
			await expect(page.getByText(`+${gameTally.length - LISTED} more games`)).toBeVisible();
		});

		test("ranks the winners", async ({ page }) => {
			const list = page.getByRole("main").getByRole("list").nth(1);
			const rows = list.getByRole("link");
			const top = winTally.slice(0, LISTED);

			await expectRows(
				rows,
				top.map(([player, count]) => `${player} ${plural(count, "win")}`),
			);
			await expect(rows.first()).toHaveAttribute("href", `/players/${playerId(top[0][0])}`);
			await expect(page.getByText(`+${winTally.length - LISTED} more player`)).toBeVisible();
		});

		test("lists the recent sessions and opens all of them", async ({ page }) => {
			const main = page.getByRole("main");
			const list = main.getByRole("list").nth(2);
			const rows = list.getByRole("link");

			await expect(main.getByRole("heading", { name: "Recent sessions" })).toBeVisible();
			await expectRows(rows, played.slice(0, LISTED).map(sessionRow));
			await expect(rows.first()).toHaveAttribute("href", `/sessions/update/${sessions.indexOf(played[0]) + 1}`);

			await main.getByRole("button", { name: `Show all ${played.length} sessions` }).click();

			await expect(main.getByRole("heading", { name: `All sessions (${played.length})` })).toBeVisible();
			await expectRows(rows, played.map(sessionRow));

			await main.getByRole("button", { name: "Show fewer" }).click();

			await expect(rows).toHaveCount(LISTED);
		});
	});
}

test.describe("detail", () => {
	test("opens a session from the list", async ({ page }) => {
		const [newest] = playedAt(clubhouse);
		await openDetail(page, clubhouse);

		await page.getByRole("main").getByRole("list").nth(2).getByRole("link").first().click();

		await expect(page).toHaveURL(`/sessions/update/${sessions.indexOf(newest) + 1}`);
	});

	test("goes back to the list", async ({ page }) => {
		await openList(page);
		await tiles(page).filter({ hasText: library }).click();

		await page.getByRole("button", { name: "Back to locations" }).click();

		await expect(page).toHaveURL("/locations");
	});

	test("goes to the list when opened directly", async ({ page }) => {
		await openDetail(page, library);

		await page.getByRole("button", { name: "Back to locations" }).click();

		await expect(page).toHaveURL("/locations");
	});

	test("keeps the location when editing or deleting is cancelled", async ({ page }) => {
		await openDetail(page, cafe);
		const dialog = page.getByRole("dialog");

		await page.getByRole("button", { name: `Edit ${cafe}` }).click();
		await expect(dialog.getByRole("textbox", { name: "Name" })).toHaveValue(cafe);
		await expect(dialog.getByRole("button", { name: "Save" })).toBeDisabled();
		await dialog.getByRole("textbox", { name: "Name" }).fill("Not saved");
		await dialog.getByRole("button", { name: "Cancel" }).click();
		await expect(dialog).toBeHidden();
		await expect(page.getByRole("heading", { name: cafe, level: 1 })).toBeVisible();

		await page.getByRole("button", { name: `Delete ${cafe}` }).click();
		await expect(dialog).toContainText(
			`Delete location '${cafe}'? Its ${playedAt(cafe).length} sessions are kept, but no longer linked to a location.`,
		);
		await dialog.getByRole("button", { name: "Cancel" }).click();
		await expect(dialog).toBeHidden();
		await expect(page.getByRole("heading", { name: cafe, level: 1 })).toBeVisible();
	});
});
