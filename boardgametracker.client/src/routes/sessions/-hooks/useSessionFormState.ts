import { useCallback, useEffect, useReducer, useRef } from 'react';
import { addMinutes } from 'date-fns';
import { UseFormReturn } from '@tanstack/react-form';

import type { CreateSession, CreateSessionPlayer, CreatePlayerSessionNoScoring, Expansion, Game } from '@/models';

interface UseSessionFormStateProps {
  form: UseFormReturn<CreateSession>;
  games: Game[];
  initialGameId?: number;
  initialExpansions?: Expansion[];
  initialPlayerSessions?: (CreateSessionPlayer | CreatePlayerSessionNoScoring)[];
}

type PlayerType = CreateSessionPlayer | CreatePlayerSessionNoScoring;

interface SessionFormState {
  selectedGameId: number;
  expansionList: Expansion[];
  selectedExpansionIds: number[];
  players: PlayerType[];
  playerModal: { type: 'create' | 'update' | null; playerIdToEdit: number | null };
}

type SessionFormAction =
  | { type: 'SET_GAME'; gameId: number; expansionList: Expansion[] }
  | { type: 'SET_SELECTED_EXPANSIONS'; expansionIds: number[] }
  | { type: 'ADD_PLAYER'; player: PlayerType }
  | { type: 'UPDATE_PLAYER'; player: PlayerType }
  | { type: 'REMOVE_PLAYER'; index: number }
  | { type: 'OPEN_CREATE_MODAL' }
  | { type: 'OPEN_UPDATE_MODAL'; playerId: number }
  | { type: 'CLOSE_MODAL' };

function sessionFormReducer(state: SessionFormState, action: SessionFormAction): SessionFormState {
  switch (action.type) {
    case 'SET_GAME':
      return { ...state, selectedGameId: action.gameId, expansionList: action.expansionList };
    case 'SET_SELECTED_EXPANSIONS':
      return { ...state, selectedExpansionIds: action.expansionIds };
    case 'ADD_PLAYER':
      return {
        ...state,
        players: [...state.players, action.player],
        playerModal: { type: null, playerIdToEdit: null },
      };
    case 'UPDATE_PLAYER': {
      const index = state.players.findIndex((x) => x.playerId === action.player.playerId);
      if (index === -1) return { ...state, playerModal: { type: null, playerIdToEdit: null } };
      const players = [...state.players];
      players[index] = action.player;
      return { ...state, players, playerModal: { type: null, playerIdToEdit: null } };
    }
    case 'REMOVE_PLAYER':
      return { ...state, players: state.players.filter((_, i) => i !== action.index) };
    case 'OPEN_CREATE_MODAL':
      return { ...state, playerModal: { type: 'create', playerIdToEdit: null } };
    case 'OPEN_UPDATE_MODAL':
      return { ...state, playerModal: { type: 'update', playerIdToEdit: action.playerId } };
    case 'CLOSE_MODAL':
      return { ...state, playerModal: { type: null, playerIdToEdit: null } };
  }
}

export const useSessionFormState = ({
  form,
  games,
  initialGameId,
  initialExpansions = [],
  initialPlayerSessions = [],
}: UseSessionFormStateProps) => {
  const [state, dispatch] = useReducer(sessionFormReducer, {
    selectedGameId: initialGameId ?? 0,
    expansionList: [],
    selectedExpansionIds: initialExpansions.map((x) => x.id),
    players: initialPlayerSessions,
    playerModal: { type: null, playerIdToEdit: null },
  });

  // Track latest selectedGameId in a ref to avoid stale closures in the subscription
  const selectedGameIdRef = useRef(state.selectedGameId);
  selectedGameIdRef.current = state.selectedGameId;

  // Subscribe to game changes and update form accordingly
  useEffect(() => {
    const subscription = form.store.subscribe(() => {
      const newGameId = form.store.state.values.gameId;
      if (newGameId !== selectedGameIdRef.current) {
        const selectedBoardGame = games.find((g) => g.id === newGameId);
        if (selectedBoardGame) {
          form.setFieldValue('minutes', selectedBoardGame.maxPlayTime ?? 30);
          form.setFieldValue('start', addMinutes(new Date(), -(selectedBoardGame?.maxPlayTime ?? 30)));
          dispatch({ type: 'SET_GAME', gameId: newGameId, expansionList: selectedBoardGame.expansions });
        }
      }
    });
    return () => subscription();
  }, [form, games]);

  const addPlayer = useCallback((player: PlayerType) => {
    dispatch({ type: 'ADD_PLAYER', player });
  }, []);

  const updatePlayer = useCallback((player: PlayerType) => {
    dispatch({ type: 'UPDATE_PLAYER', player });
  }, []);

  const removePlayer = useCallback((index: number) => {
    dispatch({ type: 'REMOVE_PLAYER', index });
  }, []);

  const openEditPlayerModal = useCallback((playerId: number) => {
    dispatch({ type: 'OPEN_UPDATE_MODAL', playerId });
  }, []);

  const openCreateModal = useCallback(() => {
    dispatch({ type: 'OPEN_CREATE_MODAL' });
  }, []);

  const closeModal = useCallback(() => {
    dispatch({ type: 'CLOSE_MODAL' });
  }, []);

  const setSelectedExpansionIds = useCallback((expansionIds: number[]) => {
    dispatch({ type: 'SET_SELECTED_EXPANSIONS', expansionIds });
  }, []);

  return {
    selectedGameId: state.selectedGameId,
    expansionList: state.expansionList,
    selectedExpansionIds: state.selectedExpansionIds,
    setSelectedExpansionIds,

    players: state.players,
    addPlayer,
    updatePlayer,
    removePlayer,
    openEditPlayerModal,

    isCreateModalOpen: state.playerModal.type === 'create',
    isUpdateModalOpen: state.playerModal.type === 'update',
    openCreateModal,
    closeModal,
    playerIdToEdit: state.playerModal.playerIdToEdit,
  };
};
