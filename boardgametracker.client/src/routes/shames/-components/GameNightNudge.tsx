import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import type { Shame, ShameStatistics } from "@/models";
import { toDisplay } from "@/utils/dateUtils";

interface Props {
	statistics: ShameStatistics;
	shames: Shame[];
	dateFormat: string;
	canPlan: boolean;
}

export const GameNightNudge = ({ statistics, shames, dateFormat, canPlan }: Props) => {
	const { t, i18n } = useTranslation("shames");
	const night = statistics.nextGameNight;
	const planned = statistics.plannedCount;

	if (!night) {
		if (!canPlan) return null;
		return (
			<p className="rounded-lg border border-dashed border-primary/30 px-3 py-2 text-sm text-white/60">
				{t("plan.no-night")}{" "}
				<Link
					to="/game-nights"
					className="inline-flex min-h-10 items-center font-semibold text-primary-text hover:underline md:min-h-0"
				>
					{t("plan.plan-night")}
				</Link>
			</p>
		);
	}

	const nights = new Set(
		shames.filter((shame) => shame.plannedGameNightId !== null).map((shame) => shame.plannedGameNightId),
	);
	const progress =
		planned === 0
			? canPlan && t("nudge.pick")
			: nights.size > 1
				? t("nudge.planned-across", { count: planned, nights: nights.size })
				: t("nudge.planned", { count: planned });

	return (
		<p
			className={cx(
				"rounded-lg border px-3 py-2 text-sm text-white/70",
				planned > 0 ? "border-ok/40 bg-ok/5" : "border-primary/30 bg-primary/5",
			)}
		>
			<span className="font-semibold text-white">
				{t("nudge.next", { title: night.title, date: toDisplay(night.startDate, dateFormat, i18n.language) })}
			</span>
			{progress && <> {progress}</>}
		</p>
	);
};
