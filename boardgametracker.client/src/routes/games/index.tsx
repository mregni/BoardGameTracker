import { noop, useQuery } from "@tanstack/react-query";
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
import { usePermissions } from "@/hooks/usePermissions";
import { getGames, getTrackedPrices } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { NoGamesEmptyPage } from "../-components/NoGamesEmptyPage";
import { ActiveFilterChips, filterGames, type GamesFilterSearch, GamesFilters } from "./-components/GamesFilters";
import { TrackedPriceIcon } from "./-components/TrackedPriceIcon";
import { useDebouncedSearchQuery } from "./-hooks/useDebouncedSearchQuery";
import { useGamesData } from "./-hooks/useGamesData";
import { filterByTitle, hasGridFilters, parseGamesSearch, withoutUndefined } from "./-utils/gamesSearch";

export const Route = createFileRoute("/games/")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.query(getGames()).catch(noop);
		queryClient.query(getSettings()).catch(noop);
	},
	validateSearch: (search: Record<string, unknown>): GamesFilterSearch => parseGamesSearch(search),
});

function RouteComponent() {
	const search = Route.useSearch();
	const { q, category, players, playTime, weight, age } = search;
	const { t } = useTranslation(["games", "common"]);
	const navigate = useNavigate();
	const { games, isLoading, isError, retry } = useGamesData();
	const { canWrite } = usePermissions();
	const [showFilters, setShowFilters] = useState(false);

	const categories = useMemo(
		() => [...new Set(games.flatMap((game) => game.categories.map((cat) => cat.name)))].sort(),
		[games],
	);

	const updateSearch = useCallback(
		(partial: Partial<GamesFilterSearch>) => {
			navigate({
				to: "/games",
				search: (prev) => withoutUndefined({ ...prev, ...partial }),
				replace: true,
			});
		},
		[navigate],
	);

	const [filterValue, setFilterValue] = useDebouncedSearchQuery(q, (value) => updateSearch({ q: value }));

	const filteredGames = useMemo(() => {
		let result = filterByTitle(games, q);
		if (category !== undefined) {
			result = result.filter((game) => game.categories.some((cat) => cat.name === category));
		}
		return filterGames(result, { playerCount: players, maxPlayTime: playTime, weight, age });
	}, [games, q, category, players, playTime, weight, age]);

	const activeFilterCount = [category, players, playTime, weight, age].filter((value) => value !== undefined).length;

	const clearFilters = () => {
		setFilterValue("");
		navigate({ to: "/games", search: {}, replace: true });
	};
	const settingsQuery = useQuery(getSettings());
	const trackedPricesQuery = useQuery({
		...getTrackedPrices(),
		enabled: !!settingsQuery.data?.changeDetectionStatus?.isConfigured,
	});
	const priceMap = useMemo(
		() => new Map((trackedPricesQuery.data ?? []).map((price) => [price.gameId, price])),
		[trackedPricesQuery.data],
	);

	if (isLoading) return null;

	if (isError) {
		return (
			<BgtEmptyPage
				header={t("title")}
				icon={Game}
				title={t("load-error")}
				action={{ label: t("common:retry"), onClick: () => retry() }}
			/>
		);
	}

	if (games.length === 0) {
		return <NoGamesEmptyPage header={t("title")} icon={Game} />;
	}

	return (
		<BgtPage>
			<BgtPageHeader
				header={t("title")}
				icon={Game}
				actions={[
					{
						onClick: () => navigate({ to: "/games/table", search: q ? { q } : {} }),
						variant: "cancel",
						content: "games:view.table",
					},
					...(canWrite
						? [
								{
									onClick: () => navigate({ to: "/games/add" }),
									variant: "primary" as const,
									content: "games:new",
								},
							]
						: []),
				]}
			></BgtPageHeader>
			<BgtPageContent>
				<div className="flex flex-col md:flex-row md:flex-wrap md:items-center gap-2">
					<div className="w-full md:flex-1 md:min-w-[8rem]">
						<SearchInputField value={filterValue} onChange={(event) => setFilterValue(event.target.value)} />
					</div>
					<button
						type="button"
						onClick={() => setShowFilters((value) => !value)}
						className="md:hidden flex items-center justify-center gap-1 w-full text-sm text-primary-text hover:text-indigo-300 cursor-pointer"
					>
						{showFilters ? t("filters.show-less") : t("filters.show-more")}
						{!showFilters && activeFilterCount > 0 && ` · ${activeFilterCount}`}
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
								filters={{ playerCount: players, maxPlayTime: playTime, weight, age }}
								onCategoryChange={(value) => updateSearch({ category: value })}
								onChange={(next) =>
									updateSearch({
										players: next.playerCount,
										playTime: next.maxPlayTime,
										weight: next.weight,
										age: next.age,
									})
								}
							/>
						</div>
					</div>
				</div>
				<div className="flex flex-wrap items-center gap-2 pb-6" aria-live="polite">
					{filteredGames.length > 0 ? (
						<BgtText size="3" color="primary" weight="medium">
							{t("count", { count: filteredGames.length })}
						</BgtText>
					) : (
						<BgtText size="3" color="white" opacity={70}>
							{t("filters.no-match")}
						</BgtText>
					)}
					<ActiveFilterChips search={search} onRemove={(key) => updateSearch({ [key]: undefined })} />
					{hasGridFilters(search) && (
						<button
							type="button"
							onClick={clearFilters}
							className="text-sm text-primary-text hover:text-indigo-300 underline-offset-2 hover:underline cursor-pointer"
						>
							{t("filters.clear")}
						</button>
					)}
				</div>
				<BgtCardList>
					{filteredGames.map((x) => (
						<BgtImageCard
							key={x.id}
							title={x.title}
							image={x.image}
							state={x.state}
							isLoaned={x.isLoaned}
							link={`/games/${x.id}`}
							badge={
								x.changeDetectionWatchId ? (
									<TrackedPriceIcon livePrice={priceMap.get(x.id)} variant="overlay" />
								) : undefined
							}
						/>
					))}
				</BgtCardList>
			</BgtPageContent>
		</BgtPage>
	);
}
