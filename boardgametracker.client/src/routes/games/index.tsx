import { noop, useQuery } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useCallback, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import CaretDownIcon from "@/assets/icons/caret-down.svg?react";
import CaretUpIcon from "@/assets/icons/caret-up.svg?react";
import Game from "@/assets/icons/dices.svg?react";
import { BgtSimpleSelect, SearchInputField } from "@/components/BgtForm";
import { BgtImageCard } from "@/components/BgtImageCard/BgtImageCard";
import { BgtCardList } from "@/components/BgtLayout/BgtCardList";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtText } from "@/components/BgtText/BgtText";
import { useDebouncedSearchQuery } from "@/hooks/useDebouncedSearchQuery";
import { useMediaQuery } from "@/hooks/useMediaQuery";
import { usePermissions } from "@/hooks/usePermissions";
import { useStorageData } from "@/hooks/useStorage";
import { GameState, StorageLayout } from "@/models";
import { getGames, getTrackedPrices } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { toRelative } from "@/utils/dateUtils";
import { allPlaces, itemsIn, needsAPlace, placeKey, placeLabel, placeNames, storageItems } from "@/utils/storage";
import { NoGamesEmptyPage } from "../-components/NoGamesEmptyPage";
import { ExpansionQuickView } from "./-components/ExpansionQuickView";
import {
	ActiveFilterChips,
	type GameSort,
	type GamesFilterSearch,
	GamesFilters,
	NO_STORAGE,
} from "./-components/GamesFilters";
import { GamesTableView } from "./-components/GamesTableView";
import { TrackedPriceIcon } from "./-components/TrackedPriceIcon";
import { useGamesData } from "./-hooks/useGamesData";
import { buildCards, findGames, storageGoal } from "./-utils/findGames";
import { GAME_SORTS, hasActiveFilters, isPlayable, parseGamesSearch, withoutUndefined } from "./-utils/gamesSearch";

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
	const { view, q, category, players, playTime, weight, age, state, language, inStock, storage, type, sort } = search;
	const { t } = useTranslation(["games", "common"]);
	const navigate = useNavigate({ from: Route.fullPath });
	const { games, isLoading, isError, retry } = useGamesData();
	const { canWrite } = usePermissions();
	const [showFilters, setShowFilters] = useState(false);
	const [typeSwitched, setTypeSwitched] = useState(false);
	const [quickView, setQuickView] = useState<{ gameId: number; expansionId: number } | null>(null);
	const { nodes, locations } = useStorageData();
	const isDesktop = useMediaQuery("(min-width: 48rem)");
	const filtersCollapsed = !showFilters && !isDesktop;

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

	const settingsQuery = useQuery(getSettings());
	const showLivePrices = settingsQuery.data?.changeDetectionStatus?.isConfigured ?? false;
	const uiLanguage = settingsQuery.data?.uiLanguage ?? "en-US";
	const trackedPricesQuery = useQuery({ ...getTrackedPrices(), enabled: showLivePrices });
	const priceMap = useMemo(
		() => new Map((trackedPricesQuery.data ?? []).map((price) => [price.gameId, price])),
		[trackedPricesQuery.data],
	);

	const storageFilterItems = useMemo(() => {
		const stored = storageItems(games);
		return [
			{
				value: NO_STORAGE,
				label: `${t("filters.storage.none")} (${needsAPlace(games).length})`,
				triggerLabel: t("filters.storage.none"),
			},
			...allPlaces(nodes, locations)
				.filter((place) => nodes.find((node) => node.id === place.storageLocationId)?.layout !== StorageLayout.Slot)
				.map((place) => ({
					value: placeKey(place),
					label: `${placeLabel(place, nodes, locations, t)} (${itemsIn(stored, place, nodes).length})`,
					triggerLabel: placeNames(place, nodes, locations, t).at(-1),
				})),
		];
	}, [games, nodes, locations, t]);
	const target = storageGoal(storage);
	let storageName: string | undefined;
	if (storage === NO_STORAGE) storageName = t("filters.storage.none");
	else if (target) storageName = placeLabel(target, nodes, locations, t);

	const found = useMemo(
		() =>
			findGames(
				games,
				{ q, category, players, playTime, weight, age, state, language, storage, sort },
				nodes,
				(game) => !inStock || !showLivePrices || priceMap.get(game.id)?.inStock === true,
			),
		[
			games,
			q,
			category,
			players,
			playTime,
			weight,
			age,
			state,
			language,
			inStock,
			showLivePrices,
			priceMap,
			sort,
			storage,
			nodes,
		],
	);
	const { filteredGames, dimmed } = found;

	const cards = buildCards(found, type, sort);
	const shownCount = view === "table" ? filteredGames.length - dimmed.size : cards.length;
	const expansionCount = view === "table" ? 0 : cards.filter((card) => card.expansion).length;
	const quickViewGame = games.find((game) => game.id === quickView?.gameId);
	const quickViewExpansion = quickViewGame?.expansions.find((expansion) => expansion.id === quickView?.expansionId);

	const changeFilters = (partial: Partial<GamesFilterSearch>) => {
		if ("type" in partial) setTypeSwitched(false);
		if (partial.storage && partial.storage !== NO_STORAGE && type === undefined && view !== "table") {
			setTypeSwitched(true);
			updateSearch({ ...partial, type: "any" });
			return;
		}
		updateSearch(partial);
	};

	const activeFilterCount = [category, players, playTime, weight, age, state, language, inStock, storage, type].filter(
		(value) => value !== undefined,
	).length;

	const clearFilters = () => {
		setFilterValue("");
		navigate({ to: "/games", search: withoutUndefined({ view, sort }), replace: true });
	};

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

	const sortItems = GAME_SORTS.map((value) => ({ value, label: t(`sort.${value}`) }));
	const viewButton = (target: GamesFilterSearch["view"], label: string) => (
		<button
			type="button"
			aria-pressed={view === target}
			onClick={() => updateSearch({ view: target })}
			className={cx(
				"px-3 h-11 md:h-9 text-sm font-bold cursor-pointer",
				view === target ? "bg-primary/60 text-white" : "text-cancel hover:bg-cancel/10",
			)}
		>
			{label}
		</button>
	);

	return (
		<BgtPage>
			<BgtPageHeader
				header={t("title")}
				icon={Game}
				actions={
					canWrite
						? [{ onClick: () => navigate({ to: "/games/add" }), variant: "primary" as const, content: "games:new" }]
						: []
				}
			></BgtPageHeader>
			<BgtPageContent>
				<div className="flex flex-col gap-2">
					<div className="flex items-center gap-2">
						<SearchInputField value={filterValue} onChange={(event) => setFilterValue(event.target.value)} />
						<button
							type="button"
							onClick={() => setShowFilters((value) => !value)}
							aria-expanded={showFilters}
							aria-controls="games-filters"
							className="md:hidden flex shrink-0 items-center justify-center gap-1 px-3 min-h-11 rounded-lg border border-primary/30 bg-primary/10 text-sm text-primary-text hover:text-indigo-300 cursor-pointer"
						>
							{showFilters ? t("filters.show-less") : t("filters.show-more")}
							{!showFilters && activeFilterCount > 0 && ` · ${activeFilterCount}`}
							{showFilters ? <CaretUpIcon className="size-4" /> : <CaretDownIcon className="size-4" />}
						</button>
					</div>
					<div
						className={cx(
							"grid w-full transition-[grid-template-rows] duration-300 ease-in-out md:block",
							showFilters ? "grid-rows-[1fr]" : "grid-rows-[0fr] -mt-2 md:mt-0",
						)}
					>
						<div id="games-filters" inert={filtersCollapsed} className="overflow-hidden md:overflow-visible">
							<GamesFilters
								categories={categories}
								storageItems={storageFilterItems}
								showType={view !== "table"}
								search={search}
								onChange={changeFilters}
							/>
						</div>
					</div>
				</div>
				<div className="flex flex-wrap items-center gap-2">
					<div className="flex flex-wrap items-center gap-2 w-full md:w-auto md:flex-1" aria-live="polite">
						{shownCount > 0 ? (
							<BgtText size="3" color="white" opacity={70} weight="medium">
								{expansionCount > 0
									? `${t("count", { count: shownCount - expansionCount })}, ${t("count-expansions", { count: expansionCount })}`
									: t("count", { count: shownCount })}
							</BgtText>
						) : (
							<BgtText size="3" color="white" opacity={70}>
								{t("filters.no-match")}
							</BgtText>
						)}
						<ActiveFilterChips
							search={search}
							storageName={storageName}
							onRemove={(key) => changeFilters({ [key]: undefined })}
						/>
						{hasActiveFilters(search) && (
							<button
								type="button"
								onClick={clearFilters}
								className="text-sm text-primary-text hover:text-indigo-300 underline-offset-2 hover:underline cursor-pointer"
							>
								{t("filters.clear")}
							</button>
						)}
					</div>
					<div className="flex items-center gap-2 w-full md:w-auto">
						{filteredGames.some(isPlayable) && (
							<button
								type="button"
								onClick={() =>
									navigate({ to: "/games/pick", search: withoutUndefined({ players, playTime, category }) })
								}
								className="shrink-0 px-3 min-h-11 md:min-h-9 rounded-lg border border-primary/30 bg-primary/10 text-sm font-semibold text-primary-text hover:bg-primary/20 cursor-pointer"
							>
								{t("pick.button")}
							</button>
						)}
						<BgtSimpleSelect
							className="flex-1 md:flex-none md:w-44"
							aria-label={t("sort.label")}
							value={sort ?? "title"}
							items={sortItems}
							onValueChange={(value) => updateSearch({ sort: value === "title" ? undefined : (value as GameSort) })}
						/>
						<div className="flex rounded border border-cancel/60 overflow-hidden">
							{viewButton(undefined, t("view.grid"))}
							{viewButton("table", t("view.table"))}
						</div>
					</div>
				</div>
				{typeSwitched && type === "any" && (
					<output className="block text-sm text-white/70">{t("filters.type.switched")}</output>
				)}
				{shownCount > 0 && view === "table" && (
					<GamesTableView
						games={filteredGames}
						dimmed={dimmed}
						priceMap={priceMap}
						showLivePrices={showLivePrices}
						inStock={inStock === true}
						onInStockChange={(value) => updateSearch({ inStock: value ? true : undefined })}
						currency={settingsQuery.data?.currency}
						uiLanguage={settingsQuery.data?.uiLanguage}
						dateFormat={settingsQuery.data?.dateFormat}
					/>
				)}
				{shownCount > 0 && view !== "table" && (
					<BgtCardList>
						{cards.map(({ game: x, expansion }) =>
							expansion ? (
								<BgtImageCard
									key={`expansion-${expansion.id}`}
									title={expansion.title}
									image={expansion.image}
									state={expansion.state === GameState.Owned ? undefined : expansion.state}
									onClick={() => setQuickView({ gameId: x.id, expansionId: expansion.id })}
									tag={
										<span className="rounded border border-white/30 bg-background/85 px-1.5 text-[11px] font-bold leading-4 tracking-wider text-stat-title">
											{t("table.expansion-tag")}
										</span>
									}
									meta={
										<BgtText size="1" color="white" opacity={50} className="truncate">
											{t("expansion-card", { title: x.title })}
										</BgtText>
									}
								/>
							) : (
								<BgtImageCard
									key={x.id}
									title={x.title}
									image={x.image}
									state={x.state === GameState.Owned && !x.isLoaned ? undefined : x.state}
									isLoaned={x.isLoaned}
									link={`/games/${x.id}`}
									meta={
										x.playCount > 0 ? (
											<BgtText size="1" className="text-card-value truncate">
												{t("plays", { count: x.playCount })}
												{x.lastPlayed && ` · ${toRelative(x.lastPlayed, uiLanguage)}`}
											</BgtText>
										) : (
											<BgtText size="1" color="white" opacity={50} className="truncate">
												{t("never-played")}
											</BgtText>
										)
									}
									badge={
										x.changeDetectionWatchId ? (
											<TrackedPriceIcon livePrice={priceMap.get(x.id)} variant="overlay" />
										) : undefined
									}
								/>
							),
						)}
					</BgtCardList>
				)}
				{quickViewGame && quickViewExpansion && (
					<ExpansionQuickView
						game={quickViewGame}
						expansion={quickViewExpansion}
						canWrite={canWrite}
						currency={settingsQuery.data?.currency}
						close={() => setQuickView(null)}
					/>
				)}
			</BgtPageContent>
		</BgtPage>
	);
}
