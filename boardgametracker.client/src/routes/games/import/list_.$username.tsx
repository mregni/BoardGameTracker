import { noop } from "@tanstack/react-query";
import { createFileRoute, redirect } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { RequireWrite } from "@/routes/-components/RequireWrite";
import { getBggCollection, getGames } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { ImportList } from "./-components/ImportList";

export const Route = createFileRoute("/games/import/list_/$username")({
	component: WriteRouteComponent,
	beforeLoad: async ({ context: { queryClient } }) => {
		const settings = await queryClient.query({ ...getSettings(), staleTime: "static" });
		if (!settings.bggStatus?.isConfigured) {
			throw redirect({ to: "/games" });
		}
	},
	loader: ({ params, context: { queryClient } }) => {
		queryClient.query(getBggCollection(params.username)).catch(noop);
		queryClient.query(getGames()).catch(noop);
		queryClient.query(getSettings()).catch(noop);
	},
});

function RouteComponent() {
	const { username } = Route.useParams();
	const { t } = useTranslation("bgg-import");

	return <ImportList collectionQuery={getBggCollection(username)} header={t("title")} introKey="games:import.intro" />;
}

function WriteRouteComponent() {
	return (
		<RequireWrite>
			<RouteComponent />
		</RequireWrite>
	);
}
