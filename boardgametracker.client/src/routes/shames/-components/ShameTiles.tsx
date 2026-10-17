import { useTranslation } from "react-i18next";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { Shame, ShameStatistics } from "@/models";
import { toDisplay, toMonthYear } from "@/utils/dateUtils";
import { EMPTY_PRICE } from "@/utils/numberUtils";
import { oldestNeverPlayed } from "../-utils/shameFilters";

interface Props {
	statistics: ShameStatistics;
	shames: Shame[];
	currency: string;
	dateFormat: string;
}

export const ShameTiles = ({ statistics, shames, currency, dateFormat }: Props) => {
	const { t, i18n } = useTranslation("shames");
	const format = useNumberFormat();
	const oldest = oldestNeverPlayed(shames);
	const paid = statistics.totalValue ?? 0;

	const pricedCaption = t("tiles.priced", {
		priced: format.integer(statistics.pricedGameCount),
		count: statistics.count,
		total: format.integer(statistics.count),
	});
	const paidCaption =
		statistics.collectionValue > 0
			? `${pricedCaption} · ${t("tiles.share", {
					share: format.ratio(paid, statistics.collectionValue),
					total: format.price(statistics.collectionValue, currency),
				})}`
			: pricedCaption;

	const plannedCaption = () => {
		const planned = shames.filter((shame) => shame.plannedGameNightId !== null && shame.plannedFor !== null);
		const nights = new Set(planned.map((shame) => shame.plannedGameNightId));
		if (nights.size > 1) return t("tiles.planned-across", { count: nights.size });
		if (planned.length > 0)
			return t("tiles.planned-next", { date: toDisplay(planned[0].plannedFor, dateFormat, i18n.language) });
		return statistics.nextGameNight
			? t("tiles.planned-for", { date: toDisplay(statistics.nextGameNight.startDate, dateFormat, i18n.language) })
			: t("tiles.no-night");
	};

	return (
		<div className="grid grid-cols-2 gap-3 lg:grid-cols-4 xl:gap-6">
			<BgtTextStatistic
				title={t("tiles.on-shelf")}
				content={statistics.count}
				caption={t("tiles.of-owned", { count: statistics.ownedCount, total: format.integer(statistics.ownedCount) })}
			/>
			<BgtTextStatistic
				title={t("tiles.never-played")}
				content={statistics.neverPlayedCount}
				caption={
					oldest
						? oldest.additionDate
							? t("tiles.oldest-since", {
									title: oldest.title,
									date: toMonthYear(oldest.additionDate, i18n.language),
								})
							: t("tiles.oldest", { title: oldest.title })
						: undefined
				}
			/>
			<BgtTextStatistic
				title={t("tiles.paid-not-played")}
				content={statistics.pricedGameCount > 0 ? format.price(paid, currency) : EMPTY_PRICE}
				caption={statistics.pricedGameCount > 0 ? paidCaption : t("tiles.no-prices")}
			/>
			<BgtTextStatistic title={t("tiles.planned")} content={statistics.plannedCount} caption={plannedCaption()} />
		</div>
	);
};
