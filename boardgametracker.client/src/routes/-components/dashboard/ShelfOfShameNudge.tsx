import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import AlertTriangle from "@/assets/icons/alert-triangle.svg?react";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { DashboardShelfOfShame } from "@/models";
import { dashboardRowClasses } from "./dashboardStyles";

interface Props {
	shelfOfShame: DashboardShelfOfShame | null;
	currency: string;
}

export const ShelfOfShameNudge = (props: Props) => {
	const { shelfOfShame, currency } = props;
	const { t } = useTranslation("dashboard");
	const { integer, price } = useNumberFormat();

	if (shelfOfShame === null || shelfOfShame.count === 0) return null;

	const details = [
		shelfOfShame.neverPlayedCount > 0
			? t("shame.never-played", {
					count: shelfOfShame.neverPlayedCount,
					value: integer(shelfOfShame.neverPlayedCount),
				})
			: null,
		shelfOfShame.totalValue !== null ? t("shame.paid", { value: price(shelfOfShame.totalValue, currency) }) : null,
	].filter((detail) => detail !== null);

	return (
		<Link
			to="/shames"
			className={cx(dashboardRowClasses, "grid grid-cols-[2.25rem_minmax(0,1fr)_auto] items-center gap-3 px-4 py-3")}
		>
			<span className="grid size-9 place-items-center rounded-lg bg-warn/15 text-warn">
				<AlertTriangle className="size-5" aria-hidden="true" />
			</span>
			<span className="flex min-w-0 flex-col gap-0.5">
				<strong className="font-semibold text-white">
					{t("shame.title", { count: shelfOfShame.count, value: integer(shelfOfShame.count) })}
				</strong>
				{details.length > 0 && <span className="text-sm text-white/60">{details.join(" · ")}</span>}
			</span>
			<CaretRight className="size-4 text-primary-text" aria-hidden="true" />
		</Link>
	);
};
