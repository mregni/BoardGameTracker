import { noop } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Users from "@/assets/icons/users.svg?react";
import { SearchInputField } from "@/components/BgtForm";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtText } from "@/components/BgtText/BgtText";
import { BgtTileGrid } from "@/components/BgtTile/BgtTileGrid";
import { useFilteredList } from "@/hooks/useFilteredList";
import { usePermissions } from "@/hooks/usePermissions";
import { getPlayersOverview } from "@/services/queries/players";
import { PlayerTile } from "./-components/PlayerTile";
import { usePlayersData } from "./-hooks/usePlayersData";

export const Route = createFileRoute("/players/")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.query(getPlayersOverview()).catch(noop);
	},
});

function RouteComponent() {
	const { t } = useTranslation(["player", "common"]);
	const { canWrite } = usePermissions();
	const navigate = useNavigate();
	const { players, isLoading } = usePlayersData();
	const { filterValue, setFilterValue, filtered: filteredPlayers } = useFilteredList(players, "name");

	if (isLoading) return null;

	if (players.length === 0) {
		return (
			<BgtEmptyPage
				header={t("common:players")}
				icon={Users}
				title={t("empty.title")}
				description={t("empty.description")}
				action={canWrite ? { label: t("new.button"), onClick: () => navigate({ to: "/players/new" }) } : undefined}
			/>
		);
	}

	return (
		<BgtPage>
			<BgtPageHeader
				header={t("common:players")}
				icon={Users}
				actions={
					canWrite
						? [
								{
									content: "player:new.button",
									variant: "primary",
									onClick: () => navigate({ to: "/players/new" }),
								},
							]
						: []
				}
			></BgtPageHeader>
			<BgtPageContent>
				<div className="w-full md:w-64 xl:w-[300px]">
					<SearchInputField value={filterValue} onChange={(event) => setFilterValue(event.target.value)} />
				</div>
				<BgtText size="3" className="pb-6 text-primary-text" weight="medium">
					{t("count", { count: filteredPlayers.length })}
				</BgtText>
				<BgtTileGrid>
					{filteredPlayers.map((x) => (
						<PlayerTile key={x.id} player={x} />
					))}
				</BgtTileGrid>
			</BgtPageContent>
		</BgtPage>
	);
}
