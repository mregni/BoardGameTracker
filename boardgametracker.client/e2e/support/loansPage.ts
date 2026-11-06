import { expect, type Locator, type Page, type Route } from "@playwright/test";
import { format } from "date-fns";

export interface ApiLoan {
	id: number;
	gameId: number;
	playerId: number;
	loanDate: string;
	dueDate: string | null;
	returnedDate: string | null;
	isActive: boolean;
}

export const sidebarLink = (page: Page, name: string) =>
	page.getByRole("navigation").getByRole("link", { name, exact: true });

export const toast = (page: Page, text: string) => page.getByRole("region", { name: /Notifications/ }).getByText(text);

export const borrower = (page: Page, name: string) => page.getByRole("main").getByRole("region", { name });

export const borrowers = (page: Page) => page.getByRole("main").getByRole("heading", { level: 2 });

export const loanRow = (page: Page, game: string) =>
	page
		.getByRole("main")
		.getByRole("listitem")
		.filter({ has: page.getByRole("link", { name: game, exact: true }) });

export const openActions = async (page: Page, game: string, action: "Edit" | "Delete") => {
	await page.getByRole("button", { name: `Actions for ${game}` }).click();
	await page.getByRole("menuitem", { name: action }).click();
};

export const typeDate = async (page: Page, scope: Locator, label: string, date: Date) => {
	await scope.getByRole("spinbutton", { name: `day, ${label}` }).click();
	await page.keyboard.type(format(date, "ddMMyyyy"));
};

export const pickIn = async (page: Page, dialog: Locator, combobox: string, option: string) => {
	await dialog.getByRole("combobox", { name: combobox, exact: true }).click();
	await page.getByRole("option").filter({ hasText: option }).click();
};

export const patchJson = async <T>(route: Route, patch: (body: T) => T) => {
	const response = await route.fetch();
	if (!response.ok()) {
		await route.fulfill({ response });
		return;
	}
	await route.fulfill({ response, json: patch(await response.json()) });
};

export const patchLoans = async (page: Page, patch: (loans: ApiLoan[]) => ApiLoan[]) => {
	await page.route("**/api/loans", async (route) => {
		if (route.request().method() !== "GET") {
			await route.fallback();
			return;
		}
		await patchJson(route, patch);
	});
};

export const openLoans = async (page: Page) => {
	await page.goto("/loans");
	await expect(page.getByRole("heading", { name: "Loans", level: 1 })).toBeVisible();
};
