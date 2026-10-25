import { useTranslation } from "react-i18next";
import Gamepad from "@/assets/icons/gamepad.svg?react";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { BgtText } from "@/components/BgtText/BgtText";

interface Props {
	wanted?: boolean;
}

export const GameDetailEmptyState = ({ wanted = false }: Props) => {
	const { t } = useTranslation("game");

	return (
		<div className="flex items-center justify-center min-h-[60vh]">
			<div className="text-center max-w-lg flex flex-col gap-3">
				<div className="mb-6 flex justify-center">
					<div className="w-24 h-24 rounded-full bg-primary/10 border-2 border-primary/30 flex items-center justify-center">
						<Gamepad className="text-primary/50 size-12" />
					</div>
				</div>

				<BgtHeading className="mb-3">{t(wanted ? "no-sessions.not-played" : "no-sessions.title")}</BgtHeading>

				{!wanted && (
					<BgtText color="white" opacity={60} className="leading-relaxed">
						{t("no-sessions.description")}
					</BgtText>
				)}
			</div>
		</div>
	);
};
