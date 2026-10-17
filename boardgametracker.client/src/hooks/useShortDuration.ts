import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { EMPTY_NUMBER } from "@/utils/numberUtils";

export const useShortDuration = () => {
	const { t } = useTranslation("common");
	const { integer } = useNumberFormat();

	return useCallback(
		(totalMinutes: number | null | undefined) => {
			if (typeof totalMinutes !== "number" || Number.isNaN(totalMinutes)) return EMPTY_NUMBER;

			const rounded = Math.max(0, Math.round(totalMinutes));
			const hours = Math.floor(rounded / 60);
			const minutes = rounded % 60;

			if (hours === 0) return t("short-duration.minutes", { minutes: integer(minutes) });
			if (minutes === 0) return t("short-duration.hours", { hours: integer(hours) });
			return t("short-duration.hours-minutes", { hours: integer(hours), minutes: integer(minutes) });
		},
		[t, integer],
	);
};
