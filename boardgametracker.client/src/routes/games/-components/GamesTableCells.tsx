import { Link } from "@tanstack/react-router";
import type { CellContext, HeaderContext } from "@tanstack/react-table";
import { cx } from "class-variance-authority";
import { createContext, type Dispatch, type ReactNode, type SetStateAction, useContext } from "react";
import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import SquareOutIcon from "@/assets/icons/square-out.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { type Expansion, type Game, type GamePrice, GameState, StorageItemType } from "@/models";
import { LANGUAGE_NONE } from "@/utils/languageUtils";
import { formatDecimal, formatPriceOrEmpty } from "@/utils/numberUtils";
import { canStore, parsePlaceKey, placeKey, placeOf, type StoragePlace } from "@/utils/storage";
import { SafeHttpUrl } from "@/utils/stringUtils";
import { collectionPrice } from "../-utils/gamesSearch";
import { EditableNumberCell } from "./EditableNumberCell";
import { EditableSelectCell } from "./EditableSelectCell";
import { expansionEditKey, type GameTableColumn } from "./GameTableCards";
import { LivePrice, withStateChange } from "./LivePrice";
import { TrackedPriceIcon } from "./TrackedPriceIcon";

export type GameRow =
	| { kind: "game"; game: Game }
	| { kind: "collection"; game: Game }
	| { kind: "base"; game: Game }
	| { kind: "expansion"; game: Game; expansion: Expansion; last: boolean };

export const NO_PLACE = "none";

const isGameLine = (row: GameRow) => row.kind === "game" || row.kind === "base";

interface SelectItem {
	value: string;
	label: string;
}

export interface GamesTableContextValue {
	canWrite: boolean;
	priceMap: Map<number, GamePrice>;
	dimmed?: Map<number, number[]>;
	openGames: Set<number>;
	selectable: number[];
	currency?: string;
	uiLanguage?: string;
	setSelected: Dispatch<SetStateAction<Set<number>>>;
	toggleSelected: (gameId: number) => void;
	toggleGame: (gameId: number) => void;
	startEdit: (gameId: number, column: GameTableColumn) => void;
	stopEdit: () => void;
	updateGame: (game: Game) => void;
	updateExpansion: (gameId: number, expansion: Expansion) => void;
	store: (type: StorageItemType, ids: number[], place: StoragePlace | null) => Promise<void>;
	shortPlace: (place: StoragePlace) => string;
	formatRange: (min: number | null, max: number | null) => string;
	formatDuration: (min: number | null, max: number | null) => string;
	formatDate: (date: Date | null) => string;
	stateEditItems: SelectItem[];
	languageEditItems: SelectItem[];
	gamePlaceItems: SelectItem[];
	expansionPlaceItems: SelectItem[];
}

export const GamesTableContext = createContext<GamesTableContextValue | null>(null);

const useGamesTable = () => {
	const value = useContext(GamesTableContext);
	if (!value) throw new Error("GamesTableContext is missing");
	return value;
};

const subRowTitle = (row: GameRow, tag: string, image: string | null, title: string, children?: ReactNode) => (
	<span
		className={cx(
			"relative flex items-center gap-2 pl-9",
			"before:absolute before:left-3 before:-top-3 before:w-px before:bg-card-border",
			row.kind === "expansion" && row.last ? "before:bottom-1/2" : "before:-bottom-3",
			"after:absolute after:left-3 after:top-1/2 after:h-px after:w-4 after:bg-card-border",
		)}
	>
		<BgtAvatar image={image} title={title} size="small" />
		<span className="rounded border border-primary/30 px-1 text-[11px] leading-4 tracking-wider text-stat-title">
			{tag}
		</span>
		{children ?? <span className="font-medium">{title}</span>}
	</span>
);

