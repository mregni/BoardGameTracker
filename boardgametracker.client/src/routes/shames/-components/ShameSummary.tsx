import { useTranslation } from "react-i18next";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { Shame, ShameStatistics } from "@/models";
import { toMonthYear } from "@/utils/dateUtils";
import { oldestNeverPlayed } from "../-utils/shameFilters";

interface Props {
	statistics: ShameStatistics;
	shames: Shame[];
}

export const ShameSummary = ({ statistics, shames }: Props) => {
	const { t, i18n } = useTranslation("shames");
	const format = useNumberFormat();
	const oldest = oldestNeverPlayed(shames);

	const parts = [t("summary.on-shelf", { count: statistics.count, total: format.integer(statistics.ownedCount) })];
	if (statistics.neverPlayedCount > 0) {
		parts.push(t("summary.never-played", { count: statistics.neverPlayedCount }));
	}
	if (oldest) {
		parts.push(
			oldest.additionDate
				? t("summary.oldest-since", { title: oldest.title, date: toMonthYear(oldest.additionDate, i18n.language) })
				: t("summary.oldest", { title: oldest.title }),
		);
	}

	return <p className="text-sm text-white/80">{parts.join(" · ")}</p>;
};
