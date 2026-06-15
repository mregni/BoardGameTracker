import { useMutation, useQueryClient } from "@tanstack/react-query";
import { type Game, QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { updateGameCall } from "@/services/gameService";

export const useInlineGameUpdate = () => {
	const queryClient = useQueryClient();
	const { errorToast } = useToasts();

	const mutation = useMutation({
		mutationFn: updateGameCall,
		onMutate: async (updated: Game) => {
			await queryClient.cancelQueries({ queryKey: [QUERY_KEYS.games] });
			const previous = queryClient.getQueryData<Game[]>([QUERY_KEYS.games]);
			queryClient.setQueryData<Game[]>([QUERY_KEYS.games], (old) =>
				old?.map((game) => (game.id === updated.id ? updated : game)),
			);
			return { previous };
		},
		onError: (_error, _updated, context) => {
			if (context?.previous) {
				queryClient.setQueryData([QUERY_KEYS.games], context.previous);
			}
			errorToast("game:notifications.update-failed");
		},
		onSettled: () => {
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.shames] });
		},
	});

	return { updateGame: mutation.mutate };
};
