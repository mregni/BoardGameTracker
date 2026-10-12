import { Link } from "@tanstack/react-router";
import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import SquareOutIcon from "@/assets/icons/square-out.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtLoadingSpinner } from "@/components/BgtLoadingSpinner/BgtLoadingSpinner";
import { BgtText } from "@/components/BgtText/BgtText";
import type { BgtSelectItem, Game, GameState } from "@/models";
import type { GamePrice } from "@/models/Games/GamePrice";
import { toDisplay } from "@/utils/dateUtils";
import { LANGUAGE_NONE } from "@/utils/languageUtils";
import { RoundDecimal } from "@/utils/numberUtils";
import { formatPrice } from "@/utils/priceUtils";
import { SafeHttpUrl } from "@/utils/stringUtils";
import { EditableNumberCell } from "./EditableNumberCell";
import { EditableSelectCell } from "./EditableSelectCell";
import { LivePrice, withStateChange } from "./LivePrice";
import { TrackedPriceIcon } from "./TrackedPriceIcon";

export type GameTableColumn = "language" | "state" | "buyingPrice";

interface Props {
	games: Game[];
	isLoading: boolean;
	currency?: string;
	dateFormat?: string;
	stateItems: BgtSelectItem[];
	languageItems: BgtSelectItem[];
	formatRange: (min: number | null, max: number | null, suffix?: string) => string;
	isEditing: (gameId: number, column: GameTableColumn) => boolean;
	startEdit: (gameId: number, column: GameTableColumn) => void;
	stopEdit: () => void;
	updateGame: (game: Game) => void;
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
		isEditing,
		startEdit,
		stopEdit,
		updateGame,
		readOnly = false,
		priceMap,
		uiLanguage,
	} = props;
	const { t } = useTranslation(["games", "game", "common"]);

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
				return (
					<li key={game.id} className="rounded-lg border border-card-border bg-card-black p-3 flex flex-col gap-1">
						<Link
							to="/games/$gameId"
							params={{ gameId: game.id }}
							className="flex items-center gap-2 mb-1 underline-offset-2 hover:text-primary hover:underline"
						>
							<BgtAvatar image={game.image} title={game.title} size="small" />
							<span className="font-semibold truncate">{game.title}</span>
							{priceMap && game.changeDetectionWatchId && <TrackedPriceIcon livePrice={livePrice} />}
						</Link>
						<Row label={t("games:columns.players")}>{formatRange(game.minPlayers, game.maxPlayers)}</Row>
						<Row label={t("games:columns.play-time")}>{formatRange(game.minPlayTime, game.maxPlayTime, "min")}</Row>
						<Row label={t("games:columns.weight")}>
							{game.weight != null && game.weight > 0 ? (RoundDecimal(game.weight, 0.1) ?? "-") : "-"}
						</Row>
						<Row label={t("games:columns.rating")}>
							{game.rating != null ? (RoundDecimal(game.rating, 0.1) ?? "-") : "-"}
						</Row>
						<Row label={t("games:columns.language")}>
							<EditableSelectCell
								readOnly={readOnly}
								value={game.language ?? LANGUAGE_NONE}
								emptyValue={LANGUAGE_NONE}
								items={languageItems}
								hasSearch
								editing={isEditing(game.id, "language")}
								onStartEdit={() => startEdit(game.id, "language")}
								onStopEdit={stopEdit}
								onChange={(language) => updateGame({ ...game, language: language === LANGUAGE_NONE ? null : language })}
								align="right"
								size="sm"
							/>
						</Row>
						<Row label={t("games:columns.state")}>
							<EditableSelectCell
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
						<Row label={t("games:columns.added")}>
							{game.additionDate && dateFormat ? toDisplay(game.additionDate, dateFormat, uiLanguage ?? "en-US") : "-"}
						</Row>
						<Row label={t("games:columns.price")}>
							<EditableNumberCell
								readOnly={readOnly}
								value={game.buyingPrice}
								step={0.01}
								min={0}
								prefix={currency}
								format={(price) => formatPrice(price, currency, uiLanguage)}
								editing={isEditing(game.id, "buyingPrice")}
								onStartEdit={() => startEdit(game.id, "buyingPrice")}
								onStopEdit={stopEdit}
								onChange={(buyingPrice) => updateGame({ ...game, buyingPrice })}
								align="right"
								size="sm"
								className="h-9 w-full text-sm"
							/>
						</Row>
						{(shopUrl || game.bggId) && (
							<div className="flex justify-end gap-4 pt-1">
								{shopUrl && (
									<a
										href={shopUrl}
										target="_blank"
										rel="noopener noreferrer"
										className="inline-flex items-center gap-1 text-primary hover:text-primary/80"
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
										className="inline-flex items-center gap-1 text-primary hover:text-primary/80"
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
