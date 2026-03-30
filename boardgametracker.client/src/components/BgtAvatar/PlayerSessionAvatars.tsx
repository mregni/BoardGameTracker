import type { Game, Player, PlayerSession } from "@/models";

import { BgtPlayerAvatar } from "./BgtPlayerAvatar";

interface PlayerSessionAvatarsProps {
	playerSessions: PlayerSession[];
	players: Player[];
	game?: Game;
	games?: Game[];
	gameId?: number;
	won: boolean;
}

export const PlayerSessionAvatars = ({
	playerSessions,
	players,
	game,
	games,
	gameId,
	won,
}: PlayerSessionAvatarsProps) => {
	const resolvedGame = game ?? games?.find((x) => x.id === gameId);
	const filtered = playerSessions.filter((x) => x.won === won);

	return (
		<div className="flex flex-row gap-1">
			{filtered.map((player) => (
				<BgtPlayerAvatar
					key={`${player.playerId}_${player.sessionId}`}
					player={players.find((x) => x.id === player.playerId)}
					playerSession={player}
					game={resolvedGame}
				/>
			))}
		</div>
	);
};
