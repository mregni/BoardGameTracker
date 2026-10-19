import { Link } from "@tanstack/react-router";
import { cva, cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { useDurationRange } from "@/hooks/useDurationRange";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { Shame, ShameGameNight } from "@/models";
import { toDisplay, toMonthYear, toRelative } from "@/utils/dateUtils";
import { EMPTY_PRICE } from "@/utils/numberUtils";
import { formatRange } from "@/utils/rangeUtils";
import { lastPlayedTone } from "../-utils/shameFilters";

export const shameGridClasses =
	"grid grid-cols-[2.75rem_minmax(0,1fr)_auto] gap-x-3 md:grid-cols-[2.75rem_minmax(0,1fr)_9rem_5rem_14rem]";

export const pillVariants = cva(
	"inline-block max-w-full truncate rounded-full px-2.5 py-0.5 text-xs font-semibold whitespace-nowrap",
	{
		variants: {
			tone: {
				bad: "bg-bad/15 text-bad",
				warn: "bg-warn/15 text-warn",
				neutral: "bg-white/5 text-white/60",
				ok: "bg-ok/15 text-ok",
			},
		},
	},
);

const actionClasses =
	"inline-flex items-center justify-center rounded-md border border-white/15 bg-white/5 px-2.5 py-1 text-xs font-semibold whitespace-nowrap text-white transition-colors hover:border-primary/60 hover:bg-primary/20 disabled:cursor-not-allowed disabled:opacity-50";

interface Props {
	shame: Shame;
	currency: string;
	dateFormat: string;
	canWrite: boolean;
	nextNight: ShameGameNight | null;
	planning: boolean;
	onPlan: (shame: Shame, night: ShameGameNight) => void;
	now?: Date;
}

export const ShameRow = ({
	shame,
	currency,
	dateFormat,
	canWrite,
	nextNight,
	planning,
	onPlan,
	now = new Date(),
}: Props) => {
	const { t, i18n } = useTranslation("shames");
	const format = useNumberFormat();
	const durationRange = useDurationRange();
	const tone = lastPlayedTone(shame, now);
	const lastPlayed = tone === "bad" ? t("row.never-played") : toRelative(shame.lastSessionDate, i18n.language, { now });

	const details: string[] = [];
	if (shame.additionDate) {
		details.push(t("row.owned-since", { date: toMonthYear(shame.additionDate, i18n.language) }));
	}
	if (shame.playCount > 0) {
		details.push(t("row.plays", { count: shame.playCount }));
	} else {
		const players = formatRange(shame.minPlayers, shame.maxPlayers);
		const duration = durationRange(shame.minPlayTime, shame.maxPlayTime);
		if (players) details.push(t("row.players", { count: shame.maxPlayers ?? shame.minPlayers ?? 0, range: players }));
		if (duration) details.push(duration);
	}
	const detailText = details.join(" · ");

	const planned = shame.plannedFor !== null;

	return (
		<li
			className={cx(
				shameGridClasses,
				"items-center gap-y-1.5 rounded-lg border bg-card px-3 py-2",
				"[grid-template-areas:'av_name_price'_'av_last_act'] md:[grid-template-areas:'av_name_last_price_act']",
				planned ? "border-ok/45" : "border-card-border",
			)}
		>
			<div className="[grid-area:av]">
				<BgtAvatar size="large" title={shame.title} image={shame.image} />
			</div>
			<div className="min-w-0 [grid-area:name]">
				<Link
					to="/games/$gameId"
					params={{ gameId: shame.id }}
					className="inline-flex max-w-full items-center gap-1 font-semibold text-white hover:text-primary hover:underline"
				>
					<span className="line-clamp-2 md:truncate">{shame.title}</span>
					<CaretRight aria-hidden="true" className="size-3.5 shrink-0 text-white/50" />
				</Link>
				{detailText && (
					<small className="line-clamp-2 text-xs text-white/50 md:block md:truncate" title={detailText}>
						{detailText}
					</small>
				)}
			</div>
			<div className="min-w-0 justify-self-start [grid-area:last]">
				<span className={pillVariants({ tone })}>{lastPlayed}</span>
			</div>
			<div className="text-right font-bold tabular-nums [grid-area:price]">
				{shame.price === null ? (
					<span className="font-normal text-white/50" title={t("row.no-price")}>
						<span aria-hidden="true">{EMPTY_PRICE}</span>
						<span className="sr-only">{t("row.no-price")}</span>
					</span>
				) : (
					<span className="text-card-value">{format.price(shame.price, currency)}</span>
				)}
			</div>
			<div className="flex min-w-0 justify-end gap-1.5 [grid-area:act]">
				{planned && shame.plannedFor && (
					<span className={pillVariants({ tone: "ok" })}>
						{t("row.planned", { date: toDisplay(shame.plannedFor, dateFormat, i18n.language) })}
					</span>
				)}
				{!planned && canWrite && nextNight && (
					<button
						type="button"
						className={actionClasses}
						disabled={planning}
						aria-label={t("row.plan-label", { title: shame.title, night: nextNight.title })}
						onClick={() => onPlan(shame, nextNight)}
					>
						{t("row.plan")}
					</button>
				)}
				{!planned && canWrite && (
					<Link
						to="/sessions/new/$gameId"
						params={{ gameId: shame.id }}
						className={actionClasses}
						aria-label={t("row.log-play-label", { title: shame.title })}
					>
						{t("row.log-play")}
					</Link>
				)}
			</div>
		</li>
	);
};
