import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import Crown from "@/assets/icons/crown.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import type { Game, Session } from "@/models";
import { formatMinutesToDuration, toDisplay } from "@/utils/dateUtils";

interface Props {
	session: Session;
	game: Game | null;
	winners: string[];
	dateFormat: string;
	uiLanguage: string;
	canOpen: boolean;
}

const rowClasses =
	"grid items-center gap-x-2.5 gap-y-0.5 rounded-lg border border-primary/20 bg-primary/10 px-3 py-2 grid-cols-[1.75rem_minmax(0,1fr)_auto] [grid-template-areas:'av_game_winner'_'av_date_dur'] sm:grid-cols-[5.5rem_1.75rem_minmax(0,1fr)_auto_auto] sm:[grid-template-areas:'date_av_game_winner_dur']";

export const LocationSessionRow = (props: Props) => {
	const { session, game, winners, dateFormat, uiLanguage, canOpen } = props;
	const winnerNames = winners.join(", ");

	const content = (
		<>
			<span className="[grid-area:date] text-xs text-white/60 tabular-nums">
				{toDisplay(session.start, dateFormat, uiLanguage)}
			</span>
			<span className="[grid-area:av]">
				<BgtAvatar image={game?.image} title={game?.title} size="medium" />
			</span>
			<span className="[grid-area:game] min-w-0 truncate font-semibold text-white" title={game?.title}>
				{game?.title}
			</span>
			<span className="[grid-area:winner] justify-self-end min-w-0">
				{winners.length > 0 && (
					<span
						className="inline-flex max-w-40 items-center gap-1 rounded-sm bg-green-500/15 px-2 py-0.5 text-xs font-medium text-green-400"
						title={winnerNames}
					>
						<Crown aria-hidden="true" className="size-3.5 shrink-0" />
						<span className="truncate">{winnerNames}</span>
					</span>
				)}
			</span>
			<span className="[grid-area:dur] justify-self-end text-xs text-white/60 tabular-nums">
				{formatMinutesToDuration(session.minutes, ["hours", "minutes"], uiLanguage)}
			</span>
		</>
	);

	if (!canOpen) {
		return <div className={rowClasses}>{content}</div>;
	}

	return (
		<Link
			to="/sessions/update/$sessionId"
			params={{ sessionId: session.id }}
			className={cx(rowClasses, "cursor-pointer transition-colors hover:border-primary/60")}
		>
			{content}
		</Link>
	);
};
