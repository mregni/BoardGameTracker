import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { PlayerOverview } from "@/models";
import { toRelative } from "@/utils/dateUtils";
import type { PlayerSort } from "../-utils/playersList";

interface Props {
	player: PlayerOverview;
	sort: PlayerSort;
}

export const PlayerRow = ({ player, sort }: Props) => {
	const { t, i18n } = useTranslation("player");
	const { ratio } = useNumberFormat();

	const headline = {
		recent: { value: toRelative(player.lastPlayed, i18n.language) || "–", caption: t("list.last-played") },
		name: { value: ratio(player.winCount, player.playCount), caption: t("list.win-rate") },
		sessions: { value: player.playCount, caption: t("list.sessions", { count: player.playCount }) },
	}[sort];

	return (
		<li>
			<Link
				to="/players/$playerId"
				params={{ playerId: player.id }}
				className="grid grid-cols-[2.75rem_minmax(0,1fr)_auto_1rem] items-center gap-3 rounded-[10px] border border-card-border bg-card px-3 py-2.5 tabular-nums transition-colors hover:border-primary/60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
			>
				<span aria-hidden="true">
					<BgtAvatar image={player.image} title={player.name} size="large" />
				</span>
				<span className="min-w-0">
					<strong className="block truncate font-medium text-white">{player.name}</strong>
					<span className="block truncate text-xs text-white/70">
						{t("list.record", { wins: player.winCount, count: player.playCount })}
					</span>
				</span>
				<span className="text-right leading-tight">
					<b className="block text-sm text-card-value">{headline.value}</b>
					<span className="block text-xs text-white/70">{headline.caption}</span>
				</span>
				<CaretRight aria-hidden="true" className="size-4 text-white/50" />
			</Link>
		</li>
	);
};
