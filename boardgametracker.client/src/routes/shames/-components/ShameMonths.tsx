import { useTranslation } from "react-i18next";
import CaretDownIcon from "@/assets/icons/caret-down.svg?react";
import { BgtSimpleSelect } from "@/components/BgtForm/BgtSimpleSelect";

interface Props {
	months: number;
	monthChoices: number[];
	onMonthsChange: (months: number) => void;
}

export const ShameMonths = ({ months, monthChoices, onMonthsChange }: Props) => {
	const { t } = useTranslation("shames");

	return (
		<span className="inline-flex flex-wrap items-center gap-x-1.5">
			<span>{t("subtitle-lead")}</span>
			<BgtSimpleSelect
				aria-label={t("filters.not-played-in")}
				value={months}
				items={monthChoices.map((choice) => ({ value: choice, label: t("filters.months", { count: choice }) }))}
				onValueChange={(value) => onMonthsChange(Number(value))}
				triggerClassName="inline-flex min-h-10 items-center gap-1 rounded-md border border-primary/30 bg-background px-2 font-semibold text-white transition-colors hover:border-primary/60 focus:border-primary focus:outline-none md:min-h-8"
				trigger={
					<>
						<span>{t("filters.months", { count: months })}</span>
						<CaretDownIcon aria-hidden="true" className="size-4 text-white/60" />
					</>
				}
			/>
		</span>
	);
};
