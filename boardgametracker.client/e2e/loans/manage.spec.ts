import type { APIRequestContext, Page } from "@playwright/test";
import { addDays, addWeeks, subDays } from "date-fns";
import { adminApi } from "../support/adminApi";
import { expect, test } from "../support/fixtures";
import { displayDate } from "../support/format";
import {
	type ApiLoan,
	borrower,
	loanRow,
	openActions,
	openLoans,
	patchJson,
	patchLoans,
	pickIn,
	sidebarLink,
	toast,
	typeDate,
} from "../support/loansPage";
import { games, loans, players } from "../support/seedData";
import { expectingFailures } from "../support/settings";

const activeCount = loans.filter((loan) => !loan.returned).length;
const returnedCount = loans.length - activeCount;
const gameId = (title: string) => games.findIndex((game) => game.title === title) + 1;
const playerId = (name: string) => players.indexOf(name) + 1;
const [game, secondGame] = ["Earth", "Tend"];
const guest = players[5];
const overdueLoan = loans[1];
const wheelPlayers = 5;

test.use({ allowedApiStatuses: [401, 400] });

test.afterEach(async ({ request }) => {
	const api = await adminApi(request);
	const all: ApiLoan[] = await api.get("/api/loans");
	const seeded = loans.map((loan) => gameId(loan.game));
	for (const loan of all.filter((x) => !seeded.includes(x.gameId))) {
		await api.remove(`/api/loans/${loan.id}`);
	}
});

const lend = async (
	request: APIRequestContext,
	title: string,
	dates: { loanDate?: Date; dueDate?: Date | null } = {},
) => {
	const api = await adminApi(request);
	const { loanDate = subDays(new Date(), 10), dueDate = addDays(new Date(), 20) } = dates;
	const loan: ApiLoan = await api.post("/api/loans", {
		gameId: gameId(title),
		playerId: playerId(guest),
		loanDate: loanDate.toISOString(),
		dueDate: dueDate?.toISOString() ?? null,
	});
	return { api, loan };
};

const expectGameState = async (page: Page, title: string, state: string) => {
	await page.goto(`/games/${gameId(title)}`);
	await expect(page.getByRole("heading", { name: title, level: 1 })).toBeVisible();
	await expect(page.getByRole("main").getByText(state, { exact: true })).toBeVisible();
};

const wheelRows = async (page: Page) => {
	await page.goto(`/games/pick?players=${wheelPlayers}`);
	return page.getByRole("main").getByRole("listitem");
};

const startLending = async (page: Page, borrowerName = guest) => {
	await openLoans(page);
	await page.getByRole("button", { name: "Lend a game" }).click();
	const dialog = page.getByRole("dialog", { name: "Lend a game" });
	await pickIn(page, dialog, "Game", game);
	await pickIn(page, dialog, "Borrower", borrowerName);
	return dialog;
};

test("lends a game and shows it everywhere", async ({ page }) => {
	const today = displayDate(new Date());
	const dialog = await startLending(page);

	await dialog.getByRole("button", { name: "Lend", exact: true }).click();

	await expect(toast(page, "Game lent")).toBeVisible();
	await expect(dialog).toBeHidden();
	await expect(sidebarLink(page, `Loans ${activeCount + 1}`)).toBeVisible();
	await expect(page.getByRole("main").getByRole("button", { name: `On loan ${activeCount + 1}` })).toBeVisible();
	await expect(borrower(page, guest).getByRole("paragraph")).toHaveText("1 game");
	await expect(loanRow(page, game)).toContainText(`since ${today}`);
	await expect(loanRow(page, game)).toContainText(`Due ${displayDate(addWeeks(new Date(), 2))}`);

	await page.getByRole("button", { name: "Lend a game" }).click();
	await dialog.getByRole("combobox", { name: "Game", exact: true }).click();
	const option = page.getByRole("option").filter({ hasText: game });
	await expect(option).toHaveAttribute("aria-disabled", "true");
	await expect(option).toContainText(`With ${guest} since ${today}`);
	await page.keyboard.press("Escape");

	await expectGameState(page, game, `On loan to ${guest} · since ${today}`);
	const rows = await wheelRows(page);
	await expect(rows.filter({ hasText: secondGame })).toHaveCount(1);
	await expect(rows.filter({ hasText: game })).toHaveCount(0);
});

