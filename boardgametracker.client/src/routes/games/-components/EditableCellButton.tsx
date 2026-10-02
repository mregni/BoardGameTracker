import { cx } from "class-variance-authority";
import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";

interface Props {
	onClick: () => void;
	children: ReactNode;
	className?: string;
	align?: "left" | "right";
}

export const EditableCellButton = ({ onClick, children, className, align = "left" }: Props) => {
	const { t } = useTranslation();

	return (
		<button
			type="button"
			onClick={onClick}
			title={t("click-to-edit")}
			className={cx(
				"h-9 w-full rounded-lg px-2 text-[12px] truncate border border-transparent hover:border-primary/30 hover:text-primary",
				align === "right" ? "text-right" : "text-left",
				className,
			)}
		>
			{children}
		</button>
	);
};
