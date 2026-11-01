import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import { BgtSelect } from "@/components/BgtForm/BgtSelect";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import {
	SHAME_OWNED,
	SHAME_SHOWS,
	SHAME_SORTS,
	type ShameFilterState,
	type ShameOwned,
	type ShameShow,
	type ShameSort,
} from "../-utils/shameFilters";

interface Props {
	filters: ShameFilterState;
	counts: Record<ShameShow, number>;
	onFiltersChange: (filters: ShameFilterState) => void;
}

export const ShameFilters = ({ filters, counts, onFiltersChange }: Props) => {
	const { t } = useTranslation("shames");
	const format = useNumberFormat();

	return (
		<div className="grid grid-cols-1 items-end gap-3 sm:grid-cols-2 lg:grid-cols-[auto_repeat(2,minmax(10rem,14rem))]">
			<fieldset className="flex min-w-0 flex-col sm:col-span-2 lg:col-span-1">
				<legend className="text-[15px] font-medium leading-7">{t("filters.show")}</legend>
				<div className="flex rounded-lg border border-primary/30 bg-background p-0.5">
					{SHAME_SHOWS.map((show) => (
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
							<span className={cx("tabular-nums", filters.show === show ? "text-white" : "text-white/60")}>
								{format.integer(counts[show])}
							</span>
						</button>
					))}
				</div>
			</fieldset>
			<BgtSelect
				label={t("filters.owned")}
				value={filters.owned}
				items={SHAME_OWNED.map((owned) => ({ value: owned, label: t(`filters.owned-${owned}`) }))}
				onValueChange={(value) => onFiltersChange({ ...filters, owned: value as ShameOwned })}
			/>
			<BgtSelect
				label={t("filters.sort")}
				value={filters.sort}
				items={SHAME_SORTS.map((sort) => ({ value: sort, label: t(`filters.sort-${sort}`) }))}
				onValueChange={(value) => onFiltersChange({ ...filters, sort: value as ShameSort })}
			/>
		</div>
	);
};
