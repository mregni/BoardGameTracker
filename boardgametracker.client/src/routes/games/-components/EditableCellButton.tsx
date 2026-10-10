import { cx } from "class-variance-authority";
import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";

interface Props {
	onClick: () => void;
	children: ReactNode;
	className?: string;
	align?: "left" | "right";
	readOnly?: boolean;
	size?: "xs" | "sm";
}

export const EditableCellButton = ({
	onClick,
	children,
	className,
	align = "left",
	readOnly = false,
	size = "xs",
}: Props) => {
	const { t } = useTranslation();
	const sizeClass = size === "sm" ? "text-sm" : "text-[12px]";

	if (readOnly) {
		return (
			<span
				className={cx(
					"block h-9 w-full px-2 leading-9 truncate",
					sizeClass,
					align === "right" ? "text-right" : "text-left",
					className,
				)}
			>
				{children}
			</span>
		);
	}

	return (
		<button
			type="button"
			onClick={onClick}
			title={t("click-to-edit")}
			className={cx(
				"h-9 w-full rounded-lg px-2 truncate border border-transparent hover:border-primary/30 hover:text-primary",
				sizeClass,
				align === "right" ? "text-right" : "text-left",
				className,
			)}
		>
			{children}
		</button>
	);
};
