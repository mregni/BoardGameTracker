import { addMinutes } from "date-fns";
import { useCallback, useEffect, useReducer, useRef } from "react";

import type { CreateSessionPlayer, Expansion, Game } from "@/models";
import { applyScoreWinners, winnersMatchScores } from "../-utils/sessionWinners";

interface SessionFormApi {
	store: {
		subscribe: (listener: () => void) => { unsubscribe: () => void };
		state: { values: { gameId: number } };
	};
	setFieldValue: (field: "minutes" | "start", value: number | Date) => void;
}

interface InitialPlayerSession {
	playerId: number;
	won: boolean;
	firstPlay: boolean;
	score?: number | null;
}

interface UseSessionFormStateProps {
	form: SessionFormApi;
	games: Game[];
	initialGame?: Game;
	initialExpansions?: Expansion[];
	initialPlayerSessions?: InitialPlayerSession[];
}

interface SessionFormState {
	selectedGameId: number;
	selectedExpansionIds: number[];
	players: CreateSessionPlayer[];
	winnersFromScores: boolean;
}

type SessionFormAction =
	| { type: "SET_GAME"; gameId: number }
	| { type: "SET_SELECTED_EXPANSIONS"; expansionIds: number[] }
	| { type: "ADD_PLAYER"; playerId: number }
	| { type: "REMOVE_PLAYER"; playerId: number }
	| { type: "SET_SCORE"; playerId: number; score: number | null }
	| { type: "TOGGLE_WON"; playerId: number }
	| { type: "TOGGLE_FIRST_PLAY"; playerId: number };

const withWinners = (state: SessionFormState, players: CreateSessionPlayer[]): SessionFormState => ({
	...state,
	players: state.winnersFromScores ? applyScoreWinners(players) : players,
});

const updatePlayer = (
	players: CreateSessionPlayer[],
	playerId: number,
	update: (player: CreateSessionPlayer) => CreateSessionPlayer,
) => players.map((player) => (player.playerId === playerId ? update(player) : player));

function sessionFormReducer(state: SessionFormState, action: SessionFormAction): SessionFormState {
	switch (action.type) {
		case "SET_GAME":
			return { ...state, selectedGameId: action.gameId, selectedExpansionIds: [] };
		case "SET_SELECTED_EXPANSIONS":
			return { ...state, selectedExpansionIds: action.expansionIds };
		case "ADD_PLAYER":
			if (state.players.some((x) => x.playerId === action.playerId)) return state;
			return withWinners(state, [
				...state.players,
				{ playerId: action.playerId, won: false, firstPlay: false, score: null },
			]);
		case "REMOVE_PLAYER":
			return withWinners(
				state,
				state.players.filter((x) => x.playerId !== action.playerId),
			);
		case "SET_SCORE":
			return withWinners(
				state,
				updatePlayer(state.players, action.playerId, (player) => ({ ...player, score: action.score })),
			);
		case "TOGGLE_WON":
			return {
				...state,
				winnersFromScores: false,
				players: updatePlayer(state.players, action.playerId, (player) => ({ ...player, won: !player.won })),
			};
		case "TOGGLE_FIRST_PLAY":
			return {
				...state,
				players: updatePlayer(state.players, action.playerId, (player) => ({
					...player,
					firstPlay: !player.firstPlay,
				})),
			};
	}
}

const toSessionPlayers = (playerSessions: InitialPlayerSession[]): CreateSessionPlayer[] =>
	playerSessions.map((x) => ({ playerId: x.playerId, won: x.won, firstPlay: x.firstPlay, score: x.score ?? null }));

export const useSessionFormState = ({
	form,
	games,
	initialGame,
	initialExpansions = [],
	initialPlayerSessions = [],
}: UseSessionFormStateProps) => {
	const [state, dispatch] = useReducer(sessionFormReducer, initialPlayerSessions, (playerSessions) => {
		const players = toSessionPlayers(playerSessions);
		return {
			selectedGameId: initialGame?.id ?? 0,
			selectedExpansionIds: initialExpansions.map((x) => x.id),
			players,
			winnersFromScores: winnersMatchScores(players),
		};
	});

	const selectedGameIdRef = useRef(state.selectedGameId);
	selectedGameIdRef.current = state.selectedGameId;

	useEffect(() => {
		const subscription = form.store.subscribe(() => {
			const newGameId = form.store.state.values.gameId;
			if (newGameId !== selectedGameIdRef.current) {
				selectedGameIdRef.current = newGameId;

				const selectedBoardGame = games.find((g) => g.id === newGameId);
				if (selectedBoardGame) {
					form.setFieldValue("minutes", selectedBoardGame.maxPlayTime ?? 30);
					form.setFieldValue("start", addMinutes(new Date(), -(selectedBoardGame.maxPlayTime ?? 30)));
				}
				dispatch({ type: "SET_GAME", gameId: newGameId });
			}
		});
		return () => subscription.unsubscribe();
	}, [form, games]);

	const addPlayer = useCallback((playerId: number) => {
		dispatch({ type: "ADD_PLAYER", playerId });
	}, []);

	const removePlayer = useCallback((playerId: number) => {
		dispatch({ type: "REMOVE_PLAYER", playerId });
	}, []);

	const setScore = useCallback((playerId: number, score: number | null) => {
		dispatch({ type: "SET_SCORE", playerId, score });
	}, []);

	const toggleWon = useCallback((playerId: number) => {
		dispatch({ type: "TOGGLE_WON", playerId });
	}, []);

	const toggleFirstPlay = useCallback((playerId: number) => {
		dispatch({ type: "TOGGLE_FIRST_PLAY", playerId });
	}, []);

	const setSelectedExpansionIds = useCallback((expansionIds: number[]) => {
		dispatch({ type: "SET_SELECTED_EXPANSIONS", expansionIds });
	}, []);

	const selectedGame =
		games.find((g) => g.id === state.selectedGameId) ??
		(initialGame?.id === state.selectedGameId ? initialGame : undefined);
	const expansionList: Expansion[] = selectedGame?.expansions ?? [];

	return {
		selectedGameId: state.selectedGameId,
		hasScoring: selectedGame?.hasScoring ?? true,
		expansionList,
		selectedExpansionIds: state.selectedExpansionIds,
		setSelectedExpansionIds,

		players: state.players,
		winnersFromScores: state.winnersFromScores,
		addPlayer,
		removePlayer,
		setScore,
		toggleWon,
		toggleFirstPlay,
	};
};
