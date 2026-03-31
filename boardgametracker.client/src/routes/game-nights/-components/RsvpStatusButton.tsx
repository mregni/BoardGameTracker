import { cx } from "class-variance-authority";
import { useCallback } from "react";

import type { GameNightRsvpState } from "@/models";

interface RsvpStatusButtonProps {
	rsvpId: number;
	state: GameNightRsvpState;
	onStatusChange: (rsvpId: number, state: GameNightRsvpState) => void;
	disabled: boolean;
	isActive: boolean;
	variant: "green" | "yellow" | "red";
	label: string;
}

const statusButtonStyles = {
	green: {
		active: "bg-green-500/60 text-white",
		inactive: "bg-green-500/10 text-green-300 border border-green-500/30",
		hover: "hover:bg-green-500/30",
	},
	yellow: {
		active: "bg-yellow-500/60 text-white",
		inactive: "bg-yellow-500/10 text-yellow-300 border border-yellow-500/30",
		hover: "hover:bg-yellow-500/30",
	},
	red: {
		active: "bg-red-500/60 text-white",
		inactive: "bg-red-500/10 text-red-300 border border-red-500/30",
		hover: "hover:bg-red-500/30",
	},
};

export const RsvpStatusButton = ({
	rsvpId,
	state,
	onStatusChange,
	disabled,
	isActive,
	variant,
	label,
}: RsvpStatusButtonProps) => {
	const handleClick = useCallback(() => {
		onStatusChange(rsvpId, state);
	}, [onStatusChange, rsvpId, state]);

	return (
		<button
			onClick={handleClick}
			disabled={disabled}
			className={cx(
				"px-4 py-2 rounded-lg text-sm font-medium transition-colors",
				disabled ? "opacity-50 cursor-not-allowed" : "cursor-pointer",
				isActive ? statusButtonStyles[variant].active : statusButtonStyles[variant].inactive,
				!isActive && !disabled && statusButtonStyles[variant].hover,
			)}
		>
			{label}
		</button>
	);
};
