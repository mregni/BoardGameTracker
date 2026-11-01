import { cva, cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import type { CompareOutcome, CompareSession, CompareStreak, Player } from "@/models";
import { useGameById } from "@/routes/-hooks/useGameById";
import { initialOf } from "../-utils/compareUtils";

const tileVariants = cva("inline-grid size-6 place-items-center rounded-md text-xs font-bold", {
	variants: {
		outcome: {
			playerOne: "bg-player-one text-red-950",
			playerTwo: "bg-player-two text-gray-950",
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
	const face = (player: Player) =>
		player.image ? <img src={player.image} alt="" className="size-5 rounded object-cover" /> : initialOf(player.name);
	const letter = (outcome: CompareOutcome) =>
		({
			playerOne: face(playerOne),
			playerTwo: face(playerTwo),
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
					<span
						className={cx(
							"ml-1 rounded-full px-2.5 py-0.5 text-xs font-semibold",
							streak.side === "playerOne" ? "bg-player-one/15 text-player-one" : "bg-player-two/15 text-player-two",
						)}
					>
						{t("form.streak", { player: streakPlayer.name, count: streak.count })}
					</span>
				)}
			</div>
		</div>
	);
};
