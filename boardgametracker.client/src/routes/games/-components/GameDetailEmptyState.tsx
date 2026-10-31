import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Gamepad from "@/assets/icons/gamepad.svg?react";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { BgtText } from "@/components/BgtText/BgtText";
import { usePermissions } from "@/hooks/usePermissions";

interface Props {
	gameId: number;
	wanted?: boolean;
}

export const GameDetailEmptyState = ({ gameId, wanted = false }: Props) => {
	const { t } = useTranslation("game");
	const { canWrite } = usePermissions();

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

				{!wanted && canWrite && (
					<Link
						to="/sessions/new/$gameId"
						params={{ gameId }}
						className="self-center mt-3 px-4 py-2 rounded font-bold bg-primary/60 hover:bg-primary/50 text-white"
					>
						{t("first-play.action")}
					</Link>
				)}
			</div>
		</div>
	);
};
