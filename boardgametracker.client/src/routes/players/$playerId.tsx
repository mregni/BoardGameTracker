import { createFileRoute } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { BgtDetailLayout } from "@/components/BgtLayout/BgtDetailLayout";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { usePermissions } from "@/hooks/usePermissions";
import { getBadges } from "@/services/queries/basdges";
import { getPlayer, getPlayerStatistics } from "@/services/queries/players";
import { playerIdParamSchema } from "@/utils/routeSchemas";
import { BgtPoster } from "../-components/BgtPoster";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { MostPlayedGamesCard } from "./-components/MostPlayedGamesCard";
import { PlayerAchievementsCard } from "./-components/PlayerAchievementsCard";
import { PlayerAchievementsSummary } from "./-components/PlayerAchievementsSummary";
import { PlayerHeader } from "./-components/PlayerHeader";
import { PlayerStatisticsGrid } from "./-components/PlayerStatisticsGrid";
import { PlayerWinRecordCard } from "./-components/PlayerWinRecordCard";
import { RecentPlayerSessionsCard } from "./-components/RecentPlayerSessionsCard";
import { usePlayerActions } from "./-hooks/usePlayerActions";
import { usePlayerData } from "./-hooks/usePlayerData";
import { usePlayerModals } from "./-hooks/usePlayerModals";
import { EditPlayerModal } from "./-modals/EditPlayerModal";

export const Route = createFileRoute("/players/$playerId")({
	component: RouteComponent,
	params: playerIdParamSchema,
	loader: ({ params, context: { queryClient } }) => {
		queryClient.prefetchQuery(getPlayer(params.playerId));
		queryClient.prefetchQuery(getPlayerStatistics(params.playerId));
		queryClient.prefetchQuery(getBadges());
	},
});

function RouteComponent() {
	const { playerId } = Route.useParams();
	const { t } = useTranslation("player");
	const { canWrite } = usePermissions();

	const { player, statistics, deletePlayer, badges, sessions, settings, isLoading } = usePlayerData({
		playerId,
	});

	const modals = usePlayerModals();

	const actions = usePlayerActions({
		playerId,
		deletePlayer,
		onDeleteModalClose: modals.deleteModal.hide,
	});

	return (
		<BgtPage>
			<BgtPageHeader backAction={actions.handleBackToPlayers} backText={t("back")} />
			<BgtPageContent isLoading={isLoading} data={{ player, statistics, badges, settings }}>
				{({ player, statistics, badges, settings }) => (
					<>
						<BgtDetailLayout
							sidebar={
								<>
									<div className="hidden lg:block">
										<BgtPoster title={player.name} image={player.image} />
									</div>
									<div className="max-lg:order-2">
										<PlayerAchievementsSummary playerBadges={player.badges} badges={badges} />
									</div>
									{statistics.playCount !== 0 && (
										<div className="max-lg:order-6">
											<PlayerWinRecordCard total={statistics.playCount} wins={statistics.winCount} />
										</div>
									)}
								</>
							}
						>
							<div className="max-lg:order-1 flex items-start gap-3">
								<div className="size-16 shrink-0 lg:hidden">
									<BgtPoster title={player.name} image={player.image} />
								</div>
								<div className="min-w-0 flex-1">
									<PlayerHeader
										playerName={player.name}
										canWrite={canWrite}
										onDelete={modals.deleteModal.show}
										onEdit={modals.editModal.show}
									/>
								</div>
							</div>
							{statistics.playCount !== 0 && (
								<div className="max-lg:order-3">
									<PlayerStatisticsGrid statistics={statistics} settings={settings} />
								</div>
							)}
							{statistics.playCount !== 0 && (
								<div className="max-lg:order-4">
									<RecentPlayerSessionsCard
										sessions={sessions}
										playerId={playerId}
										dateFormat={settings.dateFormat}
										uiLanguage={settings.uiLanguage}
									/>
								</div>
							)}
							{statistics.playCount !== 0 && (
								<div className="max-lg:order-5">
									<MostPlayedGamesCard games={statistics.mostPlayedGames} />
								</div>
							)}
							{statistics.playCount !== 0 && (
								<div className="max-lg:order-7">
									<PlayerAchievementsCard playerBadges={player.badges} badges={badges} />
								</div>
							)}
						</BgtDetailLayout>
						<BgtDeleteModal
							title={player.name}
							open={modals.deleteModal.isOpen}
							close={modals.deleteModal.hide}
							onDelete={actions.handleDelete}
							description={t("delete.description", {
								name: player.name,
							})}
						/>
						{modals.editModal.isOpen && (
							<EditPlayerModal open={modals.editModal.isOpen} close={modals.editModal.hide} player={player} />
						)}
					</>
				)}
			</BgtPageContent>
		</BgtPage>
	);
}
