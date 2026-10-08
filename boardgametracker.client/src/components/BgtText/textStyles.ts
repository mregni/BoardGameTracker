export type TextSize = "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9";
export type TextWeight = "light" | "regular" | "medium" | "bold";

export const textSizeClasses: Record<TextSize, string> = {
	"1": "text-[12px]/[16px]",
	"2": "text-[14px]/[20px]",
	"3": "text-[16px]/[24px]",
	"4": "text-[18px]/[26px]",
	"5": "text-[20px]/[28px]",
	"6": "text-[24px]/[30px]",
	"7": "text-[28px]/[36px]",
	"8": "text-[35px]/[40px] tracking-[-0.01em]",
	"9": "text-[60px]/[60px] tracking-[-0.025em]",
};

export const textWeightClasses: Record<TextWeight, string> = {
	light: "font-light",
	regular: "font-normal",
	medium: "font-medium",
	bold: "font-bold",
};
