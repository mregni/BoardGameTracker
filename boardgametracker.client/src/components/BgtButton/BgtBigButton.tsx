import { cva } from "class-variance-authority";
import type { ComponentType, SVGProps } from "react";

const buttonVariants = cva("w-full text-left p-6 rounded-xl border-2 transition-all", {
	variants: {
		disabled: {
			true: "bg-white/5 border-white/10 cursor-not-allowed opacity-50",
			false:
				"bg-primary/10 border-primary/20 hover:bg-primary/20 hover:border-primary/40 hover:shadow-lg hover:cursor-pointer",
		},
	},
	defaultVariants: {
		disabled: false,
	},
});

const iconContainerVariants = cva("shrink-0 size-12 rounded-xl flex items-center justify-center", {
	variants: {
		disabled: {
			true: "bg-white/5",
			false: "bg-primary/10",
		},
	},
	defaultVariants: {
		disabled: false,
	},
});

const iconVariants = cva("size-6", {
	variants: {
		disabled: {
			true: "text-white/30",
			false: "text-primary",
		},
	},
	defaultVariants: {
		disabled: false,
	},
});

const titleVariants = cva("text-lg mb-1", {
	variants: {
		disabled: {
			true: "text-white/40",
			false: "text-white",
		},
	},
	defaultVariants: {
		disabled: false,
	},
});

const subtextVariants = cva("text-sm", {
	variants: {
		disabled: {
			true: "text-white/30",
			false: "text-white/60",
		},
	},
	defaultVariants: {
		disabled: false,
	},
});

interface Props {
	title: string;
	subText: string;
	icon: ComponentType<SVGProps<SVGSVGElement>>;
	onClick: () => void;
	disabled?: boolean;
}

const BgtBigButton = (props: Props) => {
	const { title, subText, icon: Icon, onClick, disabled = false } = props;

	return (
		<button type="button" onClick={onClick} disabled={disabled} className={buttonVariants({ disabled })}>
			<div className="flex items-start gap-4">
				<div className={iconContainerVariants({ disabled })}>
					<Icon className={iconVariants({ disabled })} />
				</div>
				<div className="flex-1 min-w-0">
					<h3 className={titleVariants({ disabled })}>{title}</h3>
					<p className={subtextVariants({ disabled })}>{subText}</p>
				</div>
			</div>
		</button>
	);
};

export default BgtBigButton;
