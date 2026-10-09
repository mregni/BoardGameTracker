import { Link } from "@tanstack/react-router";
import { format } from "date-fns";
import { useTranslation } from "react-i18next";
import Calendar from "@/assets/icons/calendar.svg?react";
import Crown from "@/assets/icons/crown.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtNoData } from "@/components/BgtNoData/BgtNoData";
import type { Session } from "@/models";
import { usePlayerById } from "@/routes/-hooks/usePlayerById";

interface Props {
	sessions: Session[];
	dateFormat: string;
	gameId: number;
}

export const RecentSessionsCard = (props: Props) => {
	const { sessions, dateFormat, gameId } = props;
	const { t } = useTranslation(["game", "common"]);
	const { playerById } = usePlayerById();

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
								title={`${t("common:player", { count: session.playerSessions.length })} • ${session.minutes}${t("common:minutes-abbreviation")}`}
							>
								<span className="shrink-0 text-sm tabular-nums text-white/60">{format(session.start, dateFormat)}</span>
								<span className="flex min-w-0 flex-1 items-center gap-1">
									{winner && <Crown className="size-4 shrink-0 text-yellow-400" />}
									<span className="truncate">{winner?.name ?? "-"}</span>
								</span>
								{winnerSession?.score != null && (
									<span className="shrink-0 text-sm font-semibold text-card-value">{winnerSession.score}</span>
								)}
							</li>
						);
					})}
				</ul>
			)}
			<Link
				to="/games/$gameId/sessions"
				params={{ gameId }}
				className="mt-2 self-end text-sm text-primary hover:underline"
			>
				{t("sessions")}
			</Link>
		</BgtCard>
	);
};
