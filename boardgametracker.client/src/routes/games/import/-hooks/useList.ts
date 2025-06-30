import { useCallback, useEffect, useMemo, useState } from 'react';
import { useMutation, useQueries, useQueryClient } from '@tanstack/react-query';

import { getSettings } from '@/services/queries/settings';
import { getBggCollection, getGames } from '@/services/queries/games';
import { importGamesCall } from '@/services/gameService';
import { ImportGame, QUERY_KEYS } from '@/models';

interface Props {
  username: string;
  onSuccessImport?: () => void;
  onFailedImport?: () => void;
}

export const useList = ({ username, onSuccessImport, onFailedImport }: Props) => {
  const queryClient = useQueryClient();

  const [bggCollectionQuery, gamesQuery, settingsQuery] = useQueries({
    queries: [getBggCollection(username), getGames(), getSettings()],
  });

  const settings = useMemo(() => settingsQuery.data, [settingsQuery.data]);
  const statusCode = bggCollectionQuery.data?.statusCode ?? 202;

  const [games, setGames] = useState<ImportGame[]>([]);
  const [filterCollected, setFilterCollected] = useState<boolean>(true);
  const [inCollectionCount, setInCollectionCount] = useState<number>(0);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [processingGames, setProcessingGames] = useState<boolean>(true);

  const originalGames = useMemo(() => {
    setProcessingGames(true);
    const games = bggCollectionQuery.data?.games;
    const collectionGames = gamesQuery.data;

    if (!games) return [];

    setTotalCount(games.length);
    if (!collectionGames) {
      return games.map((game) => ({ ...game, inCollection: false }));
    }

    const collectionBggIds = new Set(collectionGames.map((game) => game.bggId));
    const processedGames = games.map((game) => {
      const existingGame = collectionGames.find((g) => g.bggId === game.bggId);
      return {
        ...game,
        inCollection: collectionBggIds.has(game.bggId),
        price: existingGame?.buyingPrice ?? 0,
        addedDate: existingGame?.additionDate ? new Date(existingGame.additionDate) : new Date(game.lastModified),
        hasScoring: existingGame?.hasScoring ?? true,
        checked: false,
      };
    });

    setInCollectionCount(processedGames.filter((game) => game.inCollection).length);

    setProcessingGames(false);
    if (filterCollected) {
      return processedGames.filter((game) => !game.inCollection);
    }
    return processedGames;
  }, [bggCollectionQuery.data?.games, gamesQuery.data, filterCollected]);

  useEffect(() => {
    setGames(originalGames);
  }, [originalGames]);

  const updateGame = useCallback((bggId: number, updates: Partial<ImportGame>) => {
    setGames((prev) => prev.map((game) => (game.bggId === bggId ? { ...game, ...updates } : game)));
  }, []);

  const startImportMutation = useMutation({
    mutationFn: importGamesCall,
    async onSuccess() {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
      queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });

      onSuccessImport?.();
    },
    onError() {
      onFailedImport?.();
    },
  });

  return {
    games,
    settings,
    statusCode,
    updateGame,
    filterCollected,
    setFilterCollected,
    inCollectionCount,
    processingGames,
    totalCount,
    startImport: startImportMutation.mutateAsync,
    importing: startImportMutation.isPending,
  };
};
