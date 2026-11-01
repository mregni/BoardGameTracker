import type { ComponentType, SVGProps } from "react";
import { useTranslation } from "react-i18next";
import BarChart from "@/assets/icons/bar-chart.svg?react";
import Clock from "@/assets/icons/clock.svg?react";
import Users from "@/assets/icons/users.svg?react";
import { useDurationRange } from "@/hooks/useDurationRange";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { Game } from "@/models";
import { toRelative } from "@/utils/dateUtils";
import { formatRange } from "@/utils/rangeUtils";
import type { PlaySummary } from "../-utils/gamesSearch";

interface Props {
	game: Game;
	plays?: PlaySummary;
}

export const GameCardMeta = ({ game, plays }: Props) => {
	const { t, i18n } = useTranslation(["common", "games"]);
	const durationRange = useDurationRange();
	const { decimal } = useNumberFormat();

	const facts: { key: string; icon: ComponentType<SVGProps<SVGSVGElement>>; label: string; value: string | null }[] = [
		{ key: "players", icon: Users, label: t("common:players"), value: formatRange(game.minPlayers, game.maxPlayers) },
		{
			key: "time",
			icon: Clock,
			label: t("common:play-time"),
			value: durationRange(game.minPlayTime, game.maxPlayTime),
		},
		{
			key: "weight",
			icon: BarChart,
			label: t("common:weight"),
			value: game.weight != null && game.weight > 0 ? decimal(game.weight, 1, 1) : null,
		},
	];
	const shown = facts.filter((fact) => fact.value !== null);

	return (
		<div className="flex flex-col gap-0.5 min-w-0">
			{shown.length > 0 && (
				<ul className="flex flex-wrap items-center gap-x-2.5 gap-y-0.5 text-xs text-white/60 tabular-nums">
					{shown.map(({ key, icon: Icon, label, value }) => (
						<li key={key} className="inline-flex items-center gap-1">
							<Icon aria-hidden="true" className="size-3.5 shrink-0 text-white/40" />
							<span className="sr-only">{label}</span>
							{value}
						</li>
					))}
				</ul>
			)}
			{plays && (
				<p className="truncate text-xs text-white/50">
					{plays.playCount > 0
						? `${t("games:plays", { count: plays.playCount })} · ${toRelative(plays.lastPlayed, i18n.language)}`
						: t("games:unplayed")}
				</p>
			)}
		</div>
	);
};
