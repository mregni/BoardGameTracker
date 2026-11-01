import { Children, type ComponentType, type ReactNode, type SVGProps } from "react";
import { useTranslation } from "react-i18next";
import Award from "@/assets/icons/award.svg?react";
import BarChart from "@/assets/icons/bar-chart.svg?react";
import Calendar from "@/assets/icons/calendar.svg?react";
import Clock from "@/assets/icons/clock.svg?react";
import Coins from "@/assets/icons/coins.svg?react";
import Target from "@/assets/icons/target.svg?react";
import User from "@/assets/icons/user.svg?react";
import Users from "@/assets/icons/users.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { useDurationRange } from "@/hooks/useDurationRange";
import { type Game, type GamePrice, GameState } from "@/models";
import { toDisplay } from "@/utils/dateUtils";
import { formatDecimal, formatPriceOrEmpty } from "@/utils/numberUtils";
import { formatRange } from "@/utils/rangeUtils";
import { LivePrice } from "./LivePrice";
import { PriceRefreshButton } from "./PriceRefreshButton";

const isInCollection = (state: GameState) => state === GameState.Owned || state === GameState.ForTrade;

interface FactRowProps {
	icon: ComponentType<SVGProps<SVGSVGElement>>;
	label: string;
	children: ReactNode;
}

const FactRow = (props: FactRowProps) => {
	const { icon: Icon, label, children } = props;

	return (
		<div className="flex w-full items-center justify-between gap-3 py-2 border-b border-primary/10 last:border-b-0">
			<span className="flex shrink-0 items-center gap-2 text-stat-title">
				<Icon className="size-4 shrink-0" />
				<span className="truncate">{label}</span>
			</span>
			<span className="flex min-w-0 flex-wrap items-center justify-end gap-x-1 font-medium text-white/85 tabular-nums">
				{children}
			</span>
		</div>
	);
};

const FactGroup = ({ title, children }: { title: string; children: ReactNode }) => {
	if (Children.toArray(children).length === 0) return null;

	return <BgtCard title={title}>{children}</BgtCard>;
};

interface Props {
	game: Game;
	currency: string;
	uiLanguage: string;
	dateFormat: string;
	price?: GamePrice;
	onRefreshPrice?: () => void;
	isRefreshingPrice?: boolean;
	onTrackPrice?: () => void;
}

export const GameFactsCard = (props: Props) => {
	const { game, currency, uiLanguage, dateFormat, price, onRefreshPrice, isRefreshingPrice, onTrackPrice } = props;
	const { t } = useTranslation(["common", "statistics", "game", "games"]);

	const players = formatRange(game.minPlayers, game.maxPlayers);
	const durationRange = useDurationRange();
	const duration = durationRange(game.minPlayTime, game.maxPlayTime);

	return (
		<div className="flex flex-col gap-3 xl:gap-6">
			<FactGroup title={t("game:facts.about")}>
				{game.yearPublished != null && game.yearPublished !== 0 && (
					<FactRow icon={Calendar} label={t("game:facts.year")}>
						{game.yearPublished}
					</FactRow>
				)}
				{players !== null && (
					<FactRow icon={Users} label={t("players")}>
						{players}
					</FactRow>
				)}
				{duration !== null && (
					<FactRow icon={Clock} label={t("duration")}>
						{duration}
					</FactRow>
				)}
				{game.minAge != null && game.minAge > 0 && (
					<FactRow icon={User} label={t("game:facts.min-age")}>
						{game.minAge}+
					</FactRow>
				)}
				{game.rating != null && game.rating > 0 && (
					<FactRow icon={Award} label={t("rating")}>
						{formatDecimal(game.rating, uiLanguage)} / 10
					</FactRow>
				)}
				{game.weight != null && game.weight > 0 && (
					<FactRow icon={BarChart} label={t("weight")}>
						{formatDecimal(game.weight, uiLanguage)} / 5
					</FactRow>
				)}
			</FactGroup>
			<FactGroup title={t("game:facts.your-copy")}>
				{game.buyingPrice != null && (
					<FactRow icon={Coins} label={t("statistics:buy-price")}>
						{formatPriceOrEmpty(game.buyingPrice, currency, uiLanguage)}
					</FactRow>
				)}
				{!!game.changeDetectionWatchId && (
					<FactRow icon={Target} label={t("game:current-price.title")}>
						<LivePrice livePrice={price} currency={currency} uiLanguage={uiLanguage} />
						{price?.available && price.inStock != null && (
							<span className="text-xs font-normal text-white/70">
								{t(price.inStock ? "games:in-stock.yes" : "games:in-stock.no")}
							</span>
						)}
						{onRefreshPrice && <PriceRefreshButton onRefresh={onRefreshPrice} isRefreshing={!!isRefreshingPrice} />}
					</FactRow>
				)}
				{!!game.additionDate && (
					<FactRow
						icon={Calendar}
						label={t(isInCollection(game.state) ? "statistics:in-collection" : "statistics:added")}
					>
						{toDisplay(game.additionDate, dateFormat, uiLanguage)}
					</FactRow>
				)}
				{onTrackPrice && (
					<div className="py-1">
						<BgtButton variant="text" size="1" onClick={onTrackPrice} className="pointer-coarse:min-h-11">
							<Target className="size-4" />
							{t("game:track-price.button")}
						</BgtButton>
					</div>
				)}
			</FactGroup>
		</div>
	);
};
