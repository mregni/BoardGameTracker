import type { Game, Session } from "@/models";

export interface LastPlayed {
	start: Date | string;
	gameTitle?: string;
}

export const lastPlayedOf = (sessions: Session[], gameById: (id: number) => Game | null): LastPlayed | null => {
	const latest = sessions.reduce<Session | undefined>(
		(current, session) => (!current || new Date(session.start) > new Date(current.start) ? session : current),
		undefined,
	);

	return latest ? { start: latest.start, gameTitle: gameById(latest.gameId)?.title } : null;
};
