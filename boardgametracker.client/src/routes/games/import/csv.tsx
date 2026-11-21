import { useQuery } from "@tanstack/react-query";
import { createFileRoute, redirect } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { BgtStatus } from "@/components/BgtStatus/BgtStatus";
import { RequireWrite } from "@/routes/-components/RequireWrite";
import { csvImportGamesKey, getCsvImportGames, getCsvImportRejected } from "@/services/queries/games";
import { ImportList } from "./-components/ImportList";

export const Route = createFileRoute("/games/import/csv")({
	component: WriteRouteComponent,
	beforeLoad: ({ context: { queryClient } }) => {
		if (!queryClient.getQueryData(csvImportGamesKey)) {
			throw redirect({ to: "/games/import/start" });
		}
	},
});

function RouteComponent() {
	const { t } = useTranslation("games");
	const { data: rejected = [] } = useQuery(getCsvImportRejected());

	return (
		<ImportList collectionQuery={getCsvImportGames()} header={t("import.csv.title")} introKey="games:import.csv.intro">
			{rejected.length > 0 && (
				<div className="mb-8">
					<BgtStatus
						variant="warning"
						title={t("import.csv.rejected-title", { count: rejected.length })}
						description={
							<ul className="list-disc pl-5">
								{rejected.map((row) => (
									<li key={row.line}>
										{t("import.csv.rejected-row", {
											line: row.line,
											title: row.title || row.bggId || "-",
											reason: t(`import.csv.reason.${row.reason}`),
										})}
									</li>
								))}
							</ul>
						}
					/>
				</div>
			)}
		</ImportList>
	);
}

function WriteRouteComponent() {
	return (
		<RequireWrite>
			<RouteComponent />
		</RequireWrite>
	);
}
