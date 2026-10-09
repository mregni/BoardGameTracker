import { createFileRoute } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { BgtDetailLayout } from "@/components/BgtLayout/BgtDetailLayout";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { useModalState } from "@/hooks/useModalState";
import { usePermissions } from "@/hooks/usePermissions";
import { getGame, getGameSessionsShortList, getGameStatistics } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { gameIdParamSchema } from "@/utils/routeSchemas";
import { BgtPoster } from "../-components/BgtPoster";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { ExpansionsDialog } from "./-components/ExpansionsDialog";
import { GameDetailEmptyState } from "./-components/GameDetailEmptyState";
import { GameFactsCard } from "./-components/GameFactsCard";
import { GameHeader } from "./-components/GameHeader";
import { GameStaticSection } from "./-components/GameStaticSection";
import { GameStatisticsGrid } from "./-components/GameStatisticsGrid";
import { ManualsDialog } from "./-components/ManualsDialog";
import { PlayerCountChartCard } from "./-components/PlayerCountChartCard";
import { RecentSessionsCard } from "./-components/RecentSessionsCard";
import { ScoringResultsCard } from "./-components/ScoringResultsCard";
import { SessionCountChartCard } from "./-components/SessionCountChartCard";
import { TopPlayersCard } from "./-components/TopPlayersCard";
import { TrackPriceDialog } from "./-components/TrackPriceDialog";
import { useGameActions } from "./-hooks/useGameActions";
import { useGameData } from "./-hooks/useGameData";
import { useGameManuals } from "./-hooks/useGameManuals";
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

	const {
		game,
		deleteGame,
		settings,
		statistics,
		sessions,
		price,
		refreshPrice,
		isRefreshingPrice,
		createWatch,
		isCreatingWatch,
		deleteExpansion,
		addManualExpansion,
		isLoading,
	} = useGameData({
		gameId,
	});

	const modals = useGameModals();
	const manualsDialog = useModalState();
	const expansionsDialog = useModalState();
	const trackPriceDialog = useModalState();
	const { manuals = [] } = useGameManuals(gameId);

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
					const hasPlays = statistics.gameStats.playCount !== 0;
					return (
						<>
							<BgtDetailLayout
								sidebar={
									<>
										<div className="hidden lg:block">
											<BgtPoster title={game.title} image={game.image} />
										</div>
										<div className="max-lg:order-3">
											<GameFactsCard
												game={game}
												currency={settings.currency}
												dateFormat={settings.dateFormat}
												uiLanguage={settings.uiLanguage}
												manualCount={manuals.length}
												price={price}
												onRefreshPrice={refreshPrice}
												isRefreshingPrice={isRefreshingPrice}
												onOpenManuals={manualsDialog.show}
												onOpenExpansions={expansionsDialog.show}
											/>
										</div>
										{hasPlays && (
											<div className="max-lg:order-5">
												<TopPlayersCard topPlayers={statistics.topPlayers} />
											</div>
										)}
										{hasPlays && (
											<div className="max-lg:order-6">
												<RecentSessionsCard sessions={sessions} dateFormat={settings.dateFormat} gameId={gameId} />
											</div>
										)}
									</>
								}
							>
								<div className="max-lg:order-1 flex items-start gap-3">
									<div className="size-16 shrink-0 lg:hidden">
										<BgtPoster title={game.title} image={game.image} />
									</div>
									<div className="min-w-0 flex-1">
										<GameHeader
											gameTitle={game.title}
											gameState={game.state}
											isLoaned={game.isLoaned}
											hasPriceWatch={!!game.changeDetectionWatchId}
											livePrice={price}
											canWrite={canWrite}
											onAddSession={actions.handleAddSession}
											onEdit={actions.handleEdit}
											onDelete={modals.deleteModal.show}
										/>
									</div>
								</div>
								<div className="max-lg:order-2">
									<GameStaticSection
										game={game}
										manualCount={manuals.length}
										ragEnabled={settings.ragEnabled && canWrite}
										canTrackPrice={
											canWrite && !!settings.changeDetectionStatus?.isConfigured && !game.changeDetectionWatchId
										}
										onTrackPrice={trackPriceDialog.show}
									/>
								</div>
								{!hasPlays && (
									<div className="max-lg:order-4">
										<GameDetailEmptyState onLogSession={canWrite ? actions.handleAddSession : undefined} />
									</div>
								)}
								{hasPlays && (
									<div className="max-lg:order-4">
										<GameStatisticsGrid gameStats={statistics.gameStats} currency={settings.currency} />
									</div>
								)}
								{hasPlays && (
									<div className="max-lg:order-7 grid grid-cols-1 2xl:grid-cols-2 gap-3 xl:gap-6">
										<ScoringResultsCard scoreRankChart={statistics.scoreRankChart} />
										<SessionCountChartCard playByDayChart={statistics.playByDayChart} />
										<PlayerCountChartCard playerCountChart={statistics.playerCountChart} />
									</div>
								)}
							</BgtDetailLayout>
							<TrackPriceDialog
								open={trackPriceDialog.isOpen}
								close={trackPriceDialog.hide}
								initialUrl={game.shopUrl}
								isPending={isCreatingWatch}
								onSubmit={async (url) => {
									await createWatch(url);
									trackPriceDialog.hide();
								}}
							/>
							<ManualsDialog
								gameId={gameId}
								open={manualsDialog.isOpen}
								close={manualsDialog.hide}
								canWrite={canWrite}
								ragEnabled={settings.ragEnabled}
								dateFormat={settings.dateFormat}
								uiLanguage={settings.uiLanguage}
							/>
							<ExpansionsDialog
								expansions={game.expansions}
								open={expansionsDialog.isOpen}
								close={expansionsDialog.hide}
								canWrite={canWrite}
								bggEnabled={bggEnabled}
								onAddExpansion={actions.handleAddExpansion}
								onAddManualExpansion={addManualExpansion}
								onDeleteExpansion={actions.handleDeleteExpansion}
							/>
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
									selectedExpansions={game.expansions.flatMap((x) => (x.bggId === null ? [] : [x.bggId]))}
								/>
							)}
						</>
					);
				}}
			</BgtPageContent>
		</BgtPage>
	);
}
