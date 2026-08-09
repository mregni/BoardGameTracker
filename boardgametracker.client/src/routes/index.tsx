import { useQuery } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Calendar from "@/assets/icons/calendar.svg?react";
import Coins from "@/assets/icons/coins.svg?react";
import Game from "@/assets/icons/gamepad.svg?react";
import Home from "@/assets/icons/home.svg?react";
import Players from "@/assets/icons/users.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { BgtText } from "@/components/BgtText/BgtText";
import { useAuth } from "@/hooks/useAuth";
import { getProfile } from "@/services/queries/auth";
import { getDashboardStatistics } from "@/services/queries/dashboard";
import { getPlayerStatistics } from "@/services/queries/players";
import { formatMinutesToDuration } from "@/utils/dateUtils";
import { GameStateChartCard } from "./-components/dashboard/GameStateChart";
import { MostPlayedDashboardGamesCard } from "./-components/dashboard/MostPlayedDashboardGames";
import { RecentActivityCard } from "./-components/dashboard/RecentActivity";
import { RecentAddedGamesCard } from "./-components/dashboard/RecentAddedGames";
import { TopPlayersCard } from "./-components/dashboard/TopPlayers";
import { useDashboardData } from "./-hooks/useDashboardData";
import { SessionCountChartCard } from "./games/-components/SessionCountChartCard";

export const Route = createFileRoute("/")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getDashboardStatistics());
	},
});

function RouteComponent() {
	const { statistics, settings } = useDashboardData();
	const navigate = useNavigate();
	const { t } = useTranslation(["statistics", "common", "dashboard"]);

	const authStatus = useAuth((s) => s.authStatus);
	const isAuthenticated = useAuth((s) => s.isAuthenticated);
	const username = useAuth((s) => s.user?.username);
	const canPersonalize = !!authStatus?.authEnabled && isAuthenticated;

	const { data: profile } = useQuery({ ...getProfile(), enabled: canPersonalize });
	const { data: personalStats } = useQuery({
		...getPlayerStatistics(profile?.playerId ?? 0),
		enabled: profile?.playerId != null,
	});

	if (statistics === undefined || settings === undefined) return null;

	if (statistics.totalGames === 0) {
		return (
			<BgtEmptyPage
				header={t("common:dashboard")}
				icon={Home}
				emptyIcon={Game}
				title={t("dashboard:empty.title")}
				description={t("dashboard:empty.description")}
				action={{
					label: t("dashboard:empty.button"),
					onClick: () => navigate({ to: "/games" }),
				}}
			/>
		);
	}

	const totalPlayedTime = formatMinutesToDuration(
		statistics.totalPlayedTime,
		["weeks", "days", "hours", "minutes"],
		settings?.uiLanguage,
	);

	const avgSessionTime = formatMinutesToDuration(statistics.avgSessionTime, ["hours", "minutes"], settings?.uiLanguage);

	return (
		<BgtPage>
			<BgtPageHeader header={t("common:dashboard")} icon={Home} />
			<BgtPageContent>
				{profile?.playerId != null && personalStats && (
					<BgtCard className="gap-1">
						<BgtText size="5" weight="bold" color="white">
							{t("dashboard:welcome-back-title", { name: username ?? "" })}
						</BgtText>
						<BgtText color="gray">{t("dashboard:welcome-back", { count: personalStats.playCount })}</BgtText>
					</BgtCard>
				)}
				<div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
					<BgtTextStatistic
						content={statistics.totalGames}
						title={t("game-count")}
						icon={<Game />}
						textSize="8"
						iconClassName="size-9"
						link="/games"
						addLink="/games/add"
						addLabel={t("common:add-game")}
					/>
					<BgtTextStatistic
						content={statistics.activePlayers}
						title={t("player-count")}
						icon={<Players />}
						textSize="8"
						iconClassName="size-9"
						link="/players"
						addLink="/players/new"
						addLabel={t("common:add-player")}
					/>
					<BgtTextStatistic
						content={statistics.sessionsPlayed}
						title={t("session-count")}
						icon={<Calendar />}
						textSize="8"
						iconClassName="size-9"
						addLink="/sessions/new"
						addLabel={t("common:new-session")}
					/>
					<BgtTextStatistic
						content={statistics.totalCollectionValue}
						title={t("collection-value")}
						prefix={settings.currency}
						icon={<Coins />}
						textSize="8"
						iconClassName="size-9"
					/>
				</div>
				<div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
					<BgtTextStatistic content={totalPlayedTime} title={t("total-playtime")} />
					<BgtTextStatistic
						content={Math.round(statistics.avgGamePrice ?? 0)}
						title={t("average-cost")}
						prefix={settings.currency}
					/>
					<BgtTextStatistic content={statistics.expansionsOwned} title={t("expansion-count")} />
					<BgtTextStatistic content={avgSessionTime} title={t("average-playtime")} />
				</div>
				<div className="grid grid-cols-1 xl:grid-cols-2 2xl:grid-cols-6 gap-4">
					<RecentActivityCard activities={statistics.recentActivities} className="col-span-1 2xl:col-span-2" />
					<MostPlayedDashboardGamesCard games={statistics.mostPlayedGames} className="col-span-1 2xl:col-span-2" />
					<GameStateChartCard data={statistics.collection} className="col-span-1 2xl:col-span-2" />
					<TopPlayersCard topPlayers={statistics.topPlayers} className="col-span-1 2xl:col-span-3" />
					<RecentAddedGamesCard games={statistics.recentAddedGames} className="col-span-1 2xl:col-span-3" />
					<SessionCountChartCard
						playByDayChart={statistics.sessionsByDayOfWeek}
						className="col-span-1 2xl:col-span-6"
					/>
				</div>
				<div className="grid grid-cols-1 "></div>
			</BgtPageContent>
		</BgtPage>
	);
}
