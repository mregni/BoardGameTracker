import { expect, request as playwrightRequest } from "@playwright/test";
import { credentials } from "./fixtures";

const SEEDED_LOCATION = 1;

interface Seat {
	playerId: number;
	score?: number | null;
	won?: boolean;
}

export const signedInApi = async (baseURL?: string) => {
	const request = await playwrightRequest.newContext({ baseURL });
	const login = await request.post("/api/auth/login", { data: credentials });
	expect(login.ok()).toBe(true);
	const { accessToken } = await login.json();
	const headers = { Authorization: `Bearer ${accessToken}` };

	const post = async (path: string, data: unknown): Promise<{ id: number }> => {
		const response = await request.post(path, { data, headers });
		expect(response.ok(), `${path}: ${await response.text()}`).toBe(true);
		return response.json();
	};

	const remove = async (path: string) => {
		const response = await request.delete(path, { headers });
		expect(response.ok(), `${path}: ${await response.text()}`).toBe(true);
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
