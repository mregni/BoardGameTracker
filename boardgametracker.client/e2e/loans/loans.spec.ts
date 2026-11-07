import type { Page } from "@playwright/test";
import { addDays, addMonths, addWeeks, differenceInCalendarDays, subDays } from "date-fns";
import { expect, expectVisibleImagesToLoad, test } from "../support/fixtures";
import { displayDate } from "../support/format";
import { gameLink, inTitleOrder, openGames } from "../support/gamesPage";
import {
	type ApiLoan,
	borrower,
	borrowers,
	loanRow,
	openLoans,
	patchLoans,
	pickIn,
	sidebarLink,
	typeDate,
} from "../support/loansPage";
import { games, loans, players, seedTime, sessions } from "../support/seedData";

const [onTimeLoan, overdueLoan, returnedLoan] = loans;
const activeLoans = loans.filter((loan) => !loan.returned);
const overdueCount = activeLoans.filter((loan) => loan.dueDate && loan.dueDate < new Date()).length;
const overdueDays = differenceInCalendarDays(new Date(), overdueLoan.dueDate as Date);
const inDueOrder = [overdueLoan.player, onTimeLoan.player];
const gameId = (title: string) => games.findIndex((game) => game.title === title) + 1;
const playerId = (name: string) => players.indexOf(name) + 1;
const dayFromNow = (days: number) => addDays(new Date(), days).toISOString();
const searchBox = (page: Page) => page.getByRole("textbox", { name: "Search game or borrower" });
const lendButton = (page: Page) => page.getByRole("button", { name: "Lend a game" });

const fakeLoan = (id: number, game: string, player: string, dueDate: string | null): ApiLoan => ({
	id,
	gameId: gameId(game),
	playerId: playerId(player),
	loanDate: subDays(new Date(), 40).toISOString(),
	dueDate,
	returnedDate: null,
	isActive: true,
});

