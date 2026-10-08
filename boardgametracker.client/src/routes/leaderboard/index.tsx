import { useQuery } from "@tanstack/react-query";
import { createFileRoute, Link } from "@tanstack/react-router";
import type { ColumnDef } from "@tanstack/react-table";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import ClockIcon from "@/assets/icons/clock.svg?react";
import CrownIcon from "@/assets/icons/crown.svg?react";
import GamepadIcon from "@/assets/icons/gamepad.svg?react";
import TargetIcon from "@/assets/icons/target.svg?react";
import TrophyIcon from "@/assets/icons/trophy.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtDataTable } from "@/components/BgtTable/BgtDataTable";
import type { LeaderboardEntry } from "@/models";
import { getLeaderboard } from "@/services/queries/leaderboard";
import { LeaderboardHighlight } from "./-components/LeaderboardHighlight";

export const Route = createFileRoute("/leaderboard/")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getLeaderboard());
	},
});

const formatMinutes = (minutes: number, hourLabel: string, minuteLabel: string) => {
	const hours = Math.floor(minutes / 60);
	const rest = Math.round(minutes % 60);
	return hours > 0 ? `${hours}${hourLabel} ${rest}${minuteLabel}` : `${rest}${minuteLabel}`;
};

function RouteComponent() {
	const { t } = useTranslation(["leaderboard", "common"]);
	const { data: leaderboard, isLoading } = useQuery(getLeaderboard());

	const columns: ColumnDef<LeaderboardEntry>[] = useMemo(
		() => [
			{
				accessorKey: "rank",
				header: t("columns.rank"),
				cell: ({ row }) => <span className="font-semibold">{row.original.rank}</span>,
			},
			{
				accessorKey: "name",
				header: t("columns.player"),
				cell: ({ row }) => (
					<Link
						to="/players/$playerId"
						params={{ playerId: row.original.playerId }}
						className="flex items-center gap-2 hover:text-primary"
					>
						<BgtAvatar image={row.original.image} title={row.original.name} size="small" />
						<span>{row.original.name}</span>
					</Link>
				),
			},
			{ accessorKey: "winCount", header: t("columns.wins") },
			{ accessorKey: "playCount", header: t("columns.plays") },
			{
				accessorKey: "winPercentage",
				header: t("columns.win-rate"),
				cell: ({ row }) => `${row.original.winPercentage}%`,
			},
			{ accessorKey: "podiumCount", header: t("columns.podiums"), meta: { hideOnMobile: true } },
			{
				accessorKey: "minutesPlayed",
				header: t("columns.time-played"),
				cell: ({ row }) => formatMinutes(row.original.minutesPlayed, t("common:hour-short"), t("common:minute-short")),
				meta: { hideOnMobile: true },
			},
		],
		[t],
	);

	if (!isLoading && leaderboard && leaderboard.players.length === 0) {
		return (
			<BgtEmptyPage
				header={t("title")}
				icon={TrophyIcon}
				title={t("empty.title")}
				description={t("empty.description")}
			/>
		);
	}

	return (
		<BgtPage>
			<BgtPageHeader header={t("title")} icon={TrophyIcon} />
			<BgtPageContent isLoading={isLoading} data={{ leaderboard }}>
				{({ leaderboard }) => (
					<>
						<div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-3 xl:gap-6">
							<LeaderboardHighlight
								title={t("highlights.most-wins")}
								entry={leaderboard.mostWins}
								value={t("values.wins", { count: leaderboard.mostWins?.winCount ?? 0 })}
								emptyText={t("highlights.nobody-yet")}
								icon={CrownIcon}
							/>
							<LeaderboardHighlight
								title={t("highlights.most-plays")}
								entry={leaderboard.mostPlays}
								value={t("values.plays", { count: leaderboard.mostPlays?.playCount ?? 0 })}
								emptyText={t("highlights.nobody-yet")}
								icon={GamepadIcon}
							/>
							<LeaderboardHighlight
								title={t("highlights.best-win-rate")}
								entry={leaderboard.bestWinRate}
								value={`${leaderboard.bestWinRate?.winPercentage ?? 0}%`}
								emptyText={t("highlights.min-plays", { count: leaderboard.minimumPlaysForWinRate })}
								icon={TargetIcon}
							/>
							<LeaderboardHighlight
								title={t("highlights.most-time")}
								entry={leaderboard.mostTimePlayed}
								value={formatMinutes(
									leaderboard.mostTimePlayed?.minutesPlayed ?? 0,
									t("common:hour-short"),
									t("common:minute-short"),
								)}
								emptyText={t("highlights.nobody-yet")}
								icon={ClockIcon}
							/>
						</div>
						<BgtDataTable columns={columns} data={leaderboard.players} noDataMessage={t("empty.title")} />
					</>
				)}
			</BgtPageContent>
		</BgtPage>
	);
}
