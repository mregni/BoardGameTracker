import { noop } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Game from "@/assets/icons/gamepad.svg?react";
import Home from "@/assets/icons/home.svg?react";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { usePermissions } from "@/hooks/usePermissions";
import { getDashboardStatistics } from "@/services/queries/dashboard";
import { CollectionCard } from "./-components/dashboard/CollectionCard";
import { DashboardHeader } from "./-components/dashboard/DashboardHeader";
import { LastSessionsCard } from "./-components/dashboard/LastSessionsCard";
import { MostPlayedCard } from "./-components/dashboard/MostPlayedCard";
import { NextGameNightCard } from "./-components/dashboard/NextGameNightCard";
import { ShelfOfShameNudge } from "./-components/dashboard/ShelfOfShameNudge";
import { TopPlayersCard } from "./-components/dashboard/TopPlayersCard";
import { useDashboardData } from "./-hooks/useDashboardData";
import { useShortDuration } from "./-hooks/useShortDuration";

export const Route = createFileRoute("/")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.query(getDashboardStatistics()).catch(noop);
	},
});

function RouteComponent() {
	const { statistics, settings } = useDashboardData();
	const navigate = useNavigate();
	const { t } = useTranslation(["dashboard", "common"]);
	const { canWrite } = usePermissions();
	const shortDuration = useShortDuration();
	const { integer } = useNumberFormat();

	if (statistics === undefined || settings === undefined) return null;

	if (statistics.totalGames === 0) {
		return (
			<BgtEmptyPage
				header={t("common:dashboard")}
				icon={Home}
				emptyIcon={Game}
				title={t("empty.title")}
				description={t("empty.description")}
				action={{
					label: t("empty.button"),
					onClick: () => navigate({ to: "/games" }),
				}}
			/>
		);
	}

	return (
		<BgtPage>
			<BgtPageContent>
				<DashboardHeader
					linkedPlayer={statistics.linkedPlayer}
					sessionsThisMonth={statistics.sessionsThisMonth}
					canWrite={canWrite}
				/>
				{settings.gameNightsEnabled && (
					<NextGameNightCard
						gameNight={statistics.nextGameNight}
						plannedGameNights={statistics.plannedGameNights}
						uiLanguage={settings.uiLanguage}
						timeFormat={settings.timeFormat}
						canWrite={canWrite}
					/>
				)}
				<div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
					<BgtTextStatistic
						title={t("tiles.games")}
						content={statistics.totalGames}
						textSize="7"
						link="/games"
						caption={t("tiles.games-detail", {
							owned: integer(statistics.ownedGames),
							expansions: integer(statistics.expansionsOwned),
							count: statistics.expansionsOwned,
						})}
					/>
					<BgtTextStatistic
						title={t("tiles.players")}
						content={statistics.activePlayers}
						textSize="7"
						link="/players"
						caption={t("tiles.players-detail", {
							count: statistics.activePlayersThisMonth,
							value: integer(statistics.activePlayersThisMonth),
						})}
					/>
					<BgtTextStatistic
						title={t("tiles.sessions")}
						content={statistics.sessionsPlayed}
						textSize="7"
						caption={t("tiles.sessions-detail", {
							count: statistics.sessionsThisMonth,
							value: integer(statistics.sessionsThisMonth),
						})}
					/>
					<BgtTextStatistic
						title={t("tiles.play-time")}
						content={shortDuration(statistics.totalPlayedTime)}
						textSize="7"
						caption={
							statistics.sessionsPlayed > 0
								? t("tiles.play-time-detail", { duration: shortDuration(statistics.avgSessionTime) })
								: undefined
						}
					/>
				</div>
				<div className="grid grid-cols-1 items-start gap-4 xl:grid-cols-[minmax(0,1.35fr)_minmax(0,1fr)]">
					<div className="flex flex-col gap-4">
						<LastSessionsCard
							sessions={statistics.recentActivities}
							dateFormat={settings.dateFormat}
							uiLanguage={settings.uiLanguage}
						/>
						{settings.shelfOfShameEnabled && (
							<ShelfOfShameNudge shelfOfShame={statistics.shelfOfShame} currency={settings.currency} />
						)}
					</div>
					<div className="flex flex-col gap-4">
						<TopPlayersCard players={statistics.topPlayers} includesCoopWins={statistics.topPlayersIncludeCoopWins} />
						<MostPlayedCard allTime={statistics.mostPlayedGames} thisMonth={statistics.mostPlayedGamesThisMonth} />
						<CollectionCard
							collection={statistics.collection}
							ownedGames={statistics.ownedGames}
							totalCollectionValue={statistics.totalCollectionValue}
							pricedGameCount={statistics.pricedGameCount}
							recentAddedGames={statistics.recentAddedGames}
							currency={settings.currency}
							uiLanguage={settings.uiLanguage}
						/>
					</div>
				</div>
			</BgtPageContent>
		</BgtPage>
	);
}
