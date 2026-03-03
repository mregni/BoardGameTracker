import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useCallback, useState } from "react";
import { useTranslation } from "react-i18next";
import Game from "@/assets/icons/gamepad.svg?react";
import { BgtBadge } from "@/components/BgtBadge/BgtBadge";
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
import { BggGameModal } from "@/routes/games/-modals/BggGameModal";
import CreateGameModal from "@/routes/games/-modals/CreateGameModal";
import { getGames } from "@/services/queries/games";
import { useGameModals } from "./-hooks/useGameModals";
import { useGamesData } from "./-hooks/useGamesData";

type GamesFilterSearch = {
	category?: string;
};

export const Route = createFileRoute("/games/")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getGames());
	},
	validateSearch: (search: Record<string, unknown>): GamesFilterSearch => {
		return {
			category: search.category ? (search.category as string) : undefined,
		};
	},
});

function RouteComponent() {
	const { category } = Route.useSearch();
	const { t } = useTranslation();
	const navigate = useNavigate();
	const { games, isLoading } = useGamesData();
	const { canWrite } = usePermissions();
	const modals = useGameModals();
	const [categoryFilter, setCategoryFilter] = useState<string | undefined>(category);

	const categoryPreFilter = useCallback(
		(items: typeof games) => {
			if (categoryFilter === undefined) return items;
			return items.filter((game) => game.categories.some((cat) => cat.name === categoryFilter));
		},
		[categoryFilter],
	);

	const { filterValue, setFilterValue, filtered: filteredGames } = useFilteredList(games, "title", categoryPreFilter);

	const openManual = () => {
		modals.createModal.hide();
		navigate({ to: "/games/new" });
	};

	const openBgg = () => {
		modals.createModal.hide();
		modals.bggModal.show();
	};

	if (isLoading) return null;

	if (games.length === 0) {
		return (
			<BgtEmptyPage
				header={t("games.title")}
				icon={Game}
				title={t("dashboard.empty.title")}
				description={t("dashboard.empty.description")}
				action={canWrite ? { onClick: modals.createModal.show, label: t("games.new") } : undefined}
			>
				<BggGameModal open={modals.bggModal.isOpen} close={modals.bggModal.hide} />
				<CreateGameModal
					open={modals.createModal.isOpen}
					close={modals.createModal.hide}
					openBgg={openBgg}
					openManual={openManual}
				/>
			</BgtEmptyPage>
		);
	}

	return (
		<BgtPage>
			<BgtPageHeader
				header={t("games.title")}
				icon={Game}
				actions={
					canWrite
						? [
								{
									onClick: modals.createModal.show,
									variant: "primary",
									content: "games.new",
								},
							]
						: []
				}
			></BgtPageHeader>
			<BgtPageContent>
				<div className="flex flex-row gap-3">
					<SearchInputField value={filterValue} onChange={(event) => setFilterValue(event.target.value)} />
				</div>
				<BgtText size="3" color="primary" className="pb-6" weight="medium">
					{t("games.count", { count: filteredGames.length })}
				</BgtText>
				{categoryFilter !== undefined && (
					<div className="flex flex-row gap-2 items-center text-sm text-gray-400">
						<div>{t("common.filter")}:</div>
						<BgtBadge color="green" variant="soft" onClose={() => setCategoryFilter(undefined)}>
							{categoryFilter}
						</BgtBadge>
					</div>
				)}
				<BgtCardList>
					{filteredGames.map((x) => (
						<BgtImageCard
							key={x.id}
							title={x.title}
							image={x.image}
							state={x.state}
							isLoaned={x.isLoaned}
							link={`/games/${x.id}`}
						/>
					))}
				</BgtCardList>
				<BggGameModal open={modals.bggModal.isOpen} close={modals.bggModal.hide} />
				<CreateGameModal
					open={modals.createModal.isOpen}
					close={modals.createModal.hide}
					openBgg={openBgg}
					openManual={openManual}
				/>
			</BgtPageContent>
		</BgtPage>
	);
}
