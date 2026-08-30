import { useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { formatDistanceToNow, formatDuration, intervalToDuration } from "date-fns";
import { useTranslation } from "react-i18next";
import Clock from "@/assets/icons/clock.svg?react";
import Coins from "@/assets/icons/coins.svg?react";
import List from "@/assets/icons/list.svg?react";
import Package from "@/assets/icons/package.svg?react";
import Refresh from "@/assets/icons/refresh.svg?react";
import Trophy from "@/assets/icons/trophy.svg?react";
import Users from "@/assets/icons/users.svg?react";
import { BgtBadge } from "@/components/BgtBadge/BgtBadge";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtFancyTextStatistic } from "@/components/BgtStatistic/BgtFancyTextStatistic";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { BgtText } from "@/components/BgtText/BgtText";
import type { Game, GamePrice } from "@/models";
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
	price?: GamePrice;
	onRefreshPrice?: () => void;
	isRefreshingPrice?: boolean;
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
		price,
		onRefreshPrice,
		isRefreshingPrice,
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
					{price?.available && (
						<BgtTextStatistic
							content={price.price ?? "-"}
							title={t("game:current-price.title")}
							prefix={price.price != null ? currency : undefined}
							icon={<Coins />}
						/>
					)}
					{price?.available && price.inStock != null && (
						<BgtTextStatistic
							content={price.inStock ? t("game:in-stock.yes") : t("game:in-stock.no")}
							title={t("game:in-stock.title")}
							icon={<Package />}
						/>
					)}
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
				{game.changeDetectionWatchId && onRefreshPrice && (
					<div className="flex items-center gap-3">
						<BgtButton variant="cancel" size="1" disabled={isRefreshingPrice} onClick={onRefreshPrice}>
							<Refresh className={cx("size-4", isRefreshingPrice && "animate-spin")} />
							{t("game:price.refresh")}
						</BgtButton>
						{price?.available && price.fetchedAt && (
							<BgtText size="1" className="text-white/50">
								{t("game:price.updated", {
									time: formatDistanceToNow(new Date(price.fetchedAt), { addSuffix: true }),
								})}
							</BgtText>
						)}
					</div>
				)}
			</div>
		</div>
	);
};
