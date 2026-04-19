import { cva } from "class-variance-authority";
import type { ComponentType, ReactNode, SVGProps } from "react";

import AlertTriangle from "@/assets/icons/alert-triangle.svg?react";
import Check from "@/assets/icons/check.svg?react";
import Database from "@/assets/icons/database.svg?react";

type Variant = "warning" | "success" | "info";

const containerVariants = cva("mb-6 p-4 rounded-lg border", {
	variants: {
		variant: {
			warning: "bg-orange-500/10 border-orange-500/20",
			success: "bg-green-500/10 border-green-500/20",
			info: "bg-cyan-500/10 border-cyan-500/20",
		},
	},
});

const colorVariants = cva("", {
	variants: {
		variant: {
			warning: "text-orange-400",
			success: "text-green-400",
			info: "text-cyan-400",
		},
	},
});

const icons: Record<Variant, ComponentType<SVGProps<SVGSVGElement>>> = {
	warning: AlertTriangle,
	success: Check,
	info: Database,
};

interface Props {
	title: string;
	description?: ReactNode;
	variant: Variant;
	icon?: ComponentType<SVGProps<SVGSVGElement>>;
}

export const BgtStatus = ({ title, description, variant, icon }: Props) => {
	const Icon = icon ?? icons[variant];

	return (
		<div className={containerVariants({ variant })}>
			<div className="flex items-start gap-3">
				<Icon className={colorVariants({ variant, className: "shrink-0 mt-0.5 size-5" })} />
				<div className="flex-1 min-w-0">
					<div className={colorVariants({ variant, className: "text-sm font-medium mb-0.5" })}>{title}</div>
					{description && <div className="text-xs text-white/60">{description}</div>}
				</div>
			</div>
		</div>
	);
};
