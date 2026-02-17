import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Target from "@/assets/icons/target.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import type { MostPlayedGame } from "@/models";

interface Props extends React.HTMLAttributes<HTMLDivElement> {
	games: MostPlayedGame[];
}

export const MostPlayedDashboardGamesCard = (props: Props) => {
	const { games, className } = props;
	const { t } = useTranslation();

	return (
		<BgtCard title={t("player.cards.most-played-games")} icon={Target} className={className}>
			<div className="flex flex-col gap-3">
				{games.map((game) => (
					<MostPlayedGameItem key={game.id} game={game} />
				))}
			</div>
		</BgtCard>
	);
};

interface MostPlayedGameItemProps {
	game: MostPlayedGame;
}

const MostPlayedGameItem = ({ game }: MostPlayedGameItemProps) => {
	const { t } = useTranslation();

	return (
		<Link to="/games/$gameId" params={{ gameId: game.id }}>
			<BgtCard className="cursor-pointer p-3">
				<div className="flex items-center gap-4">
					<div className="w-10 h-10 rounded-full overflow-hidden bg-primary/20 border border-primary/30 shrink-0">
						<BgtAvatar
							image={game.image}
							title={game.title}
							size="large"
						/>
					</div>
					<div className="flex-1">
						<BgtText color="white">{game.title}</BgtText>
						<BgtText color="white" size="2" opacity={50}>
							{t("common.sessions", { count: game.totalSessions })}
						</BgtText>
					</div>
				</div>
			</BgtCard>
		</Link>
	);
};
