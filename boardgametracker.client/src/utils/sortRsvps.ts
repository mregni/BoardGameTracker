import { GameNightRsvpState, type GameNightRsvps } from "@/models";

type Compare = (a: GameNightRsvps, b: GameNightRsvps) => number;

const stateOrder: Record<GameNightRsvpState, number> = {
	[GameNightRsvpState.Accepted]: 0,
	[GameNightRsvpState.Maybe]: 1,
	[GameNightRsvpState.Pending]: 2,
	[GameNightRsvpState.Declined]: 3,
};

export const byState: Compare = (a, b) => stateOrder[a.state] - stateOrder[b.state];

export const byName: Compare = (a, b) => a.player.name.localeCompare(b.player.name);

export const sortRsvps = (rsvps: GameNightRsvps[], hostId: number, compare: Compare = byState): GameNightRsvps[] =>
	[...rsvps].sort((a, b) => {
		if (a.playerId === hostId) return -1;
		if (b.playerId === hostId) return 1;
		return compare(a, b);
	});
