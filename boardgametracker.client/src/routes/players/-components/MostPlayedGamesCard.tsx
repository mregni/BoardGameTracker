import { useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Trophy from "@/assets/icons/trophy.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import type { MostPlayedGame } from "@/models";

interface Props {
	games: MostPlayedGame[];
}

export const MostPlayedGamesCard = (props: Props) => {
	const { games } = props;
	const { t } = useTranslation();

	return (
		<BgtCard title={t("player.cards.most-played-games")} icon={Trophy}>
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
	const navigate = useNavigate();
	const { t } = useTranslation();

	return (
		<BgtCard>
			<div className="flex items-center gap-4">
				<div className="w-10 h-10 rounded-full overflow-hidden bg-primary/20 border border-primary/30 shrink-0">
					<BgtAvatar
						onClick={() => navigate({ to: `/games/${game.id}` })}
						image={game.image}
						title={game.title}
						size="large"
					/>
				</div>
				<div className="flex-1">
					<BgtText color="white">{game.title}</BgtText>
					<div className="text-white/50 text-sm">
						{t("common.sessions", { count: game.totalSessions ?? 0 })} •{" "}
						{t("common.win", { count: game.totalWins ?? 0 })}
					</div>
				</div>
				<div className="text-cyan-400 font-bold">{game.winningPercentage}%</div>
			</div>
		</BgtCard>
	);
};
