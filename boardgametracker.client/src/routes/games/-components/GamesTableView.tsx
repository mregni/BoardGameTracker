import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import type { ColumnDef, RowData } from "@tanstack/react-table";
import { useCallback, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import Refresh from "@/assets/icons/refresh.svg?react";
import SquareOutIcon from "@/assets/icons/square-out.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtDataTable } from "@/components/BgtTable/BgtDataTable";
import { useDurationRange } from "@/hooks/useDurationRange";
import { usePermissions } from "@/hooks/usePermissions";
import { useTimeouts } from "@/hooks/useTimeouts";
import { type Game, type GamePrice, GameState, QUERY_KEYS } from "@/models";
import { getTrackedPricesCall } from "@/services/gameService";
import { toDisplay } from "@/utils/dateUtils";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { COMMON_LANGUAGE_CODES, getLanguageName, LANGUAGE_INDEPENDENT, LANGUAGE_NONE } from "@/utils/languageUtils";
import { formatDecimal, formatPrice, formatPriceOrEmpty } from "@/utils/numberUtils";
import { formatRange as formatValueRange } from "@/utils/rangeUtils";
import { SafeHttpUrl } from "@/utils/stringUtils";
import { useInlineGameUpdate } from "../-hooks/useInlineGameUpdate";
import { ownedPriceStats } from "../-utils/gamesSearch";
import { EditableNumberCell } from "./EditableNumberCell";
import { EditableSelectCell } from "./EditableSelectCell";
import { GameTableCards, type GameTableColumn } from "./GameTableCards";
import { LivePrice, withStateChange } from "./LivePrice";
import { TrackedPriceIcon } from "./TrackedPriceIcon";

declare module "@tanstack/react-table" {
	interface TableMeta<TData extends RowData> {
		isEditing?: (id: number, column: GameTableColumn) => boolean;
	}
}

interface EditingCell {
	gameId: number;
	column: GameTableColumn;
}

interface Props {
	games: Game[];
	priceMap: Map<number, GamePrice>;
	showLivePrices: boolean;
	inStock: boolean;
	onInStockChange: (inStock: boolean) => void;
	currency?: string;
	uiLanguage?: string;
	dateFormat?: string;
}

export const GamesTableView = (props: Props) => {
	const { games, priceMap, showLivePrices, inStock, onInStockChange, currency, uiLanguage, dateFormat } = props;
	const { t, i18n } = useTranslation(["games", "game", "common"]);
	const { canWrite } = usePermissions();
	const { updateGame } = useInlineGameUpdate();
	const [editingCell, setEditingCell] = useState<EditingCell | null>(null);
	const stopEdit = useCallback(() => setEditingCell(null), []);
	const startEdit = useCallback((gameId: number, column: GameTableColumn) => setEditingCell({ gameId, column }), []);
	const isEditing = useCallback(
		(gameId: number, column: EditingCell["column"]) => editingCell?.gameId === gameId && editingCell.column === column,
		[editingCell],
	);
	const tableMeta = useMemo(() => ({ isEditing }), [isEditing]);

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

	const ownedPrices = useMemo(() => ownedPriceStats(games), [games]);
	const hasPrices = ownedPrices.pricedCount > 0;

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

	const columns: ColumnDef<Game>[] = useMemo(
		() => [
			{
				accessorKey: "title",
				header: t("games:columns.title"),
				enableSorting: false,
				cell: ({ row }) => {
					const livePrice = priceMap.get(row.original.id);
					return (
						<Link
							to="/games/$gameId"
							params={{ gameId: row.original.id }}
							className="flex items-center gap-2 underline-offset-2 hover:text-primary-text hover:underline"
						>
							<BgtAvatar image={row.original.image} title={row.original.title} size="small" />
							<span>{row.original.title}</span>
							{row.original.changeDetectionWatchId && <TrackedPriceIcon livePrice={livePrice} />}
						</Link>
					);
				},
			},
			{
				id: "players",
				accessorFn: (game) => game.minPlayers ?? game.maxPlayers ?? 0,
				header: t("games:columns.players"),
				cell: ({ row }) => formatRange(row.original.minPlayers, row.original.maxPlayers),
				meta: { hideOnMobile: true },
			},
			{
				id: "play-time",
				accessorFn: (game) => game.minPlayTime ?? game.maxPlayTime ?? 0,
				header: t("games:columns.play-time"),
				cell: ({ row }) => formatDuration(row.original.minPlayTime, row.original.maxPlayTime),
				meta: { hideOnMobile: true },
			},
			{
				accessorKey: "weight",
				header: t("games:columns.weight"),
				cell: ({ row }) =>
					row.original.weight != null && row.original.weight > 0 ? formatDecimal(row.original.weight, uiLanguage) : "-",
				meta: { hideOnMobile: true },
			},
			{
				accessorKey: "rating",
				header: t("games:columns.rating"),
				cell: ({ row }) => formatDecimal(row.original.rating, uiLanguage),
				meta: { hideOnMobile: true },
			},
			{
				accessorKey: "language",
				header: t("games:columns.language"),
				cell: ({ row, table }) => (
					<EditableSelectCell
						readOnly={!canWrite}
						value={row.original.language ?? LANGUAGE_NONE}
						emptyValue={LANGUAGE_NONE}
						items={languageEditItems}
						hasSearch
						editing={!!table.options.meta?.isEditing?.(row.original.id, "language")}
						onStartEdit={() => startEdit(row.original.id, "language")}
						onStopEdit={stopEdit}
						onChange={(language) =>
							updateGame({ ...row.original, language: language === LANGUAGE_NONE ? null : language })
						}
					/>
				),
				meta: { hideOnMobile: true },
			},
			{
				accessorKey: "state",
				header: t("games:columns.state"),
				cell: ({ row, table }) => (
					<EditableSelectCell
						readOnly={!canWrite}
						value={row.original.state}
						items={stateEditItems}
						editing={!!table.options.meta?.isEditing?.(row.original.id, "state")}
						onStartEdit={() => startEdit(row.original.id, "state")}
						onStopEdit={stopEdit}
						onChange={(state) =>
							updateGame(withStateChange(row.original, state as GameState, priceMap.get(row.original.id)))
						}
					/>
				),
				meta: { hideOnMobile: true },
			},
			{
				accessorKey: "additionDate",
				header: t("games:columns.added"),
				cell: ({ row }) =>
					row.original.additionDate && dateFormat
						? toDisplay(row.original.additionDate, dateFormat, uiLanguage ?? "en-US")
						: "-",
				meta: { hideOnMobile: true },
			},
			{
				accessorKey: "buyingPrice",
				header: t("games:columns.price"),
				cell: ({ row, table }) => (
					<EditableNumberCell
						readOnly={!canWrite}
						value={row.original.buyingPrice}
						min={0}
						prefix={currency}
						format={(price) => formatPriceOrEmpty(price, currency, uiLanguage)}
						editing={!!table.options.meta?.isEditing?.(row.original.id, "buyingPrice")}
						onStartEdit={() => startEdit(row.original.id, "buyingPrice")}
						onStopEdit={stopEdit}
						onChange={(buyingPrice) => updateGame({ ...row.original, buyingPrice })}
						ariaLabel={`${t("games:columns.price")}: ${row.original.title}`}
					/>
				),
				meta: { hideOnMobile: true },
			},
			{
				accessorKey: "shopUrl",
				header: t("games:columns.shop"),
				enableSorting: false,
				cell: ({ row }) => {
					const url = SafeHttpUrl(priceMap.get(row.original.id)?.shopUrl ?? row.original.shopUrl);
					return url ? (
						<a
							href={url}
							target="_blank"
							rel="noopener noreferrer"
							className="inline-flex items-center gap-1 text-primary-text hover:text-indigo-300"
						>
							<SquareOutIcon className="size-4" />
							{t("games:columns.shop")}
						</a>
					) : (
						"-"
					);
				},
			},
			{
				accessorKey: "bggId",
				header: t("games:columns.bgg"),
				enableSorting: false,
				cell: ({ row }) =>
					row.original.bggId ? (
						<a
							href={`https://boardgamegeek.com/boardgame/${row.original.bggId}`}
							target="_blank"
							rel="noopener noreferrer"
							className="inline-flex items-center gap-1 text-primary-text hover:text-indigo-300"
						>
							<SquareOutIcon className="size-4" />
							{t("games:columns.bgg")}
						</a>
					) : (
						"-"
					),
			},
			...(showLivePrices
				? [
						{
							id: "current-price",
							header: t("games:columns.current-price"),
							accessorFn: (game: Game) => priceMap.get(game.id)?.price ?? undefined,
							sortUndefined: "last" as const,
							cell: ({ row }: { row: { original: Game } }) => (
								<LivePrice livePrice={priceMap.get(row.original.id)} currency={currency} uiLanguage={uiLanguage} />
							),
							meta: { hideOnMobile: true },
						},
					]
				: []),
		],
		[
			t,
			currency,
			uiLanguage,
			dateFormat,
			formatRange,
			formatDuration,
			updateGame,
			stateEditItems,
			languageEditItems,
			showLivePrices,
			priceMap,
			startEdit,
			stopEdit,
			canWrite,
		],
	);

	const columnWidths: (string | null)[] = [null, null, null, null, null, "w-52", "w-52"];

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
			<div className="hidden md:block">
				<BgtDataTable
					columns={columns}
					data={games}
					meta={tableMeta}
					enableSorting={false}
					isLoading={false}
					noDataMessage={t("games:table.empty")}
					widths={columnWidths}
				/>
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
				/>
			</div>
		</>
	);
};
