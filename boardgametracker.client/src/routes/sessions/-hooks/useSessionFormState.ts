import { useCallback, useEffect, useState } from 'react';
import { addMinutes } from 'date-fns';
import { UseFormReturn } from '@tanstack/react-form';

import { CreateSession, CreateSessionPlayer, CreatePlayerSessionNoScoring, Expansion, Game } from '@/models';

interface UseSessionFormStateProps {
  form: UseFormReturn<CreateSession>;
  games: Game[];
  initialGameId?: number;
  initialExpansions?: Expansion[];
  initialPlayerSessions?: (CreateSessionPlayer | CreatePlayerSessionNoScoring)[];
}

export const useSessionFormState = ({
  form,
  games,
  initialGameId,
  initialExpansions = [],
  initialPlayerSessions = [],
}: UseSessionFormStateProps) => {
  const [selectedGameId, setSelectedGameId] = useState<number>(initialGameId ?? 0);
  const [expansionList, setExpansionList] = useState<Expansion[]>([]);
  const [selectedExpansionIds, setSelectedExpansionIds] = useState<number[]>(initialExpansions.map((x) => x.id));
  const [players, setPlayers] = useState<(CreateSessionPlayer | CreatePlayerSessionNoScoring)[]>(initialPlayerSessions);

  // Modal state
  const [openCreatePlayerModal, setOpenCreatePlayerModal] = useState(false);
  const [openUpdatePlayerModal, setOpenUpdatePlayerModal] = useState(false);
  const [playerIdToEdit, setPlayerIdToEdit] = useState<number | null>(null);

  // Subscribe to game changes and update form accordingly
  useEffect(() => {
    const subscription = form.store.subscribe(() => {
      const newGameId = form.store.state.values.gameId;
      if (newGameId !== selectedGameId) {
        setSelectedGameId(newGameId);

        const selectedBoardGame = games.find((g) => g.id === newGameId);
        if (selectedBoardGame) {
          form.setFieldValue('minutes', selectedBoardGame.maxPlayTime ?? 30);
          form.setFieldValue('start', addMinutes(new Date(), -(selectedBoardGame?.maxPlayTime ?? 30)));
          setExpansionList(selectedBoardGame.expansions);
        }
      }
    });
    return () => subscription();
  }, [form, games, selectedGameId]);

  // Player management callbacks
  const addPlayer = useCallback((player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => {
    setPlayers((prev) => [...prev, player]);
    setPlayerIdToEdit(null);
    setOpenCreatePlayerModal(false);
  }, []);

  const updatePlayer = useCallback((player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => {
    setPlayers((prev) => {
      const index = prev.findIndex((x) => x.playerId === player.playerId);
      if (index !== -1) {
        const updatedPlayers = [...prev];
        updatedPlayers[index] = player;
        return updatedPlayers;
      }
      return prev;
    });
    setPlayerIdToEdit(null);
    setOpenUpdatePlayerModal(false);
  }, []);

  const removePlayer = useCallback((index: number) => {
    setPlayers((prev) => prev.filter((_, i) => i !== index));
  }, []);

  const openEditPlayerModal = useCallback((playerId: number) => {
    setPlayerIdToEdit(playerId);
    setOpenUpdatePlayerModal(true);
  }, []);

  return {
    // Game and expansion state
    selectedGameId,
    expansionList,
    selectedExpansionIds,
    setSelectedExpansionIds,

    // Player state
    players,
    addPlayer,
    updatePlayer,
    removePlayer,
    openEditPlayerModal,

    // Modal state
    openCreatePlayerModal,
    setOpenCreatePlayerModal,
    openUpdatePlayerModal,
    setOpenUpdatePlayerModal,
    playerIdToEdit,
  };
};