test.describe("overview", () => {
	test.beforeEach(async ({ page }) => {
		await openLoans(page);
	});

	test("counts the active, overdue and soon due loans", async ({ page }) => {
		const main = page.getByRole("main");

		await expect(main.getByRole("button", { name: `On loan ${activeLoans.length}` })).toHaveAttribute(
			"aria-pressed",
			"true",
		);
		await expect(main.getByRole("button", { name: `Overdue ${overdueCount}` })).toHaveAttribute(
			"aria-pressed",
			"false",
		);
		await expect(main.getByRole("button", { name: /Due this week/ })).toHaveCount(0);
		await expect(main).toContainText("Due this week0");
	});

	test("shows the active loans in the sidebar", async ({ page }) => {
		await expect(sidebarLink(page, `Loans ${activeLoans.length}`)).toHaveAttribute("href", "/loans");
	});

	test("groups the loans by borrower with the first due date on top", async ({ page }) => {
		await expect(borrowers(page)).toHaveText(inDueOrder);
		await expect(borrower(page, returnedLoan.player)).toHaveCount(0);
		await expectVisibleImagesToLoad(page);
	});

	test("marks an overdue loan", async ({ page }) => {
		const row = loanRow(page, overdueLoan.game);

		await expect(borrower(page, overdueLoan.player).getByRole("paragraph")).toHaveText("1 game · 1 overdue");
		await expect(row).toContainText(
			`since ${displayDate(overdueLoan.loanDate)} · due ${displayDate(overdueLoan.dueDate as Date)}`,
		);
		await expect(row.getByText(`${overdueDays} days overdue`)).toHaveClass(/text-warn/);
	});

	test("shows the due date of a loan that is on time", async ({ page }) => {
		const row = loanRow(page, onTimeLoan.game);

		await expect(borrower(page, onTimeLoan.player).getByRole("paragraph")).toHaveText("1 game");
		await expect(row).toContainText(`since ${displayDate(onTimeLoan.loanDate)}`);
		await expect(row).toContainText(`Due ${displayDate(onTimeLoan.dueDate as Date)}`);
		await expect(row).not.toContainText("overdue");
	});

	test("links to the game and the borrower", async ({ page }) => {
		const main = page.getByRole("main");
		await expect(main.getByRole("link", { name: overdueLoan.game })).toHaveAttribute(
			"href",
			`/games/${gameId(overdueLoan.game)}`,
		);

		await main.getByRole("link", { name: overdueLoan.player }).click();

		await expect(page).toHaveURL(`/players/${playerId(overdueLoan.player)}`);
		await expect(page.getByRole("heading", { name: overdueLoan.player, level: 1 })).toBeVisible();

		await page.goBack();
		await main.getByRole("link", { name: onTimeLoan.game }).click();

		await expect(page).toHaveURL(`/games/${gameId(onTimeLoan.game)}`);
		await expect(page.getByRole("heading", { name: onTimeLoan.game, level: 1 })).toBeVisible();
	});

	test("hides the returned loan until the history is opened", async ({ page }) => {
		const main = page.getByRole("main");
		const toggle = main.getByRole("button", { name: "Show history" });
		await expect(main.getByText("1 returned loan", { exact: true })).toBeVisible();
		await expect(toggle).toHaveAttribute("aria-expanded", "false");
		await expect(loanRow(page, returnedLoan.game)).toHaveCount(0);

		await toggle.click();

		const row = loanRow(page, returnedLoan.game);
		await expect(row).toContainText(`${returnedLoan.player} · since ${displayDate(returnedLoan.loanDate)}`);
		await expect(row).toContainText(`Returned ${displayDate(seedTime)}`);
		await expect(row.getByRole("link", { name: returnedLoan.player })).toHaveAttribute(
			"href",
			`/players/${playerId(returnedLoan.player)}`,
		);
		await expect(row.getByRole("button", { name: /Mark returned/ })).toHaveCount(0);
		await expect(row.getByRole("button", { name: `Actions for ${returnedLoan.game}` })).toBeVisible();

		await main.getByRole("button", { name: "Hide history" }).click();

		await expect(loanRow(page, returnedLoan.game)).toHaveCount(0);
	});

	test("filters on overdue loans and clears the filter", async ({ page }) => {
		const main = page.getByRole("main");
		const overdue = main.getByRole("button", { name: `Overdue ${overdueCount}` });

		await overdue.click();

		await expect(overdue).toHaveAttribute("aria-pressed", "true");
		await expect(borrowers(page)).toHaveText([overdueLoan.player]);

		await main.getByRole("button", { name: /Showing: Overdue/ }).click();

		await expect(overdue).toHaveAttribute("aria-pressed", "false");
		await expect(borrowers(page)).toHaveText(inDueOrder);
		await expect(main.getByRole("button", { name: /Showing/ })).toHaveCount(0);

		await overdue.click();
		await overdue.click();

		await expect(borrowers(page)).toHaveText(inDueOrder);

		await overdue.click();
		await main.getByRole("button", { name: `On loan ${activeLoans.length}` }).click();

		await expect(borrowers(page)).toHaveText(inDueOrder);
	});

	test("searches on game and on borrower", async ({ page }) => {
		await searchBox(page).fill(onTimeLoan.game.toUpperCase());
		await expect(borrowers(page)).toHaveText([onTimeLoan.player]);

		await searchBox(page).fill(` ${overdueLoan.player.toLowerCase()} `);
		await expect(borrowers(page)).toHaveText([overdueLoan.player]);
		await expect(page.getByText("0 of 1 returned loan")).toBeVisible();
	});

	test("says when the search finds nothing and clears it", async ({ page }) => {
		const main = page.getByRole("main");

		await searchBox(page).fill("no such game");

		await expect(main.getByText("No active loans match “no such game”.")).toBeVisible();
		await expect(borrowers(page)).toHaveCount(0);

		await main.getByRole("button", { name: "Show all loans" }).click();

		await expect(searchBox(page)).toHaveValue("");
		await expect(borrowers(page)).toHaveText(inDueOrder);
	});

	test("opens the history when only a returned loan matches", async ({ page }) => {
		await searchBox(page).fill(returnedLoan.game);

		await expect(page.getByText("1 of 1 returned loan")).toBeVisible();
		await expect(page.getByRole("main").getByRole("button", { name: "Hide history" })).toBeVisible();
		await expect(loanRow(page, returnedLoan.game)).toContainText(returnedLoan.player);
	});

	test("says when the search and the filter exclude each other", async ({ page }) => {
		const main = page.getByRole("main");
		const overdue = main.getByRole("button", { name: `Overdue ${overdueCount}` });
		await overdue.click();

		await searchBox(page).fill(onTimeLoan.game);

		await expect(main.getByText("No loans match this filter.")).toBeVisible();

		await main.getByRole("button", { name: "Show all loans" }).click();

		await expect(borrowers(page)).toHaveText(inDueOrder);
		await expect(overdue).toHaveAttribute("aria-pressed", "false");
	});

	test("offers edit and delete in the row menu", async ({ page }) => {
		const trigger = page.getByRole("button", { name: `Actions for ${overdueLoan.game}` });

		await trigger.click();

		await expect(page.getByRole("menu").getByRole("menuitem")).toHaveText(["Edit", "Delete"]);

		await page.keyboard.press("Escape");

		await expect(page.getByRole("menu")).toHaveCount(0);
		await expect(trigger).toBeFocused();
	});

	test("keeps the loan when the dialogs are cancelled", async ({ page }) => {
		const dialog = page.getByRole("dialog");

		await page.getByRole("button", { name: `Mark returned ${overdueLoan.game}` }).click();
		await expect(dialog.getByRole("heading", { name: `Return ${overdueLoan.game}` })).toBeVisible();
		await expect(dialog).toContainText("When was the game returned?");
		await dialog.getByRole("button", { name: "Cancel" }).click();
		await expect(dialog).toBeHidden();
		await expect(borrowers(page)).toHaveText(inDueOrder);

		await page.getByRole("button", { name: `Actions for ${overdueLoan.game}` }).click();
		await page.getByRole("menuitem", { name: "Delete" }).click();
		await expect(dialog.getByRole("heading", { name: `Delete loan of ${overdueLoan.game}` })).toBeVisible();
		await dialog.getByRole("button", { name: "Cancel" }).click();
		await expect(dialog).toBeHidden();
		await expect(borrowers(page)).toHaveText(inDueOrder);
	});
});

