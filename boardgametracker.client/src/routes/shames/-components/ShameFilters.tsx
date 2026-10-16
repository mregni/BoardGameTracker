import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import { BgtSelect } from "@/components/BgtForm/BgtSelect";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { ShameFilterState, ShameOwned, ShameShow, ShameSort } from "../-utils/shameFilters";

interface Props {
	filters: ShameFilterState;
	counts: Record<ShameShow, number>;
	months: number;
	monthChoices: number[];
	onFiltersChange: (filters: ShameFilterState) => void;
	onMonthsChange: (months: number) => void;
}

const SHOWS: ShameShow[] = ["all", "never", "played"];
const OWNED: ShameOwned[] = ["any", "1y", "2y"];
const SORTS: ShameSort[] = ["unplayed", "price", "owned", "title"];

export const ShameFilters = ({ filters, counts, months, monthChoices, onFiltersChange, onMonthsChange }: Props) => {
	const { t } = useTranslation("shames");
	const format = useNumberFormat();

	return (
		<div className="grid grid-cols-1 items-end gap-3 sm:grid-cols-2 lg:grid-cols-[auto_repeat(3,minmax(10rem,14rem))]">
			<fieldset className="flex min-w-0 flex-col sm:col-span-2 lg:col-span-1">
				<legend className="text-[15px] font-medium leading-7">{t("filters.show")}</legend>
				<div className="flex rounded-lg border border-primary/30 bg-background p-0.5">
					{SHOWS.map((show) => (
						<button
							key={show}
							type="button"
							aria-pressed={filters.show === show}
							onClick={() => onFiltersChange({ ...filters, show })}
							className={cx(
								"flex flex-1 items-center justify-center gap-1.5 rounded-md px-3 py-2 text-sm whitespace-nowrap transition-colors lg:flex-none",
								filters.show === show ? "bg-primary/60 font-semibold text-white" : "text-white/70 hover:text-white",
							)}
						>
							<span>{t(`filters.${show}`)}</span>{" "}
							<span className="tabular-nums text-white/60">{format.integer(counts[show])}</span>
						</button>
					))}
				</div>
			</fieldset>
			<BgtSelect
				label={t("filters.not-played-in")}
				value={months}
				items={monthChoices.map((choice) => ({ value: choice, label: t("filters.months", { count: choice }) }))}
				onValueChange={(value) => onMonthsChange(Number(value))}
			/>
			<BgtSelect
				label={t("filters.owned")}
				value={filters.owned}
				items={OWNED.map((owned) => ({ value: owned, label: t(`filters.owned-${owned}`) }))}
				onValueChange={(value) => onFiltersChange({ ...filters, owned: value as ShameOwned })}
			/>
			<BgtSelect
				label={t("filters.sort")}
				value={filters.sort}
				items={SORTS.map((sort) => ({ value: sort, label: t(`filters.sort-${sort}`) }))}
				onValueChange={(value) => onFiltersChange({ ...filters, sort: value as ShameSort })}
			/>
		</div>
	);
};
