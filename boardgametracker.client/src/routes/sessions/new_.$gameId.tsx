import { noop } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { addMinutes } from "date-fns";
import { useTranslation } from "react-i18next";
import AlertTriangle from "@/assets/icons/alert-triangle.svg?react";
import { BgtEmptyState } from "@/components/BgtLayout/BgtEmptyState";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { usePermissions } from "@/hooks/usePermissions";
import type { CreateSession } from "@/models";
import { RequireWrite } from "@/routes/-components/RequireWrite";
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
		queryClient.query(getGame(params.gameId)).catch(noop);
		queryClient.query(getGames()).catch(noop);
		queryClient.query(getLocations()).catch(noop);
		queryClient.query(getPlayers()).catch(noop);
	},
});

function RouteComponent() {
	const { gameId } = Route.useParams();
	const navigate = useNavigate();
	const { t } = useTranslation(["player-session", "game"]);
	const { canWrite } = usePermissions();

	const { game, isLoading, error, retry, isPending, saveSession } = useNewSessionWithGameData({ gameId });

	if (!canWrite) return <RequireWrite>{null}</RequireWrite>;

	const save = async (data: CreateSession) => {
		const result = await saveSession(data);
		navigate({ to: `/games/${result.gameId}` });
	};

	if (game === undefined) {
		return (
			<BgtPage>
				<BgtPageContent
					isLoading={isLoading}
					data={{ game }}
					error={error}
					onRetry={retry}
					notFound={
						<BgtEmptyState
							icon={AlertTriangle}
							title={t("game:notifications.not-found")}
							description={t("game:load-error.not-found-description")}
							action={{ label: t("game:load-error.back"), onClick: () => navigate({ to: "/games" }) }}
						/>
					}
				>
					{() => null}
				</BgtPageContent>
			</BgtPage>
		);
	}

	return (
		<SessionForm
			game={game}
			minutes={game.maxPlayTime ?? 30}
			start={addMinutes(new Date(), -(game?.maxPlayTime ?? 30))}
			buttonText={t("save-new")}
			onClick={save}
			disabled={isLoading || isPending}
			title={t("title-new")}
			backTo={`/games/${gameId}`}
		/>
	);
}
