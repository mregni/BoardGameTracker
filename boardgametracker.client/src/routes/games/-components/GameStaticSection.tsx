import { useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { formatDuration, intervalToDuration } from "date-fns";
import { useTranslation } from "react-i18next";
import Clock from "@/assets/icons/clock.svg?react";
import Coins from "@/assets/icons/coins.svg?react";
import List from "@/assets/icons/list.svg?react";
import Package from "@/assets/icons/package.svg?react";
import Trophy from "@/assets/icons/trophy.svg?react";
import Users from "@/assets/icons/users.svg?react";
import { BgtBadge } from "@/components/BgtBadge/BgtBadge";
import { BgtFancyTextStatistic } from "@/components/BgtStatistic/BgtFancyTextStatistic";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { BgtText } from "@/components/BgtText/BgtText";
import type { Game } from "@/models";
import { toDisplay } from "@/utils/dateUtils";
import { BgtPoster } from "../../-components/BgtPoster";
import { RulebookChatButton } from "./RulebookChatButton";

const formatMinMax = (min: number | null, max: number | null): string | null => {
	if (min == null && max == null) {
		return null;
	}
	if (min != null && max != null) {
		return `${min} - ${max}`;
	}
	return `${min ?? max}`;
};

interface Props {
	game: Game;
	playCount: number;
	currency: string;
	uiLanguage: string;
	dateFormat: string;
	manualCount: number;
	ragEnabled: boolean;
	onOpenManuals: () => void;
	onOpenExpansions: () => void;
}

export const GameStaticSection = (props: Props) => {
	const {
		game,
		playCount,
		currency,
		uiLanguage,
		dateFormat,
		manualCount,
		ragEnabled,
		onOpenManuals,
		onOpenExpansions,
	} = props;
	const { t } = useTranslation(["common", "statistics", "game"]);
	const navigate = useNavigate();

	const playersContent = formatMinMax(game.minPlayers, game.maxPlayers);
	const durationContent = formatMinMax(game.minPlayTime, game.maxPlayTime);

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
				{ragEnabled && (
					<div className="flex">
						<RulebookChatButton gameId={game.id} disabled={manualCount === 0} />
					</div>
				)}
				<div className="grid grid-cols-2 md:grid-cols-4 2xl:grid-cols-7 gap-3 xl:gap-6">
					{playersContent !== null && (
						<BgtTextStatistic content={playersContent} title={t("players")} icon={<Users />} />
					)}
					{durationContent !== null && (
						<BgtTextStatistic
							content={durationContent}
							title={t("duration")}
							suffix={t("minutes-abbreviation")}
							icon={<Clock />}
						/>
					)}
					<BgtTextStatistic content={playCount} title={t("statistics:play-count")} icon={<Trophy />} />
					<BgtTextStatistic
						content={game.buyingPrice}
						title={t("statistics:buy-price")}
						prefix={currency}
						icon={<Coins />}
					/>
					<BgtTextStatistic
						content={manualCount}
						title={t("game:manuals.title")}
						icon={<List />}
						onClick={onOpenManuals}
					/>
					<BgtTextStatistic
						content={game.expansions.length}
						title={t("game:expansions.title")}
						icon={<Package />}
						onClick={onOpenExpansions}
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
