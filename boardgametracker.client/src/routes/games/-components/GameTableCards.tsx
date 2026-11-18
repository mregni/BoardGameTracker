import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { type ReactNode, useState } from "react";
import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import SquareOutIcon from "@/assets/icons/square-out.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtLoadingSpinner } from "@/components/BgtLoadingSpinner/BgtLoadingSpinner";
import { BgtText } from "@/components/BgtText/BgtText";
import { useStorageData } from "@/hooks/useStorage";
import { type BgtSelectItem, type Expansion, type Game, GameState } from "@/models";
import type { GamePrice } from "@/models/Games/GamePrice";
import { toDisplay } from "@/utils/dateUtils";
import { LANGUAGE_NONE } from "@/utils/languageUtils";
import { formatDecimal, formatPriceOrEmpty } from "@/utils/numberUtils";
import { canStore, placeLabel, placeOf } from "@/utils/storage";
import { SafeHttpUrl } from "@/utils/stringUtils";
import { collectionPrice } from "../-utils/gamesSearch";
import { EditableNumberCell } from "./EditableNumberCell";
import { EditableSelectCell } from "./EditableSelectCell";
import { LivePrice, withStateChange } from "./LivePrice";
import { TrackedPriceIcon } from "./TrackedPriceIcon";

export type GameTableColumn = "language" | "state" | "buyingPrice" | "storage";

export const expansionEditKey = (expansionId: number) => -expansionId;

interface Props {
	games: Game[];
	isLoading: boolean;
	currency?: string;
	dateFormat?: string;
	stateItems: BgtSelectItem[];
	languageItems: BgtSelectItem[];
	formatRange: (min: number | null, max: number | null) => string;
	formatDuration: (min: number | null, max: number | null) => string;
	isEditing: (gameId: number, column: GameTableColumn) => boolean;
	startEdit: (gameId: number, column: GameTableColumn) => void;
	stopEdit: () => void;
	updateGame: (game: Game) => void;
	updateExpansion: (gameId: number, expansion: Expansion) => void;
	readOnly?: boolean;
	priceMap?: Map<number, GamePrice>;
	uiLanguage?: string;
}

const Row = ({ label, children }: { label: string; children: ReactNode }) => (
	<div className="flex items-center justify-between gap-3 min-h-9">
		<BgtText size="2" color="gray" className="shrink-0">
			{label}
		</BgtText>
		<div className="min-w-0 flex-1 text-right">{children}</div>
	</div>
);

