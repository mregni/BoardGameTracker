import { useTranslation } from "react-i18next";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";

interface Props {
	name: string;
	playerName: string | null;
	playCount: number;
	totalSessions: number;
}

export const WelcomeCard = ({ name, playerName, playCount, totalSessions }: Props) => {
	const { t } = useTranslation("dashboard");

	return (
		<BgtCard className="gap-1">
			<BgtText size="5" weight="bold" color="white">
				{t("welcome-back-title", { name })}
			</BgtText>
			<BgtText color="gray">
				{playerName
					? t("welcome-back-linked", { player: playerName, played: playCount, count: totalSessions })
					: t("welcome-back", { count: playCount })}
			</BgtText>
		</BgtCard>
	);
};
