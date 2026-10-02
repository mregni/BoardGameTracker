import { createFileRoute, Navigate, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { BgtDataGuard } from "@/components/BgtDataGuard/BgtDataGuard";
import { usePermissions } from "@/hooks/usePermissions";
import type { CreateSession, Session } from "@/models";
import { getGame } from "@/services/queries/games";
import { getSession } from "@/services/queries/sessions";
import { sessionIdParamSchema } from "@/utils/routeSchemas";
import { SessionForm } from "./-components/SessionForm";
import { useUpdateSessionData } from "./-hooks/useUpdateSessionData";

export const Route = createFileRoute("/sessions/update_/$sessionId")({
	component: RouteComponent,
	params: sessionIdParamSchema,
	loader: async ({ params, context: { queryClient } }) => {
		const data = await queryClient.fetchQuery(getSession(params.sessionId));
		queryClient.prefetchQuery(getGame(data.gameId));
	},
});

function RouteComponent() {
	const { sessionId } = Route.useParams();
	const { t } = useTranslation("player-session");
	const navigate = useNavigate();
	const { canWrite } = usePermissions();

	const { game, session, updateSession, isPending, isLoading } = useUpdateSessionData({
		sessionId,
	});

	if (!canWrite) return <Navigate to="/" />;

	const save = async (data: CreateSession) => {
		if (session === undefined) return;
		const updatedSession = { ...data } as Session;
		updatedSession.id = session.id;
		const result = await updateSession(updatedSession);
		navigate({ to: `/games/${result.gameId}/sessions` });
	};

	return (
		<BgtDataGuard isLoading={isLoading} data={{ game, session }}>
			{({ game, session }) => (
				<SessionForm
					game={game}
					minutes={session.minutes}
					start={session.start}
					comment={session.comment}
					locationId={session.locationId}
					playerSessions={session.playerSessions}
					expansions={session.expansions}
					buttonText={t("save-update")}
					onClick={save}
					disabled={isPending}
					title={t("title-update")}
				/>
			)}
		</BgtDataGuard>
	);
}
