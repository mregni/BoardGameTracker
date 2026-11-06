import type { Page } from "@playwright/test";
import { expect, test } from "../support/fixtures";
import { inTitleOrder } from "../support/gamesPage";
import {
	dialogAtRest,
	expectDialogFits,
	expectFooterInView,
	expectFooterReachable,
	footerOf,
	gameId,
	phone,
	settled,
	smallPhone,
} from "../support/phone";
import { expansions, gameNights, games, loans, locations, players } from "../support/seedData";

test.use({ allowedApiStatuses: [401, 404] });

const firstGame = gameId(games[0].title);
const clubhouse = locations[0];
const pastNight = gameNights.length + 1;
const loanedGame = [...loans]
	.sort((a, b) => a.loanDate.getTime() - b.loanDate.getTime())
	.find((x) => !x.returned)?.game;

type DialogCase = {
	name: string;
	path: string;
	opener: string | RegExp;
	title: string;
	footer: string[];
	longerThanPhone?: boolean;
};

const cases: DialogCase[] = [
	{
		name: "manuals",
		path: `/games/${firstGame}`,
		opener: /^Manuals/,
		title: "Manuals (0)",
		footer: ["Close", "Upload"],
	},
	{
		name: "expansions",
		path: `/games/${firstGame}`,
		opener: /^Expansions/,
		title: `Expansions (${expansions.filter((x) => x.game === games[0].title).length})`,
		footer: ["Close"],
	},
	{
		name: "delete game",
		path: `/games/${firstGame}`,
		opener: "Delete",
		title: `Delete ${games[0].title}`,
		footer: ["Cancel", "Delete"],
	},
	{
		name: "edit player",
		path: "/players/1",
		opener: "Edit",
		title: "Edit player",
		footer: ["Cancel", "Save"],
	},
	{
		name: "delete player",
		path: "/players/1",
		opener: "Delete",
		title: `Delete ${players[0]}`,
		footer: ["Cancel", "Delete"],
	},
	{
		name: "add location",
		path: "/locations",
		opener: "Add location",
		title: "Add location",
		footer: ["Cancel", "Add"],
	},
	{
		name: "edit location",
		path: "/locations/1",
		opener: `Edit ${clubhouse}`,
		title: "Edit location",
		footer: ["Cancel", "Save"],
	},
	{
		name: "delete location",
		path: "/locations/1",
		opener: `Delete ${clubhouse}`,
		title: "This location is in use",
		footer: ["Close"],
	},
	{ name: "lend a game", path: "/loans", opener: "Lend a game", title: "Lend a game", footer: ["Cancel", "Lend"] },
	{
		name: "return a loan",
		path: "/loans",
		opener: `Mark returned ${loanedGame}`,
		title: `Return ${loanedGame}`,
		footer: ["Cancel", "Mark returned"],
	},
	{
		name: "add game night",
		path: "/game-nights",
		opener: "Add game night",
		title: "Add game night",
		footer: ["Cancel", "Add"],
	},
	{
		name: "edit game night",
		path: "/game-nights/1",
		opener: "Edit",
		title: "Edit game night",
		footer: ["Cancel", "Save"],
		longerThanPhone: true,
	},
	{
		name: "delete game night",
		path: "/game-nights/1",
		opener: "Delete",
		title: "Delete game night",
		footer: ["Cancel", "Delete"],
	},
	{
		name: "manage RSVPs",
		path: "/game-nights/1",
		opener: "Manage RSVPs",
		title: "Manage RSVPs",
		footer: ["Close"],
		longerThanPhone: true,
	},
	{
		name: "attendance",
		path: `/game-nights/${pastNight}`,
		opener: "Edit who's here",
		title: "Who's here?",
		footer: ["Cancel", "Save"],
	},
	{
		name: "change password",
		path: "/settings?tab=account",
		opener: "Change password",
		title: "Change password",
		footer: ["Cancel", "Change password"],
	},
	{ name: "add user", path: "/settings?tab=account", opener: "Add user", title: "Add user", footer: ["Cancel", "Add"] },
	{
		name: "reset data",
		path: "/settings?tab=advanced",
		opener: "Reset data",
		title: "Reset data",
		footer: ["Cancel", "Reset data"],
	},
	{
		name: "factory reset",
		path: "/settings?tab=advanced",
		opener: "Factory reset",
		title: "Factory reset",
		footer: ["Cancel", "Factory reset everything"],
	},
];

const openDialog = async (page: Page, { path, opener, title }: DialogCase) => {
	await page.goto(path);
	await settled(page);
	await page
		.getByRole("main")
		.getByRole("button", { name: opener, exact: typeof opener === "string" })
		.click();
	const dialog = page.getByRole("dialog", { name: title, exact: true });
	await dialogAtRest(dialog);
	return dialog;
};

