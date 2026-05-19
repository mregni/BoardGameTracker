import { createFileRoute } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { usePermissions } from "@/hooks/usePermissions";
import { getGame, getGameSessionsShortList, getGameStatistics } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { gameIdParamSchema } from "@/utils/routeSchemas";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { ExpansionsCard } from "./-components/ExpansionsCard";
import { GameDetailEmptyState } from "./-components/GameDetailEmptyState";
import { GameHeader } from "./-components/GameHeader";
import { GameStaticSection } from "./-components/GameStaticSection";
import { GameStatisticsGrid } from "./-components/GameStatisticsGrid";
import { PlayerCountChartCard } from "./-components/PlayerCountChartCard";
import { RecentSessionsCard } from "./-components/RecentSessionsCard";
import { ScoringResultsCard } from "./-components/ScoringResultsCard";
import { SessionCountChartCard } from "./-components/SessionCountChartCard";
import { TopPlayersCard } from "./-components/TopPlayersCard";
import { useGameActions } from "./-hooks/useGameActions";
import { useGameData } from "./-hooks/useGameData";
import { useGameModals } from "./-hooks/useGameModals";
import { ExpansionSelectorModal } from "./-modals/ExpansionSelectorModal";

export const Route = createFileRoute("/games/$gameId")({
	component: RouteComponent,
	params: gameIdParamSchema,
	loader: async ({ params, context: { queryClient } }) => {
		queryClient.prefetchQuery(getGame(params.gameId));
		queryClient.prefetchQuery(getGameStatistics(params.gameId));
		queryClient.prefetchQuery(getSettings());
		queryClient.prefetchQuery(getGameSessionsShortList(params.gameId, 5));
	},
});

function RouteComponent() {
	const { gameId } = Route.useParams();
	const { t } = useTranslation(["games", "common"]);
	const { canWrite } = usePermissions();

	const { game, deleteGame, settings, statistics, sessions, deleteExpansion, isLoading } = useGameData({
		gameId,
	});

	const modals = useGameModals();

	const actions = useGameActions({
		gameId,
		deleteGame,
		deleteExpansion,
		onDeleteModalClose: modals.deleteModal.hide,
		onExpansionModalOpen: modals.expansionModal.show,
	});

	return (
		<BgtPage>
			<BgtPageHeader backAction={actions.handleBackToGames} backText={t("back")} />
			<BgtPageContent isLoading={isLoading} data={{ game, settings, statistics, sessions }}>
				{({ game, settings, statistics, sessions }) => {
					const bggEnabled = settings.bggStatus?.isConfigured ?? false;
					return (
						<>
							<GameHeader
								gameTitle={game.title}
								gameState={game.state}
								isLoaned={game.isLoaned}
								canWrite={canWrite}
								onAddSession={actions.handleAddSession}
								onEdit={actions.handleEdit}
								onDelete={modals.deleteModal.show}
							/>
							<GameStaticSection
								game={game}
								playCount={statistics.gameStats.playCount}
								currency={settings.currency}
								dateFormat={settings.dateFormat}
								uiLanguage={settings.uiLanguage}
							/>
							{statistics.gameStats.playCount === 0 && (
								<GameDetailEmptyState onLogSession={canWrite ? actions.handleAddSession : undefined} />
							)}
							{statistics.gameStats.playCount !== 0 && (
								<>
									<GameStatisticsGrid
										gameStats={statistics.gameStats}
										expansionCount={game.expansions.length}
										currency={settings.currency}
									/>
									<div className="grid grid-cols-1 lg:grid-cols-2 gap-3 xl:gap-6">
										<div className="flex flex-col gap-3 xl:gap-6">
											<TopPlayersCard topPlayers={statistics.topPlayers} />
											<RecentSessionsCard
												sessions={sessions}
												dateFormat={settings.dateFormat}
												gameId={gameId.toString()}
											/>
											<SessionCountChartCard playByDayChart={statistics.playByDayChart} />
										</div>
										<div className="flex flex-col gap-3 xl:gap-6">
											<ScoringResultsCard scoreRankChart={statistics.scoreRankChart} />
											<PlayerCountChartCard playerCountChart={statistics.playerCountChart} />
											<ExpansionsCard
												expansions={game.expansions}
												canWrite={canWrite && bggEnabled}
												onAddExpansion={actions.handleAddExpansion}
												onDeleteExpansion={actions.handleDeleteExpansion}
											/>
										</div>
									</div>
								</>
							)}
							<BgtDeleteModal
								title={game.title}
								open={modals.deleteModal.isOpen}
								close={modals.deleteModal.hide}
								onDelete={actions.handleDelete}
								description={t("common:delete.description", {
									title: game.title,
								})}
							/>
							{bggEnabled && modals.expansionModal.isOpen && (
								<ExpansionSelectorModal
									open={modals.expansionModal.isOpen}
									close={modals.expansionModal.hide}
									gameId={gameId}
									selectedExpansions={game.expansions.map((x) => x.bggId)}
								/>
							)}
						</>
					);
				}}
			</BgtPageContent>
		</BgtPage>
	);
}
