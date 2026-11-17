import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import type { ColumnDef, RowData } from "@tanstack/react-table";
import { cx } from "class-variance-authority";
import { type ReactNode, useCallback, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import Refresh from "@/assets/icons/refresh.svg?react";
import SquareOutIcon from "@/assets/icons/square-out.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtDataTable } from "@/components/BgtTable/BgtDataTable";
import { useDurationRange } from "@/hooks/useDurationRange";
import { usePermissions } from "@/hooks/usePermissions";
import { useTimeouts } from "@/hooks/useTimeouts";
import { type Expansion, type Game, type GamePrice, GameState, QUERY_KEYS } from "@/models";
import { getTrackedPricesCall } from "@/services/gameService";
import { toDisplay } from "@/utils/dateUtils";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { COMMON_LANGUAGE_CODES, getLanguageName, LANGUAGE_INDEPENDENT, LANGUAGE_NONE } from "@/utils/languageUtils";
import { formatDecimal, formatPrice, formatPriceOrEmpty } from "@/utils/numberUtils";
import { formatRange as formatValueRange } from "@/utils/rangeUtils";
import { SafeHttpUrl } from "@/utils/stringUtils";
import { useInlineExpansionUpdate } from "../-hooks/useInlineExpansionUpdate";
import { useInlineGameUpdate } from "../-hooks/useInlineGameUpdate";
import { collectionPrice, ownedPriceStats } from "../-utils/gamesSearch";
import { EditableNumberCell } from "./EditableNumberCell";
import { EditableSelectCell } from "./EditableSelectCell";
import { expansionEditKey, GameTableCards, type GameTableColumn } from "./GameTableCards";
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

type GameRow =
	| { kind: "game"; game: Game }
	| { kind: "collection"; game: Game }
	| { kind: "base"; game: Game }
	| { kind: "expansion"; game: Game; expansion: Expansion; last: boolean };

const isGameLine = (row: GameRow) => row.kind === "game" || row.kind === "base";

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
	const { updateExpansion } = useInlineExpansionUpdate();
	const [editingCell, setEditingCell] = useState<EditingCell | null>(null);
	const [openGames, setOpenGames] = useState<Set<number>>(new Set());
	const stopEdit = useCallback(() => setEditingCell(null), []);
	const startEdit = useCallback((gameId: number, column: GameTableColumn) => setEditingCell({ gameId, column }), []);
	const isEditing = useCallback(
		(gameId: number, column: EditingCell["column"]) => editingCell?.gameId === gameId && editingCell.column === column,
		[editingCell],
	);
	const tableMeta = useMemo(() => ({ isEditing }), [isEditing]);
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

	const ownedPrices = useMemo(() => ownedPriceStats(games), [games]);
	const hasPrices = ownedPrices.pricedCount > 0;

	const rows = useMemo(
		() =>
			games.flatMap((game): GameRow[] => {
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
		[games, openGames],
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

	const subRowTitle = useCallback(
		(row: GameRow, tag: string, image: string | null, title: string, children?: ReactNode) => (
			<span
				className={cx(
					"relative flex items-center gap-2 pl-9",
					"before:absolute before:left-3 before:-top-3 before:w-px before:bg-card-border",
					row.kind === "expansion" && row.last ? "before:bottom-1/2" : "before:-bottom-3",
					"after:absolute after:left-3 after:top-1/2 after:h-px after:w-4 after:bg-card-border",
				)}
			>
				<BgtAvatar image={image} title={title} size="small" />
				<span className="rounded border border-primary/30 px-1 text-[10px] leading-4 tracking-wider text-stat-title">
					{tag}
				</span>
				{children ?? <span className="font-medium">{title}</span>}
			</span>
		),
		[],
	);

	const columns: ColumnDef<GameRow>[] = useMemo(
		() => [
			{
				id: "title",
				header: t("games:columns.title"),
				enableSorting: false,
				cell: ({ row: { original: row } }) => {
					const { game } = row;
					if (row.kind === "expansion") {
						return subRowTitle(row, t("games:table.expansion-tag"), row.expansion.image, row.expansion.title);
					}
					if (row.kind === "base") {
						return subRowTitle(row, t("games:table.base-tag"), game.image, game.title);
					}
					const livePrice = priceMap.get(game.id);
					const open = row.kind === "collection";
					return (
						<div className="flex flex-col items-start gap-0.5">
							<Link
								to="/games/$gameId"
								params={{ gameId: game.id }}
								className="flex items-center gap-2 underline-offset-2 hover:text-primary-text hover:underline"
							>
								<BgtAvatar image={game.image} title={game.title} size="small" />
								<span>{game.title}</span>
								{game.changeDetectionWatchId && <TrackedPriceIcon livePrice={livePrice} />}
							</Link>
							{game.expansions.length > 0 && (
								<button
									type="button"
									aria-expanded={open}
									aria-label={t("games:table.toggle-expansions", { count: game.expansions.length, title: game.title })}
									onClick={() => toggleGame(game.id)}
									className="ml-9 -mt-1 inline-flex items-center gap-1 rounded px-1.5 py-0.5 text-xs font-semibold text-primary-text hover:bg-primary/20"
								>
									<CaretRight
										className={cx("size-3 motion-safe:transition-transform", open && "rotate-90")}
										aria-hidden="true"
									/>
									{t("games:table.expansions", { count: game.expansions.length })}
								</button>
							)}
						</div>
					);
				},
			},
			{
				id: "players",
				header: t("games:columns.players"),
				cell: ({ row: { original: row } }) =>
					isGameLine(row) ? formatRange(row.game.minPlayers, row.game.maxPlayers) : null,
				meta: { hideOnMobile: true },
			},
			{
				id: "play-time",
				header: t("games:columns.play-time"),
				cell: ({ row: { original: row } }) =>
					isGameLine(row) ? formatDuration(row.game.minPlayTime, row.game.maxPlayTime) : null,
				meta: { hideOnMobile: true },
			},
			{
				id: "weight",
				header: t("games:columns.weight"),
				cell: ({ row: { original: row } }) => {
					if (!isGameLine(row)) return null;
					return row.game.weight != null && row.game.weight > 0 ? formatDecimal(row.game.weight, uiLanguage) : "-";
				},
				meta: { hideOnMobile: true },
			},
			{
				id: "rating",
				header: t("games:columns.rating"),
				cell: ({ row: { original: row } }) => (isGameLine(row) ? formatDecimal(row.game.rating, uiLanguage) : null),
				meta: { hideOnMobile: true },
			},
			{
				id: "language",
				header: t("games:columns.language"),
				cell: ({ row: { original: row }, table }) =>
					isGameLine(row) ? (
						<EditableSelectCell
							label={t("games:columns.language")}
							readOnly={!canWrite}
							value={row.game.language ?? LANGUAGE_NONE}
							emptyValue={LANGUAGE_NONE}
							items={languageEditItems}
							hasSearch
							editing={!!table.options.meta?.isEditing?.(row.game.id, "language")}
							onStartEdit={() => startEdit(row.game.id, "language")}
							onStopEdit={stopEdit}
							onChange={(language) =>
								updateGame({ ...row.game, language: language === LANGUAGE_NONE ? null : language })
							}
						/>
					) : null,
				meta: { hideOnMobile: true },
			},
			{
				id: "state",
				header: t("games:columns.state"),
				cell: ({ row: { original: row }, table }) => {
					if (row.kind === "expansion") {
						const key = expansionEditKey(row.expansion.id);
						return (
							<EditableSelectCell
								label={`${t("games:columns.state")}: ${row.expansion.title}`}
								readOnly={!canWrite}
								value={row.expansion.state}
								items={stateEditItems}
								editing={!!table.options.meta?.isEditing?.(key, "state")}
								onStartEdit={() => startEdit(key, "state")}
								onStopEdit={stopEdit}
								onChange={(state) => updateExpansion(row.game.id, { ...row.expansion, state: state as GameState })}
							/>
						);
					}
					if (!isGameLine(row)) return null;
					return (
						<EditableSelectCell
							label={t("games:columns.state")}
							readOnly={!canWrite}
							value={row.game.state}
							items={stateEditItems}
							editing={!!table.options.meta?.isEditing?.(row.game.id, "state")}
							onStartEdit={() => startEdit(row.game.id, "state")}
							onStopEdit={stopEdit}
							onChange={(state) => updateGame(withStateChange(row.game, state as GameState, priceMap.get(row.game.id)))}
						/>
					);
				},
				meta: { hideOnMobile: true },
			},
			{
				id: "additionDate",
				header: t("games:columns.added"),
				cell: ({ row: { original: row } }) => {
					if (row.kind === "expansion") return formatDate(row.expansion.additionDate);
					return isGameLine(row) ? formatDate(row.game.additionDate) : null;
				},
				meta: { hideOnMobile: true },
			},
			{
				id: "buyingPrice",
				header: t("games:columns.price"),
				cell: ({ row: { original: row }, table }) => {
					if (row.kind === "expansion") {
						const key = expansionEditKey(row.expansion.id);
						return (
							<EditableNumberCell
								readOnly={!canWrite}
								value={row.expansion.buyingPrice}
								min={0}
								prefix={currency}
								format={(price) => formatPriceOrEmpty(price, currency, uiLanguage)}
								editing={!!table.options.meta?.isEditing?.(key, "buyingPrice")}
								onStartEdit={() => startEdit(key, "buyingPrice")}
								onStopEdit={stopEdit}
								onChange={(buyingPrice) => updateExpansion(row.game.id, { ...row.expansion, buyingPrice })}
								ariaLabel={`${t("games:columns.price")}: ${row.expansion.title}`}
							/>
						);
					}
					if (row.kind === "collection" || (row.kind === "game" && row.game.expansions.length > 0)) {
						const owned = row.game.expansions.filter((expansion) => expansion.state === GameState.Owned).length;
						return (
							<span className="inline-flex flex-col items-start pl-[9px]">
								<span>{formatPriceOrEmpty(collectionPrice(row.game), currency, uiLanguage)}</span>
								<span className="text-[11px] font-medium leading-3 text-white/60">
									{t("games:table.collection-price", { count: owned })}
								</span>
							</span>
						);
					}
					return (
						<EditableNumberCell
							readOnly={!canWrite}
							value={row.game.buyingPrice}
							min={0}
							prefix={currency}
							format={(price) => formatPriceOrEmpty(price, currency, uiLanguage)}
							editing={!!table.options.meta?.isEditing?.(row.game.id, "buyingPrice")}
							onStartEdit={() => startEdit(row.game.id, "buyingPrice")}
							onStopEdit={stopEdit}
							onChange={(buyingPrice) => updateGame({ ...row.game, buyingPrice })}
							ariaLabel={`${t("games:columns.price")}: ${row.game.title}`}
						/>
					);
				},
				meta: { hideOnMobile: true },
			},
			{
				id: "shopUrl",
				header: t("games:columns.shop"),
				enableSorting: false,
				cell: ({ row: { original: row } }) => {
					if (!isGameLine(row)) return null;
					const url = SafeHttpUrl(priceMap.get(row.game.id)?.shopUrl ?? row.game.shopUrl);
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
				id: "bggId",
				header: t("games:columns.bgg"),
				enableSorting: false,
				cell: ({ row: { original: row } }) => {
					if (row.kind === "collection") return null;
					const bggId = row.kind === "expansion" ? row.expansion.bggId : row.game.bggId;
					return bggId ? (
						<a
							href={`https://boardgamegeek.com/boardgame/${bggId}`}
							target="_blank"
							rel="noopener noreferrer"
							className="inline-flex items-center gap-1 text-primary-text hover:text-indigo-300"
						>
							<SquareOutIcon className="size-4" />
							{t("games:columns.bgg")}
						</a>
					) : (
						"-"
					);
				},
			},
			...(showLivePrices
				? [
						{
							id: "current-price",
							header: t("games:columns.current-price"),
							cell: ({ row: { original: row } }: { row: { original: GameRow } }) =>
								isGameLine(row) ? (
									<LivePrice livePrice={priceMap.get(row.game.id)} currency={currency} uiLanguage={uiLanguage} />
								) : null,
							meta: { hideOnMobile: true },
						},
					]
				: []),
		],
		[
			t,
			currency,
			uiLanguage,
			formatRange,
			formatDuration,
			formatDate,
			updateGame,
			updateExpansion,
			stateEditItems,
			languageEditItems,
			showLivePrices,
			priceMap,
			startEdit,
			stopEdit,
			canWrite,
			toggleGame,
			subRowTitle,
		],
	);

	const columnWidths: (string | null)[] = [null, null, null, null, null, "w-52", "w-52"];
	const rowClassName = useCallback(
		(row: GameRow) =>
			row.kind === "base" || row.kind === "expansion"
				? "bg-card-black/70 [&>td]:py-2"
				: row.kind === "collection"
					? "[&>td]:border-b-transparent"
					: undefined,
		[],
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
			<div className="hidden md:block">
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
