import { noop } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import AlertTriangle from "@/assets/icons/alert-triangle.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtEmptyState } from "@/components/BgtLayout/BgtEmptyState";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtDataTable } from "@/components/BgtTable/BgtDataTable";
import { usePermissions } from "@/hooks/usePermissions";
import { getGame, getGameSessions } from "@/services/queries/games";
import { getPlayers } from "@/services/queries/players";
import { getSettings } from "@/services/queries/settings";
import { gameIdParamSchema } from "@/utils/routeSchemas";
import { useBack } from "../-hooks/useBack";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { SessionCardList } from "../sessions/-components/SessionCardList";
import { useSessionColumns } from "../sessions/-hooks/useSessionColumns";
import { useGameSessionsData } from "./-hooks/useGameSessionsData";

export const Route = createFileRoute("/games/$gameId_/sessions")({
	component: RouteComponent,
	params: gameIdParamSchema,
	loader: ({ params, context: { queryClient } }) => {
		queryClient.query(getGame(params.gameId)).catch(noop);
		queryClient.query(getSettings()).catch(noop);
		queryClient.query(getPlayers()).catch(noop);
		queryClient.query(getGameSessions(params.gameId)).catch(noop);
	},
});

function RouteComponent() {
	const { gameId } = Route.useParams();
	const { t } = useTranslation(["common", "sessions", "game"]);
	const navigate = useNavigate();
	const { canWrite } = usePermissions();
	const [sessionToDelete, setSessionToDelete] = useState<number | null>(null);
	const goBack = useBack(`/games/${gameId}`);

	const { settings, game, sessions, deleteSession, players, isLoading, error } = useGameSessionsData({
		gameId,
		onDeleteSuccess: () => {
			setSessionToDelete(null);
		},
	});

	const columns = useSessionColumns({ settings, players, onDelete: setSessionToDelete });

	return (
		<BgtPage>
			<BgtPageContent
				isLoading={isLoading}
				data={{ game, settings, players }}
				error={error}
				notFound={
					<BgtEmptyState
						icon={AlertTriangle}
						title={t("game:notifications.not-found")}
						description={t("game:load-error.not-found-description")}
						action={{ label: t("game:load-error.back"), onClick: () => navigate({ to: "/games" }) }}
					/>
				}
			>
				{({ game, settings, players }) => {
					return (
						<>
							<BgtPageHeader
								backAction={goBack}
								header={t("sessions:title-of", { name: game.title })}
								actions={
									canWrite
										? [
												{
													onClick: () => navigate({ to: `/sessions/new/${gameId}` }),
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
									games={[game]}
									dateFormat={settings.dateFormat}
									timeFormat={settings.timeFormat}
									uiLanguage={settings.uiLanguage}
									canWrite={canWrite}
									onEdit={(sessionId) => navigate({ to: `/sessions/update/${sessionId}` })}
									onDelete={setSessionToDelete}
								/>
								<BgtDeleteModal
									open={sessionToDelete !== null}
									close={() => setSessionToDelete(null)}
									onDelete={() => sessionToDelete && deleteSession(sessionToDelete)}
									title={t("sessions:delete.title")}
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
