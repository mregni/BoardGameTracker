export const SafeHttpUrl = (value: string | null | undefined): string | null => {
	if (!value) return null;
	return /^https?:\/\//i.test(value.trim()) ? value : null;
};

const SATURATION = 0.85;
const MAX_LIGHTNESS = 35;
const MIN_CONTRAST_WITH_WHITE = 4.5;

export type Rgb = [number, number, number];

export const StringToHsl = (value: string | undefined): string => {
	const hue = value === undefined ? 0 : calculateHash(value);
	return `hsl(${hue}, 85%, ${avatarLightness(hue)}%)`;
};

export const StringToRgb = (value: string | undefined): string => {
	if (value === undefined) {
		return "#000000";
	}

	const [r, g, b] = AvatarRgb(calculateHash(value));
	return `rgb(${r}, ${g}, ${b})`;
};

export const AvatarRgb = (hue: number): Rgb => hslToRgb(hue, avatarLightness(hue));

export const ContrastWithWhite = ([r, g, b]: Rgb): number => {
	const channel = (value: number) => {
		const c = value / 255;
		return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
	};
	const luminance = 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
	return 1.05 / (luminance + 0.05);
};

const avatarLightness = (hue: number): number => {
	let lightness = MAX_LIGHTNESS;
	while (lightness > 0 && ContrastWithWhite(hslToRgb(hue, lightness)) < MIN_CONTRAST_WITH_WHITE) {
		lightness -= 1;
	}
	return lightness;
};

const hslToRgb = (hue: number, lightness: number): Rgb => {
	const h = hue / 360;
	const l = lightness / 100;
	const q = l < 0.5 ? l * (1 + SATURATION) : l + SATURATION - l * SATURATION;
	const p = 2 * l - q;
	return [
		Math.round(hueToRgb(p, q, h + 1 / 3) * 255),
		Math.round(hueToRgb(p, q, h) * 255),
		Math.round(hueToRgb(p, q, h - 1 / 3) * 255),
	];
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
