import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtEditDeleteButtons } from "@/components/BgtButton/BgtEditDeleteButtons";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { Game, Player, Session } from "@/models";
import { toDisplay, toDisplayTime } from "@/utils/dateUtils";
import { SessionPlayerPills } from "./SessionPlayerPills";

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
	const shortDuration = useShortDuration();

	if (sessions.length === 0) {
		return <p className={cx("py-4 text-center text-white/60", className)}>{t("no-data-yet")}</p>;
	}

	return (
		<ul className={cx("flex flex-col divide-y divide-primary/10", className)} data-testid="session-card-list">
			{sessions.map((session) => {
				const game = games.find((x) => x.id === session.gameId);
				const comment = session.comment?.trim();

				return (
					<li key={session.id} className="flex flex-col gap-2 py-3 first:pt-0 last:pb-0">
						<div className="flex items-start justify-between gap-3">
							<div className="flex min-w-0 flex-col gap-1">
								{showGame && game && (
									<Link
										to="/games/$gameId"
										params={{ gameId: game.id }}
										className="flex items-center gap-2 hover:text-primary-text"
									>
										<BgtAvatar image={game.image} title={game.title} size="medium" />
										<span className="font-semibold break-words">{game.title}</span>
									</Link>
								)}
								<span className="text-sm tabular-nums text-white/60">
									{toDisplay(session.start, dateFormat, uiLanguage)} ·{" "}
									{toDisplayTime(session.start, timeFormat, uiLanguage)} · {shortDuration(session.minutes)}
								</span>
							</div>
							{canWrite && (
								<BgtEditDeleteButtons onEdit={() => onEdit(session.id)} onDelete={() => onDelete(session.id)} />
							)}
						</div>
						<SessionPlayerPills playerSessions={session.playerSessions} players={players} />
						{comment && <p className="text-sm text-white/70 break-words">{comment}</p>}
					</li>
				);
			})}
		</ul>
	);
};
