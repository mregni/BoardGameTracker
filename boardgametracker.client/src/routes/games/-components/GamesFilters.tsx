import { useTranslation } from "react-i18next";
import CloseIcon from "@/assets/icons/x.svg?react";
import { BgtSimpleSelect } from "@/components/BgtForm/BgtSimpleSelect";
import type { BgtSelectItem, Game } from "@/models";

export type WeightBucket = "light" | "medium" | "heavy";
export type AgeBucket = "0-6" | "7-9" | "10-12" | "13plus";

export interface GameFilterState {
	playerCount?: number;
	maxPlayTime?: number;
	weight?: WeightBucket;
	age?: AgeBucket;
}

export interface GamesFilterSearch {
	q?: string;
	category?: string;
	players?: number;
	playTime?: number;
	weight?: WeightBucket;
	age?: AgeBucket;
}

const ANY = "any";

const PLAYER_COUNTS = [1, 2, 3, 4, 5, 6, 7, 8];
const PLAY_TIMES = [30, 60, 90, 120, 180];
const WEIGHTS: WeightBucket[] = ["light", "medium", "heavy"];
const AGES: AgeBucket[] = ["0-6", "7-9", "10-12", "13plus"];

const matchesAge = (game: Game, bucket: AgeBucket): boolean => {
	const age = game.minAge;
	if (age === null || age <= 0) return false;
	if (bucket === "0-6") return age <= 6;
	if (bucket === "7-9") return age >= 7 && age <= 9;
	if (bucket === "10-12") return age >= 10 && age <= 12;
	return age >= 13;
};

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
		if (filters.age !== undefined && !matchesAge(game, filters.age)) return false;
		return true;
	});
};

export type ActiveFilterKey = "category" | "players" | "playTime" | "weight" | "age";

interface ActiveFilterChipsProps {
	search: GamesFilterSearch;
	onRemove: (key: ActiveFilterKey) => void;
}

export const ActiveFilterChips = ({ search, onRemove }: ActiveFilterChipsProps) => {
	const { t } = useTranslation(["games"]);

	const chips: { key: ActiveFilterKey; name: string; value?: string }[] = [
		{ key: "category", name: t("games:filters.category"), value: search.category },
		{
			key: "players",
			name: t("games:filters.player-count"),
			value: search.players ? t("games:filters.player-count-value", { count: search.players }) : undefined,
		},
		{
			key: "playTime",
			name: t("games:filters.play-time"),
			value: search.playTime ? t("games:filters.play-time-value", { count: search.playTime }) : undefined,
		},
		{
			key: "weight",
			name: t("games:filters.weight"),
			value: search.weight && t(`games:filters.weight-${search.weight}`),
		},
		{ key: "age", name: t("games:filters.age"), value: search.age && t(`games:filters.age-${search.age}`) },
	];

	return (
		<>
			{chips
				.filter((chip) => chip.value)
				.map((chip) => {
					const label = `${chip.name}: ${chip.value}`;
					return (
						<button
							key={chip.key}
							type="button"
							onClick={() => onRemove(chip.key)}
							aria-label={t("games:filters.remove", { name: label })}
							className="flex items-center gap-2 px-3 py-1 rounded-full text-xs text-chip-selected bg-primary/20 border border-primary/30 hover:bg-primary/30 cursor-pointer"
						>
							{label}
							<CloseIcon className="size-3" aria-hidden />
						</button>
					);
				})}
		</>
	);
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
	const ageItems = withAny(AGES.map((a) => ({ value: a, label: t(`games:filters.age-${a}`) })));

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
			<BgtSimpleSelect
				className="w-full md:w-36"
				placeholder={t("games:filters.age")}
				value={filters.age ?? null}
				items={ageItems}
				onValueChange={(value) => onChange({ ...filters, age: value === ANY ? undefined : (value as AgeBucket) })}
			/>
		</>
	);
};
