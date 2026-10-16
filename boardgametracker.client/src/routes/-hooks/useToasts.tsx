import { useTranslation } from "react-i18next";
import { toast } from "sonner";

const ACTION_TOAST_DURATION = 8000;

export interface ToastAction {
	label: string;
	onClick: () => void;
}

export const useToasts = () => {
	const { i18n } = useTranslation();

	const successToast = (message: string, action?: ToastAction, values?: Record<string, unknown>) => {
		const text = values ? i18n.t(message, values) : i18n.t(message);
		if (action) {
			toast.success(text, {
				duration: ACTION_TOAST_DURATION,
				action: { label: i18n.t(action.label), onClick: action.onClick },
			});
			return;
		}
		toast.success(text);
	};

	const errorToast = (message: string, action?: ToastAction) => {
		if (action) {
			toast.error(i18n.t(message), { action: { label: i18n.t(action.label), onClick: action.onClick } });
			return;
		}
		toast.error(i18n.t(message));
	};

	const infoToast = (message: string) => {
		toast.info(i18n.t(message));
	};

	const warningToast = (message: string) => {
		toast.warning(i18n.t(message));
	};

	return {
		successToast,
		errorToast,
		infoToast,
		warningToast,
	};
};
