export const StringToHsl = (value: string | undefined): string => {
	if (value === undefined) {
		return "hsl(0, 85%, 35%)";
	}

	return `hsl(${calculateHash(value)}, 85%, 35%)`;
};

export const StringToRgb = (value: string | undefined): string => {
	if (value === undefined) {
		return "#000000";
	}

	const h = calculateHash(value) / 360;
	const s = 0.85;
	const l = 0.35;

	const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
	const p = 2 * l - q;
	const r = hueToRgb(p, q, h + 1 / 3);
	const g = hueToRgb(p, q, h);
	const b = hueToRgb(p, q, h - 1 / 3);

	return `rgb(${Math.round(r * 255)}, ${Math.round(g * 255)}, ${Math.round(b * 255)})`;
};

const calculateHash = (input: string): number => {
	let hash = 0;
	for (let i = 0; i < input.length; i += 1) {
		hash = input.charCodeAt(i) + ((hash << 5) - hash);
	}

	return ((hash % 360) + 360) % 360;
};

const hueToRgb = (p: number, q: number, t: number): number => {
	let value = t;
	if (value < 0) value += 1;
	if (value > 1) value -= 1;
	if (value < 1 / 6) return p + (q - p) * 6 * value;
	if (value < 1 / 2) return q;
	if (value < 2 / 3) return p + (q - p) * (2 / 3 - value) * 6;
	return p;
};
