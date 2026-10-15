import { cva } from "class-variance-authority";
import type { CSSProperties } from "react";

export type ProgressTone =
	| "primary"
	| "value"
	| "ok"
	| "warn"
	| "bad"
	| "muted"
	| "subtle"
	| "playerOne"
	| "playerTwo"
	| "empty";

export type ProgressPattern = "solid" | "hatch";

export interface ProgressPaint {
	tone?: ProgressTone;
	color?: string;
	pattern?: ProgressPattern;
}

export const progressToneVariants = cva("", {
	variants: {
		tone: {
			primary: "bg-primary",
			value: "bg-card-value",
			ok: "bg-ok",
			warn: "bg-warn",
			bad: "bg-bad",
			muted: "bg-bar-muted",
			subtle: "bg-bar-subtle",
			playerOne: "bg-player-one",
			playerTwo: "bg-player-two",
			empty: "bg-card border border-card-border",
			custom: "",
		},
		pattern: {
			solid: "",
			hatch: "bg-hatch",
		},
	},
	defaultVariants: {
		tone: "primary",
		pattern: "solid",
	},
});

export const paintClasses = (paint: ProgressPaint, fallback: ProgressTone, className?: string): string =>
	progressToneVariants({
		tone: paint.color ? "custom" : (paint.tone ?? fallback),
		pattern: paint.pattern ?? "solid",
		className,
	});

export const paintStyle = (paint: ProgressPaint): CSSProperties | undefined =>
	paint.color ? { backgroundColor: paint.color } : undefined;
