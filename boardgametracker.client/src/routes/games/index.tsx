import { noop, useQuery } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useCallback, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import CaretDownIcon from "@/assets/icons/caret-down.svg?react";
import CaretUpIcon from "@/assets/icons/caret-up.svg?react";
import Game from "@/assets/icons/gamepad.svg?react";
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
import { GameState } from "@/models";
import { getGames, getTrackedPrices } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { toRelative } from "@/utils/dateUtils";
import { LANGUAGE_NONE } from "@/utils/languageUtils";
import { NoGamesEmptyPage } from "../-components/NoGamesEmptyPage";
import {
	ActiveFilterChips,
	filterGames,
	type GameSort,
	type GamesFilterSearch,
	GamesFilters,
} from "./-components/GamesFilters";
import { GamesTableView } from "./-components/GamesTableView";
import { TrackedPriceIcon } from "./-components/TrackedPriceIcon";
import { useGamesData } from "./-hooks/useGamesData";
import {
	filterByTitle,
	GAME_SORTS,
	hasActiveFilters,
	isPlayable,
	parseGamesSearch,
	sortGames,
	withoutUndefined,
} from "./-utils/gamesSearch";

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
	const { view, q, category, players, playTime, weight, age, state, language, inStock, sort } = search;
	const { t } = useTranslation(["games", "common"]);
	const navigate = useNavigate({ from: Route.fullPath });
	const { games, isLoading, isError, retry } = useGamesData();
	const { canWrite } = usePermissions();
	const [showFilters, setShowFilters] = useState(false);
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

	const filteredGames = useMemo(() => {
		const result = filterByTitle(games, q).filter(
			(game) =>
				(category === undefined || game.categories.some((cat) => cat.name === category)) &&
				(state === undefined || game.state === state) &&
				(language === undefined || (language === LANGUAGE_NONE ? !game.language : game.language === language)) &&
				(!inStock || !showLivePrices || priceMap.get(game.id)?.inStock === true),
		);
		return sortGames(filterGames(result, { playerCount: players, maxPlayTime: playTime, weight, age }), sort);
	}, [games, q, category, players, playTime, weight, age, state, language, inStock, showLivePrices, priceMap, sort]);

	const activeFilterCount = [category, players, playTime, weight, age, state, language, inStock].filter(
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
							<GamesFilters categories={categories} search={search} onChange={updateSearch} />
						</div>
					</div>
				</div>
				<div className="flex flex-wrap items-center gap-2">
					<div className="flex flex-wrap items-center gap-2 w-full md:w-auto md:flex-1" aria-live="polite">
						{filteredGames.length > 0 ? (
							<BgtText size="3" color="white" opacity={70} weight="medium">
								{t("count", { count: filteredGames.length })}
							</BgtText>
						) : (
							<BgtText size="3" color="white" opacity={70}>
								{t("filters.no-match")}
							</BgtText>
						)}
						<ActiveFilterChips search={search} onRemove={(key) => updateSearch({ [key]: undefined })} />
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
				{filteredGames.length === 0 ? null : view === "table" ? (
					<GamesTableView
						games={filteredGames}
						priceMap={priceMap}
						showLivePrices={showLivePrices}
						inStock={inStock === true}
						onInStockChange={(value) => updateSearch({ inStock: value ? true : undefined })}
						currency={settingsQuery.data?.currency}
						uiLanguage={settingsQuery.data?.uiLanguage}
						dateFormat={settingsQuery.data?.dateFormat}
					/>
				) : (
					<BgtCardList>
						{filteredGames.map((x) => (
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
						))}
					</BgtCardList>
				)}
			</BgtPageContent>
		</BgtPage>
	);
}
