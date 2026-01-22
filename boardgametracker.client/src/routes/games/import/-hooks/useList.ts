import { useCallback, useEffect, useMemo, useState } from 'react';
import { useMutation, useQueries } from '@tanstack/react-query';

import { getSettings } from '@/services/queries/settings';
import { getBggCollection, getGames } from '@/services/queries/games';
import { importGamesCall } from '@/services/gameService';
import { ImportGame } from '@/models';
import { useQueryInvalidator } from '@/hooks/useQueryInvalidator';

interface Props {
  username: string;
  onSuccessImport?: () => void;
  onFailedImport?: () => void;
}

export const useList = ({ username, onSuccessImport, onFailedImport }: Props) => {
  const invalidator = useQueryInvalidator();

  const [bggCollectionQuery, gamesQuery, settingsQuery] = useQueries({
    queries: [getBggCollection(username), getGames(), getSettings()],
  });

  const settings = settingsQuery.data;
  const statusCode = bggCollectionQuery.data?.statusCode ?? 202;

  const [filterCollected, setFilterCollected] = useState<boolean>(true);

  // Derive all values during render instead of storing in state
  const processingGames = bggCollectionQuery.isLoading || gamesQuery.isLoading;

  const totalCount = useMemo(() => {
    return bggCollectionQuery.data?.games?.length ?? 0;
  }, [bggCollectionQuery.data?.games]);

  const processedGames = useMemo(() => {
    const bggGames = bggCollectionQuery.data?.games;
    const collectionGames = gamesQuery.data;

    if (!bggGames) return [];

    if (!collectionGames) {
      return bggGames.map((game) => ({ ...game, inCollection: false }));
    }

    const collectionBggIds = new Set(collectionGames.map((game) => game.bggId));
    return bggGames.map((game) => {
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
  }, [bggCollectionQuery.data?.games, gamesQuery.data]);

  const inCollectionCount = useMemo(() => {
    return processedGames.filter((game) => game.inCollection).length;
  }, [processedGames]);

  const derivedGames = useMemo(() => {
    if (filterCollected) {
      return processedGames.filter((game) => !game.inCollection);
    }
    return processedGames;
  }, [processedGames, filterCollected]);

  // Keep games in state to allow mutations via updateGame
  const [games, setGames] = useState<ImportGame[]>([]);

  // Sync state with derived value when dependencies change
  useEffect(() => {
    setGames(derivedGames);
  }, [derivedGames]);

  const updateGame = useCallback((bggId: number, updates: Partial<ImportGame>) => {
    setGames((prev) => prev.map((game) => (game.bggId === bggId ? { ...game, ...updates } : game)));
  }, []);

  const startImportMutation = useMutation({
    mutationFn: importGamesCall,
    async onSuccess() {
      // Use centralized invalidation for games
      await invalidator.invalidateGames();
      // Dashboard includes counts, so this is covered
      await invalidator.invalidateDashboard();

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
