import { type Api, apiFor, expect, test } from "../support/session";

const writes: [string, string][] = [
	["POST", "/api/game"],
	["PUT", "/api/game"],
	["DELETE", "/api/game/1"],
	["POST", "/api/game/bgg/search"],
	["GET", "/api/game/bgg/import"],
	["POST", "/api/game/bgg/import"],
	["POST", "/api/rag/game/1/ask"],
	["POST", "/api/game/1/watch"],
	["POST", "/api/game/1/expansions"],
	["POST", "/api/game/1/expansions/manual"],
	["DELETE", "/api/game/1/expansion/1"],
	["POST", "/api/player"],
	["PUT", "/api/player"],
	["DELETE", "/api/player/1"],
	["POST", "/api/session"],
	["PUT", "/api/session"],
	["DELETE", "/api/session/1"],
	["POST", "/api/gamenight"],
	["PUT", "/api/gamenight"],
	["DELETE", "/api/gamenight/1"],
	["POST", "/api/gamenight/1/end"],
	["POST", "/api/gamenight/1/reopen"],
	["PUT", "/api/gamenight/1/attendance"],
	["PUT", "/api/gamenight/1/suggestions/1"],
	["DELETE", "/api/gamenight/1/suggestions/1"],
	["POST", "/api/gamenight/1/send-invites"],
	["PUT", "/api/gamenight/rsvp"],
	["POST", "/api/gamenight/1/running-games"],
	["POST", "/api/gamenight/running-games/1/finish"],
	["DELETE", "/api/gamenight/running-games/1"],
	["POST", "/api/image"],
	["POST", "/api/loans"],
	["PUT", "/api/loans"],
	["PUT", "/api/loans/return"],
	["PUT", "/api/loans/1/undo-return"],
	["DELETE", "/api/loans/1"],
	["POST", "/api/location"],
	["PUT", "/api/location"],
	["DELETE", "/api/location/1"],
	["POST", "/api/manual/game/1"],
	["POST", "/api/manual/1/reindex"],
	["DELETE", "/api/manual/1"],
	["POST", "/api/auth/player-link/new"],
];

const adminOnly: [string, string][] = [
	["PUT", "/api/settings"],
	["POST", "/api/auth/register"],
	["POST", "/api/auth/reset-password/1"],
	["GET", "/api/admin/users"],
	["GET", "/api/admin/users/1"],
	["PUT", "/api/admin/users/1"],
	["PUT", "/api/admin/users/1/role"],
	["DELETE", "/api/admin/users/1"],
	["POST", "/api/admin/users/1/disable"],
	["POST", "/api/admin/users/1/enable"],
	["PUT", "/api/admin/players/1/account"],
	["POST", "/api/admin/users/1/role/identity-provider"],
	["GET", "/api/admin/users/1/external-logins"],
	["DELETE", "/api/admin/users/1/external-logins/1"],
	["GET", "/api/admin/oidc-providers"],
	["GET", "/api/admin/oidc-providers/setup"],
	["POST", "/api/admin/oidc-providers/test-discovery"],
	["POST", "/api/admin/oidc-providers"],
	["GET", "/api/admin/oidc-providers/1"],
	["PUT", "/api/admin/oidc-providers/1"],
	["DELETE", "/api/admin/oidc-providers/1"],
	["GET", "/api/settings/integrations/status"],
	["POST", "/api/settings/integrations/bgg/test"],
	["POST", "/api/maintenance/reset"],
	["POST", "/api/maintenance/factory-reset"],
	["POST", "/api/update/check"],
];

const reads = [
	"/api/game",
	"/api/game/1",
	"/api/player",
	"/api/player/1",
	"/api/gamenight",
	"/api/gamenight/1",
	"/api/loans",
	"/api/location",
];

const expectStatus = async (api: Api, method: string, path: string, status: number) => {
	const response = await api.call(method, path, method === "GET" || method === "DELETE" ? undefined : {});
	expect(response.status(), `${method} ${path}`).toBe(status);
};

test.describe("a reader", () => {
	let api: Api;

	test.beforeAll(async ({ readerContext }) => {
		api = await apiFor(readerContext);
	});

	for (const [method, path] of [...writes, ...adminOnly]) {
		test(`is refused ${method} ${path}`, async () => {
			await expectStatus(api, method, path, 403);
		});
	}

	for (const path of reads) {
		test(`may read ${path}`, async () => {
			await expectStatus(api, "GET", path, 200);
		});
	}
});

test.describe("a user", () => {
	let api: Api;

	test.beforeAll(async ({ writerContext }) => {
		api = await apiFor(writerContext);
	});

	for (const [method, path] of adminOnly) {
		test(`is refused ${method} ${path}`, async () => {
			await expectStatus(api, method, path, 403);
		});
	}

	for (const path of reads) {
		test(`may read ${path}`, async () => {
			await expectStatus(api, "GET", path, 200);
		});
	}

	for (const [method, path] of writes.filter(([, url]) => url !== "/api/auth/player-link/new")) {
		test(`passes the role check for ${method} ${path}`, async () => {
			const unknownIds = path.replace(/\/1(?=\/|$)/g, "/999999");
			const response = await api.call(method, unknownIds, method === "POST" || method === "PUT" ? {} : undefined);

			expect([401, 403]).not.toContain(response.status());
		});
	}
});

test.describe("a visitor", () => {
	for (const [method, path] of [...writes.filter(([, url]) => url !== "/api/auth/player-link/new"), ...adminOnly]) {
		test(`is refused ${method} ${path}`, async ({ guestPage }) => {
			const response = await guestPage.request.fetch(path, { method, data: method === "GET" ? undefined : {} });
			expect(response.status(), `${method} ${path}`).toBe(401);
		});
	}

	for (const path of reads) {
		test(`may not read ${path}`, async ({ guestPage }) => {
			expect((await guestPage.request.get(path)).status()).toBe(401);
		});
	}
});
