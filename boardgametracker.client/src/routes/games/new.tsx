import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import type { Game } from "@/models";
import type { CreateGame } from "@/models/Games/CreateGame";

import { getSettings } from "@/services/queries/settings";
import { GameForm } from "./-components/GameForm";
import { useNewGame } from "./-hooks/useNewGame";

export const Route = createFileRoute("/games/new")({
	component: RouteComponent,
	loader: async ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getSettings());
	},
});

function RouteComponent() {
	const navigate = useNavigate();
	const { t } = useTranslation();

	const onSuccess = (game: Game) => {
		navigate({ to: `/games/${game.id}` });
		window.scrollTo(0, 0);
	};

	const { saveGame, isLoading } = useNewGame({ onSuccess });
	const save = async (game: CreateGame) => {
		const result = await saveGame(game);
		navigate({ to: `/games/${result.id}` });
	};

	return (
		<GameForm buttonText={t("game.new.save")} title={t("game.new.manual.title")} onClick={save} disabled={isLoading} />
	);
}
