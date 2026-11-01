import { noop, useQuery } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useCallback, useMemo, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import CaretDownIcon from "@/assets/icons/caret-down.svg?react";
import CaretUpIcon from "@/assets/icons/caret-up.svg?react";
import Game from "@/assets/icons/dices.svg?react";
import FilterIcon from "@/assets/icons/sliders-horizontal.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtDialog, BgtDialogContent, BgtDialogFooter, BgtDialogTitle } from "@/components/BgtDialog";
import { SearchInputField } from "@/components/BgtForm";
import { BgtSimpleSelect } from "@/components/BgtForm/BgtSimpleSelect";
import { BgtImageCard } from "@/components/BgtImageCard/BgtImageCard";
import { BgtCardList } from "@/components/BgtLayout/BgtCardList";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtText } from "@/components/BgtText/BgtText";
import { usePermissions } from "@/hooks/usePermissions";
import { getGamePlaySummaries, getGames, getTrackedPrices } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { NoGamesEmptyPage } from "../-components/NoGamesEmptyPage";
import { GameCardMeta } from "./-components/GameCardMeta";
import {
	ActiveFilterChips,
	type FilterKey,
	type GamesFilterSearch,
	GamesFilters,
	type SortOption,
	useSecondaryFilterCount,
} from "./-components/GamesFilters";
import { GamesLoadError } from "./-components/GamesLoadError";
import { TrackedPriceIcon } from "./-components/TrackedPriceIcon";
import { useDebouncedSearchQuery } from "./-hooks/useDebouncedSearchQuery";
import { useGamesData } from "./-hooks/useGamesData";
import {
	applyGridFilters,
	hasGridFilters,
	type PlaySummary,
	parseGamesSearch,
	sortGames,
	withoutUndefined,
} from "./-utils/gamesSearch";

