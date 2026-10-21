import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { Leaderboard, LeaderboardEntry } from "@/models";
import { EMPTY_NUMBER } from "@/utils/numberUtils";

interface Props {
	leaderboard: Leaderboard;
}

interface Tile {
	key: string;
	title: string;
	entry: LeaderboardEntry | null;
	value: (entry: LeaderboardEntry) => string;
	suffix?: string;
}

const Holder = ({ entry, suffix }: { entry: LeaderboardEntry; suffix?: string }) => (
	<span className="flex min-w-0 items-center gap-1.5">
		<BgtAvatar image={entry.image} title={entry.name} size="small" />
		<span className="truncate">
			{entry.name}
			{suffix && ` · ${suffix}`}
		</span>
	</span>
);

export const LeaderboardTiles = ({ leaderboard }: Props) => {
	const { t } = useTranslation("leaderboard");
	const format = useNumberFormat();
	const duration = useShortDuration();

	const tiles: Tile[] = [
		{
			key: "most-wins",
			title: t("highlights.most-wins"),
			entry: leaderboard.mostWins,
			value: (entry) => format.integer(entry.winCount),
		},
		{
			key: "most-sessions",
			title: t("highlights.most-sessions"),
			entry: leaderboard.mostPlays,
			value: (entry) => format.integer(entry.playCount),
		},
		{
			key: "best-win-rate",
			title: t("highlights.best-win-rate"),
			entry: leaderboard.bestWinRate,
			value: (entry) => format.percent(entry.winPercentage / 100),
			suffix: t("highlights.min-sessions", { count: leaderboard.minimumSessions }),
		},
		{
			key: "most-time",
			title: t("highlights.most-time"),
			entry: leaderboard.mostTimePlayed,
			value: (entry) => duration(entry.minutesPlayed),
		},
	];

	return (
		<div className="grid grid-cols-2 gap-3 xl:grid-cols-4 xl:gap-6">
			{tiles.map((tile) => (
				<BgtTextStatistic
					key={tile.key}
					title={tile.title}
					content={tile.entry ? tile.value(tile.entry) : EMPTY_NUMBER}
					link={tile.entry ? `/players/${tile.entry.playerId}` : undefined}
					caption={
						tile.entry ? (
							<Holder entry={tile.entry} suffix={tile.suffix} />
						) : (
							<span>
								{t("highlights.nobody-yet")}
								{tile.suffix && ` · ${tile.suffix}`}
							</span>
						)
					}
				/>
			))}
		</div>
	);
};
