import { useQuery } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useCallback, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import CaretDownIcon from "@/assets/icons/caret-down.svg?react";
import CaretUpIcon from "@/assets/icons/caret-up.svg?react";
import Game from "@/assets/icons/gamepad.svg?react";
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
import CreateGameModal from "@/routes/games/-modals/CreateGameModal";
import { getGames } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { filterGames, type GamesFilterSearch, GamesFilters, type WeightBucket } from "./-components/GamesFilters";
import { useGameModals } from "./-hooks/useGameModals";
import { useGamesData } from "./-hooks/useGamesData";

const WEIGHT_BUCKETS: WeightBucket[] = ["light", "medium", "heavy"];

const parsePositiveInt = (value: unknown): number | undefined => {
	const parsed = Number(value);
	return Number.isInteger(parsed) && parsed > 0 ? parsed : undefined;
};

export const Route = createFileRoute("/games/")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getGames());
		queryClient.prefetchQuery(getSettings());
	},
	validateSearch: (search: Record<string, unknown>): GamesFilterSearch => {
		const weight = search.weight as WeightBucket;
		return {
			category: search.category ? (search.category as string) : undefined,
			players: parsePositiveInt(search.players),
			playTime: parsePositiveInt(search.playTime),
			weight: WEIGHT_BUCKETS.includes(weight) ? weight : undefined,
		};
	},
});

function RouteComponent() {
	const search = Route.useSearch();
	const { category, players, playTime, weight } = search;
	const { t } = useTranslation(["games", "dashboard", "common"]);
	const navigate = useNavigate();
	const { games, isLoading } = useGamesData();
	const { canWrite } = usePermissions();
	const modals = useGameModals();
	const [showFilters, setShowFilters] = useState(false);

	const settingsQuery = useQuery(getSettings());
	const bggEnabled = settingsQuery.data?.bggStatus?.isConfigured ?? false;

	const categories = useMemo(
		() => [...new Set(games.flatMap((game) => game.categories.map((cat) => cat.name)))].sort(),
		[games],
	);

	const updateSearch = useCallback(
		(partial: Partial<GamesFilterSearch>) => {
			navigate({
				to: "/games",
				search: (prev) => {
					const next = { ...prev, ...partial };
					for (const key of Object.keys(next) as (keyof GamesFilterSearch)[]) {
						if (next[key] === undefined) delete next[key];
					}
					return next;
				},
			});
		},
		[navigate],
	);

	const categoryPreFilter = useCallback(
		(items: typeof games) => {
			let result = items;
			if (category !== undefined) {
				result = result.filter((game) => game.categories.some((cat) => cat.name === category));
			}
			return filterGames(result, { playerCount: players, maxPlayTime: playTime, weight });
		},
		[category, players, playTime, weight],
	);

	const { filterValue, setFilterValue, filtered: filteredGames } = useFilteredList(games, "title", categoryPreFilter);

	const openManual = () => {
		modals.createModal.hide();
		navigate({ to: "/games/new" });
	};

	const openBgg = () => {
		modals.createModal.hide();
		navigate({ to: "/games/bgg" });
	};

	if (isLoading) return null;

	if (games.length === 0) {
		return (
			<BgtEmptyPage
				header={t("title")}
				icon={Game}
				title={t("dashboard:empty.title")}
				description={t("dashboard:empty.description")}
				action={canWrite ? { onClick: modals.createModal.show, label: t("new") } : undefined}
			>
				<CreateGameModal
					open={modals.createModal.isOpen}
					close={modals.createModal.hide}
					bggEnabled={bggEnabled}
					openBgg={openBgg}
					openManual={openManual}
				/>
			</BgtEmptyPage>
		);
	}

	return (
		<BgtPage>
			<BgtPageHeader
				header={t("title")}
				icon={Game}
				actions={
					canWrite
						? [
								{
									onClick: modals.createModal.show,
									variant: "primary",
									content: "games:new",
								},
							]
						: []
				}
			></BgtPageHeader>
			<BgtPageContent>
				<div className="flex flex-col md:flex-row md:flex-wrap md:items-center gap-2">
					<div className="w-full md:flex-1 md:min-w-[8rem]">
						<SearchInputField value={filterValue} onChange={(event) => setFilterValue(event.target.value)} />
					</div>
					<button
						type="button"
						onClick={() => setShowFilters((value) => !value)}
						className="md:hidden flex items-center justify-center gap-1 w-full text-sm text-primary hover:text-primary/80 cursor-pointer"
					>
						{showFilters ? t("filters.show-less") : t("filters.show-more")}
						{showFilters ? <CaretUpIcon className="size-4" /> : <CaretDownIcon className="size-4" />}
					</button>
					<div
						className={cx(
							"grid w-full transition-[grid-template-rows] duration-300 ease-in-out md:contents",
							showFilters ? "grid-rows-[1fr]" : "grid-rows-[0fr]",
						)}
					>
						<div className="overflow-hidden flex flex-col gap-2 md:contents">
							<GamesFilters
								categories={categories}
								category={category}
								filters={{ playerCount: players, maxPlayTime: playTime, weight }}
								onCategoryChange={(value) => updateSearch({ category: value })}
								onChange={(next) =>
									updateSearch({ players: next.playerCount, playTime: next.maxPlayTime, weight: next.weight })
								}
							/>
						</div>
					</div>
				</div>
				<BgtText size="3" color="primary" className="pb-6" weight="medium">
					{t("count", { count: filteredGames.length })}
				</BgtText>
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
				<CreateGameModal
					open={modals.createModal.isOpen}
					close={modals.createModal.hide}
					bggEnabled={bggEnabled}
					openBgg={openBgg}
					openManual={openManual}
				/>
			</BgtPageContent>
		</BgtPage>
	);
}
