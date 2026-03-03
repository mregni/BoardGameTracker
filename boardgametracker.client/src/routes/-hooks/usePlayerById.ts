import { useQueries } from "@tanstack/react-query";
import { useCallback } from "react";
import type { Player } from "@/models";
import { getPlayers } from "@/services/queries/players";

export const usePlayerById = () => {
	const [playersQuery] = useQueries({
		queries: [getPlayers()],
	});

	const players = playersQuery.data ?? [];

	const playerById = useCallback(
		(id: number | undefined): Player | null => {
			if (id === undefined) return null;

			const index = players.findIndex((x) => x.id === id);
			if (index !== -1) {
				return players[index];
			}

			return null;
		},
		[players],
	);

	return {
		playerById,
	};
};
