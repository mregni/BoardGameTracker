import { useTranslation } from "react-i18next";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtProgressBar, type BgtProgressSegment } from "@/components/BgtProgressBar/BgtProgressBar";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { Shame, ShameStatistics } from "@/models";

interface Props {
	statistics: ShameStatistics;
	shames: Shame[];
	currency: string;
	onSortByPrice: () => void;
}

export const MoneyOnTheShelf = ({ statistics, shames, currency, onSortByPrice }: Props) => {
	const { t } = useTranslation("shames");
	const format = useNumberFormat();

	if (statistics.collectionValue <= 0) return null;

	const shelfValue = statistics.totalValue ?? 0;
	const neverPlayed = statistics.neverPlayedValue;
	const notPlayed = Math.max(0, shelfValue - neverPlayed);
	const rest = Math.max(0, statistics.collectionValue - shelfValue);
	const formatValue = (value: number) => format.price(value, currency);

	const segments: BgtProgressSegment[] = [
		{ key: "never", label: t("money.never-played"), value: neverPlayed, tone: "bad" },
		{ key: "not-played", label: t("money.not-played", { count: statistics.months }), value: notPlayed, tone: "warn" },
		{ key: "rest", label: t("money.rest"), value: rest, tone: "subtle" },
	];

	const unpriced = shames.filter((shame) => shame.price === null);

	return (
		<BgtCard>
			<section aria-labelledby="money-on-the-shelf" className="flex flex-col gap-2">
				<div className="flex flex-wrap items-baseline justify-between gap-2">
					<h2 id="money-on-the-shelf" className="font-semibold text-white">
						{t("money.title")}
					</h2>
					<span className="text-xs text-white/50">{t("money.subtitle")}</span>
				</div>
				<BgtProgressBar
					segments={segments}
					max={statistics.collectionValue}
					size="lg"
					track="card"
					gapped
					legend
					formatValue={formatValue}
					formatLabel={(summary) =>
						t("money.label", {
							total: formatValue(summary.max),
							parts: summary.segments
								.filter((segment) => segment.value > 0)
								.map((segment) => `${segment.label} ${formatValue(segment.value)}`)
								.join(", "),
						})
					}
				/>
				<p className="text-xs text-white/50">
					{unpriced.length === 1 && t("money.unpriced-one", { title: unpriced[0].title })}
					{unpriced.length > 1 && t("money.unpriced", { count: unpriced.length })}{" "}
					<button type="button" className="text-primary hover:underline" onClick={onSortByPrice}>
						{t("money.sort-by-price")}
					</button>
				</p>
			</section>
		</BgtCard>
	);
};