export const SelectAllHeader = ({ table }: HeaderContext<GameRow, unknown>) => {
	const { t } = useTranslation("games");
	const { selectable, setSelected } = useGamesTable();
	return (
		<input
			type="checkbox"
			className="size-4 accent-primary align-middle"
			aria-label={t("games:table.select-all")}
			checked={!!table.options.meta?.allSelected}
			onChange={(event) => setSelected(event.target.checked ? new Set(selectable) : new Set())}
		/>
	);
};

export const SelectCell = ({ row: { original: row }, table }: CellContext<GameRow, unknown>) => {
	const { t } = useTranslation("games");
	const { selectable, toggleSelected } = useGamesTable();
	return row.kind === "game" || row.kind === "collection" ? (
		<input
			type="checkbox"
			className="size-4 accent-primary align-middle"
			aria-label={t("games:table.select", { title: row.game.title })}
			disabled={!selectable.includes(row.game.id)}
			checked={!!table.options.meta?.isSelected?.(row.game.id)}
			onChange={() => toggleSelected(row.game.id)}
		/>
	) : null;
};

export const TitleCell = ({ row: { original: row } }: CellContext<GameRow, unknown>) => {
	const { t } = useTranslation("games");
	const { priceMap, dimmed, toggleGame } = useGamesTable();
	const { game } = row;
	if (row.kind === "expansion") {
		return subRowTitle(row, t("games:table.expansion-tag"), row.expansion.image, row.expansion.title);
	}
	if (row.kind === "base") {
		return subRowTitle(row, t("games:table.base-tag"), game.image, game.title);
	}
	const livePrice = priceMap.get(game.id);
	const open = row.kind === "collection";
	const only = dimmed?.get(game.id);
	return (
		<div data-keep={only ? "" : undefined} className="flex flex-col items-start gap-0.5">
			<Link
				to="/games/$gameId"
				params={{ gameId: game.id }}
				className={cx(
					"flex items-center gap-2 underline-offset-2 hover:text-primary-text hover:underline",
					only && "opacity-45",
				)}
			>
				<BgtAvatar image={game.image} title={game.title} size="small" />
				<span>{game.title}</span>
				{game.changeDetectionWatchId && <TrackedPriceIcon livePrice={livePrice} />}
			</Link>
			{only && <span className="ml-9 text-xs text-white/60">{t("games:table.stored-elsewhere")}</span>}
			{game.expansions.length > 0 && !only && (
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
};

export const PlayersCell = ({ row: { original: row } }: CellContext<GameRow, unknown>) => {
	const { formatRange } = useGamesTable();
	return isGameLine(row) ? formatRange(row.game.minPlayers, row.game.maxPlayers) : null;
};

export const PlayTimeCell = ({ row: { original: row } }: CellContext<GameRow, unknown>) => {
	const { formatDuration } = useGamesTable();
	return isGameLine(row) ? formatDuration(row.game.minPlayTime, row.game.maxPlayTime) : null;
};

export const WeightCell = ({ row: { original: row } }: CellContext<GameRow, unknown>) => {
	const { uiLanguage } = useGamesTable();
	if (!isGameLine(row)) return null;
	return row.game.weight != null && row.game.weight > 0 ? formatDecimal(row.game.weight, uiLanguage) : "-";
};

export const RatingCell = ({ row: { original: row } }: CellContext<GameRow, unknown>) => {
	const { uiLanguage } = useGamesTable();
	return isGameLine(row) ? formatDecimal(row.game.rating, uiLanguage) : null;
};

export const LanguageCell = ({ row: { original: row }, table }: CellContext<GameRow, unknown>) => {
	const { t } = useTranslation("games");
	const { canWrite, languageEditItems, startEdit, stopEdit, updateGame } = useGamesTable();
	return isGameLine(row) ? (
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
			onChange={(language) => updateGame({ ...row.game, language: language === LANGUAGE_NONE ? null : language })}
		/>
	) : null;
};

export const StateCell = ({ row: { original: row }, table }: CellContext<GameRow, unknown>) => {
	const { t } = useTranslation("games");
	const { canWrite, priceMap, stateEditItems, startEdit, stopEdit, updateGame, updateExpansion } = useGamesTable();
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
};

export const StorageCell = ({ row: { original: row }, table }: CellContext<GameRow, unknown>) => {
	const { t } = useTranslation("games");
	const { canWrite, openGames, dimmed, gamePlaceItems, expansionPlaceItems, shortPlace, startEdit, stopEdit, store } =
		useGamesTable();
	if (row.kind === "expansion") {
		const key = expansionEditKey(row.expansion.id);
		if (!canStore(row.expansion.state)) return <span className="text-white/40">-</span>;
		const place = placeOf(row.expansion);
		return (
			<EditableSelectCell
				label={`${t("games:columns.storage")}: ${row.expansion.title}`}
				readOnly={!canWrite}
				value={place ? placeKey(place) : NO_PLACE}
				items={expansionPlaceItems}
				hasSearch
				className="max-w-60"
				display={place ? shortPlace(place) : undefined}
				editing={!!table.options.meta?.isEditing?.(key, "storage")}
				flash={!!table.options.meta?.isStored?.(key)}
				onFlashEnd={() => table.options.meta?.storedDone?.(key)}
				onStartEdit={() => startEdit(key, "storage")}
				onStopEdit={stopEdit}
				onChange={(value) => store(StorageItemType.Expansion, [row.expansion.id], parsePlaceKey(value))}
			/>
		);
	}
	if (row.kind === "collection" && (openGames.has(row.game.id) || dimmed?.has(row.game.id))) return null;
	if (!canStore(row.game.state)) return <span className="text-white/40">-</span>;
	const place = placeOf(row.game);
	const elsewhere = row.game.expansions.filter((expansion) => placeOf(expansion) !== null).length;
	return (
		<span className="flex flex-col items-start">
			<EditableSelectCell
				label={`${t("games:columns.storage")}: ${row.game.title}`}
				readOnly={!canWrite}
				value={place ? placeKey(place) : NO_PLACE}
				items={gamePlaceItems}
				hasSearch
				className="max-w-60"
				display={place ? shortPlace(place) : undefined}
				editing={!!table.options.meta?.isEditing?.(row.game.id, "storage")}
				flash={!!table.options.meta?.isStored?.(row.game.id)}
				onFlashEnd={() => table.options.meta?.storedDone?.(row.game.id)}
				onStartEdit={() => startEdit(row.game.id, "storage")}
				onStopEdit={stopEdit}
				onChange={(value) => store(StorageItemType.Game, [row.game.id], parsePlaceKey(value))}
			/>
			{row.kind !== "base" && elsewhere > 0 && (
				<span className="pl-[9px] text-[11px] leading-3 text-white/60">
					{t("games:table.expansions-elsewhere", { count: elsewhere })}
				</span>
			)}
		</span>
	);
};

export const AdditionDateCell = ({ row: { original: row } }: CellContext<GameRow, unknown>) => {
	const { formatDate } = useGamesTable();
	if (row.kind === "expansion") return formatDate(row.expansion.additionDate);
	return isGameLine(row) ? formatDate(row.game.additionDate) : null;
};

export const BuyingPriceCell = ({ row: { original: row }, table }: CellContext<GameRow, unknown>) => {
	const { t } = useTranslation("games");
	const { canWrite, currency, uiLanguage, startEdit, stopEdit, updateGame, updateExpansion } = useGamesTable();
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
};

export const ShopUrlCell = ({ row: { original: row } }: CellContext<GameRow, unknown>) => {
	const { t } = useTranslation("games");
	const { priceMap } = useGamesTable();
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
};

export const BggIdCell = ({ row: { original: row } }: CellContext<GameRow, unknown>) => {
	const { t } = useTranslation("games");
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
};

export const CurrentPriceCell = ({ row: { original: row } }: CellContext<GameRow, unknown>) => {
	const { priceMap, currency, uiLanguage } = useGamesTable();
	return isGameLine(row) ? (
		<LivePrice livePrice={priceMap.get(row.game.id)} currency={currency} uiLanguage={uiLanguage} />
	) : null;
};