test.describe("roles", () => {
	test("lets a reader look but not change anything", async ({ readerPage }) => {
		await openLoans(readerPage);

		await expect(borrowers(readerPage)).toHaveText(inDueOrder);
		await expect(lendButton(readerPage)).toHaveCount(0);
		await expect(readerPage.getByRole("button", { name: /Mark returned/ })).toHaveCount(0);
		await expect(readerPage.getByRole("button", { name: /Actions for/ })).toHaveCount(0);
	});

	test("lets a user lend and return", async ({ writerPage }) => {
		await openLoans(writerPage);

		await expect(lendButton(writerPage)).toBeVisible();
		await expect(writerPage.getByRole("button", { name: /Mark returned/ })).toHaveCount(activeLoans.length);
		await expect(writerPage.getByRole("button", { name: /Actions for/ })).toHaveCount(activeLoans.length);
	});
});

test("shows the filters as chips on a phone", async ({ page }) => {
	await page.setViewportSize({ width: 390, height: 800 });
	await openLoans(page);
	const main = page.getByRole("main");

	await expect(main.getByRole("button", { name: `${activeLoans.length} on loan` })).toBeVisible();
	await expect(main.getByRole("button", { name: "0 this week" })).toBeDisabled();

	await main.getByRole("button", { name: `${overdueCount} overdue` }).click();

	await expect(borrowers(page)).toHaveText([overdueLoan.player]);
});

