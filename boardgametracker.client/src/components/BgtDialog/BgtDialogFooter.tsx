import { cx } from "class-variance-authority";
import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { Bars } from "react-loading-icons";
import BgtButton from "../BgtButton/BgtButton";

const footerButton = "min-h-10 flex-auto md:min-w-32 md:flex-none";

interface BgtDialogFooterProps {
	confirmLabel?: ReactNode;
	onConfirm?: () => void;
	confirmType?: "button" | "submit";
	form?: string;
	variant?: "primary" | "destructive";
	disabled?: boolean;
	loading?: boolean;
	onCancel?: () => void;
	cancelLabel?: ReactNode;
	cancelDisabled?: boolean;
	secondaryLabel?: ReactNode;
	onSecondary?: () => void;
	className?: string;
}

export const BgtDialogFooter = (props: BgtDialogFooterProps) => {
	const {
		confirmLabel,
		onConfirm,
		confirmType = "button",
		form,
		variant = "primary",
		disabled = false,
		loading = false,
		onCancel,
		cancelLabel,
		cancelDisabled = false,
		secondaryLabel,
		onSecondary,
		className,
	} = props;
	const { t } = useTranslation();

	return (
		<div data-slot="dialog-footer" className={cx("flex flex-row items-stretch gap-3 pt-2 md:justify-end", className)}>
			{onCancel && (
				<BgtButton
					type="button"
					variant="cancel"
					className={footerButton}
					onClick={onCancel}
					disabled={cancelDisabled || loading}
				>
					{cancelLabel ?? t("common:cancel")}
				</BgtButton>
			)}
			{secondaryLabel && onSecondary && (
				<BgtButton
					type="button"
					variant="text"
					className={footerButton}
					onClick={onSecondary}
					disabled={disabled || loading}
				>
					{secondaryLabel}
				</BgtButton>
			)}
			{confirmLabel && (
				<BgtButton
					type={confirmType}
					form={form}
					variant={variant === "destructive" ? "error" : "primary"}
					className={footerButton}
					onClick={onConfirm}
					disabled={disabled || loading}
				>
					{loading && <Bars className="size-4" />}
					{confirmLabel}
				</BgtButton>
			)}
		</div>
	);
};
