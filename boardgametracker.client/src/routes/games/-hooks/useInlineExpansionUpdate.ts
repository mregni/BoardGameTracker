import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useCallback } from "react";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import { type Expansion, type Game, QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { updateExpansionCall } from "@/services/gameService";

const withExpansion = (game: Game, updated: Expansion): Game => ({
	...game,
	expansions: game.expansions.map((expansion) => (expansion.id === updated.id ? updated : expansion)),
});

export const useInlineExpansionUpdate = () => {
	const queryClient = useQueryClient();
	const invalidator = useQueryInvalidator();
	const { errorToast } = useToasts();

	const mutation = useMutation({
		mutationFn: ({ gameId, expansion }: { gameId: number; expansion: Expansion }) =>
			updateExpansionCall(gameId, expansion.id, {
				state: expansion.state,
				buyingPrice: expansion.buyingPrice,
				soldPrice: expansion.soldPrice,
				additionDate: expansion.additionDate,
			}),
		onMutate: async ({ gameId, expansion }) => {
			await Promise.all([
				queryClient.cancelQueries({ queryKey: [QUERY_KEYS.games] }),
				queryClient.cancelQueries({ queryKey: [QUERY_KEYS.game, gameId] }),
			]);
			const previousGames = queryClient.getQueryData<Game[]>([QUERY_KEYS.games]);
			const previousGame = queryClient.getQueryData<Game>([QUERY_KEYS.game, gameId]);
			queryClient.setQueryData<Game[]>([QUERY_KEYS.games], (old) =>
				old?.map((game) => (game.id === gameId ? withExpansion(game, expansion) : game)),
			);
			queryClient.setQueryData<Game>([QUERY_KEYS.game, gameId], (old) => (old ? withExpansion(old, expansion) : old));
			return { previousGames, previousGame };
		},
		onError: (_error, { gameId }, context) => {
			if (context?.previousGames) {
				queryClient.setQueryData([QUERY_KEYS.games], context.previousGames);
			}
			if (context?.previousGame) {
				queryClient.setQueryData([QUERY_KEYS.game, gameId], context.previousGame);
			}
			errorToast("game:notifications.expansion-update-failed");
		},
		onSettled: (_data, _error, { gameId }) => {
			void invalidator.invalidateGame(gameId);
		},
	});

	const { mutate } = mutation;
	const updateExpansion = useCallback(
		(gameId: number, expansion: Expansion) => mutate({ gameId, expansion }),
		[mutate],
	);

	return { updateExpansion };
};