test("lends without a due date", async ({ page }) => {
	const dialog = await startLending(page);

	await dialog.getByRole("radio", { name: "No due date" }).click();
	await dialog.getByRole("button", { name: "Lend", exact: true }).click();

	await expect(loanRow(page, game).getByText("No due date")).toBeVisible();
});

test("lends with a picked date and a start in the past", async ({ page }) => {
	const start = subDays(new Date(), 3);
	const dialog = await startLending(page);

	await typeDate(page, dialog, "Loan start", start);
	await dialog.getByRole("radio", { name: "Pick date" }).click();
	await typeDate(page, dialog, "Return by", addDays(new Date(), 2));
	await dialog.getByRole("button", { name: "Lend", exact: true }).click();

	await expect(loanRow(page, game)).toContainText(`since ${displayDate(start)}`);
	await expect(loanRow(page, game).getByText("Due in 2 days")).toBeVisible();
	await expect(page.getByRole("main").getByRole("button", { name: "Due this week 1" })).toBeVisible();
});

test.describe("a new borrower", () => {
	const name = "Temporary borrower";

	const createInDialog = async (page: Page) => {
		await openLoans(page);
		await page.getByRole("button", { name: "Lend a game" }).click();
		const dialog = page.getByRole("dialog", { name: "Lend a game" });
		await pickIn(page, dialog, "Game", game);
		await dialog.getByRole("button", { name: "New player" }).click();
		const playerDialog = page.getByRole("dialog").last();
		await playerDialog.getByRole("textbox").first().fill(name);
		await playerDialog.getByRole("button", { name: /save|add|create/i }).click();
		await expect(page.getByRole("dialog")).toHaveCount(1);
		return dialog;
	};

	test.afterEach(async ({ request }) => {
		const api = await adminApi(request);
		const all: { id: number; name: string }[] = await api.get("/api/player");
		for (const player of all.filter((x) => x.name === name)) {
			await api.remove(`/api/player/${player.id}`);
		}
	});

	test("can be created from the dialog", async ({ page }) => {
		const dialog = await createInDialog(page);

		await pickIn(page, dialog, "Borrower", name);
		await dialog.getByRole("button", { name: "Lend", exact: true }).click();

		await expect(borrower(page, name).getByRole("paragraph")).toHaveText("1 game");
	});

	test("is selected right away", async ({ page }) => {
		const dialog = await createInDialog(page);

		test.fail(true, "BUG: a player created from the lend dialog is not selected as borrower");
		await expect(dialog.getByRole("combobox", { name: "Borrower", exact: true })).toContainText(name, {
			timeout: 3_000,
		});
	});
});

test.describe("a game that was lent in the meantime", () => {
	const tryToLend = async (page: Page) => {
		await patchLoans(page, (all) => all.filter((loan) => loan.gameId !== gameId(overdueLoan.game)));
		await page.route("**/api/game", (route) =>
			patchJson<{ id: number; isLoaned: boolean }[]>(route, (all) =>
				all.map((x) => (x.id === gameId(overdueLoan.game) ? { ...x, isLoaned: false } : x)),
			),
		);
		await openLoans(page);
		await page.getByRole("button", { name: "Lend a game" }).click();
		const dialog = page.getByRole("dialog");
		await pickIn(page, dialog, "Game", overdueLoan.game);
		await pickIn(page, dialog, "Borrower", guest);
		await dialog.getByRole("button", { name: "Lend", exact: true }).click();
		await expect(toast(page, "This game is already on loan in that period")).toBeVisible();
		return dialog;
	};

	expectingFailures("is refused", async ({ page }) => {
		const dialog = await tryToLend(page);

		await expect(dialog).toBeVisible();
	});

	test("gives no unhandled error in the page", async ({ page }) => {
		await tryToLend(page);
		test.fail(true, "BUG: a refused loan rejects mutateAsync inside form submit: unhandled promise rejection");
	});
});

