import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import type { CreateGame, Game } from "@/models";
import { getSettings } from "@/services/queries/settings";

import { gameIdParamSchema } from "@/utils/routeSchemas";
import { GameForm } from "./-components/GameForm";
import { useUpdateGame } from "./-hooks/useUpdateGame";

export const Route = createFileRoute("/games/$gameId_/update")({
	component: RouteComponent,
	params: gameIdParamSchema,
	loader: async ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getSettings());
	},
});

function RouteComponent() {
	const { gameId } = Route.useParams();
	const navigate = useNavigate();
	const { t } = useTranslation();

	const onSuccess = () => {
		navigate({ to: `/games/${gameId}` });
		window.scrollTo(0, 0);
	};

	const { game, updateGame, isLoading } = useUpdateGame({ gameId, onSuccess });

	if (game === undefined) return null;

	const save = async (data: CreateGame) => {
		const updatedGame: Game = {
			...game,
			...data,
			image: data.image ?? game.image,
			additionDate: data.additionDate ? data.additionDate.toISOString() : null,
		};
		const result = await updateGame(updatedGame);

		navigate({ to: `/games/${result.id}` });
	};

	return (
		<GameForm
			game={game}
			buttonText={t("game.update.save")}
			title={t("game.update.title")}
			onClick={save}
			disabled={isLoading}
		/>
	);
}
