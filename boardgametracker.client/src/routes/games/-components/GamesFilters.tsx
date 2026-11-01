import { useTranslation } from "react-i18next";
import CloseIcon from "@/assets/icons/x.svg?react";
import { BgtSimpleSelect } from "@/components/BgtForm/BgtSimpleSelect";
import { type BgtSelectItem, type Game, GameState } from "@/models";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { COMMON_LANGUAGE_CODES, getLanguageName, LANGUAGE_INDEPENDENT, LANGUAGE_NONE } from "@/utils/languageUtils";

export type WeightBucket = "light" | "medium" | "heavy";
export type AgeBucket = "0-6" | "7-9" | "10-12" | "13plus";
export type GameSort = "title" | "plays" | "last-played" | "added" | "rating" | "weight";

export interface GameFilterState {
	playerCount?: number;
	maxPlayTime?: number;
	weight?: WeightBucket;
	age?: AgeBucket;
}

export interface GamesFilterSearch {
	view?: "table";
	q?: string;
	category?: string;
	players?: number;
	playTime?: number;
	weight?: WeightBucket;
	age?: AgeBucket;
	state?: GameState;
	language?: string;
	inStock?: boolean;
	sort?: GameSort;
}

export const ANY = "any";

export const PLAYER_COUNTS = [1, 2, 3, 4, 5, 6, 7, 8];
export const PLAY_TIMES = [30, 60, 90, 120, 180];
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

export type ActiveFilterKey = "category" | "players" | "playTime" | "weight" | "age" | "state" | "language" | "inStock";

const languageLabel = (t: (key: string) => string, language: string, uiLanguage: string) => {
	if (language === LANGUAGE_NONE) return t("game:language.none");
	if (language === LANGUAGE_INDEPENDENT) return t("game:language-independent");
	return getLanguageName(language, uiLanguage);
};

interface ActiveFilterChipsProps {
	search: GamesFilterSearch;
	onRemove: (key: ActiveFilterKey) => void;
}

export const ActiveFilterChips = ({ search, onRemove }: ActiveFilterChipsProps) => {
	const { t, i18n } = useTranslation(["games", "game"]);

	const chips: { key: ActiveFilterKey; name?: string; value?: string }[] = [
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
		{
			key: "state",
			name: t("games:columns.state"),
			value: search.state && t(getItemStateTranslationKey(search.state, false)),
		},
		{
			key: "language",
			name: t("games:columns.language"),
			value: search.language && languageLabel(t, search.language, i18n.language),
		},
		{ key: "inStock", value: search.inStock ? t("games:filters.in-stock-only") : undefined },
	];

	return (
		<>
			{chips
				.filter((chip) => chip.value)
				.map((chip) => {
					const label = chip.name ? `${chip.name}: ${chip.value}` : (chip.value ?? "");
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
	search: GamesFilterSearch;
	onChange: (partial: Partial<GamesFilterSearch>) => void;
}

export const GamesFilters = (props: Props) => {
	const { categories, search, onChange } = props;
	const { t, i18n } = useTranslation(["games", "game"]);

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
	const stateItems = withAny(
		Object.values(GameState).map((value) => ({ value, label: t(getItemStateTranslationKey(value, false)) })),
	);
	const languageItems = withAny(
		[LANGUAGE_NONE, LANGUAGE_INDEPENDENT, ...COMMON_LANGUAGE_CODES].map((code) => ({
			value: code,
			label: languageLabel(t, code, i18n.language),
		})),
	);

	const select = (
		placeholder: string,
		value: string | number | undefined,
		items: BgtSelectItem[],
		onValue: (value: string | number | undefined) => void,
		hasSearch = false,
	) => (
		<BgtSimpleSelect
			className="w-full"
			placeholder={placeholder}
			valuePrefix={placeholder}
			value={value ?? null}
			items={items}
			hasSearch={hasSearch}
			onValueChange={(next) => onValue(next === ANY ? undefined : next)}
		/>
	);

	return (
		<div className="flex flex-col xl:flex-row gap-2">
			<fieldset
				aria-label={t("games:filters.group-game")}
				className="grid grid-cols-2 md:grid-cols-5 gap-2 min-w-0 xl:flex-5"
			>
				{select(
					t("games:filters.category"),
					search.category,
					categoryItems,
					(value) => onChange({ category: value === undefined ? undefined : String(value) }),
					true,
				)}
				{select(t("games:filters.player-count"), search.players, playerItems, (value) =>
					onChange({ players: value === undefined ? undefined : Number(value) }),
				)}
				{select(t("games:filters.play-time"), search.playTime, playTimeItems, (value) =>
					onChange({ playTime: value === undefined ? undefined : Number(value) }),
				)}
				{select(t("games:filters.weight"), search.weight, weightItems, (value) =>
					onChange({ weight: value as WeightBucket | undefined }),
				)}
				{select(t("games:filters.age"), search.age, ageItems, (value) =>
					onChange({ age: value as AgeBucket | undefined }),
				)}
			</fieldset>
			<fieldset
				aria-label={t("games:filters.group-copy")}
				className="grid grid-cols-2 gap-2 min-w-0 xl:flex-2 xl:border-l xl:border-primary/20 xl:pl-2"
			>
				{select(t("games:columns.state"), search.state, stateItems, (value) =>
					onChange({ state: value as GameState | undefined }),
				)}
				{select(
					t("games:columns.language"),
					search.language,
					languageItems,
					(value) => onChange({ language: value === undefined ? undefined : String(value) }),
					true,
				)}
			</fieldset>
		</div>
	);
};