test("returns a game and undoes it", async ({ page, request }) => {
	await lend(request, game);
	await openLoans(page);
	await expect(sidebarLink(page, `Loans ${activeCount + 1}`)).toBeVisible();

	await page.getByRole("button", { name: `Mark returned ${game}` }).click();
	const dialog = page.getByRole("dialog");
	await expect(dialog.getByRole("heading", { name: `Return ${game}` })).toBeVisible();
	await dialog.getByRole("button", { name: "Mark returned" }).click();

	await expect(toast(page, "Marked as returned")).toBeVisible();
	await expect(dialog).toBeHidden();
	await expect(sidebarLink(page, `Loans ${activeCount}`)).toBeVisible();
	await expect(borrower(page, guest)).toHaveCount(0);
	await expect(page.getByText(`${returnedCount + 1} returned loans`)).toBeVisible();

	await page.getByRole("button", { name: "Show history" }).click();
	await expect(loanRow(page, game)).toContainText(`Returned ${displayDate(new Date())}`);

	await page.getByRole("button", { name: "Undo" }).click();

	await expect(toast(page, "Back on loan")).toBeVisible();
	await expect(sidebarLink(page, `Loans ${activeCount + 1}`)).toBeVisible();
	await expect(borrower(page, guest).getByRole("paragraph")).toHaveText("1 game");
	await expect(page.getByText(`${returnedCount} returned loan`, { exact: true })).toBeVisible();
});

test("makes a returned game available again", async ({ page, request }) => {
	const { api, loan } = await lend(request, game);
	await expectGameState(page, game, `On loan to ${guest} · since ${displayDate(new Date(loan.loanDate))}`);

	await api.put("/api/loans/return", { id: loan.id, returnDate: new Date().toISOString() });

	await expectGameState(page, game, "Owned");
	await expect((await wheelRows(page)).filter({ hasText: game })).toHaveCount(1);
});

test("refuses a return date before the start of the loan", async ({ page, request }) => {
	const start = subDays(new Date(), 10);
	await lend(request, game, { loanDate: start });
	await openLoans(page);
	await page.getByRole("button", { name: `Mark returned ${game}` }).click();
	const dialog = page.getByRole("dialog");

	await typeDate(page, dialog, "Returned on", subDays(start, 1));

	await expect(dialog.getByText("The return date can't be before the start of the loan")).toBeVisible();
	const sent = page.waitForRequest("**/api/loans/return", { timeout: 1_000 }).catch(() => null);

	await dialog.getByRole("button", { name: "Mark returned" }).click();

	expect(await sent).toBeNull();
	await expect(dialog).toBeVisible();

	await dialog.getByRole("button", { name: "Cancel" }).click();

	await expect(borrower(page, guest).getByRole("paragraph")).toHaveText("1 game");
});

test("returns all games of a borrower and undoes that", async ({ page, request }) => {
	await lend(request, game);
	await lend(request, secondGame);
	await openLoans(page);
	await expect(borrower(page, guest).getByRole("paragraph")).toHaveText("2 games");

	await borrower(page, guest).getByRole("button", { name: "Mark all returned" }).click();
	const dialog = page.getByRole("dialog");
	await expect(dialog.getByRole("heading", { name: `Return 2 games from ${guest}` })).toBeVisible();
	await dialog.getByRole("button", { name: "Mark returned" }).click();

	await expect(borrower(page, guest)).toHaveCount(0);
	await expect(sidebarLink(page, `Loans ${activeCount}`)).toBeVisible();
	await expect(page.getByText(`${returnedCount + 2} returned loans`)).toBeVisible();

	await page.getByRole("button", { name: "Undo" }).click();

	await expect(borrower(page, guest).getByRole("paragraph")).toHaveText("2 games");
	await expect(sidebarLink(page, `Loans ${activeCount + 2}`)).toBeVisible();
});

