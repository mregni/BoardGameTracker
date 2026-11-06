import type { APIRequestContext, Page } from "@playwright/test";
import { credentials, expect, paceLogin } from "./fixtures";

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

let token: Promise<string> | null = null;

const authorization = async (request: APIRequestContext) => {
	token ??= paceLogin()
		.then(() => request.post("/api/auth/login", { data: credentials }))
		.then((response) => response.json())
		.then((body) => body.accessToken as string);
	return { Authorization: `Bearer ${await token}` };
};

export const api = async (
	request: APIRequestContext,
	method: "get" | "post" | "put" | "delete",
	path: string,
	data?: unknown,
) => {
	const send = async () => request[method](path, { data, headers: await authorization(request) });
	let response = await send();
	if (response.status() === 401) {
		token = null;
		response = await send();
	}
	const body = await response.text();
	expect(response.ok(), `${method} ${path}: ${body}`).toBe(true);
	return body ? JSON.parse(body) : null;
};
