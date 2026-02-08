import { useMemo } from 'react';
import { useMutation, useQueries, useQueryClient } from '@tanstack/react-query';

import { getSettings } from '@/services/queries/settings';
import { getPlayers } from '@/services/queries/players';
import { getLocations } from '@/services/queries/locations';
import { getGames } from '@/services/queries/games';
import { getGameNights, getGameNightStatistics } from '@/services/queries/gameNights';
import {
  createGameNightCall,
  deleteGameNightCall,
  updateGameNightCall,
  updateGameNightRsvpCall,
} from '@/services/gameNightService';
import { QUERY_KEYS } from '@/models';

interface Props {
  onCreateSuccess?: () => void;
  onCreateError?: () => void;
  onUpdateSuccess?: () => void;
  onUpdateError?: () => void;
  onDeleteSuccess?: () => void;
  onDeleteError?: () => void;
  onRsvpUpdateSuccess?: () => void;
  onRsvpUpdateError?: () => void;
}

export const useGameNightData = (props: Props = {}) => {
  const {
    onCreateSuccess,
    onCreateError,
    onUpdateSuccess,
    onUpdateError,
    onDeleteSuccess,
    onDeleteError,
    onRsvpUpdateSuccess,
    onRsvpUpdateError,
  } = props;

  const queryClient = useQueryClient();

  const [gameNightsQuery, statisticsQuery, settingsQuery, playersQuery, gamesQuery, locationsQuery] = useQueries({
    queries: [getGameNights(), getGameNightStatistics(), getSettings(), getPlayers(), getGames(), getLocations()],
  });

  const gameNights = useMemo(() => gameNightsQuery.data ?? [], [gameNightsQuery.data]);
  const statistics = useMemo(() => statisticsQuery.data, [statisticsQuery.data]);
  const settings = useMemo(() => settingsQuery.data, [settingsQuery.data]);
  const players = useMemo(() => playersQuery.data ?? [], [playersQuery.data]);
  const games = useMemo(() => gamesQuery.data ?? [], [gamesQuery.data]);
  const locations = useMemo(() => locationsQuery.data ?? [], [locationsQuery.data]);

  const invalidateQueries = () => {
    queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.gameNights] });
    queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.gameNightStatistics] });
  };

  const createMutation = useMutation({
    mutationFn: createGameNightCall,
    onSuccess() {
      onCreateSuccess?.();
      invalidateQueries();
    },
    onError: () => {
      onCreateError?.();
    },
  });

  const updateMutation = useMutation({
    mutationFn: updateGameNightCall,
    onSuccess() {
      onUpdateSuccess?.();
      invalidateQueries();
    },
    onError: () => {
      onUpdateError?.();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: deleteGameNightCall,
    onSuccess() {
      onDeleteSuccess?.();
      invalidateQueries();
    },
    onError: () => {
      onDeleteError?.();
    },
  });

  const rsvpMutation = useMutation({
    mutationFn: updateGameNightRsvpCall,
    onSuccess() {
      onRsvpUpdateSuccess?.();
      invalidateQueries();
    },
    onError: () => {
      onRsvpUpdateError?.();
    },
  });

  return {
    gameNights,
    statistics,
    settings,
    players,
    games,
    locations,
    isLoading:
      gameNightsQuery.isLoading ||
      statisticsQuery.isLoading ||
      settingsQuery.isLoading ||
      playersQuery.isLoading ||
      gamesQuery.isLoading ||
      locationsQuery.isLoading,
    createGameNight: createMutation.mutateAsync,
    updateGameNight: updateMutation.mutateAsync,
    deleteGameNight: deleteMutation.mutateAsync,
    updateRsvp: rsvpMutation.mutateAsync,
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isDeleting: deleteMutation.isPending,
  };
};
