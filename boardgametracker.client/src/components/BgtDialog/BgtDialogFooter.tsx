import { cx } from "class-variance-authority";
import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "../BgtButton/BgtButton";
import { BgtSpinner } from "../BgtLoadingSpinner/BgtSpinner";

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
			{confirmLabel && (
				<BgtButton
					type={confirmType}
					form={form}
					variant={variant === "destructive" ? "error" : "primary"}
					className={footerButton}
					onClick={onConfirm}
					disabled={disabled || loading}
				>
					{loading && <BgtSpinner className="size-4" />}
					{confirmLabel}
				</BgtButton>
			)}
		</div>
	);
};
