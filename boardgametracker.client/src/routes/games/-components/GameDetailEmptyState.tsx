import { useTranslation } from "react-i18next";
import Gamepad from "@/assets/icons/gamepad.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { BgtText } from "@/components/BgtText/BgtText";

interface Props {
	wanted?: boolean;
	onAddSession?: () => void;
}

export const GameDetailEmptyState = ({ wanted = false, onAddSession }: Props) => {
	const { t } = useTranslation("game");

	return (
		<div className="flex flex-col items-center gap-3 rounded-lg border border-primary/20 bg-primary/10 px-4 py-10 text-center">
			<Gamepad className="text-primary size-10" />
			<BgtHeading as="h2" size="6">
				{t(wanted ? "no-sessions.not-played" : "no-sessions.title")}
			</BgtHeading>
			{!wanted && (
				<BgtText color="white" opacity={70} className="max-w-md leading-relaxed">
					{t("no-sessions.description")}
				</BgtText>
			)}
			{!wanted && onAddSession && (
				<BgtButton variant="primary" onClick={onAddSession} className="mt-2">
					{t("first-play.action")}
				</BgtButton>
			)}
		</div>
	);
};
