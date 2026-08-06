import { describe, expect, it } from "vitest";
import type { Player, UserDto } from "@/models";
import { buildLinkablePlayerItems } from "./playerLinkOptions";

const player = (id: number, name: string): Player => ({ id, name, image: null, email: null, badges: [] });

const user = (id: string, playerId: number | null): UserDto => ({
	id,
	username: id,
	email: null,
	roles: [],
	createdAt: new Date(),
	lastLoginAt: null,
	playerId,
});

describe("buildLinkablePlayerItems", () => {
	it("includes unlinked players and excludes players linked to other users", () => {
		const players = [player(1, "Alice"), player(2, "Bob"), player(3, "Cara")];
		const users = [user("u1", 2)];

		const result = buildLinkablePlayerItems(players, users);

		expect(result.map((r) => r.value)).toEqual([1, 3]);
	});

	it("retains the excluded user's own linked player", () => {
		const players = [player(1, "Alice"), player(2, "Bob")];
		const users = [user("u1", 2)];

		const result = buildLinkablePlayerItems(players, users, "u1");

		expect(result.map((r) => r.value)).toEqual([1, 2]);
	});

	it("excludes a player linked to a different user even when excludeUserId is set", () => {
		const players = [player(1, "Alice"), player(2, "Bob")];
		const users = [user("u1", 2)];

		const result = buildLinkablePlayerItems(players, users, "u2");

		expect(result.map((r) => r.value)).toEqual([1]);
	});

	it("returns all players when no users are linked", () => {
		const players = [player(1, "Alice"), player(2, "Bob")];

		const result = buildLinkablePlayerItems(players, [], "u1");

		expect(result.map((r) => r.value)).toEqual([1, 2]);
	});

	it("maps players to value/label pairs", () => {
		const result = buildLinkablePlayerItems([player(7, "Dana")], []);

		expect(result).toEqual([{ value: 7, label: "Dana" }]);
	});
});
