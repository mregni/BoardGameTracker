import { useNavigate } from "@tanstack/react-router";
import type { ComponentType, SVGProps } from "react";
import { useTranslation } from "react-i18next";
import Game from "@/assets/icons/dices.svg?react";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { usePermissions } from "@/hooks/usePermissions";

interface Props {
	header: string;
	icon: ComponentType<SVGProps<SVGSVGElement>>;
}

export const NoGamesEmptyPage = ({ header, icon }: Props) => {
	const { t } = useTranslation("dashboard");
	const navigate = useNavigate();
	const { canWrite } = usePermissions();

	return (
		<BgtEmptyPage
			header={header}
			icon={icon}
			emptyIcon={Game}
			title={t("empty.title")}
			action={canWrite ? { label: t("empty.button"), onClick: () => navigate({ to: "/games/add" }) } : undefined}
		/>
	);
};
