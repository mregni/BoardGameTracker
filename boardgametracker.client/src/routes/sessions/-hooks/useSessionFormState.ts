import { useCallback, useRef, useState } from "react";

import type { CreateSessionPlayer, Expansion, Game } from "@/models";
import { applyScoreWinners, winnersMatchScores } from "../-utils/sessionWinners";

type PlayersUpdater = (players: CreateSessionPlayer[]) => CreateSessionPlayer[];

interface UseSessionFormStateProps {
	selectedGameId: number;
	games: Game[];
	initialGame?: Game;
	initialExpansions?: Expansion[];
	initialPlayers: CreateSessionPlayer[];
	setPlayers: (updater: PlayersUpdater) => void;
}

const updatePlayer = (
	players: CreateSessionPlayer[],
	playerId: number,
	update: (player: CreateSessionPlayer) => CreateSessionPlayer,
) => players.map((player) => (player.playerId === playerId ? update(player) : player));

export const useSessionFormState = ({
	selectedGameId,
	games,
	initialGame,
	initialExpansions = [],
	initialPlayers,
	setPlayers,
}: UseSessionFormStateProps) => {
	const [expansionSelection, setExpansionSelection] = useState(() => ({
		gameId: initialGame?.id ?? 0,
		ids: initialExpansions.map((x) => x.id),
	}));
	const winnersFromScores = useRef(winnersMatchScores(initialPlayers));

	const changePlayers = useCallback(
		(updater: PlayersUpdater) => {
			setPlayers((players) => {
				const next = updater(players);
				return winnersFromScores.current ? applyScoreWinners(next) : next;
			});
		},
		[setPlayers],
	);

	const addPlayer = useCallback(
		(playerId: number) => {
			changePlayers((players) =>
				players.some((x) => x.playerId === playerId)
					? players
					: [...players, { playerId, won: false, firstPlay: false, score: null }],
			);
		},
		[changePlayers],
	);

	const removePlayer = useCallback(
		(playerId: number) => {
			changePlayers((players) => players.filter((x) => x.playerId !== playerId));
		},
		[changePlayers],
	);

	const setScore = useCallback(
		(playerId: number, score: number | null) => {
			changePlayers((players) => updatePlayer(players, playerId, (player) => ({ ...player, score })));
		},
		[changePlayers],
	);

	const toggleWon = useCallback(
		(playerId: number) => {
			winnersFromScores.current = false;
			changePlayers((players) => updatePlayer(players, playerId, (player) => ({ ...player, won: !player.won })));
		},
		[changePlayers],
	);

	const toggleFirstPlay = useCallback(
		(playerId: number) => {
			changePlayers((players) =>
				updatePlayer(players, playerId, (player) => ({ ...player, firstPlay: !player.firstPlay })),
			);
		},
		[changePlayers],
	);

	const setSelectedExpansionIds = useCallback(
		(ids: number[]) => setExpansionSelection({ gameId: selectedGameId, ids }),
		[selectedGameId],
	);

	const selectedGame =
		games.find((g) => g.id === selectedGameId) ?? (initialGame?.id === selectedGameId ? initialGame : undefined);

	return {
		hasScoring: selectedGame?.hasScoring ?? true,
		expansionList: selectedGame?.expansions ?? [],
		selectedExpansionIds: expansionSelection.gameId === selectedGameId ? expansionSelection.ids : [],
		setSelectedExpansionIds,
		addPlayer,
		removePlayer,
		setScore,
		toggleWon,
		toggleFirstPlay,
	};
};