test("edits the dates of a loan", async ({ page, request }) => {
	const start = subDays(new Date(), 10);
	const due = addDays(new Date(), 20);
	const newStart = subDays(new Date(), 12);
	await lend(request, game, { loanDate: start, dueDate: due });
	await openLoans(page);

	await openActions(page, game, "Edit");
	const dialog = page.getByRole("dialog", { name: "Edit loan" });
	await expect(dialog).toContainText("Change the dates of this loan.");
	await expect(dialog.getByRole("group", { name: "Loan start" }).getByRole("spinbutton")).toHaveText(
		displayDate(start).split("-"),
	);
	await expect(dialog.getByRole("group", { name: "Due date" }).getByRole("spinbutton")).toHaveText(
		displayDate(due).split("-"),
	);
	await expect(dialog.getByRole("group", { name: "Returned on" })).toHaveCount(0);

	await typeDate(page, dialog, "Loan start", newStart);
	await typeDate(page, dialog, "Due date", addDays(new Date(), 2));
	await dialog.getByRole("button", { name: "Save" }).click();

	await expect(toast(page, "Loan saved")).toBeVisible();
	await expect(dialog).toBeHidden();
	await expect(loanRow(page, game)).toContainText(`since ${displayDate(newStart)}`);
	await expect(loanRow(page, game).getByText("Due in 2 days")).toBeVisible();

	await page.reload();

	await expect(loanRow(page, game)).toContainText(`since ${displayDate(newStart)}`);
});

test("keeps the dates when editing is cancelled", async ({ page, request }) => {
	const start = subDays(new Date(), 10);
	await lend(request, game, { loanDate: start });
	await openLoans(page);
	await openActions(page, game, "Edit");
	const dialog = page.getByRole("dialog", { name: "Edit loan" });

	await typeDate(page, dialog, "Loan start", subDays(start, 2));
	await dialog.getByRole("button", { name: "Cancel" }).click();

	await expect(dialog).toBeHidden();
	await expect(loanRow(page, game)).toContainText(`since ${displayDate(start)}`);
});

test("removes the due date of a loan", async ({ page, request }) => {
	await lend(request, game);
	await openLoans(page);
	await openActions(page, game, "Edit");
	const dialog = page.getByRole("dialog", { name: "Edit loan" });

	await dialog.getByRole("button", { name: /clear/i }).click();
	await dialog.getByRole("button", { name: "Save" }).click();

	await expect(loanRow(page, game).getByText("No due date")).toBeVisible();
});

test("refuses a due date before the start when editing", async ({ page, request }) => {
	const start = subDays(new Date(), 10);
	await lend(request, game, { loanDate: start });
	await openLoans(page);
	await openActions(page, game, "Edit");
	const dialog = page.getByRole("dialog", { name: "Edit loan" });

	await typeDate(page, dialog, "Due date", subDays(start, 1));

	await expect(dialog.getByText("The due date can't be before the start date")).toBeVisible();
	const sent = page.waitForRequest("**/api/loans", { timeout: 1_000 }).catch(() => null);

	await dialog.getByRole("button", { name: "Save" }).click();

	expect(await sent).toBeNull();
	await expect(dialog).toBeVisible();
});

test("edits the return date of a returned loan", async ({ page, request }) => {
	const start = subDays(new Date(), 10);
	const newReturn = subDays(new Date(), 4);
	const { api, loan } = await lend(request, game, { loanDate: start });
	await api.put("/api/loans/return", { id: loan.id, returnDate: new Date().toISOString() });
	await openLoans(page);
	await page.getByRole("button", { name: "Show history" }).click();

	await openActions(page, game, "Edit");
	const dialog = page.getByRole("dialog", { name: "Edit loan" });
	await typeDate(page, dialog, "Returned on", subDays(start, 1));
	await expect(dialog.getByText("The return date can't be before the start of the loan")).toBeVisible();

	await typeDate(page, dialog, "Returned on", newReturn);
	await dialog.getByRole("button", { name: "Save" }).click();

	await expect(loanRow(page, game)).toContainText(`Returned ${displayDate(newReturn)}`);
});

