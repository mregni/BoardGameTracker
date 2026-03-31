import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import Calendar from "@/assets/icons/calendar.svg?react";
import { RecentActivityCard } from "@/components/BgtCard/RecentActivityCard";
import type { Session } from "@/models/Session/Session";
import { useGameById } from "@/routes/-hooks/useGameById";
import { PlayerSessionCardItem } from "./PlayerSessionCardItem";

interface Props {
	sessions: Session[];
	dateFormat: string;
	uiLanguage: string;
	playerId: number;
}

export const RecentPlayerSessionsCard = (props: Props) => {
	const { sessions, dateFormat, uiLanguage, playerId } = props;
	const { t } = useTranslation("game");

	const { gameById } = useGameById();

	const renderSession = useCallback(
		(session: Session) => (
			<PlayerSessionCardItem
				session={session}
				game={gameById(session.gameId) ?? undefined}
				dateFormat={dateFormat}
				uiLanguage={uiLanguage}
				playerId={playerId}
			/>
		),
		[dateFormat, uiLanguage, playerId, gameById],
	);

	const getSessionKey = useCallback((session: Session) => session.id, []);

	return (
		<RecentActivityCard
			items={sessions}
			renderItem={renderSession}
			title={t("titles.recent-sessions")}
			viewAllRoute={`/players/${playerId}/sessions`}
			viewAllText={t("sessions")}
			icon={Calendar}
			getKey={getSessionKey}
		/>
	);
};
