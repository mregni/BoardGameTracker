import type { TFunction } from "i18next";

import type { PieChartDatum } from "@/models/Charts/PieChartDatum";

export const transformPlayerCountChartData = (
	data: Array<{ players: number | string; playCount: number }>,
	t: TFunction,
): PieChartDatum[] => {
	return data.map((item) => ({
		label: t("common:player", { count: +(item.players as number) }),
		id: item.players,
		value: item.playCount,
	}));
};

const WEEKDAYS = ["sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday"];
const FIRST_SUNDAY_OF_2026 = 4;

export const shortWeekday = (dayOfWeek: string, locale: string): string | null => {
	const index = WEEKDAYS.indexOf(dayOfWeek.toLowerCase());
	if (index === -1) return null;
	return new Intl.DateTimeFormat(locale, { weekday: "short", timeZone: "UTC" }).format(
		Date.UTC(2026, 0, FIRST_SUNDAY_OF_2026 + index),
	);
};

export const transformSessionCountChartData = (
	data: Array<{ dayOfWeek: string; playCount: number }>,
	t: TFunction,
	locale: string,
): Array<{ day: string; shortDay: string; sessions: number }> => {
	return data.map((item) => {
		const day = t(`common:days.${item.dayOfWeek}`);
		return { day, shortDay: shortWeekday(item.dayOfWeek, locale) ?? day, sessions: item.playCount };
	});
};
