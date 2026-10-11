import { GameNightRsvpState, type GameNightRsvps } from "@/models";

const stateOrder: Record<GameNightRsvpState, number> = {
	[GameNightRsvpState.Accepted]: 0,
	[GameNightRsvpState.Pending]: 1,
	[GameNightRsvpState.Declined]: 2,
};

export const sortRsvps = (rsvps: GameNightRsvps[], hostId: number): GameNightRsvps[] =>
	[...rsvps].sort((a, b) => {
		if (a.playerId === hostId) return -1;
		if (b.playerId === hostId) return 1;
		return stateOrder[a.state] - stateOrder[b.state];
	});
