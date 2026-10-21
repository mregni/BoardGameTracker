import { cx } from "class-variance-authority";
import type { ReactNode } from "react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";

const chip = "inline-flex items-center gap-1.5 rounded-full border text-xs font-medium";

const sizes = {
	default: "p-1 pr-3",
	compact: "p-0 pr-2",
} as const;

const tones = {
	neutral: "border-white/10 bg-white/5",
	coming: "border-green-400/50 bg-green-400/10",
	maybe: "border-amber-400/50 bg-amber-400/10",
	pending: "border-white/20 bg-white/5",
	declined: "border-red-400/50 bg-red-400/10",
	winner: "border-yellow-400/50 bg-yellow-400/10",
} as const;

interface Props {
	title: string;
	image: string | null | undefined;
	onClick?: () => void;
	tone?: keyof typeof tones;
	size?: keyof typeof sizes;
	children?: ReactNode;
}

export const AvatarChip = ({ title, image, onClick, tone = "neutral", size = "default", children }: Props) => (
	<div className={cx(chip, sizes[size], tones[tone])}>
		<BgtAvatar title={title} image={image} size="small" shape="round" withTitle onClick={onClick} />
		{children}
	</div>
);

interface ButtonProps {
	title: string;
	image: string | null | undefined;
	onClick: () => void;
	disabled?: boolean;
	label: string;
	children?: ReactNode;
}

export const AvatarChipButton = ({ title, image, onClick, disabled, label, children }: ButtonProps) => (
	<button
		type="button"
		disabled={disabled}
		onClick={onClick}
		aria-label={label}
		className={cx(
			chip,
			sizes.default,
			tones.neutral,
			"hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-60",
		)}
	>
		<BgtAvatar title={title} image={image} size="small" shape="round" />
		{title}
		{children}
	</button>
);
