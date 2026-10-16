import { cva, type VariantProps } from "class-variance-authority";
import { memo, type ReactNode } from "react";
import Crown from "@/assets/icons/crown.svg?react";
import { BgtText } from "@/components/BgtText/BgtText";

const cardVariants = cva("border rounded-xl p-5 backdrop-blur-sm relative transition-all duration-200", {
	variants: {
		color: {
			red: "bg-player-one/10 border-player-one/30",
			blue: "bg-player-two/10 border-player-two/30",
		},
		isWinner: {
			true: "",
		},
	},
	compoundVariants: [
		{
			color: "red",
			isWinner: true,
			class: "ring-4 ring-player-one/20",
		},
		{
			color: "blue",
			isWinner: true,
			class: "ring-4 ring-player-two/20",
		},
	],
});

const iconVariants = cva("", {
	variants: {
		color: {
			red: "text-player-one",
			blue: "text-player-two",
		},
	},
});

const labelVariants = cva("text-xs tracking-wider mb-1", {
	variants: {
		color: {
			red: "text-red-200",
			blue: "text-blue-200",
		},
	},
});

interface CompareCardProps<T> extends VariantProps<typeof cardVariants> {
	value: T;
	label: string;
	icon?: ReactNode;
	color: "red" | "blue";
}

const CompareCardComponent = <T,>({ value, label, icon, color, isWinner }: CompareCardProps<T>) => {
	return (
		<div className={cardVariants({ color, isWinner })}>
			<div className="flex items-center gap-4">
				{icon && <div className={iconVariants({ color })}>{icon}</div>}
				<div className="flex-1">
					<div className={labelVariants({ color })}>{label}</div>
					<BgtText color="cyan" size="6">
						{String(value)}
					</BgtText>
				</div>
				{isWinner && <Crown className="text-yellow-400 fill-yellow-400" />}
			</div>
		</div>
	);
};

export const CompareCard = memo(CompareCardComponent) as typeof CompareCardComponent;