test.describe("other data", () => {
	test("explains the page without any loan", async ({ page, readerPage }) => {
		await patchLoans(page, () => []);
		await patchLoans(readerPage, () => []);
		await openLoans(page);
		await openLoans(readerPage);

		await expect(page.getByRole("heading", { name: "No loans yet", level: 2 })).toBeVisible();
		await expect(page.getByText("Lend a game to a friend and keep track of when it should come back.")).toBeVisible();
		await expect(searchBox(page)).toHaveCount(0);
		await expect(readerPage.getByRole("heading", { name: "No loans yet", level: 2 })).toBeVisible();
		await expect(lendButton(readerPage)).toHaveCount(0);

		await lendButton(page).click();

		await expect(page.getByRole("dialog", { name: "Lend a game" })).toBeVisible();
	});

	test("says so when every loan came back", async ({ page }) => {
		await patchLoans(page, (all) => all.filter((loan) => loan.returnedDate));
		await openLoans(page);
		const main = page.getByRole("main");

		await expect(main.getByText("There are no active loans at the moment.")).toBeVisible();
		await expect(main).toContainText("On loan0");
		await expect(main.getByText("1 returned loan", { exact: true })).toBeVisible();
	});

	test("groups the loans that are due this week", async ({ page }) => {
		await patchLoans(page, () => [
			fakeLoan(101, "Tend", "Rowlf", dayFromNow(0)),
			fakeLoan(102, "Iliad", "Rowlf", dayFromNow(3)),
			fakeLoan(103, "Lairs", "Beaker", dayFromNow(7)),
			fakeLoan(104, "Earth", "Scooter", dayFromNow(8)),
		]);
		await openLoans(page);
		const main = page.getByRole("main");

		await expect(loanRow(page, "Tend")).toContainText("Due today");
		await expect(loanRow(page, "Iliad")).toContainText("Due in 3 days");
		await expect(loanRow(page, "Lairs")).toContainText("Due in 7 days");
		await expect(loanRow(page, "Earth")).toContainText(`Due ${displayDate(addDays(new Date(), 8))}`);
		await expect(borrower(page, "Rowlf").getByRole("paragraph")).toHaveText("2 games");

		await main.getByRole("button", { name: "Due this week 3" }).click();

		await expect(borrowers(page)).toHaveText(["Rowlf", "Beaker"]);
		await expect(main.getByRole("button", { name: /Showing: Due this week/ })).toBeVisible();

		await searchBox(page).fill("Scooter");

		await expect(main.getByText("No loans match this filter.")).toBeVisible();
	});

	test("turns red after thirty days overdue", async ({ page }) => {
		await patchLoans(page, () => [
			fakeLoan(101, "Tend", "Rowlf", dayFromNow(-30)),
			fakeLoan(102, "Iliad", "Beaker", dayFromNow(-31)),
			fakeLoan(103, "Lairs", "Scooter", dayFromNow(-1)),
		]);
		await openLoans(page);

		await expect(borrowers(page)).toHaveText(["Beaker", "Rowlf", "Scooter"]);
		await expect(loanRow(page, "Tend").getByText("30 days overdue")).toHaveClass(/text-warn/);
		await expect(loanRow(page, "Iliad").getByText("31 days overdue")).toHaveClass(/text-bad/);
		await expect(loanRow(page, "Lairs").getByText("1 day overdue")).toBeVisible();
	});

	test("puts a loan without due date last", async ({ page }) => {
		await patchLoans(page, () => [
			fakeLoan(101, "Tend", "Rowlf", null),
			fakeLoan(102, "Iliad", "Beaker", dayFromNow(20)),
		]);
		await openLoans(page);

		await expect(borrowers(page)).toHaveText(["Beaker", "Rowlf"]);
		await expect(loanRow(page, "Tend").getByText("No due date")).toBeVisible();
	});

	test("offers to return all games of one borrower", async ({ page }) => {
		await patchLoans(page, () => [
			fakeLoan(101, "Tend", "Rowlf", dayFromNow(-2)),
			fakeLoan(102, "Iliad", "Rowlf", dayFromNow(-3)),
		]);
		await openLoans(page);
		const group = borrower(page, "Rowlf");

		await expect(group.getByRole("paragraph")).toHaveText("2 games · 2 overdue");

		await group.getByRole("button", { name: "Mark all returned" }).click();

		const dialog = page.getByRole("dialog");
		await expect(dialog.getByRole("heading", { name: "Return 2 games from Rowlf" })).toBeVisible();
		await expect(dialog).toContainText("When were the games returned?");
		await expect(dialog.getByRole("list", { name: "Games" }).getByRole("listitem")).toHaveText(["Iliad", "Tend"]);
	});

	test("names a game or borrower it cannot find", async ({ page }) => {
		await patchLoans(page, () => [
			{ ...fakeLoan(101, "Tend", "Rowlf", null), gameId: 9999 },
			{ ...fakeLoan(102, "Iliad", "Rowlf", null), playerId: 9999 },
		]);
		await openLoans(page);
		const main = page.getByRole("main");

		await expect(main.getByText("Unknown game")).toBeVisible();
		await expect(main.getByRole("heading", { name: "Unknown borrower", level: 2 })).toBeVisible();
		await expect(main.getByRole("link", { name: "Unknown" })).toHaveCount(0);
		await expect(main.getByRole("button", { name: "Actions for Unknown game" })).toBeVisible();
	});

	test.describe("when the loans fail to load", () => {
		test.use({ allowedApiStatuses: [401, 400] });

		test("does not pretend there are no loans", async ({ page }) => {
			await page.route("**/api/loans", (route) => route.fulfill({ status: 400, json: {} }));
			await page.goto("/loans");

			await expect(page.getByRole("main").getByRole("button", { name: "Try again" })).toBeVisible({ timeout: 3_000 });
		});
	});
});

