import type { APIRequestContext } from "@playwright/test";
import { api } from "./api";

export const adminApi = async (request: APIRequestContext) => ({
	get: (path: string) => api(request, "get", path),
	post: (path: string, data: unknown) => api(request, "post", path, data),
	put: (path: string, data: unknown) => api(request, "put", path, data),
	remove: async (path: string) => {
		await api(request, "delete", path);
	},
});
