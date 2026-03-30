import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { format } from "date-fns";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { PlayerSessionAvatars } from "@/components/BgtAvatar/PlayerSessionAvatars";
import { BgtEditDeleteButtons } from "@/components/BgtButton/BgtEditDeleteButtons";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtDataTable, type DataTableProps } from "@/components/BgtTable/BgtDataTable";
import type { Session } from "@/models";
import { getGames } from "@/services/queries/games";
import { getPlayer, getPlayerSessions, getPlayers } from "@/services/queries/players";
import { getSettings } from "@/services/queries/settings";
import { playerIdParamSchema } from "@/utils/routeSchemas";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { usePlayerSessionData } from "./-hooks/usePlayerSessionData";

export const Route = createFileRoute("/players/$playerId_/sessions")({
	component: RouteComponent,
	params: playerIdParamSchema,
	loader: ({ params, context: { queryClient } }) => {
		queryClient.prefetchQuery(getSettings());
		queryClient.prefetchQuery(getGames());
		queryClient.prefetchQuery(getPlayers());
		queryClient.prefetchQuery(getPlayer(params.playerId));
		queryClient.prefetchQuery(getPlayerSessions(params.playerId));
	},
});

function RouteComponent() {
	const { playerId } = Route.useParams();
	const { t } = useTranslation(["common", "sessions"]);
	const navigate = useNavigate();
	const [sessionToDelete, setSessionToDelete] = useState<number | null>(null);

	const { sessions, deleteSession, settings, games, player, players, isLoading } = usePlayerSessionData({
		playerId,
		onDeleteSuccess: () => {
			setSessionToDelete(null);
		},
	});

	return (
		<BgtPage>
			<BgtPageContent isLoading={isLoading} data={{ player, settings, games, players }}>
				{({ player, settings, games, players }) => {
					const columns: DataTableProps<Session>["columns"] = [
						{
							accessorKey: "0",
							cell: ({ row }) => format(new Date(row.original.start), settings.dateFormat),
							header: t("date"),
						},
						{
							accessorKey: "1",
							cell: ({ row }) => format(new Date(row.original.start), settings.timeFormat),
							header: t("time"),
						},
						{
							accessorKey: "2",
							cell: ({ row }) => {
								const game = games.find((x) => x.id === row.original.gameId);
								return (
									<div className="flex flex-row gap-2 items-center">
										<BgtAvatar
											image={game?.image}
											title={game?.title}
											onClick={() => navigate({ to: `/games/${game?.id}` })}
										/>
										<span>{game?.title}</span>
									</div>
								);
							},
							header: t("game"),
						},
						{
							accessorKey: "3",
							cell: ({ row }) => `${row.original.minutes} ${t("minutes", { count: row.original.minutes })}`,
							header: t("duration"),
						},
						{
							accessorKey: "4",
							cell: ({ row }) => (
								<PlayerSessionAvatars playerSessions={row.original.playerSessions} players={players} games={games} gameId={row.original.gameId} won={true} />
							),
							header: t("winners"),
						},
						{
							accessorKey: "5",
							cell: ({ row }) => (
								<PlayerSessionAvatars playerSessions={row.original.playerSessions} players={players} games={games} gameId={row.original.gameId} won={false} />
							),
							header: t("other-players"),
						},
						{
							accessorKey: "6",
							cell: ({ row }) => {
								const highScore = row.original.playerSessions
									.filter((x) => x.score !== undefined)
									.sort((a, b) => b.score! - a.score!);

								if (highScore.length === 0) return "";

								return highScore[0].score;
							},
							header: t("high-score"),
						},
						{
							accessorKey: "200",
							cell: ({ row }) => (
								<BgtEditDeleteButtons
									onDelete={() => setSessionToDelete(row.original.id)}
									onEdit={() => navigate({ to: `/sessions/update/${row.original.id}` })}
								/>
							),
							header: () => <div className="flex justify-end">{t("actions")}</div>,
						},
					];

					return (
						<>
							<BgtPageHeader
								backAction={() => navigate({ to: `/players/${playerId}` })}
								header={`${player.name} - ${t("sessions:title")}`}
								actions={[
									{
										onClick: () => navigate({ to: "/sessions/new" }),
										variant: "primary",
										content: "sessions:new",
									},
								]}
							/>
							<BgtCard className="p-4">
								<BgtDataTable
									columns={columns}
									data={sessions}
									noDataMessage={t("no-data-yet")}
									widths={["w-[70px]", "w-[100px]", "w-[75px]"]}
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
