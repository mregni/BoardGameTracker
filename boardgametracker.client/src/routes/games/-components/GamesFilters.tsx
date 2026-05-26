import { useTranslation } from "react-i18next";
import { BgtSimpleSelect } from "@/components/BgtForm/BgtSimpleSelect";
import type { BgtSelectItem, Game } from "@/models";

export type WeightBucket = "light" | "medium" | "heavy";

export interface GameFilterState {
	playerCount?: number;
	maxPlayTime?: number;
	weight?: WeightBucket;
}

export interface GamesFilterSearch {
	category?: string;
	players?: number;
	playTime?: number;
	weight?: WeightBucket;
}

const ANY = "any";

const PLAYER_COUNTS = [1, 2, 3, 4, 5, 6, 7, 8];
const PLAY_TIMES = [30, 60, 90, 120, 180];
const WEIGHTS: WeightBucket[] = ["light", "medium", "heavy"];

const matchesPlayerCount = (game: Game, count: number): boolean => {
	const lo = game.minPlayers ?? game.maxPlayers;
	const hi = game.maxPlayers ?? game.minPlayers;
	if (lo === null || hi === null) return false;
	return count >= lo && count <= hi;
};

const matchesWeight = (game: Game, bucket: WeightBucket): boolean => {
	const weight = game.weight;
	if (weight === null || weight <= 0) return false;
	if (bucket === "light") return weight < 2;
	if (bucket === "medium") return weight >= 2 && weight <= 3;
	return weight > 3;
};

export const filterGames = (games: Game[], filters: GameFilterState): Game[] => {
	return games.filter((game) => {
		if (filters.playerCount !== undefined && !matchesPlayerCount(game, filters.playerCount)) return false;
		if (filters.maxPlayTime !== undefined) {
			if (game.maxPlayTime === null || game.maxPlayTime <= 0 || game.maxPlayTime > filters.maxPlayTime) return false;
		}
		if (filters.weight !== undefined && !matchesWeight(game, filters.weight)) return false;
		return true;
	});
};

interface Props {
	categories: string[];
	category?: string;
	filters: GameFilterState;
	onCategoryChange: (category?: string) => void;
	onChange: (filters: GameFilterState) => void;
}

export const GamesFilters = (props: Props) => {
	const { categories, category, filters, onCategoryChange, onChange } = props;
	const { t } = useTranslation(["games"]);

	const withAny = (items: BgtSelectItem[]): BgtSelectItem[] => [
		{ value: ANY, label: t("games:filters.any") },
		...items,
	];

	const categoryItems = withAny(categories.map((name) => ({ value: name, label: name })));
	const playerItems = withAny(
		PLAYER_COUNTS.map((c) => ({ value: c, label: t("games:filters.player-count-value", { count: c }) })),
	);
	const playTimeItems = withAny(
		PLAY_TIMES.map((m) => ({ value: m, label: t("games:filters.play-time-value", { count: m }) })),
	);
	const weightItems = withAny(WEIGHTS.map((w) => ({ value: w, label: t(`games:filters.weight-${w}`) })));

	return (
		<>
			<BgtSimpleSelect
				className="w-full md:w-36"
				placeholder={t("games:filters.category")}
				value={category ?? null}
				items={categoryItems}
				hasSearch
				onValueChange={(value) => onCategoryChange(value === ANY ? undefined : String(value))}
			/>
			<BgtSimpleSelect
				className="w-full md:w-36"
				placeholder={t("games:filters.player-count")}
				value={filters.playerCount ?? null}
				items={playerItems}
				onValueChange={(value) => onChange({ ...filters, playerCount: value === ANY ? undefined : Number(value) })}
			/>
			<BgtSimpleSelect
				className="w-full md:w-36"
				placeholder={t("games:filters.play-time")}
				value={filters.maxPlayTime ?? null}
				items={playTimeItems}
				onValueChange={(value) => onChange({ ...filters, maxPlayTime: value === ANY ? undefined : Number(value) })}
			/>
			<BgtSimpleSelect
				className="w-full md:w-36"
				placeholder={t("games:filters.weight")}
				value={filters.weight ?? null}
				items={weightItems}
				onValueChange={(value) => onChange({ ...filters, weight: value === ANY ? undefined : (value as WeightBucket) })}
			/>
		</>
	);
};
