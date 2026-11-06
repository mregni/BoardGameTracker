import { request as playwrightRequest } from "@playwright/test";
import { api } from "./api";

const SEEDED_LOCATION = 1;

interface Seat {
	playerId: number;
	score?: number | null;
	won?: boolean;
}

export const signedInApi = async (baseURL?: string) => {
	const request = await playwrightRequest.newContext({ baseURL });

	const post = (path: string, data: unknown): Promise<{ id: number }> => api(request, "post", path, data);

	const remove = async (path: string) => {
		await api(request, "delete", path);
	};

	return {
		addPlayer: async (name: string) => (await post("/api/player", { name })).id,
		addSession: async (session: {
			gameId: number;
			start: Date;
			minutes: number;
			seats: Seat[];
			isCoop?: boolean;
			teamWon?: boolean | null;
		}) =>
			(
				await post("/api/session", {
					gameId: session.gameId,
					start: session.start.toISOString(),
					minutes: session.minutes,
					locationId: SEEDED_LOCATION,
					expansionIds: [],
					isCoop: session.isCoop ?? false,
					teamWon: session.teamWon ?? null,
					playerSessions: session.seats.map((seat) => ({
						playerId: seat.playerId,
						score: seat.score ?? null,
						firstPlay: false,
						won: seat.won ?? false,
					})),
				})
			).id,
		removeSession: (id: number) => remove(`/api/session/${id}`),
		removePlayer: (id: number) => remove(`/api/player/${id}`),
	};
};

export type Api = Awaited<ReturnType<typeof signedInApi>>;