export const GameTableCards = (props: Props) => {
	const {
		games,
		isLoading,
		currency,
		dateFormat,
		stateItems,
		languageItems,
		formatRange,
		formatDuration,
		isEditing,
		startEdit,
		stopEdit,
		updateGame,
		updateExpansion,
		readOnly = false,
		priceMap,
		uiLanguage,
	} = props;
	const { t } = useTranslation(["games", "game", "common"]);
	const { nodes, locations } = useStorageData();
	const [openGames, setOpenGames] = useState<Set<number>>(new Set());
	const toggleGame = (gameId: number) =>
		setOpenGames((previous) => {
			const next = new Set(previous);
			if (!next.delete(gameId)) next.add(gameId);
			return next;
		});

	if (isLoading) {
		return <BgtLoadingSpinner />;
	}

	if (games.length === 0) {
		return (
			<BgtText size="2" color="gray">
				{t("games:table.empty")}
			</BgtText>
		);
	}

	return (
		<ul className="flex flex-col gap-3">
			{games.map((game) => {
				const livePrice = priceMap?.get(game.id);
				const shopUrl = SafeHttpUrl(livePrice?.shopUrl ?? game.shopUrl);
				const hasExpansions = game.expansions.length > 0;
				const open = openGames.has(game.id);
				const ownedExpansions = game.expansions.filter((expansion) => expansion.state === GameState.Owned).length;
				const basePrice = (
					<EditableNumberCell
						readOnly={readOnly}
						value={game.buyingPrice}
						min={0}
						prefix={currency}
						format={(price) => formatPriceOrEmpty(price, currency, uiLanguage)}
						editing={isEditing(game.id, "buyingPrice")}
						onStartEdit={() => startEdit(game.id, "buyingPrice")}
						onStopEdit={stopEdit}
						onChange={(buyingPrice) => updateGame({ ...game, buyingPrice })}
						ariaLabel={`${t("games:columns.price")}: ${game.title}`}
						align="right"
						size="sm"
						className="h-9 w-full text-sm"
					/>
				);
				return (
					<li key={game.id} className="rounded-lg border border-card-border bg-card-black p-3 flex flex-col gap-1">
						<Link
							to="/games/$gameId"
							params={{ gameId: game.id }}
							className="flex items-center gap-2 mb-1 underline-offset-2 hover:text-primary-text hover:underline"
						>
							<BgtAvatar image={game.image} title={game.title} size="small" />
							<span className="font-semibold truncate">{game.title}</span>
							{priceMap && game.changeDetectionWatchId && <TrackedPriceIcon livePrice={livePrice} />}
						</Link>
						<div className="grid grid-cols-2 gap-x-4">
							<Row label={t("games:columns.players")}>{formatRange(game.minPlayers, game.maxPlayers)}</Row>
							<Row label={t("games:columns.play-time")}>{formatDuration(game.minPlayTime, game.maxPlayTime)}</Row>
							<Row label={t("games:columns.weight")}>
								{game.weight != null && game.weight > 0 ? formatDecimal(game.weight, uiLanguage) : "-"}
							</Row>
							<Row label={t("games:columns.rating")}>{formatDecimal(game.rating, uiLanguage)}</Row>
							<Row label={t("games:columns.language")}>
								<EditableSelectCell
									label={t("games:columns.language")}
									readOnly={readOnly}
									value={game.language ?? LANGUAGE_NONE}
									emptyValue={LANGUAGE_NONE}
									items={languageItems}
									hasSearch
									editing={isEditing(game.id, "language")}
									onStartEdit={() => startEdit(game.id, "language")}
									onStopEdit={stopEdit}
									onChange={(language) =>
										updateGame({ ...game, language: language === LANGUAGE_NONE ? null : language })
									}
									align="right"
									size="sm"
								/>
							</Row>
							<Row label={t("games:columns.state")}>
								<EditableSelectCell
									label={t("games:columns.state")}
									readOnly={readOnly}
									value={game.state}
									items={stateItems}
									editing={isEditing(game.id, "state")}
									onStartEdit={() => startEdit(game.id, "state")}
									onStopEdit={stopEdit}
									onChange={(state) => updateGame(withStateChange(game, state as GameState, livePrice))}
									align="right"
									size="sm"
								/>
							</Row>
							{priceMap && (
								<Row label={t("games:columns.current-price")}>
									<LivePrice livePrice={livePrice} currency={currency} uiLanguage={uiLanguage} />
								</Row>
							)}
							{canStore(game.state) && (
								<Row label={t("games:columns.storage")}>
									<span className="block truncate text-sm">
										{(() => {
											const place = placeOf(game);
											return place ? placeLabel(place, nodes, locations, t) : t("storage:place.none");
										})()}
									</span>
								</Row>
							)}
							<Row label={t("games:columns.added")}>
								{game.additionDate && dateFormat
									? toDisplay(game.additionDate, dateFormat, uiLanguage ?? "en-US")
									: "-"}
							</Row>
							<Row label={t("games:columns.price")}>
								{hasExpansions ? (
									<span className="inline-flex flex-col items-end pr-[9px]">
										<span>{formatPriceOrEmpty(collectionPrice(game), currency, uiLanguage)}</span>
										<span className="text-[11px] leading-3 text-white/60">
											{t("games:table.collection-price", { count: ownedExpansions })}
										</span>
									</span>
								) : (
									basePrice
								)}
							</Row>
						</div>
						{hasExpansions && (
							<div className="border-t border-card-border pt-1">
								<button
									type="button"
									aria-expanded={open}
									aria-label={t("games:table.toggle-expansions", { count: game.expansions.length, title: game.title })}
									onClick={() => toggleGame(game.id)}
									className="inline-flex min-h-11 items-center gap-1 text-sm font-semibold text-primary-text"
								>
									<CaretRight
										className={cx("size-3 motion-safe:transition-transform", open && "rotate-90")}
										aria-hidden="true"
									/>
									{t("games:table.expansions", { count: game.expansions.length })}
								</button>
								{open && (
									<ul className="flex flex-col gap-2 pb-1">
										<li className="flex flex-col gap-1 rounded-md bg-primary/5 p-2">
											<span className="flex items-center gap-2 text-sm font-medium">
												<span className="rounded border border-primary/30 px-1 text-[11px] leading-4 tracking-wider text-stat-title">
													{t("games:table.base-tag")}
												</span>
												{game.title}
											</span>
											<Row label={t("games:columns.price")}>{basePrice}</Row>
										</li>
										{game.expansions.map((expansion) => {
											const key = expansionEditKey(expansion.id);
											return (
												<li key={expansion.id} className="flex flex-col gap-1 rounded-md bg-primary/5 p-2">
													<span className="flex items-center gap-2 text-sm font-medium">
														<BgtAvatar image={expansion.image} title={expansion.title} size="small" />
														<span className="rounded border border-primary/30 px-1 text-[11px] leading-4 tracking-wider text-stat-title">
															{t("games:table.expansion-tag")}
														</span>
														<span className="truncate">{expansion.title}</span>
													</span>
													<Row label={t("games:columns.state")}>
														<EditableSelectCell
															label={`${t("games:columns.state")}: ${expansion.title}`}
															readOnly={readOnly}
															value={expansion.state}
															items={stateItems}
															editing={isEditing(key, "state")}
															onStartEdit={() => startEdit(key, "state")}
															onStopEdit={stopEdit}
															onChange={(state) =>
																updateExpansion(game.id, { ...expansion, state: state as GameState })
															}
															align="right"
															size="sm"
														/>
													</Row>
													<Row label={t("games:columns.price")}>
														<EditableNumberCell
															readOnly={readOnly}
															value={expansion.buyingPrice}
															min={0}
															prefix={currency}
															format={(price) => formatPriceOrEmpty(price, currency, uiLanguage)}
															editing={isEditing(key, "buyingPrice")}
															onStartEdit={() => startEdit(key, "buyingPrice")}
															onStopEdit={stopEdit}
															onChange={(buyingPrice) => updateExpansion(game.id, { ...expansion, buyingPrice })}
															ariaLabel={`${t("games:columns.price")}: ${expansion.title}`}
															align="right"
															size="sm"
															className="h-9 w-full text-sm"
														/>
													</Row>
												</li>
											);
										})}
									</ul>
								)}
							</div>
						)}
						{(shopUrl || game.bggId) && (
							<div className="flex justify-end gap-4 pt-1">
								{shopUrl && (
									<a
										href={shopUrl}
										target="_blank"
										rel="noopener noreferrer"
										className="inline-flex items-center gap-1 text-primary-text hover:text-indigo-300"
									>
										<SquareOutIcon className="size-4" />
										{t("games:columns.shop")}
									</a>
								)}
								{game.bggId && (
									<a
										href={`https://boardgamegeek.com/boardgame/${game.bggId}`}
										target="_blank"
										rel="noopener noreferrer"
										className="inline-flex items-center gap-1 text-primary-text hover:text-indigo-300"
									>
										<SquareOutIcon className="size-4" />
										{t("games:columns.bgg")}
									</a>
								)}
							</div>
						)}
					</li>
				);
			})}
		</ul>
	);
};
