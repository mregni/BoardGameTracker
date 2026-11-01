import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import Trophy from "@/assets/icons/trophy.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtNoData } from "@/components/BgtNoData/BgtNoData";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { MostPlayedGame } from "@/models";

interface Props {
	games: MostPlayedGame[];
}

export const MostPlayedGamesCard = (props: Props) => {
	const { games } = props;
	const { t } = useTranslation(["common", "player"]);

	return (
		<BgtCard title={t("player:cards.most-played-games")} icon={Trophy}>
			{games.length === 0 ? (
				<BgtNoData />
			) : (
				<ul className="-mt-2 flex flex-col divide-y divide-white/10">
					{games.map((game) => (
						<li key={game.id}>
							<MostPlayedGameItem game={game} />
						</li>
					))}
				</ul>
			)}
		</BgtCard>
	);
};

interface MostPlayedGameItemProps {
	game: MostPlayedGame;
}

const MostPlayedGameItem = ({ game }: MostPlayedGameItemProps) => {
	const { t } = useTranslation(["common", "player"]);
	const { percent } = useNumberFormat();

	return (
		<Link
			to="/games/$gameId"
			params={{ gameId: game.id }}
			className="-mx-2 flex items-center gap-3 rounded-md px-2 py-2 transition-colors hover:bg-primary/10"
		>
			<span aria-hidden="true">
				<BgtAvatar image={game.image} title={game.title} size="large" />
			</span>
			<span className="min-w-0 flex-1">
				<span className="block truncate font-semibold">{game.title}</span>
				<span className="block text-xs text-white/60">
					{t("sessions", { count: game.totalSessions ?? 0 })} · {t("win", { count: game.totalWins ?? 0 })}
				</span>
			</span>
			<span
				className={cx("shrink-0 font-bold tabular-nums", game.winningPercentage >= 50 ? "text-ok" : "text-white/70")}
			>
				{percent(game.winningPercentage / 100)}
			</span>
		</Link>
	);
};
