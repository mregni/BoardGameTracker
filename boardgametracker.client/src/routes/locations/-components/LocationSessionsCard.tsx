import { useState } from "react";
import { useTranslation } from "react-i18next";
import Calendar from "@/assets/icons/calendar.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import type { Session } from "@/models";
import { useGameById } from "@/routes/-hooks/useGameById";
import { usePlayerById } from "@/routes/-hooks/usePlayerById";
import { LocationSessionRow } from "./LocationSessionRow";

const VISIBLE_SESSIONS = 10;

interface Props {
	sessions: Session[];
	dateFormat: string;
	uiLanguage: string;
	canOpen: boolean;
}

export const LocationSessionsCard = (props: Props) => {
	const { sessions, dateFormat, uiLanguage, canOpen } = props;
	const { t } = useTranslation("location");
	const { gameById } = useGameById();
	const { playerById } = usePlayerById();
	const [showAll, setShowAll] = useState(false);

	const visibleSessions = showAll ? sessions : sessions.slice(0, VISIBLE_SESSIONS);

	return (
		<BgtCard title={t("detail.sessions")} icon={Calendar}>
			{sessions.length === 0 && (
				<BgtText size="2" color="white" opacity={60}>
					{t("detail.no-sessions")}
				</BgtText>
			)}
			{sessions.length > 0 && (
				<div className="flex flex-col gap-1.5">
					{visibleSessions.map((session) => (
						<LocationSessionRow
							key={session.id}
							session={session}
							game={gameById(session.gameId)}
							winners={session.playerSessions
								.filter((x) => x.won)
								.map((x) => playerById(x.playerId)?.name)
								.filter((name): name is string => name !== undefined)}
							dateFormat={dateFormat}
							uiLanguage={uiLanguage}
							canOpen={canOpen}
						/>
					))}
				</div>
			)}
			{sessions.length > VISIBLE_SESSIONS && (
				<BgtButton variant="text" className="mt-3 self-center" onClick={() => setShowAll((value) => !value)}>
					{showAll ? t("detail.show-less") : t("detail.show-all", { count: sessions.length })}
				</BgtButton>
			)}
		</BgtCard>
	);
};
