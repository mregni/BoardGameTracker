import type { Page } from "@playwright/test";
import { expect } from "./fixtures";

export const apiFor = async (page: Page) => {
	await page.goto("/settings");
	const { accessToken } = await page.evaluate(() =>
		fetch("/api/auth/refresh", { method: "POST", headers: { "X-Requested-With": "XMLHttpRequest" } }).then((response) =>
			response.json(),
		),
	);
	const headers = { Authorization: `Bearer ${accessToken}` };

	const post = async (path: string, data: unknown) => {
		const response = await page.request.post(path, { data, headers });
		expect(response.ok(), `${path}: ${await response.text()}`).toBe(true);
		return (await response.json()) as { id: number };
	};

	const remove = async (path: string) => {
		const response = await page.request.delete(path, { headers });
		expect(response.ok(), `${path}: ${response.status()}`).toBe(true);
	};

	return { post, remove };
};
