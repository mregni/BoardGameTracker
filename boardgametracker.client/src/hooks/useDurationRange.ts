import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { useShortDuration } from "@/hooks/useShortDuration";

const MINUTES_PER_HOUR = 60;

export const useDurationRange = () => {
	const { t } = useTranslation("common");
	const { integer } = useNumberFormat();
	const shortDuration = useShortDuration();

	return useCallback(
		(min: number | null | undefined, max: number | null | undefined): string | null => {
			const low = min ?? max;
			const high = max ?? min;
			if (low == null || high == null) return null;
			if (low === high) return shortDuration(low);
			if (low < MINUTES_PER_HOUR) {
				return t("short-duration.minutes", { minutes: `${integer(low)}–${integer(high)}` });
			}
			if (low % MINUTES_PER_HOUR === 0 && high % MINUTES_PER_HOUR === 0) {
				return t("short-duration.hours", {
					hours: `${integer(low / MINUTES_PER_HOUR)}–${integer(high / MINUTES_PER_HOUR)}`,
				});
			}
			return `${shortDuration(low)}–${shortDuration(high)}`;
		},
		[t, integer, shortDuration],
	);
};