test.describe("lend dialog", () => {
	const owned = games.filter((game) => game.state === "owned").map((game) => game.title);
	const lent = inTitleOrder(owned.filter((title) => activeLoans.some((loan) => loan.game === title)));
	const free = inTitleOrder(owned.filter((title) => !lent.includes(title)));

	test.beforeEach(async ({ page }) => {
		await openLoans(page);
		await lendButton(page).click();
		await expect(page.getByRole("dialog", { name: "Lend a game" })).toBeVisible();
	});

	test("starts today, due in two weeks", async ({ page }) => {
		const dialog = page.getByRole("dialog");

		await expect(dialog.getByRole("combobox", { name: "Game", exact: true })).toHaveText("Choose game");
		await expect(dialog.getByRole("combobox", { name: "Borrower", exact: true })).toHaveText("Choose borrower");
		await expect(dialog.getByRole("group", { name: "Loan start" }).getByRole("spinbutton")).toHaveText(
			displayDate(new Date()).split("-"),
		);
		await expect(dialog.getByRole("radiogroup", { name: "Due date" }).getByRole("radio")).toHaveText([
			"1 week",
			`2 weeks · ${displayDate(addWeeks(new Date(), 2))}`,
			"1 month",
			"No due date",
			"Pick date",
		]);
		await expect(dialog.getByRole("radio", { name: /2 weeks/ })).toBeChecked();
	});

	test("lists the owned games with the lent ones disabled at the end", async ({ page }) => {
		await page.getByRole("dialog").getByRole("combobox", { name: "Game", exact: true }).click();
		const options = page.getByRole("option");

		await expect(options).toHaveCount(owned.length);
		await expect(options).toContainText([...free, ...lent]);
		for (const title of lent) {
			const loan = activeLoans.find((x) => x.game === title);
			const option = options.filter({ hasText: title });
			await expect(option).toHaveAttribute("aria-disabled", "true");
			await expect(option).toContainText(`With ${loan?.player} since ${displayDate(loan?.loanDate as Date)}`);
		}
		await expect(options.filter({ hasText: returnedLoan.game })).not.toHaveAttribute("aria-disabled", "true");
	});

	test("lists every player as borrower", async ({ page }) => {
		await page.getByRole("dialog").getByRole("combobox", { name: "Borrower", exact: true }).click();

		await expect(page.getByRole("option")).toHaveCount(players.length);
		await expect(page.getByRole("option")).toContainText(inTitleOrder(players));
	});

	test("refuses a loan without game or borrower", async ({ page }) => {
		const dialog = page.getByRole("dialog");

		await dialog.getByRole("button", { name: "Lend", exact: true }).click();

		await expect(dialog.getByText("Game is required")).toBeVisible();
		await expect(dialog.getByText("Player is required")).toBeVisible();

		await pickIn(page, dialog, "Game", returnedLoan.game);
		await expect(dialog.getByText("Game is required")).toBeHidden();
		await dialog.getByRole("button", { name: "Lend", exact: true }).click();

		await expect(dialog.getByText("Player is required")).toBeVisible();
		await expect(dialog).toBeVisible();
	});

	test("follows the chosen due date preset", async ({ page }) => {
		const dialog = page.getByRole("dialog");
		const start = subDays(new Date(), 2);

		await dialog.getByRole("radio", { name: "1 week" }).click();
		await expect(dialog.getByRole("radio", { name: /1 week/ })).toHaveText(
			`1 week · ${displayDate(addWeeks(new Date(), 1))}`,
		);

		await dialog.getByRole("radio", { name: "1 month" }).click();
		await expect(dialog.getByRole("radio", { name: /1 month/ })).toHaveText(
			`1 month · ${displayDate(addMonths(new Date(), 1))}`,
		);

		await typeDate(page, dialog, "Loan start", start);
		await expect(dialog.getByRole("radio", { name: /1 month/ })).toHaveText(
			`1 month · ${displayDate(addMonths(start, 1))}`,
		);

		await dialog.getByRole("radio", { name: "No due date" }).click();
		await expect(dialog.getByRole("group", { name: "Return by" })).toHaveCount(0);

		await dialog.getByRole("radio", { name: "Pick date" }).click();
		await expect(dialog.getByRole("group", { name: "Return by" })).toBeVisible();
	});

	test("refuses a due date before the start", async ({ page }) => {
		const dialog = page.getByRole("dialog");
		await dialog.getByRole("radio", { name: "Pick date" }).click();

		await typeDate(page, dialog, "Return by", subDays(new Date(), 1));

		await expect(dialog.getByText("The due date can't be before the start date")).toBeVisible();
	});

	test("forgets the choices when cancelled", async ({ page }) => {
		const dialog = page.getByRole("dialog");
		await pickIn(page, dialog, "Game", returnedLoan.game);
		await dialog.getByRole("radio", { name: "No due date" }).click();

		await dialog.getByRole("button", { name: "Cancel" }).click();

		await expect(dialog).toBeHidden();
		await expect(borrowers(page)).toHaveText(inDueOrder);

		await lendButton(page).click();

		await expect(dialog.getByRole("combobox", { name: "Game", exact: true })).toHaveText("Choose game");
		await expect(dialog.getByRole("radio", { name: /2 weeks/ })).toBeChecked();
	});
});

