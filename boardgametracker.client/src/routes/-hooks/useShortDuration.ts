import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import { useNumberFormat } from "@/hooks/useNumberFormat";

export const useShortDuration = () => {
	const { t } = useTranslation("dashboard");
	const { integer } = useNumberFormat();

	return useCallback(
		(totalMinutes: number) => {
			const rounded = Math.max(0, Math.round(totalMinutes));
			const hours = Math.floor(rounded / 60);
			const minutes = rounded % 60;

			if (hours === 0) return t("duration.minutes", { minutes: integer(minutes) });
			if (minutes === 0) return t("duration.hours", { hours: integer(hours) });
			return t("duration.hours-minutes", { hours: integer(hours), minutes: integer(minutes) });
		},
		[t, integer],
	);
};
