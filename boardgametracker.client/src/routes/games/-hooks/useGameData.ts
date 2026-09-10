import { useMutation, useQueries, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { QUERY_KEYS } from "@/models";
import { isPriceError, priceErrorKey } from "@/models/Games/GamePrice";
import { useToasts } from "@/routes/-hooks/useToasts";
import { createWatchCall, deleteExpansionCall, deleteGameCall, getGamePriceCall } from "@/services/gameService";
import { getGame, getGamePrice, getGameSessionsShortList, getGameStatistics } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { apiErrorMessage } from "@/utils/errorUtils";

interface UseGameDataProps {
	gameId: number;
	onDeleteSuccess?: () => void;
	onDeleteExpansionSuccess?: () => void;
}

export const useGameData = (props: UseGameDataProps) => {
	const { gameId, onDeleteSuccess, onDeleteExpansionSuccess } = props;
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();

	const [gameQuery, settingsQuery, statisticsQuery, sessionsQuery] = useQueries({
		queries: [getGame(gameId), getSettings(), getGameStatistics(gameId), getGameSessionsShortList(gameId, 5)],
	});

	const game = gameQuery.data;
	const settings = settingsQuery.data;
	const statistics = statisticsQuery.data;
	const sessions = sessionsQuery.data;
	const isLoading =
		gameQuery.isLoading || settingsQuery.isLoading || sessionsQuery.isLoading || statisticsQuery.isLoading;

	const priceQuery = useQuery({
		...getGamePrice(gameId),
		enabled: !!game?.changeDetectionWatchId && !!settings?.changeDetectionStatus?.isConfigured,
	});
	const price = priceQuery.data;

	const pollPrice = (delays: number[]) => {
		const [next, ...rest] = delays;
		if (next === undefined) return;
		setTimeout(() => {
			getGamePriceCall(gameId, true)
				.then((fresh) => {
					queryClient.setQueryData([QUERY_KEYS.game, gameId, QUERY_KEYS.price], fresh);
					if (!fresh.available) pollPrice(rest);
				})
				.catch(() => {});
		}, next);
	};

	const refreshPriceMutation = useMutation({
		mutationFn: () => getGamePriceCall(gameId, true),
		onSuccess: (data) => {
			queryClient.setQueryData([QUERY_KEYS.game, gameId, QUERY_KEYS.price], data);
			if (data && !data.available && isPriceError(data.status)) {
				errorToast(priceErrorKey(data.status));
			}
			if (data?.recheckQueued) {
				successToast("game:price.recheck-queued");
				pollPrice([20000]);
			}
		},
		onError: () => errorToast("game:price.refresh-failed"),
	});

	const createWatchMutation = useMutation({
		mutationFn: (url: string) => createWatchCall(gameId, url),
		onSuccess: (data) => {
			queryClient.setQueryData([QUERY_KEYS.game, gameId], data);
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.game, gameId, QUERY_KEYS.price] });
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.trackedPrices] });
			successToast("game:track-price.success");
			pollPrice([5000, 15000, 30000]);
		},
		onError: (error) => toast.error(apiErrorMessage(error, "game:track-price.failed")),
	});

	const deleteGame = async () => {
		if (gameId !== undefined) {
			try {
				await deleteGameCall(gameId);
				queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
				queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
				queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.trackedPrices] });
				successToast("game:delete.successfull");
				onDeleteSuccess?.();
			} catch {
				errorToast("game:delete.failed");
			}
		}
	};

	const deleteExpansion = async (id: number, gameIdParam: number) => {
		try {
			await deleteExpansionCall(id, gameIdParam);
			queryClient.invalidateQueries({
				queryKey: [QUERY_KEYS.game, gameIdParam],
			});
			successToast("expansions:delete.successfull");
			onDeleteExpansionSuccess?.();
		} catch {
			errorToast("expansions:delete.failed");
		}
	};

	return {
		isLoading,
		game,
		deleteGame,
		settings,
		statistics,
		sessions,
		price,
		refreshPrice: () => refreshPriceMutation.mutate(),
		isRefreshingPrice: refreshPriceMutation.isPending,
		createWatch: (url: string) => createWatchMutation.mutateAsync(url),
		isCreatingWatch: createWatchMutation.isPending,
		deleteExpansion,
	};
};
