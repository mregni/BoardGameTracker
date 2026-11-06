import { noop } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import AlertTriangle from "@/assets/icons/alert-triangle.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtEmptyState } from "@/components/BgtLayout/BgtEmptyState";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtDataTable, type DataTableProps } from "@/components/BgtTable/BgtDataTable";
import { usePermissions } from "@/hooks/usePermissions";
import type { Session } from "@/models";
import { getGames } from "@/services/queries/games";
import { getPlayer, getPlayerSessions, getPlayers } from "@/services/queries/players";
import { getSettings } from "@/services/queries/settings";
import { playerIdParamSchema } from "@/utils/routeSchemas";
import { useBack } from "../-hooks/useBack";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { SessionCardList } from "../sessions/-components/SessionCardList";
import { useSessionColumns } from "../sessions/-hooks/useSessionColumns";
import { usePlayerSessionData } from "./-hooks/usePlayerSessionData";

export const Route = createFileRoute("/players/$playerId_/sessions")({
	component: RouteComponent,
	params: playerIdParamSchema,
	loader: ({ params, context: { queryClient } }) => {
		queryClient.query(getSettings()).catch(noop);
		queryClient.query(getGames()).catch(noop);
		queryClient.query(getPlayers()).catch(noop);
		queryClient.query(getPlayer(params.playerId)).catch(noop);
		queryClient.query(getPlayerSessions(params.playerId)).catch(noop);
	},
});

function RouteComponent() {
	const { playerId } = Route.useParams();
	const { t } = useTranslation(["common", "sessions", "player"]);
	const navigate = useNavigate();
	const [sessionToDelete, setSessionToDelete] = useState<number | null>(null);
	const goBack = useBack(`/players/${playerId}`);
	const { canWrite } = usePermissions();

	const { sessions, deleteSession, settings, games, player, players, isLoading, error, retry } = usePlayerSessionData({
		playerId,
		onDeleteSuccess: () => {
			setSessionToDelete(null);
		},
	});

	const gameMap = useMemo(() => new Map(games.map((game) => [game.id, game])), [games]);
	const subjectColumn = useMemo<DataTableProps<Session>["columns"][number]>(
		() => ({
			id: "game",
			accessorFn: (session) => gameMap.get(session.gameId)?.title ?? "",
			cell: ({ row }) => {
				const game = gameMap.get(row.original.gameId);
				return (
					game && (
						<div className="flex flex-row gap-2 items-center">
							<BgtAvatar image={game.image} title={game.title} onClick={() => navigate({ to: `/games/${game.id}` })} />
							<span>{game.title}</span>
						</div>
					)
				);
			},
			header: t("game"),
		}),
		[gameMap, navigate, t],
	);
	const columns = useSessionColumns({ settings, players, onDelete: setSessionToDelete, subjectColumn });

	return (
		<BgtPage>
			<BgtPageContent
				isLoading={isLoading}
				data={{ player, settings, games, players }}
				error={error}
				onRetry={retry}
				notFound={
					<BgtEmptyState
						icon={AlertTriangle}
						title={t("player:notifications.not-found")}
						action={{ label: t("player:back"), onClick: () => navigate({ to: "/players" }) }}
					/>
				}
			>
				{({ player, settings, games, players }) => {
					return (
						<>
							<BgtPageHeader
								backAction={goBack}
								header={t("sessions:title-of", { name: player.name })}
								actions={
									canWrite
										? [
												{
													onClick: () => navigate({ to: "/sessions/new", search: { player: playerId } }),
													variant: "primary",
													content: "game:add",
												},
											]
										: []
								}
							/>
							<BgtCard className="p-4">
								<div className="hidden md:block">
									<BgtDataTable
										columns={columns}
										data={sessions}
										noDataMessage={t("no-data-yet")}
										widths={["w-[70px]", "w-[100px]", "w-[75px]"]}
									/>
								</div>
								<SessionCardList
									className="md:hidden"
									sessions={sessions}
									players={players}
									games={games}
									dateFormat={settings.dateFormat}
									timeFormat={settings.timeFormat}
									uiLanguage={settings.uiLanguage}
									showGame
									canWrite={canWrite}
									onEdit={(sessionId) => navigate({ to: `/sessions/update/${sessionId}` })}
									onDelete={setSessionToDelete}
								/>
								<BgtDeleteModal
									open={sessionToDelete !== null}
									close={() => setSessionToDelete(null)}
									onDelete={() => sessionToDelete && deleteSession(sessionToDelete)}
									heading={t("sessions:delete.title")}
									description={t("sessions:delete.description")}
								/>
							</BgtCard>
						</>
					);
				}}
			</BgtPageContent>
		</BgtPage>
	);
}
