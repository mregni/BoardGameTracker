import { createFileRoute, Navigate, useNavigate } from "@tanstack/react-router";
import { addMinutes } from "date-fns";
import { useTranslation } from "react-i18next";
import Game from "@/assets/icons/gamepad.svg?react";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { usePermissions } from "@/hooks/usePermissions";
import type { CreateSession } from "@/models";
import { getGames } from "@/services/queries/games";
import { getLocations } from "@/services/queries/locations";
import { getPlayers } from "@/services/queries/players";
import { SessionForm } from "./-components/SessionForm";
import { useNewSessionData } from "./-hooks/useNewSessionData";

export const Route = createFileRoute("/sessions/new")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getGames());
		queryClient.prefetchQuery(getPlayers());
		queryClient.prefetchQuery(getLocations());
	},
});

function RouteComponent() {
	const navigate = useNavigate();
	const { t } = useTranslation();
	const { canWrite } = usePermissions();
	const { isPending, saveSession, games } = useNewSessionData();

	if (!canWrite) return <Navigate to="/" />;

	const save = async (data: CreateSession) => {
		const result = await saveSession(data);
		navigate({ to: `/games/${result.gameId}` });
	};

	if (games !== undefined && games.length === 0) {
		return (
			<BgtEmptyPage
				header={t("player-session.title-new")}
				icon={Game}
				title={t("dashboard.empty.title")}
				description={t("dashboard.empty.description")}
				action={{
					label: t("dashboard.empty.button"),
					onClick: () => navigate({ to: "/games" }),
				}}
			/>
		);
	}

	return (
		<SessionForm
			start={addMinutes(new Date(), -30)}
			buttonText={t("player-session.save-new")}
			onClick={save}
			disabled={isPending}
			title={t("player-session.title-new")}
		/>
	);
}
