import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtProgressBar } from "@/components/BgtProgressBar/BgtProgressBar";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { DashboardTopPlayer } from "@/models";
import { cardLinkClasses } from "./dashboardStyles";

interface Props {
	players: DashboardTopPlayer[];
}

export const TopPlayersCard = (props: Props) => {
	const { players } = props;
	const { t } = useTranslation("dashboard");
	const { integer, percent } = useNumberFormat();

	return (
		<BgtCard className="gap-3">
			<section aria-labelledby="dashboard-top-players" className="flex flex-col gap-3">
				<div className="flex items-center justify-between gap-2">
					<h2 id="dashboard-top-players" className="font-semibold text-white">
						{t("top-players.title")}
					</h2>
					<Link to="/leaderboard" className={cardLinkClasses}>
						{t("top-players.leaderboard")}
						<CaretRight className="size-3.5" aria-hidden="true" />
					</Link>
				</div>
				{players.length === 0 ? (
					<p className="py-2 text-sm text-white/60">{t("top-players.empty")}</p>
				) : (
					<>
						<ol className="flex flex-col gap-1.5">
							{players.map((player, index) => {
								const winRate = player.playCount > 0 ? player.winCount / player.playCount : 0;
								return (
									<li key={player.id}>
										<Link
											to="/players/$playerId"
											params={{ playerId: player.id }}
											title={t("top-players.row", {
												name: player.name,
												wins: integer(player.winCount),
												count: player.playCount,
												total: integer(player.playCount),
											})}
											className={cx(
												"grid grid-cols-[1.25rem_auto_minmax(0,1fr)_minmax(3rem,5rem)_auto] items-center gap-3 rounded-md border border-transparent px-2 py-1.5 transition-colors hover:border-primary/40 hover:bg-primary/10",
												index === 0 && "bg-primary/10",
											)}
										>
											<span className="text-sm font-semibold tabular-nums text-white/60">{index + 1}</span>
											<BgtAvatar image={player.image} title={player.name} size="medium" />
											<span className="flex min-w-0 flex-col">
												<span className="truncate font-semibold text-white">{player.name}</span>
												<span className="text-xs text-white/60">
													{t("top-players.sessions", { count: player.playCount, value: integer(player.playCount) })}
												</span>
											</span>
											<BgtProgressBar
												size="xs"
												value={player.winCount}
												max={player.playCount}
												tone="ok"
												aria-label={t("top-players.won", { percent: percent(winRate) })}
											/>
											<span className="flex flex-col items-end">
												<span className="font-bold tabular-nums text-white">{integer(player.winCount)}</span>
												<span className="text-xs text-white/60">
													{t("top-players.won", { percent: percent(winRate) })}
												</span>
											</span>
										</Link>
									</li>
								);
							})}
						</ol>
						<p className="text-xs text-white/50">{t("top-players.hint")}</p>
					</>
				)}
			</section>
		</BgtCard>
	);
};
