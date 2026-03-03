import { useQueries } from "@tanstack/react-query";
import { useCallback } from "react";
import type { Game } from "@/models";
import { getGames } from "@/services/queries/games";

export const useGameById = () => {
	const [gameQuery] = useQueries({
		queries: [getGames()],
	});

	const games = gameQuery.data ?? [];

	const gameById = useCallback(
		(id: string | number | undefined): Game | null => {
			if (id === undefined) return null;

			const index = games.findIndex((x) => x.id === id);
			if (index !== -1) {
				return games[index];
			}

			return null;
		},
		[games],
	);

	return {
		gameById,
	};
};
