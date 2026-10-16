import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtProgressBar } from "@/components/BgtProgressBar/BgtProgressBar";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { DashboardMostPlayedGame } from "@/models";

type Period = "month" | "all";

interface Props {
	allTime: DashboardMostPlayedGame[];
	thisMonth: DashboardMostPlayedGame[];
}

const periodButton = (active: boolean) =>
	cx(
		"rounded px-2.5 py-1 text-xs font-medium transition-colors",
		active ? "bg-primary/60 text-white" : "text-white/70 hover:text-white",
	);

export const MostPlayedCard = (props: Props) => {
	const { allTime, thisMonth } = props;
	const { t } = useTranslation("dashboard");
	const { integer } = useNumberFormat();
	const [period, setPeriod] = useState<Period>("all");

	const games = period === "month" ? thisMonth : allTime;
	const top = games[0]?.totalSessions ?? 0;

	return (
		<BgtCard className="gap-3">
			<section aria-labelledby="dashboard-most-played" className="flex flex-col gap-3">
				<div className="flex flex-wrap items-center justify-between gap-2">
					<h2 id="dashboard-most-played" className="font-semibold text-white">
						{t("most-played.title")}
					</h2>
					<fieldset className="flex gap-0.5 rounded-md border border-primary/30 p-0.5">
						<legend className="sr-only">{t("most-played.period")}</legend>
						<button
							type="button"
							aria-pressed={period === "month"}
							className={periodButton(period === "month")}
							onClick={() => setPeriod("month")}
						>
							{t("most-played.this-month")}
						</button>
						<button
							type="button"
							aria-pressed={period === "all"}
							className={periodButton(period === "all")}
							onClick={() => setPeriod("all")}
						>
							{t("most-played.all-time")}
						</button>
					</fieldset>
				</div>
				{games.length === 0 ? (
					<p className="py-2 text-sm text-white/60">
						{period === "month" ? t("most-played.empty-month") : t("most-played.empty")}
					</p>
				) : (
					<ol className="flex flex-col gap-1.5">
						{games.map((game, index) => (
							<li key={game.id}>
								<Link
									to="/games/$gameId"
									params={{ gameId: game.id }}
									className="grid grid-cols-[1.25rem_auto_minmax(0,1fr)_minmax(3rem,5rem)_2.5rem] items-center gap-3 rounded-md border border-transparent px-2 py-1.5 transition-colors hover:border-primary/40 hover:bg-primary/10"
								>
									<span className="text-sm font-semibold tabular-nums text-white/60">{index + 1}</span>
									<BgtAvatar image={game.image} title={game.title} size="medium" />
									<span className="truncate font-semibold text-white">{game.title}</span>
									<BgtProgressBar
										size="xs"
										value={game.totalSessions}
										max={top}
										tone="value"
										aria-label={t("most-played.times", { value: integer(game.totalSessions) })}
									/>
									<span className="text-right font-bold tabular-nums text-white">
										{t("most-played.times", { value: integer(game.totalSessions) })}
									</span>
								</Link>
							</li>
						))}
					</ol>
				)}
			</section>
		</BgtCard>
	);
};
