import { cx } from "class-variance-authority";
import type { ReactNode } from "react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";

const chip = "inline-flex items-center gap-1.5 rounded-full border p-1 pr-3 text-xs font-medium";

const tones = {
	neutral: "border-white/10 bg-white/5",
	coming: "border-green-400/50 bg-green-400/10",
	pending: "border-amber-400/50 bg-amber-400/10",
	declined: "border-red-400/50 bg-red-400/10",
	winner: "border-yellow-400/50 bg-yellow-400/10",
} as const;

interface Props {
	title: string;
	image: string | null | undefined;
	onClick?: () => void;
	tone?: keyof typeof tones;
	children?: ReactNode;
}

export const AvatarChip = ({ title, image, onClick, tone = "neutral", children }: Props) => (
	<div className={cx(chip, tones[tone])}>
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
		className={cx(chip, tones.neutral, "hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-60")}
	>
		<BgtAvatar title={title} image={image} size="small" shape="round" />
		{title}
		{children}
	</button>
);
