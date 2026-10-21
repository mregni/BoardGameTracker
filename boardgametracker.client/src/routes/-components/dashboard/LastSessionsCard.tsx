import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import Crown from "@/assets/icons/crown.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { CoopResultPill } from "@/components/CoopResultPill/CoopResultPill";
import { AvatarChip } from "@/components/GameNight/AvatarChip";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { RecentActivity } from "@/models";
import { toDisplay } from "@/utils/dateUtils";
import { dashboardRowClasses, pillVariants } from "./dashboardStyles";

interface Props {
	sessions: RecentActivity[];
	dateFormat: string;
	uiLanguage: string;
}

export const LastSessionsCard = (props: Props) => {
	const { sessions, dateFormat, uiLanguage } = props;
	const { t } = useTranslation(["dashboard", "common"]);
	const shortDuration = useShortDuration();

	return (
		<section aria-labelledby="dashboard-last-sessions" className="flex flex-col gap-2">
			<h2 id="dashboard-last-sessions" className="font-semibold text-white">
				{t("last-sessions.title")}
			</h2>
			{sessions.length === 0 ? (
				<p className={cx(dashboardRowClasses, "px-4 py-6 text-center text-sm text-white/60 hover:border-primary/20")}>
					{t("last-sessions.empty")}
				</p>
			) : (
				<ul className="flex flex-col gap-2">
					{sessions.map((session) => (
						<li key={session.id}>
							<Link
								to="/games/$gameId/sessions"
								params={{ gameId: session.gameId }}
								className={cx(
									dashboardRowClasses,
									"grid grid-cols-[2.75rem_minmax(0,1fr)_auto] items-center gap-x-3 gap-y-1 px-3 py-2",
								)}
							>
								<span className="row-span-2 flex justify-center">
									<BgtAvatar image={session.gameImage} title={session.gameTitle} size="large" />
								</span>
								<span className="flex min-w-0 flex-col">
									<span className="truncate font-semibold text-white">{session.gameTitle}</span>
									<span className="truncate text-xs tabular-nums text-white/60">
										{toDisplay(session.start, dateFormat, uiLanguage)} ·{" "}
										{t("last-sessions.players", { count: session.playerCount })}
									</span>
								</span>
								<span className="text-sm tabular-nums text-white/70">{shortDuration(session.durationInMinutes)}</span>
								<span className="col-span-2 flex flex-wrap gap-1.5">
									{session.isCoop && <CoopResultPill won={session.teamWon === true} />}
									{!session.isCoop && session.winners.length === 0 && (
										<span className={pillVariants({ tone: "neutral" })}>{t("last-sessions.no-winner")}</span>
									)}
									{!session.isCoop &&
										session.winners.map((winner) => (
											<AvatarChip key={winner.id} title={winner.name} image={winner.image} tone="winner" size="compact">
												<Crown className="size-3.5 text-yellow-400" role="img" aria-label={t("common:won")} />
											</AvatarChip>
										))}
								</span>
							</Link>
						</li>
					))}
				</ul>
			)}
		</section>
	);
};
