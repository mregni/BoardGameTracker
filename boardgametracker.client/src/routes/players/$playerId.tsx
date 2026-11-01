import { noop } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect } from "react";
import { useTranslation } from "react-i18next";
import Game from "@/assets/icons/gamepad.svg?react";
import UsersIcon from "@/assets/icons/users.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtDetailLayout } from "@/components/BgtLayout/BgtDetailLayout";
import { BgtEmptyState } from "@/components/BgtLayout/BgtEmptyState";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { usePermissions } from "@/hooks/usePermissions";
import { isApiError } from "@/models";
import { getBadges } from "@/services/queries/basdges";
import { getPlayer, getPlayerStatistics } from "@/services/queries/players";
import { playerIdParamSchema } from "@/utils/routeSchemas";
import { BgtPoster } from "../-components/BgtPoster";
import { useGameById } from "../-hooks/useGameById";
import { usePlayerById } from "../-hooks/usePlayerById";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { MostPlayedGamesCard } from "./-components/MostPlayedGamesCard";
import { PlayerAchievementsCard } from "./-components/PlayerAchievementsCard";
import { PlayerHeader } from "./-components/PlayerHeader";
import { PlayerRivalsCard } from "./-components/PlayerRivalsCard";
import { PlayerStatisticsGrid } from "./-components/PlayerStatisticsGrid";
import { RecentPlayerSessionsCard } from "./-components/RecentPlayerSessionsCard";
import { usePlayerActions } from "./-hooks/usePlayerActions";
import { silent, usePlayerData } from "./-hooks/usePlayerData";
import { usePlayerModals } from "./-hooks/usePlayerModals";
import { EditPlayerModal } from "./-modals/EditPlayerModal";
import { lastPlayedOf } from "./-utils/lastPlayed";
import { formOf, nemesisOf } from "./-utils/rivalry";

const RECENT_SESSION_COUNT = 5;

export const Route = createFileRoute("/players/$playerId")({
	component: RouteComponent,
	params: playerIdParamSchema,
	loader: ({ params, context: { queryClient } }) => {
		queryClient.query({ ...getPlayer(params.playerId), meta: silent }).catch(noop);
		queryClient.query({ ...getPlayerStatistics(params.playerId), meta: silent }).catch(noop);
		queryClient.query(getBadges()).catch(noop);
	},
});

function RouteComponent() {
	const { playerId } = Route.useParams();
	const { t } = useTranslation("player");
	const { canWrite } = usePermissions();
	const navigate = useNavigate();

	const { player, statistics, deletePlayer, badges, sessions, settings, isLoading, error, retry } = usePlayerData({
		playerId,
	});

	const { gameById } = useGameById();
	const { playerById } = usePlayerById();
	const modals = usePlayerModals();
	const lastPlayed = lastPlayedOf(sessions, gameById);
	const form = formOf(sessions, playerId);
	const rivals = statistics?.rivals ?? [];
	const nemesisRival = nemesisOf(rivals);
	const nemesisName = nemesisRival ? playerById(nemesisRival.playerId)?.name : undefined;
	const nemesis = nemesisRival && nemesisName ? { ...nemesisRival, name: nemesisName } : null;

	const actions = usePlayerActions({
		playerId,
		deletePlayer,
		onDeleteModalClose: modals.deleteModal.hide,
	});

	const notFound = isApiError(error) && error.status === 404;
	const pageTitle = player?.name ?? (notFound ? t("missing.title") : undefined);
	useEffect(() => {
		if (!pageTitle) return;
		const previous = document.title;
		document.title = `${pageTitle} · BoardGameTracker`;
		return () => {
			document.title = previous;
		};
	}, [pageTitle]);

	return (
		<BgtPage>
			<BgtPageHeader backAction={actions.handleBackToPlayers} backText={t("back")} />
			<BgtPageContent
				isLoading={isLoading}
				data={{ player, statistics, badges, settings }}
				error={error}
				onRetry={retry}
				notFound={
					<BgtEmptyState
						icon={UsersIcon}
						headingAs="h1"
						title={t("missing.title")}
						description={t("missing.description")}
						action={{ label: t("back"), onClick: actions.handleBackToPlayers }}
					/>
				}
			>
				{({ player, statistics, badges, settings }) => {
					const content = (
						<>
							<PlayerHeader
								playerId={playerId}
								playerName={player.name}
								image={player.image}
								playCount={statistics.playCount}
								canWrite={canWrite}
								onDelete={modals.deleteModal.show}
								onEdit={modals.editModal.show}
							/>
							{statistics.playCount === 0 ? (
								<BgtCard>
									<BgtEmptyState
										icon={Game}
										headingAs="h2"
										title={t("no-sessions.title", { name: player.name })}
										description={t("no-sessions.description")}
										action={
											canWrite
												? {
														label: t("actions.log-session"),
														onClick: () => navigate({ to: "/sessions/new", search: { player: playerId } }),
													}
												: undefined
										}
									/>
								</BgtCard>
							) : (
								<>
									<PlayerStatisticsGrid
										playerId={playerId}
										statistics={statistics}
										settings={settings}
										lastPlayed={lastPlayed}
										form={form}
										nemesis={nemesis}
									/>
									<RecentPlayerSessionsCard
										sessions={sessions.slice(0, RECENT_SESSION_COUNT)}
										playerId={playerId}
										dateFormat={settings.dateFormat}
										uiLanguage={settings.uiLanguage}
									/>
									<div className="@container">
										<div
											className={
												rivals.length > 0 ? "grid items-start gap-3 xl:gap-6 @2xl:grid-cols-2" : "grid gap-3 xl:gap-6"
											}
										>
											<PlayerRivalsCard playerId={playerId} rivals={rivals} />
											<MostPlayedGamesCard games={statistics.mostPlayedGames} />
										</div>
									</div>
									<PlayerAchievementsCard playerBadges={player.badges} badges={badges} />
								</>
							)}
						</>
					);

					return (
						<>
							{player.image ? (
								<BgtDetailLayout
									sidebar={
										<div className="hidden lg:block">
											<BgtPoster title={player.name} image={player.image} />
										</div>
									}
								>
									{content}
								</BgtDetailLayout>
							) : (
								<div className="flex flex-col gap-3 xl:gap-6">{content}</div>
							)}
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
					);
				}}
			</BgtPageContent>
		</BgtPage>
	);
}