test("deletes a loan", async ({ page, request }) => {
	await lend(request, game);
	await openLoans(page);
	await expect(sidebarLink(page, `Loans ${activeCount + 1}`)).toBeVisible();

	await openActions(page, game, "Delete");
	const dialog = page.getByRole("dialog");
	await expect(dialog.getByRole("heading", { name: `Delete loan of ${game}` })).toBeVisible();
	await expect(dialog).toContainText("This removes the loan and its history for good.");
	await dialog.getByRole("button", { name: "Delete" }).click();

	await expect(toast(page, "Loan deleted")).toBeVisible();
	await expect(dialog).toBeHidden();
	await expect(borrower(page, guest)).toHaveCount(0);
	await expect(sidebarLink(page, `Loans ${activeCount}`)).toBeVisible();

	await expectGameState(page, game, "Owned");
});

test("deletes a returned loan from the history", async ({ page, request }) => {
	const { api, loan } = await lend(request, game);
	await api.put("/api/loans/return", { id: loan.id, returnDate: new Date().toISOString() });
	await openLoans(page);
	await expect(page.getByText(`${returnedCount + 1} returned loans`)).toBeVisible();
	await page.getByRole("button", { name: "Show history" }).click();

	await openActions(page, game, "Delete");
	await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();

	await expect(loanRow(page, game)).toHaveCount(0);
	await expect(page.getByText(`${returnedCount} returned loan`, { exact: true })).toBeVisible();
});

test("removes the loans of a deleted borrower", async ({ page, request }) => {
	const name = "Leaving borrower";
	const api = await adminApi(request);
	const player = await api.post("/api/player", { name });
	await api.post("/api/loans", {
		gameId: gameId(game),
		playerId: player.id,
		loanDate: subDays(new Date(), 2).toISOString(),
		dueDate: null,
	});
	await openLoans(page);
	await expect(borrower(page, name).getByRole("paragraph")).toHaveText("1 game");

	await api.remove(`/api/player/${player.id}`);
	await page.reload();

	await expect(borrower(page, overdueLoan.player)).toBeVisible();
	await expect(borrower(page, name)).toHaveCount(0);
	await expect(sidebarLink(page, `Loans ${activeCount}`)).toBeVisible();
});

test("does not call a game lent before the loan starts", async ({ page, request }) => {
	await lend(request, game, { loanDate: addDays(new Date(), 5), dueDate: addDays(new Date(), 10) });
	await openLoans(page);

	await expect(loanRow(page, game)).toContainText(`since ${displayDate(addDays(new Date(), 5))}`);
	await expect(sidebarLink(page, `Loans ${activeCount}`)).toBeVisible();

	await expectGameState(page, game, "Owned");
});

test("shows the same number of active loans on the page and in the sidebar", async ({ page, request }) => {
	await lend(request, game, { loanDate: addDays(new Date(), 5), dueDate: addDays(new Date(), 10) });
	await openLoans(page);
	await expect(sidebarLink(page, `Loans ${activeCount}`)).toBeVisible();

	test.fail(true, "BUG: a loan that starts in the future counts as On loan on /loans but not in the sidebar");
	await expect(page.getByRole("main").getByRole("button", { name: `On loan ${activeCount}` })).toBeVisible({
		timeout: 2_000,
	});
});

test("shows an error when returning fails", async ({ page, request }) => {
	await lend(request, game);
	await page.route("**/api/loans/return", (route) => route.abort());
	await openLoans(page);

	await page.getByRole("button", { name: `Mark returned ${game}` }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Mark returned" }).click();

	await expect(toast(page, "Couldn't mark the game as returned. Try again in a moment.")).toBeVisible();
	await expect(borrower(page, guest).getByRole("paragraph")).toHaveText("1 game");
	await expect(sidebarLink(page, `Loans ${activeCount + 1}`)).toBeVisible();
});

test("shows an error when deleting fails", async ({ page, request }) => {
	const { loan } = await lend(request, game);
	await page.route(`**/api/loans/${loan.id}`, (route) => route.abort());
	await openLoans(page);

	await openActions(page, game, "Delete");
	await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();

	await expect(toast(page, "Couldn't delete the loan. Try again in a moment.")).toBeVisible();
	await expect(borrower(page, guest).getByRole("paragraph")).toHaveText("1 game");
});
