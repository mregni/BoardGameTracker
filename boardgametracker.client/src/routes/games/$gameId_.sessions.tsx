import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { format } from "date-fns";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { PlayerSessionAvatars } from "@/components/BgtAvatar/PlayerSessionAvatars";
import { BgtEditDeleteButtons } from "@/components/BgtButton/BgtEditDeleteButtons";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtDataTable, type DataTableProps } from "@/components/BgtTable/BgtDataTable";
import { usePermissions } from "@/hooks/usePermissions";
import type { Session } from "@/models";
import { getGame, getGameSessions } from "@/services/queries/games";
import { getPlayers } from "@/services/queries/players";
import { getSettings } from "@/services/queries/settings";
import { toDisplay } from "@/utils/dateUtils";
import { gameIdParamSchema } from "@/utils/routeSchemas";
import { useGoUp } from "../-hooks/useGoUp";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { SessionCardList } from "../sessions/-components/SessionCardList";
import { SessionCommentCell } from "../sessions/-components/SessionCommentCell";
import { useGameSessionsData } from "./-hooks/useGameSessionsData";

export const Route = createFileRoute("/games/$gameId_/sessions")({
	component: RouteComponent,
	params: gameIdParamSchema,
	loader: ({ params, context: { queryClient } }) => {
		queryClient.prefetchQuery(getGame(params.gameId));
		queryClient.prefetchQuery(getSettings());
		queryClient.prefetchQuery(getPlayers());
		queryClient.prefetchQuery(getGameSessions(params.gameId));
	},
});

function RouteComponent() {
	const { gameId } = Route.useParams();
	const { t } = useTranslation(["common", "sessions"]);
	const navigate = useNavigate();
	const { canWrite } = usePermissions();
	const [sessionToDelete, setSessionToDelete] = useState<number | null>(null);
	const goUp = useGoUp(`/games/${gameId}`);

	const { settings, game, sessions, deleteSession, players, isLoading } = useGameSessionsData({
		gameId,
		onDeleteSuccess: () => {
			setSessionToDelete(null);
		},
	});

	return (
		<BgtPage>
			<BgtPageContent isLoading={isLoading} data={{ game, settings, players }}>
				{({ game, settings, players }) => {
					const columns: DataTableProps<Session>["columns"] = [
						{
							accessorKey: "0",
							cell: ({ row }) => toDisplay(row.original.start, settings.dateFormat, settings.uiLanguage),
							header: t("date"),
						},
						{
							accessorKey: "1",
							cell: ({ row }) => format(new Date(row.original.start), settings.timeFormat),
							header: t("time"),
						},
						{
							accessorKey: "2",
							cell: ({ row }) => `${row.original.minutes} ${t("minutes", { count: row.original.minutes })}`,
							header: t("duration"),
						},
						{
							accessorKey: "3",
							cell: ({ row }) => (
								<PlayerSessionAvatars
									playerSessions={row.original.playerSessions}
									players={players}
									game={game}
									won={true}
								/>
							),
							header: t("winners"),
						},
						{
							accessorKey: "4",
							cell: ({ row }) => (
								<PlayerSessionAvatars
									playerSessions={row.original.playerSessions}
									players={players}
									game={game}
									won={false}
								/>
							),
							header: t("other-players"),
						},
						{
							accessorKey: "5",
							cell: ({ row }) => {
								const highScore = row.original.playerSessions
									.filter((x) => x.score != null)
									.sort((a, b) => b.score! - a.score!);

								if (highScore.length === 0) return "";

								return highScore[0].score;
							},
							header: t("high-score"),
						},
						{
							accessorKey: "6",
							cell: ({ row }) => <SessionCommentCell comment={row.original.comment} />,
							header: t("comment"),
						},
						...(canWrite
							? [
									{
										accessorKey: "200",
										cell: ({ row }: { row: { original: Session } }) => (
											<BgtEditDeleteButtons
												onDelete={() => setSessionToDelete(row.original.id)}
												onEdit={() => navigate({ to: `/sessions/update/${row.original.id}` })}
											/>
										),
										header: () => <div className="flex justify-end">{t("actions")}</div>,
									},
								]
							: []),
					];

					return (
						<>
							<BgtPageHeader
								backAction={goUp}
								header={`${game.title} - ${t("sessions:title")}`}
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
