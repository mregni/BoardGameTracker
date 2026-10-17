import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import Crown from "@/assets/icons/crown.svg?react";
import { AvatarChip } from "@/components/GameNight/AvatarChip";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { Player, PlayerSession } from "@/models";

interface Props {
	playerSessions: PlayerSession[];
	players: Player[];
	className?: string;
}

const byResult = (a: PlayerSession, b: PlayerSession) => {
	if (a.won !== b.won) return a.won ? -1 : 1;
	return (b.score ?? Number.NEGATIVE_INFINITY) - (a.score ?? Number.NEGATIVE_INFINITY);
};

export const SessionPlayerPills = ({ playerSessions, players, className }: Props) => {
	const { t } = useTranslation("common");
	const { decimal } = useNumberFormat();

	if (playerSessions.length === 0) return null;

	const results = [...playerSessions].sort(byResult);

	return (
		<ul className={cx("flex flex-wrap gap-1.5", className)} aria-label={t("players")}>
			{results.map((playerSession) => {
				const player = players.find((x) => x.id === playerSession.playerId);
				return (
					<li key={playerSession.playerId}>
						<AvatarChip
							title={player?.name ?? ""}
							image={player?.image}
							tone={playerSession.won ? "winner" : "neutral"}
						>
							{playerSession.won && <Crown className="size-3.5 text-yellow-400" role="img" aria-label={t("won")} />}
							{playerSession.score != null && (
								<span className="font-semibold tabular-nums">{decimal(playerSession.score)}</span>
							)}
						</AvatarChip>
					</li>
				);
			})}
		</ul>
	);
};
