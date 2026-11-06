import { type APIRequestContext, expect } from "@playwright/test";
import { credentials } from "./fixtures";

let accessToken: string | null = null;

const signIn = async (request: APIRequestContext) => {
	const login = await request.post("/api/auth/login", { data: credentials });
	expect(login.ok()).toBe(true);
	return (await login.json()).accessToken as string;
};

export const adminApi = async (request: APIRequestContext) => {
	accessToken ??= await signIn(request);
	const headers = { Authorization: `Bearer ${accessToken}` };

	const send = async (method: "get" | "post" | "put" | "delete", path: string, data?: unknown) => {
		const response = await request[method](path, { data, headers });
		expect(response.ok(), `${method} ${path}: ${response.status()}`).toBe(true);
		return response;
	};

	return {
		get: async (path: string) => (await send("get", path)).json(),
		post: async (path: string, data: unknown) => (await send("post", path, data)).json(),
		put: async (path: string, data: unknown) => (await send("put", path, data)).json(),
		remove: async (path: string) => {
			await send("delete", path);
		},
	};
};
