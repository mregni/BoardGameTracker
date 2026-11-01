import { noop, useQuery } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { useEffect } from "react";
import { useTranslation } from "react-i18next";
import AlertTriangle from "@/assets/icons/alert-triangle.svg?react";
import { BgtDetailLayout } from "@/components/BgtLayout/BgtDetailLayout";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { useModalState } from "@/hooks/useModalState";
import { usePermissions } from "@/hooks/usePermissions";
import { GameState } from "@/models";
import { getGame, getGameSessionsShortList, getGameStatistics } from "@/services/queries/games";
import { getLoans } from "@/services/queries/loans";
import { getSettings } from "@/services/queries/settings";
import { toDisplay } from "@/utils/dateUtils";
import { gameIdParamSchema } from "@/utils/routeSchemas";
import { BgtPoster } from "../-components/BgtPoster";
import { usePlayerById } from "../-hooks/usePlayerById";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { ExpansionsDialog } from "./-components/ExpansionsDialog";
import { GameDetailEmptyState } from "./-components/GameDetailEmptyState";
import { GameDetailsStatus } from "./-components/GameDetailsStatus";
import { GameFactsCard } from "./-components/GameFactsCard";
import { GameHeader } from "./-components/GameHeader";
import { GameStaticSection } from "./-components/GameStaticSection";
import { GameStatisticsGrid } from "./-components/GameStatisticsGrid";
import { withStateChange } from "./-components/LivePrice";
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
import { useInlineGameUpdate } from "./-hooks/useInlineGameUpdate";
import { ExpansionSelectorModal } from "./-modals/ExpansionSelectorModal";

export const Route = createFileRoute("/games/$gameId")({
	component: RouteComponent,
	params: gameIdParamSchema,
	loader: async ({ params, context: { queryClient } }) => {
		queryClient.query(getGame(params.gameId)).catch(noop);
		queryClient.query(getGameStatistics(params.gameId)).catch(noop);
		queryClient.query(getSettings()).catch(noop);
		queryClient.query(getGameSessionsShortList(params.gameId, 5)).catch(noop);
	},
});

function RouteComponent() {
	const { gameId } = Route.useParams();
	const { t } = useTranslation(["games", "game", "common"]);
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
		loadFailed,
		isNotFound,
		detailsFailed,
		retry,
	} = useGameData({
		gameId,
	});

	const modals = useGameModals();
	const manualsDialog = useModalState();
	const expansionsDialog = useModalState();
	const trackPriceDialog = useModalState();
	const { manuals = [] } = useGameManuals(gameId);
	const { updateGame } = useInlineGameUpdate();
	const { playerById } = usePlayerById();
	const { data: loans } = useQuery({ ...getLoans(), enabled: !!game?.isLoaned });
	const activeLoan = loans?.find((loan) => loan.gameId === gameId && loan.isActive);
	const borrower = playerById(activeLoan?.playerId);

	const gameTitle = isNotFound ? t("game:notifications.not-found") : game?.title;
	useEffect(() => {
		if (!gameTitle) return;
		const previous = document.title;
		document.title = `${gameTitle} · BoardGameTracker`;
		return () => {
			document.title = previous;
		};
	}, [gameTitle]);

	const actions = useGameActions({
		gameId,
		deleteGame,
		deleteExpansion,
		onDeleteModalClose: modals.deleteModal.hide,
		onExpansionModalOpen: modals.expansionModal.show,
	});

	if (loadFailed) {
		return (
			<BgtEmptyPage
				showHeader={false}
				icon={AlertTriangle}
				title={t(isNotFound ? "game:notifications.not-found" : "game:load-error.failed")}
				description={t(isNotFound ? "game:load-error.not-found-description" : "game:load-error.failed-description")}
				action={
					isNotFound
						? { label: t("game:load-error.back"), onClick: actions.handleBackToGames }
						: { label: t("game:load-error.retry"), onClick: retry }
				}
			/>
		);
	}

	return (
		<BgtPage>
			<BgtPageHeader backAction={actions.handleBackToGames} backText={t("back")} />
			<BgtPageContent isLoading={isLoading} data={{ game, settings }}>
				{({ game, settings }) => {
					const bggEnabled = settings.bggStatus?.isConfigured ?? false;
					const details = statistics && sessions ? { statistics, sessions } : null;
					const played = details?.statistics.gameStats.playCount ? details : null;
					const hasPlays = played !== null;
					return (
						<>
							<BgtDetailLayout
								sidebar={
									<>
										<div className="hidden lg:block">
											<BgtPoster title={game.title} image={game.image} />
										</div>
										<div className="max-lg:order-7">
											<GameFactsCard
												game={game}
												currency={settings.currency}
												dateFormat={settings.dateFormat}
												uiLanguage={settings.uiLanguage}
												price={price}
												onRefreshPrice={refreshPrice}
												isRefreshingPrice={isRefreshingPrice}
												onTrackPrice={
													canWrite && settings.changeDetectionStatus?.isConfigured && !game.changeDetectionWatchId
														? trackPriceDialog.show
														: undefined
												}
											/>
										</div>
										{played && (
											<div className="max-lg:order-5">
												<TopPlayersCard topPlayers={played.statistics.topPlayers} />
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
											loan={
												activeLoan && borrower
													? {
															playerName: borrower.name,
															since: toDisplay(activeLoan.loanDate, settings.dateFormat, settings.uiLanguage),
														}
													: undefined
											}
											canWrite={canWrite}
											showAddSession={details === null || hasPlays}
											onAddSession={actions.handleAddSession}
											onMarkOwned={() => updateGame(withStateChange(game, GameState.Owned, price))}
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
										onOpenManuals={manualsDialog.show}
										onOpenExpansions={expansionsDialog.show}
									/>
								</div>
								{details === null && (
									<div className="max-lg:order-3">
										<GameDetailsStatus failed={detailsFailed} onRetry={retry} />
									</div>
								)}
								{details !== null && !hasPlays && (
									<div className="max-lg:order-3">
										<GameDetailEmptyState
											wanted={game.state === GameState.Wanted}
											onAddSession={canWrite ? actions.handleAddSession : undefined}
										/>
									</div>
								)}
								{played && (
									<div className="max-lg:order-3">
										<GameStatisticsGrid
											gameStats={played.statistics.gameStats}
											currency={settings.currency}
											hasScoringResults={played.statistics.scoreRankChart.length > 0}
										/>
									</div>
								)}
								{played && (
									<div className="max-lg:order-4">
										<RecentSessionsCard sessions={played.sessions} dateFormat={settings.dateFormat} gameId={gameId} />
									</div>
								)}
								{played && (
									<div className="max-lg:order-6 grid grid-cols-1 2xl:grid-cols-2 gap-3 xl:gap-6">
										<ScoringResultsCard scoreRankChart={played.statistics.scoreRankChart} />
										<SessionCountChartCard playByDayChart={played.statistics.playByDayChart} />
										<PlayerCountChartCard playerCountChart={played.statistics.playerCountChart} />
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
