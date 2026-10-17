import { describe, expect, it } from "vitest";
import { type GameNight, GameNightRsvpState, type Player } from "@/models";
import { inviteBlocker, inviteRecipients } from "./inviteRecipients";

const players = [
	{ id: 1, name: "Host", email: "host@example.com" },
	{ id: 2, name: "Zoe", email: " zoe@example.com " },
	{ id: 3, name: "Bob", email: "  " },
	{ id: 4, name: "Anna", email: "anna@example.com" },
	{ id: 5, name: "Cleo", email: "cleo@example.com" },
	{ id: 6, name: "Dirk", email: null },
] as unknown as Player[];

const rsvp = (playerId: number, state: GameNightRsvpState) => ({
	id: playerId + 10,
	playerId,
	state,
	player: players.find((player) => player.id === playerId),
});

const gameNight = {
	hostId: 1,
	invitedPlayers: [
		rsvp(1, GameNightRsvpState.Accepted),
		rsvp(2, GameNightRsvpState.Pending),
		rsvp(3, GameNightRsvpState.Pending),
		rsvp(4, GameNightRsvpState.Pending),
		rsvp(5, GameNightRsvpState.Maybe),
		rsvp(6, GameNightRsvpState.Pending),
	],
} as unknown as GameNight;

describe("inviteRecipients", () => {
	it("mails only the invitees without an answer, split by whether they have an email address", () => {
		expect(inviteRecipients(gameNight, players)).toEqual({
			withEmail: ["Anna", "Zoe"],
			withoutEmail: ["Bob", "Dirk"],
		});
	});

	it("treats invitees missing from the player list as having no email address", () => {
		expect(inviteRecipients(gameNight, [])).toEqual({
			withEmail: [],
			withoutEmail: ["Anna", "Bob", "Dirk", "Zoe"],
		});
	});
});

describe("inviteBlocker", () => {
	it("names the first thing that stops invitations from being mailed", () => {
		expect(inviteBlocker(false, false)).toBe("email-disabled");
		expect(inviteBlocker(false, true)).toBe("email-disabled");
		expect(inviteBlocker(true, false)).toBe("public-url-missing");
		expect(inviteBlocker(true, true)).toBeNull();
	});
});
