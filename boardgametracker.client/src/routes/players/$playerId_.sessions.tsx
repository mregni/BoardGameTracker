import { noop } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtEditDeleteButtons } from "@/components/BgtButton/BgtEditDeleteButtons";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtDataTable, type DataTableProps } from "@/components/BgtTable/BgtDataTable";
import { CoopResultPill } from "@/components/CoopResultPill/CoopResultPill";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { usePermissions } from "@/hooks/usePermissions";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { Session } from "@/models";
import { getGames } from "@/services/queries/games";
import { getPlayer, getPlayerSessions, getPlayers } from "@/services/queries/players";
import { getSettings } from "@/services/queries/settings";
import { toDisplay, toDisplayTime } from "@/utils/dateUtils";
import { playerIdParamSchema } from "@/utils/routeSchemas";
import { useBack } from "../-hooks/useBack";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { SessionCardList } from "../sessions/-components/SessionCardList";
import { SessionCommentCell } from "../sessions/-components/SessionCommentCell";
import { SessionPlayerPills } from "../sessions/-components/SessionPlayerPills";
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
	const { t } = useTranslation(["common", "sessions"]);
	const navigate = useNavigate();
	const shortDuration = useShortDuration();
	const { decimal } = useNumberFormat();
	const [sessionToDelete, setSessionToDelete] = useState<number | null>(null);
	const goBack = useBack(`/players/${playerId}`);
	const { canWrite } = usePermissions();

	const { sessions, deleteSession, settings, games, player, players, isLoading } = usePlayerSessionData({
		playerId,
		onDeleteSuccess: () => {
			setSessionToDelete(null);
		},
	});

	const gameMap = useMemo(() => new Map(games.map((game) => [game.id, game])), [games]);
	const columns = useMemo<DataTableProps<Session>["columns"]>(() => {
		if (!settings) return [];
		return [
			{
				accessorKey: "0",
				cell: ({ row }) => toDisplay(row.original.start, settings.dateFormat, settings.uiLanguage),
				header: t("date"),
			},
			{
				accessorKey: "1",
				cell: ({ row }) => toDisplayTime(row.original.start, settings.timeFormat, settings.uiLanguage),
				header: t("time"),
			},
			{
				accessorKey: "2",
				cell: ({ row }) => {
					const game = gameMap.get(row.original.gameId);
					return (
						game && (
							<div className="flex flex-row gap-2 items-center">
								<BgtAvatar
									image={game.image}
									title={game.title}
									onClick={() => navigate({ to: `/games/${game.id}` })}
								/>
								<span>{game.title}</span>
							</div>
						)
					);
				},
				header: t("game"),
			},
			{
				accessorKey: "3",
				cell: ({ row }) => shortDuration(row.original.minutes),
				header: t("duration"),
			},
			{
				accessorKey: "4",
				cell: ({ row }) => (
					<div className="flex flex-wrap items-center gap-1.5">
						{row.original.isCoop && <CoopResultPill won={row.original.teamWon === true} />}
						{row.original.isCoop && row.original.teamScore != null && (
							<span className="text-sm font-semibold tabular-nums">{decimal(row.original.teamScore)}</span>
						)}
						<SessionPlayerPills playerSessions={row.original.playerSessions} players={players} />
					</div>
				),
				header: t("players"),
			},
			{
				accessorKey: "5",
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
	}, [settings, gameMap, players, t, navigate, shortDuration, decimal, canWrite]);

	return (
		<BgtPage>
			<BgtPageContent isLoading={isLoading} data={{ player, settings, games, players }}>
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
													onClick: () => navigate({ to: "/sessions/new" }),
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