const FILTER_KEYS: FilterKey[] = ["category", "players", "playTime", "weight", "age", "show"];
const UNPLAYED: PlaySummary = { playCount: 0, lastPlayed: null };

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
	const { q, sort } = search;
	const { t } = useTranslation(["games", "common"]);
	const navigate = useNavigate();
	const { games, isLoading, isError, refetch } = useGamesData();
	const { canWrite } = usePermissions();
	const [showSheet, setShowSheet] = useState(false);
	const [showMore, setShowMore] = useState<boolean | undefined>(undefined);
	const chipFocus = useRef<number | null>(null);

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

	const playsQuery = useQuery(getGamePlaySummaries());
	const playMap = useMemo(
		() => new Map((playsQuery.data ?? []).map((summary) => [summary.gameId, summary])),
		[playsQuery.data],
	);
	const hasPlays = playsQuery.data !== undefined;

	const filteredGames = useMemo(() => applyGridFilters(games, search), [games, search]);
	const sortedGames = useMemo(() => sortGames(filteredGames, sort, playMap), [filteredGames, sort, playMap]);
	const hiddenWanted = useMemo(
		() => (search.show === undefined ? applyGridFilters(games, { ...search, show: "wanted" }).length : 0),
		[games, search],
	);

	const relaxedCounts = useMemo(() => {
		if (filteredGames.length > 0) return undefined;
		return Object.fromEntries(
			FILTER_KEYS.filter((key) => search[key] !== undefined).map((key) => [
				key,
				applyGridFilters(games, { ...search, [key]: undefined }).length,
			]),
		) as Partial<Record<FilterKey, number>>;
	}, [games, search, filteredGames.length]);
	const withoutSearchCount = useMemo(
		() => (filteredGames.length === 0 && q ? applyGridFilters(games, { ...search, q: undefined }).length : 0),
		[games, search, q, filteredGames.length],
	);

	const clearFilters = () => {
		setFilterValue("");
		navigate({ to: "/games", search: sort ? { sort } : {}, replace: true });
	};
	const focusSearch = () => document.querySelector<HTMLInputElement>("#games-search input")?.focus();
	const settingsQuery = useQuery(getSettings());
	const trackedPricesQuery = useQuery({
		...getTrackedPrices(),
		enabled: !!settingsQuery.data?.changeDetectionStatus?.isConfigured,
	});
	const filterProps = { categories, search, onChange: updateSearch };
	const activeFilterCount = FILTER_KEYS.filter((key) => search[key] !== undefined).length;
	const secondaryCount = useSecondaryFilterCount(filterProps);
	const moreOpen = showMore ?? secondaryCount > 0;
	const sortOptions: SortOption[] = hasPlays ? ["name", "recent", "plays", "added"] : ["name", "added"];
	const actions = [
		{
			onClick: () => navigate({ to: "/games/table", search: q ? { q } : {} }),
			variant: "cancel" as const,
			content: "games:view.table",
		},
		...(canWrite
			? [{ onClick: () => navigate({ to: "/games/add" }), variant: "primary" as const, content: "games:new" }]
			: []),
	];
	const priceMap = useMemo(
		() => new Map((trackedPricesQuery.data ?? []).map((price) => [price.gameId, price])),
		[trackedPricesQuery.data],
	);

	if (isError) return <GamesLoadError onRetry={() => refetch()} />;

	if (isLoading) {
		return (
			<BgtPage>
				<BgtPageHeader header={t("title")} icon={Game} actions={actions} />
				<BgtPageContent>
					<div className="flex flex-col gap-2 motion-safe:animate-pulse" aria-hidden="true">
						<div className="flex gap-2">
							<div className="h-11 md:h-9 flex-1 rounded-lg bg-card-light" />
							<div className="h-11 w-24 md:h-9 md:w-36 rounded-lg bg-card-light" />
							<div className="hidden md:block h-9 w-36 rounded-lg bg-card-light" />
						</div>
						<div className="h-4 w-24 my-2 rounded bg-card-light" />
					</div>
					<BgtCardList aria-busy="true" aria-label={t("title")}>
						{Array.from({ length: 15 }, (_, index) => (
							<div key={index} className="flex flex-col gap-2 motion-safe:animate-pulse">
								<div className="aspect-square rounded-lg bg-card-light" />
								<div className="h-4 w-3/4 rounded bg-card-light" />
								<div className="h-3 w-1/2 rounded bg-card-light" />
							</div>
						))}
					</BgtCardList>
				</BgtPageContent>
			</BgtPage>
		);
	}

	if (games.length === 0) {
		return <NoGamesEmptyPage header={t("title")} icon={Game} />;
	}

	return (
		<BgtPage>
			<BgtPageHeader header={t("title")} icon={Game} actions={actions} />
			<BgtPageContent>
				<div className="flex flex-col gap-2">
					<div className="flex items-center gap-2 flex-wrap">
						<div id="games-search" className="flex-1 min-w-[10rem]">
							<SearchInputField
								value={filterValue}
								placeholder={t("search")}
								onChange={(event) => setFilterValue(event.target.value)}
							/>
						</div>
						<button
							type="button"
							onClick={() => setShowSheet(true)}
							className="md:hidden inline-flex items-center gap-1.5 min-h-11 rounded-lg border border-primary/30 px-3 text-sm text-white cursor-pointer hover:bg-primary/10"
						>
							<FilterIcon aria-hidden="true" className="size-4 text-primary-text" />
							{t("filters.title")}
							{activeFilterCount > 0 && <span className="tabular-nums text-primary-text">({activeFilterCount})</span>}
						</button>
						<div className="hidden md:contents">
							<GamesFilters group="primary" {...filterProps} />
							<button
								type="button"
								onClick={() => setShowMore(!moreOpen)}
								aria-expanded={moreOpen}
								aria-controls="games-more-filters"
								className="inline-flex items-center gap-1 h-9 px-2 text-xs font-medium text-primary-text hover:text-indigo-300 cursor-pointer"
							>
								{moreOpen ? t("filters.fewer") : t("filters.more")}
								{!moreOpen && secondaryCount > 0 && <span className="tabular-nums">({secondaryCount})</span>}
								{moreOpen ? <CaretUpIcon className="size-4" /> : <CaretDownIcon className="size-4" />}
							</button>
						</div>
					</div>
					{moreOpen && (
						<div id="games-more-filters" className="hidden md:flex flex-wrap items-center gap-2">
							<GamesFilters group="secondary" {...filterProps} />
						</div>
					)}
				</div>
				<div className="flex flex-wrap items-center gap-2 min-h-8 pt-2 pb-1">
					<BgtText size="2" color="gray" weight="medium" className="tabular-nums mr-1" aria-live="polite">
						{t("count", { count: filteredGames.length })}
					</BgtText>
					{filteredGames.length > 0 && (
						<ActiveFilterChips {...filterProps} focusRef={chipFocus} onEmpty={focusSearch} />
					)}
					{hiddenWanted > 0 && (
						<BgtButton
							variant="text"
							size="1"
							onClick={() => updateSearch({ show: "all" })}
							className="pointer-coarse:min-h-11"
						>
							{t("filters.show-hidden-wanted", { count: hiddenWanted })}
						</BgtButton>
					)}
					{hasGridFilters(search) && (
						<BgtButton variant="text" size="1" onClick={clearFilters} className="pointer-coarse:min-h-11">
							{t("filters.clear")}
						</BgtButton>
					)}
					<BgtSimpleSelect
						className="ml-auto w-auto min-w-40 pointer-coarse:[&_button]:min-h-11"
						valuePrefix={t("sort.label")}
						aria-label={t("sort.label")}
						value={sort && sortOptions.includes(sort) ? sort : "name"}
						items={sortOptions.map((option) => ({ value: option, label: t(`sort.${option}`) }))}
						onValueChange={(value) => updateSearch({ sort: value === "name" ? undefined : (value as SortOption) })}
					/>
				</div>
				{filteredGames.length === 0 && (
					<section aria-labelledby="games-no-match" className="flex flex-col items-center text-center gap-3 py-12">
						<h2 id="games-no-match" className="text-lg font-semibold text-white">
							{t("filters.no-match")}
						</h2>
						<p className="text-sm text-gray-400">{t("filters.no-match-hint")}</p>
						<div className="flex flex-wrap justify-center gap-2 max-w-2xl">
							<ActiveFilterChips {...filterProps} counts={relaxedCounts} focusRef={chipFocus} onEmpty={focusSearch} />
							{q && (
								<button
									type="button"
									disabled={withoutSearchCount === 0}
									onClick={() => {
										setFilterValue("");
										updateSearch({ q: undefined });
										focusSearch();
									}}
									className="inline-flex items-center gap-1.5 min-h-8 rounded-full border border-primary/40 bg-primary/15 px-3 text-xs font-medium text-white hover:bg-primary/25 cursor-pointer disabled:cursor-not-allowed disabled:opacity-40 pointer-coarse:min-h-11"
								>
									{t("filters.chip", { filter: t("common:search"), value: `“${q}”` })}
									<span className="text-white/60 tabular-nums">→ {t("count", { count: withoutSearchCount })}</span>
								</button>
							)}
						</div>
					</section>
				)}
				<BgtCardList>
					{sortedGames.map((x) => (
						<BgtImageCard
							key={x.id}
							title={x.title}
							image={x.image}
							state={x.state}
							isLoaned={x.isLoaned}
							link={`/games/${x.id}`}
							meta={<GameCardMeta game={x} plays={hasPlays ? (playMap.get(x.id) ?? UNPLAYED) : undefined} />}
							badge={
								x.changeDetectionWatchId ? (
									<TrackedPriceIcon livePrice={priceMap.get(x.id)} variant="overlay" />
								) : undefined
							}
						/>
					))}
				</BgtCardList>
				<BgtDialog open={showSheet} onClose={() => setShowSheet(false)}>
					<BgtDialogContent size="sheet">
						<BgtDialogTitle>{t("filters.title")}</BgtDialogTitle>
						<div className="flex flex-col gap-3">
							<GamesFilters group="all" stacked {...filterProps} />
						</div>
						<BgtDialogFooter
							onCancel={hasGridFilters(search) ? clearFilters : undefined}
							cancelLabel={t("filters.clear")}
							confirmLabel={t("filters.show-results", { count: filteredGames.length })}
							onConfirm={() => setShowSheet(false)}
						/>
					</BgtDialogContent>
				</BgtDialog>
			</BgtPageContent>
		</BgtPage>
	);
}
