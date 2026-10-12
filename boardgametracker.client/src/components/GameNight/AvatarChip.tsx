import { cx } from "class-variance-authority";
import type { ReactNode } from "react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";

const chip =
	"inline-flex items-center gap-1.5 rounded-full border border-white/10 bg-white/5 py-1 pr-3 pl-1 text-xs font-medium";

interface Props {
	title: string;
	image: string | null | undefined;
	onClick?: () => void;
}

export const AvatarChip = ({ title, image, onClick }: Props) => (
	<div className={chip}>
		<BgtAvatar title={title} image={image} size="small" withTitle onClick={onClick} />
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
		className={cx(chip, "hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-60")}
	>
		<BgtAvatar title={title} image={image} size="small" />
		{title}
		{children}
	</button>
);
