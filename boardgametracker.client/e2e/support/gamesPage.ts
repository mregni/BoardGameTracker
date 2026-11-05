import { expect, type Page } from "@playwright/test";
import { expected } from "./seedData";

export const gameLinks = (page: Page) => page.getByRole("main").getByRole("link");

export const expectGames = async (page: Page, titles: string[]) => {
	await expect(gameLinks(page)).toHaveCount(titles.length);
	if (titles.length > 0) await expect(gameLinks(page)).toContainText(titles);
};

export const inTitleOrder = (titles: string[]) => [...titles].sort((a, b) => a.localeCompare(b));

export const gameLink = (page: Page, title: string) => page.getByRole("link", { name: new RegExp(`^${title} `) });

export const pick = async (page: Page, combobox: string, option: string) => {
	await page.getByRole("combobox", { name: combobox, exact: true }).click();
	await page.getByRole("option", { name: option, exact: true }).click();
};

export const openGames = async (page: Page) => {
	await page.goto("/games");
	await expect(page.getByText(`${expected.gameCount} games`)).toBeVisible();
};
