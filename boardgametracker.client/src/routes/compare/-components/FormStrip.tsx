import { cva } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import type { CompareOutcome, CompareSession, CompareStreak, Player } from "@/models";
import { useGameById } from "@/routes/-hooks/useGameById";
import { initialOf } from "../-utils/compareUtils";

const tileVariants = cva("inline-grid size-6 place-items-center rounded-md text-[11px] font-bold", {
	variants: {
		outcome: {
			playerOne: "bg-player-one text-red-950",
			playerTwo: "bg-player-two text-white",
			both: "bg-linear-to-br from-player-one from-50% to-player-two to-50% text-white",
			other: "border border-card-border bg-card-light text-white/60",
			sameTeam: "bg-hatch text-white",
		},
	},
});

interface Props {
	sessions: CompareSession[];
	streak: CompareStreak | null;
	playerOne: Player;
	playerTwo: Player;
}

export const FormStrip = ({ sessions, streak, playerOne, playerTwo }: Props) => {
	const { t } = useTranslation("compare");
	const { gameById } = useGameById();

	if (sessions.length === 0) {
		return null;
	}

	const oldestFirst = [...sessions].reverse();
	const letter = (outcome: CompareOutcome) =>
		({
			playerOne: initialOf(playerOne.name),
			playerTwo: initialOf(playerTwo.name),
			both: "+",
			other: "–",
			sameTeam: "=",
		})[outcome];
	const who = (session: CompareSession) =>
		({
			playerOne: playerOne.name,
			playerTwo: playerTwo.name,
			both: t("form.both"),
			other: t("form.other"),
			sameTeam: session.teamWon ? t("form.same-team-won") : t("form.same-team-lost"),
		})[session.outcome];
	const streakPlayer = streak?.side === "playerOne" ? playerOne : streak?.side === "playerTwo" ? playerTwo : null;
	const hasSameTeam = sessions.some((session) => session.outcome === "sameTeam");

	return (
		<div className="flex flex-col gap-1">
			<span className="text-xs font-semibold">
				{t("form.title", { count: sessions.length })}{" "}
				<span className="font-normal text-white/60">{t("form.order")}</span>
			</span>
			<div className="flex flex-wrap items-center gap-1">
				<div
					role="img"
					aria-label={t("form.label", { list: oldestFirst.map(who).join(", ") })}
					className="flex flex-wrap gap-1"
				>
					{oldestFirst.map((session) => (
						<span
							key={session.sessionId}
							data-testid="form-tile"
							data-outcome={session.outcome}
							title={`${gameById(session.gameId)?.title ?? ""} · ${who(session)}`}
							className={tileVariants({ outcome: session.outcome })}
						>
							{letter(session.outcome)}
						</span>
					))}
				</div>
				{streak && streakPlayer && (
					<span className="ml-1 rounded-full bg-primary/15 px-2.5 py-0.5 text-xs font-semibold text-indigo-300">
						{t("form.streak", { player: streakPlayer.name, count: streak.count })}
					</span>
				)}
			</div>
			{hasSameTeam && <span className="text-xs text-white/60">{t("form.legend-coop")}</span>}
		</div>
	);
};
