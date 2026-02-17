import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Sparkles from "@/assets/icons/sparkles.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import type { RecentGame } from "@/models";
import { useSettingsData } from "@/routes/settings/-hooks/useSettingsData";
import { toRelative } from "@/utils/dateUtils";
import i18n from "@/utils/i18n";

interface Props extends React.HTMLAttributes<HTMLDivElement> {
	games: RecentGame[];
}

export const RecentAddedGamesCard = ({ games, className }: Props) => {
	const { t } = useTranslation();

	return (
		<BgtCard title={t("dashboard.recent-added-games")} icon={Sparkles} className={className}>
			<div className="flex flex-col gap-3">
				{games.map((game) => (
					<GameCardItem key={game.id} game={game} />
				))}
			</div>
		</BgtCard>
	);
};

interface ItemProps {
	game: RecentGame;
}

const GameCardItem = (props: ItemProps) => {
	const { game } = props;
	const { settings } = useSettingsData({});

	if (settings === undefined) return null;

	return (
		<Link to="/games/$gameId" params={{ gameId: game.id }} className="flex items-center gap-4 bg-primary/5 rounded-lg p-4 border border-primary/10">
			<BgtAvatar
				image={game.image}
				title={game.title}
				size="large"
			/>
			<div className="flex-1">
				<BgtText color="white" className="text-white uppercase">
					{game.title}
				</BgtText>
				<BgtText color="white" opacity={50} size="2">
					{toRelative(game.additionDate, i18n.language)}
					{game.price !== null && game.price > 0 && (
						<span>
							{" "}
							• {settings.currency}
							{game.price}
						</span>
					)}
				</BgtText>
			</div>
		</Link>
	);
};
