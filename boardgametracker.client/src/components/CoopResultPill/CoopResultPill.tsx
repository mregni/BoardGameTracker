import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import UsersIcon from "@/assets/icons/users.svg?react";

interface Props {
	won: boolean;
	className?: string;
}

export const CoopResultPill = ({ won, className }: Props) => {
	const { t } = useTranslation("common");

	return (
		<span
			className={cx(
				"inline-flex shrink-0 items-center gap-1 whitespace-nowrap rounded-full px-2 py-0.5 text-xs font-semibold",
				won ? "bg-ok/20 text-ok" : "bg-bad/20 text-bad",
				className,
			)}
		>
			<UsersIcon className="size-3.5" />
			{won ? t("coop.won") : t("coop.lost")}
		</span>
	);
};
