import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Plus from "@/assets/icons/plus.svg?react";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { BgtText } from "@/components/BgtText/BgtText";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { DashboardLinkedPlayer } from "@/models";
import { dashboardLinkClasses } from "./dashboardStyles";

interface Props {
	linkedPlayer: DashboardLinkedPlayer | null;
	sessionsThisMonth: number;
	canWrite: boolean;
}

export const DashboardHeader = (props: Props) => {
	const { linkedPlayer, sessionsThisMonth, canWrite } = props;
	const { t } = useTranslation(["dashboard", "common"]);
	const { integer } = useNumberFormat();

	return (
		<div className="flex flex-wrap items-center justify-between gap-3">
			<div className="flex min-w-0 flex-col gap-0.5">
				<BgtHeading as="h1" size="7">
					{linkedPlayer ? t("welcome.title", { name: linkedPlayer.name }) : t("common:dashboard")}
				</BgtHeading>
				<BgtText size="2" color="gray">
					{linkedPlayer
						? t("welcome.linked", {
								count: sessionsThisMonth,
								total: integer(sessionsThisMonth),
								played: integer(linkedPlayer.sessionsThisMonth),
								wins: integer(linkedPlayer.winsThisMonth),
							})
						: t("welcome.group", { count: sessionsThisMonth, total: integer(sessionsThisMonth) })}
				</BgtText>
			</div>
			{canWrite && (
				<div className="flex gap-2">
					<Link to="/games/add" className={dashboardLinkClasses({ variant: "ghost", className: "max-md:hidden" })}>
						<Plus className="size-4" aria-hidden="true" />
						{t("actions.add-game")}
					</Link>
					<Link to="/sessions/new" className={dashboardLinkClasses({ variant: "primary" })}>
						<Plus className="size-4" aria-hidden="true" />
						{t("actions.log-session")}
					</Link>
				</div>
			)}
		</div>
	);
};
