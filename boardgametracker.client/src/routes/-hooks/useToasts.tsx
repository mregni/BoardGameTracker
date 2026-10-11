import { useTranslation } from "react-i18next";
import { toast } from "sonner";

export const useToasts = () => {
	const { i18n } = useTranslation();

	const successToast = (message: string) => {
		toast.success(i18n.t(message));
	};

	const errorToast = (message: string) => {
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
