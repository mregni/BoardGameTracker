import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { ColumnDef, RowData } from "@tanstack/react-table";
import { cx } from "class-variance-authority";
import { useCallback, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import Refresh from "@/assets/icons/refresh.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { StoragePicker } from "@/components/BgtStorage/StoragePicker";
import { BgtDataTable } from "@/components/BgtTable/BgtDataTable";
import { useDurationRange } from "@/hooks/useDurationRange";
import { usePermissions } from "@/hooks/usePermissions";
import { useStorageActions, useStorageData } from "@/hooks/useStorage";
import { useTimeouts } from "@/hooks/useTimeouts";
import { type Game, type GamePrice, GameState, QUERY_KEYS, StorageItemType } from "@/models";
import { getTrackedPricesCall } from "@/services/gameService";
import { toDisplay } from "@/utils/dateUtils";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { COMMON_LANGUAGE_CODES, getLanguageName, LANGUAGE_INDEPENDENT, LANGUAGE_NONE } from "@/utils/languageUtils";
import { formatPrice } from "@/utils/numberUtils";
import { formatRange as formatValueRange } from "@/utils/rangeUtils";
import { allPlaces, canStore, placeKey, placeLabel, placeNames, type StoragePlace } from "@/utils/storage";
import { useInlineExpansionUpdate } from "../-hooks/useInlineExpansionUpdate";
import { useInlineGameUpdate } from "../-hooks/useInlineGameUpdate";
import { ownedPriceStats } from "../-utils/gamesSearch";
import {
	AdditionDateCell,
	BggIdCell,
	BuyingPriceCell,
	CurrentPriceCell,
	type GameRow,
	GamesTableContext,
	LanguageCell,
	NO_PLACE,
	PlayersCell,
	PlayTimeCell,
	RatingCell,
	SelectAllHeader,
	SelectCell,
	ShopUrlCell,
	StateCell,
	StorageCell,
	TitleCell,
	WeightCell,
} from "./GamesTableCells";
import { expansionEditKey, GameTableCards, type GameTableColumn } from "./GameTableCards";

declare module "@tanstack/react-table" {
	interface TableMeta<TData extends RowData> {
		isEditing?: (id: number, column: GameTableColumn) => boolean;
		isSelected?: (id: number) => boolean;
		allSelected?: boolean;
		isStored?: (id: number) => boolean;
		storedDone?: (id: number) => void;
	}
}

interface EditingCell {
	gameId: number;
	column: GameTableColumn;
}

interface Props {
	games: Game[];
	dimmed?: Map<number, number[]>;
	priceMap: Map<number, GamePrice>;
	showLivePrices: boolean;
	inStock: boolean;
	onInStockChange: (inStock: boolean) => void;
	currency?: string;
	uiLanguage?: string;
	dateFormat?: string;
}

export const GamesTableView = (props: Props) => {
	const { games, dimmed, priceMap, showLivePrices, inStock, onInStockChange, currency, uiLanguage, dateFormat } = props;
	const { t, i18n } = useTranslation(["games", "game", "common"]);
	const { canWrite } = usePermissions();
	const { updateGame } = useInlineGameUpdate();
	const { updateExpansion } = useInlineExpansionUpdate();
	const { nodes, locations } = useStorageData();
	const { assignStorage } = useStorageActions();
	const [selected, setSelected] = useState<Set<number>>(new Set());
	const [stored, setStored] = useState<Set<number>>(new Set());
	const [editingCell, setEditingCell] = useState<EditingCell | null>(null);
	const [openGames, setOpenGames] = useState<Set<number>>(new Set());
	const stopEdit = useCallback(() => setEditingCell(null), []);
	const startEdit = useCallback((gameId: number, column: GameTableColumn) => setEditingCell({ gameId, column }), []);
	const isEditing = useCallback(
		(gameId: number, column: EditingCell["column"]) => editingCell?.gameId === gameId && editingCell.column === column,
		[editingCell],
	);
	const toggleGame = useCallback(
		(gameId: number) =>
			setOpenGames((previous) => {
				const next = new Set(previous);
				if (!next.delete(gameId)) next.add(gameId);
				return next;
			}),
		[],
	);

	const queryClient = useQueryClient();
	const schedule = useTimeouts();
	const refreshPricesMutation = useMutation({
		mutationFn: () => getTrackedPricesCall(true),
		onSuccess: (data) => {
			queryClient.setQueryData([QUERY_KEYS.trackedPrices], data);
			if (data.some((price) => price.recheckQueued)) {
				schedule(() => {
					getTrackedPricesCall(true)
						.then((fresh) => queryClient.setQueryData([QUERY_KEYS.trackedPrices], fresh))
						.catch(() => {});
				}, 20000);
			}
		},
	});

	const durationRange = useDurationRange();
	const formatRange = useCallback((min: number | null, max: number | null): string => {
		return formatValueRange(min, max) ?? "-";
	}, []);
	const formatDuration = useCallback(
		(min: number | null, max: number | null): string => durationRange(min, max) ?? "-",
		[durationRange],
	);

	const ownedPrices = useMemo(() => ownedPriceStats(games.filter((game) => !dimmed?.has(game.id))), [games, dimmed]);
	const selectable = useMemo(
		() => games.filter((game) => canStore(game.state) && !dimmed?.has(game.id)).map((game) => game.id),
		[games, dimmed],
	);
	const selectedIds = useMemo(() => selectable.filter((id) => selected.has(id)), [selectable, selected]);
	const tableMeta = useMemo(
		() => ({
			isEditing,
			isSelected: (id: number) => selected.has(id) && selectable.includes(id),
			allSelected: selectable.length > 0 && selectedIds.length === selectable.length,
			isStored: (id: number) => stored.has(id),
			storedDone: (id: number) =>
				setStored((previous) => {
					const next = new Set(previous);
					next.delete(id);
					return next;
				}),
		}),
		[isEditing, selected, selectable, selectedIds, stored],
	);
	const toggleSelected = useCallback(
		(gameId: number) =>
			setSelected((previous) => {
				const next = new Set(previous);
				if (!next.delete(gameId)) next.add(gameId);
				return next;
			}),
		[],
	);

	const placeItems = useMemo(
		() =>
			allPlaces(nodes, locations).map((place) => ({
				value: placeKey(place),
				label:
					place.storageLocationId === null
						? t("storage:place.somewhere-in", { name: placeLabel(place, nodes, locations, t) })
						: placeLabel(place, nodes, locations, t),
			})),
		[nodes, locations, t],
	);
	const shortPlace = useCallback(
		(place: StoragePlace) => placeNames(place, nodes, locations, t).slice(-2).join(" › "),
		[nodes, locations, t],
	);
	const gamePlaceItems = useMemo(
		() => [{ value: NO_PLACE, label: t("storage:place.none") }, ...placeItems],
		[placeItems, t],
	);
	const expansionPlaceItems = useMemo(
		() => [{ value: NO_PLACE, label: t("storage:place.in-base-box") }, ...placeItems],
		[placeItems, t],
	);
	const store = useCallback(
		(type: StorageItemType, ids: number[], place: StoragePlace | null) =>
			assignStorage({
				items: ids.map((id) => ({ type, id })),
				locationId: place?.locationId ?? null,
				storageLocationId: place?.storageLocationId ?? null,
			}).then(
				() => setStored(new Set(type === StorageItemType.Expansion ? ids.map(expansionEditKey) : ids)),
				() => undefined,
			),
		[assignStorage],
	);
	const hasPrices = ownedPrices.pricedCount > 0;

	const rows = useMemo(
		() =>
			games.flatMap((game): GameRow[] => {
				const only = dimmed?.get(game.id);
				if (only) {
					const shown = game.expansions.filter((expansion) => only.includes(expansion.id));
					return [
						{ kind: "collection", game },
						...shown.map((expansion, index) => ({
							kind: "expansion" as const,
							game,
							expansion,
							last: index === shown.length - 1,
						})),
					];
				}
				if (game.expansions.length === 0 || !openGames.has(game.id)) {
					return [{ kind: "game", game }];
				}
				return [
					{ kind: "collection", game },
					{ kind: "base", game },
					...game.expansions.map((expansion, index) => ({
						kind: "expansion" as const,
						game,
						expansion,
						last: index === game.expansions.length - 1,
					})),
				];
			}),
		[games, openGames, dimmed],
	);

	const stateEditItems = useMemo(
		() => Object.values(GameState).map((value) => ({ value, label: t(getItemStateTranslationKey(value, false)) })),
		[t],
	);
	const languageEditItems = useMemo(
		() => [
			{ value: LANGUAGE_NONE, label: t("game:language.none") },
			{ value: LANGUAGE_INDEPENDENT, label: t("game:language-independent") },
			...COMMON_LANGUAGE_CODES.map((code) => ({ value: code, label: getLanguageName(code, i18n.language) })),
		],
		[t, i18n.language],
	);

	const formatDate = useCallback(
		(date: Date | null) => (date && dateFormat ? toDisplay(date, dateFormat, uiLanguage ?? "en-US") : "-"),
		[dateFormat, uiLanguage],
	);

	const tableContext = useMemo(
		() => ({
			canWrite,
			priceMap,
			dimmed,
			openGames,
			selectable,
			currency,
			uiLanguage,
			setSelected,
			toggleSelected,
			toggleGame,
			startEdit,
			stopEdit,
			updateGame,
			updateExpansion,
			store,
			shortPlace,
			formatRange,
			formatDuration,
			formatDate,
			stateEditItems,
			languageEditItems,
			gamePlaceItems,
			expansionPlaceItems,
		}),
		[
			canWrite,
			priceMap,
			dimmed,
			openGames,
			selectable,
			currency,
			uiLanguage,
			toggleSelected,
			toggleGame,
			startEdit,
			stopEdit,
			updateGame,
			updateExpansion,
			store,
			shortPlace,
			formatRange,
			formatDuration,
			formatDate,
			stateEditItems,
			languageEditItems,
			gamePlaceItems,
			expansionPlaceItems,
		],
	);

	const columns: ColumnDef<GameRow>[] = useMemo(
		() => [
			...(canWrite
				? [
						{
							id: "select",
							header: SelectAllHeader,
							enableSorting: false,
							cell: SelectCell,
						},
					]
				: []),
			{
				id: "title",
				header: t("games:columns.title"),
				enableSorting: false,
				cell: TitleCell,
			},
			{
				id: "players",
				header: t("games:columns.players"),
				cell: PlayersCell,
				meta: { hideOnMobile: true },
			},
			{
				id: "play-time",
				header: t("games:columns.play-time"),
				cell: PlayTimeCell,
				meta: { hideOnMobile: true },
			},
			{
				id: "weight",
				header: t("games:columns.weight"),
				cell: WeightCell,
				meta: { hideOnMobile: true },
			},
			{
				id: "rating",
				header: t("games:columns.rating"),
				cell: RatingCell,
				meta: { hideOnMobile: true },
			},
			{
				id: "language",
				header: t("games:columns.language"),
				cell: LanguageCell,
				meta: { hideOnMobile: true },
			},
			{
				id: "state",
				header: t("games:columns.state"),
				cell: StateCell,
				meta: { hideOnMobile: true },
			},
			{
				id: "storage",
				header: t("games:columns.storage"),
				cell: StorageCell,
				meta: { hideOnMobile: true },
			},
			{
				id: "additionDate",
				header: t("games:columns.added"),
				cell: AdditionDateCell,
				meta: { hideOnMobile: true },
			},
			{
				id: "buyingPrice",
				header: t("games:columns.price"),
				cell: BuyingPriceCell,
				meta: { hideOnMobile: true },
			},
			{
				id: "shopUrl",
				header: t("games:columns.shop"),
				enableSorting: false,
				cell: ShopUrlCell,
			},
			{
				id: "bggId",
				header: t("games:columns.bgg"),
				enableSorting: false,
				cell: BggIdCell,
			},
			...(showLivePrices
				? [
						{
							id: "current-price",
							header: t("games:columns.current-price"),
							cell: CurrentPriceCell,
							meta: { hideOnMobile: true },
						},
					]
				: []),
		],
		[t, canWrite, showLivePrices],
	);

	const columnWidths: (string | null)[] = [
		...(canWrite ? ["w-10"] : []),
		null,
		null,
		null,
		null,
		null,
		"w-52",
		"w-52",
		"w-56",
	];
	const rowClassName = useCallback(
		(row: GameRow) =>
			cx(
				row.kind === "base" || row.kind === "expansion"
					? "bg-card-black/70 [&>td]:py-2"
					: row.kind === "collection"
						? "[&>td]:border-b-transparent"
						: undefined,
				row.kind === "collection" && dimmed?.has(row.game.id) && "[&>td>*:not([data-keep])]:opacity-45",
			),
		[dimmed],
	);

	return (
		<>
			{(hasPrices || showLivePrices) && (
				<div className="flex flex-col md:flex-row md:items-center gap-2">
					{hasPrices && (
						<p className="text-sm text-white/70 md:mr-auto">
							{t("games:table.total-price-owned")}:{" "}
							<span className="font-bold text-card-value">{formatPrice(ownedPrices.total, currency, uiLanguage)}</span>
							{" · "}
							{t("games:table.mean-price-owned", { count: ownedPrices.pricedCount })}:{" "}
							<span className="font-bold text-card-value">{formatPrice(ownedPrices.mean, currency, uiLanguage)}</span>
						</p>
					)}
					{showLivePrices && (
						<div className="flex gap-2 md:ml-auto">
							<BgtButton
								variant={inStock ? "primary" : "cancel"}
								className="flex-1 md:flex-none"
								onClick={() => onInStockChange(!inStock)}
							>
								{t("games:filters.in-stock-only")}
							</BgtButton>
							<BgtButton
								variant="cancel"
								className="flex-1 md:flex-none"
								disabled={refreshPricesMutation.isPending}
								onClick={() => refreshPricesMutation.mutate()}
							>
								<Refresh className={refreshPricesMutation.isPending ? "size-4 animate-spin" : "size-4"} />
								{t("games:refresh-prices")}
							</BgtButton>
						</div>
					)}
				</div>
			)}
			{selectedIds.length > 0 && (
				<section
					aria-label={t("games:table.selection", { count: selectedIds.length })}
					className="flex flex-wrap items-center gap-3 rounded-lg border border-primary/30 bg-primary/10 px-3 py-2 motion-safe:animate-rise-in motion-reduce:animate-fade-in"
				>
					<span className="text-sm">
						<span className="font-bold tabular-nums text-card-value">{selectedIds.length}</span>{" "}
						{t("games:table.selected", { count: selectedIds.length })}
					</span>
					<BgtButton variant="cancel" size="1" onClick={() => setSelected(new Set())}>
						{t("games:table.clear-selection")}
					</BgtButton>
					<StoragePicker
						className="min-w-60 flex-1 md:flex-none"
						value={null}
						placeholder={t("games:table.move-to-storage")}
						aria-label={t("games:table.move-to-storage")}
						canCreate
						onChange={(place) => {
							if (!place) return;
							void store(StorageItemType.Game, selectedIds, place).then(() => setSelected(new Set()));
						}}
					/>
				</section>
			)}
			<div className="hidden md:block">
				<GamesTableContext.Provider value={tableContext}>
					<BgtDataTable
						columns={columns}
						data={rows}
						meta={tableMeta}
						enableSorting={false}
						isLoading={false}
						noDataMessage={t("games:table.empty")}
						widths={columnWidths}
						rowClassName={rowClassName}
					/>
				</GamesTableContext.Provider>
			</div>
			<div className="md:hidden">
				<GameTableCards
					readOnly={!canWrite}
					priceMap={showLivePrices ? priceMap : undefined}
					uiLanguage={uiLanguage}
					games={games}
					isLoading={false}
					currency={currency}
					dateFormat={dateFormat}
					stateItems={stateEditItems}
					languageItems={languageEditItems}
					formatRange={formatRange}
					formatDuration={formatDuration}
					isEditing={isEditing}
					startEdit={startEdit}
					stopEdit={stopEdit}
					updateGame={updateGame}
					updateExpansion={updateExpansion}
				/>
			</div>
		</>
	);
};
