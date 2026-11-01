import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { memo, useMemo } from "react";
import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtText } from "@/components/BgtText/BgtText";
import type { Game, Session } from "@/models";
import { usePlayerById } from "@/routes/-hooks/usePlayerById";
import { toDisplay } from "@/utils/dateUtils";
import { winnerIdsOf, wonSession } from "../-utils/rivalry";

interface Props {
	session: Session;
	game?: Game;
	dateFormat: string;
	uiLanguage: string;
	playerId: number;
	canOpen?: boolean;
}

const rowClasses = "flex items-center gap-3 py-2";

const PlayerSessionCardItemComponent = (props: Props) => {
	const { session, game, dateFormat, uiLanguage, playerId, canOpen = false } = props;
	const { t } = useTranslation(["common", "player"]);
	const { playerById } = usePlayerById();

	const playerSession = useMemo(
		() => session.playerSessions.find((ps) => ps.playerId === playerId) ?? null,
		[playerId, session.playerSessions],
	);

	const won = wonSession(session, playerId);
	const winners = won
		? []
		: winnerIdsOf(session, playerId).flatMap((id) => {
				const name = playerById(id)?.name;
				return name ? [name] : [];
			});
	const details = [
		toDisplay(session.start, dateFormat, uiLanguage),
		t("player:session.players", { count: session.playerSessions.length }),
		winners.length > 0 && t("player:session.won-by", { names: winners.join(", ") }),
	].filter(Boolean);

	const content = (
		<>
			<span aria-hidden="true">
				<BgtAvatar image={game?.image} title={game?.title} size="large" />
			</span>
			<span className="min-w-0 flex-1">
				<span className="block truncate font-semibold">{game?.title}</span>
				<span className="block truncate text-xs tabular-nums text-white/60">{details.join(" · ")}</span>
			</span>
			<span className="flex shrink-0 flex-col items-end">
				<BgtText color={won ? "green" : "red"} weight="bold">
					{won ? t("won") : t("lost")}
				</BgtText>
				{session.isCoop && <span className="text-xs text-white/60">{t("coop.label")}</span>}
				{!session.isCoop && playerSession?.score != null && (
					<span className="text-xs tabular-nums text-white/60">{t("points", { count: playerSession.score })}</span>
				)}
			</span>
		</>
	);

	if (!canOpen) return <div className={rowClasses}>{content}</div>;

	return (
		<Link
			to="/sessions/update/$sessionId"
			params={{ sessionId: session.id }}
			className={cx(rowClasses, "-mx-2 rounded-md px-2 transition-colors hover:bg-primary/10")}
		>
			{content}
		</Link>
	);
};

PlayerSessionCardItemComponent.displayName = "PlayerSessionCardItem";

export const PlayerSessionCardItem = memo(PlayerSessionCardItemComponent);
