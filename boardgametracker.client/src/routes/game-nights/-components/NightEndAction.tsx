import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { DisabledActionHint } from "./DisabledActionHint";

interface Props {
	hasEnded: boolean;
	hasRunningGames: boolean;
	onEnd: () => void;
	onReopen: () => void;
}

export const NightEndAction = (props: Props) => {
	const { hasEnded, hasRunningGames, onEnd, onReopen } = props;
	const { t } = useTranslation("game-nights");

	if (hasEnded) {
		return (
			<BgtButton variant="cancel" size="1" onClick={onReopen}>
				{t("end.reopen")}
			</BgtButton>
		);
	}

	if (!hasRunningGames) {
		return (
			<BgtButton variant="cancel" size="1" onClick={onEnd}>
				{t("end.button")}
			</BgtButton>
		);
	}

	return <DisabledActionHint label={t("end.button")} hint={t("end.running-hint")} />;
};
