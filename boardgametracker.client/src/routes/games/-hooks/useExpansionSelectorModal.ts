import { useMutation, useQueries, useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";
import { type ExpansionLink, QUERY_KEYS } from "@/models";
import { saveGameExpansionCall } from "@/services/gameService";
import { getGameExpansions } from "@/services/queries/games";

interface Props {
	gameId: number;
}

export const useExpansionSelectorModal = ({ gameId }: Props) => {
	const queryClient = useQueryClient();

	const [expansionQuery] = useQueries({
		queries: [getGameExpansions(gameId)],
	});

	const expansions = useMemo<ExpansionLink[]>(
		() => (expansionQuery.data ?? []).map((expansion) => ({ id: expansion.bggId, value: expansion.title })),
		[expansionQuery.data],
	);

	const mutateExpasions = useMutation({
		mutationFn: saveGameExpansionCall,
		onSuccess: async () => {
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.game, gameId] });
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
		},
	});

	return {
		expansions,
		isLoading: expansionQuery.isLoading,
		saveExpansions: mutateExpasions.mutateAsync,
		isPending: mutateExpasions.isPending,
	};
};
