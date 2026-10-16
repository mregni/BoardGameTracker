import { cx } from "class-variance-authority";
import type { ComponentType, ReactNode, SVGProps } from "react";
import { useTranslation } from "react-i18next";
import Award from "@/assets/icons/award.svg?react";
import BarChart from "@/assets/icons/bar-chart.svg?react";
import Calendar from "@/assets/icons/calendar.svg?react";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import Clock from "@/assets/icons/clock.svg?react";
import Coins from "@/assets/icons/coins.svg?react";
import List from "@/assets/icons/list.svg?react";
import Package from "@/assets/icons/package.svg?react";
import Target from "@/assets/icons/target.svg?react";
import User from "@/assets/icons/user.svg?react";
import Users from "@/assets/icons/users.svg?react";
import type { Game, GamePrice } from "@/models";
import { toDisplay } from "@/utils/dateUtils";
import { formatDecimal, formatPrice } from "@/utils/numberUtils";
import { formatRange } from "@/utils/rangeUtils";
import { LivePrice } from "./LivePrice";
import { PriceRefreshButton } from "./PriceRefreshButton";

interface FactRowProps {
	icon: ComponentType<SVGProps<SVGSVGElement>>;
	label: string;
	onClick?: () => void;
	children: ReactNode;
}

const FactRow = (props: FactRowProps) => {
	const { icon: Icon, label, onClick, children } = props;
	const className = cx(
		"flex w-full items-center justify-between gap-3 py-2 text-left border-b border-primary/10 last:border-b-0",
		onClick && "cursor-pointer hover:text-primary-text",
	);
	const content = (
		<>
			<span className="flex min-w-0 items-center gap-2 text-stat-title">
				<Icon className="size-4 shrink-0" />
				<span className="truncate">{label}</span>
			</span>
			<span className="flex shrink-0 items-center gap-1 font-semibold text-card-value">
				{children}
				{onClick && <CaretRight className="size-4 text-primary" />}
			</span>
		</>
	);

	if (onClick) {
		return (
			<button type="button" onClick={onClick} className={className}>
				{content}
			</button>
		);
	}

	return <div className={className}>{content}</div>;
};

interface Props {
	game: Game;
	currency: string;
	uiLanguage: string;
	dateFormat: string;
	manualCount: number;
	price?: GamePrice;
	onRefreshPrice?: () => void;
	isRefreshingPrice?: boolean;
	onOpenManuals: () => void;
	onOpenExpansions: () => void;
}

export const GameFactsCard = (props: Props) => {
	const {
		game,
		currency,
		uiLanguage,
		dateFormat,
		manualCount,
		price,
		onRefreshPrice,
		isRefreshingPrice,
		onOpenManuals,
		onOpenExpansions,
	} = props;
	const { t } = useTranslation(["common", "statistics", "game"]);

	const players = formatRange(game.minPlayers, game.maxPlayers);
	const duration = formatRange(game.minPlayTime, game.maxPlayTime);

	return (
		<div className="rounded-lg border border-primary/20 bg-primary/10 px-4 py-1">
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
					{duration} {t("minutes-abbreviation")}
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
			{game.buyingPrice != null && (
				<FactRow icon={Coins} label={t("statistics:buy-price")}>
					{formatPrice(game.buyingPrice, currency, uiLanguage)}
				</FactRow>
			)}
			{game.changeDetectionWatchId && (
				<FactRow icon={Target} label={t("game:current-price.title")}>
					<LivePrice livePrice={price} currency={currency} uiLanguage={uiLanguage} />
					{onRefreshPrice && <PriceRefreshButton onRefresh={onRefreshPrice} isRefreshing={!!isRefreshingPrice} />}
				</FactRow>
			)}
			{game.additionDate && (
				<FactRow icon={Calendar} label={t("statistics:in-collection")}>
					{toDisplay(game.additionDate, dateFormat, uiLanguage)}
				</FactRow>
			)}
			<FactRow icon={List} label={t("game:manuals.title")} onClick={onOpenManuals}>
				{manualCount}
			</FactRow>
			<FactRow icon={Package} label={t("game:expansions.title")} onClick={onOpenExpansions}>
				{game.expansions.length}
			</FactRow>
		</div>
	);
};
