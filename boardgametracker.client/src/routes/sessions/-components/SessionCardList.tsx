import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { format } from "date-fns";
import { useTranslation } from "react-i18next";
import Crown from "@/assets/icons/crown.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtEditDeleteButtons } from "@/components/BgtButton/BgtEditDeleteButtons";
import type { Game, Player, PlayerSession, Session } from "@/models";
import { toDisplay } from "@/utils/dateUtils";

interface Props {
	sessions: Session[];
	players: Player[];
	games: Game[];
	dateFormat: string;
	timeFormat: string;
	uiLanguage: string;
	showGame?: boolean;
	canWrite: boolean;
	onEdit: (sessionId: number) => void;
	onDelete: (sessionId: number) => void;
	className?: string;
}

const byResult = (a: PlayerSession, b: PlayerSession) => {
	if (a.won !== b.won) return a.won ? -1 : 1;
	return (b.score ?? Number.NEGATIVE_INFINITY) - (a.score ?? Number.NEGATIVE_INFINITY);
};

export const SessionCardList = (props: Props) => {
	const {
		sessions,
		players,
		games,
		dateFormat,
		timeFormat,
		uiLanguage,
		showGame = false,
		canWrite,
		onEdit,
		onDelete,
		className,
	} = props;
	const { t } = useTranslation("common");

	if (sessions.length === 0) {
		return <p className={cx("py-4 text-center text-white/60", className)}>{t("no-data-yet")}</p>;
	}

	return (
		<ul className={cx("flex flex-col divide-y divide-primary/10", className)} data-testid="session-card-list">
			{sessions.map((session) => {
				const game = games.find((x) => x.id === session.gameId);
				const results = [...session.playerSessions].sort(byResult);
				const comment = session.comment?.trim();

				return (
					<li key={session.id} className="flex flex-col gap-2 py-3 first:pt-0 last:pb-0">
						<div className="flex items-start justify-between gap-3">
							<div className="flex min-w-0 flex-col gap-1">
								{showGame && game && (
									<Link
										to="/games/$gameId"
										params={{ gameId: game.id }}
										className="flex items-center gap-2 hover:text-primary"
									>
										<BgtAvatar image={game.image} title={game.title} size="medium" />
										<span className="font-semibold break-words">{game.title}</span>
									</Link>
								)}
								<span className="text-sm tabular-nums text-white/60">
									{toDisplay(session.start, dateFormat, uiLanguage)} · {format(new Date(session.start), timeFormat)} ·{" "}
									{session.minutes} {t("minutes", { count: session.minutes })}
								</span>
							</div>
							{canWrite && (
								<BgtEditDeleteButtons onEdit={() => onEdit(session.id)} onDelete={() => onDelete(session.id)} />
							)}
						</div>
						{results.length > 0 && (
							<ul className="flex flex-wrap gap-1.5" aria-label={t("players")}>
								{results.map((playerSession) => {
									const player = players.find((x) => x.id === playerSession.playerId);
									return (
										<li
											key={playerSession.playerId}
											className={cx(
												"inline-flex items-center gap-1.5 rounded-full border py-0.5 pr-2.5 pl-1 text-xs",
												playerSession.won ? "border-yellow-400/50 bg-yellow-400/10" : "border-white/10 bg-white/5",
											)}
										>
											<BgtAvatar image={player?.image} title={player?.name} size="small" />
											<span>{player?.name}</span>
											{playerSession.won && (
												<Crown className="size-3.5 text-yellow-400" role="img" aria-label={t("won")} />
											)}
											{playerSession.score != null && (
												<span className="font-semibold tabular-nums">{playerSession.score}</span>
											)}
										</li>
									);
								})}
							</ul>
						)}
						{comment && <p className="text-sm text-white/70 break-words">{comment}</p>}
					</li>
				);
			})}
		</ul>
	);
};
