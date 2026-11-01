import { type RefObject, useEffect, useRef } from "react";
import { useTranslation } from "react-i18next";
import XIcon from "@/assets/icons/x.svg?react";
import { BgtSimpleSelect } from "@/components/BgtForm/BgtSimpleSelect";
import { BgtSegmentedControl } from "@/components/BgtSegmentedControl/BgtSegmentedControl";
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
	show?: ShowOption;
	sort?: SortOption;
}

const ANY = "any";

const PLAYER_COUNTS = [1, 2, 3, 4, 5, 6, 7, 8];
const PLAY_TIMES = [30, 60, 90, 120, 180];
const WEIGHTS: WeightBucket[] = ["light", "medium", "heavy"];
const AGES: AgeBucket[] = ["0-6", "7-9", "10-12", "13plus"];
const SHOW_OPTIONS: ShowOption[] = ["wanted", "all"];

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

export type ShowOption = "wanted" | "all";
export type SortOption = "name" | "recent" | "plays" | "added";
export type FilterKey = "category" | "players" | "playTime" | "weight" | "age" | "show";

interface Props {
	categories: string[];
	search: GamesFilterSearch;
	onChange: (partial: Partial<GamesFilterSearch>) => void;
}

interface FilterField {
	key: FilterKey;
	label: string;
	items: BgtSelectItem[];
	value: string | number | undefined;
	defaultLabel: string;
	primary?: boolean;
	hasSearch?: boolean;
	segmented?: "always" | "stacked";
}

const useFilterFields = ({ categories, search }: Pick<Props, "categories" | "search">): FilterField[] => {
	const { t } = useTranslation(["games"]);
	const any = t("games:filters.any");

	return [
		{
			key: "players",
			label: t("games:filters.player-count"),
			items: PLAYER_COUNTS.map((c) => ({ value: c, label: c.toString() })),
			value: search.players,
			defaultLabel: any,
			primary: true,
		},
		{
			key: "playTime",
			label: t("games:filters.play-time"),
			items: PLAY_TIMES.map((m) => ({ value: m, label: t("games:filters.play-time-value", { count: m }) })),
			value: search.playTime,
			defaultLabel: any,
			primary: true,
			segmented: "stacked",
		},
		{
			key: "category",
			label: t("games:filters.category"),
			items: categories.map((name) => ({ value: name, label: name })),
			value: search.category,
			defaultLabel: any,
			hasSearch: true,
		},
		{
			key: "weight",
			label: t("games:filters.weight"),
			items: WEIGHTS.map((w) => ({ value: w, label: t(`games:filters.weight-${w}`) })),
			value: search.weight,
			defaultLabel: any,
			segmented: "always",
		},
		{
			key: "age",
			label: t("games:filters.age"),
			items: AGES.map((a) => ({ value: a, label: t(`games:filters.age-${a}`) })),
			value: search.age,
			defaultLabel: any,
			segmented: "always",
		},
		{
			key: "show",
			label: t("games:filters.show"),
			items: SHOW_OPTIONS.map((s) => ({ value: s, label: t(`games:filters.show-${s}`) })),
			value: search.show,
			defaultLabel: t("games:filters.show-collection"),
			segmented: "always",
		},
	];
};

const toSearchValue = (key: FilterKey, value: string | number | undefined): Partial<GamesFilterSearch> => {
	if (value === undefined || value === ANY) return { [key]: undefined };
	return { [key]: key === "players" || key === "playTime" ? Number(value) : value };
};

const valueLabel = (field: FilterField) =>
	field.items.find((item) => item.value === field.value)?.label ?? String(field.value);

export const useSecondaryFilterCount = (props: Pick<Props, "categories" | "search">) =>
	useFilterFields(props).filter((field) => !field.primary && field.value !== undefined).length;

interface GamesFiltersProps extends Props {
	group: "primary" | "secondary" | "all";
	stacked?: boolean;
}

export const GamesFilters = ({ group, stacked = false, onChange, ...props }: GamesFiltersProps) => {
	const fields = useFilterFields(props).filter(
		(field) => group === "all" || (group === "primary") === (field.primary ?? false),
	);

	return fields.map((field) => {
		const update = (value: string | number) => onChange(toSearchValue(field.key, value));
		const segmented = field.segmented === "always" || (field.segmented === "stacked" && stacked);

		if (segmented) {
			const control = (
				<BgtSegmentedControl
					key={field.key}
					size="sm"
					aria-label={field.label}
					value={field.value?.toString() ?? ANY}
					options={[
						{ value: ANY, label: field.defaultLabel },
						...field.items.map((item) => ({ value: item.value.toString(), label: item.label })),
					]}
					onChange={update}
					className="pointer-coarse:[&_button]:min-h-11"
				/>
			);
			return stacked ? (
				<div key={field.key} className="flex flex-col gap-1.5">
					<span aria-hidden="true" className="text-sm font-medium text-white">
						{field.label}
					</span>
					{control}
				</div>
			) : (
				control
			);
		}

		return (
			<BgtSimpleSelect
				key={field.key}
				className={stacked ? "w-full pointer-coarse:[&_button]:min-h-11" : "w-auto min-w-36"}
				label={stacked ? field.label : undefined}
				placeholder={stacked ? field.defaultLabel : field.label}
				valuePrefix={stacked ? undefined : field.label}
				aria-label={field.label}
				value={field.value ?? null}
				items={[{ value: ANY, label: field.defaultLabel }, ...field.items]}
				hasSearch={field.hasSearch}
				onValueChange={update}
			/>
		);
	});
};

interface ChipsProps extends Props {
	counts?: Partial<Record<FilterKey, number>>;
	focusRef: RefObject<number | null>;
	onEmpty?: () => void;
}

export const ActiveFilterChips = ({ counts, focusRef, onEmpty, onChange, ...props }: ChipsProps) => {
	const { t } = useTranslation(["games"]);
	const fields = useFilterFields(props).filter((field) => field.value !== undefined);
	const listRef = useRef<HTMLDivElement>(null);

	useEffect(() => {
		if (focusRef.current === null) return;
		const buttons = listRef.current?.querySelectorAll<HTMLButtonElement>("button:not(:disabled)");
		const target = buttons?.[Math.min(focusRef.current, buttons.length - 1)];
		focusRef.current = null;
		if (target) target.focus();
		else onEmpty?.();
	});

	return (
		<div ref={listRef} className="contents">
			{fields.map((field, index) => {
				const text = t("games:filters.chip", { filter: field.label, value: valueLabel(field) });
				const count = counts?.[field.key];
				return (
					<button
						key={field.key}
						type="button"
						disabled={count === 0}
						onClick={() => {
							focusRef.current = index;
							onChange({ [field.key]: undefined });
						}}
						aria-label={
							count === undefined
								? t("games:filters.remove", { filter: text })
								: t("games:filters.without", { filter: text, count })
						}
						className="inline-flex items-center gap-1.5 min-h-8 rounded-full border border-primary/40 bg-primary/15 pl-3 pr-2 text-xs font-medium text-white hover:bg-primary/25 cursor-pointer disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:bg-primary/15 pointer-coarse:min-h-11"
					>
						{text}
						{count !== undefined && <span className="text-white/60 tabular-nums">→ {t("games:count", { count })}</span>}
						<XIcon aria-hidden="true" className="size-3.5 text-primary-text" />
					</button>
				);
			})}
		</div>
	);
};
