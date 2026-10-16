import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import { formatShortDuration } from "../-utils/compareUtils";

export const useShortDuration = () => {
	const { t, i18n } = useTranslation("common");
	const hour = t("hour-short");
	const minute = t("minute-short");
	const locale = i18n?.language;

	return useCallback(
		(minutes: number) => formatShortDuration(minutes, { hour, minute }, locale),
		[hour, minute, locale],
	);
};
