import { noop } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import type { Game } from "@/models";
import { RequireWrite } from "@/routes/-components/RequireWrite";
import { getSettings } from "@/services/queries/settings";
import { GameForm } from "./-components/GameForm";
import { useNewGame } from "./-hooks/useNewGame";

export const Route = createFileRoute("/games/new")({
	component: WriteRouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.query(getSettings()).catch(noop);
	},
});

function RouteComponent() {
	const navigate = useNavigate();
	const { t } = useTranslation("game");

	const onSuccess = (game: Game) => {
		void navigate({ to: `/games/${game.id}` });
		window.scrollTo(0, 0);
	};

	const { saveGame, isLoading } = useNewGame({ onSuccess });
	return (
		<GameForm
			buttonText={t("new.save")}
			title={t("new.manual.title")}
			backTo="/games/add"
			onClick={saveGame}
			disabled={isLoading}
		/>
	);
}

function WriteRouteComponent() {
	return (
		<RequireWrite>
			<RouteComponent />
		</RequireWrite>
	);
}
