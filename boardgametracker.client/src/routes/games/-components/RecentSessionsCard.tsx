import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Calendar from "@/assets/icons/calendar.svg?react";
import Crown from "@/assets/icons/crown.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtNoData } from "@/components/BgtNoData/BgtNoData";
import { CoopResultPill } from "@/components/CoopResultPill/CoopResultPill";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { Session } from "@/models";
import { usePlayerById } from "@/routes/-hooks/usePlayerById";
import { toDisplay } from "@/utils/dateUtils";

interface Props {
	sessions: Session[];
	dateFormat: string;
	gameId: number;
}

export const RecentSessionsCard = (props: Props) => {
	const { sessions, dateFormat, gameId } = props;
	const { t, i18n } = useTranslation(["game", "common"]);
	const { playerById } = usePlayerById();
	const shortDuration = useShortDuration();

	return (
		<BgtCard title={t("titles.recent-sessions")} icon={Calendar}>
			{sessions.length === 0 ? (
				<BgtNoData />
			) : (
				<ul className="flex flex-col divide-y divide-primary/10">
					{sessions.map((session) => {
						const winnerSession = session.playerSessions.find((playerSession) => playerSession.won);
						const winner = winnerSession ? playerById(winnerSession.playerId) : undefined;

						return (
							<li
								key={session.id}
								className="flex items-center gap-2 py-2"
								title={`${t("common:player", { count: session.playerSessions.length })} • ${shortDuration(session.minutes)}`}
							>
								<span className="shrink-0 text-sm tabular-nums text-white/60">
									{toDisplay(session.start, dateFormat, i18n.language)}
								</span>
								{session.isCoop ? (
									<span className="flex min-w-0 flex-1 items-center">
										<CoopResultPill won={session.teamWon === true} />
									</span>
								) : (
									<span className="flex min-w-0 flex-1 items-center gap-1">
										{winner && <Crown className="size-4 shrink-0 text-yellow-400" />}
										<span className="truncate">{winner?.name ?? "-"}</span>
									</span>
								)}
								{(session.isCoop ? session.teamScore : winnerSession?.score) != null && (
									<span className="shrink-0 text-sm font-semibold text-card-value">
										{session.isCoop ? session.teamScore : winnerSession?.score}
									</span>
								)}
							</li>
						);
					})}
				</ul>
			)}
			<Link
				to="/games/$gameId/sessions"
				params={{ gameId }}
				className="mt-2 self-end text-sm text-primary-text hover:underline"
			>
				{t("sessions")}
			</Link>
		</BgtCard>
	);
};
