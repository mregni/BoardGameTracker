import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Users from "@/assets/icons/users.svg?react";
import { SearchInputField } from "@/components/BgtForm";
import { BgtImageCard } from "@/components/BgtImageCard/BgtImageCard";
import { BgtCardList } from "@/components/BgtLayout/BgtCardList";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtText } from "@/components/BgtText/BgtText";
import { useFilteredList } from "@/hooks/useFilteredList";
import { usePermissions } from "@/hooks/usePermissions";
import { getPlayers } from "@/services/queries/players";
import { usePlayersData } from "./-hooks/usePlayersData";

export const Route = createFileRoute("/players/")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getPlayers());
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
				<BgtText size="3" className="pb-6 text-primary" weight="medium">
					{t("count", { count: filteredPlayers.length })}
				</BgtText>
				<BgtCardList>
					{filteredPlayers.map((x) => (
						<BgtImageCard key={x.id} title={x.name} image={x.image} link={`/players/${x.id}`} />
					))}
				</BgtCardList>
			</BgtPageContent>
		</BgtPage>
	);
}
