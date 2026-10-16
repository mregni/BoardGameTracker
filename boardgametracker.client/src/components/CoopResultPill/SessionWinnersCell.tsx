import { PlayerSessionAvatars } from "@/components/BgtAvatar/PlayerSessionAvatars";
import type { Game, Player, Session } from "@/models";
import { CoopResultPill } from "./CoopResultPill";

interface Props {
	session: Pick<Session, "isCoop" | "teamWon" | "playerSessions" | "gameId">;
	players: Player[];
	game?: Game;
	games?: Game[];
}

export const SessionWinnersCell = ({ session, players, game, games }: Props) => {
	if (session.isCoop) {
		return <CoopResultPill won={session.teamWon === true} />;
	}

	return (
		<PlayerSessionAvatars
			playerSessions={session.playerSessions}
			players={players}
			game={game}
			games={games}
			gameId={session.gameId}
			won={true}
		/>
	);
};
