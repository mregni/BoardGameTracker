import { cx } from "class-variance-authority";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import CaretDown from "@/assets/icons/caret-down.svg?react";
import { BgtDatePicker } from "@/components/BgtForm/BgtDatePicker";
import { BgtSelect } from "@/components/BgtForm/BgtSelect";
import type { BgtSelectImageItem, BgtSelectItem, Game } from "@/models";
import {
	DEFAULT_MIN_SESSIONS,
	DEFAULT_RANK_BY,
	type LeaderboardPeriod,
	type LeaderboardSearch,
	MIN_SESSION_OPTIONS,
	PERIODS,
	RANK_BY_OPTIONS,
} from "../-utils/leaderboardSearch";

const ALL_GAMES = 0;

const RANK_BY_KEYS = {
	wins: "filters.rank-by-wins",
	winRate: "filters.rank-by-win-rate",
	sessions: "filters.rank-by-sessions",
} as const;

interface Props {
	search: LeaderboardSearch;
	games: Game[];
	onChange: (next: Partial<LeaderboardSearch>) => void;
}

export const LeaderboardFilters = ({ search, games, onChange }: Props) => {
	const { t } = useTranslation("leaderboard");
	const period: LeaderboardPeriod = search.range ?? "all";
	const [moreOpen, setMoreOpen] = useState(false);
	const activeMore = [search.game, search.min, search.rank].filter((value) => value !== undefined).length;

	const periodLabels: Record<LeaderboardPeriod, string> = {
		all: t("common:period.all-time"),
		year: t("common:period.this-year"),
		"12m": t("common:period.last-12-months"),
		custom: t("common:period.custom"),
	};

	const shortLabels: Partial<Record<LeaderboardPeriod, string>> = {
		"12m": t("common:period.last-12-months-short"),
	};

	const gameItems = useMemo<(BgtSelectItem | BgtSelectImageItem)[]>(
		() => [
			{ value: ALL_GAMES, label: t("filters.all-games") },
			...[...games]
				.sort((a, b) => a.title.localeCompare(b.title))
				.map((game) => ({ value: game.id, label: game.title, image: game.image })),
		],
		[games, t],
	);

	const minItems = MIN_SESSION_OPTIONS.map((count) => ({
		value: count,
		label: t("filters.min-sessions-value", { count }),
	}));

	const rankItems = RANK_BY_OPTIONS.map((rankBy) => ({ value: rankBy, label: t(RANK_BY_KEYS[rankBy]) }));

	const selectPeriod = (next: LeaderboardPeriod) => {
		onChange({
			range: next === "all" ? undefined : next,
			from: next === "custom" ? search.from : undefined,
			to: next === "custom" ? search.to : undefined,
		});
	};

	return (
		<div className="flex flex-col gap-3">
			<div className="grid grid-cols-2 gap-3 xl:grid-cols-[minmax(0,auto)_minmax(0,1fr)_minmax(0,11rem)_minmax(0,11rem)] xl:items-end">
				<fieldset className="col-span-2 min-w-0 md:col-span-1">
					<legend className="p-0 text-[15px] font-medium leading-7">{t("common:period.label")}</legend>
					<div className="grid h-11 grid-cols-4 gap-1 rounded-lg border border-primary/30 bg-background p-1 md:h-10">
						{PERIODS.map((option) => (
							<label
								key={option}
								className={cx(
									"flex min-w-0 cursor-pointer items-center justify-center rounded-md px-1 text-sm transition-colors has-focus-visible:outline-2 has-focus-visible:outline-offset-2 has-focus-visible:outline-primary sm:px-3",
									period === option
										? "bg-primary-strong text-white"
										: "text-white/70 hover:bg-primary/20 hover:text-white",
								)}
							>
								<input
									type="radio"
									name="leaderboard-period"
									className="sr-only"
									checked={period === option}
									onChange={() => selectPeriod(option)}
									aria-label={periodLabels[option]}
								/>
								<span aria-hidden="true" className="truncate">
									{shortLabels[option] ? (
										<>
											<span className="sm:hidden md:inline xl:hidden">{shortLabels[option]}</span>
											<span className="hidden sm:inline md:hidden xl:inline">{periodLabels[option]}</span>
										</>
									) : (
										periodLabels[option]
									)}
								</span>
							</label>
						))}
					</div>
				</fieldset>
				<button
					type="button"
					aria-expanded={moreOpen}
					aria-controls="leaderboard-more-filters"
					onClick={() => setMoreOpen(!moreOpen)}
					className="col-span-2 flex h-11 items-center justify-between rounded-lg border border-primary/30 bg-background px-3 text-sm transition-colors hover:border-primary/60 md:hidden"
				>
					<span>
						{t("filters.more")}
						{activeMore > 0 && <span className="ml-1.5 text-primary-text">({activeMore})</span>}
					</span>
					<CaretDown aria-hidden="true" className={cx("size-4 transition-transform", moreOpen && "rotate-180")} />
				</button>
				<div
					id="leaderboard-more-filters"
					className={cx("col-span-2 grid-cols-2 gap-3 md:contents", moreOpen ? "grid" : "hidden")}
				>
					<div className="col-span-2 min-w-0 md:col-span-1">
						<BgtSelect
							label={t("filters.game")}
							items={gameItems}
							value={search.game ?? ALL_GAMES}
							hasSearch
							onValueChange={(value) => onChange({ game: Number(value) === ALL_GAMES ? undefined : Number(value) })}
						/>
					</div>
					<BgtSelect
						label={t("filters.min-sessions")}
						items={minItems}
						value={search.min ?? DEFAULT_MIN_SESSIONS}
						onValueChange={(value) =>
							onChange({ min: Number(value) === DEFAULT_MIN_SESSIONS ? undefined : Number(value) })
						}
					/>
					<BgtSelect
						label={t("filters.rank-by")}
						items={rankItems}
						value={search.rank ?? DEFAULT_RANK_BY}
						onValueChange={(value) =>
							onChange({ rank: value === DEFAULT_RANK_BY ? undefined : (value as LeaderboardSearch["rank"]) })
						}
					/>
				</div>
			</div>
			{period === "custom" && (
				<div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:max-w-xl">
					<BgtDatePicker
						label={t("filters.from")}
						value={search.from ?? null}
						maxValue={search.to}
						clearable
						onValueChange={(value) => onChange({ from: value || undefined })}
					/>
					<BgtDatePicker
						label={t("filters.to")}
						value={search.to ?? null}
						minValue={search.from}
						clearable
						onValueChange={(value) => onChange({ to: value || undefined })}
					/>
				</div>
			)}
		</div>
	);
};
