import { cva } from "class-variance-authority";

export const dashboardLinkClasses = cva(
	"inline-flex items-center justify-center gap-1.5 rounded-sm px-3 py-2 text-sm font-bold whitespace-nowrap transition-colors",
	{
		variants: {
			variant: {
				primary: "bg-primary/60 text-white hover:bg-primary/50",
				ghost: "border border-primary/40 text-white hover:bg-primary/15",
			},
		},
		defaultVariants: {
			variant: "primary",
		},
	},
);

export const dashboardRowClasses =
	"rounded-lg border border-primary/20 bg-primary/10 transition-colors hover:border-primary/60";

export const cardLinkClasses = "inline-flex items-center gap-1 text-sm text-primary hover:text-primary/70";

export const pillVariants = cva(
	"inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium whitespace-nowrap",
	{
		variants: {
			tone: {
				ok: "border-ok/50 bg-ok/10 text-ok",
				warn: "border-warn/50 bg-warn/10 text-warn",
				bad: "border-bad/50 bg-bad/10 text-bad",
				neutral: "border-white/15 bg-white/5 text-white/70",
			},
		},
		defaultVariants: {
			tone: "neutral",
		},
	},
);
