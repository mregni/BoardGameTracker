import type { CreateSessionPlayer, Game } from "@/models";

interface PlayModeValues {
	isCoop: boolean;
	teamWon: boolean | null;
	teamScore?: number | null;
	playerSessions: CreateSessionPlayer[];
}

export const coopDefaults = (game?: Pick<Game, "hasCoopMode" | "coopByDefault">) => ({
	isCoop: (game?.hasCoopMode ?? false) && (game?.coopByDefault ?? false),
	teamWon: null as boolean | null,
	teamScore: null as number | null,
});

export const applyPlayMode = <T extends PlayModeValues>(value: T, hasScoring: boolean): T => {
	if (value.isCoop) {
		return {
			...value,
			teamScore: hasScoring ? (value.teamScore ?? null) : null,
			playerSessions: value.playerSessions.map((player) => ({ ...player, won: value.teamWon === true, score: null })),
		};
	}

	return {
		...value,
		teamWon: null,
		teamScore: null,
		playerSessions: value.playerSessions.map((player) => (hasScoring ? player : { ...player, score: null })),
	};
};
