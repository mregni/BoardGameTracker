import { cx } from "class-variance-authority";

import { BgtText } from "@/components/BgtText/BgtText";

interface RsvpStatBadgeProps {
	count: number;
	label: string;
	variant: "green" | "amber" | "red";
}

const variantStyles = {
	green: "bg-green-500/10 border-green-500/20",
	amber: "bg-amber-500/10 border-amber-500/20",
	red: "bg-red-500/10 border-red-500/20",
};

export const RsvpStatBadge = ({ count, label, variant }: RsvpStatBadgeProps) => (
	<div className={cx("border rounded-lg p-4 text-center", variantStyles[variant])}>
		<BgtText size="5" weight="bold" color={variant}>
			{count}
		</BgtText>
		<BgtText size="2" color={variant}>
			{label}
		</BgtText>
	</div>
);
