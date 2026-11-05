export const duration = (minutes: number) => {
	const hours = Math.floor(minutes / 60);
	const rest = minutes % 60;
	if (hours === 0) return `${minutes} min`;
	return rest === 0 ? `${hours}h` : `${hours}h ${rest}m`;
};
