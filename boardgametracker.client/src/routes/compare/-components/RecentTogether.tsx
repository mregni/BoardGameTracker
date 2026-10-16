import { Link } from "@tanstack/react-router";
import { cva, cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import Crown from "@/assets/icons/crown.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { CoopResultPill } from "@/components/CoopResultPill/CoopResultPill";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { CompareSession, Player } from "@/models";
import { useGameById } from "@/routes/-hooks/useGameById";
import { usePlayerById } from "@/routes/-hooks/usePlayerById";
import { toDisplay } from "@/utils/dateUtils";
import { useShortDuration } from "../-hooks/useShortDuration";
import { SectionTitle } from "./SectionTitle";

export const RECENT_ROWS = 5;

const pillVariants = cva(
	"inline-flex items-center gap-1 justify-self-end whitespace-nowrap rounded-full px-2 py-0.5 text-xs font-semibold",
	{
		variants: {
			tone: {
				ok: "bg-ok/15 text-ok",
				neutral: "bg-card-light text-white/60",
			},
		},
	},
);

const rowClasses = cx(
	"grid grid-cols-[28px_minmax(0,1fr)_auto] items-center gap-x-2.5 gap-y-0.5 rounded-lg border border-card-border bg-card px-3 py-2",
	"sm:grid-cols-[84px_28px_minmax(0,1fr)_auto_auto]",
);

interface Props {
	sessions: CompareSession[];
	playerOne: Player;
	playerTwo: Player;
	dateFormat: string;
	uiLanguage: string;
	canOpen: boolean;
}

export const RecentTogether = ({ sessions, playerOne, playerTwo, dateFormat, uiLanguage, canOpen }: Props) => {
	const { t } = useTranslation("compare");
	const { gameById } = useGameById();
	const { playerById } = usePlayerById();
	const { number } = useNumberFormat();
	const duration = useShortDuration();

	if (sessions.length === 0) {
		return null;
	}

	const winner = (session: CompareSession) => {
		if (session.outcome === "playerOne" || session.outcome === "playerTwo") {
			const player = session.outcome === "playerOne" ? playerOne : playerTwo;
			return (
				<span className={pillVariants({ tone: "ok" })}>
					<Crown aria-hidden="true" className="size-3.5" />
					{player.name}
				</span>
			);
		}

		if (session.outcome === "sameTeam") {
			return <CoopResultPill won={session.teamWon === true} className="justify-self-end" />;
		}

		if (session.outcome === "both") {
			return <span className={pillVariants({ tone: "ok" })}>{t("recent.both-won")}</span>;
		}

		const other = session.otherWinnerId ? playerById(session.otherWinnerId) : null;
		return (
			<span className={pillVariants({ tone: "neutral" })}>
				{other && <Crown aria-hidden="true" className="size-3.5" />}
				{other?.name ?? t("recent.no-winner")}
			</span>
		);
	};

	return (
		<section aria-labelledby="recent-together-title" className="flex flex-col gap-1.5">
			<SectionTitle id="recent-together-title">{t("recent.title")}</SectionTitle>
			<ul className="flex flex-col gap-1.5">
				{sessions.slice(0, RECENT_ROWS).map((session) => {
					const game = gameById(session.gameId);
					const scores =
						session.outcome === "sameTeam"
							? ` · ${t("recent.same-team")}`
							: session.playerOneScore !== null && session.playerTwoScore !== null
								? ` · ${number(session.playerOneScore)} – ${number(session.playerTwoScore)}`
								: "";
					const content = (
						<>
							<span className="col-start-2 row-start-2 whitespace-nowrap text-xs tabular-nums text-white/60 sm:col-start-1 sm:row-start-1">
								{toDisplay(session.start, dateFormat, uiLanguage)}
							</span>
							<span className="col-start-1 row-span-2 row-start-1 sm:col-start-2 sm:row-span-1">
								<BgtAvatar image={game?.image} title={game?.title ?? ""} size="medium" />
							</span>
							<span className="col-start-2 row-start-1 truncate font-semibold sm:col-start-3">
								{game?.title}
								{scores}
							</span>
							<span className="col-start-3 row-start-1 sm:col-start-4">{winner(session)}</span>
							<span className="col-start-3 row-start-2 justify-self-end text-xs tabular-nums text-white/60 sm:col-start-5 sm:row-start-1">
								{duration(session.minutes)}
							</span>
						</>
					);

					return (
						<li key={session.sessionId}>
							{canOpen ? (
								<Link
									to="/sessions/update/$sessionId"
									params={{ sessionId: session.sessionId }}
									className={cx(rowClasses, "hover:border-primary/60")}
								>
									{content}
								</Link>
							) : (
								<div className={rowClasses}>{content}</div>
							)}
						</li>
					);
				})}
			</ul>
		</section>
	);
};
