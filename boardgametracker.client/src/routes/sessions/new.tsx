import { noop } from "@tanstack/react-query";
import { createFileRoute, Navigate, useNavigate } from "@tanstack/react-router";
import { addMinutes } from "date-fns";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import Game from "@/assets/icons/gamepad.svg?react";
import { usePermissions } from "@/hooks/usePermissions";
import type { CreateSession } from "@/models";
import { getGames } from "@/services/queries/games";
import { getLocations } from "@/services/queries/locations";
import { getPlayers } from "@/services/queries/players";
import { NoGamesEmptyPage } from "../-components/NoGamesEmptyPage";
import { SessionForm } from "./-components/SessionForm";
import { useNewSessionData } from "./-hooks/useNewSessionData";

const newSessionSearchSchema = z.object({
	player: z.number().int().positive().optional().catch(undefined),
});

export const Route = createFileRoute("/sessions/new")({
	component: RouteComponent,
	validateSearch: newSessionSearchSchema,
	loader: ({ context: { queryClient } }) => {
		queryClient.query(getGames()).catch(noop);
		queryClient.query(getPlayers()).catch(noop);
		queryClient.query(getLocations()).catch(noop);
	},
});

function RouteComponent() {
	const navigate = useNavigate();
	const { player } = Route.useSearch();
	const { t } = useTranslation("player-session");
	const { canWrite } = usePermissions();
	const { isPending, isLoading, saveSession, games } = useNewSessionData();

	if (!canWrite) return <Navigate to="/" />;

	const save = async (data: CreateSession) => {
		const result = await saveSession(data);
		navigate({ to: `/games/${result.gameId}` });
	};

	if (!isLoading && games.length === 0) {
		return <NoGamesEmptyPage header={t("title-new")} icon={Game} />;
	}

	return (
		<SessionForm
			start={addMinutes(new Date(), -30)}
			playerSessions={player ? [{ playerId: player, won: false, firstPlay: false, score: null }] : undefined}
			buttonText={t("save-new")}
			onClick={save}
			disabled={isPending}
			title={t("title-new")}
			backTo="/"
		/>
	);
}