for (const dialogCase of cases) {
	const { name, title, footer } = dialogCase;

	test(`${name} fits a phone without sideways scrolling`, async ({ page }) => {
		await page.setViewportSize(phone);
		const dialog = await openDialog(page, dialogCase);

		await expect(dialog.getByRole("heading", { name: title })).toBeVisible();
		await expectDialogFits(dialog);
	});

	test(`${name} shows its footer on a phone`, async ({ page }) => {
		await page.setViewportSize(phone);
		const dialog = await openDialog(page, dialogCase);

		await expect(footerOf(dialog).getByRole("button")).toHaveText(footer);
		if (dialogCase.longerThanPhone) await expectFooterReachable(dialog);
		else await expectFooterInView(page, dialog);
	});

	test(`${name} can scroll to every footer button on a small phone`, async ({ page }) => {
		await page.setViewportSize(smallPhone);
		const dialog = await openDialog(page, dialogCase);
		const buttons = footerOf(dialog).getByRole("button");

		for (const label of footer) {
			const button = buttons.filter({ hasText: label });
			await button.scrollIntoViewIfNeeded();
			await expect(button).toBeInViewport({ ratio: 1 });
		}
		await expect(dialog.getByRole("button", { name: "Close" }).first()).toBeVisible();
	});

	test(`${name} closes with the cross and with Escape`, async ({ page }) => {
		await page.setViewportSize(phone);
		const dialog = await openDialog(page, dialogCase);

		await dialog.getByRole("button", { name: "Close" }).first().click();
		await expect(dialog).toBeHidden();
		await page
			.getByRole("main")
			.getByRole("button", { name: dialogCase.opener, exact: typeof dialogCase.opener === "string" })
			.click();
		await expect(dialog).toBeVisible();

		await page.keyboard.press("Escape");
		await expect(dialog).toBeHidden();
		await expect(page).toHaveURL(dialogCase.path);
	});
}

test("reset data needs the typed word before its button works", async ({ page }) => {
	await page.setViewportSize(phone);
	const dialog = await openDialog(page, cases.find((x) => x.name === "reset data") as DialogCase);
	const confirm = footerOf(dialog).getByRole("button", { name: "Reset data" });

	await expect(confirm).toBeDisabled();
	await dialog.getByRole("textbox").fill("RESET");
	await expect(confirm).toBeEnabled();
	await footerOf(dialog).getByRole("button", { name: "Cancel" }).click();
	await expect(dialog).toBeHidden();
});

test("a select inside a dialog opens its options inside the screen", async ({ page }) => {
	await page.setViewportSize(phone);
	const dialog = await openDialog(page, cases.find((x) => x.name === "lend a game") as DialogCase);

	const [firstTitle] = inTitleOrder(games.map((game) => game.title));
	await dialog.getByRole("combobox", { name: "Game", exact: true }).click();
	const option = page.getByRole("option", { name: new RegExp(`^${firstTitle}`) });

	await expect(option).toBeInViewport({ ratio: 1 });
	await option.click();
	await expect(dialog.getByRole("combobox", { name: "Game", exact: true })).toContainText(firstTitle);
	await expect(dialog).toBeVisible();
});

test("a date picker inside a dialog stays inside the screen", async ({ page }) => {
	await page.setViewportSize(phone);
	const dialog = await openDialog(page, cases.find((x) => x.name === "lend a game") as DialogCase);

	await dialog.getByRole("button", { name: "Open calendar Loan start" }).click();
	const grid = page.getByRole("grid");

	await expect(grid).toBeInViewport({ ratio: 1 });
	await page.keyboard.press("Escape");
	await expect(grid).toBeHidden();
	await expect(dialog).toBeVisible();
});

test("the actions of the first user open a dialog that fits", async ({ page }) => {
	await page.setViewportSize(phone);
	await page.goto("/settings?tab=account");
	await settled(page);

	await page
		.getByRole("button", { name: /^Actions for / })
		.first()
		.click();
	await page.getByRole("menuitem", { name: "Edit" }).click();

	const dialog = page.getByRole("dialog");
	await expectDialogFits(dialog);
	await expectFooterInView(page, dialog);
	await footerOf(dialog).getByRole("button", { name: "Cancel" }).click();
	await expect(dialog).toBeHidden();
});

test("every action of the last user can be reached", async ({ page }) => {
	await page.setViewportSize(phone);
	await page.goto("/settings?tab=account");
	await settled(page);

	await page
		.getByRole("button", { name: /^Actions for / })
		.last()
		.click();

	await page.getByRole("menuitem").last().click({ trial: true, timeout: 3000 });
});

test("an error toast leaves the buttons of a sheet free", async ({ page }) => {
	await page.setViewportSize(phone);
	await page.route("**/api/loans/return", (route) => route.abort());
	await page.goto("/loans");
	await settled(page);

	await page.getByRole("button", { name: `Mark returned ${loanedGame}` }).click();
	const dialog = page.getByRole("dialog");
	await dialogAtRest(dialog);
	await footerOf(dialog).getByRole("button", { name: "Mark returned" }).click();

	const toast = page.getByRole("region", { name: /Notifications/ }).getByRole("listitem");
	await expect(toast).toBeVisible();
	await expect(dialog).toBeVisible();
	const toastBox = await toast.boundingBox();
	const footerBox = await footerOf(dialog).boundingBox();
	expect((toastBox?.y ?? 0) + (toastBox?.height ?? 0)).toBeLessThanOrEqual(footerBox?.y ?? 0);
});

test("the delete session dialog names what it deletes once", async ({ page }) => {
	await page.setViewportSize(phone);
	await page.goto(`/games/${firstGame}/sessions`);
	await settled(page);

	await page.getByRole("main").getByRole("button", { name: "Delete" }).first().click();

	await expect(page.getByRole("dialog").getByRole("heading")).toHaveText("Delete session", { timeout: 1000 });
});
