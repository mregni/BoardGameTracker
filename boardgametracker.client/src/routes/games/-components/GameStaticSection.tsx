import { useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { formatDuration, intervalToDuration } from "date-fns";
import { useTranslation } from "react-i18next";
import Clock from "@/assets/icons/clock.svg?react";
import Coins from "@/assets/icons/coins.svg?react";
import Trophy from "@/assets/icons/trophy.svg?react";
import Users from "@/assets/icons/users.svg?react";
import { BgtBadge } from "@/components/BgtBadge/BgtBadge";
import { BgtFancyTextStatistic } from "@/components/BgtStatistic/BgtFancyTextStatistic";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { BgtText } from "@/components/BgtText/BgtText";
import type { Game } from "@/models";
import { toDisplay } from "@/utils/dateUtils";
import { BgtPoster } from "../../-components/BgtPoster";

interface Props {
	game: Game;
	playCount: number;
	currency: string;
	uiLanguage: string;
	dateFormat: string;
}

export const GameStaticSection = (props: Props) => {
	const { game, playCount, currency, uiLanguage, dateFormat } = props;
	const { t } = useTranslation(["common", "statistics"]);
	const navigate = useNavigate();

	return (
		<div className="flex flex-col lg:flex-row gap-6">
			<div className="aspect-square rounded-lg overflow-hidden w-48 mx-auto lg:mx-0">
				<BgtPoster title={game.title} image={game.image} />
			</div>
			<div className="flex flex-col flex-1 gap-2">
				<div className="flex flex-wrap gap-2">
					{game.categories.map((cat) => (
						<BgtBadge
							key={cat.id}
							color="primary"
							variant="soft"
							onClick={() =>
								navigate({
									to: "/games",
									search: () => ({ category: cat.name }),
								})
							}
						>
							{cat.name}
						</BgtBadge>
					))}
				</div>
				<div>
					<BgtText className={cx("xl:line-clamp-2 line-clamp-3 text-white/70")}>{game.description}</BgtText>
				</div>
				<div className="grid grid-cols-2 md:grid-cols-3 2xl:grid-cols-5 gap-3 xl:gap-6">
					<BgtTextStatistic content={`${game.minPlayers} - ${game.maxPlayers}`} title={t("players")} icon={<Users />} />
					<BgtTextStatistic
						content={`${game.minPlayTime} - ${game.maxPlayTime}`}
						title={t("duration")}
						suffix={t("minutes-abbreviation")}
						icon={<Clock />}
					/>
					<BgtTextStatistic content={playCount} title={t("statistics:play-count")} icon={<Trophy />} />
					<BgtTextStatistic
						content={game.buyingPrice}
						title={t("statistics:buy-price")}
						prefix={currency}
						icon={<Coins />}
					/>
					{game.additionDate && (
						<BgtFancyTextStatistic
							content={formatDuration(
								intervalToDuration({
									start: game.additionDate,
									end: new Date(),
								}),
								{
									format: ["months", "days"],
								},
							)}
							title={t("statistics:in-collection")}
							suffix={t("since", {
								date: toDisplay(game.additionDate, dateFormat, uiLanguage),
							})}
						/>
					)}
				</div>
			</div>
		</div>
	);
};
