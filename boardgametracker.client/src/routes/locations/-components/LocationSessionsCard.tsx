import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import Calendar from "@/assets/icons/calendar.svg?react";
import Crown from "@/assets/icons/crown.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtNoData } from "@/components/BgtNoData/BgtNoData";
import { CoopResultPill } from "@/components/CoopResultPill/CoopResultPill";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { Session } from "@/models";
import { useGameById } from "@/routes/-hooks/useGameById";
import { usePlayerById } from "@/routes/-hooks/usePlayerById";
import { toDisplay } from "@/utils/dateUtils";

interface Props {
	sessions: Session[];
	dateFormat: string;
	uiLanguage: string;
	canOpen: boolean;
}

export const LocationSessionsCard = (props: Props) => {
	const { sessions, dateFormat, uiLanguage, canOpen } = props;
	const { t } = useTranslation("location");
	const shortDuration = useShortDuration();
	const { gameById } = useGameById();
	const { playerById } = usePlayerById();

	return (
		<BgtCard title={t("detail.sessions")} icon={Calendar}>
			{sessions.length === 0 ? (
				<BgtNoData />
			) : (
				<ul className="flex flex-col divide-y divide-primary/10">
					{sessions.map((session) => {
						const game = gameById(session.gameId);
						const winnerSession = session.playerSessions.find((playerSession) => playerSession.won);
						const winner = winnerSession ? playerById(winnerSession.playerId) : undefined;
						const row = (
							<>
								<span className="flex min-w-0 flex-1 items-center gap-2">
									<BgtAvatar image={game?.image} title={game?.title} size="medium" />
									<span className="truncate font-semibold">{game?.title}</span>
									{session.isCoop ? (
										<CoopResultPill won={session.teamWon === true} className="ml-auto" />
									) : (
										<span className="ml-auto flex min-w-0 shrink items-center gap-1">
											{winner && <Crown className="size-4 shrink-0 text-yellow-400" />}
											<span className="truncate">{winner?.name ?? "-"}</span>
										</span>
									)}
								</span>
								<span className="flex justify-between gap-3 text-sm tabular-nums text-white/60 sm:contents">
									<span className="shrink-0 sm:order-first sm:w-24">
										{toDisplay(session.start, dateFormat, uiLanguage)}
									</span>
									<span className="shrink-0 sm:order-last">{shortDuration(session.minutes)}</span>
								</span>
							</>
						);
						const rowClasses = "flex flex-col gap-1 py-2 sm:flex-row sm:items-center sm:gap-3";

						return (
							<li key={session.id}>
								{canOpen ? (
									<Link
										to="/sessions/update/$sessionId"
										params={{ sessionId: session.id }}
										className={cx(rowClasses, "cursor-pointer hover:text-primary-text")}
									>
										{row}
									</Link>
								) : (
									<div className={rowClasses}>{row}</div>
								)}
							</li>
						);
					})}
				</ul>
			)}
		</BgtCard>
	);
};
