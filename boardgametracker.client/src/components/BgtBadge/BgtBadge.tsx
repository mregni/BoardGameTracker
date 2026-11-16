import { cva, type VariantProps } from "class-variance-authority";
import type { ComponentPropsWithoutRef } from "react";

import CloseIcon from "@/assets/icons/x.svg?react";

const badgeVariants = cva(
	"group rounded-full text-xs inline-flex flex-row items-center gap-2 border whitespace-nowrap",
	{
		variants: {
			variant: {
				soft: "",
			},
			color: {
				green: "text-card-value bg-[#34FFAA1F]",
				red: "text-error-dark bg-error-dark/20 border-error-dark/30",
				primary: "text-chip-selected bg-primary/20 border-primary/30",
				ok: "text-ok bg-ok/15 border-ok/30",
				warn: "text-warn bg-warn/15 border-warn/30",
				neutral: "text-white/75 bg-white/10 border-white/15",
			},
			size: {
				default: "px-3 py-1",
				sm: "px-2 py-0.5 font-semibold",
			},
			interactive: {
				true: "cursor-pointer",
				false: "",
			},
		},
		defaultVariants: {
			variant: "soft",
			color: "green",
			size: "default",
			interactive: false,
		},
	},
);

interface Props extends Omit<ComponentPropsWithoutRef<"span">, "color">, VariantProps<typeof badgeVariants> {
	onClose?: () => void;
}

export const BgtBadge = (props: Props) => {
	const { className, children, variant, color, size, onClick, onClose, ...rest } = props;

	const badgeClasses = badgeVariants({
		variant,
		color,
		size,
		interactive: !!onClick,
		className,
	});

	const handleClose = (e: React.MouseEvent<SVGSVGElement>) => {
		e.stopPropagation();
		onClose?.();
	};

	return (
		<span
			className={badgeClasses}
			onClick={onClick}
			onKeyDown={
				onClick
					? (e: React.KeyboardEvent) => {
							if (e.key === "Enter" || e.key === " ") {
								e.preventDefault();
								onClick(e as unknown as React.MouseEvent<HTMLSpanElement>);
							}
						}
					: undefined
			}
			role={onClick ? "button" : undefined}
			tabIndex={onClick ? 0 : undefined}
			{...rest}
		>
			{children}
			{onClose && <CloseIcon onClick={handleClose} className="cursor-pointer" />}
		</span>
	);
};
