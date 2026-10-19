import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { Fragment } from "react";
import { useTranslation } from "react-i18next";
import Crown from "@/assets/icons/crown.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import { CoopResultPill } from "@/components/CoopResultPill/CoopResultPill";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { Game, Player, Session } from "@/models";

interface Props {
	sessions: Session[];
	games: Game[];
	players: Player[];
	uiLanguage: string;
	canOpen: boolean;
	emptyText: string;
}

const byScore = (a: Session["playerSessions"][number], b: Session["playerSessions"][number]) =>
	(b.score ?? Number.NEGATIVE_INFINITY) - (a.score ?? Number.NEGATIVE_INFINITY);

export const PlayedSessionsList = (props: Props) => {
	const { sessions, games, players, uiLanguage, canOpen, emptyText } = props;

	const { t } = useTranslation("game-nights");
	const { number } = useNumberFormat(uiLanguage);
	const shortDuration = useShortDuration();
	const nameOf = (playerId: number) => players.find((player) => player.id === playerId)?.name ?? "";

	if (sessions.length === 0) {
		return (
			<BgtCard>
				<BgtText size="2" color="gray">
					{emptyText}
				</BgtText>
			</BgtCard>
		);
	}

	return (
		<ul className="flex flex-col gap-1.5">
			{sessions.map((session, index) => {
				const game = games.find((x) => x.id === session.gameId);
				const results = [...session.playerSessions].sort(byScore);
				const label = (x: Session["playerSessions"][number]) =>
					x.score === null || x.score === undefined ? nameOf(x.playerId) : `${nameOf(x.playerId)} ${number(x.score)}`;
				const row = (
					<>
						<span className="w-7 shrink-0 text-xs tabular-nums text-gray-400">#{sessions.length - index}</span>
						<BgtAvatar size="medium" title={game?.title} image={game?.image} />
						<span className="flex min-w-0 flex-1 flex-col gap-0.5">
							<span className="flex flex-wrap items-center gap-x-2 gap-y-1">
								<span className="font-semibold wrap-anywhere">{game?.title}</span>
								{session.isCoop && <CoopResultPill won={session.teamWon === true} />}
							</span>
							<span className="text-xs text-gray-400 wrap-anywhere">
								{results.map((x, position) => (
									<Fragment key={x.playerId}>
										{position > 0 && " · "}
										{x.won && !session.isCoop ? (
											<span className="inline-flex items-center gap-0.5 font-semibold text-green-300">
												<Crown className="size-3" aria-label={t("played.won")} />
												{label(x)}
											</span>
										) : (
											label(x)
										)}
									</Fragment>
								))}
							</span>
						</span>
						<span className="shrink-0 text-sm tabular-nums text-gray-400">{shortDuration(session.minutes)}</span>
					</>
				);
				const rowClasses = "flex items-center gap-2.5 rounded-lg border border-primary/20 bg-primary/10 px-3 py-2";

				return (
					<li key={session.id} data-testid="played-session">
						{canOpen ? (
							<Link
								to="/sessions/update/$sessionId"
								params={{ sessionId: session.id }}
								className={cx(rowClasses, "hover:border-primary/60")}
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
	);
};
