import { createFileRoute, Navigate, useNavigate } from "@tanstack/react-router";
import { usePermissions } from "@/hooks/usePermissions";
import { addMinutes } from "date-fns";
import { useTranslation } from "react-i18next";
import type { CreateSession } from "@/models";
import { getGame, getGames } from "@/services/queries/games";
import { getLocations } from "@/services/queries/locations";
import { getPlayers } from "@/services/queries/players";
import { gameIdParamSchema } from "@/utils/routeSchemas";
import { SessionForm } from "./-components/SessionForm";
import { useNewSessionWithGameData } from "./-hooks/useNewSessionWithGameData";

export const Route = createFileRoute("/sessions/new_/$gameId")({
	component: RouteComponent,
	params: gameIdParamSchema,
	loader: async ({ params, context: { queryClient } }) => {
		queryClient.prefetchQuery(getGame(params.gameId));
		queryClient.prefetchQuery(getGames());
		queryClient.prefetchQuery(getLocations());
		queryClient.prefetchQuery(getPlayers());
	},
});

function RouteComponent() {
	const { gameId } = Route.useParams();
	const navigate = useNavigate();
	const { t } = useTranslation();
	const { canWrite } = usePermissions();

	if (!canWrite) return <Navigate to="/" />;
	const { game, isLoading, isPending, saveSession } = useNewSessionWithGameData({ gameId });

	const save = async (data: CreateSession) => {
		const result = await saveSession(data);
		navigate({ to: `/games/${result.gameId}` });
	};

	if (isLoading || game === undefined) return null;

	return (
		<SessionForm
			game={game}
			minutes={game.maxPlayTime ?? 30}
			start={addMinutes(new Date(), -(game?.maxPlayTime ?? 30))}
			buttonText={t("player-session.save-new")}
			onClick={save}
			disabled={isLoading || isPending}
			title={t("player-session.title-new")}
		/>
	);
}
