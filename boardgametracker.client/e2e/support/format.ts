export const duration = (minutes: number) => {
	const hours = Math.floor(minutes / 60);
	const rest = minutes % 60;
	if (hours === 0) return `${minutes} min`;
	return rest === 0 ? `${hours}h` : `${hours}h ${rest}m`;
};

export const whole = (text: string) => new RegExp(`(?<!\\d)${text}(?! \\d+m)`);

const dateFormat = new Intl.DateTimeFormat("en-GB", {
	timeZone: "Europe/Brussels",
	day: "2-digit",
	month: "2-digit",
	year: "numeric",
});

export const displayDate = (date: Date) => dateFormat.format(date).replaceAll("/", "-");