test.describe("elsewhere in the app", () => {
	for (const loan of activeLoans) {
		test(`says on the page of ${loan.game} who has it`, async ({ page }) => {
			await page.goto(`/games/${gameId(loan.game)}`);
			const link = page
				.getByRole("main")
				.getByRole("link", { name: `On loan to ${loan.player} · since ${displayDate(loan.loanDate)}` });

			await expect(link).toHaveAttribute("href", "/loans");

			await link.click();

			await expect(page).toHaveURL("/loans");
		});
	}

	test("shows a returned game as owned again", async ({ page }) => {
		await page.goto(`/games/${gameId(returnedLoan.game)}`);
		const main = page.getByRole("main");

		await expect(main.getByRole("heading", { name: returnedLoan.game, level: 1 })).toBeVisible();
		await expect(main.getByText("Owned", { exact: true })).toBeVisible();
		await expect(main.getByText(/On loan/)).toHaveCount(0);
	});

	test("marks only the lent games in the collection", async ({ page }) => {
		await openGames(page);

		await expect(page.getByRole("main").getByText("On loan", { exact: true })).toHaveCount(activeLoans.length);
		for (const loan of activeLoans) {
			await expect(gameLink(page, loan.game)).toContainText("On loan");
		}
		await expect(gameLink(page, returnedLoan.game)).not.toContainText("On loan");
	});

	test("leaves the lent games off the wheel", async ({ page }) => {
		const playerCount = 5;
		const onTheWheel = games
			.filter((game) => game.state === "owned" && game.minPlayers <= playerCount && game.maxPlayers >= playerCount)
			.filter((game) => !activeLoans.some((loan) => loan.game === game.title))
			.map((game) => game.title);
		await page.goto(`/games/pick?players=${playerCount}`);
		const main = page.getByRole("main");

		await expect(main.getByText(`${onTheWheel.length} games on the wheel`)).toBeVisible();
		await expect(main.getByRole("listitem")).toHaveCount(onTheWheel.length);
		const shown = await main.getByRole("listitem").allTextContents();
		expect(inTitleOrder(shown)).toEqual(inTitleOrder(onTheWheel));
		expect(shown).toContain(returnedLoan.game);
	});

	test("counts the lent games out of the whole wheel", async ({ page }) => {
		const owned = games.filter((game) => game.state === "owned").length;
		await page.goto("/games/pick");

		await expect(page.getByText(`10 of ${owned - activeLoans.length} games on the wheel`)).toBeVisible();
	});

	test("keeps the sessions of a lent game", async ({ page }) => {
		const played = sessions.filter((session) => session.game === onTimeLoan.game).length;
		await page.goto(`/games/${gameId(onTimeLoan.game)}`);

		await expect(page.getByRole("heading", { name: new RegExp(`^Played ${played} times`) })).toBeVisible();
	});
});
