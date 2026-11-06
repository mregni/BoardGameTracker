import type { Page } from "@playwright/test";
import { addDays, subDays } from "date-fns";
import { adminApi } from "../support/adminApi";
import { expect, test } from "../support/fixtures";
import { displayDate, duration } from "../support/format";
import { sidebarLink, toast } from "../support/loansPage";
import { gameNights, games, locations, players } from "../support/seedData";
import { expectingFailures } from "../support/settings";

const [, cafe] = locations;
const night = gameNights.find((gameNight) => gameNight.location === cafe) ?? gameNights[0];
const cafeId = locations.indexOf(cafe) + 1;
const game = games[0];
const hallNight = "Night in the hall";
const nameBox = (page: Page) => page.getByRole("dialog").getByRole("textbox", { name: "Name" });
const tile = (page: Page, name: string) => page.getByRole("main").getByRole("link").filter({ hasText: name });

const openAddDialog = async (page: Page) => {
	await page.goto("/locations");
	await page.getByRole("button", { name: "Add location" }).click();
};

const deleteOnPage = async (page: Page, name: string) => {
	await page.getByRole("button", { name: `Delete ${name}` }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();
};

const sessionAt = (locationId: number, minutes: number) => ({
	gameId: 1,
	start: subDays(new Date(), 2).toISOString(),
	minutes,
	locationId,
	expansionIds: [],
	isCoop: false,
	teamWon: null,
	playerSessions: [
		{ playerId: 1, score: 10, firstPlay: false, won: true },
		{ playerId: 2, score: 5, firstPlay: false, won: false },
	],
});

test.use({ allowedApiStatuses: [401, 400] });

test.afterEach(async ({ request }) => {
	const api = await adminApi(request);
	const nights: { id: number; title: string }[] = await api.get("/api/gamenight");
	for (const { id } of nights.filter(({ title }) => title === hallNight)) await api.remove(`/api/gamenight/${id}`);
	const all: { id: number }[] = await api.get("/api/location");
	for (const { id } of all.filter((location) => location.id > locations.length)) {
		await api.remove(`/api/location/${id}`);
	}
});

test("adds a location, renames it and deletes it again", async ({ page }) => {
	const name = "Sunroom";
	const renamed = "Winter garden";
	await openAddDialog(page);

	await nameBox(page).fill(`  ${name}  `);
	await page.getByRole("dialog").getByRole("button", { name: "Add" }).click();

	await expect(toast(page, "Location added")).toBeVisible();
	await expect(page.getByRole("dialog")).toBeHidden();
	await expect(tile(page, name)).toHaveAccessibleName(`${name} No sessions yet`);
	await expect(sidebarLink(page, `Locations ${locations.length + 1}`)).toBeVisible();

	await tile(page, name).click();
	const main = page.getByRole("main");
	await expect(main.getByRole("heading", { name, level: 1 })).toBeVisible();
	await expect(main.getByText("No sessions here yet")).toBeVisible();
	await expect(main).toContainText("Sessions0Games played0Players0Play time0 min");
	await expect(main.getByText("Choose this location when you log a session and it shows up here.")).toBeVisible();
	await expect(main.getByRole("heading", { name: "Most played here" })).toHaveCount(0);
	await expect(main.getByRole("heading", { name: "Top winners here" })).toHaveCount(0);

	await page.getByRole("button", { name: `Edit ${name}` }).click();
	await nameBox(page).fill(renamed);
	await page.getByRole("dialog").getByRole("button", { name: "Save" }).click();

	await expect(toast(page, "Location saved")).toBeVisible();
	await expect(main.getByRole("heading", { name: renamed, level: 1 })).toBeVisible();

	await page.getByRole("button", { name: `Delete ${renamed}` }).click();
	await expect(page.getByRole("dialog")).toContainText(`Delete location '${renamed}'?`);
	await expect(page.getByRole("dialog")).not.toContainText("session");
	await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();

	await expect(toast(page, "Location deleted")).toBeVisible();
	await expect(page).toHaveURL("/locations");
	await expect(tile(page, renamed)).toHaveCount(0);
	await expect(sidebarLink(page, `Locations ${locations.length}`)).toBeVisible();
});

test.describe("a name that is already used", () => {
	const tryToAdd = async (page: Page) => {
		await openAddDialog(page);
		await nameBox(page).fill(` ${cafe.toUpperCase()} `);
		await page.getByRole("dialog").getByRole("button", { name: "Add" }).click();
		await expect(toast(page, "A location with this name already exists")).toBeVisible();
	};

	expectingFailures("is refused for a new location", async ({ page }) => {
		await tryToAdd(page);

		await expect(page.getByRole("dialog")).toBeVisible();

		await page.getByRole("dialog").getByRole("button", { name: "Cancel" }).click();
		await expect(page.getByRole("main").getByRole("link")).toHaveCount(locations.length);
	});

	expectingFailures("is refused when renaming", async ({ page }) => {
		await page.goto(`/locations/${cafeId}`);
		await page.getByRole("button", { name: `Edit ${cafe}` }).click();
		const dialog = page.getByRole("dialog");

		await nameBox(page).fill("");
		await expect(dialog.getByText("Location name is required")).toBeVisible();

		await nameBox(page).fill(locations[2]);
		await dialog.getByRole("button", { name: "Save" }).click();

		await expect(toast(page, "A location with this name already exists")).toBeVisible();
		await dialog.getByRole("button", { name: "Cancel" }).click();
		await expect(page.getByRole("heading", { name: cafe, level: 1 })).toBeVisible();
	});

	test("gives no unhandled error in the page", async ({ page }) => {
		await tryToAdd(page);
		test.fail(true, "BUG: a refused name rejects mutateAsync inside form submit: unhandled promise rejection");
	});
});

test("shows a new name at the game nights and sessions of the location", async ({ page, request }) => {
	const renamed = "Corner cafe";
	const api = await adminApi(request);

	try {
		await page.goto(`/locations/${cafeId}`);
		await page.getByRole("button", { name: `Edit ${cafe}` }).click();
		await nameBox(page).fill(renamed);
		await page.getByRole("dialog").getByRole("button", { name: "Save" }).click();
		await expect(page.getByRole("heading", { name: renamed, level: 1 })).toBeVisible();

		await page.locator('a[href^="/sessions/update"]').first().click();
		await expect(page.getByRole("combobox", { name: "Location" })).toHaveText(renamed);

		await page.goto("/game-nights");
		await expect(page.getByRole("main").getByText(renamed)).toHaveCount(1);
		await expect(page.getByRole("main").getByText(cafe, { exact: true })).toHaveCount(0);
		await expect(page.getByRole("main").getByText(night.title)).toBeVisible();
	} finally {
		await api.put("/api/location", { id: cafeId, name: cafe });
	}

	await page.goto(`/locations/${cafeId}`);
	await expect(page.getByRole("heading", { name: cafe, level: 1 })).toBeVisible();
});

test("keeps the sessions of a deleted location", async ({ page, request }) => {
	const name = "Attic";
	const minutes = 75;
	const api = await adminApi(request);
	const location = await api.post("/api/location", { name });
	const session = await api.post("/api/session", sessionAt(location.id, minutes));

	try {
		await page.goto(`/locations/${location.id}`);
		const main = page.getByRole("main");
		await expect(main.getByText(`Last played 2 days ago · ${game.title}`)).toBeVisible();
		await expect(main).toContainText(`Sessions1Games played1Players2Play time${duration(minutes)}`);
		await expect(main.getByRole("link", { name: `${game.title} 1 session` })).toBeVisible();
		await expect(main.getByRole("link", { name: `${players[0]} 1 win` })).toBeVisible();
		await expect(
			main.getByRole("link", {
				name: `${game.title} Winner: ${players[0]} ${displayDate(subDays(new Date(), 2))} ${duration(minutes)}`,
			}),
		).toBeVisible();
		await expect(main.getByRole("button", { name: /^Show all/ })).toHaveCount(0);

		await page.getByRole("button", { name: `Delete ${name}` }).click();
		await expect(page.getByRole("dialog")).toContainText(
			`Delete location '${name}'? Its 1 session is kept, but no longer linked to a location.`,
		);
		await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();

		await expect(toast(page, "Location deleted")).toBeVisible();
		await expect(page).toHaveURL("/locations");

		const kept = await api.get(`/api/session/${session.id}`);
		expect(kept.locationId).toBeNull();
	} finally {
		await api.remove(`/api/session/${session.id}`);
	}
});

test("refuses to delete a location with a game night until the night is gone", async ({ page, request }) => {
	const name = "Booked hall";
	const api = await adminApi(request);
	const location = await api.post("/api/location", { name });
	const gameNight = await api.post("/api/gamenight", {
		title: hallNight,
		notes: "",
		startDate: addDays(new Date(), 30).toISOString(),
		hostId: 1,
		locationId: location.id,
		suggestedGameIds: [],
		invitedPlayerIds: [1],
	});

	await page.goto(`/locations/${location.id}`);
	await deleteOnPage(page, name);

	await expect(toast(page, "This location is used by one or more game nights")).toBeVisible();
	await expect(page).toHaveURL(`/locations/${location.id}`);
	await expect(page.getByRole("heading", { name, level: 1 })).toBeVisible();

	await api.remove(`/api/gamenight/${gameNight.id}`);
	await deleteOnPage(page, name);

	await expect(toast(page, "Location deleted")).toBeVisible();
	await expect(page).toHaveURL("/locations");
	await expect(tile(page, name)).toHaveCount(0);
});
