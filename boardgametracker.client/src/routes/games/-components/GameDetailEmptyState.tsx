import { useTranslation } from "react-i18next";
import Gamepad from "@/assets/icons/gamepad.svg?react";
import Plus from "@/assets/icons/plus.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { BgtText } from "@/components/BgtText/BgtText";

interface GameDetailEmptyStateProps {
	onLogSession?: () => void;
}

export const GameDetailEmptyState = ({ onLogSession }: GameDetailEmptyStateProps) => {
	const { t } = useTranslation();

	return (
		<div className="flex items-center justify-center min-h-[60vh]">
			<div className="text-center max-w-lg flex flex-col gap-3">
				<div className="mb-6 flex justify-center">
					<div className="w-24 h-24 rounded-full bg-primary/10 border-2 border-primary/30 flex items-center justify-center">
						<Gamepad className="text-primary/50 size-12" />
					</div>
				</div>

				<BgtHeading className="mb-3">{t("game.no-sessions.title")}</BgtHeading>

				<BgtText color="white" opacity={60} className="mb-8 leading-relaxed">
					{t("game.no-sessions.description")}
				</BgtText>

				{onLogSession && (
					<BgtButton onClick={onLogSession} className="mt-6">
						<Plus /> {t("game.no-sessions.button")}
					</BgtButton>
				)}

				<BgtText color="white" opacity={40} size="2" className="pt-6">
					{t("game.no-sessions.extra")}
				</BgtText>
			</div>
		</div>
	);
};
